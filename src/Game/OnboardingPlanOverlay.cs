using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class OnboardingPlanOverlay : FullScreenUi
{
    private readonly LocaleService _locale;
    private readonly List<OnboardingPlanCard> _cards;
    private readonly OnboardingNinetyMinuteCoverageContract _coverage;
    private readonly Label _counter;
    private readonly Label _title;
    private readonly PanelContainer _progressPanel;
    private readonly GridContainer _progressGrid;
    private readonly Label _body;
    private readonly Button _next;
    private readonly Button _dismiss;
    private readonly Button _close;
    private int _index;

    public OnboardingPlanOverlay(
        Theme theme,
        OnboardingPlan plan,
        OnboardingNinetyMinuteCoverageContract coverage,
        LocaleService locale
    ) : base(theme)
    {
        _locale = locale;
        _cards = plan.Cards.ToList();
        _coverage = coverage;
        AddChild(Dim(new Color(0.008f, 0.014f, 0.058f, 0.82f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(430, 318)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0b1734fa"),
                ThemeFactory.Mint,
                2,
                9
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        _title = ThemeFactory.Label(size: 20, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(_title);
        _counter = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _counter.HorizontalAlignment = HorizontalAlignment.Right;
        header.AddChild(_counter);
        column.AddChild(header);

        _progressPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(392, 58)
        };
        _progressPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#101f3ff2"),
                ThemeFactory.PanelEdge,
                1,
                6,
                5
            )
        );
        _progressGrid = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _progressGrid.AddThemeConstantOverride("h_separation", 4);
        _progressGrid.AddThemeConstantOverride("v_separation", 3);
        _progressPanel.AddChild(_progressGrid);
        column.AddChild(_progressPanel);

        var paper = new PanelContainer
        {
            CustomMinimumSize = new Vector2(392, 126)
        };
        paper.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#172746f2"),
                ThemeFactory.Gold,
                1,
                7,
                8
            )
        );
        _body = ThemeFactory.Label(size: 11);
        _body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _body.CustomMinimumSize = new Vector2(374, 102);
        paper.AddChild(_body);
        column.AddChild(paper);

        var buttons = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        buttons.AddThemeConstantOverride("separation", 8);
        _next = ThemeFactory.Button("");
        _next.CustomMinimumSize = new Vector2(118, 26);
        _dismiss = ThemeFactory.Button("");
        _dismiss.CustomMinimumSize = new Vector2(118, 26);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(118, 26);
        buttons.AddChild(_next);
        buttons.AddChild(_dismiss);
        buttons.AddChild(_close);
        column.AddChild(buttons);

        _next.Pressed += NextCard;
        _dismiss.Pressed += DismissCurrent;
        _close.Pressed += () => CloseRequested?.Invoke();
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _next.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action<string>? CardDismissed;
    public event Action? CloseRequested;

    public override void _ExitTree()
    {
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

    private void NextCard()
    {
        if (_cards.Count == 0)
        {
            CloseRequested?.Invoke();
            return;
        }

        _index = (_index + 1) % _cards.Count;
        RefreshText();
    }

    private void DismissCurrent()
    {
        if (_cards.Count == 0)
        {
            CloseRequested?.Invoke();
            return;
        }

        var card = _cards[_index];
        if (!card.CanDismiss)
        {
            return;
        }

        _cards.RemoveAt(_index);
        CardDismissed?.Invoke(card.Id);
        if (_cards.Count == 0)
        {
            CloseRequested?.Invoke();
            return;
        }

        _index = Math.Clamp(_index, 0, _cards.Count - 1);
        RefreshText();
    }

    private void RefreshText()
    {
        _next.Text = _locale.Tr("onboarding.action.next");
        _dismiss.Text = _locale.Tr("onboarding.action.dismiss");
        _close.Text = _locale.Tr("onboarding.action.close");

        if (_cards.Count == 0)
        {
            _title.Text = string.Empty;
            _body.Text = string.Empty;
            _counter.Text = string.Empty;
            ClearProgress();
            _next.Disabled = true;
            _dismiss.Disabled = true;
            return;
        }

        var display = OnboardingPlanPresenter.Create(
            new OnboardingPlan(_cards),
            _coverage,
            _locale
        );
        RefreshProgress(display.CapabilityProgress);
        var card = display.Cards[_index];
        _title.Text = card.Title;
        _body.Text = card.Body;
        _counter.Text = $"{_index + 1}/{_cards.Count}";
        _next.Disabled = _cards.Count <= 1;
        _dismiss.Disabled = !card.CanDismiss;
    }

    private void RefreshProgress(
        IReadOnlyList<OnboardingCapabilityProgressItem> items
    )
    {
        ClearProgress();
        _progressPanel.Visible = items.Count > 0;
        foreach (var item in items)
        {
            _progressGrid.AddChild(ProgressTile(item));
        }
    }

    private void ClearProgress()
    {
        foreach (var child in _progressGrid.GetChildren())
        {
            child.QueueFree();
        }
    }

    private static Control ProgressTile(OnboardingCapabilityProgressItem item)
    {
        var tile = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(124, 16),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = item.Status
        };
        tile.AddThemeConstantOverride("separation", 4);

        var marker = new ColorRect
        {
            Color = ProgressColor(item.State),
            CustomMinimumSize = new Vector2(6, 14)
        };
        tile.AddChild(marker);

        var title = ThemeFactory.Label(item.Title, 8, ThemeFactory.Ink);
        title.ClipText = true;
        title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        title.TooltipText = item.Status;
        tile.AddChild(title);

        return tile;
    }

    private static Color ProgressColor(OnboardingCoverageState state) =>
        state switch
        {
            OnboardingCoverageState.Complete => ThemeFactory.Gold,
            OnboardingCoverageState.InProgress => ThemeFactory.Mint,
            _ => new Color("#51647d")
        };
}
