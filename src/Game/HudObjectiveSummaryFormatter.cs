namespace Luminfield.Game;

public readonly record struct HudObjectiveSummary(
    string VisibleText,
    string FullText,
    int HiddenLineCount
)
{
    public bool HasHiddenLines => HiddenLineCount > 0;
}

public static class HudObjectiveSummaryFormatter
{
    public const int MaxVisibleCharacters = 52;
    private const int FirstLineBudget = 20;
    private const int TwoLineFirstBudget = 24;
    private const string Separator = " · ";
    private const string Ellipsis = "…";

    public static HudObjectiveSummary Create(string objectiveText)
    {
        var lines = objectiveText
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
        if (lines.Length == 0)
        {
            return new HudObjectiveSummary(string.Empty, string.Empty, 0);
        }

        var fullText = string.Join('\n', lines);
        if (lines.Length == 1)
        {
            return new HudObjectiveSummary(
                TrimToBudget(lines[0], MaxVisibleCharacters),
                fullText,
                0
            );
        }

        if (lines.Length == 2)
        {
            var visibleText = CompactWithTail(
                lines[0],
                lines[1],
                string.Empty,
                TwoLineFirstBudget
            );
            return new HudObjectiveSummary(
                visibleText,
                fullText,
                0
            );
        }

        var hiddenLineCount = lines.Length - 2;
        var hiddenBadge = $"+{hiddenLineCount}";
        var summaryText = CompactWithLeadingBadge(
            hiddenBadge,
            lines[0],
            lines[^1],
            FirstLineBudget
        );
        return new HudObjectiveSummary(
            summaryText,
            fullText,
            hiddenLineCount
        );
    }

    private static string CompactWithTail(
        string firstLine,
        string tailLine,
        string badge,
        int firstLineBudget
    )
    {
        var firstBudget = Math.Min(firstLineBudget, MaxVisibleCharacters);
        var first = TrimToBudget(firstLine, firstBudget);
        var tailBudget = MaxVisibleCharacters -
            first.Length -
            Separator.Length -
            badge.Length;
        var tail = TrimToBudget(tailLine, tailBudget);
        return $"{first}{Separator}{tail}{badge}";
    }

    private static string CompactWithLeadingBadge(
        string badge,
        string firstLine,
        string tailLine,
        int firstLineBudget
    )
    {
        var prefix = $"{badge}{Separator}";
        var firstBudget = Math.Min(
            firstLineBudget,
            MaxVisibleCharacters - prefix.Length - Separator.Length
        );
        var first = TrimToBudget(firstLine, firstBudget);
        var tailBudget = MaxVisibleCharacters -
            prefix.Length -
            first.Length -
            Separator.Length;
        var tail = TrimToBudget(tailLine, tailBudget);
        return $"{prefix}{first}{Separator}{tail}";
    }

    private static string TrimToBudget(string text, int maxCharacters)
    {
        if (maxCharacters <= 0)
        {
            return string.Empty;
        }

        if (text.Length <= maxCharacters)
        {
            return text;
        }

        if (maxCharacters == 1)
        {
            return Ellipsis;
        }

        return text[..(maxCharacters - 1)].TrimEnd() + Ellipsis;
    }
}

public static class HudInputNavigationContract
{
    public const string ObjectiveDetailsOpenAction =
        InputSetup.HudObjectiveDetails;
    public const string ObjectiveDetailsFocusedControlKey = "menu.back";
    public const string MinimapToggleAction = InputSetup.HudMinimapToggle;
    public const string MinimapFilterAction = InputSetup.HudMinimapFilter;

    public static IReadOnlyList<string> ObjectiveDetailsCloseActions { get; } =
    [
        InputSetup.HudObjectiveDetails,
        InputSetup.UiCancel,
        InputSetup.Pause
    ];
}

public enum HudMinimapMarkerFilter
{
    All,
    Landmarks,
    Forage
}

public readonly record struct HudMinimapState(
    bool Collapsed,
    HudMinimapMarkerFilter MarkerFilter
)
{
    public const string CollapsedFlag = "--ui-minimap-collapsed";
    public const string LandmarksFilterFlag = "--ui-minimap-filter-landmarks";
    public const string ForageFilterFlag = "--ui-minimap-filter-forage";

    public static HudMinimapState Default =>
        new(false, HudMinimapMarkerFilter.All);

    public HudMinimapState ToggleCollapsed() =>
        this with { Collapsed = !Collapsed };

    public HudMinimapState NextFilter()
    {
        return MarkerFilter switch
        {
            HudMinimapMarkerFilter.All =>
                this with { MarkerFilter = HudMinimapMarkerFilter.Landmarks },
            HudMinimapMarkerFilter.Landmarks =>
                this with { MarkerFilter = HudMinimapMarkerFilter.Forage },
            _ => this with { MarkerFilter = HudMinimapMarkerFilter.All }
        };
    }

    public static HudMinimapState FromArguments(IEnumerable<string> arguments)
    {
        var state = Default;
        foreach (var argument in arguments)
        {
            if (argument == CollapsedFlag)
            {
                state = state with { Collapsed = true };
            }
            else if (argument == LandmarksFilterFlag)
            {
                state = state with
                {
                    MarkerFilter = HudMinimapMarkerFilter.Landmarks
                };
            }
            else if (argument == ForageFilterFlag)
            {
                state = state with
                {
                    MarkerFilter = HudMinimapMarkerFilter.Forage
                };
            }
        }

        return state;
    }
}

public static class HudObjectiveDetailsStartup
{
    public const string OpenFlag = "--ui-objective-details-open";

    public static bool ShouldOpen(IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            if (argument == OpenFlag)
            {
                return true;
            }
        }

        return false;
    }
}
