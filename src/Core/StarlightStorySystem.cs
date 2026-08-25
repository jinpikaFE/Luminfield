namespace Luminfield.Core;

public enum StarlightStoryBeatKind
{
    Discovery,
    Restoration,
    RegionResponse,
    MainStoryRevisit
}

public sealed record StarlightStoryBeatDefinition(
    string Id,
    string PedestalId,
    StarlightStoryBeatKind Kind,
    string SpeakerKey,
    string StatusKey,
    IReadOnlyList<string> DialogueKeys,
    IReadOnlyList<string> PrerequisiteBeatIds,
    string? RequiredLocationId = null,
    WorldBiome? RequiredBiome = null,
    string? RequiredNpcId = null,
    int MinimumDaysAfterPrerequisites = 0,
    bool RequiresPedestalDiscovered = false,
    bool RequiresPedestalRestored = false,
    GridPosition? RequiredWorldCell = null,
    int RequiredWorldRadius = 0
);

public sealed record StarlightStoryLocalizedListArgument(
    IReadOnlyList<string> Keys,
    string SeparatorKey,
    string EmptyKey
);

public sealed record StarlightStoryDialogue(
    string BeatId,
    string SpeakerKey,
    string StatusKey,
    IReadOnlyList<string> DialogueKeys,
    IReadOnlyList<IReadOnlyList<object>>? DialogueArguments = null
);

public sealed record StarlightStoryProgressContext(
    int CurrentDay,
    string CurrentLocationId,
    WorldBiome? CurrentBiome,
    IReadOnlySet<string> DiscoveredPedestalIds,
    IReadOnlySet<string> RestoredPedestalIds,
    IReadOnlySet<string> MetNpcIds,
    IReadOnlySet<WorldBiome> ExploredBiomes,
    IReadOnlySet<string> CompletedCharacterEventIds,
    bool MainStoryCompleted,
    GridPosition? CurrentWorldCell = null
)
{
    public static StarlightStoryProgressContext Empty(int currentDay = 1) =>
        new(
            Math.Max(1, currentDay),
            PlayerLocationIds.World,
            null,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<WorldBiome>(),
            new HashSet<string>(StringComparer.Ordinal),
            false,
            null
        );

    public static IReadOnlySet<WorldBiome> ExploredBiomesFrom(
        IEnumerable<string> discoveredChunkIds
    )
    {
        var biomes = new HashSet<WorldBiome>();
        foreach (var id in discoveredChunkIds.Distinct(StringComparer.Ordinal))
        {
            if (!WorldDefinition.TryParseChunkId(id, out var chunk))
            {
                continue;
            }

            var minX = chunk.X * WorldDefinition.ChunkSize;
            var minY = chunk.Y * WorldDefinition.ChunkSize;
            var maxX = Math.Min(
                WorldDefinition.Width,
                minX + WorldDefinition.ChunkSize
            );
            var maxY = Math.Min(
                WorldDefinition.Height,
                minY + WorldDefinition.ChunkSize
            );
            for (var y = minY; y < maxY; y++)
            {
                for (var x = minX; x < maxX; x++)
                {
                    biomes.Add(WorldDefinition.GetBiome(
                        new GridPosition(x, y)
                    ));
                }
            }
        }

        return biomes;
    }
}

public static class StarlightStoryCatalog
{
    public const string WoodlandDiscoveryId =
        "story01_starlight_woodland_discovery";
    public const string WoodlandRestorationId =
        "story01_starlight_woodland_restoration";
    public const string WoodlandResponseId =
        "story01_starlight_woodland_response";
    public const string WoodlandRevisitId =
        "story01_starlight_woodland_revisit";

    public const string HomesteadDiscoveryId =
        "story01_starlight_homestead_discovery";
    public const string HomesteadRestorationId =
        "story01_starlight_homestead_restoration";
    public const string HomesteadResponseId =
        "story01_starlight_homestead_response";
    public const string HomesteadRevisitId =
        "story01_starlight_homestead_revisit";

    public const string MeadowDiscoveryId =
        "story01_starlight_meadow_discovery";
    public const string MeadowRestorationId =
        "story01_starlight_meadow_restoration";
    public const string MeadowResponseId =
        "story01_starlight_meadow_response";
    public const string MeadowRevisitId =
        "story01_starlight_meadow_revisit";

    public const string MoonwaterDiscoveryId =
        "story01_starlight_moonwater_discovery";
    public const string MoonwaterRestorationId =
        "story01_starlight_moonwater_restoration";
    public const string MoonwaterResponseId =
        "story01_starlight_moonwater_response";
    public const string MoonwaterRevisitId =
        "story01_starlight_moonwater_revisit";

