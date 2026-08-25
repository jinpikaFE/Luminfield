using Xunit;

namespace Luminfield.Tests;

public sealed class MainExperienceIntegrationArchitectureTests
{
    private static readonly string[] ExperienceMethodDeclarations =
    [
        "private void OpenPause()",
        "private void ClosePause()",
        "private void OpenOnboardingPlan()",
        "private void CloseOnboardingPlan()",
        "private void OpenMorningBriefing()",
        "private void CloseMorningBriefing()",
        "private void OnMorningBriefingNavigationRequested(",
        "private bool TryCloseExperienceOverlay(",
        "private bool TryOpenExperienceOverlay("
    ];

    private static readonly string[] FeedbackMethodDeclarations =
    [
        "private void UpdateAudioContext()",
        "private void ShowImmediateFeedback(",
        "private void ShowRewardFeedback(ActionResult result)",
        "private void ShowRewardFeedback(string _)",
        "private static ImmediateFeedbackDomain FarmFeedbackDomain("
    ];

    private static readonly string[] OverlayInputCloseSequence =
    [
        "_settingsOverlay is not null",
        "_fishingMinigameOverlay is not null",
        "_fishingGearOverlay is not null",
        "_deepMineOverlay is not null",
        "_starGateOverlay is not null",
        "_stellarResonanceOverlay is not null",
        "_mainStoryEndingOverlay is not null",
        "_compendiumOverlay is not null",
        "_festivalShowcaseOverlay is not null",
        "_festivalShopOverlay is not null",
        "_gleamrisePlantingOverlay is not null",
        "_gleamriseSeedExchangeOverlay is not null",
        "_shopOverlay is not null",
        "_processorOverlay is not null",
        "_shippingOverlay is not null",
        "_commissionOverlay is not null",
        "_constructionOverlay is not null",
        "_livestockAutomationOverlay is not null",
        "_mailOverlay is not null",
        "_postDeliveryOverlay is not null",
        "_starfallWatchOverlay is not null",
        "_starlightOverlay is not null",
        "_kitchenOverlay is not null",
        "_ingredientPantryOverlay is not null",
        "_cookedDishOverlay is not null",
        "_craftingOverlay is not null",
        "_farmingSpecializationOverlay is not null",
        "_storageOverlay is not null",
        "_nightlySummaryOverlay is not null",
        "_backpackOverlay is not null",
        "_fishingCollectionOverlay is not null",
        "_fishingDonationOverlay is not null",
        "_gleamriseSeasonOverlay is not null"
    ];

    private static readonly string[] RouteGuidanceMethodDeclarations =
    [
        "private void EnsureRouteGuidanceHud()",
        "private void OpenRouteGuidance()",
        "private void CloseRouteGuidance()",
        "private void OnRouteGuidanceSelected(string routeId)",
        "private void OnRouteGuidanceCleared()",
        "private void StartRouteGuidanceJourney(WorldBiome destination)",
        "private void RefreshRouteGuidanceProjection()",
        "private void ResetRouteGuidance()"
    ];

    private static readonly string[] FestivalMethodDeclarations =
    [
        "private void TryEnterStarharvestMarket()",
        "private void TryEnterGleamrisePlantingFestival()",
        "private void TryEnterLongnightLanternFeast()",
        "private void TryEnterFireflyTide()",
        "private void OpenFestivalShowcase()",
        "private void OpenFestivalShop()",
        "private void OpenGleamrisePlanting()",
        "private void OpenGleamriseSeedExchange()",
        "private void OpenLongnightFeast(",
        "private void OpenLongnightStall()",
        "private void OpenFireflyTideActivity(",
        "private void OpenFireflyTideShop()"
    ];

    private static readonly string[] PlayerServiceMethodDeclarations =
    [
        "private void OpenShop()",
        "private void OpenShop(ShopOverlayMode mode)",
        "private void OpenProcessor(",
        "private void OpenShipping()",
        "private void OpenCommissionBoard()",
        "private void OpenWeeklyCommissionBoard()",
        "private void OpenStarlightMail()",
        "private void OpenStarlightPedestal()",
        "private void OpenBackpack()",
        "private void OpenCrafting()",
        "private void OpenStorage("
    ];

    private static readonly string[] PauseChildOpenMethods =
    [
        "OpenOnboardingPlan",
        "OpenMorningBriefing",
        "OpenRouteGuidance",
        "OpenGleamriseSeasonGoals",
        "OpenFishingCollection",
        "OpenFishingGear",
        "OpenStellarResonance",
        "OpenSettings"
    ];

    private static readonly string[] PauseChildCloseMethods =
    [
        "CloseOnboardingPlan",
        "CloseMorningBriefing",
        "CloseRouteGuidance",
        "CloseGleamriseSeasonGoals",
        "CloseFishingCollection",
        "CloseFishingGear",
        "CloseStellarResonance",
        "CloseSettings"
    ];

