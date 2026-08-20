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
        save.Player.LocationId = PlayerLocationIds.Normalize(
            save.Player.LocationId,
            save.Player.InsideCottage
        );
        save.Player.InsideCottage =
            save.Player.LocationId == PlayerLocationIds.Cottage;
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
        foreach (var tile in save.FarmTiles)
        {
            if (!tile.Tilled)
            {
                tile.Watered = false;
                tile.FertilizerId = null;
                tile.CropId = null;
                tile.WateredNights = 0;
                tile.QualityRoll = -1;
                tile.PlantedDay = 0;
                tile.ResonanceItemId = null;
                continue;
            }

            if (tile.FertilizerId != DataCatalog.StarsoilFertilizerId)
            {
                tile.FertilizerId = null;
            }

            if (string.IsNullOrWhiteSpace(tile.CropId) ||
                !DataCatalog.Crops.ContainsKey(tile.CropId))
            {
                tile.CropId = null;
                tile.WateredNights = 0;
                tile.QualityRoll = -1;
                tile.PlantedDay = 0;
                tile.ResonanceItemId = null;
                continue;
            }

            var crop = DataCatalog.Crop(tile.CropId);
            tile.WateredNights = Math.Clamp(
                tile.WateredNights,
                0,
                crop.MatureAfterWateredNights
            );
            tile.QualityRoll = Math.Clamp(tile.QualityRoll, 0, 99);
            tile.PlantedDay = Math.Max(1, tile.PlantedDay);
            if (!crop.AllowsResonanceItem(tile.ResonanceItemId))
            {
                tile.ResonanceItemId = null;
            }
        }
        save.Quest ??= new QuestSave();
        save.Coins = Math.Max(0, save.Coins);
        save.Processor ??= new ProcessorSave();
        save.Processor.Machines ??= [];
        save.Processor.Machines = save.Processor.Machines
            .Where(entry =>
                entry is not null &&
                ProcessorCatalog.Machines.ContainsKey(entry.MachineId)
            )
            .GroupBy(entry => entry.MachineId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
        foreach (var machine in save.Processor.Machines)
        {
            if (string.IsNullOrWhiteSpace(machine.RecipeId) ||
                !DataCatalog.ProcessorRecipes.TryGetValue(
                    machine.RecipeId,
                    out var machineRecipe
                ) ||
                !ProcessorCatalog.SupportsRecipe(
                    machine.MachineId,
                    machine.RecipeId
                ))
            {
                machine.RecipeId = string.Empty;
                machine.RemainingNights = 0;
                continue;
            }

            machine.RemainingNights = Math.Clamp(
                machine.RemainingNights,
                0,
                machineRecipe.Nights
            );
        }

        if (save.Processor.Machines.Count > 0)
        {
            var mainMachine = save.Processor.Machines.FirstOrDefault(entry =>
                entry.MachineId == ProcessorCatalog.MainMachineId
            );
            save.Processor.RecipeId = mainMachine?.RecipeId ?? string.Empty;
            save.Processor.RemainingNights = mainMachine?.RemainingNights ?? 0;
        }
        else if (!string.IsNullOrWhiteSpace(save.Processor.RecipeId) &&
            DataCatalog.ProcessorRecipes.TryGetValue(
                save.Processor.RecipeId,
                out var legacyRecipe
            ) &&
            ProcessorCatalog.SupportsRecipe(
                ProcessorCatalog.MainMachineId,
                save.Processor.RecipeId
            ))
        {
            save.Processor.RemainingNights = Math.Clamp(
                save.Processor.RemainingNights,
                0,
                legacyRecipe.Nights
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
        save.Resources.DepletedNodes ??= [];
        var datedDepletions = save.Resources.DepletedNodes
            .Where(entry =>
                WorldDefinition.TryParseCellId(entry.NodeId, out var cell) &&
                WorldDefinition.ResourceAt(cell) != WorldResourceKind.None)
            .GroupBy(entry => entry.NodeId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => Math.Clamp(
                    group.Max(entry => entry.RemovedDay),
                    1,
                    save.Day
                ),
                StringComparer.Ordinal
            );
        foreach (var id in save.Resources.RemovedNodes)
        {
            datedDepletions.TryAdd(id, save.Day);
        }

        save.Resources.DepletedNodes = datedDepletions
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new ResourceDepletionSave
            {
                NodeId = pair.Key,
                RemovedDay = pair.Value
            })
            .ToList();
        save.Resources.RemovedNodes = save.Resources.DepletedNodes
            .Select(entry => entry.NodeId)
            .ToList();
        save.Weather ??= new WeatherSave();
        var weatherMatchesDay = save.Weather.Day == save.Day;
        save.Weather.Day = save.Day;
        save.Weather.CurrentId = weatherMatchesDay &&
            DataCatalog.WeatherDefinitions.ContainsKey(
            save.Weather.CurrentId
        )
            ? save.Weather.CurrentId
            : WeatherSystem.WeatherForDay(save.Day);
        save.Weather.ForecastId = weatherMatchesDay &&
            DataCatalog.WeatherDefinitions.ContainsKey(
            save.Weather.ForecastId
        )
            ? save.Weather.ForecastId
            : WeatherSystem.WeatherForDay(save.Day + 1);
        save.Shipping ??= new ShippingSave();
        save.Shipping.Pending = NormalizeShippingEntries(save.Shipping.Pending);
        save.Shipping.LastSettlement ??= new ShippingSettlementSave();
        save.Shipping.LastSettlement.Day = Math.Max(
            0,
            Math.Min(save.Shipping.LastSettlement.Day, save.Day)
        );
        save.Shipping.LastSettlement.Entries = NormalizeShippingEntries(
            save.Shipping.LastSettlement.Entries
        );
        save.Animals = AnimalSystem.NormalizeSave(save.Animals);
        var occupiedAnimalCells = save.Animals.CoopBuilt
            ? AnimalCatalog.CoopCells.ToHashSet()
            : new HashSet<GridPosition>();
        save.Storage ??= new StorageSave();
        save.Storage.Chests ??= [];
        var occupiedFarmTiles = save.FarmTiles
            .Select(tile => tile.Position)
            .ToHashSet();
        var farm = new FarmSystem();
        save.Storage.Chests = save.Storage.Chests
            .Where(chest =>
            {
                var position = new GridPosition(chest.X, chest.Y);
                return WorldDefinition.IsHomeCell(position) &&
                    !WorldDefinition.IsBlocked(position) &&
                    !FarmLayout.IsStaticBlocked(position) &&
                    !FarmSystem.IsPlantingBed(position) &&
                    !farm.IsReserved(position) &&
                    !occupiedFarmTiles.Contains(position) &&
                    !occupiedAnimalCells.Contains(position);
            })
            .GroupBy(chest => new GridPosition(chest.X, chest.Y))
            .Select(group =>
            {
                var chest = group.First();
                chest.Items = NormalizeStorageItems(chest.Items);
                return chest;
            })
            .OrderBy(chest => chest.Y)
            .ThenBy(chest => chest.X)
            .ToList();
        save.FarmObjects ??= new FarmObjectSave();
        save.FarmObjects.Objects ??= [];
        var occupiedStorageCells = save.Storage.Chests
            .Select(chest => new GridPosition(chest.X, chest.Y))
            .ToHashSet();
        var occupiedObjectCells = new HashSet<GridPosition>();
        var normalizedFarmObjects = new List<PlacedFarmObjectSave>();
        foreach (var entry in save.FarmObjects.Objects
                     .OrderBy(entry => entry.Y)
                     .ThenBy(entry => entry.X))
        {
            if (!DataCatalog.FarmObjects.TryGetValue(
                    entry.ItemId,
                    out var definition
                ))
            {
                continue;
            }

            var position = new GridPosition(entry.X, entry.Y);
            var isPlantingBed = FarmSystem.IsPlantingBed(position);
            var hasCorrectSurface =
                definition.Surface == FarmObjectSurface.PlantingBed
                    ? isPlantingBed
                    : !isPlantingBed;
            if (!WorldDefinition.IsHomeCell(position) ||
                WorldDefinition.IsBlocked(position) ||
                FarmLayout.IsStaticBlocked(position) ||
                farm.IsReserved(position) ||
                occupiedFarmTiles.Contains(position) ||
                occupiedStorageCells.Contains(position) ||
                occupiedObjectCells.Contains(position) ||
                occupiedAnimalCells.Contains(position) ||
                !hasCorrectSurface)
            {
                continue;
            }

            occupiedObjectCells.Add(position);
            normalizedFarmObjects.Add(new PlacedFarmObjectSave
            {
                X = position.X,
                Y = position.Y,
                ItemId = definition.ItemId
            });
        }
        save.FarmObjects.Objects = normalizedFarmObjects;
        save.Orchard = OrchardSystem.NormalizeSave(
            save.Orchard,
            save.FarmObjects,
            occupiedFarmTiles,
            occupiedStorageCells,
            occupiedAnimalCells
        );
        save.Commission = DailyCommissionSystem.NormalizeSave(
            save.Commission,
            save.Day
        );
        save.WeeklyCommission = WeeklyCommissionSystem.NormalizeSave(
            save.WeeklyCommission,
            save.Day
        );
        save.Starlight = StarlightSystem.NormalizeSave(save.Starlight);
        save.Village = VillageSystem.NormalizeSave(save.Village);
        save.Mail = MailSystem.NormalizeSave(save.Mail);
        save.CharacterEvents = CharacterEventSystem.NormalizeSave(
            save.CharacterEvents,
            save.Day
        );
        save.Construction = ConstructionSystem.NormalizeSave(
            save.Construction
        );
        save.FarmingSkill = FarmingSkillSystem.NormalizeSave(
            save.FarmingSkill
        );
        save.GleamriseSeason = GleamriseSeasonGoalSystem.NormalizeSave(
            save.GleamriseSeason,
            save.Day
        );
        save.Festival = FestivalSystem.NormalizeSave(
            save.Festival,
            save.Day
        );
    }

    private static List<ShippingEntrySave> NormalizeShippingEntries(
        IEnumerable<ShippingEntrySave>? entries
    ) => (entries ?? [])
        .Where(entry =>
            entry.Count > 0 &&
            DataCatalog.Items.TryGetValue(entry.ItemId, out var item) &&
            item.SellPrice > 0
        )
        .GroupBy(entry => entry.ItemId, StringComparer.Ordinal)
        .Select(group => new ShippingEntrySave
        {
            ItemId = group.Key,
            Count = Math.Min(
                group.Sum(entry => entry.Count),
                Inventory.SlotCount * 99
            )
        })
            .OrderBy(entry => entry.ItemId, StringComparer.Ordinal)
            .ToList();

    private static List<InventorySlot> NormalizeStorageItems(
        IEnumerable<InventorySlot>? slots
    )
    {
        var normalized = new List<InventorySlot>();
        foreach (var group in (slots ?? [])
                     .Where(slot =>
                         slot.Count > 0 &&
                         DataCatalog.StorableItemIds.Contains(
                             slot.ItemId,
                             StringComparer.Ordinal
                         ))
                     .GroupBy(slot => slot.ItemId, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var definition = DataCatalog.Item(group.Key);
            var remaining = Math.Min(
                group.Sum(slot => Math.Max(0, slot.Count)),
                StorageChestState.SlotCount * definition.MaxStack
            );
            while (remaining > 0 && normalized.Count < StorageChestState.SlotCount)
            {
                var count = Math.Min(remaining, definition.MaxStack);
                normalized.Add(new InventorySlot
                {
                    ItemId = group.Key,
                    Count = count
                });
                remaining -= count;
            }

            if (normalized.Count >= StorageChestState.SlotCount)
            {
                break;
            }
        }

        return normalized;
    }

    private string PreserveBrokenSave()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        var preserved = $"{Path}.broken-{timestamp}";
        File.Move(Path, preserved, true);
        return preserved;
    }
}
