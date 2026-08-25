using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public enum CommissionBoardPage
{
    Daily,
    Weekly
}

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
    private readonly Label _itemStatus;
    private readonly ProgressBar _progress;
    private readonly Label _reward;
    private readonly Label _state;
    private readonly Label _notice;
    private readonly Button _action;
    private readonly Button _close;
    private readonly Button _dailyTab;
    private readonly Button _weeklyTab;
    private CommissionBoardPage _page;

    public CommissionBoardOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        CommissionBoardPage initialPage = CommissionBoardPage.Daily
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _page = initialPage;
        AddChild(Dim(new Color(0.012f, 0.018f, 0.075f, 0.84f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(486, 340)
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
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        header.AddChild(Icon(
            GeneratedArt.CreateCommissionParchmentIcon(),
            new Vector2(52, 48)
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

        var tabs = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        tabs.AddThemeConstantOverride("separation", 8);
        _dailyTab = ThemeFactory.Button("");
        _dailyTab.CustomMinimumSize = new Vector2(150, 24);
        _dailyTab.Pressed += () => ShowPage(CommissionBoardPage.Daily);
        _weeklyTab = ThemeFactory.Button("");
        _weeklyTab.CustomMinimumSize = new Vector2(150, 24);
        _weeklyTab.Pressed += () => ShowPage(CommissionBoardPage.Weekly);
        tabs.AddChild(_dailyTab);
        tabs.AddChild(_weeklyTab);
        column.AddChild(tabs);

        var paper = new PanelContainer
        {
            CustomMinimumSize = new Vector2(438, 108)
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
        _description.CustomMinimumSize = new Vector2(410, 30);
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
        _itemStatus = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        paperColumn.AddChild(_itemStatus);
        column.AddChild(paper);

        var rewardRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        rewardRow.AddThemeConstantOverride("separation", 7);
        rewardRow.AddChild(Icon(
            GeneratedArt.CreateCommissionRewardIcon(),
            new Vector2(30, 30)
        ));
        _reward = ThemeFactory.Label(size: 13, color: ThemeFactory.Gold);
        _state = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        rewardRow.AddChild(_reward);
        rewardRow.AddChild(_state);
        column.AddChild(rewardRow);

        _action = ThemeFactory.Button("");
        _action.CustomMinimumSize = new Vector2(318, 29);
        _action.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _action.Pressed += Execute;
        column.AddChild(_action);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(430, 14);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 24);
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
    public event Action<string>? RewardClaimed;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("commission.board.title");
        _dailyTab.Text = _locale.Tr("commission.tab.daily");
        _weeklyTab.Text = _locale.Tr("commission.tab.weekly");
        _dailyTab.Disabled = _page == CommissionBoardPage.Daily;
        _weeklyTab.Disabled = _page == CommissionBoardPage.Weekly;
        _close.Text = _locale.Tr("menu.back");

        if (_page == CommissionBoardPage.Weekly)
        {
            RefreshWeekly();
            return;
        }

        RefreshDaily();
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private void RefreshDaily()
    {
        var definition = _session.Commission.Current;
        var targetName = _locale.Tr(
            DataCatalog.Item(definition.TargetId).NameKey
        );
        var progress = _session.Commission.DisplayProgress(
            _session.Inventory
        );

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
        _itemStatus.Text = ItemStatus(
            definition.TargetId,
            definition.RequiredCount
        );
        _reward.Text = _locale.Tr(
            "commission.reward",
            definition.RewardCoins
        );
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

    private void RefreshWeekly()
    {
        var commission = _session.WeeklyCommission;
        var definition = commission.Current;
        var stage = commission.CurrentStage;
        var targetName = _locale.Tr(
            DataCatalog.Item(stage.TargetId).NameKey
        );
        var rewardName = _locale.Tr(
            DataCatalog.Item(definition.RewardItemId).NameKey
        );
        var progress = commission.DisplayProgress(_session.Inventory);

        _day.Text = _locale.Tr(
            "weekly_commission.board.week",
            commission.Week
        );
        _kind.Text = _locale.Tr(
            "weekly_commission.board.stage",
            commission.CurrentStageIndex + 1,
            definition.Stages.Count
        );
        _commissionTitle.Text = _locale.Tr(definition.TitleKey);
        _description.Text = _locale.Tr(
            stage.DescriptionKey,
            stage.RequiredCount,
            targetName
        );
        _progress.MaxValue = stage.RequiredCount;
        _progress.Value = progress;
        _progressText.Text = _locale.Tr(
            "commission.progress",
            progress,
            stage.RequiredCount
        );
        _itemStatus.Text = ItemStatus(stage.TargetId, stage.RequiredCount);
        _reward.Text = _locale.Tr(
            "weekly_commission.reward",
            definition.RewardCoins,
            definition.RewardItemCount,
            rewardName
        );

        if (commission.Claimed)
        {
            _state.Text = _locale.Tr(
                "weekly_commission.state.claimed"
            );
            _action.Text = _locale.Tr(
                "weekly_commission.action.claimed"
            );
            _action.Disabled = true;
            return;
        }

        if (!commission.Accepted)
        {
            _state.Text = _locale.Tr(
                "weekly_commission.state.offered"
            );
            _action.Text = _locale.Tr(
                "weekly_commission.action.accept"
            );
            _action.Disabled = false;
            return;
        }

        if (commission.IsReady(_session.Inventory))
        {
            RefreshWeeklyReadyState(commission.IsFinalStage);
            return;
        }

        _state.Text = _locale.Tr(
            "weekly_commission.state.tracking"
        );
        _action.Text = _locale.Tr(
            "weekly_commission.action.tracking"
        );
        _action.Disabled = true;
    }

    private void Execute()
    {
        if (_page == CommissionBoardPage.Weekly)
        {
            ExecuteWeekly();
            return;
        }

        ExecuteDaily();
    }

    private string ItemStatus(string itemId, int requiredCount)
    {
        var owned = _session.Inventory.CountFamily(itemId);
        var donated = _session.Collection.IsDonated(itemId) ||
            _session.Fishing.IsDonated(itemId);
        return _locale.Tr(
            "commission.item_status",
            owned,
            requiredCount,
            _locale.Tr(donated ? "settings.yes" : "settings.no"),
            Math.Max(0, requiredCount - owned)
        );
    }

    private void ExecuteDaily()
    {
        var succeeded = false;
        var messageKey = "commission.not_ready";
        var rewardClaimed = false;
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
            rewardClaimed = result.Succeeded;
        }

        _notice.Text = _locale.Tr(messageKey);
        if (rewardClaimed)
        {
            RewardClaimed?.Invoke(messageKey);
        }
        else if (succeeded)
        {
            CommissionChanged?.Invoke();
        }
        RefreshText();
    }

    private void ExecuteWeekly()
    {
        var succeeded = false;
        var messageKey = "weekly_commission.not_ready";
        var rewardClaimed = false;
        var commission = _session.WeeklyCommission;
        if (!commission.Accepted)
        {
            var result = _session.AcceptWeeklyCommission();
            succeeded = result.Succeeded;
            messageKey = result.MessageKey;
        }
        else if (commission.IsReady(_session.Inventory))
        {
            if (commission.IsFinalStage)
            {
                var result = _session.ClaimWeeklyCommission();
                succeeded = result.Succeeded;
                messageKey = result.MessageKey;
                rewardClaimed = result.Succeeded;
            }
            else
            {
                var result = _session.AdvanceWeeklyCommissionStage();
                succeeded = result.Succeeded;
                messageKey = result.MessageKey;
            }
        }

        _notice.Text = _locale.Tr(messageKey);
        if (rewardClaimed)
        {
            RewardClaimed?.Invoke(messageKey);
        }
        else if (succeeded)
        {
            CommissionChanged?.Invoke();
        }
        RefreshText();
    }

    private void ShowPage(CommissionBoardPage page)
    {
        if (_page == page)
        {
            return;
        }

        _page = page;
        _notice.Text = string.Empty;
        RefreshText();
        _action.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void RefreshWeeklyReadyState(bool isFinalStage)
    {
        if (isFinalStage)
        {
            _state.Text = _locale.Tr(
                "weekly_commission.state.reward_ready"
            );
            _action.Text = _locale.Tr(
                "weekly_commission.action.claim"
            );
        }
        else
        {
            _state.Text = _locale.Tr(
                "weekly_commission.state.stage_ready"
            );
            _action.Text = _locale.Tr(
                "weekly_commission.action.advance"
            );
        }

        _action.Disabled = false;
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
