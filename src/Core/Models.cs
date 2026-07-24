namespace Luminfield.Core;

public readonly record struct GridPosition(int X, int Y);

public sealed class InventorySlot
{
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(ItemId) || Count <= 0;

    public InventorySlot Clone() => new()
    {
        ItemId = ItemId,
        Count = Count
    };
}

public sealed class FarmTileState
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Tilled { get; set; }
    public bool Watered { get; set; }
    public string? CropId { get; set; }
    public int WateredNights { get; set; }

    public GridPosition Position => new(X, Y);

    public FarmTileState Clone() => new()
    {
        X = X,
        Y = Y,
        Tilled = Tilled,
        Watered = Watered,
        CropId = CropId,
        WateredNights = WateredNights
    };
}

public enum QuestStage
{
    TalkToMira,
    Till,
    Plant,
    Water,
    Grow,
    Harvest,
    ReturnToMira,
    Complete
}

public sealed class QuestSave
{
    public QuestStage Stage { get; set; } = QuestStage.TalkToMira;
    public int Tilled { get; set; }
    public int Planted { get; set; }
    public int Watered { get; set; }
    public int GrownNights { get; set; }
    public int Harvested { get; set; }
}

public sealed class PlayerSave
{
    public float X { get; set; } = GameSession.NewGamePlayerX;
    public float Y { get; set; } = GameSession.NewGamePlayerY;
    public int Energy { get; set; } = GameSession.MaxEnergy;
    public int SelectedSlot { get; set; }
    public bool InsideCottage { get; set; }
}

public sealed class GameSaveV1
{
    public int SchemaVersion { get; set; } = SaveService.CurrentSchemaVersion;
    public int Day { get; set; } = 1;
    public int MinuteOfDay { get; set; } = GameClock.StartMinute;
    public string Locale { get; set; } = LocaleService.SimplifiedChinese;
    public PlayerSave Player { get; set; } = new();
    public List<InventorySlot> Inventory { get; set; } = [];
    public List<FarmTileState> FarmTiles { get; set; } = [];
    public QuestSave Quest { get; set; } = new();
}

public sealed record ActionResult(
    bool Succeeded,
    int EnergyCost = 0,
    string MessageKey = "",
    string? GrantedItemId = null,
    int GrantedItemCount = 0
)
{
    public static ActionResult Fail(string messageKey) => new(false, 0, messageKey);
    public static ActionResult Success(int energyCost = 0, string messageKey = "") =>
        new(true, energyCost, messageKey);
}

public sealed record InteractionContext(GameSession Session, GridPosition Target);

public interface IInteractable
{
    void Interact(InteractionContext context);
}

public interface IToolAction
{
    ActionResult TryApply(GameSession session, GridPosition target);
}
