using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class CommissionBoardOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _day;
    private readonly Label _kind;
    private readonly Label _commissionTitle;
    private readonly Label _description;
    private readonly Label _progressText;
    private readonly ProgressBar _progress;
    private readonly Label _reward;
    private readonly Label _state;
    private readonly Label _notice;
    private readonly Button _action;
    private readonly Button _close;

    public CommissionBoardOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.012f, 0.018f, 0.075f, 0.84f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(486, 320)
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
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        header.AddChild(Icon(
            GeneratedArt.CreateCommissionParchmentIcon(),
            new Vector2(62, 58)
        ));

        var headerText = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _title = ThemeFactory.Label(size: 21, color: ThemeFactory.Mint);
        _day = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        headerText.AddChild(_title);
        headerText.AddChild(_day);
        header.AddChild(headerText);

        _kind = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _kind.HorizontalAlignment = HorizontalAlignment.Right;
        _kind.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_kind);
        column.AddChild(header);

        var paper = new PanelContainer
        {
            CustomMinimumSize = new Vector2(438, 126)
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
        var paperColumn = new VBoxContainer();
        paperColumn.AddThemeConstantOverride("separation", 4);
        paper.AddChild(paperColumn);
        _commissionTitle = ThemeFactory.Label(size: 16, color: ThemeFactory.Gold);
        _description = ThemeFactory.Label(size: 10);
        _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _description.CustomMinimumSize = new Vector2(410, 38);
        paperColumn.AddChild(_commissionTitle);
        paperColumn.AddChild(_description);

        var progressRow = new HBoxContainer();
        progressRow.AddThemeConstantOverride("separation", 8);
        _progress = new ProgressBar
        {
            MinValue = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(320, 12),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _progressText = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _progressText.CustomMinimumSize = new Vector2(70, 16);
        _progressText.HorizontalAlignment = HorizontalAlignment.Right;
        progressRow.AddChild(_progress);
        progressRow.AddChild(_progressText);
        paperColumn.AddChild(progressRow);
        column.AddChild(paper);

        var rewardRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        rewardRow.AddThemeConstantOverride("separation", 7);
        rewardRow.AddChild(Icon(
            GeneratedArt.CreateCommissionRewardIcon(),
            new Vector2(34, 34)
        ));
        _reward = ThemeFactory.Label(size: 13, color: ThemeFactory.Gold);
        _state = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        rewardRow.AddChild(_reward);
        rewardRow.AddChild(_state);
        column.AddChild(rewardRow);

        _action = ThemeFactory.Button("");
        _action.CustomMinimumSize = new Vector2(318, 32);
        _action.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _action.Pressed += Execute;
        column.AddChild(_action);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(430, 16);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 27);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _action.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? CommissionChanged;

    public void RefreshText()
    {
        var definition = _session.Commission.Current;
        var targetName = _locale.Tr(
            DataCatalog.Item(definition.TargetId).NameKey
        );
        var progress = _session.Commission.DisplayProgress(
            _session.Inventory
        );

        _title.Text = _locale.Tr("commission.board.title");
        _day.Text = _locale.Tr("commission.board.day", _session.Clock.Day);
        _kind.Text = _locale.Tr(KindKey(definition.Kind));
        _commissionTitle.Text = _locale.Tr(definition.TitleKey);
        _description.Text = _locale.Tr(
            definition.DescriptionKey,
            definition.RequiredCount,
            targetName
        );
        _progress.MaxValue = definition.RequiredCount;
        _progress.Value = progress;
        _progressText.Text = _locale.Tr(
            "commission.progress",
            progress,
            definition.RequiredCount
        );
        _reward.Text = _locale.Tr(
            "commission.reward",
            definition.RewardCoins
        );
        _close.Text = _locale.Tr("menu.back");

        if (_session.Commission.Claimed)
        {
            _state.Text = _locale.Tr("commission.state.claimed");
            _action.Text = _locale.Tr("commission.action.claimed");
            _action.Disabled = true;
            return;
        }

        if (!_session.Commission.Accepted)
        {
            _state.Text = _locale.Tr("commission.state.offered");
            _action.Text = _locale.Tr("commission.action.accept");
            _action.Disabled = false;
            return;
        }

        if (_session.Commission.IsReady(_session.Inventory))
        {
            _state.Text = _locale.Tr("commission.state.ready");
            _action.Text = _locale.Tr("commission.action.claim");
            _action.Disabled = false;
            return;
        }

        _state.Text = _locale.Tr("commission.state.tracking");
        _action.Text = _locale.Tr("commission.action.tracking");
        _action.Disabled = true;
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private void Execute()
    {
        var succeeded = false;
        var messageKey = "commission.not_ready";
        if (!_session.Commission.Accepted)
        {
            var result = _session.AcceptDailyCommission();
            succeeded = result.Succeeded;
            messageKey = result.MessageKey;
        }
        else if (_session.Commission.IsReady(_session.Inventory))
        {
            var result = _session.ClaimDailyCommission();
            succeeded = result.Succeeded;
            messageKey = result.MessageKey;
        }

        _notice.Text = _locale.Tr(messageKey);
        if (succeeded)
        {
            CommissionChanged?.Invoke();
        }
        RefreshText();
    }

    private static string KindKey(DailyCommissionKind kind) => kind switch
    {
        DailyCommissionKind.Plant => "commission.kind.plant",
        DailyCommissionKind.Gather => "commission.kind.gather",
        DailyCommissionKind.Deliver => "commission.kind.deliver",
        _ => "commission.kind.plant"
    };

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
