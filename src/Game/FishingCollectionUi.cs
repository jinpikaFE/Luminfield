using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class FishingCollectionOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _summary;
    private readonly Label _hint;
    private readonly Label _rewardTitle;
    private readonly Label _rewardDetail;
    private readonly Button _rewardAction;
    private readonly Label _notice;
    private readonly Button _close;
    private readonly List<FishingCollectionRow> _rows = [];
    private string _visibleRewardId = string.Empty;

    public FishingCollectionOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.015f, 0.02f, 0.08f, 0.82f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(548, 342) };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(new Color("#0c1735fa"), ThemeFactory.Mint, 2, 9)
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 5);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        var emblem = new HotbarSlotContent();
        emblem.CustomMinimumSize = new Vector2(42, 38);
        emblem.SetState("__fish_shadow__", 0, 0, false);
        _title = ThemeFactory.Label(size: 19, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.VerticalAlignment = VerticalAlignment.Center;
        _summary = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _summary.HorizontalAlignment = HorizontalAlignment.Right;
        _summary.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(emblem);
        header.AddChild(_title);
        header.AddChild(_summary);
        column.AddChild(header);

        _hint = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _hint.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_hint);

        var rewardPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(514, 52)
        };
        rewardPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#142943f2"),
                ThemeFactory.Gold,
                1,
                6,
                5
            )
        );
        var rewardRow = new HBoxContainer();
        rewardRow.AddThemeConstantOverride("separation", 8);
        rewardPanel.AddChild(rewardRow);

        var rewardText = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        rewardText.AddThemeConstantOverride("separation", 1);
        _rewardTitle = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        _rewardDetail = ThemeFactory.Label(size: 8, color: ThemeFactory.MutedInk);
        _rewardDetail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        rewardText.AddChild(_rewardTitle);
        rewardText.AddChild(_rewardDetail);
        rewardRow.AddChild(rewardText);

        _rewardAction = ThemeFactory.Button("");
        _rewardAction.CustomMinimumSize = new Vector2(104, 24);
        _rewardAction.Pressed += ClaimVisibleReward;
        rewardRow.AddChild(_rewardAction);
        column.AddChild(rewardPanel);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(514, 118),
            SizeFlagsVertical = SizeFlags.Fill
        };
        var list = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(498, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        list.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(list);
        column.AddChild(scroll);

        foreach (var entry in _session.Fishing.CollectionEntries())
        {
            var row = new FishingCollectionRow(_session, _locale, entry.Fish);
            list.AddChild(row);
            _rows.Add(row);
        }

        _notice = ThemeFactory.Label(size: 8, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(500, 12);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(180, 28);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action<string>? RewardClaimed;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("fishing.collection.title");
        _summary.Text = _locale.Tr(
            "fishing.collection.summary",
            _session.Fishing.CaughtCount,
            _session.Fishing.TotalFishCount
        );
        _hint.Text = _locale.Tr("fishing.collection.hint");
        _close.Text = _locale.Tr("menu.back");
        RefreshReward();

        foreach (var row in _rows)
        {
            row.RefreshText();
        }
    }

    private void RefreshReward()
    {
        var reward = VisibleReward();
        _visibleRewardId = reward.Definition.Id;
        _rewardTitle.Text = _locale.Tr(reward.Definition.TitleKey);
        _rewardDetail.Text = _locale.Tr(
            "fishing.reward.detail",
            _locale.Tr(reward.Definition.DescriptionKey),
            reward.Progress,
            reward.Definition.RequiredCaughtCount,
            RewardText(reward.Definition)
        );
        _rewardAction.Text = RewardButtonText(reward.Status);
        _rewardAction.Disabled =
            reward.Status != FishingCollectionRewardStatus.Ready;
    }

    private FishingCollectionRewardSnapshot VisibleReward()
    {
        var snapshots = _session.FishingCollectionRewards();
        var ready = snapshots.FirstOrDefault(snapshot =>
            snapshot.Status == FishingCollectionRewardStatus.Ready
        );
        if (ready is not null)
        {
            return ready;
        }

        var locked = snapshots.FirstOrDefault(snapshot =>
            snapshot.Status == FishingCollectionRewardStatus.Locked
        );
        if (locked is not null)
        {
            return locked;
        }

        return snapshots.Last();
    }

    private string RewardButtonText(FishingCollectionRewardStatus status)
    {
        return status switch
        {
            FishingCollectionRewardStatus.Claimed =>
                _locale.Tr("fishing.reward.status.claimed"),
            FishingCollectionRewardStatus.Ready =>
                _locale.Tr("fishing.reward.action.claim"),
            _ => _locale.Tr("fishing.reward.status.locked")
        };
    }

    private string RewardText(FishingCollectionRewardDefinition definition)
    {
        var parts = new List<string>();
        if (definition.RewardCoins > 0)
        {
            parts.Add(_locale.Tr(
                "fishing.reward.coins",
                definition.RewardCoins
            ));
        }

        if (!string.IsNullOrWhiteSpace(definition.RewardItemId) &&
            definition.RewardItemCount > 0)
        {
            parts.Add(_locale.Tr(
                "fishing.reward.item",
                _locale.Tr(
                    DataCatalog.Item(definition.RewardItemId).NameKey
                ),
                definition.RewardItemCount
            ));
        }

        return string.Join(" + ", parts);
    }

    private void ClaimVisibleReward()
    {
        if (string.IsNullOrWhiteSpace(_visibleRewardId))
        {
            return;
        }

        var result = _session.ClaimFishingCollectionReward(_visibleRewardId);
        RefreshText();
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            RewardClaimed?.Invoke(result.MessageKey);
        }
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }
}

