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
        ("--playtest-crops", PlaytestScenarioId.Crops),
        ("--playtest-economy", PlaytestScenarioId.Economy),
        ("--playtest-processor", PlaytestScenarioId.Processor),
        ("--playtest-archive-gift", PlaytestScenarioId.ArchiveGift),
        ("--playtest-archive", PlaytestScenarioId.Archive),
        ("--playtest-archive-door", PlaytestScenarioId.ArchiveDoor),
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
            "--playtest-village-restday-en",
            PlaytestScenarioId.VillageRestdayEnglish
        ),
        ("--playtest-village", PlaytestScenarioId.Village),
        ("--playtest-world", PlaytestScenarioId.World),
        ("--playtest-gate", PlaytestScenarioId.Gate),
        ("--playtest-backpack", PlaytestScenarioId.Backpack),
        ("--playtest-resource", PlaytestScenarioId.Resource),
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
