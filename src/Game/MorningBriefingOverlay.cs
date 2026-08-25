using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class MorningBriefingOverlay : FullScreenUi
{
    public const string NavigateLocalizationKey = "morning.briefing.navigate";

    public static IReadOnlyList<string> RequiredLocalizationKeys { get; } =
        MorningBriefingPresenter.RequiredLocalizationKeys
            .Append(NavigateLocalizationKey)
            .ToList();

    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly PanelContainer _summaryPanel;
    private readonly VBoxContainer _summaryList;
    private readonly VBoxContainer _cardList;
    private readonly Label _empty;
    private readonly Button _close;
    private Button? _firstNavigationButton;

    public MorningBriefingOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;

        AddChild(Dim(new Color(0.008f, 0.015f, 0.06f, 0.86f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(548, 334)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0c1836fc"),
                ThemeFactory.Mint,
                2,
                10
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);

        _title = ThemeFactory.Label(size: 20, color: ThemeFactory.Gold);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);

        _summaryPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(514, 88)
        };
        _summaryPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#101f3ff2"),
                ThemeFactory.Mint,
                1,
                6,
                6
            )
        );
        _summaryList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _summaryList.AddThemeConstantOverride("separation", 2);
        _summaryPanel.AddChild(_summaryList);
        column.AddChild(_summaryPanel);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(514, 148),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        _cardList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _cardList.AddThemeConstantOverride("separation", 5);
        scroll.AddChild(_cardList);
        column.AddChild(scroll);

        _empty = ThemeFactory.Label(size: 11, color: ThemeFactory.MutedInk);
        _empty.HorizontalAlignment = HorizontalAlignment.Center;
        _empty.Visible = false;
        column.AddChild(_empty);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 26);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        FocusInitialControl();
    }

    public event Action? CloseRequested;
    public event Action<WorldNavigationDestination>? NavigationRequested;

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed(InputSetup.UiCancel))
        {
            return;
        }

        CloseRequested?.Invoke();
        GetViewport().SetInputAsHandled();
    }

    public void RefreshText()
    {
        var display = MorningBriefingPresenter.Create(
            MorningBriefingSystem.Create(_session),
            _locale
        );

        _title.Text = _locale.Tr("morning.briefing.title");
        _empty.Text = _locale.Tr("morning.briefing.empty");
        _empty.Visible = display.IsEmpty;
        _close.Text = _locale.Tr("morning.briefing.close");

        foreach (var child in _summaryList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var child in _cardList.GetChildren())
        {
            child.QueueFree();
        }

        _firstNavigationButton = null;
        _summaryPanel.Visible = display.DecisionSummary.Count > 0;
        for (var index = 0; index < display.DecisionSummary.Count; index++)
        {
            _summaryList.AddChild(SummaryRow(display.DecisionSummary[index], index));
        }

        foreach (var card in display.Cards)
        {
            _cardList.AddChild(CardRow(card));
        }
    }

    private Control SummaryRow(
        MorningBriefingDecisionSummaryItem item,
        int index
    )
    {
        var destination = NavigationDestinationFor(item, _session);
        if (destination is { } navigationTarget)
        {
            return NavigableSummaryRow(item, index, navigationTarget);
        }

        return ReadOnlySummaryRow(item, index);
    }

    private Control NavigableSummaryRow(
        MorningBriefingDecisionSummaryItem item,
        int index,
        WorldNavigationDestination destination
    )
    {
        var row = SummaryRowContainer();
        row.AddChild(SummaryMarker(item, index));

        var button = ThemeFactory.Button(
            _locale.Tr(NavigateLocalizationKey, SummaryText(item))
        );
        button.CustomMinimumSize = new Vector2(460, 22);
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        ThemeFactory.SetFontSize(button, 9);
        button.Pressed += () => NavigationRequested?.Invoke(destination);
        row.AddChild(button);

        _firstNavigationButton ??= button;
        return row;
    }

    private static Control ReadOnlySummaryRow(
        MorningBriefingDecisionSummaryItem item,
        int index
    )
    {
        var row = SummaryRowContainer();
        row.AddChild(SummaryMarker(item, index));

        var text = ThemeFactory.Label(
            SummaryText(item),
            9,
            ThemeFactory.Mint
        );
        text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        text.CustomMinimumSize = new Vector2(460, 13);
        row.AddChild(text);

        return row;
    }

    private static HBoxContainer SummaryRowContainer()
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 5);
        return row;
    }

    private static Label SummaryMarker(
        MorningBriefingDecisionSummaryItem item,
        int index
    )
    {
        var marker = ThemeFactory.Label(
            $"{index + 1}",
            8,
            BorderColor(item.Priority)
        );
        marker.CustomMinimumSize = new Vector2(14, 12);
        marker.HorizontalAlignment = HorizontalAlignment.Center;
        return marker;
    }

    private static string SummaryText(MorningBriefingDecisionSummaryItem item) =>
        $"{item.Action} · {item.Title}";

    private static WorldNavigationDestination? NavigationDestinationFor(
        MorningBriefingDecisionSummaryItem item,
        GameSession session
    ) => MorningBriefingNavigationPresenter.TargetFor(item, session);

    private void FocusInitialControl()
    {
        var initial = _firstNavigationButton as Control ?? _close;
        initial.CallDeferred(Control.MethodName.GrabFocus);
    }

    private static Control CardRow(MorningBriefingDisplayCard card)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(500, 48)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#132446f2"),
                BorderColor(card.Priority),
                1,
                6,
                7
            )
        );

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        panel.AddChild(row);

        var marker = new ColorRect
        {
            Color = BorderColor(card.Priority),
            CustomMinimumSize = new Vector2(5, 42)
        };
        row.AddChild(marker);

        var text = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        text.AddThemeConstantOverride("separation", 2);
        row.AddChild(text);

        var title = ThemeFactory.Label(card.Title, 11, ThemeFactory.Gold);
        text.AddChild(title);

        var body = ThemeFactory.Label(card.Body, 9);
        body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        body.CustomMinimumSize = new Vector2(400, 25);
        text.AddChild(body);

        if (!string.IsNullOrWhiteSpace(card.Action))
        {
            var action = ThemeFactory.Label(card.Action, 8, ThemeFactory.Mint);
            text.AddChild(action);
        }

        return panel;
    }

    private static Color BorderColor(MorningBriefingPriority priority) =>
        priority switch
        {
            MorningBriefingPriority.Primary => ThemeFactory.Gold,
            MorningBriefingPriority.Secondary => ThemeFactory.Mint,
            _ => new Color("#51647d")
        };
}