internal sealed partial class FishingCollectionRow : PanelContainer
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly FishDefinition _fish;
    private readonly HotbarSlotContent _icon;
    private readonly Label _name;
    private readonly Label _detail;
    private readonly Label _status;

    public FishingCollectionRow(
        GameSession session,
        LocaleService locale,
        FishDefinition fish
    )
    {
        _session = session;
        _locale = locale;
        _fish = fish;
        CustomMinimumSize = new Vector2(498, 58);
        MouseFilter = MouseFilterEnum.Ignore;

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        AddChild(row);

        _icon = new HotbarSlotContent { CustomMinimumSize = new Vector2(48, 48) };
        row.AddChild(_icon);

        var textColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        textColumn.AddThemeConstantOverride("separation", 1);
        _name = ThemeFactory.Label(size: 12, color: ThemeFactory.Mint);
        _detail = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detail.CustomMinimumSize = new Vector2(310, 20);
        textColumn.AddChild(_name);
        textColumn.AddChild(_detail);
        row.AddChild(textColumn);

        _status = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _status.HorizontalAlignment = HorizontalAlignment.Right;
        _status.VerticalAlignment = VerticalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(72, 48);
        row.AddChild(_status);

        RefreshText();
    }

    public void RefreshText()
    {
        var caught = _session.Fishing.IsCaught(_fish.Id);
        AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                caught ? new Color("#142943f4") : new Color("#0d1830e8"),
                caught ? ThemeFactory.Mint : new Color("#405b72"),
                caught ? 2 : 1,
                6,
                4
            )
        );

        if (!caught)
        {
            _icon.SetState("__fish_shadow__", 0, 0, false);
            _icon.Modulate = new Color(0.54f, 0.82f, 0.9f, 0.48f);
            _name.Text = _locale.Tr("fishing.collection.hidden_name");
            _detail.Text = _locale.Tr("fishing.collection.hidden_detail");
            _status.Text = _locale.Tr("fishing.collection.unseen");
            TooltipText = _detail.Text;
            return;
        }

        _icon.Modulate = Colors.White;
        _icon.SetState(_fish.ItemId, 0, 0, false);
        var water = _locale.Tr(WaterKindKey(_fish.WaterKind));
        var condition = ConditionText();
        _name.Text = _locale.Tr(_fish.NameKey);
        _detail.Text = _locale.Tr(
            "fishing.collection.detail",
            water,
            condition
        );
        _status.Text = _locale.Tr("fishing.collection.caught");
        TooltipText = _detail.Text;
    }

    private string ConditionText()
    {
        return _locale.Tr(
            "fishing.condition.seasoned",
            SeasonText(),
            TimeWeatherText()
        );
    }

    private string SeasonText()
    {
        if (_fish.SeasonIds is not { Count: > 0 })
        {
            return _locale.Tr("fishing.condition.all_seasons");
        }

        return string.Join(
            " / ",
            _fish.SeasonIds.Select(seasonId =>
                _locale.Tr($"calendar.season.{seasonId}")
            )
        );
    }

    private string TimeWeatherText()
    {
        var hasTimeWindow =
            _fish.StartMinute != GameClock.StartMinute ||
            _fish.EndMinute != GameClock.EndMinute;
        var hasWeather = !string.IsNullOrWhiteSpace(_fish.WeatherId);
        if (!hasTimeWindow && !hasWeather)
        {
            return _locale.Tr("fishing.condition.any_time");
        }

        if (hasWeather && hasTimeWindow)
        {
            return _locale.Tr(
                "fishing.condition.weather_time",
                WeatherName(),
                FormatMinute(_fish.StartMinute),
                FormatMinute(_fish.EndMinute)
            );
        }

        if (hasWeather)
        {
            return _locale.Tr(
                "fishing.condition.weather",
                WeatherName()
            );
        }

        return _locale.Tr(
            "fishing.condition.time",
            FormatMinute(_fish.StartMinute),
            FormatMinute(_fish.EndMinute)
        );
    }

    private string WeatherName()
    {
        if (_fish.WeatherId is null ||
            !DataCatalog.WeatherDefinitions.TryGetValue(
                _fish.WeatherId,
                out var weather
            ))
        {
            return _locale.Tr("fishing.condition.any_weather");
        }

        return _locale.Tr(weather.NameKey);
    }

    private static string WaterKindKey(FishingWaterKind waterKind) =>
        waterKind switch
        {
            FishingWaterKind.CrystalStream =>
                "fishing.water.crystal_stream",
            FishingWaterKind.MoonwaterWetlands =>
                "fishing.water.moonwater_wetlands",
            _ => "fishing.water.homestead_pond"
        };

    private static string FormatMinute(int minute)
    {
        var hour = minute / 60;
        var part = minute % 60;
        return $"{hour:00}:{part:00}";
    }
}
