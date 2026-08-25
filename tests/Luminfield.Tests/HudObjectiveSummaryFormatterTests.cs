using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class HudObjectiveSummaryFormatterTests
{
    [Fact]
    public void SingleObjectiveKeepsVisibleAndFullTextAligned()
    {
        var summary = HudObjectiveSummaryFormatter.Create("✦ Harvest starbuds");

        Assert.Equal("✦ Harvest starbuds", summary.VisibleText);
        Assert.Equal("✦ Harvest starbuds", summary.FullText);
        Assert.False(summary.HasHiddenLines);
        Assert.Equal(0, summary.HiddenLineCount);
    }

    [Fact]
    public void LongSingleObjectiveUsesStableVisibleBudget()
    {
        var summary = HudObjectiveSummaryFormatter.Create(
            "✦ Find Mira beside the moonlit greenhouse before the morning route changes"
        );

        Assert.EndsWith("…", summary.VisibleText, StringComparison.Ordinal);
        Assert.True(
            summary.VisibleText.Length <=
                HudObjectiveSummaryFormatter.MaxVisibleCharacters
        );
        Assert.Equal(
            "✦ Find Mira beside the moonlit greenhouse before the morning route changes",
            summary.FullText
        );
    }

    [Fact]
    public void TwoObjectivesBecomeOneCompactHudLine()
    {
        var summary = HudObjectiveSummaryFormatter.Create(
            "✦ Talk to Mira\n◇ Daily: Bring lumenwood (1/3)"
        );

        Assert.Equal(
            "✦ Talk to Mira · ◇ Daily: Bring lumenwood (1/3)",
            summary.VisibleText
        );
        Assert.Equal(
            "✦ Talk to Mira\n◇ Daily: Bring lumenwood (1/3)",
            summary.FullText
        );
        Assert.False(summary.HasHiddenLines);
    }

    [Fact]
    public void TwoLongObjectivesKeepWithinVisibleBudget()
    {
        var summary = HudObjectiveSummaryFormatter.Create(
            "✦ Find Mira beside the moonlit greenhouse\n◇ Daily: Deliver polished moonroots to the archive desk (12/18)"
        );

        Assert.Equal(
            "✦ Find Mira beside the… · ◇ Daily: Deliver polished…",
            summary.VisibleText
        );
        Assert.True(
            summary.VisibleText.Length <=
                HudObjectiveSummaryFormatter.MaxVisibleCharacters
        );
    }

    [Fact]
    public void ThreeOrMoreObjectivesKeepFirstLastAndCountMiddleLines()
    {
        var summary = HudObjectiveSummaryFormatter.Create(
            "✦ Harvest starbuds\n◇ Daily: Bring moonroot (2/4)\n✉ 2 unread Starlight Mail\n✦ Festival tomorrow"
        );

        Assert.Equal(
            "+2 · ✦ Harvest starbuds · ✦ Festival tomorrow",
            summary.VisibleText
        );
        Assert.Equal(
            "✦ Harvest starbuds\n◇ Daily: Bring moonroot (2/4)\n✉ 2 unread Starlight Mail\n✦ Festival tomorrow",
            summary.FullText
        );
        Assert.True(summary.HasHiddenLines);
        Assert.Equal(2, summary.HiddenLineCount);
    }

    [Fact]
    public void HiddenCountBadgeSurvivesLongFirstAndLastLines()
    {
        var summary = HudObjectiveSummaryFormatter.Create(
            "✦ Find Mira beside the moonlit greenhouse\n◇ Daily: Bring moonroot (2/4)\n✉ 2 unread Starlight Mail\n✦ Starharvest Market opens beside the restored plaza tomorrow"
        );

        Assert.Equal(
            "+2 · ✦ Find Mira beside… · ✦ Starharvest Market ope…",
            summary.VisibleText
        );
        Assert.StartsWith("+2 · ", summary.VisibleText, StringComparison.Ordinal);
        Assert.True(
            summary.VisibleText.Length <=
                HudObjectiveSummaryFormatter.MaxVisibleCharacters
        );
    }

    [Fact]
    public void BlankLinesAreIgnoredBeforeSummarizing()
    {
        var summary = HudObjectiveSummaryFormatter.Create(
            "  ✦ Harvest starbuds  \n\n  ✉ Mail waiting  "
        );

        Assert.Equal("✦ Harvest starbuds · ✉ Mail waiting", summary.VisibleText);
        Assert.Equal("✦ Harvest starbuds\n✉ Mail waiting", summary.FullText);
    }

    [Fact]
    public void MinimapStateStartsExpandedWithAllMarkers()
    {
        var state = HudMinimapState.Default;

        Assert.False(state.Collapsed);
        Assert.Equal(HudMinimapMarkerFilter.All, state.MarkerFilter);
    }

    [Fact]
    public void MinimapStateTogglesCollapsedWithoutChangingFilter()
    {
        var state = HudMinimapState.Default.NextFilter();

        var collapsed = state.ToggleCollapsed();

        Assert.True(collapsed.Collapsed);
        Assert.Equal(HudMinimapMarkerFilter.Landmarks, collapsed.MarkerFilter);
    }

    [Fact]
    public void MinimapStateCyclesMarkerFilters()
    {
        var landmarks = HudMinimapState.Default.NextFilter();
        var forage = landmarks.NextFilter();
        var all = forage.NextFilter();

        Assert.Equal(HudMinimapMarkerFilter.Landmarks, landmarks.MarkerFilter);
        Assert.Equal(HudMinimapMarkerFilter.Forage, forage.MarkerFilter);
        Assert.Equal(HudMinimapMarkerFilter.All, all.MarkerFilter);
    }

    [Fact]
    public void MinimapStartupArgumentsCanOpenCollapsedForageView()
    {
        var state = HudMinimapState.FromArguments(
            [
                HudMinimapState.CollapsedFlag,
                HudMinimapState.ForageFilterFlag
            ]
        );

        Assert.True(state.Collapsed);
        Assert.Equal(HudMinimapMarkerFilter.Forage, state.MarkerFilter);
    }

    [Fact]
    public void MinimapStartupArgumentsUseLastFilterFlag()
    {
        var state = HudMinimapState.FromArguments(
            [
                HudMinimapState.ForageFilterFlag,
                HudMinimapState.LandmarksFilterFlag
            ]
        );

        Assert.False(state.Collapsed);
        Assert.Equal(HudMinimapMarkerFilter.Landmarks, state.MarkerFilter);
    }

    [Fact]
    public void ObjectiveDetailsStartupFlagIsExplicit()
    {
        Assert.False(HudObjectiveDetailsStartup.ShouldOpen([]));

        Assert.True(
            HudObjectiveDetailsStartup.ShouldOpen(
                [HudObjectiveDetailsStartup.OpenFlag]
            )
        );
    }
}
