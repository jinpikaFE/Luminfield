namespace Luminfield.Core;

public sealed record AnimalBuildingDefinition(
    string Id,
    string LocationId,
    string ConstructionProjectId,
    int Capacity,
    string FeedItemId
);

public sealed record AnimalStarterDefinition(
    string InstanceId,
    string SpeciesId,
    string BuildingId
);

public sealed record AnimalSpeciesDefinition(
    string Id,
    string BuildingId,
    string NameKey,
    int AdultAfterFedNights,
    int ProductAfterFedNights,
    string RegularProductItemId,
    string LuminousProductItemId,
    string StarlightProductItemId
);

public static class AnimalCatalog
{
    public const string StarfeatherCoopId = "starfeather_coop";
    public const string StarfeatherChickenId = "starfeather_chicken";
    public const string StarterStarfeatherChickenId =
        "starter_starfeather_chicken";
    public const string MoonfleeceBarnId = "moonfleece_barn";
    public const string MoonfleeceSheepId = "moonfleece_sheep";
    public const string StarterMoonfleeceSheepId =
        "starter_moonfleece_sheep";
    public const string DewhornId = "dewhorn";
    public const string StarterDewhornId = "starter_dewhorn";

    public static AnimalBuildingDefinition StarfeatherCoop { get; } = new(
        StarfeatherCoopId,
        PlayerLocationIds.StarfeatherCoop,
        ConstructionCatalog.HomesteadStarfeatherCoopProjectId,
        4,
        DataCatalog.MeadowFodderId
    );

    public static AnimalBuildingDefinition MoonfleeceBarn { get; } = new(
        MoonfleeceBarnId,
        PlayerLocationIds.MoonfleeceBarn,
        ConstructionCatalog.HomesteadMoonfleeceBarnProjectId,
        4,
        DataCatalog.MeadowFodderId
    );

    public static AnimalSpeciesDefinition StarfeatherChicken { get; } = new(
        StarfeatherChickenId,
        StarfeatherCoopId,
        "animal.starfeather_chicken.name",
        AdultAfterFedNights: 2,
        ProductAfterFedNights: 2,
        DataCatalog.StarfeatherEggId,
        DataCatalog.StarfeatherEggLuminousId,
        DataCatalog.StarfeatherEggStarlightId
    );

    public static AnimalSpeciesDefinition MoonfleeceSheep { get; } = new(
        MoonfleeceSheepId,
        MoonfleeceBarnId,
        "animal.moonfleece_sheep.name",
        AdultAfterFedNights: 3,
        ProductAfterFedNights: 3,
        DataCatalog.MoonfleeceId,
        DataCatalog.MoonfleeceLuminousId,
        DataCatalog.MoonfleeceStarlightId
    );

    public static AnimalSpeciesDefinition Dewhorn { get; } = new(
        DewhornId,
        MoonfleeceBarnId,
        "animal.dewhorn.name",
        AdultAfterFedNights: 4,
        ProductAfterFedNights: 2,
        DataCatalog.DewhornMilkId,
        DataCatalog.DewhornMilkLuminousId,
        DataCatalog.DewhornMilkStarlightId
    );

    public static IReadOnlyList<AnimalBuildingDefinition> Buildings { get; } =
        Array.AsReadOnly([StarfeatherCoop, MoonfleeceBarn]);

    public static IReadOnlyList<AnimalSpeciesDefinition> Species { get; } =
        Array.AsReadOnly([StarfeatherChicken, MoonfleeceSheep, Dewhorn]);

    public static IReadOnlyList<AnimalStarterDefinition> Starters { get; } =
        Array.AsReadOnly(
        [
            new AnimalStarterDefinition(
                StarterStarfeatherChickenId,
                StarfeatherChickenId,
                StarfeatherCoopId
            ),
            new AnimalStarterDefinition(
                StarterMoonfleeceSheepId,
                MoonfleeceSheepId,
                MoonfleeceBarnId
            ),
            new AnimalStarterDefinition(
                StarterDewhornId,
                DewhornId,
                MoonfleeceBarnId
            )
        ]);

