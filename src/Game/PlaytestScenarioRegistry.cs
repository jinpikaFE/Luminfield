namespace Luminfield.Game;

public enum PlaytestScenarioId
{
    Door,
    Cottage,
    CottageUpgradeReady,
    CottageUpgradeInProgress,
    CottageUpgradeCompleted,
    Crops,
    GleamriseCrops,
    Economy,
    Processor,
    MultiProcessorBatch,
    ArchiveGift,
    Archive,
    ArchiveDoor,
    LioraEventOne,
    LioraEventTwo,
    TaviEventOne,
    TaviEventTwo,
    NemiEventOne,
    NemiEventTwo,
    KaelEventOne,
    KaelEventTwo,
    SelaEventOne,
    SelaEventTwo,
    OrinEventOne,
    OrinEventTwo,
    WorkshopTavi,
    Workshop,
    WorkshopDoor,
    TeaHouseVessa,
    TeaHouse,
    TeaHouseDoor,
    EmporiumOrin,
    Emporium,
    EmporiumDoor,
    EmporiumRotation,
    EmporiumRestdayDoor,
    StarlightPostNemi,
    StarlightPost,
    StarlightPostWrongTool,
    StarlightPostDoor,
    StarfallWatchKael,
    StarfallWatch,
    StarfallWatchWrongTool,
    StarfallWatchDoor,
    VillageDialogue,
    SelaDialogue,
    VillageExpansion,
    NpcPathfinding,
    VillageRestdayEnglish,
    VillageRainSchedule,
    VillageRainveilSchedule,
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
    WeeklyCommissionOffer,
    WeeklyCommissionStageReady,
    WeeklyCommissionRewardReady,
    WeeklyCommissionMap,
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
    OrchardHives,
    StarfeatherChickens,
    FarmingSpecialization,
    GleamriseSeason,
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
        new(
            PlaytestScenarioId.CottageUpgradeReady,
            "--playtest-cottage-upgrade-ready"
        ),
        new(
            PlaytestScenarioId.CottageUpgradeInProgress,
            "--playtest-cottage-upgrade-in-progress"
        ),
        new(
            PlaytestScenarioId.CottageUpgradeCompleted,
            "--playtest-cottage-upgrade-completed"
        ),
        new(PlaytestScenarioId.Crops, "--playtest-crops"),
        new(
            PlaytestScenarioId.GleamriseCrops,
            "--playtest-gleamrise-crops"
        ),
        new(PlaytestScenarioId.Economy, "--playtest-economy"),
        new(PlaytestScenarioId.Processor, "--playtest-processor"),
        new(
            PlaytestScenarioId.MultiProcessorBatch,
            "--playtest-multi-processor"
        ),
        new(PlaytestScenarioId.ArchiveGift, "--playtest-archive-gift"),
        new(PlaytestScenarioId.Archive, "--playtest-archive"),
        new(PlaytestScenarioId.ArchiveDoor, "--playtest-archive-door"),
        new(
            PlaytestScenarioId.LioraEventOne,
            "--playtest-liora-event-one"
        ),
        new(
            PlaytestScenarioId.LioraEventTwo,
            "--playtest-liora-event-two"
        ),
        new(
            PlaytestScenarioId.TaviEventOne,
            "--playtest-tavi-event-one"
        ),
        new(
            PlaytestScenarioId.TaviEventTwo,
            "--playtest-tavi-event-two"
        ),
        new(
            PlaytestScenarioId.NemiEventOne,
            "--playtest-nemi-event-one"
        ),
        new(
            PlaytestScenarioId.NemiEventTwo,
            "--playtest-nemi-event-two"
        ),
        new(
            PlaytestScenarioId.KaelEventOne,
            "--playtest-kael-event-one"
        ),
        new(
            PlaytestScenarioId.KaelEventTwo,
            "--playtest-kael-event-two"
        ),
        new(
            PlaytestScenarioId.SelaEventOne,
            "--playtest-sela-event-one"
        ),
        new(
            PlaytestScenarioId.SelaEventTwo,
            "--playtest-sela-event-two"
        ),
        new(
            PlaytestScenarioId.OrinEventOne,
            "--playtest-orin-event-one"
        ),
        new(
            PlaytestScenarioId.OrinEventTwo,
            "--playtest-orin-event-two"
        ),
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
            PlaytestScenarioId.TeaHouseVessa,
            "--playtest-tea-house-vessa"
        ),
        new(PlaytestScenarioId.TeaHouse, "--playtest-tea-house"),
        new(
            PlaytestScenarioId.TeaHouseDoor,
            "--playtest-tea-house-door"
        ),
        new(
            PlaytestScenarioId.EmporiumOrin,
            "--playtest-emporium-orin"
        ),
        new(PlaytestScenarioId.Emporium, "--playtest-emporium"),
        new(
            PlaytestScenarioId.EmporiumDoor,
            "--playtest-emporium-door"
        ),
        new(
            PlaytestScenarioId.EmporiumRotation,
            "--playtest-emporium-rotation"
        ),
        new(
            PlaytestScenarioId.EmporiumRestdayDoor,
            "--playtest-emporium-restday-door"
        ),
        new(
            PlaytestScenarioId.StarlightPostNemi,
            "--playtest-starlight-post-nemi"
        ),
        new(
            PlaytestScenarioId.StarlightPost,
            "--playtest-starlight-post"
        ),
        new(
            PlaytestScenarioId.StarlightPostWrongTool,
            "--playtest-starlight-post-wrong-tool"
        ),
        new(
            PlaytestScenarioId.StarlightPostDoor,
            "--playtest-starlight-post-door"
        ),
        new(
            PlaytestScenarioId.StarfallWatchKael,
            "--playtest-starfall-watch-kael"
        ),
        new(
            PlaytestScenarioId.StarfallWatch,
            "--playtest-starfall-watch"
        ),
        new(
            PlaytestScenarioId.StarfallWatchWrongTool,
            "--playtest-starfall-watch-wrong-tool"
        ),
        new(
            PlaytestScenarioId.StarfallWatchDoor,
            "--playtest-starfall-watch-door"
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
            PlaytestScenarioId.NpcPathfinding,
            "--playtest-npc-pathfinding"
        ),
        new(
            PlaytestScenarioId.VillageRestdayEnglish,
            "--playtest-village-restday-en"
        ),
        new(
            PlaytestScenarioId.VillageRainSchedule,
            "--playtest-village-rain-schedule"
        ),
        new(
            PlaytestScenarioId.VillageRainveilSchedule,
            "--playtest-village-rainveil-schedule"
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
            PlaytestScenarioId.WeeklyCommissionOffer,
            "--playtest-weekly-commission-offer"
        ),
        new(
            PlaytestScenarioId.WeeklyCommissionStageReady,
            "--playtest-weekly-commission-stage-ready"
        ),
        new(
            PlaytestScenarioId.WeeklyCommissionRewardReady,
            "--playtest-weekly-commission-reward-ready"
        ),
        new(
            PlaytestScenarioId.WeeklyCommissionMap,
            "--playtest-weekly-commission-map"
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
        new(
            PlaytestScenarioId.FarmingSpecialization,
            "--playtest-farming-specialization"
        ),
        new(
            PlaytestScenarioId.GleamriseSeason,
            "--playtest-gleamrise-season"
        ),
        new(
            PlaytestScenarioId.OrchardHives,
            "--playtest-orchard-hives"
        ),
        new(
            PlaytestScenarioId.StarfeatherChickens,
            "--playtest-starfeather-chickens"
        ),
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
