using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class FishingMinigameOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _fish;
    private readonly Label _hint;
    private readonly ProgressBar _progress;
    private readonly ProgressBar _tension;
    private readonly FishingChallengeView _challenge;
    private double _terminalDelay;
    private bool _finished;

    public FishingMinigameOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.01f, 0.025f, 0.09f, 0.78f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(430, 278)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0a1839fa"),
                ThemeFactory.Teal,
                2,
                8
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);

        var header = new HBoxContainer();
        var icon = new TextureRect
        {
            Texture = FishingGearArt.HookedFishIcon(),
            CustomMinimumSize = new Vector2(42, 42),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        _title = ThemeFactory.Label(size: 21, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.VerticalAlignment = VerticalAlignment.Center;
        _fish = ThemeFactory.Label(size: 12, color: ThemeFactory.Gold);
        _fish.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(icon);
        header.AddChild(_title);
        header.AddChild(_fish);
        column.AddChild(header);

        _challenge = new FishingChallengeView
        {
            CustomMinimumSize = new Vector2(400, 92)
        };
        column.AddChild(_challenge);

        _progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(400, 18)
        };
        _tension = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(400, 18)
        };
        column.AddChild(_progress);
        column.AddChild(_tension);

        _hint = ThemeFactory.Label(size: 11, color: ThemeFactory.MutedInk);
        _hint.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_hint);
        Refresh();
    }

    public event Action? Finished;

    public override void _Process(double delta)
    {
        var snapshot = _session.FishingMinigame.Snapshot();
        if (snapshot.Status == FishingChallengeStatus.Active)
        {
            snapshot = _session.AdvanceFishingChallenge(
                (float)delta,
                Input.IsActionPressed(InputSetup.Interact)
            );
            Refresh(snapshot);
            return;
        }

        if (_finished)
        {
            return;
        }

        _terminalDelay += delta;
        Refresh(snapshot);
        if (_terminalDelay < 0.45)
        {
            return;
        }

        _finished = true;
        Finished?.Invoke();
    }

    private void Refresh() => Refresh(_session.FishingMinigame.Snapshot());

    private void Refresh(FishingChallengeSnapshot snapshot)
    {
        _title.Text = _locale.Tr("fishing.minigame.title");
        _fish.Text = string.IsNullOrWhiteSpace(snapshot.FishId)
            ? string.Empty
            : _locale.Tr(DataCatalog.Fishes[snapshot.FishId].NameKey);
        _progress.Value = snapshot.Progress * 100;
        _tension.Value = snapshot.Tension * 100;
        _challenge.SetSnapshot(snapshot);
        _hint.Text = snapshot.Status switch
        {
            FishingChallengeStatus.Succeeded =>
                _locale.Tr("fishing.minigame.success"),
            FishingChallengeStatus.Failed =>
                _locale.Tr("fishing.minigame.failure"),
            _ => _locale.Tr("fishing.minigame.hint")
        };
    }
}

internal sealed partial class FishingChallengeView : Control
{
    private FishingChallengeSnapshot _snapshot = new(
        FishingChallengeStatus.Idle,
        string.Empty,
        0,
        0.5f,
        0.5f,
        0.2f,
        0,
        1,
        0
    );

    public void SetSnapshot(FishingChallengeSnapshot snapshot)
    {
        _snapshot = snapshot;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var track = new Rect2(16, 22, Math.Max(10, Size.X - 32), 48);
        DrawStyleBox(
            ThemeFactory.CompactBox(
                new Color("#071126"),
                new Color("#31516f"),
                1,
                3
            ),
            track
        );
        for (var index = 1; index < 8; index++)
        {
            var x = track.Position.X + track.Size.X * index / 8f;
            DrawLine(
                new Vector2(x, track.Position.Y + 6),
                new Vector2(x, track.End.Y - 6),
                new Color("#203c55"),
                1
            );
        }

        var zoneWidth = track.Size.X * _snapshot.CatchZoneSize;
        var hookX = track.Position.X + track.Size.X * _snapshot.HookPosition;
        DrawRect(
            new Rect2(
                hookX - zoneWidth / 2,
                track.Position.Y + 5,
                zoneWidth,
                track.Size.Y - 10
            ),
            new Color(ThemeFactory.Mint, 0.25f)
        );
        DrawLine(
            new Vector2(hookX, track.Position.Y + 3),
            new Vector2(hookX, track.End.Y - 3),
            ThemeFactory.Gold,
            3
        );

        var fishX = track.Position.X + track.Size.X * _snapshot.FishPosition;
        DrawCircle(
            new Vector2(fishX, track.GetCenter().Y),
            8,
            ThemeFactory.Teal
        );
        DrawColoredPolygon(
            [
                new Vector2(fishX - 8, track.GetCenter().Y),
                new Vector2(fishX - 16, track.GetCenter().Y - 7),
                new Vector2(fishX - 16, track.GetCenter().Y + 7)
            ],
            ThemeFactory.Teal
        );
    }
}