    private static readonly IReadOnlyDictionary<
        string,
        AnimalBuildingDefinition
    > BuildingsById = Buildings.ToDictionary(
        definition => definition.Id,
        StringComparer.Ordinal
    );

    private static readonly IReadOnlyDictionary<
        string,
        AnimalSpeciesDefinition
    > SpeciesById = Species.ToDictionary(
        definition => definition.Id,
        StringComparer.Ordinal
    );

    private static readonly IReadOnlyDictionary<
        string,
        AnimalStarterDefinition
    > StartersById = Starters.ToDictionary(
        definition => definition.InstanceId,
        StringComparer.Ordinal
    );

    public static bool TryBuilding(
        string? buildingId,
        out AnimalBuildingDefinition definition
    ) => BuildingsById.TryGetValue(
        buildingId ?? string.Empty,
        out definition!
    );

    public static AnimalBuildingDefinition Building(string buildingId) =>
        BuildingsById.TryGetValue(buildingId, out var definition)
            ? definition
            : throw new KeyNotFoundException(
                $"Unknown animal building '{buildingId}'."
            );

    public static bool TrySpecies(
        string? speciesId,
        out AnimalSpeciesDefinition definition
    ) => SpeciesById.TryGetValue(speciesId ?? string.Empty, out definition!);

    public static AnimalSpeciesDefinition SpeciesDefinition(
        string speciesId
    ) => SpeciesById.TryGetValue(speciesId, out var definition)
        ? definition
        : throw new KeyNotFoundException(
            $"Unknown animal species '{speciesId}'."
        );

    public static bool TryStarter(
        string? instanceId,
        out AnimalStarterDefinition definition
    ) => StartersById.TryGetValue(instanceId ?? string.Empty, out definition!);

    public static IReadOnlyList<AnimalStarterDefinition> StartersForBuilding(
        string buildingId
    ) => Starters
        .Where(definition => definition.BuildingId == buildingId)
        .ToArray();
}

public sealed record AnimalState(
    string InstanceId,
    string SpeciesId,
    string BuildingId,
    int AgeNights,
    int Mood,
    int LastFedDay,
    int LastPettedDay,
    int ProductionProgress,
    string PendingProductItemId
)
{
    public bool IsAdult => AgeNights >= AnimalCatalog
        .SpeciesDefinition(SpeciesId).AdultAfterFedNights;

    public bool HasPendingProduct =>
        !string.IsNullOrWhiteSpace(PendingProductItemId);
}

public sealed record AnimalProjection(
    string InstanceId,
    string SpeciesId,
    string BuildingId,
    string LocationId,
    GridPosition Cell,
    bool IsOutdoors
);

public sealed record AnimalAutomationState(
    string BuildingId,
    int StoredFeed,
    IReadOnlyList<CraftingIngredient> StoredProducts,
    int LastResolvedDay,
    int LastAutoFedCount,
    int LastAutoCollectedCount,
    string LastFeedStatusId,
    string LastCollectionStatusId
)
{
    public int StoredProductCount => StoredProducts.Sum(item => item.Count);
}

public sealed record AnimalAutomationResolution(
    bool Succeeded,
    int Count,
    string StatusId
);

public static class AnimalAutomationStatusIds
{
    public const string NotRun = "not_run";
    public const string NoNeed = "no_need";
    public const string Succeeded = "succeeded";
    public const string InsufficientFeed = "insufficient_feed";
    public const string ProductCapacity = "product_capacity";

    public static bool IsValid(string? statusId) => statusId is
        NotRun or NoNeed or Succeeded or InsufficientFeed or ProductCapacity;

    public static string Normalize(string? statusId) =>
        IsValid(statusId) ? statusId! : NotRun;
}

public sealed class AnimalSystem
{
    public const int MinimumMood = 0;
    public const int MaximumMood = 5;
    public const int InitialMood = 2;
    public const int AutomationFeedCapacity = 28;
    public const int AutomationProductCapacity = 12;

    private readonly Dictionary<string, AnimalEntrySave> _animals =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, AnimalBuildingAutomationSave>
        _automation = new(StringComparer.Ordinal);

