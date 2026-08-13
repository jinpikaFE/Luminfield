namespace Luminfield.Core;

public enum ConstructionPhase
{
    NotStarted,
    InProgress,
    Completed
}

public sealed record ConstructionProjectDefinition(
    string Id,
    string NameKey,
    string DescriptionKey,
    int CoinCost,
    IReadOnlyList<CraftingIngredient> Materials,
    int RequiredNights
);

public static class ConstructionCatalog
{
    public const string CottageFirstUpgradeId = "cottage_first_upgrade";

    public static ConstructionProjectDefinition CottageFirstUpgrade { get; } =
        new(
            CottageFirstUpgradeId,
            "construction.cottage_first_upgrade.name",
            "construction.cottage_first_upgrade.description",
            240,
            Array.AsReadOnly(
            [
                new CraftingIngredient(DataCatalog.LumenwoodId, 12),
                new CraftingIngredient(DataCatalog.CrystalShardId, 4)
            ]),
            2
        );
}

public sealed class ConstructionSystem
{
    private ConstructionSave _state = new();

    public ConstructionProjectDefinition Project =>
        ConstructionCatalog.CottageFirstUpgrade;
    public string ProjectId => _state.ProjectId;
    public int RemainingNights => _state.RemainingNights;
    public bool IsInProgress =>
        _state.ProjectId == Project.Id &&
        !_state.Completed &&
        _state.RemainingNights > 0;
    public bool IsCompleted =>
        _state.ProjectId == Project.Id && _state.Completed;
    public ConstructionPhase Phase
    {
        get
        {
            if (IsCompleted)
            {
                return ConstructionPhase.Completed;
            }

            if (IsInProgress)
            {
                return ConstructionPhase.InProgress;
            }

            return ConstructionPhase.NotStarted;
        }
    }

    public event Action? Changed;

    public void Reset()
    {
        _state = new ConstructionSave();
        Changed?.Invoke();
    }

    public void Restore(ConstructionSave? save)
    {
        _state = NormalizeSave(save);
        Changed?.Invoke();
    }

    public ActionResult CheckStart(Inventory inventory, int coins)
    {
        if (IsCompleted)
        {
            return ActionResult.Fail("construction.already_completed");
        }

        if (IsInProgress)
        {
            return ActionResult.Fail("construction.already_in_progress");
        }

        if (coins < Project.CoinCost)
        {
            return ActionResult.Fail("construction.insufficient_coins");
        }

        var missing = Project.Materials.FirstOrDefault(material =>
            inventory.Count(material.ItemId) < material.Count
        );
        if (missing?.ItemId == DataCatalog.LumenwoodId)
        {
            return ActionResult.Fail("construction.insufficient_lumenwood");
        }

        if (missing?.ItemId == DataCatalog.CrystalShardId)
        {
            return ActionResult.Fail("construction.insufficient_crystal");
        }

        if (missing is not null)
        {
            return ActionResult.Fail("construction.insufficient_materials");
        }

        return ActionResult.Success(
            messageKey: "construction.ready_to_start"
        );
    }

    public void BeginChecked()
    {
        _state = new ConstructionSave
        {
            ProjectId = Project.Id,
            RemainingNights = Project.RequiredNights,
            Completed = false
        };
        Changed?.Invoke();
    }

    public bool ResolveNight()
    {
        if (!IsInProgress)
        {
            return false;
        }

        _state.RemainingNights--;
        if (_state.RemainingNights == 0)
        {
            _state.Completed = true;
        }

        Changed?.Invoke();
        return _state.Completed;
    }

    public ConstructionSave Capture() => new()
    {
        ProjectId = _state.ProjectId,
        RemainingNights = _state.RemainingNights,
        Completed = _state.Completed
    };

    public static ConstructionSave NormalizeSave(ConstructionSave? save)
    {
        if (save is null ||
            save.ProjectId != ConstructionCatalog.CottageFirstUpgradeId)
        {
            return new ConstructionSave();
        }

        if (save.Completed)
        {
            return new ConstructionSave
            {
                ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                RemainingNights = 0,
                Completed = true
            };
        }

        if (save.RemainingNights <= 0)
        {
            return new ConstructionSave();
        }

        return new ConstructionSave
        {
            ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
            RemainingNights = Math.Clamp(
                save.RemainingNights,
                1,
                ConstructionCatalog.CottageFirstUpgrade.RequiredNights
            ),
            Completed = false
        };
    }
}
