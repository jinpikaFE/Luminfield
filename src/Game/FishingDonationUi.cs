using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class FishingDonationOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _summary;
    private readonly Label _hint;
    private readonly Label _notice;
    private readonly Button _close;
    private readonly List<FishingDonationRow> _rows = [];

    public FishingDonationOverlay(
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
            ThemeFactory.Box(new Color("#0c1735fa"), ThemeFactory.Gold, 2, 9)
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
        _title = ThemeFactory.Label(size: 19, color: ThemeFactory.Gold);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.VerticalAlignment = VerticalAlignment.Center;
        _summary = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _summary.HorizontalAlignment = HorizontalAlignment.Right;
        _summary.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(emblem);
        header.AddChild(_title);
        header.AddChild(_summary);
        column.AddChild(header);

        _hint = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _hint.HorizontalAlignment = HorizontalAlignment.Center;
        _hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _hint.CustomMinimumSize = new Vector2(514, 24);
        column.AddChild(_hint);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(514, 204),
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

        foreach (var entry in _session.FishingDonationEntries())
        {
            var row = new FishingDonationRow(_session, _locale, entry.Fish);
            row.DonateRequested += DonateFish;
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

    public void RefreshText()
    {
        _title.Text = _locale.Tr("fishing.donation.title");
        _summary.Text = _locale.Tr(
            "fishing.donation.summary",
            _session.Fishing.DonatedCount,
            _session.Fishing.TotalFishCount
        );
        _hint.Text = _locale.Tr("fishing.donation.hint");
        _close.Text = _locale.Tr("menu.back");

        foreach (var row in _rows)
        {
            row.RefreshText();
        }
    }

    private void DonateFish(string fishId)
    {
        var result = _session.DonateFishToArchive(fishId);
        RefreshText();
        _notice.Text = _locale.Tr(result.MessageKey);
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }
}

internal sealed partial class FishingDonationRow : PanelContainer
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly FishDefinition _fish;
    private readonly HotbarSlotContent _icon;
    private readonly Label _name;
    private readonly Label _detail;
    private readonly Label _status;
    private readonly Button _action;

    public FishingDonationRow(
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
        _detail.CustomMinimumSize = new Vector2(260, 20);
        textColumn.AddChild(_name);
        textColumn.AddChild(_detail);
        row.AddChild(textColumn);

        var actionColumn = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(94, 48)
        };
        actionColumn.AddThemeConstantOverride("separation", 2);
        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _action = ThemeFactory.Button("");
        _action.CustomMinimumSize = new Vector2(90, 22);
        _action.Pressed += () => DonateRequested?.Invoke(_fish.Id);
        actionColumn.AddChild(_status);
        actionColumn.AddChild(_action);
        row.AddChild(actionColumn);

        RefreshText();
    }

    public event Action<string>? DonateRequested;

    public void RefreshText()
    {
        var caught = _session.Fishing.IsCaught(_fish.Id);
        var donated = _session.Fishing.IsDonated(_fish.Id);
        var owned = _session.Inventory.Count(_fish.ItemId);
        var available = caught && !donated && owned > 0;
        AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                donated ? new Color("#13263bf4") : new Color("#0d1830e8"),
                BorderColor(caught, donated, available),
                donated || available ? 2 : 1,
                6,
                4
            )
        );

        if (!caught)
        {
            _icon.SetState("__fish_shadow__", 0, 0, false);
            _icon.Modulate = new Color(0.54f, 0.82f, 0.9f, 0.48f);
            _name.Text = _locale.Tr("fishing.collection.hidden_name");
            _detail.Text = _locale.Tr("fishing.donation.hidden_detail");
            _status.Text = _locale.Tr("fishing.donation.unseen");
            _action.Text = _locale.Tr("fishing.donation.status.locked");
            _action.Disabled = true;
            TooltipText = _detail.Text;
            return;
        }

        _icon.Modulate = Colors.White;
        _icon.SetState(_fish.ItemId, 0, 0, false);
        _name.Text = _locale.Tr(_fish.NameKey);
        _detail.Text = _locale.Tr(
            "fishing.donation.detail",
            WaterText(),
            ConditionText(),
            owned
        );

        if (donated)
        {
            _status.Text = _locale.Tr("fishing.donation.donated_status");
            _action.Text = _locale.Tr("fishing.donation.status.donated");
            _action.Disabled = true;
        }
        else if (available)
        {
            _status.Text = _locale.Tr("fishing.donation.available");
            _action.Text = _locale.Tr("fishing.donation.action.donate");
            _action.Disabled = false;
        }
        else
        {
            _status.Text = _locale.Tr("fishing.donation.need_item");
            _action.Text = _locale.Tr("fishing.donation.status.missing");
            _action.Disabled = true;
        }

        TooltipText = _detail.Text;
    }

    private static Color BorderColor(
        bool caught,
        bool donated,
        bool available
    )
    {
        if (donated)
        {
            return ThemeFactory.Mint;
        }

        if (available)
        {
            return ThemeFactory.Gold;
        }

        return caught ? new Color("#405b72") : new Color("#24364a");
    }

    private string WaterText() => _locale.Tr(
        _fish.WaterKind switch
        {
            FishingWaterKind.CrystalStream =>
                "fishing.water.crystal_stream",
            FishingWaterKind.MoonwaterWetlands =>
                "fishing.water.moonwater_wetlands",
            _ => "fishing.water.homestead_pond"
        }
    );

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
            return _locale.Tr("fishing.condition.weather", WeatherName());
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

    private static string FormatMinute(int minute)
    {
        var hour = minute / 60;
        var part = minute % 60;
        return $"{hour:00}:{part:00}";
    }
}