public sealed partial class FishingGearOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _summary;
    private readonly Label _notice;
    private readonly TextureRect _rodIcon;
    private readonly Button _upgradeRod;
    private readonly Button _close;
    private readonly Dictionary<string, Button> _offerButtons =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> _equipButtons =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> _specializationButtons =
        new(StringComparer.Ordinal);

    public FishingGearOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.01f, 0.02f, 0.08f, 0.82f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(610, 338)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0a1736fb"),
                ThemeFactory.Violet,
                2,
                8
            )
        );
        center.AddChild(panel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        panel.AddChild(root);
        var header = new HBoxContainer();
        _rodIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(48, 48),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        _title = ThemeFactory.Label(size: 21, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.VerticalAlignment = VerticalAlignment.Center;
        _summary = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        _summary.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_rodIcon);
        header.AddChild(_title);
        header.AddChild(_summary);
        root.AddChild(header);

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 10);
        root.AddChild(columns);
        var shop = Section("fishing.gear.shop", columns);
        var loadout = Section("fishing.gear.loadout", columns);

        _upgradeRod = CompactButton(shop);
        _upgradeRod.Pressed += () => Apply(_session.UpgradeFishingRod());
        foreach (var offer in FishingProgressionCatalog.GearOffers)
        {
            var button = CompactButton(shop);
            button.Pressed += () => Apply(
                _session.PurchaseFishingGear(offer.ItemId)
            );
            _offerButtons[offer.ItemId] = button;
        }

        foreach (var itemId in FishingProgressionCatalog.BaitItemIds.Concat(
                     FishingProgressionCatalog.BobberItemIds
                 ))
        {
            var button = CompactButton(loadout);
            button.Pressed += () => Equip(itemId);
            _equipButtons[itemId] = button;
        }

        foreach (var specializationId in new[]
                 {
                     FishingProgressionCatalog.CurrentListenerSpecializationId,
                     FishingProgressionCatalog.DeepThreaderSpecializationId
                 })
        {
            var button = CompactButton(loadout);
            button.Icon = FishingGearArt.SpecializationIcon(specializationId);
            button.ExpandIcon = true;
            button.Pressed += () => Apply(
                _session.ChooseFishingSpecialization(specializationId)
            );
            _specializationButtons[specializationId] = button;
        }

        _notice = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(580, 18);
        root.AddChild(_notice);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(150, 28);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        root.AddChild(_close);

        session.Changed += Refresh;
        locale.LocaleChanged += Refresh;
        Refresh();
        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? GearChanged;

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private VBoxContainer Section(string titleKey, HBoxContainer parent)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(285, 210),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        parent.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 3);
        panel.AddChild(column);
        var title = ThemeFactory.Label(
            _locale.Tr(titleKey),
            13,
            ThemeFactory.Mint
        );
        title.SetMeta("title_key", titleKey);
        column.AddChild(title);
        return column;
    }

    private static Button CompactButton(Container parent)
    {
        var button = ThemeFactory.Button("");
        button.CustomMinimumSize = new Vector2(270, 25);
        button.AddThemeFontSizeOverride("font_size", 10);
        parent.AddChild(button);
        return button;
    }

    private void Equip(string itemId)
    {
        var result = FishingProgressionCatalog.BaitItemIds.Contains(itemId)
            ? _session.EquipFishingBait(itemId)
            : _session.EquipFishingBobber(itemId);
        Apply(result);
    }

    private void Apply(ActionResult result)
    {
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            GearChanged?.Invoke();
        }
        Refresh();
    }

    private void Refresh()
    {
        var progression = _session.FishingProgression;
        _title.Text = _locale.Tr("fishing.gear.title");
        _summary.Text = _locale.Tr(
            "fishing.gear.summary",
            progression.Level,
            progression.Experience
        );
        _rodIcon.Texture = FishingGearArt.RodTierIcon(
            progression.RodTierId
        );
        _upgradeRod.Text = _locale.Tr("fishing.rod.upgrade_action");

        foreach (var offer in FishingProgressionCatalog.GearOffers)
        {
            var item = DataCatalog.Item(offer.ItemId);
            var button = _offerButtons[offer.ItemId];
            button.Text = _locale.Tr(
                "fishing.gear.offer",
                _locale.Tr(item.NameKey),
                offer.Quantity,
                offer.CoinCost,
                offer.RequiredLevel
            );
            button.Disabled = progression.Level < offer.RequiredLevel ||
                (offer.Kind == FishingGearOfferKind.Bobber &&
                 progression.OwnsBobber(offer.ItemId));
        }

        foreach (var pair in _equipButtons)
        {
            var equipped = pair.Key == progression.EquippedBaitId ||
                pair.Key == progression.EquippedBobberId;
            pair.Value.Text = _locale.Tr(
                equipped
                    ? "fishing.gear.equipped"
                    : "fishing.gear.equip",
                _locale.Tr(DataCatalog.Item(pair.Key).NameKey)
            );
            var available = FishingProgressionCatalog.BaitItemIds.Contains(
                pair.Key
            )
                ? _session.Inventory.Count(pair.Key) > 0
                : progression.OwnsBobber(pair.Key);
            pair.Value.Disabled = equipped || !available;
        }

        foreach (var pair in _specializationButtons)
        {
            var chosen = progression.SpecializationId == pair.Key;
            pair.Value.Text = _locale.Tr(
                chosen
                    ? "fishing.skill.chosen"
                    : "fishing.skill.choose",
                _locale.Tr($"{pair.Key}.name")
            );
            pair.Value.Disabled = chosen ||
                !progression.CanChooseSpecialization;
        }

        _close.Text = _locale.Tr("menu.resume");
    }
}
