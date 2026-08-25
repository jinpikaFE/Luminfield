using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class HudInformationHierarchyAcceptanceTests
{
    private const string DenseObjectiveText =
        "✦ Find Mira beside the moonlit greenhouse before the morning route changes\n" +
        "◇ Daily: Deliver polished moonroots to the archive desk (12/18)\n" +
        "✉ 2 unread Starlight Mail\n" +
        "✦ Starharvest Market opens beside the restored plaza tomorrow";

    public static TheoryData<int, bool, bool, HudMinimapMarkerFilter> Matrix => new()
    {
        { 100, false, false, HudMinimapMarkerFilter.All },
        { 100, false, false, HudMinimapMarkerFilter.Landmarks },
        { 100, false, false, HudMinimapMarkerFilter.Forage },
        { 100, false, true, HudMinimapMarkerFilter.All },
        { 100, false, true, HudMinimapMarkerFilter.Landmarks },
        { 100, false, true, HudMinimapMarkerFilter.Forage },
        { 100, true, false, HudMinimapMarkerFilter.All },
        { 100, true, false, HudMinimapMarkerFilter.Landmarks },
        { 100, true, false, HudMinimapMarkerFilter.Forage },
        { 100, true, true, HudMinimapMarkerFilter.All },
        { 100, true, true, HudMinimapMarkerFilter.Landmarks },
        { 100, true, true, HudMinimapMarkerFilter.Forage },
        { 120, false, false, HudMinimapMarkerFilter.All },
        { 120, false, false, HudMinimapMarkerFilter.Landmarks },
        { 120, false, false, HudMinimapMarkerFilter.Forage },
        { 120, false, true, HudMinimapMarkerFilter.All },
        { 120, false, true, HudMinimapMarkerFilter.Landmarks },
        { 120, false, true, HudMinimapMarkerFilter.Forage },
        { 120, true, false, HudMinimapMarkerFilter.All },
        { 120, true, false, HudMinimapMarkerFilter.Landmarks },
        { 120, true, false, HudMinimapMarkerFilter.Forage },
        { 120, true, true, HudMinimapMarkerFilter.All },
        { 120, true, true, HudMinimapMarkerFilter.Landmarks },
        { 120, true, true, HudMinimapMarkerFilter.Forage }
    };

    [Theory]
    [MemberData(nameof(Matrix))]
    public void HudInformationHierarchyMatrixKeepsCoreSignalsVisible(
        int fontScalePercent,
        bool objectiveDetailsOpen,
        bool minimapCollapsed,
        HudMinimapMarkerFilter minimapFilter
    )
    {
        var settings = new AccessibilitySettings
        {
            FontScalePercent = fontScalePercent
        };
        settings.Normalize();

        var summary = HudObjectiveSummaryFormatter.Create(DenseObjectiveText);
        var minimapState = new HudMinimapState(
            minimapCollapsed,
            minimapFilter
        );

        Assert.Equal(fontScalePercent, settings.FontScalePercent);
        Assert.InRange(settings.TextScale, 1f, 1.2f);
        Assert.Equal(
            objectiveDetailsOpen,
            HudObjectiveDetailsStartup.ShouldOpen(
                objectiveDetailsOpen
                    ? [HudObjectiveDetailsStartup.OpenFlag]
                    : []
            )
        );
        Assert.True(
            summary.VisibleText.Length <=
                HudObjectiveSummaryFormatter.MaxVisibleCharacters
        );
        Assert.StartsWith("+2 · ", summary.VisibleText, StringComparison.Ordinal);
        Assert.DoesNotContain('\n', summary.VisibleText);
        Assert.Equal(DenseObjectiveText, summary.FullText);
        Assert.Equal(2, summary.HiddenLineCount);
        Assert.Equal(minimapCollapsed, minimapState.Collapsed);
        Assert.Equal(minimapFilter, minimapState.MarkerFilter);
    }

    [Fact]
    public void HudInformationHierarchyMatrixCoversEveryRequestedCombination()
    {
        var cases = Matrix
            .Select(row => (
                FontScalePercent: (int)row[0],
                ObjectiveDetailsOpen: (bool)row[1],
                MinimapCollapsed: (bool)row[2],
                MinimapFilter: (HudMinimapMarkerFilter)row[3]
            ))
            .ToArray();

        Assert.Equal(24, cases.Length);
        Assert.Equal([100, 120], cases.Select(row => row.FontScalePercent).Distinct().Order());
        Assert.Equal(
            [false, true],
            cases.Select(row => row.ObjectiveDetailsOpen).Distinct().Order()
        );
        Assert.Equal(
            [false, true],
            cases.Select(row => row.MinimapCollapsed).Distinct().Order()
        );
        Assert.Equal(
            [
                HudMinimapMarkerFilter.All,
                HudMinimapMarkerFilter.Landmarks,
                HudMinimapMarkerFilter.Forage
            ],
            cases.Select(row => row.MinimapFilter).Distinct().Order()
        );
    }
}