    public const string CrystalValeDiscoveryId =
        "story01_starlight_crystal_vale_discovery";
    public const string CrystalValeRestorationId =
        "story01_starlight_crystal_vale_restoration";
    public const string CrystalValeResponseId =
        "story01_starlight_crystal_vale_response";
    public const string CrystalValeRevisitId =
        "story01_starlight_crystal_vale_revisit";

    public const string StarfallRuinsDiscoveryId =
        "story01_starlight_starfall_ruins_discovery";
    public const string StarfallRuinsRestorationId =
        "story01_starlight_starfall_ruins_restoration";
    public const string StarfallRuinsResponseId =
        "story01_starlight_starfall_ruins_response";
    public const string StarfallRuinsRevisitId =
        "story01_starlight_starfall_ruins_revisit";

    public static IReadOnlyList<StarlightStoryBeatDefinition> Beats { get; } =
        BuildBeats();

    public static IReadOnlyDictionary<string, StarlightStoryBeatDefinition>
        ById { get; } = Beats.ToDictionary(
            beat => beat.Id,
            StringComparer.Ordinal
        );

    public static IReadOnlyList<string> ValidationErrors { get; } =
        Validate(Beats);

    static StarlightStoryCatalog()
    {
        if (ValidationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Invalid STORY-01 catalog: {string.Join("; ", ValidationErrors)}"
            );
        }
    }

    public static StarlightStoryBeatDefinition? Find(
        string pedestalId,
        StarlightStoryBeatKind kind
    ) => Beats.FirstOrDefault(beat =>
        beat.PedestalId == pedestalId && beat.Kind == kind
    );

    public static IReadOnlyList<StarlightStoryBeatDefinition> ForPedestal(
        string pedestalId
    ) => Beats.Where(beat => beat.PedestalId == pedestalId).ToArray();

    private static IReadOnlyList<StarlightStoryBeatDefinition> BuildBeats()
    {
        var beats = new List<StarlightStoryBeatDefinition>(24);
        AddRegionChain(
            beats,
            "woodland",
            DataCatalog.WoodlandStarlightId,
            WorldBiome.WhisperingWoods,
            WorldDefinition.WoodlandStarlightCell,
            WoodlandDiscoveryId,
            WoodlandRestorationId,
            WoodlandResponseId,
            WoodlandRevisitId
        );
        AddRegionChain(
            beats,
            "homestead",
            DataCatalog.HomesteadStarlightId,
            WorldBiome.Home,
            FarmLayout.HomesteadStoryResponseCell,
            HomesteadDiscoveryId,
            HomesteadRestorationId,
            HomesteadResponseId,
            HomesteadRevisitId
        );
        AddRegionChain(
            beats,
            "meadow",
            DataCatalog.MeadowStarlightId,
            WorldBiome.StarfallMeadow,
            WorldDefinition.MeadowStarlightCell,
            MeadowDiscoveryId,
            MeadowRestorationId,
            MeadowResponseId,
            MeadowRevisitId
        );
        AddRegionChain(
            beats,
            "moonwater",
            DataCatalog.MoonwaterStarlightId,
            WorldBiome.MoonwaterWetlands,
            WorldDefinition.MoonwaterStarlightCell,
            MoonwaterDiscoveryId,
            MoonwaterRestorationId,
            MoonwaterResponseId,
            MoonwaterRevisitId
        );
        AddRegionChain(
            beats,
            "crystal_vale",
            DataCatalog.CrystalValeStarlightId,
            WorldBiome.StarfallRuins,
            StarfallRuinsTrialLayout.WorldEntryCell,
            CrystalValeDiscoveryId,
            CrystalValeRestorationId,
            CrystalValeResponseId,
            CrystalValeRevisitId
        );
        AddRegionChain(
            beats,
            "starfall_ruins",
            DataCatalog.StarfallRuinsStarlightId,
            WorldBiome.LumenVillage,
            FarmLayout.StarGateCell,
            StarfallRuinsDiscoveryId,
            StarfallRuinsRestorationId,
            StarfallRuinsResponseId,
            StarfallRuinsRevisitId
        );
        return Array.AsReadOnly(beats.ToArray());
    }

    private static void AddRegionChain(
        ICollection<StarlightStoryBeatDefinition> beats,
        string regionId,
        string pedestalId,
        WorldBiome biome,
        GridPosition responseCell,
        string discoveryId,
        string restorationId,
        string responseId,
        string revisitId
    )
    {
        var pedestalNameKey = DataCatalog.StarlightPedestal(pedestalId).NameKey;
        beats.Add(new StarlightStoryBeatDefinition(
            discoveryId,
            pedestalId,
            StarlightStoryBeatKind.Discovery,
            pedestalNameKey,
            StatusKey(regionId, "discovery"),
            DialogueKeys(regionId, "discovery", 3),
            [],
            PlayerLocationIds.World,
            RequiresPedestalDiscovered: true
        ));
        beats.Add(new StarlightStoryBeatDefinition(
            restorationId,
            pedestalId,
            StarlightStoryBeatKind.Restoration,
            pedestalNameKey,
            StatusKey(regionId, "restoration"),
            DialogueKeys(regionId, "restoration", 3),
            [discoveryId],
            PlayerLocationIds.World,
            RequiresPedestalDiscovered: true,
            RequiresPedestalRestored: true
        ));
        beats.Add(new StarlightStoryBeatDefinition(
            responseId,
            pedestalId,
            StarlightStoryBeatKind.RegionResponse,
            pedestalNameKey,
            StatusKey(regionId, "response"),
            DialogueKeys(regionId, "response", 2),
            [restorationId],
            PlayerLocationIds.World,
            biome,
            MinimumDaysAfterPrerequisites: 1,
            RequiresPedestalDiscovered: true,
            RequiresPedestalRestored: true,
            RequiredWorldCell: responseCell,
            RequiredWorldRadius: 2
        ));
        beats.Add(new StarlightStoryBeatDefinition(
            revisitId,
            pedestalId,
            StarlightStoryBeatKind.MainStoryRevisit,
            "village.npc.liora.name",
            "village.npc.liora.role",
            DialogueKeys(regionId, "revisit", 3),
            [responseId],
            PlayerLocationIds.MoonlitArchive,
            RequiredNpcId: VillageCatalog.LioraId,
            RequiresPedestalDiscovered: true,
            RequiresPedestalRestored: true
        ));
    }

    private static IReadOnlyList<string> DialogueKeys(
        string regionId,
        string beatId,
        int count
    ) => Enumerable.Range(1, count)
        .Select(index => $"story01.starlight.{regionId}.{beatId}.{index}")
        .ToArray();

    private static string StatusKey(string regionId, string beatId) =>
        $"story01.starlight.status.{regionId}.{beatId}";

    private static IReadOnlyList<string> Validate(
        IReadOnlyList<StarlightStoryBeatDefinition> beats
    )
    {
        var errors = new List<string>();
        var byId = new Dictionary<string, StarlightStoryBeatDefinition>(
            StringComparer.Ordinal
        );
        foreach (var beat in beats)
        {
            if (string.IsNullOrWhiteSpace(beat.Id) || !byId.TryAdd(beat.Id, beat))
            {
                errors.Add($"duplicate_or_empty_id:{beat.Id}");
            }
            if (!DataCatalog.StarlightPedestals.ContainsKey(beat.PedestalId))
            {
                errors.Add($"unknown_pedestal:{beat.Id}");
            }
            if (beat.DialogueKeys.Count == 0 ||
                beat.DialogueKeys.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"empty_dialogue:{beat.Id}");
            }
            if (beat.RequiredLocationId is { } locationId &&
                !PlayerLocationIds.IsValid(locationId))
            {
                errors.Add($"unknown_location:{beat.Id}");
            }
            if (beat.RequiredNpcId is { } npcId &&
                !VillageCatalog.Npcs.ContainsKey(npcId))
            {
                errors.Add($"unknown_npc:{beat.Id}");
            }
            if (beat.MinimumDaysAfterPrerequisites < 0)
            {
                errors.Add($"negative_delay:{beat.Id}");
            }
            if (beat.Kind == StarlightStoryBeatKind.RegionResponse &&
                beat.RequiredBiome is null)
            {
                errors.Add($"response_without_biome:{beat.Id}");
            }
            if (beat.Kind == StarlightStoryBeatKind.RegionResponse)
            {
                if (beat.RequiredWorldCell is not { } responseCell)
                {
                    errors.Add($"response_without_world_cell:{beat.Id}");
                }
                else if (!WorldDefinition.IsInBounds(responseCell) ||
                         WorldDefinition.GetBiome(responseCell) !=
                             beat.RequiredBiome)
                {
                    errors.Add($"invalid_response_world_cell:{beat.Id}");
                }
            }
            if (beat.RequiredWorldRadius < 0)
            {
                errors.Add($"negative_world_radius:{beat.Id}");
            }
            if (beat.Kind == StarlightStoryBeatKind.MainStoryRevisit &&
                beat.RequiredNpcId is null)
            {
                errors.Add($"revisit_without_npc:{beat.Id}");
            }
        }

        foreach (var beat in beats)
        {
            foreach (var prerequisite in beat.PrerequisiteBeatIds)
            {
                if (!byId.ContainsKey(prerequisite))
                {
                    errors.Add($"unknown_prerequisite:{beat.Id}:{prerequisite}");
                }
                else if (prerequisite == beat.Id)
                {
                    errors.Add($"self_prerequisite:{beat.Id}");
                }
            }
        }

        foreach (var pedestalId in DataCatalog.StarlightPedestals.Keys)
        {
            var pedestalBeats = beats.Where(beat =>
                beat.PedestalId == pedestalId
            ).ToArray();
            foreach (var kind in Enum.GetValues<StarlightStoryBeatKind>())
            {
                if (pedestalBeats.Count(beat => beat.Kind == kind) != 1)
                {
                    errors.Add($"invalid_pedestal_kind_count:{pedestalId}:{kind}");
                }
            }
        }

        var visitState = new Dictionary<string, int>(StringComparer.Ordinal);
        bool Visit(string id)
        {
            if (!byId.TryGetValue(id, out var beat))
            {
                return false;
            }
            if (visitState.TryGetValue(id, out var state))
            {
                return state == 1;
            }

            visitState[id] = 1;
            foreach (var prerequisite in beat.PrerequisiteBeatIds)
            {
                if (Visit(prerequisite))
                {
                    errors.Add($"cyclic_prerequisite:{id}:{prerequisite}");
                    return true;
                }
            }
            visitState[id] = 2;
            return false;
        }

        foreach (var id in byId.Keys)
        {
            Visit(id);
        }

        return errors.Distinct(StringComparer.Ordinal).ToArray();
    }
}

