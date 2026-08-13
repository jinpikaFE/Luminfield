namespace Luminfield.Core;

public sealed record WeeklyCommissionClaimResult(
    bool Succeeded,
    string MessageKey,
    int RewardCoins = 0
);

public sealed class WeeklyCommissionSystem
{
    private WeeklyCommissionSave _state = CreateForWeek(1);

    public int Week => _state.Week;
    public WeeklyCommissionDefinition Current => DataCatalog.WeeklyCommission;
    public bool Accepted => _state.Accepted;
    public int Progress => _state.Progress;
    public bool Claimed => _state.Claimed;
    public WeeklyCommissionStageDefinition CurrentStage =>
        Current.Stages[CurrentStageIndex];
    public int CurrentStageIndex => StageIndex(Current, _state.StageId);
    public bool IsFinalStage => CurrentStageIndex == Current.Stages.Count - 1;

    public event Action? Changed;

    public void Reset(int day)
    {
        _state = CreateForWeek(WeekForDay(day));
        Changed?.Invoke();
    }

    public void Restore(WeeklyCommissionSave? save, int currentDay)
    {
        _state = NormalizeSave(save, currentDay);
        Changed?.Invoke();
    }

    public void RefreshForDay(int day)
    {
        var week = WeekForDay(day);
        if (_state.Week == week)
        {
            return;
        }

        _state = CreateForWeek(week);
        Changed?.Invoke();
    }

    public ActionResult Accept()
    {
        if (_state.Claimed)
        {
            return ActionResult.Fail("weekly_commission.already_claimed");
        }

        if (_state.Accepted)
        {
            return ActionResult.Fail("weekly_commission.already_accepted");
        }

        _state.Accepted = true;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "weekly_commission.accepted"
        );
    }

    public int DisplayProgress(Inventory inventory)
    {
        if (CurrentStage.Kind == WeeklyCommissionStageKind.Deliver)
        {
            return Math.Min(
                CurrentStage.RequiredCount,
                inventory.Count(CurrentStage.TargetId)
            );
        }

        return Math.Min(CurrentStage.RequiredCount, _state.Progress);
    }

    public bool IsReady(Inventory inventory) =>
        _state.Accepted &&
        !_state.Claimed &&
        DisplayProgress(inventory) >= CurrentStage.RequiredCount;

    public ActionResult AdvanceStage(Inventory inventory)
    {
        if (_state.Claimed)
        {
            return ActionResult.Fail("weekly_commission.already_claimed");
        }

        if (!_state.Accepted)
        {
            return ActionResult.Fail("weekly_commission.accept_first");
        }

        if (!IsReady(inventory))
        {
            return ActionResult.Fail("weekly_commission.not_ready");
        }

        if (IsFinalStage)
        {
            return ActionResult.Fail(
                "weekly_commission.claim_final_reward"
            );
        }

        _state.StageId = Current.Stages[CurrentStageIndex + 1].Id;
        _state.Progress = 0;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "weekly_commission.stage_advanced"
        );
    }

    public WeeklyCommissionClaimResult Claim(Inventory inventory)
    {
        if (_state.Claimed)
        {
            return new WeeklyCommissionClaimResult(
                false,
                "weekly_commission.already_claimed"
            );
        }

        if (!_state.Accepted)
        {
            return new WeeklyCommissionClaimResult(
                false,
                "weekly_commission.accept_first"
            );
        }

        if (!IsFinalStage || !IsReady(inventory))
        {
            return new WeeklyCommissionClaimResult(
                false,
                "weekly_commission.not_ready"
            );
        }

        if (!inventory.TryExchange(
                [
                    new CraftingIngredient(
                        CurrentStage.TargetId,
                        CurrentStage.RequiredCount
                    )
                ],
                Current.RewardItemId,
                Current.RewardItemCount
            ))
        {
            return new WeeklyCommissionClaimResult(
                false,
                "weekly_commission.backpack_full"
            );
        }

        _state.Claimed = true;
        Changed?.Invoke();
        return new WeeklyCommissionClaimResult(
            true,
            "weekly_commission.claimed",
            Current.RewardCoins
        );
    }

    public void RecordPlant(string cropId)
    {
        RecordProgress(WeeklyCommissionStageKind.Plant, cropId, 1);
    }

    public void RecordGather(string itemId, int count)
    {
        RecordProgress(WeeklyCommissionStageKind.Gather, itemId, count);
    }

    public WeeklyCommissionSave Capture() => new()
    {
        Week = _state.Week,
        DefinitionId = _state.DefinitionId,
        Accepted = _state.Accepted,
        StageId = _state.StageId,
        Progress = _state.Progress,
        Claimed = _state.Claimed
    };

    public static int WeekForDay(int day) =>
        CalendarSystem.WeekNumber(day);

    public static WeeklyCommissionSave NormalizeSave(
        WeeklyCommissionSave? save,
        int currentDay
    )
    {
        var week = WeekForDay(currentDay);
        var definition = DataCatalog.WeeklyCommission;
        if (save is null ||
            save.Week != week ||
            !string.Equals(
                save.DefinitionId,
                definition.Id,
                StringComparison.Ordinal
            ))
        {
            return CreateForWeek(week);
        }

        var stageIndex = StageIndex(definition, save.StageId);
        if (stageIndex < 0)
        {
            return CreateForWeek(week);
        }

        var accepted = save.Accepted || save.Claimed;
        if (!accepted)
        {
            return CreateForWeek(week);
        }

        if (save.Claimed)
        {
            return new WeeklyCommissionSave
            {
                Week = week,
                DefinitionId = definition.Id,
                Accepted = true,
                StageId = definition.Stages[^1].Id,
                Claimed = true
            };
        }

        var stage = definition.Stages[stageIndex];
        var progress = 0;
        if (stage.Kind != WeeklyCommissionStageKind.Deliver)
        {
            progress = Math.Clamp(
                save.Progress,
                0,
                stage.RequiredCount
            );
        }

        return new WeeklyCommissionSave
        {
            Week = week,
            DefinitionId = definition.Id,
            Accepted = true,
            StageId = stage.Id,
            Progress = progress
        };
    }

    private static WeeklyCommissionSave CreateForWeek(int week)
    {
        var definition = DataCatalog.WeeklyCommission;
        return new WeeklyCommissionSave
        {
            Week = Math.Max(1, week),
            DefinitionId = definition.Id,
            StageId = definition.Stages[0].Id
        };
    }

    private static int StageIndex(
        WeeklyCommissionDefinition definition,
        string stageId
    )
    {
        for (var index = 0; index < definition.Stages.Count; index++)
        {
            if (string.Equals(
                    definition.Stages[index].Id,
                    stageId,
                    StringComparison.Ordinal
                ))
            {
                return index;
            }
        }

        return -1;
    }

    private void RecordProgress(
        WeeklyCommissionStageKind kind,
        string targetId,
        int count
    )
    {
        if (count <= 0 ||
            !_state.Accepted ||
            _state.Claimed ||
            CurrentStage.Kind != kind ||
            !string.Equals(
                CurrentStage.TargetId,
                targetId,
                StringComparison.Ordinal
            ))
        {
            return;
        }

        var next = Math.Min(
            CurrentStage.RequiredCount,
            _state.Progress + count
        );
        if (next == _state.Progress)
        {
            return;
        }

        _state.Progress = next;
        Changed?.Invoke();
    }
}