    public event Action? Changed;

    public IReadOnlyList<AnimalState> Animals => _animals.Values
        .OrderBy(entry => entry.InstanceId, StringComparer.Ordinal)
        .Select(ToState)
        .ToArray();

    public void Reset()
    {
        _animals.Clear();
        _automation.Clear();
        foreach (var building in AnimalCatalog.Buildings)
        {
            _automation[building.Id] = EmptyAutomation(building.Id);
        }
        Changed?.Invoke();
    }

    public void Restore(AnimalSave? save, int currentDay)
    {
        _animals.Clear();
        foreach (var entry in NormalizeSave(save, currentDay).Animals)
        {
            _animals[entry.InstanceId] = Clone(entry);
        }

        foreach (var entry in NormalizeSave(save, currentDay).Automation)
        {
            _automation[entry.BuildingId] = Clone(entry);
        }

        Changed?.Invoke();
    }

    public bool EnsureStarter(string buildingId)
    {
        if (!AnimalCatalog.TryBuilding(buildingId, out var building))
        {
            return false;
        }

        var changed = false;
        var residentCount = _animals.Values.Count(entry =>
            entry.BuildingId == building.Id
        );
        foreach (var starter in AnimalCatalog.StartersForBuilding(buildingId))
        {
            if (_animals.ContainsKey(starter.InstanceId) ||
                residentCount >= building.Capacity)
            {
                continue;
            }

            _animals[starter.InstanceId] = new()
            {
                InstanceId = starter.InstanceId,
                SpeciesId = starter.SpeciesId,
                BuildingId = starter.BuildingId,
                Mood = InitialMood
            };
            residentCount++;
            changed = true;
        }

        if (changed)
        {
            Changed?.Invoke();
        }

        return changed;
    }

    public bool EnsureStarterStarfeatherChicken() =>
        EnsureStarter(AnimalCatalog.StarfeatherCoopId);

    public AnimalState? Animal(string instanceId) =>
        _animals.TryGetValue(instanceId, out var entry)
            ? ToState(entry)
            : null;

    public IReadOnlyList<AnimalState> AnimalsInBuilding(string buildingId) =>
        _animals.Values
            .Where(entry => entry.BuildingId == buildingId)
            .OrderBy(entry => entry.InstanceId, StringComparer.Ordinal)
            .Select(ToState)
            .ToArray();

    public AnimalAutomationState AutomationFor(string buildingId)
    {
        if (!_automation.TryGetValue(buildingId, out var entry))
        {
            entry = EmptyAutomation(buildingId);
        }

        return ToState(entry);
    }

    public bool EnsureAutomation(string buildingId)
    {
        if (!AnimalCatalog.TryBuilding(buildingId, out _) ||
            _automation.ContainsKey(buildingId))
        {
            return false;
        }

        _automation[buildingId] = EmptyAutomation(buildingId);
        Changed?.Invoke();
        return true;
    }

    public ActionResult CheckStoreAutomationFeed(
        string buildingId,
        int count,
        Inventory inventory
    )
    {
        if (!AnimalCatalog.TryBuilding(buildingId, out var building) ||
            !_automation.TryGetValue(buildingId, out var automation))
        {
            return ActionResult.Fail("animal.unknown_building");
        }

        if (count <= 0 || inventory.Count(building.FeedItemId) < count)
        {
            return ActionResult.Fail("animal.feed.insufficient_fodder");
        }

        return automation.StoredFeed + count <= AutomationFeedCapacity
            ? ActionResult.Success()
            : ActionResult.Fail("animal.automation.feed_capacity");
    }

    public void StoreAutomationFeedChecked(string buildingId, int count)
    {
        if (!_automation.TryGetValue(buildingId, out var automation) ||
            count <= 0 ||
            automation.StoredFeed + count > AutomationFeedCapacity)
        {
            throw new InvalidOperationException(
                "Automation feed storage was not checked before commit."
            );
        }

        automation.StoredFeed += count;
        Changed?.Invoke();
    }