    [Fact]
    public void ExperienceOrchestrationStaysOutOfSharedMainHotspot()
    {
        var repositoryRoot = FindRepositoryRoot();
        var mainSource = File.ReadAllText(
            Path.Combine(repositoryRoot, "src", "Game", "Main.cs")
        );
        var experienceSource = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "Game",
                "Main.ExperienceIntegration.cs"
            )
        );

        foreach (var declaration in ExperienceMethodDeclarations)
        {
            Assert.DoesNotContain(declaration, mainSource);
            Assert.Contains(declaration, experienceSource);
        }
    }

    [Fact]
    public void ExperienceIntegrationKeepsOnePartialMainEntryPoint()
    {
        var source = File.ReadAllText(
            Path.Combine(
                FindRepositoryRoot(),
                "src",
                "Game",
                "Main.ExperienceIntegration.cs"
            )
        );

        Assert.Contains("public sealed partial class Main : Node", source);
        Assert.DoesNotContain("new GameSession(", source);
        Assert.DoesNotContain("new SaveService(", source);
        foreach (var declaration in FeedbackMethodDeclarations)
        {
            Assert.DoesNotContain(declaration, source);
        }
    }

    [Fact]
    public void PauseChildrenShareOneFocusRestorationLifecycle()
    {
        var repositoryRoot = FindRepositoryRoot();
        var experienceSource = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "Game",
                "Main.ExperienceIntegration.cs"
            )
        );
        var mainSource = File.ReadAllText(
            Path.Combine(repositoryRoot, "src", "Game", "Main.cs")
        );
        var routeSource = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "Game",
                "Main.RouteGuidanceIntegration.cs"
            )
        );
        var allSources = string.Concat(
            experienceSource,
            mainSource,
            routeSource
        );

        foreach (var openMethod in PauseChildOpenMethods)
        {
            Assert.Contains(
                $"OpenPauseChild(\n                {openMethod},",
                experienceSource
            );
        }

        foreach (var closeMethod in PauseChildCloseMethods)
        {
            var closeBody = SourceBetween(
                allSources,
                $"private void {closeMethod}()",
                "\n    }"
            );
            Assert.Contains("RestorePauseAfterChild()", closeBody);
        }

        AssertContainsInOrder(
            experienceSource,
            [
                "openChild();",
                "if (!isChildOpen())",
                "RestorePauseAfterChild();"
            ]
        );
        Assert.DoesNotContain(
            "ClosePause();\n            Open",
            experienceSource
        );
    }

    [Fact]
    public void FeedbackIntegrationStaysOutOfExperienceHotspot()
    {
        var repositoryRoot = FindRepositoryRoot();
        var experienceSource = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "Game",
                "Main.ExperienceIntegration.cs"
            )
        );
        var feedbackSource = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "Game",
                "Main.FeedbackIntegration.cs"
            )
        );

        foreach (var declaration in FeedbackMethodDeclarations)
        {
            Assert.DoesNotContain(declaration, experienceSource);
            Assert.Contains(declaration, feedbackSource);
        }
    }

    [Fact]
    public void FeedbackIntegrationKeepsOnePartialMainEntryPoint()
    {
        var source = File.ReadAllText(
            Path.Combine(
                FindRepositoryRoot(),
                "src",
                "Game",
                "Main.FeedbackIntegration.cs"
            )
        );

        Assert.Contains("public sealed partial class Main : Node", source);
        Assert.DoesNotContain("new GameSession(", source);
        Assert.DoesNotContain("new SaveService(", source);
        Assert.Contains("ImmediateFeedbackAudio.SoundFor(cue)", source);
        Assert.DoesNotContain("_audio.Play(PixelSound.Error)", source);
        Assert.Contains(
            "Region: _session.PlayerLocationId == PlayerLocationIds.World",
            source
        );
    }

    [Fact]
    public void OverlayInputCloseDispatchStaysOutOfSharedMainHotspot()
    {
        var repositoryRoot = FindRepositoryRoot();
        var mainSource = File.ReadAllText(
            Path.Combine(repositoryRoot, "src", "Game", "Main.cs")
        );
        var overlayInputSource = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "Game",
                "Main.OverlayInputIntegration.cs"
            )
        );
        var closeDispatchSegment = SourceBetween(
            mainSource,
            "if (TryCloseExperienceOverlay(@event, overlayCancelPressed))",
            "if (@event.IsActionPressed(InputSetup.Backpack) && !IsInputBlocked)"
        );

        Assert.DoesNotContain(
            "private bool TryClosePlayerOverlay(",
            mainSource
        );
        Assert.Contains(
            "private bool TryClosePlayerOverlay(",
            overlayInputSource
        );
        Assert.Contains(
            "if (TryClosePlayerOverlay(@event, overlayCancelPressed))",
            closeDispatchSegment
        );

        foreach (var marker in OverlayInputCloseSequence)
        {
            Assert.DoesNotContain(marker, closeDispatchSegment);
        }
    }

    [Fact]
    public void OverlayInputIntegrationKeepsOnePartialMainEntryPoint()
    {
        var source = File.ReadAllText(
            Path.Combine(
                FindRepositoryRoot(),
                "src",
                "Game",
                "Main.OverlayInputIntegration.cs"
            )
        );

        Assert.Contains("public sealed partial class Main : Node", source);
        Assert.DoesNotContain("new GameSession(", source);
        Assert.DoesNotContain("new SaveService(", source);
        AssertContainsInOrder(source, OverlayInputCloseSequence);
        Assert.Equal(
            OverlayInputCloseSequence.Length,
            CountOccurrences(source, "GetViewport().SetInputAsHandled();")
        );
        Assert.Equal(
            OverlayInputCloseSequence.Length,
            CountOccurrences(source, "return true;")
        );
        Assert.Contains("return false;", source);
    }

    [Fact]
    public void RouteGuidanceOwnsEphemeralSelectionOutsideSharedMain()
    {
        var repositoryRoot = FindRepositoryRoot();
        var mainSource = File.ReadAllText(
            Path.Combine(repositoryRoot, "src", "Game", "Main.cs")
        );
        var routeSource = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "Game",
                "Main.RouteGuidanceIntegration.cs"
            )
        );

        foreach (var declaration in RouteGuidanceMethodDeclarations)
        {
            Assert.DoesNotContain(declaration, mainSource);
            Assert.Contains(declaration, routeSource);
        }

        Assert.Contains(
            "WorldNavigationRouteSelection _routeGuidanceSelection",
            routeSource
        );
        Assert.Contains(
            "RouteGuidanceOriginPresenter.Resolve(",
            routeSource
        );
        Assert.Contains(
            "WorldNavigationRouteSelection.AvailableFrom(",
            routeSource
        );
        Assert.Contains(
            "_routeGuidanceOverlay.SetSelectedRoute(selectedRoute)",
            routeSource
        );
        Assert.Contains("_routeGuidanceSelection.Select(routeId)", routeSource);
        Assert.Contains(
            "_routeGuidanceSelection.TryHandoffToLocation",
            routeSource
        );
        Assert.Contains("_routeGuidanceHud?.SetGuidanceVisible", routeSource);
        Assert.Contains("_session.CanOccupyNavigationCell", routeSource);
        Assert.Contains("_hud?.SetNavigationProgress", routeSource);
        Assert.DoesNotContain("SaveNow(", routeSource);
        Assert.DoesNotContain("new GameSession(", routeSource);
        Assert.DoesNotContain("new SaveService(", routeSource);
    }

    [Fact]
    public void FestivalOrchestrationStaysOutOfSharedMainHotspot()
    {
        var repositoryRoot = FindRepositoryRoot();
        var mainSource = File.ReadAllText(
            Path.Combine(repositoryRoot, "src", "Game", "Main.cs")
        );
        var festivalSource = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "Game",
                "Main.FestivalIntegration.cs"
            )
        );

        foreach (var declaration in FestivalMethodDeclarations)
        {
            Assert.DoesNotContain(declaration, mainSource);
            Assert.Contains(declaration, festivalSource);
        }

        Assert.DoesNotContain("new GameSession(", festivalSource);
        Assert.DoesNotContain("new SaveService(", festivalSource);
    }

    [Fact]
    public void PlayerServiceOverlaysStayOutOfSharedMainHotspot()
    {
        var repositoryRoot = FindRepositoryRoot();
        var mainSource = File.ReadAllText(
            Path.Combine(repositoryRoot, "src", "Game", "Main.cs")
        );
        var servicesSource = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "Game",
                "Main.PlayerServicesIntegration.cs"
            )
        );

        foreach (var declaration in PlayerServiceMethodDeclarations)
        {
            Assert.DoesNotContain(declaration, mainSource);
            Assert.Contains(declaration, servicesSource);
        }

        Assert.DoesNotContain("new GameSession(", servicesSource);
        Assert.DoesNotContain("new SaveService(", servicesSource);
    }

    private static void AssertContainsInOrder(
        string source,
        IReadOnlyList<string> markers
    )
    {
        var searchStart = 0;
        foreach (var marker in markers)
        {
            var nextIndex = source.IndexOf(
                marker,
                searchStart,
                StringComparison.Ordinal
            );
            Assert.True(
                nextIndex >= 0,
                $"Expected to find '{marker}' after index {searchStart}."
            );
            searchStart = nextIndex + marker.Length;
        }
    }

    private static int CountOccurrences(string source, string marker)
    {
        var count = 0;
        var searchStart = 0;
        while (searchStart < source.Length)
        {
            var nextIndex = source.IndexOf(
                marker,
                searchStart,
                StringComparison.Ordinal
            );
            if (nextIndex < 0)
            {
                break;
            }

            count++;
            searchStart = nextIndex + marker.Length;
        }

        return count;
    }

    private static string SourceBetween(
        string source,
        string startMarker,
        string endMarker
    )
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected to find '{startMarker}'.");
        var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end >= 0, $"Expected to find '{endMarker}'.");
        return source[start..end];
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "project.godot")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the Luminfield repository root."
        );
    }
}