public sealed class StarlightStorySystem
{
    private Dictionary<string, int> _completedDays =
        new(StringComparer.Ordinal);

    public string? ActiveBeatId { get; private set; }
    public IReadOnlyDictionary<string, int> CompletedDays => _completedDays;

    public event Action? Changed;

    public void Reset()
    {
        _completedDays.Clear();
        ActiveBeatId = null;
        Changed?.Invoke();
    }

    public void Restore(
        StarlightStorySave? save,
        int currentDay,
        StarlightStoryProgressContext? context = null
    )
    {
        var normalized = NormalizeSave(save, currentDay, context);
        _completedDays = normalized.Entries.ToDictionary(
            entry => entry.BeatId,
            entry => entry.CompletedDay,
            StringComparer.Ordinal
        );
        ActiveBeatId = null;
        Changed?.Invoke();
    }

    public bool IsCompleted(string beatId) => _completedDays.ContainsKey(beatId);

    public int CompletedDay(string beatId) =>
        _completedDays.TryGetValue(beatId, out var day) ? day : 0;

    public bool CanBegin(
        string beatId,
        StarlightStoryProgressContext context
    ) => ActiveBeatId is null &&
        !IsCompleted(beatId) &&
        StarlightStoryCatalog.ById.TryGetValue(beatId, out var beat) &&
        IsRuntimeEligible(beat, context);

