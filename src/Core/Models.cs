namespace Luminfield.Core;

public readonly record struct GridPosition(int X, int Y);

public static class PlayerLocationIds
{
    public const string World = "world";
    public const string Cottage = "cottage";
    public const string MoonlitArchive = "moonlit_archive";
    public const string MoonstoneWorkshop = "moonstone_workshop";

    public static bool IsValid(string? locationId) =>
        locationId is World or Cottage or MoonlitArchive or
            MoonstoneWorkshop;

    public static string Normalize(
        string? locationId,
        bool legacyInsideCottage = false
    )
    {
        if (IsValid(locationId))
        {
            return locationId!;
        }

        return legacyInsideCottage ? Cottage : World;
    }
}

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
    public string? FertilizerId { get; set; }
    public string? CropId { get; set; }
    public int WateredNights { get; set; }
    public int QualityRoll { get; set; } = -1;

    public GridPosition Position => new(X, Y);

    public FarmTileState Clone() => new()
    {
        X = X,
        Y = Y,
        Tilled = Tilled,
        Watered = Watered,
        FertilizerId = FertilizerId,
        CropId = CropId,
        WateredNights = WateredNights,
        QualityRoll = QualityRoll
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
    public int WateringCanWater { get; set; } = GameSession.MaxWateringCanWater;
    public int SelectedSlot { get; set; }
    public string LocationId { get; set; } = string.Empty;
    public bool InsideCottage { get; set; }
}

public sealed class ProcessorSave
{
    public string RecipeId { get; set; } = string.Empty;
    public int RemainingNights { get; set; }
}

public sealed class ExplorationSave
{
    public List<string> DiscoveredChunks { get; set; } = [];
}

public sealed class ResourceSave
{
    public List<string> RemovedNodes { get; set; } = [];
    public List<ResourceDepletionSave> DepletedNodes { get; set; } = [];
}

public sealed class ResourceDepletionSave
{
    public string NodeId { get; set; } = string.Empty;
    public int RemovedDay { get; set; } = 1;
}

public sealed class WeatherSave
{
    public int Day { get; set; }
    public string CurrentId { get; set; } = string.Empty;
    public string ForecastId { get; set; } = string.Empty;
}

public sealed class ShippingEntrySave
{
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class ShippingSettlementSave
{
    public int Day { get; set; }
    public List<ShippingEntrySave> Entries { get; set; } = [];
}

public sealed class ShippingSave
{
    public List<ShippingEntrySave> Pending { get; set; } = [];
    public ShippingSettlementSave LastSettlement { get; set; } = new();
}

public sealed class PlacedChestSave
{
    public int X { get; set; }
    public int Y { get; set; }
    public List<InventorySlot> Items { get; set; } = [];
}

public sealed class StorageSave
{
    public List<PlacedChestSave> Chests { get; set; } = [];
}

public sealed class PlacedFarmObjectSave
{
    public int X { get; set; }
    public int Y { get; set; }
    public string ItemId { get; set; } = string.Empty;
}

public sealed class FarmObjectSave
{
    public List<PlacedFarmObjectSave> Objects { get; set; } = [];
}

public sealed class DailyCommissionSave
{
    public int Day { get; set; } = 1;
    public string DefinitionId { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public int Progress { get; set; }
    public bool Claimed { get; set; }
}

public sealed class StarlightContributionSave
{
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class StarlightNodeSave
{
    public string NodeId { get; set; } = string.Empty;
    public List<StarlightContributionSave> Contributions { get; set; } = [];
}

public sealed class StarlightSave
{
    public string PedestalId { get; set; } = string.Empty;
    public bool Discovered { get; set; }
    public bool RewardUnlocked { get; set; }
    public List<StarlightNodeSave> Nodes { get; set; } = [];
}

public sealed class VillageSave
{
    public List<string> MetNpcIds { get; set; } = [];
    public List<VillageRelationshipSave> Relationships { get; set; } = [];
}

public sealed class VillageRelationshipSave
{
    public string NpcId { get; set; } = string.Empty;
    public int Points { get; set; }
    public int LastTalkDay { get; set; }
    public int LastGiftDay { get; set; }
}

public sealed class MailEntrySave
{
    public string MailId { get; set; } = string.Empty;
    public int DeliveredDay { get; set; } = 1;
    public bool IsRead { get; set; }
    public bool AttachmentClaimed { get; set; }
}

public sealed class MailSave
{
    public List<MailEntrySave> Entries { get; set; } = [];
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
    public int Coins { get; set; } = GameSession.NewGameCoins;
    public ProcessorSave Processor { get; set; } = new();
    public ExplorationSave Exploration { get; set; } = new();
    public ResourceSave Resources { get; set; } = new();
    public WeatherSave Weather { get; set; } = new();
    public ShippingSave Shipping { get; set; } = new();
    public StorageSave Storage { get; set; } = new();
    public FarmObjectSave FarmObjects { get; set; } = new();
    public DailyCommissionSave Commission { get; set; } = new();
    public StarlightSave Starlight { get; set; } = new();
    public VillageSave Village { get; set; } = new();
    public MailSave Mail { get; set; } = new();
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

    public static ActionResult Grant(
        string itemId,
        int count,
        int energyCost,
        string messageKey
    ) => new(true, energyCost, messageKey, itemId, count);
}

public enum TargetPreviewState
{
    Neutral,
    Available,
    NeedsTool,
    Blocked
}

public enum TargetPreviewKind
{
    None,
    Ground,
    Soil,
    Crop,
    Tree,
    Crystal,
    Water,
    Landmark,
    StarlightPedestal,
    Mailbox,
    Character,
    Door,
    Station,
    CommissionBoard,
    StorageChest,
    Path,
    Fence,
    Torch,
    Sprinkler,
    Bed
}

public sealed record TargetPreview(
    GridPosition Target,
    TargetPreviewState State,
    TargetPreviewKind Kind,
    string LabelKey = ""
)
{
    public bool IsAvailable => State == TargetPreviewState.Available;

    public static TargetPreview Neutral(GridPosition target) =>
        new(target, TargetPreviewState.Neutral, TargetPreviewKind.None);

    public static TargetPreview Available(
        GridPosition target,
        TargetPreviewKind kind,
        string labelKey
    ) => new(target, TargetPreviewState.Available, kind, labelKey);

    public static TargetPreview NeedsTool(
        GridPosition target,
        TargetPreviewKind kind,
        string labelKey
    ) => new(target, TargetPreviewState.NeedsTool, kind, labelKey);

    public static TargetPreview Blocked(
        GridPosition target,
        TargetPreviewKind kind,
        string labelKey
    ) => new(target, TargetPreviewState.Blocked, kind, labelKey);
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
