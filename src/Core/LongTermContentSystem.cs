namespace Luminfield.Core;

public enum RegionalEventKind
{
    RepeatableEnvironment,
    OneTimeNarrative,
    PostgameRare
}

public sealed record RegionalEventDefinition(
    string Id,
    string PackageId,
    WorldBiome Biome,
    RegionalEventKind Kind,
    string SpeakerKey,
    string StatusKey,
    IReadOnlyList<string> DialogueKeys,
    int StartMinute,
    int EndMinute,
    IReadOnlyList<string> SeasonIds,
    IReadOnlyList<string> WeatherIds,
    string RelationshipNpcId,
    int MinimumRelationshipPoints,
    int CooldownDays,
    bool RequiresMainStory
);

public sealed record RegionalEventDialogue(
    string EventId,
    RegionalEventKind Kind,
    string SpeakerKey,
    string StatusKey,
    IReadOnlyList<string> DialogueKeys
);

public static class RegionalEventCatalog
{
    public const string WoodsMeadowPackageId =
        "regional_event_pack_woods_meadow";
    public const string WetlandsCrystalPackageId =
        "regional_event_pack_wetlands_crystal";
    public const string VillageRuinsPackageId =
        "regional_event_pack_village_ruins";

    public const string WoodsRareEventId =
        "regional_event_postgame_woods_echo_grove";
    public const string WetlandsRareEventId =
        "regional_event_postgame_wetlands_moonwake";
    public const string RuinsRareEventId =
        "regional_event_postgame_ruins_answering_arch";

    public static IReadOnlyList<RegionalEventDefinition> Definitions { get; } =
    [
        Environment(
            "regional_event_woods_moonroot_chorus",
            WoodsMeadowPackageId,
            WorldBiome.WhisperingWoods,
            [CalendarSystem.GleamriseSeasonId],
            [],
            6 * 60,
            12 * 60
        ),
        Narrative(
            "regional_event_woods_vessa_listening_path",
            WoodsMeadowPackageId,
            WorldBiome.WhisperingWoods,
            VillageCatalog.VessaId
        ),
        Environment(
            "regional_event_meadow_starwind_ribbons",
            WoodsMeadowPackageId,
            WorldBiome.StarfallMeadow,
            [],
            [DataCatalog.StardustWindWeatherId],
            12 * 60,
            19 * 60
        ),
        Narrative(
            "regional_event_meadow_brial_pollinator_round",
            WoodsMeadowPackageId,
            WorldBiome.StarfallMeadow,
            VillageCatalog.BrialId
        ),
        Rare(
            WoodsRareEventId,
            WoodsMeadowPackageId,
            WorldBiome.WhisperingWoods,
            19 * 60,
            GameClock.EndMinute
        ),
        Environment(
            "regional_event_wetlands_rainbell_tide",
            WetlandsCrystalPackageId,
            WorldBiome.MoonwaterWetlands,
            [],
            [DataCatalog.RainWeatherId],
            15 * 60,
            GameClock.EndMinute
        ),
        Narrative(
            "regional_event_wetlands_elowen_waterline",
            WetlandsCrystalPackageId,
            WorldBiome.MoonwaterWetlands,
            VillageCatalog.ElowenId
        ),
        Environment(
            "regional_event_crystal_prism_hum",
            WetlandsCrystalPackageId,
            WorldBiome.CrystalVale,
            [],
            [DataCatalog.StardustWindWeatherId],
            8 * 60,
            16 * 60
        ),
        Narrative(
            "regional_event_crystal_tavi_resonant_joint",
            WetlandsCrystalPackageId,
            WorldBiome.CrystalVale,
            VillageCatalog.TaviId
        ),
        Rare(
            WetlandsRareEventId,
            WetlandsCrystalPackageId,
            WorldBiome.MoonwaterWetlands,
            18 * 60,
            GameClock.EndMinute
        ),
        Environment(
            "regional_event_village_lantern_exchange",
            VillageRuinsPackageId,
            WorldBiome.LumenVillage,
            [CalendarSystem.StarharvestSeasonId],
            [],
            17 * 60,
            GameClock.EndMinute
        ),
        Narrative(
            "regional_event_village_liora_uncopied_map",
            VillageRuinsPackageId,
            WorldBiome.LumenVillage,
            VillageCatalog.LioraId
        ),
        Environment(
            "regional_event_ruins_snowlit_inscriptions",
            VillageRuinsPackageId,
            WorldBiome.StarfallRuins,
            [CalendarSystem.LongnightSeasonId],
            [DataCatalog.LongnightSnowWeatherId],
            16 * 60,
            GameClock.EndMinute
        ),
        Narrative(
            "regional_event_ruins_kael_return_marker",
            VillageRuinsPackageId,
            WorldBiome.StarfallRuins,
            VillageCatalog.KaelId
        ),
        Rare(
            RuinsRareEventId,
            VillageRuinsPackageId,
            WorldBiome.StarfallRuins,
            18 * 60,
            GameClock.EndMinute
        )
    ];

