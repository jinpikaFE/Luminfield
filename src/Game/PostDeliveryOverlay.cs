using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed record PostDeliveryRouteDisplayItem(
    string RouteId,
    string RouteName,
    string TargetNpcName,
    int RewardCoins,
    int RelationshipPoints,
    string StatusKey,
    string ButtonText
);

public sealed partial class PostDeliveryOverlay : FullScreenUi
{
    public const string TitleKey = "post.delivery.ui.title";
    public const string DayProgressKey = "post.delivery.ui.day_progress";
    public const string RouteKey = "post.delivery.ui.route";
    public const string RewardKey = "post.delivery.ui.reward";
    public const string StateOfferedKey = "post.delivery.ui.state.offered";
    public const string StateActiveKey = "post.delivery.ui.state.active";
    public const string StateCompletedKey =
        "post.delivery.ui.state.completed";
    public const string StateLockedKey = "post.delivery.ui.state.locked";
    public const string EmptyKey = "post.delivery.ui.empty";
    public const string ActiveHintKey = "post.delivery.ui.active_hint";
    public const string ActionAcceptKey = "post.delivery.ui.action.accept";
    public const string CloseKey = "menu.back";

    public static IReadOnlyList<string> RequiredLocalizationKeys
    {
        get
        {
            var keys = new List<string>
            {
                TitleKey,
                DayProgressKey,
                RouteKey,
                RewardKey,
                StateOfferedKey,
                StateActiveKey,
                StateCompletedKey,
                StateLockedKey,
                EmptyKey,
                ActiveHintKey,
                ActionAcceptKey,
                CloseKey,
                "post.delivery.unavailable",
                "post.delivery.already_completed",
                "post.delivery.daily_limit_reached",
                "post.delivery.active_route_exists",
                "post.delivery.ready",
                "post.delivery.accepted",
                "post.delivery.none_active",
                "post.delivery.ready_to_deliver",
                "post.delivery.wrong_recipient",
                "post.delivery.completed",
                "post.delivery.completed.notice",
                "post.delivery.board.opened"
            };

            foreach (var route in PostDeliverySystem.Routes)
            {
                keys.Add(route.NameKey);
                keys.Add(route.ResponseKey);
            }

            return keys;
        }
    }

    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly VBoxContainer _routeList;
    private readonly Dictionary<string, Button> _routeButtons =
        new(StringComparer.Ordinal);
    private readonly Label _title;
    private readonly Label _day;
    private readonly Label _subtitle;
    private readonly Label _routeName;
    private readonly Label _target;
    private readonly Label _reward;
    private readonly Label _state;
    private readonly Label _instructions;
    private readonly Label _notice;
    private readonly Label _empty;
    private readonly Button _accept;
    private readonly Button _close;
    private string? _selectedRouteId;

    public PostDeliveryOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;

