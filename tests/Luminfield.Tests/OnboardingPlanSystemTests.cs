using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class OnboardingPlanSystemTests
{
    private static readonly GridPosition[] OpeningCropCells =
    [
        new(12, 16),
        new(13, 16),
        new(14, 16),
        new(15, 16),
        new(16, 16)
    ];

    [Fact]
    public void NewGamePlanCoversFirstDayLoopWithoutMutatingSession()
    {
        var session = new GameSession();
        session.NewGame();
        var before = Snapshot(session);

        var plan = OnboardingPlanSystem.Create(session);

        Assert.Equal(6, plan.Cards.Count);
        Assert.Equal(
            [
                OnboardingPlanCardKind.Quest,
                OnboardingPlanCardKind.Weather,
                OnboardingPlanCardKind.Shipping,
                OnboardingPlanCardKind.Processor,
                OnboardingPlanCardKind.Commission,
                OnboardingPlanCardKind.Exploration
            ],
            plan.Cards.Select(card => card.Kind)
        );
        Assert.All(plan.Cards, card => Assert.True(card.CanDismiss));
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void NewGameCoverageContractCoversAllOpeningCapabilitiesWithoutMutatingSession()
    {
        var session = new GameSession();
        session.NewGame();
        var before = Snapshot(session);

        var contract =
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(session);

        Assert.Equal(90, contract.WindowMinutes);
        Assert.True(contract.HasCompletePromptCoverage);
        Assert.Equal(
            [
                OnboardingPlanCardKind.Quest,
                OnboardingPlanCardKind.Weather,
                OnboardingPlanCardKind.Shipping,
                OnboardingPlanCardKind.Processor,
                OnboardingPlanCardKind.Commission,
                OnboardingPlanCardKind.Exploration
            ],
            contract.Capabilities.Select(capability => capability.Kind)
        );
        Assert.Equal(
            [
                OnboardingPlanSystem.QuestCardId,
                OnboardingPlanSystem.WeatherCardId,
                OnboardingPlanSystem.ShippingCardId,
                OnboardingPlanSystem.ProcessorCardId,
                OnboardingPlanSystem.CommissionCardId,
                OnboardingPlanSystem.ExplorationCardId
            ],
            contract.Capabilities.Select(capability => capability.CardId)
        );
        Assert.All(
            contract.Capabilities,
            capability =>
            {
                Assert.Equal(OnboardingCoverageState.NewGame, capability.State);
                Assert.EndsWith(".action", capability.Prompt.ActionKey);
                Assert.EndsWith(".location", capability.Prompt.LocationKey);
                Assert.Contains(".result.new_game", capability.Prompt.ResultKey);
                Assert.NotEmpty(capability.Evidence);
            }
        );
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void OpeningInputRouteUsesPreviewBackedActionsThroughNextMorningHandoff()
    {
        var session = new GameSession();
        var journeySteps = new List<OpeningInputJourneyStep>();

        session.NewGame();
        var beforePlan = Snapshot(session);
        var plan = OnboardingPlanSystem.Create(session);
        var contract =
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(session);
        RecordInputCheck(
            journeySteps,
            "open_first_day_onboarding",
            "ui.open_onboarding_plan",
            "onboarding.coverage.quest.result.new_game",
            plan.Cards.Count == 6 &&
                plan.Cards.All(card => card.CanDismiss) &&
                contract.HasCompletePromptCoverage,
            "onboarding.qa.onboarding_plan_missing",
            Evidence(
                (
                    "cardIds",
                    string.Join(",", plan.Cards.Select(card => card.Id))
                ),
                ("coverageMinutes", contract.WindowMinutes.ToString()),
                ("promptCoverage", contract
                    .HasCompletePromptCoverage
                    .ToString())
            )
        );
        Assert.Equal(beforePlan, Snapshot(session));

        RecordInputCheck(
            journeySteps,
            "read_first_day_weather",
            "ui.read_weather_hint",
            "onboarding.coverage.weather.result.new_game",
            !string.IsNullOrWhiteSpace(session.Weather.CurrentId) &&
                !string.IsNullOrWhiteSpace(session.Weather.ForecastId),
            "onboarding.qa.weather_missing",
            Evidence(
                ("day", session.Clock.Day.ToString()),
                ("currentWeatherId", session.Weather.CurrentId),
                ("forecastWeatherId", session.Weather.ForecastId)
            )
        );

        MovePlayerNextTo(session, FarmLayout.CommissionBoardCell);
        RecordPreviewedInputAction(
            journeySteps,
            session,
            "open_commission_board_from_target_prompt",
            "input.interact",
            FarmLayout.CommissionBoardCell,
            TargetPreviewKind.CommissionBoard,
            "target.action.open_commission",
            () => session.UseSelected(FarmLayout.CommissionBoardCell),
            result => Evidence(
                ("messageKey", result.MessageKey),
                ("commissionId", session.Commission.Current.Id),
                ("playerCell", Cell(session.PlayerCell))
            )
        );
        RecordInputAction(
            journeySteps,
            "accept_commission_from_board_overlay",
            "ui.confirm_commission",
            "onboarding.coverage.commission.result.in_progress",
            session.AcceptDailyCommission(),
            result => Evidence(
                ("messageKey", result.MessageKey),
                ("commissionId", session.Commission.Current.Id),
                ("accepted", session.Commission.Accepted.ToString())
            )
        );

        MovePlayerNextTo(session, FarmLayout.MiraCell);
        RecordPreviewedInputAction(
            journeySteps,
            session,
            "talk_to_mira_from_target_prompt",
            "input.interact",
            FarmLayout.MiraCell,
            TargetPreviewKind.Character,
            "target.action.talk",
            () => session.InteractWithMira()
                ? ActionResult.Success(messageKey: "notice.seeds_received")
                : ActionResult.Fail("onboarding.qa.mira_seed_gift_failed"),
            result => Evidence(
                ("messageKey", result.MessageKey),
                ("seedCount", session.Inventory
                    .Count(DataCatalog.StarbudSeedId)
                    .ToString()),
                ("stage", session.Quest.Stage.ToString()),
                ("previewSource", "GameSession.PreviewSelectedTarget"),
                ("playerCell", Cell(session.PlayerCell))
            )
        );

        session.Inventory.Select(1);
        foreach (var cell in OpeningCropCells)
        {
            RecordPreviewedInputAction(
                journeySteps,
                session,
                $"till_starbud_bed_{Cell(cell)}",
                "input.hotbar_shovel_then_interact",
                cell,
                TargetPreviewKind.Ground,
                "target.action.till",
                () => session.UseSelected(cell),
                result => Evidence(
                    ("messageKey", result.MessageKey),
                    ("selectedItemId", session.Inventory.Selected.ItemId),
                    ("tilledTiles", session.Farm.Tiles.Count.ToString()),
                    ("stage", session.Quest.Stage.ToString())
                )
            );
        }

        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.StarbudSeedId
        ));
        foreach (var cell in OpeningCropCells)
        {
            RecordPreviewedInputAction(
                journeySteps,
                session,
                $"plant_starbud_{Cell(cell)}",
                "input.hotbar_seed_then_interact",
                cell,
                TargetPreviewKind.Soil,
                "target.action.plant",
                () => session.UseSelected(cell),
                result => Evidence(
                    ("messageKey", result.MessageKey),
                    ("selectedItemId", session.Inventory.Selected.ItemId),
                    ("remainingSeeds", session.Inventory
                        .Count(DataCatalog.StarbudSeedId)
                        .ToString()),
                    ("commissionProgress", session
                        .Commission
                        .Progress
                        .ToString()),
                    ("stage", session.Quest.Stage.ToString())
                )
            );
        }

        session.Inventory.Select(3);
        foreach (var cell in OpeningCropCells)
        {
            RecordPreviewedInputAction(
                journeySteps,
                session,
                $"water_starbud_{Cell(cell)}",
                "input.hotbar_watering_can_then_interact",
                cell,
                TargetPreviewKind.Crop,
                "target.action.water",
                () => session.UseSelected(cell),
                result => Evidence(
                    ("messageKey", result.MessageKey),
                    ("selectedItemId", session.Inventory.Selected.ItemId),
                    ("wateringCanWater", session.WateringCanWater.ToString()),
                    ("stage", session.Quest.Stage.ToString())
                )
            );
        }

        var forecastBeforeSleep = session.Weather.ForecastId;
        session.SetPlayerLocation(
            CottageLayout.BedCell.X * 16 + 8,
            CottageLayout.BedCell.Y * 16 + 8,
            PlayerLocationIds.Cottage
        );
        var sleepPreview = PreviewWithoutMutation(
            session,
            CottageLayout.BedCell
        );
        Assert.Equal(TargetPreviewState.Available, sleepPreview.State);
        Assert.Equal(TargetPreviewKind.Bed, sleepPreview.Kind);
        Assert.Equal("target.action.rest", sleepPreview.LabelKey);
        var sleepResult = session.RestInCottage();
        Assert.True(sleepResult.Succeeded, sleepResult.MessageKey);
        var settlement = session.EndDay();
        RecordPreviewedInputStep(
            journeySteps,
            "sleep_in_cottage_from_bed_prompt",
            "input.interact_bed",
            CottageLayout.BedCell,
            sleepPreview,
            sleepResult,
            Evidence(
                ("messageKey", sleepResult.MessageKey),
                ("day", session.Clock.Day.ToString()),
                ("currentWeatherId", session.Weather.CurrentId),
                ("expectedWeatherId", forecastBeforeSleep),
                ("settledItems", settlement.TotalItems.ToString())
            )
        );

        var nextMorningSnapshot = Snapshot(session);
        var nextMorningContract =
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(session);
        var morningBriefing = MorningBriefingSystem.Create(session);
        RecordInputCheck(
            journeySteps,
            "handoff_to_next_morning_briefing",
            "ui.open_morning_briefing",
            "onboarding.qa.morning_briefing_handoff_ready",
            session.Clock.Day == 2 &&
                session.Weather.CurrentId == forecastBeforeSleep &&
                morningBriefing.Cards.Count == 7 &&
                Capability(nextMorningContract, OnboardingPlanCardKind.Weather)
                    .State == OnboardingCoverageState.Complete,
            "onboarding.qa.morning_briefing_handoff_failed",
            Evidence(
                ("day", session.Clock.Day.ToString()),
                ("currentWeatherId", session.Weather.CurrentId),
                ("weatherCoverageState", Capability(
                    nextMorningContract,
                    OnboardingPlanCardKind.Weather
                ).State.ToString()),
                ("briefingCardIds", string.Join(
                    ",",
                    morningBriefing.Cards.Select(card => card.Id)
                )),
                ("integrationOwner", "BASE.MorningBriefingSystem")
            )
        );
        Assert.Equal(nextMorningSnapshot, Snapshot(session));

        var journey = new OpeningInputJourneyLog(journeySteps);
        Assert.True(journey.Succeeded);
        Assert.Empty(journey.Failures);
        Assert.DoesNotContain(journey.Steps, step => step.IsRestoreStep);
        Assert.All(
            journey.PreviewBackedActions,
            step => Assert.True(step.HasPreviewAndResult, step.StepId)
        );
        Assert.All(
            journey.Steps,
            step => Assert.True(step.HasStableEvidence, step.StepId)
        );
        Assert.Contains(
            journey.Steps,
            step => step.StepId == "talk_to_mira_from_target_prompt" &&
                step.Evidence["previewSource"] ==
                    "GameSession.PreviewSelectedTarget"
        );
        Assert.Contains(
            journey.Steps,
            step => step.StepId.StartsWith(
                "till_starbud_bed_",
                StringComparison.Ordinal
            )
        );
        Assert.Contains(
            journey.Steps,
            step => step.StepId.StartsWith(
                "plant_starbud_",
                StringComparison.Ordinal
            )
        );
        Assert.Contains(
            journey.Steps,
            step => step.StepId.StartsWith(
                "water_starbud_",
                StringComparison.Ordinal
            )
        );
        Assert.Contains(
            journey.Steps,
            step => step.StepId == "sleep_in_cottage_from_bed_prompt"
        );
        Assert.Contains(
            journey.Steps,
            step => step.StepId == "handoff_to_next_morning_briefing"
        );
    }

    [Fact]
    public void OpeningFixedTargetsUseCorePreviewForHandAndWrongTool()
    {
        var session = new GameSession();
        session.NewGame();

        var targets = new[]
        {
            (
                FarmLayout.MiraCell,
                TargetPreviewKind.Character,
                "target.action.talk"
            ),
            (
                FarmLayout.CottageDoorCell,
                TargetPreviewKind.Door,
                "target.action.enter"
            )
        };

        foreach (var (target, kind, actionKey) in targets)
        {
            var available = PreviewWithoutMutation(session, target);
            Assert.Equal(TargetPreviewState.Available, available.State);
            Assert.Equal(kind, available.Kind);
            Assert.Equal(actionKey, available.LabelKey);
        }

        session.Inventory.Select(1);
        foreach (var (target, kind, _) in targets)
        {
            var needsHand = PreviewWithoutMutation(session, target);
            Assert.Equal(TargetPreviewState.NeedsTool, needsHand.State);
            Assert.Equal(kind, needsHand.Kind);
            Assert.Equal("target.need.hand", needsHand.LabelKey);
        }
    }

    [Fact]
    public void PartialProgressCoverageContractReportsInProgressWithoutMutatingSession()
    {
        var session = new GameSession();
        session.NewGame();
        session.InteractWithMira();
        session.Clock.AdvanceRealTime(GameClock.SecondsPerTick);
        session.Inventory.Add(DataCatalog.StarbudId, 3);
        Assert.True(session.QueueForShipping(DataCatalog.StarbudId).Succeeded);
        Assert.True(session.AcceptDailyCommission().Succeeded);
        Assert.True(session.StartProcessing(
            DataCatalog.StarbudPreserveRecipeId
        ).Succeeded);
        var before = Snapshot(session);

        var contract =
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(session);

        Assert.All(
            contract.Capabilities,
            capability =>
            {
                Assert.Equal(
                    OnboardingCoverageState.InProgress,
                    capability.State
                );
                Assert.Contains(
                    ".result.in_progress",
                    capability.Prompt.ResultKey
                );
            }
        );
        Assert.Equal("1", Capability(contract, OnboardingPlanCardKind.Shipping)
            .Evidence["pendingItems"]);
        Assert.Equal(
            DataCatalog.StarbudPreserveRecipeId,
            Capability(contract, OnboardingPlanCardKind.Processor)
                .Evidence["activeRecipeId"]
        );
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void CompletedFlowAuditKeepsDailyCommissionCompletionWithoutParallelState()
    {
        var run = RunOpeningLoopToProcessorReady();
        var session = run.Session;
        var before = Snapshot(session);

        var contract =
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(session);

        Assert.Equal(
            OnboardingCoverageState.Complete,
            Capability(contract, OnboardingPlanCardKind.Quest).State
        );
        Assert.Equal(
            OnboardingCoverageState.Complete,
            Capability(contract, OnboardingPlanCardKind.Weather).State
        );
        Assert.Equal(
            OnboardingCoverageState.Complete,
            Capability(contract, OnboardingPlanCardKind.Shipping).State
        );
        Assert.Equal(
            OnboardingCoverageState.Complete,
            Capability(contract, OnboardingPlanCardKind.Processor).State
        );
        Assert.Equal(
            OnboardingCoverageState.NewGame,
            Capability(contract, OnboardingPlanCardKind.Commission).State
        );
        Assert.Equal(
            OnboardingCoverageState.Complete,
            Capability(contract, OnboardingPlanCardKind.Exploration).State
        );
        Assert.Contains(
            run.AuditSteps,
            step => step.StepId == "claim_plant_commission" &&
                step.Succeeded &&
                step.ResultKey == "onboarding.coverage.commission.result.complete"
        );
        Assert.Equal("1", Capability(contract, OnboardingPlanCardKind.Shipping)
            .Evidence["lastSettlementItems"]);
        Assert.Equal("1", Capability(contract, OnboardingPlanCardKind.Processor)
            .Evidence["readyCount"]);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void OpeningNinetyMinuteFlowRunsContinuouslyFromNewGameThroughRestore()
    {
        var run = RunOpeningLoopToProcessorReady();
        var session = run.Session;

        var collected = session.CollectProcessedItem();
        RecordAction(
            run.AuditSteps,
            "collect_finished_processor",
            OnboardingPlanCardKind.Processor,
            "onboarding.coverage.processor.action",
            "onboarding.coverage.processor.location",
            "onboarding.coverage.processor.result.complete",
            collected,
            Evidence(
                ("itemId", collected.GrantedItemId ?? string.Empty),
                ("itemCount", collected.GrantedItemCount.ToString()),
                ("processorIdle", session.Processor.IsIdle.ToString())
            )
        );

        var audit = new OnboardingNinetyMinuteFlowAudit(run.AuditSteps);
        Assert.True(audit.Succeeded);
        Assert.Empty(audit.Failures);
        Assert.All(
            audit.Steps,
            step =>
            {
                Assert.True(step.HasStableEvidence);
                Assert.True(
                    string.IsNullOrWhiteSpace(step.FailureKey),
                    step.FailureKey
                );
            }
        );
        Assert.Contains(
            audit.Steps,
            step => step.StepId == "restore_after_weather_day"
        );
        Assert.Contains(
            audit.Steps,
            step => step.StepId == "restore_after_shipping_and_processing"
        );
        Assert.Equal(QuestStage.Complete, session.Quest.Stage);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarbudPreserveId));
    }

    [Fact]
    public void CollectedProcessorGoodKeepsCoverageCompleteAfterSaveRestore()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Add(DataCatalog.StarbudId, 3);
        Assert.True(session.StartProcessing(
            DataCatalog.StarbudPreserveRecipeId
        ).Succeeded);
        session.Processor.ResolveNight();
        Assert.True(session.CollectProcessedItem().Succeeded);
        Assert.True(session.Processor.IsIdle);

        var restored = new GameSession();
        restored.Restore(session.Capture());
        var contract =
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(restored);
        var processor = Capability(
            contract,
            OnboardingPlanCardKind.Processor
        );

        Assert.Equal(OnboardingCoverageState.Complete, processor.State);
        Assert.True(restored.Collection.IsDiscovered(
            DataCatalog.StarbudPreserveId
        ));
    }

    [Fact]
    public void DismissedCardsAreOmittedFromTheReadOnlyPlan()
    {
        var session = new GameSession();
        session.NewGame();

        var plan = OnboardingPlanSystem.Create(
            session,
            [
                OnboardingPlanSystem.ShippingCardId,
                OnboardingPlanSystem.ProcessorCardId
            ]
        );

        Assert.DoesNotContain(
            plan.Cards,
            card => card.Id == OnboardingPlanSystem.ShippingCardId
        );
        Assert.DoesNotContain(
            plan.Cards,
            card => card.Id == OnboardingPlanSystem.ProcessorCardId
        );
        Assert.Contains(
            plan.Cards,
            card => card.Id == OnboardingPlanSystem.QuestCardId
        );
    }

    [Fact]
    public void ActiveSystemsPromoteReadyWorkWithoutDuplicatingProgressState()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Add(DataCatalog.StarbudId, 3);
        Assert.True(session.Shipping.QueueOne(DataCatalog.StarbudId, session.Inventory).Succeeded);
        Assert.True(session.Commission.Accept().Succeeded);
        Assert.True(session.Processor.Start(
            DataCatalog.StarbudPreserveRecipeId,
            session.Inventory
        ).Succeeded);
        session.Processor.ResolveNight();

        var plan = OnboardingPlanSystem.Create(session);
        var shipping = Card(plan, OnboardingPlanSystem.ShippingCardId);
        var processor = Card(plan, OnboardingPlanSystem.ProcessorCardId);
        var commission = Card(plan, OnboardingPlanSystem.CommissionCardId);

        Assert.Equal("onboarding.shipping.pending", shipping.BodyKey);
        Assert.Equal("1", shipping.Values["pendingItems"]);
        Assert.Equal("onboarding.processor.ready", processor.BodyKey);
        Assert.Equal(OnboardingPlanPriority.Primary, processor.Priority);
        Assert.Equal("onboarding.commission.active", commission.BodyKey);
    }

    private static OnboardingPlanCard Card(OnboardingPlan plan, string id) =>
        plan.Cards.Single(card => card.Id == id);

    private static OnboardingCapabilityCoverage Capability(
        OnboardingNinetyMinuteCoverageContract contract,
        OnboardingPlanCardKind kind
    ) => contract.Capabilities.Single(capability => capability.Kind == kind);

    private sealed record OpeningLoopRun(
        GameSession Session,
        List<OnboardingFlowStepAudit> AuditSteps
    );

    private sealed record OpeningInputJourneyLog(
        IReadOnlyList<OpeningInputJourneyStep> Steps
    )
    {
        public bool Succeeded => Steps.All(step => step.Succeeded);

        public IReadOnlyList<OpeningInputJourneyStep> Failures =>
            Steps.Where(step => !step.Succeeded).ToArray();

        public IReadOnlyList<OpeningInputJourneyStep> PreviewBackedActions =>
            Steps.Where(step => !string.IsNullOrWhiteSpace(step.Target))
                .ToArray();
    }

    private sealed record OpeningInputJourneyStep(
        string StepId,
        string InputAction,
        string Target,
        string PreviewState,
        string PreviewKind,
        string PreviewLabelKey,
        string ResultKey,
        bool Succeeded,
        string FailureKey,
        IReadOnlyDictionary<string, string> Evidence
    )
    {
        public bool HasPreviewAndResult =>
            !string.IsNullOrWhiteSpace(Target) &&
            !string.IsNullOrWhiteSpace(PreviewState) &&
            !string.IsNullOrWhiteSpace(PreviewKind) &&
            !string.IsNullOrWhiteSpace(PreviewLabelKey) &&
            !string.IsNullOrWhiteSpace(ResultKey);

        public bool HasStableEvidence =>
            Evidence.Count > 0 &&
            Evidence.All(pair =>
                !string.IsNullOrWhiteSpace(pair.Key) &&
                pair.Value is not null
            );

        public bool IsRestoreStep =>
            StepId.Contains("restore", StringComparison.OrdinalIgnoreCase) ||
            InputAction.Contains(
                "restore",
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static void RecordPreviewedInputAction(
        List<OpeningInputJourneyStep> steps,
        GameSession session,
        string stepId,
        string inputAction,
        GridPosition target,
        TargetPreviewKind expectedKind,
        string expectedLabelKey,
        Func<ActionResult> action,
        Func<ActionResult, IReadOnlyDictionary<string, string>> evidence
    )
    {
        var preview = PreviewWithoutMutation(session, target);
        Assert.Equal(TargetPreviewState.Available, preview.State);
        Assert.Equal(expectedKind, preview.Kind);
        Assert.Equal(expectedLabelKey, preview.LabelKey);

        var result = action();
        RecordPreviewedInputStep(
            steps,
            stepId,
            inputAction,
            target,
            preview,
            result,
            evidence(result)
        );
        Assert.True(result.Succeeded, $"{stepId}: {result.MessageKey}");
    }

    private static void RecordPreviewedInputStep(
        List<OpeningInputJourneyStep> steps,
        string stepId,
        string inputAction,
        GridPosition target,
        TargetPreview preview,
        ActionResult result,
        IReadOnlyDictionary<string, string> evidence
    )
    {
        var resultKey = string.IsNullOrWhiteSpace(result.MessageKey)
            ? "onboarding.qa.action_succeeded"
            : result.MessageKey;
        steps.Add(new OpeningInputJourneyStep(
            stepId,
            inputAction,
            Cell(target),
            preview.State.ToString(),
            preview.Kind.ToString(),
            preview.LabelKey,
            resultKey,
            result.Succeeded,
            result.Succeeded ? string.Empty : result.MessageKey,
            evidence
        ));
    }

    private static void RecordInputAction(
        List<OpeningInputJourneyStep> steps,
        string stepId,
        string inputAction,
        string resultKey,
        ActionResult result,
        Func<ActionResult, IReadOnlyDictionary<string, string>> evidence
    )
    {
        steps.Add(new OpeningInputJourneyStep(
            stepId,
            inputAction,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.IsNullOrWhiteSpace(result.MessageKey)
                ? resultKey
                : result.MessageKey,
            result.Succeeded,
            result.Succeeded ? string.Empty : result.MessageKey,
            evidence(result)
        ));
        Assert.True(result.Succeeded, $"{stepId}: {result.MessageKey}");
    }

    private static void RecordInputCheck(
        List<OpeningInputJourneyStep> steps,
        string stepId,
        string inputAction,
        string resultKey,
        bool succeeded,
        string failureKey,
        IReadOnlyDictionary<string, string> evidence
    )
    {
        steps.Add(new OpeningInputJourneyStep(
            stepId,
            inputAction,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            resultKey,
            succeeded,
            succeeded ? string.Empty : failureKey,
            evidence
        ));
        Assert.True(succeeded, $"{stepId}: {failureKey}");
    }

    private static TargetPreview PreviewWithoutMutation(
        GameSession session,
        GridPosition target
    )
    {
        var before = Snapshot(session);
        var preview = session.PreviewSelectedTarget(target);
        Assert.Equal(before, Snapshot(session));
        return preview;
    }

    private static void MovePlayerNextTo(
        GameSession session,
        GridPosition target
    )
    {
        session.SetPlayerLocation(
            target.X * 16 + 8,
            (target.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
    }

    private static OpeningLoopRun RunOpeningLoopToProcessorReady()
    {
        var session = new GameSession();
        var audit = new List<OnboardingFlowStepAudit>();

        session.NewGame();
        RecordCheck(
            audit,
            "new_game",
            OnboardingPlanCardKind.Quest,
            "onboarding.coverage.quest.action",
            "onboarding.coverage.quest.location",
            "onboarding.coverage.quest.result.new_game",
            session.Clock.Day == 1 &&
                session.Quest.Stage == QuestStage.TalkToMira,
            "onboarding.qa.new_game_failed",
            Evidence(
                ("day", session.Clock.Day.ToString()),
                ("stage", session.Quest.Stage.ToString()),
                ("location", session.PlayerLocationId)
            )
        );

        var openingForecastId = session.Weather.ForecastId;
        RecordCheck(
            audit,
            "read_weather_and_forecast",
            OnboardingPlanCardKind.Weather,
            "onboarding.coverage.weather.action",
            "onboarding.coverage.weather.location",
            "onboarding.coverage.weather.result.new_game",
            !string.IsNullOrWhiteSpace(openingForecastId),
            "onboarding.qa.weather_missing",
            Evidence(
                ("currentWeatherId", session.Weather.CurrentId),
                ("forecastWeatherId", openingForecastId),
                ("day", session.Clock.Day.ToString())
            )
        );

        RecordAction(
            audit,
            "open_commission_board",
            OnboardingPlanCardKind.Commission,
            "onboarding.coverage.commission.action",
            "onboarding.coverage.commission.location",
            "onboarding.coverage.commission.result.new_game",
            session.UseSelected(FarmLayout.CommissionBoardCell),
            Evidence(
                ("cell", Cell(FarmLayout.CommissionBoardCell)),
                ("commissionId", session.Commission.Current.Id)
            )
        );
        RecordAction(
            audit,
            "accept_daily_commission",
            OnboardingPlanCardKind.Commission,
            "onboarding.coverage.commission.action",
            "onboarding.coverage.commission.location",
            "onboarding.coverage.commission.result.in_progress",
            session.AcceptDailyCommission(),
            Evidence(
                ("commissionId", session.Commission.Current.Id),
                ("accepted", session.Commission.Accepted.ToString())
            )
        );

        var receivedSeeds = session.InteractWithMira();
        RecordCheck(
            audit,
            "talk_to_mira_for_seed_gift",
            OnboardingPlanCardKind.Quest,
            "onboarding.coverage.quest.action",
            "onboarding.coverage.quest.location",
            "onboarding.coverage.quest.result.in_progress",
            receivedSeeds &&
                session.Inventory.Count(DataCatalog.StarbudSeedId) == 5 &&
                session.Quest.Stage == QuestStage.Till,
            "onboarding.qa.mira_seed_gift_failed",
            Evidence(
                ("cell", Cell(FarmLayout.MiraCell)),
                ("seedCount", session.Inventory
                    .Count(DataCatalog.StarbudSeedId)
                    .ToString()),
                ("stage", session.Quest.Stage.ToString())
            )
        );

        session.Inventory.Select(1);
        RecordAction(
            audit,
            "till_five_starbud_beds",
            OnboardingPlanCardKind.Quest,
            "onboarding.coverage.quest.action",
            "onboarding.coverage.quest.location",
            "onboarding.coverage.quest.result.in_progress",
            UseSelectedMany(session, OpeningCropCells),
            Evidence(
                ("selectedItemId", session.Inventory.Selected.ItemId),
                ("tilledTiles", session.Farm.Tiles.Count.ToString()),
                ("stage", session.Quest.Stage.ToString())
            )
        );

        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.StarbudSeedId
        ));
        RecordAction(
            audit,
            "plant_five_starbuds",
            OnboardingPlanCardKind.Quest,
            "onboarding.coverage.quest.action",
            "onboarding.coverage.quest.location",
            "onboarding.coverage.quest.result.in_progress",
            UseSelectedMany(session, OpeningCropCells),
            Evidence(
                ("selectedItemId", session.Inventory.Selected.ItemId),
                ("remainingSeeds", session.Inventory
                    .Count(DataCatalog.StarbudSeedId)
                    .ToString()),
                ("commissionProgress", session.Commission.Progress.ToString()),
                ("stage", session.Quest.Stage.ToString())
            )
        );

        var commissionClaim = session.ClaimDailyCommission();
        RecordCommissionClaim(
            audit,
            "claim_plant_commission",
            commissionClaim,
            Evidence(
                ("commissionId", session.Commission.Current.Id),
                ("claimed", session.Commission.Claimed.ToString()),
                ("rewardCoins", commissionClaim.RewardCoins.ToString()),
                ("coins", session.Coins.ToString())
            )
        );

        session.Inventory.Select(3);
        RecordAction(
            audit,
            "water_five_starbuds",
            OnboardingPlanCardKind.Quest,
            "onboarding.coverage.quest.action",
            "onboarding.coverage.quest.location",
            "onboarding.coverage.quest.result.in_progress",
            UseSelectedMany(session, OpeningCropCells),
            Evidence(
                ("selectedItemId", session.Inventory.Selected.ItemId),
                ("water", session.WateringCanWater.ToString()),
                ("stage", session.Quest.Stage.ToString())
            )
        );

        var dayOneForecast = session.Weather.ForecastId;
        session.EndDay();
        RecordCheck(
            audit,
            "sleep_to_weather_day",
            OnboardingPlanCardKind.Weather,
            "onboarding.coverage.weather.action",
            "onboarding.coverage.weather.location",
            "onboarding.coverage.weather.result.in_progress",
            session.Clock.Day == 2 &&
                session.Weather.CurrentId == dayOneForecast,
            "onboarding.qa.weather_forecast_mismatch",
            Evidence(
                ("day", session.Clock.Day.ToString()),
                ("expectedWeatherId", dayOneForecast),
                ("currentWeatherId", session.Weather.CurrentId)
            )
        );
        session = RestoreCheckpoint(
            session,
            audit,
            "restore_after_weather_day",
            OnboardingPlanCardKind.Weather
        );

        var dayTwoForecast = session.Weather.ForecastId;
        session.EndDay();
        RecordCheck(
            audit,
            "sleep_to_harvest_day",
            OnboardingPlanCardKind.Quest,
            "onboarding.coverage.quest.action",
            "onboarding.coverage.quest.location",
            "onboarding.coverage.quest.result.in_progress",
            session.Clock.Day == 3 &&
                session.Quest.Stage == QuestStage.Harvest &&
                session.Weather.CurrentId == dayTwoForecast,
            "onboarding.qa.crop_growth_failed",
            Evidence(
                ("day", session.Clock.Day.ToString()),
                ("stage", session.Quest.Stage.ToString()),
                ("currentWeatherId", session.Weather.CurrentId)
            )
        );
        session = RestoreCheckpoint(
            session,
            audit,
            "restore_after_crop_growth",
            OnboardingPlanCardKind.Quest
        );

        session.Inventory.Select(0);
        RecordAction(
            audit,
            "harvest_five_starbuds",
            OnboardingPlanCardKind.Quest,
            "onboarding.coverage.quest.action",
            "onboarding.coverage.quest.location",
            "onboarding.coverage.quest.result.in_progress",
            UseSelectedMany(session, OpeningCropCells),
            Evidence(
                ("selectedItemId", session.Inventory.Selected.ItemId),
                ("starbudCount", session.Inventory
                    .Count(DataCatalog.StarbudId)
                    .ToString()),
                ("stage", session.Quest.Stage.ToString())
            )
        );

        session.InteractWithMira();
        RecordCheck(
            audit,
            "return_to_mira_complete_quest",
            OnboardingPlanCardKind.Quest,
            "onboarding.coverage.quest.action",
            "onboarding.coverage.quest.location",
            "onboarding.coverage.quest.result.complete",
            session.Quest.Stage == QuestStage.Complete,
            "onboarding.qa.quest_completion_failed",
            Evidence(
                ("cell", Cell(FarmLayout.MiraCell)),
                ("stage", session.Quest.Stage.ToString())
            )
        );

        session.SetPlayerLocation(
            70 * 16 + 8,
            20 * 16 + 8,
            PlayerLocationIds.World
        );
        RecordCheck(
            audit,
            "discover_first_external_chunk",
            OnboardingPlanCardKind.Exploration,
            "onboarding.coverage.exploration.action",
            "onboarding.coverage.exploration.location",
            "onboarding.coverage.exploration.result.complete",
            session.Exploration.DiscoveredChunks.Count > 1,
            "onboarding.qa.exploration_failed",
            Evidence(
                ("playerCell", Cell(session.PlayerCell)),
                ("discoveredChunks", session.Exploration
                    .DiscoveredChunks
                    .Count
                    .ToString())
            )
        );

        RecordAction(
            audit,
            "start_starbud_processing",
            OnboardingPlanCardKind.Processor,
            "onboarding.coverage.processor.action",
            "onboarding.coverage.processor.location",
            "onboarding.coverage.processor.result.in_progress",
            session.StartProcessing(DataCatalog.StarbudPreserveRecipeId),
            Evidence(
                ("recipeId", DataCatalog.StarbudPreserveRecipeId),
                ("remainingNights", session.Processor.RemainingNights.ToString()),
                ("starbudCount", session.Inventory
                    .Count(DataCatalog.StarbudId)
                    .ToString())
            )
        );

        RecordAction(
            audit,
            "queue_starbud_for_shipping",
            OnboardingPlanCardKind.Shipping,
            "onboarding.coverage.shipping.action",
            "onboarding.coverage.shipping.location",
            "onboarding.coverage.shipping.result.in_progress",
            session.QueueForShipping(DataCatalog.StarbudId),
            Evidence(
                ("itemId", DataCatalog.StarbudId),
                ("pendingItems", session.Shipping.PendingItemCount.ToString()),
                ("starbudCount", session.Inventory
                    .Count(DataCatalog.StarbudId)
                    .ToString())
            )
        );

        var settlement = session.EndDay();
        RecordCheck(
            audit,
            "settle_shipping_and_finish_processor",
            OnboardingPlanCardKind.Shipping,
            "onboarding.coverage.shipping.action",
            "onboarding.coverage.shipping.location",
            "onboarding.coverage.shipping.result.complete",
            session.Clock.Day == 4 &&
                settlement.TotalItems == 1 &&
                session.Processor.IsReady,
            "onboarding.qa.shipping_or_processor_failed",
            Evidence(
                ("day", session.Clock.Day.ToString()),
                ("settledItems", settlement.TotalItems.ToString()),
                ("settledCoins", settlement.TotalCoins.ToString()),
                ("processorReadyCount", session.Processor.ReadyCount.ToString())
            )
        );
        session = RestoreCheckpoint(
            session,
            audit,
            "restore_after_shipping_and_processing",
            OnboardingPlanCardKind.Shipping
        );

        return new OpeningLoopRun(session, audit);
    }

    private static ActionResult UseSelectedMany(
        GameSession session,
        IReadOnlyList<GridPosition> targets
    )
    {
        foreach (var target in targets)
        {
            var result = session.UseSelected(target);
            if (!result.Succeeded)
            {
                return result;
            }
        }

        return ActionResult.Success(messageKey: "onboarding.qa.ok");
    }

    private static GameSession RestoreCheckpoint(
        GameSession session,
        List<OnboardingFlowStepAudit> audit,
        string stepId,
        OnboardingPlanCardKind kind
    )
    {
        var save = session.Capture();
        var before = FlowCriticalSnapshot(save);
        var restored = new GameSession();
        restored.Restore(save);
        var afterSave = restored.Capture();
        var after = FlowCriticalSnapshot(afterSave);
        var fullCaptureEqual = SnapshotFromSave(save) == SnapshotFromSave(afterSave);
        var succeeded = before == after;
        RecordCheck(
            audit,
            stepId,
            kind,
            $"onboarding.coverage.{CoverageKey(kind)}.action",
            $"onboarding.coverage.{CoverageKey(kind)}.location",
            $"onboarding.coverage.{CoverageKey(kind)}.result.in_progress",
            succeeded,
            $"onboarding.qa.restore_changed_capture.{CriticalDifference(save, afterSave)}",
            Evidence(
                ("day", restored.Clock.Day.ToString()),
                ("minuteOfDay", restored.Clock.MinuteOfDay.ToString()),
                ("location", restored.PlayerLocationId),
                ("fullCaptureEqual", fullCaptureEqual.ToString())
            )
        );
        Assert.Equal(before, after);
        return restored;
    }

    private static void RecordAction(
        List<OnboardingFlowStepAudit> audit,
        string stepId,
        OnboardingPlanCardKind kind,
        string actionKey,
        string locationKey,
        string resultKey,
        ActionResult result,
        IReadOnlyDictionary<string, string> evidence
    )
    {
        audit.Add(new OnboardingFlowStepAudit(
            stepId,
            kind,
            actionKey,
            locationKey,
            resultKey,
            result.Succeeded,
            result.Succeeded ? string.Empty : result.MessageKey,
            evidence
        ));
        Assert.True(result.Succeeded, $"{stepId}: {result.MessageKey}");
    }

    private static void RecordCommissionClaim(
        List<OnboardingFlowStepAudit> audit,
        string stepId,
        DailyCommissionClaimResult result,
        IReadOnlyDictionary<string, string> evidence
    )
    {
        audit.Add(new OnboardingFlowStepAudit(
            stepId,
            OnboardingPlanCardKind.Commission,
            "onboarding.coverage.commission.action",
            "onboarding.coverage.commission.location",
            "onboarding.coverage.commission.result.complete",
            result.Succeeded,
            result.Succeeded ? string.Empty : result.MessageKey,
            evidence
        ));
        Assert.True(result.Succeeded, $"{stepId}: {result.MessageKey}");
    }

    private static void RecordCheck(
        List<OnboardingFlowStepAudit> audit,
        string stepId,
        OnboardingPlanCardKind kind,
        string actionKey,
        string locationKey,
        string resultKey,
        bool succeeded,
        string failureKey,
        IReadOnlyDictionary<string, string> evidence
    )
    {
        audit.Add(new OnboardingFlowStepAudit(
            stepId,
            kind,
            actionKey,
            locationKey,
            resultKey,
            succeeded,
            succeeded ? string.Empty : failureKey,
            evidence
        ));
        Assert.True(succeeded, $"{stepId}: {failureKey}");
    }

    private static IReadOnlyDictionary<string, string> Evidence(
        params (string Key, string Value)[] pairs
    ) => pairs.ToDictionary(
        pair => pair.Key,
        pair => pair.Value,
        StringComparer.Ordinal
    );

    private static string Cell(GridPosition cell) => $"{cell.X},{cell.Y}";

    private static string CoverageKey(OnboardingPlanCardKind kind) =>
        kind switch
        {
            OnboardingPlanCardKind.Quest => "quest",
            OnboardingPlanCardKind.Weather => "weather",
            OnboardingPlanCardKind.Shipping => "shipping",
            OnboardingPlanCardKind.Processor => "processor",
            OnboardingPlanCardKind.Commission => "commission",
            OnboardingPlanCardKind.Exploration => "exploration",
            _ => "quest"
        };

    private static string SnapshotFromSave(GameSaveV1 save) =>
        JsonSerializer.Serialize(save);

    private static string FlowCriticalSnapshot(GameSaveV1 save) =>
        JsonSerializer.Serialize(new
        {
            save.Day,
            save.MinuteOfDay,
            save.Player,
            save.Inventory,
            save.FarmTiles,
            save.Quest,
            save.Coins,
            save.Processor,
            save.Exploration,
            save.Weather,
            save.Shipping,
            save.Commission
        });

    private static string CriticalDifference(
        GameSaveV1 before,
        GameSaveV1 after
    )
    {
        if (before.Day != after.Day)
        {
            return "day";
        }

        if (before.MinuteOfDay != after.MinuteOfDay)
        {
            return "minute";
        }

        if (JsonSerializer.Serialize(before.Player) !=
            JsonSerializer.Serialize(after.Player))
        {
            return "player";
        }

        if (JsonSerializer.Serialize(before.Inventory) !=
            JsonSerializer.Serialize(after.Inventory))
        {
            return "inventory";
        }

        if (JsonSerializer.Serialize(before.FarmTiles) !=
            JsonSerializer.Serialize(after.FarmTiles))
        {
            return "farm";
        }

        if (JsonSerializer.Serialize(before.Quest) !=
            JsonSerializer.Serialize(after.Quest))
        {
            return "quest";
        }

        if (before.Coins != after.Coins)
        {
            return "coins";
        }

        if (JsonSerializer.Serialize(before.Processor) !=
            JsonSerializer.Serialize(after.Processor))
        {
            return "processor";
        }

        if (JsonSerializer.Serialize(before.Exploration) !=
            JsonSerializer.Serialize(after.Exploration))
        {
            return "exploration";
        }

        if (JsonSerializer.Serialize(before.Weather) !=
            JsonSerializer.Serialize(after.Weather))
        {
            return "weather";
        }

        if (JsonSerializer.Serialize(before.Shipping) !=
            JsonSerializer.Serialize(after.Shipping))
        {
            return "shipping";
        }

        if (JsonSerializer.Serialize(before.Commission) !=
            JsonSerializer.Serialize(after.Commission))
        {
            return "commission";
        }

        return "unknown";
    }

    private static string Snapshot(GameSession session) =>
        JsonSerializer.Serialize(session.Capture());
}