    public static IReadOnlyList<string> RareEventIds { get; } =
    [WoodsRareEventId, WetlandsRareEventId, RuinsRareEventId];

    public static RegionalEventDefinition Definition(string eventId) =>
        Definitions.First(definition => definition.Id == eventId);

    public static int DefinitionOrder(string eventId)
    {
        for (var index = 0; index < Definitions.Count; index++)
        {
            if (string.Equals(
                    Definitions[index].Id,
                    eventId,
                    StringComparison.Ordinal
                ))
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    private static RegionalEventDefinition Environment(
        string id,
        string packageId,
        WorldBiome biome,
        IReadOnlyList<string> seasonIds,
        IReadOnlyList<string> weatherIds,
        int startMinute,
        int endMinute
    ) => new(
        id,
        packageId,
        biome,
        RegionalEventKind.RepeatableEnvironment,
        "regional_event.speaker.land",
        $"{id}.status",
        [$"{id}.1", $"{id}.2"],
        startMinute,
        endMinute,
        seasonIds,
        weatherIds,
        string.Empty,
        0,
        1,
        false
    );

    private static RegionalEventDefinition Narrative(
        string id,
        string packageId,
        WorldBiome biome,
        string relationshipNpcId
    ) => new(
        id,
        packageId,
        biome,
        RegionalEventKind.OneTimeNarrative,
        VillageCatalog.Npcs[relationshipNpcId].NameKey,
        $"{id}.status",
        [$"{id}.1", $"{id}.2", $"{id}.3"],
        GameClock.StartMinute,
        GameClock.EndMinute,
        [],
        [],
        relationshipNpcId,
        25,
        0,
        false
    );

    private static RegionalEventDefinition Rare(
        string id,
        string packageId,
        WorldBiome biome,
        int startMinute,
        int endMinute
    ) => new(
        id,
        packageId,
        biome,
        RegionalEventKind.PostgameRare,
        "regional_event.speaker.starvein",
        $"{id}.status",
        [$"{id}.1", $"{id}.2", $"{id}.3"],
        startMinute,
        endMinute,
        [],
        [],
        string.Empty,
        0,
        CalendarSystem.DaysPerSeason,
        true
    );
}

public sealed class RegionalEventSystem
{
    private readonly Dictionary<string, int> _lastSeenDays =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _completedEventIds =
        new(StringComparer.Ordinal);
    private string? _activeEventId;

    public string? ActiveEventId => _activeEventId;
    public IReadOnlySet<string> CompletedEventIds => _completedEventIds;
    public IReadOnlySet<string> CompletedRareEventIds => _completedEventIds
        .Where(RegionalEventCatalog.RareEventIds.Contains)
        .ToHashSet(StringComparer.Ordinal);

    public event Action? Changed;

    public void Reset()
    {
        _lastSeenDays.Clear();
        _completedEventIds.Clear();
        _activeEventId = null;
        Changed?.Invoke();
    }

    public void Restore(RegionalEventSave? save, int currentDay)
    {
        var normalized = NormalizeSave(save, currentDay);
        _lastSeenDays.Clear();
        foreach (var entry in normalized.LastSeenDays)
        {
            _lastSeenDays[entry.EventId] = entry.Day;
        }
        _completedEventIds.Clear();
        _completedEventIds.UnionWith(normalized.CompletedEventIds);
        _activeEventId = null;
        Changed?.Invoke();
    }

    public RegionalEventDialogue? TryBegin(
        WorldBiome biome,
        int day,
        int minuteOfDay,
        string weatherId,
        bool mainStoryCompleted,
        Func<string, int> relationshipPoints
    )
    {
        if (_activeEventId is not null)
        {
            return DialogueFor(_activeEventId);
        }

        var definition = RegionalEventCatalog.Definitions
            .Where(candidate => candidate.Biome == biome)
            .OrderBy(candidate => Priority(candidate.Kind))
            .FirstOrDefault(candidate => IsEligible(
                candidate,
                day,
                minuteOfDay,
                weatherId,
                mainStoryCompleted,
                relationshipPoints
            ));
        if (definition is null)
        {
            return null;
        }

        _activeEventId = definition.Id;
        return DialogueFor(definition.Id);
    }

    public ActionResult CompleteActive(string eventId, int day)
    {
        if (_activeEventId != eventId)
        {
            return ActionResult.Fail("regional_event.not_active");
        }

        var definition = RegionalEventCatalog.Definition(eventId);
        _lastSeenDays[eventId] = Math.Max(1, day);
        if (definition.Kind != RegionalEventKind.RepeatableEnvironment)
        {
            _completedEventIds.Add(eventId);
        }
        _activeEventId = null;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "regional_event.completed"
        );
    }

    public void CancelActive() => _activeEventId = null;

    public RegionalEventSave Capture() => new()
    {
        CompletedEventIds = _completedEventIds
            .OrderBy(RegionalEventCatalog.DefinitionOrder)
            .ThenBy(eventId => eventId, StringComparer.Ordinal)
            .ToList(),
        LastSeenDays = _lastSeenDays
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new RegionalEventSeenSave
            {
                EventId = entry.Key,
                Day = entry.Value
            })
            .ToList()
    };