    public ActionResult CheckTakeAutomationFeed(
        string buildingId,
        int count,
        Inventory inventory
    )
    {
        if (!AnimalCatalog.TryBuilding(buildingId, out var building) ||
            !_automation.TryGetValue(buildingId, out var automation))
        {
            return ActionResult.Fail("animal.unknown_building");
        }

        if (count <= 0 || automation.StoredFeed < count)
        {
            return ActionResult.Fail("animal.automation.no_feed_stored");
        }

        return inventory.CanAdd(building.FeedItemId, count)
            ? ActionResult.Success()
            : ActionResult.Fail("notice.inventory_full");
    }

    public void TakeAutomationFeedChecked(string buildingId, int count)
    {
        if (!_automation.TryGetValue(buildingId, out var automation) ||
            count <= 0 || automation.StoredFeed < count)
        {
            throw new InvalidOperationException(
                "Automation feed withdrawal was not checked before commit."
            );
        }

        automation.StoredFeed -= count;
        Changed?.Invoke();
    }

    public IReadOnlyList<CraftingIngredient> StoredAutomationProducts(
        string buildingId
    ) => AutomationFor(buildingId).StoredProducts;

    public void BeginAutomationNight(string buildingId, int day)
    {
        if (!_automation.TryGetValue(buildingId, out var automation))
        {
            return;
        }

        automation.LastResolvedDay = day;
        automation.LastAutoFedCount = 0;
        automation.LastAutoCollectedCount = 0;
        automation.LastFeedStatusId = AnimalAutomationStatusIds.NoNeed;
        automation.LastCollectionStatusId = AnimalAutomationStatusIds.NoNeed;
        Changed?.Invoke();
    }

    public AnimalAutomationResolution ResolveAutomaticFeed(
        string buildingId,
        int day,
        IReadOnlySet<string> grazingInstanceIds
    )
    {
        if (!_automation.TryGetValue(buildingId, out var automation))
        {
            return new(false, 0, AnimalAutomationStatusIds.NotRun);
        }

        var needsFeed = _animals.Values.Where(entry =>
            entry.BuildingId == buildingId &&
            !grazingInstanceIds.Contains(entry.InstanceId) &&
            entry.LastFedDay != day
        ).ToArray();
        if (needsFeed.Length == 0)
        {
            automation.LastFeedStatusId = AnimalAutomationStatusIds.NoNeed;
            Changed?.Invoke();
            return new(true, 0, AnimalAutomationStatusIds.NoNeed);
        }

        if (automation.StoredFeed < needsFeed.Length)
        {
            automation.LastFeedStatusId =
                AnimalAutomationStatusIds.InsufficientFeed;
            Changed?.Invoke();
            return new(
                false,
                0,
                AnimalAutomationStatusIds.InsufficientFeed
            );
        }

        automation.StoredFeed -= needsFeed.Length;
        foreach (var entry in needsFeed)
        {
            entry.LastFedDay = day;
        }

        automation.LastAutoFedCount += needsFeed.Length;
        automation.LastFeedStatusId = AnimalAutomationStatusIds.Succeeded;
        Changed?.Invoke();
        return new(
            true,
            needsFeed.Length,
            AnimalAutomationStatusIds.Succeeded
        );
    }

    public AnimalAutomationResolution ResolveAutomaticCollection(
        string buildingId
    )
    {
        if (!_automation.TryGetValue(buildingId, out var automation))
        {
            return new(false, 0, AnimalAutomationStatusIds.NotRun);
        }

        var pending = PendingProductsForBuilding(buildingId);
        var pendingCount = pending.Sum(item => item.Count);
        if (pendingCount == 0)
        {
            if (automation.LastAutoCollectedCount == 0)
            {
                automation.LastCollectionStatusId =
                    AnimalAutomationStatusIds.NoNeed;
            }
            Changed?.Invoke();
            return new(true, 0, automation.LastCollectionStatusId);
        }

        var storedCount = automation.StoredProducts.Sum(item => item.Count);
        if (storedCount + pendingCount > AutomationProductCapacity)
        {
            automation.LastCollectionStatusId =
                AnimalAutomationStatusIds.ProductCapacity;
            Changed?.Invoke();
            return new(
                false,
                0,
                AnimalAutomationStatusIds.ProductCapacity
            );
        }

        var merged = automation.StoredProducts
            .Concat(pending.Select(item => new ShippingEntrySave
            {
                ItemId = item.ItemId,
                Count = item.Count
            }))
            .GroupBy(item => item.ItemId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new ShippingEntrySave
            {
                ItemId = group.Key,
                Count = group.Sum(item => item.Count)
            })
            .ToList();
        automation.StoredProducts = merged;
        ClearCollectedProductsChecked(buildingId);
        automation.LastAutoCollectedCount += pendingCount;
        automation.LastCollectionStatusId =
            AnimalAutomationStatusIds.Succeeded;
        Changed?.Invoke();
        return new(
            true,
            pendingCount,
            AnimalAutomationStatusIds.Succeeded
        );
    }