    public StarlightStoryDialogue? TryBegin(
        string beatId,
        StarlightStoryProgressContext context
    )
    {
        if (!CanBegin(beatId, context))
        {
            return null;
        }

        var beat = StarlightStoryCatalog.ById[beatId];
        ActiveBeatId = beatId;
        return new StarlightStoryDialogue(
            beat.Id,
            beat.SpeakerKey,
            beat.StatusKey,
            beat.DialogueKeys
        );
    }

    public ActionResult Complete(string beatId, int currentDay)
    {
        if (!StarlightStoryCatalog.ById.ContainsKey(beatId))
        {
            return ActionResult.Fail("story01.starlight.unknown_beat");
        }
        if (IsCompleted(beatId))
        {
            return ActionResult.Fail("story01.starlight.beat_already_complete");
        }
        if (ActiveBeatId != beatId)
        {
            return ActionResult.Fail("story01.starlight.beat_not_active");
        }

        _completedDays[beatId] = Math.Max(1, currentDay);
        ActiveBeatId = null;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "story01.starlight.beat_completed"
        );
    }

    public void CancelActive()
    {
        ActiveBeatId = null;
    }

    public StarlightStorySave Capture() => new()
    {
        Entries = _completedDays
            .OrderBy(entry => StoryOrder(entry.Key))
            .Select(entry => new StarlightStoryEntrySave
            {
                BeatId = entry.Key,
                CompletedDay = entry.Value
            })
            .ToList()
    };

    public static StarlightStorySave NormalizeSave(
        StarlightStorySave? save,
        int currentDay,
        StarlightStoryProgressContext? context = null
    )
    {
        var maximumDay = Math.Max(1, currentDay);
        var candidateDays = (save?.Entries ?? [])
            .Where(entry => StarlightStoryCatalog.ById.ContainsKey(entry.BeatId))
            .GroupBy(entry => entry.BeatId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(entry => Math.Clamp(
                        entry.CompletedDay,
                        1,
                        maximumDay
                    ))
                    .Distinct()
                    .Order()
                    .ToArray(),
                StringComparer.Ordinal
            );
        var accepted = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var beat in StarlightStoryCatalog.Beats)
        {
            if (!candidateDays.TryGetValue(beat.Id, out var days) ||
                context is not null && !HasPersistentSupport(
                    beat,
                    context,
                    requireRecordedNpc: true
                ))
            {
                continue;
            }

            var prerequisiteDays = new List<int>();
            var missingPrerequisite = false;
            foreach (var prerequisite in beat.PrerequisiteBeatIds)
            {
                if (!accepted.TryGetValue(prerequisite, out var prerequisiteDay))
                {
                    missingPrerequisite = true;
                    break;
                }
                prerequisiteDays.Add(prerequisiteDay);
            }
            if (missingPrerequisite)
            {
                continue;
            }

            var earliestDay = prerequisiteDays.Count == 0
                ? 1
                : prerequisiteDays.Max() +
                    beat.MinimumDaysAfterPrerequisites;
            var acceptedDay = days.FirstOrDefault(day => day >= earliestDay);
            if (acceptedDay > 0)
            {
                accepted[beat.Id] = acceptedDay;
            }
        }

        return new StarlightStorySave
        {
            Entries = accepted
                .OrderBy(entry => StoryOrder(entry.Key))
                .Select(entry => new StarlightStoryEntrySave
                {
                    BeatId = entry.Key,
                    CompletedDay = entry.Value
                })
                .ToList()
        };
    }

    private bool IsRuntimeEligible(
        StarlightStoryBeatDefinition beat,
        StarlightStoryProgressContext context
    )
    {
        if (!HasPersistentSupport(beat, context) ||
            beat.RequiredLocationId is { } locationId &&
            context.CurrentLocationId != locationId ||
            beat.RequiredBiome is { } biome &&
            context.CurrentBiome != biome ||
            beat.RequiredWorldCell is { } requiredCell &&
            (context.CurrentWorldCell is not { } currentCell ||
             ManhattanDistance(currentCell, requiredCell) >
                 beat.RequiredWorldRadius))
        {
            return false;
        }

        var prerequisiteDays = new List<int>();
        foreach (var prerequisite in beat.PrerequisiteBeatIds)
        {
            if (!_completedDays.TryGetValue(prerequisite, out var day))
            {
                return false;
            }
            prerequisiteDays.Add(day);
        }

        return prerequisiteDays.Count == 0 ||
            context.CurrentDay >= prerequisiteDays.Max() +
                beat.MinimumDaysAfterPrerequisites;
    }

    private static bool HasPersistentSupport(
        StarlightStoryBeatDefinition beat,
        StarlightStoryProgressContext context,
        bool requireRecordedNpc = false
    )
    {
        if (beat.RequiresPedestalDiscovered &&
            !context.DiscoveredPedestalIds.Contains(beat.PedestalId) &&
            !context.RestoredPedestalIds.Contains(beat.PedestalId))
        {
            return false;
        }
        if (beat.RequiresPedestalRestored &&
            !context.RestoredPedestalIds.Contains(beat.PedestalId))
        {
            return false;
        }
        if (beat.Kind == StarlightStoryBeatKind.RegionResponse &&
            beat.RequiredBiome is { } biome &&
            !context.ExploredBiomes.Contains(biome))
        {
            return false;
        }
        if (requireRecordedNpc &&
            beat.RequiredNpcId is { } npcId &&
            !context.MetNpcIds.Contains(npcId))
        {
            return false;
        }

        return true;
    }

    private static int ManhattanDistance(
        GridPosition left,
        GridPosition right
    ) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

    private static int StoryOrder(string beatId)
    {
        for (var index = 0; index < StarlightStoryCatalog.Beats.Count; index++)
        {
            if (StarlightStoryCatalog.Beats[index].Id == beatId)
            {
                return index;
            }
        }

        return int.MaxValue;
    }
}
