using System.Text.Json;
using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class ImmediateFeedbackAcceptanceGalleryTests
{
    private static readonly ImmediateFeedbackDomain[] ExpectedDomains =
    [
        ImmediateFeedbackDomain.Tool,
        ImmediateFeedbackDomain.Watering,
        ImmediateFeedbackDomain.Harvest,
        ImmediateFeedbackDomain.Pickup,
        ImmediateFeedbackDomain.Processing,
        ImmediateFeedbackDomain.Fishing,
        ImmediateFeedbackDomain.Damage,
        ImmediateFeedbackDomain.Dodge,
        ImmediateFeedbackDomain.Reward
    ];

    private static readonly ImmediateFeedbackOutcome[] ExpectedOutcomes =
    [
        ImmediateFeedbackOutcome.Success,
        ImmediateFeedbackOutcome.ResourceBlocked,
        ImmediateFeedbackOutcome.ToolMismatch,
        ImmediateFeedbackOutcome.Failure
    ];

    private static readonly ImmediateFeedbackAcceptanceMotionMode[] ExpectedMotionModes =
    [
        ImmediateFeedbackAcceptanceMotionMode.Standard,
        ImmediateFeedbackAcceptanceMotionMode.Reduced
    ];

    [Fact]
    public void GalleryScenariosCoverEveryDomainOutcomeAndMotionMode()
    {
        var scenarios = ImmediateFeedbackAcceptanceGallery.Scenarios;

        Assert.Equal(
            ImmediateFeedbackAcceptanceGallery.ExpectedScenarioCount,
            scenarios.Count
        );
        Assert.Equal(72, scenarios.Count);

        var combinations = scenarios
            .Select(scenario => (
                scenario.Domain,
                scenario.ExpectedOutcome,
                scenario.Motion
            ))
            .Distinct()
            .ToArray();
        Assert.Equal(scenarios.Count, combinations.Length);

        foreach (var domain in ExpectedDomains)
        {
            foreach (var outcome in ExpectedOutcomes)
            {
                foreach (var motion in ExpectedMotionModes)
                {
                    Assert.Contains(
                        scenarios,
                        scenario =>
                            scenario.Domain == domain &&
                            scenario.ExpectedOutcome == outcome &&
                            scenario.Motion == motion
                    );
                }
            }
        }
    }

    [Fact]
    public void StartupArgumentsSelectOneKnownDomainWithoutChangingFallback()
    {
        Assert.False(ImmediateFeedbackAcceptanceGallery.ShouldOpen([]));
        Assert.True(ImmediateFeedbackAcceptanceGallery.ShouldOpen(
            [ImmediateFeedbackAcceptanceGallery.OpenFlag]
        ));
        Assert.Equal(
            "watering",
            ImmediateFeedbackAcceptanceGallery.SelectedDomain(
                [$"{ImmediateFeedbackAcceptanceGallery.DomainFlagPrefix}watering"]
            )
        );
        Assert.Null(ImmediateFeedbackAcceptanceGallery.SelectedDomain(
            [$"{ImmediateFeedbackAcceptanceGallery.DomainFlagPrefix}unknown"]
        ));
        Assert.Equal(
            8,
            ImmediateFeedbackAcceptanceGallery.BuildTiles(
                domainId: "watering"
            ).Count
        );
        Assert.Equal(
            72,
            ImmediateFeedbackAcceptanceGallery.BuildTiles(
                domainId: "unknown"
            ).Count
        );
    }

    [Fact]
    public void GallerySceneIdsAreStableUniqueAndAuditScoped()
    {
        var scenarios = ImmediateFeedbackAcceptanceGallery.Scenarios;
        var sceneIds = scenarios
            .Select(scenario => scenario.SceneId)
            .ToArray();

        Assert.Equal(sceneIds.Length, sceneIds.Distinct().Count());
        Assert.All(sceneIds, sceneId =>
        {
            Assert.StartsWith(
                ImmediateFeedbackAcceptanceGallery.SceneIdPrefix,
                sceneId
            );
            Assert.DoesNotContain(' ', sceneId);
            Assert.DoesNotContain('_', sceneId);
        });
        Assert.Contains(
            "feel.acceptance.processor.resource-blocked.reduced",
            sceneIds
        );
        Assert.Contains("feel.acceptance.damage.success.standard", sceneIds);
        Assert.Contains("feel.acceptance.reward.tool-mismatch.reduced", sceneIds);
    }

    [Fact]
    public void GalleryBuildsPresenterBackedTilesForEveryScenario()
    {
        var tiles = ImmediateFeedbackAcceptanceGallery.BuildTiles();

        Assert.Equal(ImmediateFeedbackAcceptanceGallery.Scenarios.Count, tiles.Count);
        foreach (var tile in tiles)
        {
            var scenario = tile.Scenario;
            var cue = tile.Cue;

            Assert.Equal(scenario.Domain, cue.Domain);
            Assert.Equal(scenario.ExpectedOutcome, cue.Outcome);
            Assert.Equal(scenario.MessageKey, cue.MessageKey);
            Assert.Equal(scenario.ExpectsReducedEffects, cue.ReducedEffects);
            Assert.True(cue.DurationSeconds > 0);
            Assert.True(cue.BorderWidth >= 1);

            if (scenario.ExpectsReducedEffects)
            {
                Assert.Equal(0f, cue.ShakePixels);
                Assert.Equal(1f, cue.PulseScale);
            }
            else
            {
                Assert.True(cue.PulseScale > 1f);
            }
        }
    }

    [Fact]
    public void StandardGalleryTilesKeepExpectedShakeForFeedbackState()
    {
        var standardTiles = ImmediateFeedbackAcceptanceGallery
            .BuildTiles()
            .Where(tile =>
                tile.Scenario.Motion ==
                ImmediateFeedbackAcceptanceMotionMode.Standard
            );

        foreach (var tile in standardTiles)
        {
            var scenario = tile.Scenario;
            var cue = tile.Cue;
            if (scenario.Domain == ImmediateFeedbackDomain.Damage)
            {
                Assert.True(cue.ShakePixels > 0);
                continue;
            }

            if (scenario.ExpectedOutcome == ImmediateFeedbackOutcome.Success)
            {
                Assert.Equal(0f, cue.ShakePixels);
                continue;
            }

            Assert.True(cue.ShakePixels > 0);
        }
    }

    [Fact]
    public void ToolMismatchTilesUseTargetPreviewWithoutLosingMessageKeys()
    {
        var tiles = ImmediateFeedbackAcceptanceGallery.BuildTiles()
            .Where(tile =>
                tile.Scenario.ExpectedOutcome ==
                ImmediateFeedbackOutcome.ToolMismatch
            )
            .ToArray();

        Assert.Equal(
            ImmediateFeedbackAcceptanceGallery.DomainCount *
            ImmediateFeedbackAcceptanceGallery.MotionModeCount,
            tiles.Length
        );
        Assert.All(tiles, tile =>
        {
            Assert.True(tile.Scenario.UsesToolMismatchPreview);
            Assert.Equal(ImmediateFeedbackOutcome.ToolMismatch, tile.Cue.Outcome);
            Assert.Equal(tile.Scenario.MessageKey, tile.Cue.MessageKey);
        });
    }

    [Fact]
    public void GalleryLocalizationKeysExistInEnglishAndChinese()
    {
        var english = LocaleKeys("en.json");
        var chinese = LocaleKeys("zh_CN.json");

        Assert.Equal(
            ImmediateFeedbackAcceptanceGallery.RequiredLocalizationKeys.Count,
            ImmediateFeedbackAcceptanceGallery.RequiredLocalizationKeys
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.All(ImmediateFeedbackAcceptanceGallery.RequiredLocalizationKeys, key =>
        {
            Assert.Contains(key, english);
            Assert.Contains(key, chinese);
        });
    }

    [Fact]
    public void GalleryCanRenderEveryTileThroughLocaleService()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(LocaleService.SimplifiedChinese, ReadLocale("zh_CN.json"));
        var tiles = ImmediateFeedbackAcceptanceGallery.BuildTiles();

        foreach (var localeId in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(localeId);
            Assert.All(tiles, tile =>
            {
                var text = locale.Tr(tile.Cue.MessageKey);
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.DoesNotContain('[', text);
            });
        }
    }

    private static HashSet<string> LocaleKeys(string name)
    {
        using var document = JsonDocument.Parse(ReadLocale(name));
        return document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string ReadLocale(string name) =>
        File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "localization", name)
        );
}