    public void ClearAutomationProductsChecked(string buildingId)
    {
        if (!_automation.TryGetValue(buildingId, out var automation) ||
            automation.StoredProducts.Sum(item => item.Count) == 0)
        {
            throw new InvalidOperationException(
                "Automation products were not checked before commit."
            );
        }

        automation.StoredProducts.Clear();
        Changed?.Invoke();
    }

    public ActionResult CheckFeedBuilding(
        string buildingId,
        int day,
        IReadOnlySet<string> grazingInstanceIds,
        Inventory inventory
    )
    {
        if (!AnimalCatalog.TryBuilding(buildingId, out var building))
        {
            return ActionResult.Fail("animal.unknown_building");
        }

        var residents = _animals.Values
            .Where(entry => entry.BuildingId == buildingId)
            .ToArray();
        if (residents.Length == 0)
        {
            return ActionResult.Fail("animal.feed.no_animals");
        }

        var needsFeed = residents.Where(entry =>
            !grazingInstanceIds.Contains(entry.InstanceId) &&
            entry.LastFedDay != day
        ).ToArray();
        if (needsFeed.Length == 0 &&
            residents.All(entry =>
                grazingInstanceIds.Contains(entry.InstanceId)))
        {
            return ActionResult.Fail("animal.feed.grazing");
        }

        if (needsFeed.Length == 0)
        {
            return ActionResult.Fail("animal.feed.all_fed");
        }

        return inventory.Count(building.FeedItemId) >= needsFeed.Length
            ? ActionResult.Success()
            : ActionResult.Fail("animal.feed.insufficient_fodder");
    }

