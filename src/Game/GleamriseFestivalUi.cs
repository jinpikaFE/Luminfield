using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class GleamriseFestivalOverlay : FullScreenUi
{
    private sealed class StageRow
    {
        public required FestivalStageDefinition Definition { get; init; }
        public required Label Title { get; init; }
        public required Label Description { get; init; }
        public required Label Reward { get; init; }
        public required Button Action { get; init; }
    }

    private sealed class ExchangeRow
    {
        public required FestivalExchangeDefinition Definition { get; init; }
        public required Label Title { get; init; }
        public required Label Detail { get; init; }
        public required Button Action { get; init; }
    }

    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Label _summary;
    private readonly Label _stageHeader;
    private readonly Label _exchangeHeader;
    private readonly Label _notice;
    private readonly Button _close;
    private readonly List<StageRow> _stageRows = [];
    private readonly List<ExchangeRow> _exchangeRows = [];

    public GleamriseFestivalOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        bool preferExchange = false
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.006f, 0.012f, 0.06f, 0.88f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(584, 348)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#08152ffa"),
                ThemeFactory.Gold,
                2,
                8
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);

        var header = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(544, 42)
        };
        header.AddThemeConstantOverride("separation", 8);
        var headerBadge = new FestivalBadge();
        headerBadge.CustomMinimumSize = new Vector2(44, 40);
        header.AddChild(headerBadge);

        var headerText = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Mint);
        _subtitle = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        headerText.AddChild(_title);
        headerText.AddChild(_subtitle);
        header.AddChild(headerText);
        _summary = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        _summary.HorizontalAlignment = HorizontalAlignment.Right;
        _summary.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_summary);
        column.AddChild(header);

        var sections = new HBoxContainer();
        sections.AddThemeConstantOverride("separation", 6);
        column.AddChild(sections);

        var stagePanel = SectionPanel(new Vector2(276, 222));
        var stageColumn = new VBoxContainer();
        stageColumn.AddThemeConstantOverride("separation", 3);
        _stageHeader = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        stageColumn.AddChild(_stageHeader);
        stagePanel.AddChild(stageColumn);
        sections.AddChild(stagePanel);

        foreach (var stage in session.GleamriseFestivalSnapshot().Stages)
        {
            stageColumn.AddChild(BuildStageRow(stage.Definition));
        }

        var exchangePanel = SectionPanel(new Vector2(262, 222));
        var exchangeColumn = new VBoxContainer();
        exchangeColumn.AddThemeConstantOverride("separation", 3);
        _exchangeHeader = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        exchangeColumn.AddChild(_exchangeHeader);
        exchangePanel.AddChild(exchangeColumn);
        sections.AddChild(exchangePanel);

        foreach (var exchange in session.GleamriseFestivalSnapshot()
                     .ExchangeItems)
        {
            exchangeColumn.AddChild(BuildExchangeRow(exchange.Definition));
        }

        _notice = ThemeFactory.Label(size: 8, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(544, 12);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(160, 24);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        FocusInitialButton(preferExchange);
    }

    public event Action? CloseRequested;
    public event Action? FestivalChanged;

    public void RefreshText()
    {
        var snapshot = _session.GleamriseFestivalSnapshot();
        _title.Text = _locale.Tr("festival.ui.title");
        _subtitle.Text = _locale.Tr("festival.ui.subtitle");
        _summary.Text = _locale.Tr(
            "festival.ui.summary",
            _locale.Tr("calendar.season.gleamrise"),
            FestivalSystem.FestivalSeasonDay,
            _session.Inventory.Count(DataCatalog.GleamriseFestivalTokenId)
        );
        _stageHeader.Text = _locale.Tr("festival.ui.stage_header");
        _exchangeHeader.Text = _locale.Tr("festival.ui.exchange_header");
        _close.Text = _locale.Tr("menu.back");

        foreach (var row in _stageRows)
        {
            var stage = snapshot.Stages.First(item =>
                item.Definition.Id == row.Definition.Id
            );
            row.Title.Text = _locale.Tr(row.Definition.TitleKey);
            row.Description.Text = _locale.Tr(row.Definition.DescriptionKey);
            row.Reward.Text = StageRewardText(row.Definition);
            row.Action.Disabled = !stage.IsCurrent || stage.Completed;
            row.Action.Text = StageActionText(stage);
        }

        foreach (var row in _exchangeRows)
        {
            var exchange = snapshot.ExchangeItems.First(item =>
                item.Definition.ItemId == row.Definition.ItemId
            );
            var itemName = _locale.Tr(
                DataCatalog.Item(row.Definition.ItemId).NameKey
            );
            row.Title.Text = itemName;
            row.Detail.Text = _locale.Tr(
                row.Definition.DescriptionKey,
                row.Definition.Count,
                exchange.OwnedItemCount
            );
            row.Action.Disabled = !exchange.CanAfford;
            row.Action.Text = _locale.Tr(
                "festival.ui.exchange_action",
                row.Definition.TokenCost,
                row.Definition.Count
            );
        }
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private Control BuildStageRow(FestivalStageDefinition definition)
    {
        var panel = SectionPanel(new Vector2(254, 60));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 5);
        panel.AddChild(row);

        var text = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        text.AddThemeConstantOverride("separation", 1);
        var title = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        var description = ThemeFactory.Label(size: 7);
        description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        description.CustomMinimumSize = new Vector2(148, 24);
        var reward = ThemeFactory.Label(size: 7, color: ThemeFactory.Gold);
        text.AddChild(title);
        text.AddChild(description);
        text.AddChild(reward);
        row.AddChild(text);

        var action = ThemeFactory.Button("");
        action.CustomMinimumSize = new Vector2(82, 30);
        action.Pressed += AdvanceStage;
        row.AddChild(action);

        _stageRows.Add(new StageRow
        {
            Definition = definition,
            Title = title,
            Description = description,
            Reward = reward,
            Action = action
        });
        return panel;
    }

    private Control BuildExchangeRow(FestivalExchangeDefinition definition)
    {
        var panel = SectionPanel(new Vector2(240, 29));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        panel.AddChild(row);

        var text = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        text.AddThemeConstantOverride("separation", 0);
        var title = ThemeFactory.Label(size: 8, color: ThemeFactory.Mint);
        var detail = ThemeFactory.Label(size: 7, color: ThemeFactory.MutedInk);
        detail.ClipText = true;
        text.AddChild(title);
        text.AddChild(detail);
        row.AddChild(text);

        var action = ThemeFactory.Button("");
        action.CustomMinimumSize = new Vector2(72, 24);
        action.Pressed += () => Exchange(definition.ItemId);
        row.AddChild(action);

        _exchangeRows.Add(new ExchangeRow
        {
            Definition = definition,
            Title = title,
            Detail = detail,
            Action = action
        });
        return panel;
    }

    private string StageRewardText(FestivalStageDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.RewardItemId) ||
            definition.RewardItemCount <= 0)
        {
            return _locale.Tr(
                "festival.ui.stage_reward_tokens",
                definition.RewardTokens
            );
        }

        return _locale.Tr(
            "festival.ui.stage_reward",
            definition.RewardTokens,
            _locale.Tr(DataCatalog.Item(definition.RewardItemId).NameKey),
            definition.RewardItemCount
        );
    }

    private string StageActionText(FestivalStageSnapshot stage)
    {
        if (stage.Completed)
        {
            return _locale.Tr("festival.ui.stage_complete");
        }

        return stage.IsCurrent
            ? _locale.Tr("festival.ui.stage_do")
            : _locale.Tr("festival.ui.stage_wait");
    }

    private void AdvanceStage()
    {
        var result = _session.AdvanceGleamriseFestivalStage();
        _notice.Text = _locale.Tr(result.MessageKey);
        if (!result.Succeeded)
        {
            return;
        }

        FestivalChanged?.Invoke();
        RefreshText();
    }

    private void Exchange(string itemId)
    {
        var result = _session.ExchangeGleamriseFestivalItem(itemId);
        _notice.Text = _locale.Tr(result.MessageKey);
        if (!result.Succeeded)
        {
            return;
        }

        FestivalChanged?.Invoke();
        RefreshText();
    }

    private void FocusInitialButton(bool preferExchange)
    {
        if (preferExchange && _exchangeRows.Count > 0)
        {
            _exchangeRows[0].Action.CallDeferred(Control.MethodName.GrabFocus);
            return;
        }

        if (_stageRows.Count > 0)
        {
            _stageRows[0].Action.CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    private static PanelContainer SectionPanel(Vector2 size)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = size
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#111f3be8"),
                new Color("#526e89"),
                1,
                5,
                4
            )
        );
        return panel;
    }
}

internal sealed partial class FestivalBadge : TextureRect
{
    public FestivalBadge()
    {
        Texture = GeneratedArt.CreateGleamriseFestivalBadgeIcon();
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
    }
}