        AddChild(Dim(new Color(0.008f, 0.015f, 0.06f, 0.87f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(560, 332)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0b1734fc"),
                ThemeFactory.Gold,
                2,
                10
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 9);
        header.AddChild(Icon(
            GeneratedArt.CreateStarlightEnvelopeIcon(),
            new Vector2(52, 46)
        ));

        var headerText = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _title = ThemeFactory.Label(size: 21, color: ThemeFactory.Mint);
        _day = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        headerText.AddChild(_title);
        headerText.AddChild(_day);
        header.AddChild(headerText);

        _subtitle = ThemeFactory.Label(size: 10, color: ThemeFactory.MutedInk);
        _subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _subtitle.CustomMinimumSize = new Vector2(178, 42);
        _subtitle.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_subtitle);
        column.AddChild(header);

        var content = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(522, 206),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", 8);
        column.AddChild(content);

        var listPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(220, 202)
        };
        listPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#101e3aee"),
                ThemeFactory.PanelEdge,
                1,
                6,
                6
            )
        );
        _routeList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _routeList.AddThemeConstantOverride("separation", 6);
        listPanel.AddChild(_routeList);
        content.AddChild(listPanel);

        var detailPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(294, 202),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        detailPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#172746f5"),
                ThemeFactory.Gold,
                1,
                7,
                9
            )
        );
        var details = new VBoxContainer();
        details.AddThemeConstantOverride("separation", 6);
        detailPanel.AddChild(details);
        content.AddChild(detailPanel);

        _routeName = ThemeFactory.Label(size: 17, color: ThemeFactory.Gold);
        _routeName.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        details.AddChild(_routeName);

        _target = ThemeFactory.Label(size: 11, color: ThemeFactory.Mint);
        _target.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        details.AddChild(_target);

        _reward = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        _reward.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        details.AddChild(_reward);

        _state = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _state.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        details.AddChild(_state);

        _instructions = ThemeFactory.Label(
            size: 9,
            color: ThemeFactory.MutedInk
        );
        _instructions.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _instructions.CustomMinimumSize = new Vector2(268, 52);
        _instructions.SizeFlagsVertical = SizeFlags.ExpandFill;
        details.AddChild(_instructions);

        _empty = ThemeFactory.Label(size: 11, color: ThemeFactory.MutedInk);
        _empty.HorizontalAlignment = HorizontalAlignment.Center;
        _empty.VerticalAlignment = VerticalAlignment.Center;
        _empty.Visible = false;
        details.AddChild(_empty);

        _accept = ThemeFactory.Button("");
        _accept.CustomMinimumSize = new Vector2(238, 28);
        _accept.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _accept.Pressed += AcceptSelectedRoute;
        details.AddChild(_accept);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(514, 15);
        column.AddChild(_notice);

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
    public event Action? RouteAccepted;

    public void PressAcceptForPlaytest()
    {
        _accept.EmitSignal(Button.SignalName.Pressed);
    }

    public void ShowNotice(string messageKey)
    {
        _notice.Text = _locale.Tr(messageKey);
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    public void RefreshText() => RefreshText(keepNotice: false);

    public static IReadOnlyList<PostDeliveryRouteDisplayItem> CreateRouteItems(
        PostDeliveryBoardSnapshot board,
        LocaleService locale
    ) => board.Offers
        .Select(route => CreateRouteItem(board, route, locale))
        .ToArray();

    private static PostDeliveryRouteDisplayItem CreateRouteItem(
        PostDeliveryBoardSnapshot board,
        PostDeliveryRouteDefinition route,
        LocaleService locale
    )
    {
        var routeName = locale.Tr(route.NameKey);
        var targetName = TargetNpcName(route.TargetNpcId, locale);
        var statusKey = StatusKey(board, route);
        return new PostDeliveryRouteDisplayItem(
            route.Id,
            routeName,
            targetName,
            route.RewardCoins,
            PostDeliverySystem.RelationshipRewardPoints,
            statusKey,
            locale.Tr(RouteKey, targetName)
        );
    }

    private void RefreshText(bool keepNotice)
    {
        var board = _session.TodayPostDeliveryBoard;
        EnsureSelection(board);
        var items = CreateRouteItems(board, _locale);

        _title.Text = _locale.Tr(TitleKey);
        _day.Text = _locale.Tr(
            DayProgressKey,
            board.Day,
            board.CompletedCount,
            board.DailyLimit
        );
        _subtitle.Text = _locale.Tr(ActiveHintKey);
        _close.Text = _locale.Tr(CloseKey);
        if (!keepNotice)
        {
            _notice.Text = string.Empty;
        }

        RebuildRouteList(items);
        RefreshSelected(board, items);
    }

    private void RebuildRouteList(
        IReadOnlyList<PostDeliveryRouteDisplayItem> items
    )
    {
        foreach (var child in _routeList.GetChildren())
        {
            child.QueueFree();
        }
        _routeButtons.Clear();

        foreach (var item in items)
        {
            var button = ThemeFactory.Button(item.ButtonText);
            button.CustomMinimumSize = new Vector2(196, 48);
            button.ToggleMode = true;
            button.ButtonPressed = item.RouteId == _selectedRouteId;
            button.TooltipText = _locale.Tr(item.StatusKey);
            ThemeFactory.SetFontSize(button, 10);
            button.Pressed += () => SelectRoute(item.RouteId);
            _routeButtons[item.RouteId] = button;
            _routeList.AddChild(button);
        }
    }

    private void RefreshSelected(
        PostDeliveryBoardSnapshot board,
        IReadOnlyList<PostDeliveryRouteDisplayItem> items
    )
    {
        var selected = items.FirstOrDefault(item =>
            item.RouteId == _selectedRouteId
        );
        var hasSelection = selected is not null;

        _routeName.Visible = hasSelection;
        _target.Visible = hasSelection;
        _reward.Visible = hasSelection;
        _state.Visible = hasSelection;
        _instructions.Visible = hasSelection;
        _accept.Visible = hasSelection;
        _empty.Visible = !hasSelection;
        if (selected is null)
        {
            _empty.Text = _locale.Tr(EmptyKey);
            _accept.Disabled = true;
            return;
        }

        _routeName.Text = selected.RouteName;
        _target.Text = _locale.Tr(RouteKey, selected.TargetNpcName);
        _reward.Text = _locale.Tr(
            RewardKey,
            selected.RewardCoins,
            selected.RelationshipPoints
        );
        _state.Text = _locale.Tr(selected.StatusKey);
        _state.AddThemeColorOverride(
            "font_color",
            StatusColor(selected.StatusKey)
        );
        _instructions.Text = _locale.Tr(ActiveHintKey);
        RefreshAcceptButton(board, selected.RouteId);
    }

    private void RefreshAcceptButton(
        PostDeliveryBoardSnapshot board,
        string routeId
    )
    {
        _accept.Disabled = true;

        if (board.CompletedRouteIds.Contains(routeId))
        {
            _accept.Text = _locale.Tr(StateCompletedKey);
            return;
        }

        if (board.ActiveRouteId == routeId)
        {
            _accept.Text = _locale.Tr(StateActiveKey);
            return;
        }

        if (board.CompletedCount >= board.DailyLimit)
        {
            _accept.Text = _locale.Tr(StateLockedKey);
            return;
        }

        if (board.HasActiveRoute)
        {
            _accept.Text = _locale.Tr(StateLockedKey);
            return;
        }

        _accept.Text = _locale.Tr(ActionAcceptKey);
        _accept.Disabled = false;
    }

    private void SelectRoute(string routeId)
    {
        _selectedRouteId = routeId;
        RefreshText(keepNotice: true);
    }

    private void AcceptSelectedRoute()
    {
        if (_selectedRouteId is null)
        {
            return;
        }

        var result = _session.AcceptPostDelivery(_selectedRouteId);
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            RouteAccepted?.Invoke();
        }

        RefreshText(keepNotice: true);
        FocusInitialControl();
    }

    private void EnsureSelection(PostDeliveryBoardSnapshot board)
    {
        if (_selectedRouteId is not null &&
            board.Offers.Any(route => route.Id == _selectedRouteId))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(board.ActiveRouteId) &&
            board.Offers.Any(route => route.Id == board.ActiveRouteId))
        {
            _selectedRouteId = board.ActiveRouteId;
            return;
        }

        var available = board.Offers.FirstOrDefault(route =>
            !board.CompletedRouteIds.Contains(route.Id)
        );
        _selectedRouteId = available?.Id ?? board.Offers.FirstOrDefault()?.Id;
    }

    private void FocusInitialControl()
    {
        if (_selectedRouteId is not null &&
            _routeButtons.TryGetValue(_selectedRouteId, out var selected))
        {
            selected.CallDeferred(Control.MethodName.GrabFocus);
            return;
        }

        _accept.CallDeferred(Control.MethodName.GrabFocus);
    }

    private static string StatusKey(
        PostDeliveryBoardSnapshot board,
        PostDeliveryRouteDefinition route
    )
    {
        if (board.CompletedRouteIds.Contains(route.Id))
        {
            return StateCompletedKey;
        }

        if (board.ActiveRouteId == route.Id)
        {
            return StateActiveKey;
        }

        if (board.CompletedCount >= board.DailyLimit)
        {
            return StateLockedKey;
        }

        return board.HasActiveRoute ? StateLockedKey : StateOfferedKey;
    }

    private static Color StatusColor(string statusKey)
    {
        if (statusKey == StateCompletedKey)
        {
            return ThemeFactory.MutedInk;
        }

        if (statusKey == StateActiveKey ||
            statusKey == StateOfferedKey)
        {
            return ThemeFactory.Mint;
        }

        return ThemeFactory.Gold;
    }

    private static string TargetNpcName(string npcId, LocaleService locale)
    {
        if (!VillageCatalog.Npcs.TryGetValue(npcId, out var npc))
        {
            return npcId;
        }

        return locale.Tr(npc.NameKey);
    }

    private static TextureRect Icon(Texture2D texture, Vector2 size) => new()
    {
        Texture = texture,
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };
}