    public ActionResult CheckFeedBuilding(
        string buildingId,
        int day,
        bool grazing,
        Inventory inventory
    ) => CheckFeedBuilding(
        buildingId,
        day,
        grazing
            ? AnimalsInBuilding(buildingId)
                .Select(animal => animal.InstanceId)
                .ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal),
        inventory
    );

    public int FeedBuildingChecked(
        string buildingId,
        int day,
        IReadOnlySet<string> grazingInstanceIds
    )
    {
        var changed = 0;
        foreach (var entry in _animals.Values.Where(entry =>
                     entry.BuildingId == buildingId &&
                     !grazingInstanceIds.Contains(entry.InstanceId) &&
                     entry.LastFedDay != day
                 ))
        {
            entry.LastFedDay = day;
            changed++;
        }

        if (changed == 0)
        {
            throw new InvalidOperationException(
                "Animal feeding was not checked before commit."
            );
        }

        Changed?.Invoke();
        return changed;
    }

    public int FeedBuildingChecked(string buildingId, int day) =>
        FeedBuildingChecked(
            buildingId,
            day,
            new HashSet<string>(StringComparer.Ordinal)
        );

    public ActionResult CheckPet(string instanceId, int day)
    {
        if (!_animals.TryGetValue(instanceId, out var entry))
        {
            return ActionResult.Fail("animal.unknown_animal");
        }

        return entry.LastPettedDay == day
            ? ActionResult.Fail("animal.pet.already_petted")
            : ActionResult.Success();
    }

    public void PetChecked(string instanceId, int day)
    {
        if (!_animals.TryGetValue(instanceId, out var entry) ||
            entry.LastPettedDay == day)
        {
            throw new InvalidOperationException(
                "Animal petting was not checked before commit."
            );
        }

        entry.LastPettedDay = day;
        Changed?.Invoke();
    }

    public IReadOnlyList<CraftingIngredient> PendingProductsForBuilding(
        string buildingId,
        string? productBaseItemId = null
    ) => _animals.Values
        .Where(entry =>
            entry.BuildingId == buildingId &&
            !string.IsNullOrWhiteSpace(entry.PendingProductItemId) &&
            (string.IsNullOrWhiteSpace(productBaseItemId) ||
             DataCatalog.BaseItemId(entry.PendingProductItemId) ==
                productBaseItemId)
        )
        .GroupBy(
            entry => entry.PendingProductItemId,
            StringComparer.Ordinal
        )
        .OrderBy(group => group.Key, StringComparer.Ordinal)
        .Select(group => new CraftingIngredient(group.Key, group.Count()))
        .ToArray();

    public void ClearCollectedProductsChecked(
        string buildingId,
        string? productBaseItemId = null
    )
    {
        var pending = _animals.Values.Where(entry =>
            entry.BuildingId == buildingId &&
            !string.IsNullOrWhiteSpace(entry.PendingProductItemId) &&
            (string.IsNullOrWhiteSpace(productBaseItemId) ||
             DataCatalog.BaseItemId(entry.PendingProductItemId) ==
                productBaseItemId)
        ).ToArray();
        if (pending.Length == 0)
        {
            throw new InvalidOperationException(
                "Animal product collection was not checked before commit."
            );
        }

        foreach (var entry in pending)
        {
            entry.PendingProductItemId = string.Empty;
        }

        Changed?.Invoke();
    }

    public void ResolveNight(
        string buildingId,
        int endedDay,
        IReadOnlySet<string> grazingInstanceIds
    )
    {
        if (!AnimalCatalog.TryBuilding(buildingId, out _))
        {
            return;
        }

        var residents = _animals.Values
            .Where(entry => entry.BuildingId == buildingId)
            .ToArray();
        if (residents.Length == 0)
        {
            return;
        }

        foreach (var entry in residents)
        {
            var grazed = grazingInstanceIds.Contains(entry.InstanceId);
            var fed = grazed || entry.LastFedDay == endedDay;
            var petted = entry.LastPettedDay == endedDay;
            entry.Mood = Math.Clamp(
                entry.Mood + MoodChange(fed, petted, grazed),
                MinimumMood,
                MaximumMood
            );
            if (!fed)
            {
                continue;
            }

            var species = AnimalCatalog.SpeciesDefinition(entry.SpeciesId);
            if (entry.AgeNights < species.AdultAfterFedNights)
            {
                entry.AgeNights++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(entry.PendingProductItemId))
            {
                continue;
            }

            entry.ProductionProgress++;
            if (entry.ProductionProgress < species.ProductAfterFedNights)
            {
                continue;
            }

            entry.ProductionProgress = 0;
            entry.PendingProductItemId = ProductItemId(species, entry.Mood);
        }

        Changed?.Invoke();
    }

    public void ResolveNight(string buildingId, int endedDay, bool grazed) =>
        ResolveNight(
            buildingId,
            endedDay,
            grazed
                ? AnimalsInBuilding(buildingId)
                    .Select(animal => animal.InstanceId)
                    .ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal)
        );

    public AnimalSave Capture() => new()
    {
        Animals = _animals.Values
            .OrderBy(entry => entry.InstanceId, StringComparer.Ordinal)
            .Select(Clone)
            .ToList(),
        Automation = AnimalCatalog.Buildings
            .Select(building => _automation.TryGetValue(
                    building.Id,
                    out var automation
                )
                ? Clone(automation)
                : EmptyAutomation(building.Id))
            .ToList()
    };

    public static AnimalSave NormalizeSave(AnimalSave? save, int currentDay)
    {
        var candidates = (save?.Animals ?? [])
            .Where(entry => entry is not null)
            .Where(entry =>
                AnimalCatalog.TryStarter(entry.InstanceId, out var starter) &&
                entry.SpeciesId == starter.SpeciesId &&
                entry.BuildingId == starter.BuildingId &&
                AnimalCatalog.TrySpecies(entry.SpeciesId, out _) &&
                AnimalCatalog.TryBuilding(entry.BuildingId, out _)
            )
            .GroupBy(entry => entry.InstanceId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(entry => entry.InstanceId, StringComparer.Ordinal);

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var normalized = new List<AnimalEntrySave>();
        foreach (var entry in candidates)
        {
            var building = AnimalCatalog.Building(entry.BuildingId);
            var count = counts.GetValueOrDefault(building.Id);
            if (count >= building.Capacity)
            {
                continue;
            }

            var species = AnimalCatalog.SpeciesDefinition(entry.SpeciesId);
            var pending = DataCatalog.ItemFamilyIds(
                    species.RegularProductItemId
                )
                .Contains(
                    entry.PendingProductItemId,
                    StringComparer.Ordinal
                )
                ? entry.PendingProductItemId
                : string.Empty;
            normalized.Add(new AnimalEntrySave
            {
                InstanceId = entry.InstanceId,
                SpeciesId = species.Id,
                BuildingId = building.Id,
                AgeNights = Math.Clamp(
                    entry.AgeNights,
                    0,
                    species.AdultAfterFedNights
                ),
                Mood = Math.Clamp(
                    entry.Mood,
                    MinimumMood,
                    MaximumMood
                ),
                LastFedDay = Math.Clamp(entry.LastFedDay, 0, currentDay),
                LastPettedDay = Math.Clamp(
                    entry.LastPettedDay,
                    0,
                    currentDay
                ),
                ProductionProgress = Math.Clamp(
                    entry.ProductionProgress,
                    0,
                    species.ProductAfterFedNights - 1
                ),
                PendingProductItemId = pending
            });
            counts[building.Id] = count + 1;
        }

        var automation = NormalizeAutomation(save?.Automation, currentDay);
        return new AnimalSave
        {
            Animals = normalized,
            Automation = automation
        };
    }

    public static bool CanGraze(int day, string weatherId) =>
        CalendarSystem.SeasonId(day) != CalendarSystem.LongnightSeasonId &&
        weatherId == DataCatalog.ClearWeatherId;

    private static int MoodChange(bool fed, bool petted, bool grazed) =>
        !fed ? -1 : !petted ? 0 : grazed ? 2 : 1;

    private static string ProductItemId(
        AnimalSpeciesDefinition species,
        int mood
    ) => mood switch
    {
        >= 5 => species.StarlightProductItemId,
        >= 3 => species.LuminousProductItemId,
        _ => species.RegularProductItemId
    };

    private static AnimalState ToState(AnimalEntrySave entry) => new(
        entry.InstanceId,
        entry.SpeciesId,
        entry.BuildingId,
        entry.AgeNights,
        entry.Mood,
        entry.LastFedDay,
        entry.LastPettedDay,
        entry.ProductionProgress,
        entry.PendingProductItemId
    );

    private static AnimalEntrySave Clone(AnimalEntrySave entry) => new()
    {
        InstanceId = entry.InstanceId,
        SpeciesId = entry.SpeciesId,
        BuildingId = entry.BuildingId,
        AgeNights = entry.AgeNights,
        Mood = entry.Mood,
        LastFedDay = entry.LastFedDay,
        LastPettedDay = entry.LastPettedDay,
        ProductionProgress = entry.ProductionProgress,
        PendingProductItemId = entry.PendingProductItemId
    };

    private static List<AnimalBuildingAutomationSave> NormalizeAutomation(
        IEnumerable<AnimalBuildingAutomationSave>? entries,
        int currentDay
    )
    {
        var firstByBuilding = (entries ?? [])
            .Where(entry => entry is not null)
            .Where(entry => AnimalCatalog.TryBuilding(entry.BuildingId, out _))
            .GroupBy(entry => entry.BuildingId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.Ordinal
            );
        var normalized = new List<AnimalBuildingAutomationSave>();
        foreach (var building in AnimalCatalog.Buildings)
        {
            if (!firstByBuilding.TryGetValue(building.Id, out var source))
            {
                normalized.Add(EmptyAutomation(building.Id));
                continue;
            }

            var remaining = AutomationProductCapacity;
            var products = (source.StoredProducts ?? [])
                .Where(item => item is not null && item.Count > 0)
                .Where(item => ProductBelongsToBuilding(
                    building.Id,
                    item.ItemId
                ))
                .GroupBy(item => item.ItemId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new ShippingEntrySave
                {
                    ItemId = group.Key,
                    Count = (int)Math.Min(
                        int.MaxValue,
                        group.Aggregate(
                            0L,
                            (total, item) => total + item.Count
                        )
                    )
                })
                .Select(item =>
                {
                    var count = Math.Min(item.Count, remaining);
                    remaining -= count;
                    return new ShippingEntrySave
                    {
                        ItemId = item.ItemId,
                        Count = count
                    };
                })
                .Where(item => item.Count > 0)
                .ToList();
            normalized.Add(new AnimalBuildingAutomationSave
            {
                BuildingId = building.Id,
                StoredFeed = Math.Clamp(
                    source.StoredFeed,
                    0,
                    AutomationFeedCapacity
                ),
                StoredProducts = products,
                LastResolvedDay = Math.Clamp(
                    source.LastResolvedDay,
                    0,
                    currentDay
                ),
                LastAutoFedCount = Math.Clamp(
                    source.LastAutoFedCount,
                    0,
                    building.Capacity
                ),
                LastAutoCollectedCount = Math.Clamp(
                    source.LastAutoCollectedCount,
                    0,
                    AutomationProductCapacity
                ),
                LastFeedStatusId = AnimalAutomationStatusIds.Normalize(
                    source.LastFeedStatusId
                ),
                LastCollectionStatusId =
                    AnimalAutomationStatusIds.Normalize(
                        source.LastCollectionStatusId
                    )
            });
        }

        return normalized;
    }

    private static bool ProductBelongsToBuilding(
        string buildingId,
        string itemId
    ) => AnimalCatalog.Species
        .Where(species => species.BuildingId == buildingId)
        .SelectMany(species => DataCatalog.ItemFamilyIds(
            species.RegularProductItemId
        ))
        .Contains(itemId, StringComparer.Ordinal);

    private static AnimalAutomationState ToState(
        AnimalBuildingAutomationSave entry
    ) => new(
        entry.BuildingId,
        entry.StoredFeed,
        entry.StoredProducts
            .OrderBy(item => item.ItemId, StringComparer.Ordinal)
            .Select(item => new CraftingIngredient(item.ItemId, item.Count))
            .ToArray(),
        entry.LastResolvedDay,
        entry.LastAutoFedCount,
        entry.LastAutoCollectedCount,
        AnimalAutomationStatusIds.Normalize(entry.LastFeedStatusId),
        AnimalAutomationStatusIds.Normalize(entry.LastCollectionStatusId)
    );

    private static AnimalBuildingAutomationSave EmptyAutomation(
        string buildingId
    ) => new()
    {
        BuildingId = buildingId,
        LastFeedStatusId = AnimalAutomationStatusIds.NotRun,
        LastCollectionStatusId = AnimalAutomationStatusIds.NotRun
    };

    private static AnimalBuildingAutomationSave Clone(
        AnimalBuildingAutomationSave entry
    ) => new()
    {
        BuildingId = entry.BuildingId,
        StoredFeed = entry.StoredFeed,
        StoredProducts = entry.StoredProducts.Select(item => new ShippingEntrySave
        {
            ItemId = item.ItemId,
            Count = item.Count
        }).ToList(),
        LastResolvedDay = entry.LastResolvedDay,
        LastAutoFedCount = entry.LastAutoFedCount,
        LastAutoCollectedCount = entry.LastAutoCollectedCount,
        LastFeedStatusId = entry.LastFeedStatusId,
        LastCollectionStatusId = entry.LastCollectionStatusId
    };
}
