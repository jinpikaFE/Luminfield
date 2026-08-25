using System.Text.Json;
using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class StarfallWatchBoardTests
{
    [Fact]
    public void DailyBoardRotatesTwoPatrolsOneBountyAndThreePreparations()
    {
        Assert.Equal(2, StarfallWatchSystem.DailyPatrolOfferCount);
        Assert.Equal(2, StarfallWatchSystem.DailyPatrolCompletionLimit);
        Assert.Equal(3, StarfallWatchSystem.Preparations.Count);
        Assert.Equal(
            [
                StarfallWatchSystem.SealWardPreparationId,
                StarfallWatchSystem.RouteThreadsPreparationId,
                StarfallWatchSystem.FieldRationPreparationId
            ],
            StarfallWatchSystem.Preparations.Select(preparation => preparation.Id)
        );

        Assert.Equal(
            [
                StarfallWatchSystem.VillageSouthSealPatrolId,
                StarfallWatchSystem.MeadowLanternRoadPatrolId
            ],
            StarfallWatchSystem.PatrolsForDay(1).Select(patrol => patrol.Id)
        );
        Assert.Equal(
            [
                StarfallWatchSystem.CrystalValeMarkerPatrolId,
                StarfallWatchSystem.RuinsThresholdPatrolId
            ],
            StarfallWatchSystem.PatrolsForDay(2).Select(patrol => patrol.Id)
        );
        Assert.Equal(
            [
                StarfallWatchSystem.WetlandReedCrossingPatrolId,
                StarfallWatchSystem.WoodsOldWatchPatrolId
            ],
            StarfallWatchSystem.PatrolsForDay(3).Select(patrol => patrol.Id)
        );
        Assert.Equal(
            StarfallWatchSystem.PatrolsForDay(1).Select(patrol => patrol.Id),
            StarfallWatchSystem.PatrolsForDay(4).Select(patrol => patrol.Id)
        );
        Assert.Equal(
            StarfallWatchSystem.ShardlingBountyId,
            StarfallWatchSystem.BountyForDay(1).Id
        );
        Assert.Equal(
            StarfallWatchSystem.PrismWispBountyId,
            StarfallWatchSystem.BountyForDay(2).Id
        );
        Assert.Equal(
            StarfallWatchSystem.ShardlingBountyId,
            StarfallWatchSystem.BountyForDay(7).Id
        );
        Assert.All(StarfallWatchSystem.Patrols, patrol =>
        {
            Assert.Contains(patrol.TargetBiome, Enum.GetValues<WorldBiome>());
            Assert.True(patrol.RewardCoins > 0);
            Assert.True(DataCatalog.Items.ContainsKey(patrol.RewardItemId));
            Assert.True(patrol.RewardItemCount > 0);
            Assert.False(string.IsNullOrWhiteSpace(patrol.NameKey));
            Assert.False(string.IsNullOrWhiteSpace(patrol.DescriptionKey));
        });
        Assert.All(StarfallWatchSystem.Bounties, bounty =>
        {
            Assert.Contains(
                bounty.EnemyId,
                StarfallRuinsTrialCatalog.Enemies.Select(enemy => enemy.Id)
            );
            Assert.True(bounty.RequiredCount > 0);
            Assert.True(bounty.RewardCoins > 0);
            Assert.True(DataCatalog.Items.ContainsKey(bounty.RewardItemId));
            Assert.True(bounty.RewardItemCount > 0);
        });
    }

    [Fact]
    public void AcceptFailuresDoNotChangeWatchMailQuestConstructionOrInventory()
    {
        var route = StarfallWatchSystem.PatrolsForDay(1)[0];

        AssertActionKeepsGlobalState(
            WatchSession(locationId: PlayerLocationIds.World),
            session => session.AcceptStarfallWatchPatrol(route.Id),
            "notice.nothing_to_interact"
        );
        AssertActionKeepsGlobalState(
            WatchSession(selectedSlot: 1),
            session => session.AcceptStarfallWatchPatrol(route.Id),
            "notice.needs_hand"
        );
        var closed = WatchSession(
            minuteOfDay: VillageCatalog.StarfallWatchCloseMinute
        );
        var closedPreview = closed.PreviewSelectedTarget(
            VillageCatalog.SealRouteTableCell
        );
        Assert.Equal(TargetPreviewState.Blocked, closedPreview.State);
        Assert.Equal(
            "target.status.starfall_watch_closed",
            closedPreview.LabelKey
        );
        AssertActionKeepsGlobalState(
            closed,
            session => session.AcceptStarfallWatchPatrol(route.Id),
            "notice.starfall_watch_closed"
        );
        AssertActionKeepsGlobalState(
            WatchSession(),
            session => session.AcceptStarfallWatchPatrol("unknown_watch_patrol"),
            "starfall_watch.patrol.unavailable"
        );

        var active = WatchSession();
        Assert.True(active.AcceptStarfallWatchPatrol(route.Id).Succeeded);
        AssertActionKeepsGlobalState(
            active,
            session => session.AcceptStarfallWatchPatrol(
                StarfallWatchSystem.PatrolsForDay(1)[1].Id
            ),
            "starfall_watch.patrol.active_exists"
        );
    }

    [Fact]
    public void PatrolUsesRealWorldBiomeArrivalAndInventoryFullBlocksAllRewardState()
    {
        var session = WatchSession();
        var patrol = session.TodayStarfallWatchBoard.PatrolOffers[0];
        Assert.True(session.AcceptStarfallWatchPatrol(patrol.Id).Succeeded);

        var wrongBiome = Enum.GetValues<WorldBiome>()
            .First(biome => biome != patrol.TargetBiome);
        SetPlayerCell(
            session,
            FirstWalkableWorldCell(wrongBiome),
            PlayerLocationIds.World
        );
        Assert.False(session.TodayStarfallWatchBoard.PatrolTargetReached);

        var target = FirstWalkableWorldCell(patrol.TargetBiome);
        Assert.Equal(patrol.TargetBiome, WorldDefinition.GetBiome(target));
        SetPlayerCell(session, target, PlayerLocationIds.World);
        Assert.True(session.TodayStarfallWatchBoard.PatrolTargetReached);

        MoveToWatchTable(session);
        FillInventory(session, patrol.RewardItemId);
        var before = GlobalSnapshot(session);

        var blocked = session.ClaimStarfallWatchPatrolReward(out var reward);

        Assert.False(blocked.Succeeded);
        Assert.Equal("notice.inventory_full", blocked.MessageKey);
        Assert.Null(reward);
        Assert.Equal(before, GlobalSnapshot(session));
    }

    [Fact]
    public void BountyProgressRequiresRealEnemyDefeatsAndFullInventoryBlocksReward()
    {
        var session = WatchSessionWithRuinsAccess(day: 1);
        var bounty = session.TodayStarfallWatchBoard.BountyOffer;
        Assert.Equal(StarfallWatchSystem.ShardlingBountyId, bounty.Id);
        Assert.True(session.AcceptStarfallWatchBounty(bounty.Id).Succeeded);
        Assert.Equal(0, session.TodayStarfallWatchBoard.ActiveBountyProgress);

        var mismatchSession = WatchSessionWithRuinsAccess(day: 2);
        var mismatchBounty = mismatchSession.TodayStarfallWatchBoard.BountyOffer;
        Assert.Equal(StarfallWatchSystem.PrismWispBountyId, mismatchBounty.Id);
        Assert.True(mismatchSession.AcceptStarfallWatchBounty(
            mismatchBounty.Id
        ).Succeeded);
        KillStarfallEnemy(mismatchSession, "starfall_trial_shardling_01");
        Assert.Equal(
            0,
            mismatchSession.TodayStarfallWatchBoard.ActiveBountyProgress
        );

        KillStarfallEnemy(session, "starfall_trial_shardling_01");
        Assert.Equal(1, session.TodayStarfallWatchBoard.ActiveBountyProgress);
        Assert.False(
            session.ClaimStarfallWatchBountyReward(out _).Succeeded
        );

        KillStarfallEnemy(session, "starfall_trial_shardling_02");
        Assert.Equal(
            bounty.RequiredCount,
            session.TodayStarfallWatchBoard.ActiveBountyProgress
        );

        MoveToWatchTable(session);
        FillInventory(session, bounty.RewardItemId);
        var before = GlobalSnapshot(session);
        var blocked = session.ClaimStarfallWatchBountyReward(out var reward);

        Assert.False(blocked.Succeeded);
        Assert.Equal("notice.inventory_full", blocked.MessageKey);
        Assert.Null(reward);
        Assert.Equal(before, GlobalSnapshot(session));
    }

    [Fact]
    public void TrialDefeatFailsActiveBountyWithoutGrantingRewards()
    {
        var session = WatchSessionWithRuinsAccess();
        var bounty = session.TodayStarfallWatchBoard.BountyOffer;
        Assert.True(session.AcceptStarfallWatchBounty(bounty.Id).Succeeded);
        var beforeCoins = session.Coins;
        var beforeRewardCount = session.Inventory.Count(bounty.RewardItemId);
        var beforeRelationship = session.Village
            .Relationship(VillageCatalog.KaelId)
            .Points;

        MoveToRuins(session, health: 1);
        while (!session.Combat.IsDefeated)
        {
            var hit = session.ReceiveStarfallEnemyHit(
                "starfall_trial_hollow_sentinel_01"
            );
            Assert.True(hit.Succeeded);
            session.AdvanceStarfallCombat(
                CombatSystem.HurtInvulnerabilitySeconds
            );
        }

        var resolved = session.ResolveStarfallTrialDefeat();

        Assert.True(resolved.Succeeded);
        Assert.Equal(beforeCoins, session.Coins);
        Assert.Equal(
            beforeRewardCount,
            session.Inventory.Count(bounty.RewardItemId)
        );
        Assert.Equal(
            beforeRelationship,
            session.Village.Relationship(VillageCatalog.KaelId).Points
        );
        Assert.False(session.TodayStarfallWatchBoard.HasActiveBounty);
        Assert.False(session.TodayStarfallWatchBoard.BountyCompletedToday);
    }

    [Fact]
    public void SameDaySaveRestoresWatchStateAndNextDayResetsDailyContracts()
    {
        var session = WatchSession(day: 2);
        var patrol = session.TodayStarfallWatchBoard.PatrolOffers[0];
        var bounty = session.TodayStarfallWatchBoard.BountyOffer;
        Assert.True(session.AcceptStarfallWatchPatrol(patrol.Id).Succeeded);
        SetPlayerCell(
            session,
            FirstWalkableWorldCell(patrol.TargetBiome),
            PlayerLocationIds.World
        );
        MoveToWatchTable(session);
        Assert.True(session.AcceptStarfallWatchBounty(bounty.Id).Succeeded);
        Assert.True(session.StarfallWatch
            .RecordEnemyDefeated(bounty.EnemyId, session.Clock.Day)
            .Succeeded);

        var restored = new GameSession();
        restored.Restore(session.Capture());

        Assert.Equal(2, restored.Clock.Day);
        Assert.Equal(patrol.Id, restored.TodayStarfallWatchBoard.ActivePatrolId);
        Assert.True(restored.TodayStarfallWatchBoard.PatrolTargetReached);
        Assert.Equal(bounty.Id, restored.TodayStarfallWatchBoard.ActiveBountyId);
        Assert.Equal(1, restored.TodayStarfallWatchBoard.ActiveBountyProgress);

        restored.EndDay();

        Assert.Equal(3, restored.Clock.Day);
        Assert.False(restored.TodayStarfallWatchBoard.HasActivePatrol);
        Assert.False(restored.TodayStarfallWatchBoard.PatrolTargetReached);
        Assert.Empty(restored.TodayStarfallWatchBoard.CompletedPatrolIds);
        Assert.False(restored.TodayStarfallWatchBoard.HasActiveBounty);
        Assert.Equal(0, restored.TodayStarfallWatchBoard.ActiveBountyProgress);
        Assert.Empty(restored.TodayStarfallWatchBoard.CompletedBountyIds);
        Assert.Equal(
            StarfallWatchSystem.PatrolsForDay(3).Select(candidate => candidate.Id),
            restored.TodayStarfallWatchBoard.PatrolOffers
                .Select(candidate => candidate.Id)
        );
        Assert.Equal(
            StarfallWatchSystem.BountyForDay(3).Id,
            restored.TodayStarfallWatchBoard.BountyOffer.Id
        );
    }

    [Fact]
    public void InvalidWatchSaveNormalizesToReachableDailyState()
    {
        var offers = StarfallWatchSystem.PatrolsForDay(1);
        var dailyBounty = StarfallWatchSystem.BountyForDay(1);
        var normalized = StarfallWatchSystem.NormalizeSave(
            new StarfallWatchSave
            {
                Day = 1,
                ActivePatrolId = offers[0].Id,
                PatrolTargetReached = true,
                CompletedPatrolIds =
                [
                    offers[0].Id,
                    offers[1].Id,
                    offers[0].Id,
                    "unknown_patrol"
                ],
                ActiveBountyId = dailyBounty.Id,
                ActiveBountyProgress = dailyBounty.RequiredCount + 10,
                FailedBountyId = dailyBounty.Id,
                CompletedBountyIds =
                [
                    dailyBounty.Id,
                    dailyBounty.Id,
                    "unknown_bounty"
                ],
                PreparationId = StarfallWatchSystem.SealWardPreparationId,
                PreparationConsumed = true
            },
            1
        );

        Assert.Empty(normalized.ActivePatrolId);
        Assert.False(normalized.PatrolTargetReached);
        Assert.Equal(
            offers.Select(patrol => patrol.Id).Order(StringComparer.Ordinal),
            normalized.CompletedPatrolIds.Order(StringComparer.Ordinal)
        );
        Assert.Empty(normalized.ActiveBountyId);
        Assert.Equal(0, normalized.ActiveBountyProgress);
        Assert.Empty(normalized.FailedBountyId);
        Assert.Equal([dailyBounty.Id], normalized.CompletedBountyIds);
        Assert.Equal(
            StarfallWatchSystem.SealWardPreparationId,
            normalized.PreparationId
        );
        Assert.False(normalized.PreparationConsumed);

        var stale = StarfallWatchSystem.NormalizeSave(normalized, 2);
        Assert.Equal(2, stale.Day);
        Assert.Empty(stale.ActivePatrolId);
        Assert.Empty(stale.CompletedPatrolIds);
        Assert.Empty(stale.ActiveBountyId);
        Assert.Empty(stale.CompletedBountyIds);
        Assert.Empty(stale.PreparationId);
    }

    [Fact]
    public void PreparationChoiceIsExclusiveAndFieldRationIsConsumedOnce()
    {
        var ward = WatchSession();
        Assert.True(ward.SelectStarfallWatchPreparation(
            StarfallWatchSystem.SealWardPreparationId
        ).Succeeded);
        Assert.True(ward.EffectiveIncomingDamageMultiplier < 1f);
        Assert.Equal(1f, ward.EffectiveEnemySpeedMultiplier, 3);
        AssertActionKeepsGlobalState(
            ward,
            session => session.SelectStarfallWatchPreparation(
                StarfallWatchSystem.RouteThreadsPreparationId
            ),
            "starfall_watch.prep.already_selected"
        );

        var routeThreads = WatchSession();
        Assert.True(routeThreads.SelectStarfallWatchPreparation(
            StarfallWatchSystem.RouteThreadsPreparationId
        ).Succeeded);
        Assert.True(routeThreads.EffectiveEnemySpeedMultiplier < 1f);
        Assert.Equal(1f, routeThreads.EffectiveIncomingDamageMultiplier, 3);

        var ration = WatchSessionWithRuinsAccess(energy: 20);
        Assert.True(ration.SelectStarfallWatchPreparation(
            StarfallWatchSystem.FieldRationPreparationId
        ).Succeeded);
        Assert.True(ration.StarfallWatch.HasFieldRationAvailable);

        EnterRuinsThroughWorldGate(ration);

        Assert.Equal(35, ration.Energy);
        Assert.False(ration.StarfallWatch.HasFieldRationAvailable);
        var consumed = ration.StarfallWatch.Capture();
        Assert.True(consumed.PreparationConsumed);

        SetPlayerCell(
            ration,
            StarfallRuinsTrialLayout.WorldReturnCell,
            PlayerLocationIds.World
        );
        EnterRuinsThroughWorldGate(ration);
        Assert.Equal(35, ration.Energy);
    }

    [Fact]
    public void WatchRewardsDoNotTouchMailQuestOrConstructionState()
    {
        var session = WatchSessionWithRuinsAccess();
        var beforeIsolation = IsolationSnapshot(session);
        var patrol = session.TodayStarfallWatchBoard.PatrolOffers[0];
        var patrolCoins = session.Coins;
        var patrolItems = session.Inventory.Count(patrol.RewardItemId);
        var patrolRelationship = session.Village
            .Relationship(VillageCatalog.KaelId)
            .Points;
        Assert.True(session.AcceptStarfallWatchPatrol(patrol.Id).Succeeded);
        SetPlayerCell(
            session,
            FirstWalkableWorldCell(patrol.TargetBiome),
            PlayerLocationIds.World
        );
        MoveToWatchTable(session);
        Assert.True(session.ClaimStarfallWatchPatrolReward(
            out var patrolReward
        ).Succeeded);
        Assert.NotNull(patrolReward);
        Assert.Equal(patrol.RewardCoins, patrolReward.RewardCoins);
        Assert.Equal(patrol.RewardItemId, patrolReward.RewardItemId);
        Assert.Equal(patrol.RewardItemCount, patrolReward.RewardItemCount);
        Assert.Equal(patrolCoins + patrol.RewardCoins, session.Coins);
        Assert.Equal(
            patrolItems + patrol.RewardItemCount,
            session.Inventory.Count(patrol.RewardItemId)
        );
        Assert.Equal(
            patrolRelationship +
                StarfallWatchSystem.PatrolRelationshipRewardPoints,
            session.Village.Relationship(VillageCatalog.KaelId).Points
        );

        var bounty = session.TodayStarfallWatchBoard.BountyOffer;
        var bountyCoins = session.Coins;
        var bountyItems = session.Inventory.Count(bounty.RewardItemId);
        var bountyRelationship = session.Village
            .Relationship(VillageCatalog.KaelId)
            .Points;
        Assert.True(session.AcceptStarfallWatchBounty(bounty.Id).Succeeded);
        KillStarfallEnemy(session, "starfall_trial_shardling_01");
        KillStarfallEnemy(session, "starfall_trial_shardling_02");
        MoveToWatchTable(session);
        Assert.True(session.ClaimStarfallWatchBountyReward(
            out var bountyReward
        ).Succeeded);
        Assert.NotNull(bountyReward);
        Assert.Equal(bounty.RewardCoins, bountyReward.RewardCoins);
        Assert.Equal(bounty.RewardItemId, bountyReward.RewardItemId);
        Assert.Equal(bounty.RewardItemCount, bountyReward.RewardItemCount);
        Assert.Equal(bountyCoins + bounty.RewardCoins, session.Coins);
        Assert.Equal(
            bountyItems + bounty.RewardItemCount,
            session.Inventory.Count(bounty.RewardItemId)
        );
        Assert.Equal(
            bountyRelationship +
                StarfallWatchSystem.BountyRelationshipRewardPoints,
            session.Village.Relationship(VillageCatalog.KaelId).Points
        );

        Assert.Equal(beforeIsolation, IsolationSnapshot(session));
    }

    [Fact]
    public void OverlayLocalizationKeysCoverBoardPatrolsBountiesAndActions()
    {
        var requiredKeys = StarfallWatchOverlay.RequiredLocalizationKeys;
        Assert.Contains("watch.board.opened", requiredKeys);
        Assert.Contains("starfall_watch.patrol.completed", requiredKeys);
        Assert.Contains("starfall_watch.bounty.completed", requiredKeys);
        Assert.Contains("starfall_watch.prep.selected", requiredKeys);
        Assert.All(
            StarfallWatchSystem.Patrols,
            patrol =>
            {
                Assert.Contains(patrol.NameKey, requiredKeys);
                Assert.Contains(patrol.DescriptionKey, requiredKeys);
            }
        );
        Assert.All(
            StarfallWatchSystem.Bounties,
            bounty =>
            {
                Assert.Contains(bounty.NameKey, requiredKeys);
                Assert.Contains(bounty.DescriptionKey, requiredKeys);
            }
        );
        Assert.All(
            StarfallWatchSystem.Preparations,
            preparation =>
            {
                Assert.Contains(preparation.NameKey, requiredKeys);
                Assert.Contains(preparation.DescriptionKey, requiredKeys);
            }
        );

        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                requiredKeys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    private static GameSession WatchSession(
        int day = 1,
        int minuteOfDay = 9 * 60,
        int selectedSlot = 0,
        int? energy = null,
        string locationId = PlayerLocationIds.StarfallWatch
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.SelectedSlot = selectedSlot;
        save.Player.LocationId = locationId;
        save.Player.X = VillageCatalog.SealRouteTableCell.X * 16 + 8;
        save.Player.Y = (VillageCatalog.SealRouteTableCell.Y + 1) * 16 + 8;
        if (energy is not null)
        {
            save.Player.Energy = energy.Value;
        }

        session.Restore(save);
        return session;
    }

    private static GameSession WatchSessionWithRuinsAccess(
        int day = 1,
        int energy = 100
    )
    {
        var session = WatchSession(day, energy: energy);
        var save = session.Capture();
        UnlockCrystalRuinsPassage(save);
        save.Combat.CurrentHealth = CombatSystem.MaxHealth;
        var inventory = new Inventory();
        inventory.Restore(save.Inventory, save.Player.SelectedSlot);
        Assert.True(inventory.Add(DataCatalog.MoonsteelShortbladeId, 1));
        save.Inventory = inventory.Capture();
        session.Restore(save);
        return session;
    }

    private static void MoveToWatchTable(GameSession session)
    {
        session.Inventory.Select(0);
        session.Clock.Reset(session.Clock.Day, 9 * 60);
        SetPlayerCell(
            session,
            new GridPosition(
                VillageCatalog.SealRouteTableCell.X,
                VillageCatalog.SealRouteTableCell.Y + 1
            ),
            PlayerLocationIds.StarfallWatch
        );
    }

    private static void MoveToRuins(GameSession session, int health)
    {
        var save = session.Capture();
        UnlockCrystalRuinsPassage(save);
        save.Combat.CurrentHealth = health;
        save.Player.LocationId = PlayerLocationIds.StarfallRuinsTrial;
        save.Player.X = StarfallRuinsTrialLayout.SafeArrivalCell.X * 16 + 8;
        save.Player.Y = StarfallRuinsTrialLayout.SafeArrivalCell.Y * 16 + 8;
        session.Restore(save);
    }

    private static void EnterRuinsThroughWorldGate(GameSession session)
    {
        SetPlayerCell(
            session,
            new GridPosition(
                StarfallRuinsTrialLayout.WorldEntryCell.X - 1,
                StarfallRuinsTrialLayout.WorldEntryCell.Y
            ),
            PlayerLocationIds.World
        );
        session.Inventory.Select(0);
        var entry = session.TryEnterStarfallRuinsTrial(
            StarfallRuinsTrialLayout.WorldEntryCell
        );
        Assert.True(entry.Succeeded, entry.MessageKey);
        SetPlayerCell(
            session,
            StarfallRuinsTrialLayout.SafeArrivalCell,
            PlayerLocationIds.StarfallRuinsTrial
        );
    }

    private static void KillStarfallEnemy(
        GameSession session,
        string enemyInstanceId
    )
    {
        MoveToRuins(session, CombatSystem.MaxHealth);
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.MoonsteelShortbladeId
        ));
        while (!session.StarfallRuinsTrial.Enemy(enemyInstanceId).Defeated)
        {
            var enemy = session.StarfallRuinsTrial.Enemy(enemyInstanceId);
            PositionPlayerInAttackRange(session, enemyInstanceId);
            var result = session.AttackStarfallEnemy(
                enemyInstanceId,
                enemy.Cell
            );
            Assert.True(result.Succeeded, result.MessageKey);
            session.AdvanceStarfallCombat(
                StarfallRuinsTrialCatalog.MoonsteelShortblade.CooldownSeconds
            );
        }
    }

    private static void PositionPlayerInAttackRange(
        GameSession session,
        string enemyInstanceId
    )
    {
        var enemy = session.StarfallRuinsTrial.Enemy(enemyInstanceId);
        var attempts = new List<string>();
        var offsets = new (float X, float Y)[]
        {
            (-16f, 0f),
            (16f, 0f),
            (0f, -16f),
            (0f, 16f)
        };

        foreach (var offset in offsets)
        {
            session.SetPlayerLocation(
                enemy.CurrentX + offset.X,
                enemy.CurrentY + offset.Y,
                PlayerLocationIds.StarfallRuinsTrial
            );
            if (session.CheckAttackStarfallEnemy(
                    enemyInstanceId,
                    enemy.Cell
                ) is { Succeeded: true })
            {
                return;
            }

            var check = session.CheckAttackStarfallEnemy(
                enemyInstanceId,
                enemy.Cell
            );
            attempts.Add(
                $"{offset.X},{offset.Y}->{check.MessageKey}@{session.PlayerX},{session.PlayerY}"
            );
        }

        Assert.Fail(
            "No adjacent attack position was in weapon range: " +
            string.Join("; ", attempts)
        );
    }

    private static void UnlockCrystalRuinsPassage(GameSaveV1 save)
    {
        save.Mining.DeepestRoomReached = 5;
        save.ToolProgression = new ToolProgressionSave
        {
            Tools =
            [
                new ToolProgressionEntrySave
                {
                    ToolId = DataCatalog.ShovelId,
                    TierId = ToolProgressionCatalog.BronzeStarTierId
                }
            ]
        };
        save.Starlight = new StarlightSave
        {
            Pedestals =
            [
                CompletedInventoryPedestal(DataCatalog.CrystalValeStarlight)
            ]
        };
    }

    private static StarlightPedestalSave CompletedInventoryPedestal(
        StarlightPedestalDefinition definition
    ) => new()
    {
        PedestalId = definition.Id,
        Nodes = definition.Nodes.Select(node => new StarlightNodeSave
        {
            NodeId = node.Id,
            Contributions = CompleteContributions(node)
        }).ToList()
    };

    private static List<StarlightContributionSave> CompleteContributions(
        StarlightNodeDefinition node
    )
    {
        if (node.SourceKind != StarlightNodeSourceKind.Inventory)
        {
            return [];
        }

        var remaining = node.RequiredCount;
        var contributions = new List<StarlightContributionSave>();
        foreach (var option in node.Options)
        {
            var count = Math.Min(remaining, option.MaximumCount);
            if (count <= 0)
            {
                continue;
            }

            contributions.Add(new StarlightContributionSave
            {
                ItemId = option.ItemId,
                Count = count
            });
            remaining -= count;
            if (remaining == 0)
            {
                break;
            }
        }

        Assert.Equal(0, remaining);
        return contributions;
    }

    private static GridPosition FirstWalkableWorldCell(WorldBiome biome)
    {
        for (var y = 1; y < WorldDefinition.Height - 1; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var cell = new GridPosition(x, y);
                if (WorldDefinition.GetBiome(cell) == biome &&
                    !WorldDefinition.IsBlocked(cell))
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException(
            $"Could not find a walkable world cell for {biome}."
        );
    }

    private static void SetPlayerCell(
        GameSession session,
        GridPosition cell,
        string locationId
    ) => session.SetPlayerLocation(
        cell.X * 16 + 8,
        cell.Y * 16 + 8,
        locationId
    );

    private static void FillInventory(GameSession session, string excludedItemId)
    {
        var save = session.Capture();
        var fillers = DataCatalog.Items.Values
            .Where(item => item.Kind != ItemKind.Tool)
            .Where(item => item.Id != excludedItemId)
            .DistinctBy(item => item.Id, StringComparer.Ordinal)
            .Take(Inventory.SlotCount - Inventory.StartingToolCount)
            .Select(item => new InventorySlot
            {
                ItemId = item.Id,
                Count = item.MaxStack
            })
            .ToList();
        Assert.Equal(
            Inventory.SlotCount - Inventory.StartingToolCount,
            fillers.Count
        );
        save.Inventory.AddRange(fillers);
        save.Player.SelectedSlot = 0;
        session.Restore(save);
        Assert.False(session.Inventory.CanAdd(excludedItemId, 1));
    }

    private static void AssertActionKeepsGlobalState(
        GameSession session,
        Func<GameSession, ActionResult> action,
        string expectedMessageKey
    )
    {
        var before = GlobalSnapshot(session);
        var result = action(session);
        Assert.False(result.Succeeded);
        Assert.Equal(expectedMessageKey, result.MessageKey);
        Assert.Equal(before, GlobalSnapshot(session));
    }

    private static GlobalWatchSnapshot GlobalSnapshot(GameSession session)
    {
        var save = session.Capture();
        return new GlobalWatchSnapshot(
            save.Coins,
            save.Player.Energy,
            Serialize(save.Inventory),
            Serialize(save.StarfallWatch),
            Serialize(save.Mail),
            Serialize(save.Quest),
            Serialize(save.Construction),
            session.Village.Relationship(VillageCatalog.KaelId).Points
        );
    }

    private static IsolationStateSnapshot IsolationSnapshot(GameSession session)
    {
        var save = session.Capture();
        return new IsolationStateSnapshot(
            Serialize(save.Mail),
            Serialize(save.Quest),
            Serialize(save.Construction)
        );
    }

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value);

    private static string ReadLocale(string name) => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "localization", name)
    );

    private sealed record GlobalWatchSnapshot(
        int Coins,
        int Energy,
        string InventoryJson,
        string WatchJson,
        string MailJson,
        string QuestJson,
        string ConstructionJson,
        int KaelRelationship
    );

    private sealed record IsolationStateSnapshot(
        string MailJson,
        string QuestJson,
        string ConstructionJson
    );
}
