namespace Luminfield.Game;

public enum PlaytestScenarioId
{
    Door,
    Cottage,
    Crops,
    Economy,
    Processor,
    ArchiveGift,
    Archive,
    ArchiveDoor,
    WorkshopTavi,
    Workshop,
    WorkshopDoor,
    VillageDialogue,
    SelaDialogue,
    VillageExpansion,
    VillageRestdayEnglish,
    Village,
    World,
    Gate,
    Backpack,
    Resource,
    Target,
    PhaseA,
    PhaseASummary,
    PhaseARain,
    ResourceRespawn,
    Crafting,
    Placeables,
    ChestPlacement,
    Storage,
    CommissionOffer,
    CommissionReady,
    CommissionReadyEnglish,
    CommissionMap,
    MailboxUnread,
    MailPanel,
    MailReward,
    StarlightMap,
    StarlightMapRestored,
    StarlightPanel,
    StarlightRestored,
    StarlightRestoredEnglish,
    QualityCrafting,
    QualityBackpackEnglish,
    QualityBackpack,
    Quality,
    Farm
}

public sealed class PlaytestScenarioRegistry
{
    private sealed record ScenarioDefinition(
        PlaytestScenarioId Id,
        string Flag
    );

    private static readonly ScenarioDefinition[] Definitions =
    [
        new(PlaytestScenarioId.Door, "--playtest-door"),
        new(PlaytestScenarioId.Cottage, "--playtest-cottage"),
        new(PlaytestScenarioId.Crops, "--playtest-crops"),
        new(PlaytestScenarioId.Economy, "--playtest-economy"),
        new(PlaytestScenarioId.Processor, "--playtest-processor"),
        new(PlaytestScenarioId.ArchiveGift, "--playtest-archive-gift"),
        new(PlaytestScenarioId.Archive, "--playtest-archive"),
        new(PlaytestScenarioId.ArchiveDoor, "--playtest-archive-door"),
        new(
            PlaytestScenarioId.WorkshopTavi,
            "--playtest-workshop-tavi"
        ),
        new(PlaytestScenarioId.Workshop, "--playtest-workshop"),
        new(
            PlaytestScenarioId.WorkshopDoor,
            "--playtest-workshop-door"
        ),
        new(
            PlaytestScenarioId.VillageDialogue,
            "--playtest-village-dialogue"
        ),
        new(
            PlaytestScenarioId.SelaDialogue,
            "--playtest-sela-dialogue"
        ),
        new(
            PlaytestScenarioId.VillageExpansion,
            "--playtest-village-expansion"
        ),
        new(
            PlaytestScenarioId.VillageRestdayEnglish,
            "--playtest-village-restday-en"
        ),
        new(PlaytestScenarioId.Village, "--playtest-village"),
        new(PlaytestScenarioId.World, "--playtest-world"),
        new(PlaytestScenarioId.Gate, "--playtest-gate"),
        new(PlaytestScenarioId.Backpack, "--playtest-backpack"),
        new(PlaytestScenarioId.Resource, "--playtest-resource"),
        new(PlaytestScenarioId.Target, "--playtest-target"),
        new(PlaytestScenarioId.PhaseA, "--playtest-phase-a"),
        new(
            PlaytestScenarioId.PhaseASummary,
            "--playtest-phase-a-summary"
        ),
        new(
            PlaytestScenarioId.PhaseARain,
            "--playtest-phase-a-rain"
        ),
        new(
            PlaytestScenarioId.ResourceRespawn,
            "--playtest-resource-respawn"
        ),
        new(PlaytestScenarioId.Crafting, "--playtest-crafting"),
        new(PlaytestScenarioId.Placeables, "--playtest-placeables"),
        new(
            PlaytestScenarioId.ChestPlacement,
            "--playtest-chest-placement"
        ),
        new(PlaytestScenarioId.Storage, "--playtest-storage"),
        new(
            PlaytestScenarioId.CommissionOffer,
            "--playtest-commission-offer"
        ),
        new(
            PlaytestScenarioId.CommissionReady,
            "--playtest-commission-ready"
        ),
        new(
            PlaytestScenarioId.CommissionReadyEnglish,
            "--playtest-commission-ready-en"
        ),
        new(
            PlaytestScenarioId.CommissionMap,
            "--playtest-commission-map"
        ),
        new(
            PlaytestScenarioId.MailboxUnread,
            "--playtest-mailbox-unread"
        ),
        new(
            PlaytestScenarioId.MailPanel,
            "--playtest-mail-panel"
        ),
        new(
            PlaytestScenarioId.MailReward,
            "--playtest-mail-reward"
        ),
        new(
            PlaytestScenarioId.StarlightMap,
            "--playtest-starlight-map"
        ),
        new(
            PlaytestScenarioId.StarlightMapRestored,
            "--playtest-starlight-map-restored"
        ),
        new(
            PlaytestScenarioId.StarlightPanel,
            "--playtest-starlight-panel"
        ),
        new(
            PlaytestScenarioId.StarlightRestored,
            "--playtest-starlight-restored"
        ),
        new(
            PlaytestScenarioId.StarlightRestoredEnglish,
            "--playtest-starlight-restored-en"
        ),
        new(
            PlaytestScenarioId.QualityCrafting,
            "--playtest-quality-crafting"
        ),
        new(
            PlaytestScenarioId.QualityBackpackEnglish,
            "--playtest-quality-backpack-en"
        ),
        new(
            PlaytestScenarioId.QualityBackpack,
            "--playtest-quality-backpack"
        ),
        new(PlaytestScenarioId.Quality, "--playtest-quality"),
        new(PlaytestScenarioId.Farm, "--playtest-farm")
    ];

    private readonly IReadOnlyDictionary<PlaytestScenarioId, Action> _setups;

    public PlaytestScenarioRegistry(
        IReadOnlyDictionary<PlaytestScenarioId, Action> setups
    )
    {
        ArgumentNullException.ThrowIfNull(setups);

        var knownIds = Definitions
            .Select(definition => definition.Id)
            .ToHashSet();
        var missingIds = knownIds
            .Where(id => !setups.ContainsKey(id))
            .ToArray();
        var unknownIds = setups.Keys
            .Where(id => !knownIds.Contains(id))
            .ToArray();
        if (missingIds.Length > 0 || unknownIds.Length > 0)
        {
            throw new ArgumentException(
                "Playtest setup mappings must exactly match the scenario catalog.",
                nameof(setups)
            );
        }

        _setups = setups;
    }

    public static IReadOnlyList<string> KnownFlags { get; } =
        Array.AsReadOnly(
            Definitions
                .Select(definition => definition.Flag)
                .ToArray()
        );

    public static PlaytestScenarioId? ResolveScenario(
        IEnumerable<string> userArgs
    )
    {
        ArgumentNullException.ThrowIfNull(userArgs);

        var arguments = userArgs.ToHashSet(StringComparer.Ordinal);
        foreach (var definition in Definitions)
        {
            if (arguments.Contains(definition.Flag))
            {
                return definition.Id;
            }
        }

        return null;
    }

    public Action? ResolveSetup(IEnumerable<string> userArgs)
    {
        var scenario = ResolveScenario(userArgs);
        if (scenario is null)
        {
            return null;
        }

        return _setups[scenario.Value];
    }
}
