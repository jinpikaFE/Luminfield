using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class GleamrisePlantingOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly List<string> _selected = [];
    private readonly Label _title;
    private readonly Label _summary;
    private readonly GridContainer _seedButtons;
    private readonly GridContainer _plots;
    private readonly Label _status;
    private readonly Button _start;
    private readonly Button _close;

    public GleamrisePlantingOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.008f, 0.014f, 0.065f, 0.88f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(520, 330)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#08152ffa"),
                ThemeFactory.Mint,
                2,
                8
            )
        );
        center.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 5);
        panel.AddChild(column);

        var header = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(484, 34)
        };
        header.AddThemeConstantOverride("separation", 8);
        header.AddChild(SeedIcon(
            DataCatalog.DawnlaceSeedId,
            new Vector2(30, 30)
        ));
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Gold);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(_title);
        column.AddChild(header);

        _summary = ThemeFactory.Label(size: 9, color: ThemeFactory.Ink);
        _summary.HorizontalAlignment = HorizontalAlignment.Center;
        _summary.CustomMinimumSize = new Vector2(484, 30);
        _summary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_summary);

        _seedButtons = new GridContainer
        {
            Columns = 2,
            CustomMinimumSize = new Vector2(484, 82)
        };
        _seedButtons.AddThemeConstantOverride("h_separation", 4);
        _seedButtons.AddThemeConstantOverride("v_separation", 4);
        column.AddChild(_seedButtons);

        _plots = new GridContainer
        {
            Columns = 4,
            CustomMinimumSize = new Vector2(484, 86)
        };
        _plots.AddThemeConstantOverride("h_separation", 5);
        _plots.AddThemeConstantOverride("v_separation", 4);
        column.AddChild(_plots);

        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(484, 18);
        column.AddChild(_status);

        var actions = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actions.AddThemeConstantOverride("separation", 8);
        _start = ThemeFactory.Button("");
        _start.CustomMinimumSize = new Vector2(210, 25);
        _start.Pressed += StartChallenge;
        actions.AddChild(_start);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(150, 25);
        _close.Pressed += () => CloseRequested?.Invoke();
        actions.AddChild(_close);
        column.AddChild(actions);

        session.Changed += Refresh;
        locale.LocaleChanged += Refresh;
        Refresh();
    }

    public event Action? CloseRequested;

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private void Refresh()
    {
        var year = CalendarSystem.YearNumber(_session.Clock.Day);
        var attempt = _session.Festival.PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            year
        );
        var result = _session.Festival.ResultFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            year
        );
        _title.Text = _locale.Tr("festival.gleamrise.activity.title");
        _close.Text = _locale.Tr("menu.back");
        RebuildSeedButtons(attempt, result);
        RebuildPlots(attempt, result);

        if (result is not null)
        {
            _summary.Text = _locale.Tr(
                "festival.gleamrise.activity.result",
                result.Score,
                _locale.Tr(AwardNameKey(result.AwardId)),
                _session.Festival.BloomTokens
            );
            _start.Text = _locale.Tr(
                "festival.gleamrise.activity.completed"
            );
            _start.Disabled = true;
            return;
        }

        if (attempt is not null)
        {
            var remaining = _session.Festival.PlantingMinutesRemaining(
                year,
                _session.Clock.MinuteOfDay
            );
            var planted = attempt.Plantings.Count;
            _summary.Text = _locale.Tr(
                "festival.gleamrise.activity.active",
                planted,
                remaining,
                _session.Festival.CurrentPlantingScore(
                    year,
                    _session.Clock.MinuteOfDay
                )
            );
            _start.Text = _locale.Tr(
                "festival.gleamrise.activity.in_progress"
            );
            _start.Disabled = true;
            return;
        }

        _summary.Text = _locale.Tr(
            "festival.gleamrise.activity.instruction",
            _selected.Count
        );
        _start.Text = _locale.Tr("festival.gleamrise.activity.start");
        _start.Disabled = !_session.CheckStartGleamriseChallenge(
            _selected
        ).CanStart;
    }

    private void RebuildSeedButtons(
        FestivalPlantingAttemptSave? attempt,
        FestivalYearResultSave? result
    )
    {
        Clear(_seedButtons);
        foreach (var seedId in FestivalCatalog.GleamriseChallengeSeedIds)
        {
            var chosen = attempt is not null
                ? attempt.SelectedSeedItemIds.Contains(
                    seedId,
                    StringComparer.Ordinal
                )
                : result is not null
                    ? result.Plantings.Any(planting =>
                        planting.SeedItemId == seedId)
                    : _selected.Contains(seedId, StringComparer.Ordinal);
            var active = attempt?.ActiveSeedItemId == seedId;
            var item = DataCatalog.Item(seedId);
            var seedName = item.CropId is { } cropId
                ? _locale.Tr(DataCatalog.Crop(cropId).NameKey)
                : _locale.Tr(item.NameKey);
            var label = active
                ? _locale.Tr(
                    "festival.gleamrise.activity.seed.active",
                    seedName
                )
                : chosen
                    ? _locale.Tr(
                        "festival.gleamrise.activity.seed.selected",
                        seedName
                    )
                    : seedName;
            var button = ThemeFactory.Button(label);
            button.CustomMinimumSize = new Vector2(240, 38);
            button.ClipText = true;
            button.AddThemeFontSizeOverride("font_size", 10);
            button.Icon = SeedAtlas(seedId);
            button.ExpandIcon = true;
            button.Disabled = result is not null ||
                (attempt is not null && !chosen);
            button.Pressed += () => SelectSeed(seedId, attempt);
            _seedButtons.AddChild(button);
        }
    }

    private void RebuildPlots(
        FestivalPlantingAttemptSave? attempt,
        FestivalYearResultSave? result
    )
    {
        Clear(_plots);
        var plantings = (result?.Plantings ?? attempt?.Plantings ?? [])
            .ToDictionary(
                entry => entry.PlotId,
                entry => entry.SeedItemId,
                StringComparer.Ordinal
            );
        foreach (var plotId in GleamrisePlantingFestivalLayout.PlotIds)
        {
            var slot = new PanelContainer
            {
                CustomMinimumSize = new Vector2(116, 24)
            };
            slot.AddThemeStyleboxOverride(
                "panel",
                ThemeFactory.Box(
                    new Color("#101c3ee8"),
                    plantings.ContainsKey(plotId)
                        ? ThemeFactory.Mint
                        : ThemeFactory.MutedInk,
                    1,
                    4
                )
            );
            if (plantings.TryGetValue(plotId, out var seedId))
            {
                var icon = SeedIcon(seedId, new Vector2(22, 22));
                slot.AddChild(icon);
            }
            _plots.AddChild(slot);
        }
    }

    private void SelectSeed(
        string seedId,
        FestivalPlantingAttemptSave? attempt
    )
    {
        if (attempt is not null)
        {
            var selected = _session.SelectGleamriseSeed(seedId);
            _status.Text = _locale.Tr(selected.MessageKey);
            Refresh();
            return;
        }

        if (_selected.Remove(seedId))
        {
            _status.Text = string.Empty;
            Refresh();
            return;
        }

        if (_selected.Count >= 3)
        {
            _status.Text = _locale.Tr(
                "festival.gleamrise.activity.select_limit"
            );
            return;
        }

        _selected.Add(seedId);
        _status.Text = string.Empty;
        Refresh();
    }

    private void StartChallenge()
    {
        var result = _session.StartGleamriseChallenge(_selected);
        _status.Text = _locale.Tr(result.MessageKey);
        Refresh();
    }

    private static string AwardNameKey(string awardId) => awardId switch
    {
        FestivalCatalog.GleamriseStarfieldCrownAwardId =>
            "festival.gleamrise.award.crown",
        FestivalCatalog.GleamriseBloomWreathAwardId =>
            "festival.gleamrise.award.bloom",
        _ => "festival.gleamrise.award.sprout"
    };

    private static AtlasTexture? SeedAtlas(string seedId)
    {
        if (!HotbarSlotContent.TryGetIconRegion(
                seedId,
                out var texture,
                out var region
            ))
        {
            return null;
        }

        return new AtlasTexture { Atlas = texture, Region = region };
    }

    private static TextureRect SeedIcon(string seedId, Vector2 size) => new()
    {
        Texture = SeedAtlas(seedId),
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };

    private static void Clear(Node container)
    {
        foreach (var child in container.GetChildren())
        {
            child.QueueFree();
        }
    }
}

public sealed partial class GleamriseSeedExchangeOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _balance;
    private readonly Label _description;
    private readonly VBoxContainer _offers;
    private readonly Label _status;
    private readonly Button _close;

    public GleamriseSeedExchangeOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.008f, 0.014f, 0.065f, 0.88f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(430, 304)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#08152ffa"),
                ThemeFactory.Mint,
                2,
                8
            )
        );
        center.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);
        var header = new HBoxContainer();
        header.AddChild(GleamrisePlantingOverlayIcon());
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Gold);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(_title);
        _balance = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        header.AddChild(_balance);
        column.AddChild(header);
        _description = ThemeFactory.Label(size: 9, color: ThemeFactory.Ink);
        _description.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_description);
        _offers = new VBoxContainer();
        _offers.AddThemeConstantOverride("separation", 4);
        column.AddChild(_offers);
        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(390, 18);
        column.AddChild(_status);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(160, 26);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);
        session.Changed += Refresh;
        locale.LocaleChanged += Refresh;
        Refresh();
    }

    public event Action? CloseRequested;

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private void Refresh()
    {
        _title.Text = _locale.Tr("festival.gleamrise.exchange.title");
        _balance.Text = _locale.Tr(
            "festival.gleamrise.token_balance",
            _session.Festival.BloomTokens
        );
        _description.Text = _locale.Tr(
            "festival.gleamrise.exchange.description"
        );
        _close.Text = _locale.Tr("menu.back");
        foreach (var child in _offers.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var offer in FestivalCatalog.GleamriseOffers.Values)
        {
            var item = DataCatalog.Item(offer.ItemId);
            var button = ThemeFactory.Button(_locale.Tr(
                "festival.gleamrise.exchange.offer",
                _locale.Tr(item.NameKey),
                offer.Count,
                offer.ScripCost
            ));
            button.CustomMinimumSize = new Vector2(390, 30);
            button.Disabled = !_session.CheckGleamriseSeedPurchase(offer.Id)
                .CanPurchase;
            button.Icon = SeedAtlas(offer.ItemId);
            button.ExpandIcon = true;
            button.Pressed += () => Purchase(offer.Id);
            _offers.AddChild(button);
        }
    }

    private void Purchase(string offerId)
    {
        var result = _session.BuyGleamriseSeeds(offerId);
        _status.Text = _locale.Tr(result.MessageKey);
        Refresh();
    }

    private static TextureRect GleamrisePlantingOverlayIcon() => new()
    {
        Texture = SeedAtlas(DataCatalog.DawnlaceSeedId),
        CustomMinimumSize = new Vector2(34, 34),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };

    private static AtlasTexture? SeedAtlas(string seedId)
    {
        if (!HotbarSlotContent.TryGetIconRegion(
                seedId,
                out var texture,
                out var region
            ))
        {
            return null;
        }

        return new AtlasTexture { Atlas = texture, Region = region };
    }
}
