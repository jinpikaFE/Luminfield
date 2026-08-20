using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class PlaytestScenarioRegistryTests
{
    private static readonly (
        string Flag,
        PlaytestScenarioId Scenario
    )[] ExpectedScenarios =
    [
        ("--playtest-door", PlaytestScenarioId.Door),
        ("--playtest-cottage", PlaytestScenarioId.Cottage),
        (
            "--playtest-cottage-upgrade-ready",
            PlaytestScenarioId.CottageUpgradeReady
        ),
        (
            "--playtest-cottage-upgrade-in-progress",
            PlaytestScenarioId.CottageUpgradeInProgress
        ),
        (
            "--playtest-cottage-upgrade-completed",
            PlaytestScenarioId.CottageUpgradeCompleted
        ),
        ("--playtest-crops", PlaytestScenarioId.Crops),
        (
            "--playtest-gleamrise-crops",
            PlaytestScenarioId.GleamriseCrops
        ),
        ("--playtest-economy", PlaytestScenarioId.Economy),
        ("--playtest-processor", PlaytestScenarioId.Processor),
        (
            "--playtest-multi-processor",
            PlaytestScenarioId.MultiProcessorBatch
        ),
        ("--playtest-archive-gift", PlaytestScenarioId.ArchiveGift),
        ("--playtest-archive", PlaytestScenarioId.Archive),
        ("--playtest-archive-door", PlaytestScenarioId.ArchiveDoor),
        (
            "--playtest-liora-event-one",
            PlaytestScenarioId.LioraEventOne
        ),
        (
            "--playtest-liora-event-two",
            PlaytestScenarioId.LioraEventTwo
        ),
        (
            "--playtest-tavi-event-one",
            PlaytestScenarioId.TaviEventOne
        ),
        (
            "--playtest-tavi-event-two",
            PlaytestScenarioId.TaviEventTwo
        ),
        (
            "--playtest-nemi-event-one",
            PlaytestScenarioId.NemiEventOne
        ),
        (
            "--playtest-nemi-event-two",
            PlaytestScenarioId.NemiEventTwo
        ),
        (
            "--playtest-kael-event-one",
            PlaytestScenarioId.KaelEventOne
        ),
        (
            "--playtest-kael-event-two",
            PlaytestScenarioId.KaelEventTwo
        ),
        (
            "--playtest-sela-event-one",
            PlaytestScenarioId.SelaEventOne
        ),
        (
            "--playtest-sela-event-two",
            PlaytestScenarioId.SelaEventTwo
        ),
        (
            "--playtest-orin-event-one",
            PlaytestScenarioId.OrinEventOne
        ),
        (
            "--playtest-orin-event-two",
            PlaytestScenarioId.OrinEventTwo
        ),
        (
            "--playtest-workshop-tavi",
            PlaytestScenarioId.WorkshopTavi
        ),
        ("--playtest-workshop", PlaytestScenarioId.Workshop),
        (
            "--playtest-workshop-door",
            PlaytestScenarioId.WorkshopDoor
        ),
        (
            "--playtest-tea-house-vessa",
            PlaytestScenarioId.TeaHouseVessa
        ),
        ("--playtest-tea-house", PlaytestScenarioId.TeaHouse),
        (
            "--playtest-tea-house-door",
            PlaytestScenarioId.TeaHouseDoor
        ),
        (
            "--playtest-emporium-orin",
            PlaytestScenarioId.EmporiumOrin
        ),
        ("--playtest-emporium", PlaytestScenarioId.Emporium),
        (
            "--playtest-emporium-door",
            PlaytestScenarioId.EmporiumDoor
        ),
        (
            "--playtest-emporium-rotation",
            PlaytestScenarioId.EmporiumRotation
        ),
        (
            "--playtest-emporium-restday-door",
            PlaytestScenarioId.EmporiumRestdayDoor
        ),
        (
            "--playtest-starlight-post-nemi",
            PlaytestScenarioId.StarlightPostNemi
        ),
        (
            "--playtest-starlight-post",
            PlaytestScenarioId.StarlightPost
        ),
        (
            "--playtest-starlight-post-wrong-tool",
            PlaytestScenarioId.StarlightPostWrongTool
        ),
        (
            "--playtest-starlight-post-door",
            PlaytestScenarioId.StarlightPostDoor
        ),
        (
            "--playtest-starfall-watch-kael",
            PlaytestScenarioId.StarfallWatchKael
        ),
        (
            "--playtest-starfall-watch",
            PlaytestScenarioId.StarfallWatch
        ),
        (
            "--playtest-starfall-watch-wrong-tool",
            PlaytestScenarioId.StarfallWatchWrongTool
        ),
        (
            "--playtest-starfall-watch-door",
            PlaytestScenarioId.StarfallWatchDoor
        ),
        (
            "--playtest-village-dialogue",
            PlaytestScenarioId.VillageDialogue
        ),
        (
            "--playtest-sela-dialogue",
            PlaytestScenarioId.SelaDialogue
        ),
        (
            "--playtest-village-expansion",
            PlaytestScenarioId.VillageExpansion
        ),
        (
            "--playtest-npc-pathfinding",
            PlaytestScenarioId.NpcPathfinding
        ),
        (
            "--playtest-village-restday-en",
            PlaytestScenarioId.VillageRestdayEnglish
        ),
        (
            "--playtest-village-rain-schedule",
            PlaytestScenarioId.VillageRainSchedule
        ),
        (
            "--playtest-village-rainveil-schedule",
            PlaytestScenarioId.VillageRainveilSchedule
        ),
        ("--playtest-village", PlaytestScenarioId.Village),
        ("--playtest-world", PlaytestScenarioId.World),
        ("--playtest-gate", PlaytestScenarioId.Gate),
        ("--playtest-backpack", PlaytestScenarioId.Backpack),
        ("--playtest-resource", PlaytestScenarioId.Resource),
        ("--playtest-fishing", PlaytestScenarioId.Fishing),
        (
            "--playtest-fishing-collection",
            PlaytestScenarioId.FishingCollection
        ),
        ("--playtest-target", PlaytestScenarioId.Target),
        ("--playtest-phase-a", PlaytestScenarioId.PhaseA),
        (
            "--playtest-phase-a-summary",
            PlaytestScenarioId.PhaseASummary
        ),
        ("--playtest-phase-a-rain", PlaytestScenarioId.PhaseARain),
        (
            "--playtest-resource-respawn",
            PlaytestScenarioId.ResourceRespawn
        ),
        ("--playtest-crafting", PlaytestScenarioId.Crafting),
        ("--playtest-placeables", PlaytestScenarioId.Placeables),
        (
            "--playtest-chest-placement",
            PlaytestScenarioId.ChestPlacement
        ),
        ("--playtest-storage", PlaytestScenarioId.Storage),
        (
            "--playtest-commission-offer",
            PlaytestScenarioId.CommissionOffer
        ),
        (
            "--playtest-commission-ready",
            PlaytestScenarioId.CommissionReady
        ),
        (
            "--playtest-commission-ready-en",
            PlaytestScenarioId.CommissionReadyEnglish
        ),
        (
            "--playtest-commission-map",
            PlaytestScenarioId.CommissionMap
        ),
        (
            "--playtest-weekly-commission-offer",
            PlaytestScenarioId.WeeklyCommissionOffer
        ),
        (
            "--playtest-weekly-commission-stage-ready",
            PlaytestScenarioId.WeeklyCommissionStageReady
        ),
        (
            "--playtest-weekly-commission-reward-ready",
            PlaytestScenarioId.WeeklyCommissionRewardReady
        ),
        (
            "--playtest-weekly-commission-map",
            PlaytestScenarioId.WeeklyCommissionMap
        ),
        (
            "--playtest-mailbox-unread",
            PlaytestScenarioId.MailboxUnread
        ),
        (
            "--playtest-mail-panel",
            PlaytestScenarioId.MailPanel
        ),
        (
            "--playtest-mail-reward",
            PlaytestScenarioId.MailReward
        ),
        ("--playtest-starlight-map", PlaytestScenarioId.StarlightMap),
        (
            "--playtest-starlight-map-restored",
            PlaytestScenarioId.StarlightMapRestored
        ),
        (
            "--playtest-starlight-panel",
            PlaytestScenarioId.StarlightPanel
        ),
        (
            "--playtest-starlight-restored",
            PlaytestScenarioId.StarlightRestored
        ),
        (
            "--playtest-starlight-restored-en",
            PlaytestScenarioId.StarlightRestoredEnglish
        ),
        (
            "--playtest-moonwater-starlight",
            PlaytestScenarioId.MoonwaterStarlight
        ),
        (
            "--playtest-quality-crafting",
            PlaytestScenarioId.QualityCrafting
        ),
        (
            "--playtest-quality-backpack-en",
            PlaytestScenarioId.QualityBackpackEnglish
        ),
        (
            "--playtest-quality-backpack",
            PlaytestScenarioId.QualityBackpack
        ),
        ("--playtest-quality", PlaytestScenarioId.Quality),
        (
            "--playtest-farming-specialization",
            PlaytestScenarioId.FarmingSpecialization
        ),
        (
            "--playtest-gleamrise-season",
            PlaytestScenarioId.GleamriseSeason
        ),
        (
            "--playtest-orchard-hives",
            PlaytestScenarioId.OrchardHives
        ),
        (
            "--playtest-starfeather-chickens",
            PlaytestScenarioId.StarfeatherChickens
        ),
        (
            "--playtest-gleamrise-festival",
            PlaytestScenarioId.GleamriseFestival
        ),
        ("--playtest-farm", PlaytestScenarioId.Farm)
    ];

    [Fact]
    public void KnownFlagsAreUniqueAndPreserveTheExistingCatalog()
    {
        var expectedFlags = ExpectedScenarios
            .Select(scenario => scenario.Flag)
            .ToArray();

        Assert.Equal(expectedFlags, PlaytestScenarioRegistry.KnownFlags);
        Assert.Equal(
            expectedFlags.Length,
            expectedFlags.Distinct(StringComparer.Ordinal).Count()
        );
        Assert.Equal(
            Enum.GetValues<PlaytestScenarioId>().Length,
            expectedFlags.Length
        );

        foreach (var expected in ExpectedScenarios)
        {
            Assert.Equal(
                expected.Scenario,
                PlaytestScenarioRegistry.ResolveScenario([expected.Flag])
            );
        }
    }

    [Fact]
    public void RegistrationOrderWinsWhenMultipleFlagsArePresent()
    {
        var scenario = PlaytestScenarioRegistry.ResolveScenario(
        [
            "--playtest-farm",
            "--playtest-quality",
            "--playtest-door"
        ]);

        Assert.Equal(PlaytestScenarioId.Door, scenario);
    }

    [Fact]
    public void MissingPlaytestFlagKeepsTheNormalStartupFallback()
    {
        var scenario = PlaytestScenarioRegistry.ResolveScenario(
        [
            "--capture-playtest=user://capture.png",
            "--unknown"
        ]);

        Assert.Null(scenario);
    }
}
