using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luminfield.Core;

public enum SaveLoadStatus
{
    Missing,
    Loaded,
    Corrupt,
    Unsupported
}

public sealed record SaveLoadResult(
    SaveLoadStatus Status,
    GameSaveV1? Save = null,
    string? PreservedPath = null
);

public sealed class SaveService
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public SaveService(string path)
    {
        Path = path;
    }

    public string Path { get; }
    public bool Exists => File.Exists(Path);

    public void Save(GameSaveV1 save)
    {
        save.SchemaVersion = CurrentSchemaVersion;
        var directory = System.IO.Path.GetDirectoryName(Path)
            ?? throw new InvalidOperationException("Save path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{Path}.tmp";
        try
        {
            var json = JsonSerializer.Serialize(save, JsonOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, Path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public SaveLoadResult Load()
    {
        if (!File.Exists(Path))
        {
            return new SaveLoadResult(SaveLoadStatus.Missing);
        }

        try
        {
            var json = File.ReadAllText(Path);
            var save = JsonSerializer.Deserialize<GameSaveV1>(json, JsonOptions)
                ?? throw new InvalidDataException("Save JSON did not contain an object.");

            if (save.SchemaVersion > CurrentSchemaVersion)
            {
                return new SaveLoadResult(SaveLoadStatus.Unsupported);
            }

            if (save.SchemaVersion <= 0)
            {
                save.SchemaVersion = CurrentSchemaVersion;
            }

            Normalize(save);
            return new SaveLoadResult(SaveLoadStatus.Loaded, save);
        }
        catch (Exception exception) when (
            exception is JsonException or IOException or InvalidDataException
        )
        {
            var preserved = PreserveBrokenSave();
            return new SaveLoadResult(SaveLoadStatus.Corrupt, null, preserved);
        }
    }

    private static void Normalize(GameSaveV1 save)
    {
        save.Day = Math.Max(1, save.Day);
        save.MinuteOfDay = Math.Clamp(save.MinuteOfDay, GameClock.StartMinute, GameClock.EndMinute);
        save.Locale = save.Locale is LocaleService.English or LocaleService.SimplifiedChinese
            ? save.Locale
            : LocaleService.SimplifiedChinese;
        save.Player ??= new PlayerSave();
        save.Player.Energy = Math.Clamp(save.Player.Energy, 0, GameSession.MaxEnergy);
        save.Player.WateringCanWater = Math.Clamp(
            save.Player.WateringCanWater,
            0,
            GameSession.MaxWateringCanWater
        );
        save.Player.SelectedSlot = Math.Clamp(
            save.Player.SelectedSlot,
            0,
            Inventory.HotbarSlotCount - 1
        );
        save.Player.X = float.IsFinite(save.Player.X)
            ? Math.Clamp(save.Player.X, 8, WorldDefinition.Width * 16 - 8)
            : GameSession.NewGamePlayerX;
        save.Player.Y = float.IsFinite(save.Player.Y)
            ? Math.Clamp(save.Player.Y, 8, WorldDefinition.Height * 16 - 8)
            : GameSession.NewGamePlayerY;
        save.Inventory ??= [];
        foreach (var slot in save.Inventory)
        {
            if (slot.ItemId == DataCatalog.LegacyHoeId)
            {
                slot.ItemId = DataCatalog.ShovelId;
            }

            if (!DataCatalog.Items.TryGetValue(slot.ItemId, out var item))
            {
                slot.ItemId = string.Empty;
                slot.Count = 0;
                continue;
            }

            slot.Count = Math.Clamp(slot.Count, 0, item.MaxStack);
            if (slot.Count == 0)
            {
                slot.ItemId = string.Empty;
            }
        }
        save.FarmTiles ??= [];
        save.Quest ??= new QuestSave();
        save.Coins = Math.Max(0, save.Coins);
        save.Processor ??= new ProcessorSave();
        if (!string.IsNullOrWhiteSpace(save.Processor.RecipeId) &&
            DataCatalog.ProcessorRecipes.TryGetValue(save.Processor.RecipeId, out var recipe))
        {
            save.Processor.RemainingNights = Math.Clamp(
                save.Processor.RemainingNights,
                0,
                recipe.Nights
            );
        }
        else
        {
            save.Processor.RecipeId = string.Empty;
            save.Processor.RemainingNights = 0;
        }
        save.Exploration ??= new ExplorationSave();
        save.Exploration.DiscoveredChunks ??= [];
        save.Exploration.DiscoveredChunks = save.Exploration.DiscoveredChunks
            .Where(id => WorldDefinition.TryParseChunkId(id, out _))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (save.Exploration.DiscoveredChunks.Count == 0)
        {
            save.Exploration.DiscoveredChunks.Add(
                WorldDefinition.ChunkId(new ChunkPosition(0, 0))
            );
        }
        save.Resources ??= new ResourceSave();
        save.Resources.RemovedNodes ??= [];
        save.Resources.RemovedNodes = save.Resources.RemovedNodes
            .Where(id =>
                WorldDefinition.TryParseCellId(id, out var cell) &&
                WorldDefinition.ResourceAt(cell) != WorldResourceKind.None)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private string PreserveBrokenSave()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        var preserved = $"{Path}.broken-{timestamp}";
        File.Move(Path, preserved, true);
        return preserved;
    }
}