    public static RegionalEventSave NormalizeSave(
        RegionalEventSave? save,
        int currentDay
    )
    {
        var knownIds = RegionalEventCatalog.Definitions
            .Select(definition => definition.Id)
            .ToHashSet(StringComparer.Ordinal);
        var completed = (save?.CompletedEventIds ?? [])
            .Where(knownIds.Contains)
            .Where(eventId => RegionalEventCatalog.Definition(eventId).Kind !=
                RegionalEventKind.RepeatableEnvironment)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(RegionalEventCatalog.DefinitionOrder)
            .ThenBy(eventId => eventId, StringComparer.Ordinal)
            .ToList();
        var lastSeen = (save?.LastSeenDays ?? [])
            .Where(entry => knownIds.Contains(entry.EventId))
            .GroupBy(entry => entry.EventId, StringComparer.Ordinal)
            .Select(group => new RegionalEventSeenSave
            {
                EventId = group.Key,
                Day = Math.Clamp(
                    group.Max(entry => entry.Day),
                    1,
                    Math.Max(1, currentDay)
                )
            })
            .OrderBy(entry => entry.EventId, StringComparer.Ordinal)
            .ToList();
        return new RegionalEventSave
        {
            CompletedEventIds = completed,
            LastSeenDays = lastSeen
        };
    }

    private bool IsEligible(
        RegionalEventDefinition definition,
        int day,
        int minuteOfDay,
        string weatherId,
        bool mainStoryCompleted,
        Func<string, int> relationshipPoints
    )
    {
        if (definition.RequiresMainStory && !mainStoryCompleted)
        {
            return false;
        }
        if (definition.Kind == RegionalEventKind.OneTimeNarrative &&
            _completedEventIds.Contains(definition.Id))
        {
            return false;
        }
        if (minuteOfDay < definition.StartMinute ||
            minuteOfDay >= definition.EndMinute)
        {
            return false;
        }
        if (definition.SeasonIds.Count > 0 &&
            !definition.SeasonIds.Contains(
                CalendarSystem.SeasonId(day),
                StringComparer.Ordinal
            ))
        {
            return false;
        }
        if (definition.WeatherIds.Count > 0 &&
            !definition.WeatherIds.Contains(weatherId, StringComparer.Ordinal))
        {
            return false;
        }
        if (!string.IsNullOrEmpty(definition.RelationshipNpcId) &&
            relationshipPoints(definition.RelationshipNpcId) <
                definition.MinimumRelationshipPoints)
        {
            return false;
        }
        if (!_lastSeenDays.TryGetValue(definition.Id, out var lastSeenDay))
        {
            return true;
        }

        return Math.Max(1, day) - lastSeenDay >= definition.CooldownDays;
    }

    private static int Priority(RegionalEventKind kind) => kind switch
    {
        RegionalEventKind.OneTimeNarrative => 0,
        RegionalEventKind.PostgameRare => 1,
        _ => 2
    };

    private static RegionalEventDialogue DialogueFor(string eventId)
    {
        var definition = RegionalEventCatalog.Definition(eventId);
        return new RegionalEventDialogue(
            definition.Id,
            definition.Kind,
            definition.SpeakerKey,
            definition.StatusKey,
            definition.DialogueKeys
        );
    }
}

public enum PostgameObjectiveKind
{
    AnnualChallenge,
    RareEvent,
    RelationshipRevisit,
    CollectionCompletion
}

public sealed record PostgameObjectiveSnapshot(
    string Id,
    PostgameObjectiveKind Kind,
    string NameKey,
    int Progress,
    int Target
)
{
    public bool Completed => Progress >= Target;
}

public static class PostgameObjectiveCatalog
{
    public const string AnnualChallengeId = "postgame_objective_annual";
    public const string RareEventId = "postgame_objective_rare_events";
    public const string RelationshipRevisitId =
        "postgame_objective_relationship_revisits";
    public const string CollectionCompletionId =
        "postgame_objective_collection_completion";
    public const int RelationshipRevisitTarget = 4;

    public static IReadOnlyList<(string Id, PostgameObjectiveKind Kind,
        string NameKey)> Definitions { get; } =
    [
        (
            AnnualChallengeId,
            PostgameObjectiveKind.AnnualChallenge,
            "postgame.objective.annual"
        ),
        (
            RareEventId,
            PostgameObjectiveKind.RareEvent,
            "postgame.objective.rare_events"
        ),
        (
            RelationshipRevisitId,
            PostgameObjectiveKind.RelationshipRevisit,
            "postgame.objective.relationship_revisits"
        ),
        (
            CollectionCompletionId,
            PostgameObjectiveKind.CollectionCompletion,
            "postgame.objective.collection_completion"
        )
    ];
}
