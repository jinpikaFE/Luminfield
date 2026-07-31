namespace Luminfield.Core;

public sealed record DailyCommissionClaimResult(
    bool Succeeded,
    string MessageKey,
    int RewardCoins = 0
);

public sealed class DailyCommissionSystem
{
    private DailyCommissionSave _state = CreateForDay(1);

    public int Day => _state.Day;
    public DailyCommissionDefinition Current =>
        DataCatalog.DailyCommission(_state.DefinitionId);
    public bool Accepted => _state.Accepted;
    public int Progress => _state.Progress;
    public bool Claimed => _state.Claimed;

    public event Action? Changed;

    public void Reset(int day)
    {
        _state = CreateForDay(day);
        Changed?.Invoke();
    }

    public void Restore(DailyCommissionSave? save, int currentDay)
    {
        _state = NormalizeSave(save, currentDay);
        Changed?.Invoke();
    }

    public void RefreshForDay(int day)
    {
        var normalizedDay = Math.Max(1, day);
        if (_state.Day == normalizedDay)
        {
            return;
        }

        _state = CreateForDay(normalizedDay);
        Changed?.Invoke();
    }

    public ActionResult Accept()
    {
        if (_state.Claimed)
        {
            return ActionResult.Fail("commission.already_claimed");
        }

        if (_state.Accepted)
        {
            return ActionResult.Fail("commission.already_accepted");
        }

        _state.Accepted = true;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "commission.accepted");
    }

    public int DisplayProgress(Inventory inventory)
    {
        if (Current.Kind == DailyCommissionKind.Deliver)
        {
            return Math.Min(
                Current.RequiredCount,
                inventory.CountFamily(Current.TargetId)
            );
        }

        return Math.Min(Current.RequiredCount, _state.Progress);
    }

    public bool IsReady(Inventory inventory) =>
        _state.Accepted &&
        !_state.Claimed &&
        DisplayProgress(inventory) >= Current.RequiredCount;

    public DailyCommissionClaimResult Claim(Inventory inventory)
    {
        if (_state.Claimed)
        {
            return new DailyCommissionClaimResult(
                false,
                "commission.already_claimed"
            );
        }

        if (!_state.Accepted)
        {
            return new DailyCommissionClaimResult(
                false,
                "commission.accept_first"
            );
        }

        if (!IsReady(inventory))
        {
            return new DailyCommissionClaimResult(
                false,
                "commission.not_ready"
            );
        }

        if (Current.Kind == DailyCommissionKind.Deliver &&
            !inventory.RemoveFamily(
                Current.TargetId,
                Current.RequiredCount
            ))
        {
            return new DailyCommissionClaimResult(
                false,
                "commission.not_ready"
            );
        }

        _state.Claimed = true;
        Changed?.Invoke();
        return new DailyCommissionClaimResult(
            true,
            "commission.claimed",
            Current.RewardCoins
        );
    }

    public void RecordPlant(string cropId)
    {
        RecordProgress(DailyCommissionKind.Plant, cropId, 1);
    }

    public void RecordGather(string itemId, int count)
    {
        RecordProgress(DailyCommissionKind.Gather, itemId, count);
    }

    public DailyCommissionSave Capture() => new()
    {
        Day = _state.Day,
        DefinitionId = _state.DefinitionId,
        Accepted = _state.Accepted,
        Progress = _state.Progress,
        Claimed = _state.Claimed
    };

    public static DailyCommissionSave NormalizeSave(
        DailyCommissionSave? save,
        int currentDay
    )
    {
        var normalizedDay = Math.Max(1, currentDay);
        if (save is null ||
            save.Day != normalizedDay ||
            !DataCatalog.DailyCommissions.TryGetValue(
                save.DefinitionId,
                out var definition
            ))
        {
            return CreateForDay(normalizedDay);
        }

        var accepted = save.Accepted || save.Claimed;
        var progress = 0;
        if (accepted && definition.Kind != DailyCommissionKind.Deliver)
        {
            progress = Math.Clamp(
                save.Progress,
                0,
                definition.RequiredCount
            );
        }

        return new DailyCommissionSave
        {
            Day = normalizedDay,
            DefinitionId = definition.Id,
            Accepted = accepted,
            Progress = progress,
            Claimed = save.Claimed
        };
    }

    private static DailyCommissionSave CreateForDay(int day)
    {
        var normalizedDay = Math.Max(1, day);
        var index = (normalizedDay - 1) %
            DataCatalog.DailyCommissionRotation.Count;
        return new DailyCommissionSave
        {
            Day = normalizedDay,
            DefinitionId = DataCatalog.DailyCommissionRotation[index].Id
        };
    }

    private void RecordProgress(
        DailyCommissionKind kind,
        string targetId,
        int count
    )
    {
        if (count <= 0 ||
            !_state.Accepted ||
            _state.Claimed ||
            Current.Kind != kind ||
            !string.Equals(Current.TargetId, targetId, StringComparison.Ordinal))
        {
            return;
        }

        var next = Math.Min(
            Current.RequiredCount,
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
