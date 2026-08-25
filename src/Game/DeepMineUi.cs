using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class DeepMineOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _room;
    private readonly Label _health;
    private readonly Label _enemyName;
    private readonly Label _enemyHealth;
    private readonly Label _skills;
    private readonly Label _notice;
    private readonly TextureRect _enemyIcon;
    private readonly DeepMineRouteView _route;
    private readonly Button _attack;
    private readonly Button _dodge;
    private readonly Button _excavate;
    private readonly Button _advance;
    private readonly Button _leave;
    private readonly Dictionary<string, Button> _weaponButtons =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> _specializationButtons =
        new(StringComparer.Ordinal);
    private bool _defeatReported;

    public DeepMineOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.005f, 0.012f, 0.05f, 0.9f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(620, 332)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#091631fc"),
                ThemeFactory.Teal,
                2,
                8
            )
        );
        center.AddChild(panel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 3);
        panel.AddChild(root);
        var header = new HBoxContainer();
        header.AddChild(new TextureRect
        {
            Texture = DeepMineArt.AnchorIcon(),
            CustomMinimumSize = new Vector2(44, 44),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        });
        _title = ThemeFactory.Label(size: 20, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.VerticalAlignment = VerticalAlignment.Center;
        var headerStatus = new VBoxContainer();
        _room = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        _health = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        headerStatus.AddChild(_room);
        headerStatus.AddChild(_health);
        header.AddChild(_title);
        header.AddChild(headerStatus);
        root.AddChild(header);

        var body = new HBoxContainer();
        body.AddThemeConstantOverride("separation", 9);
        root.AddChild(body);
        var routePanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(196, 190)
        };
        _route = new DeepMineRouteView
        {
            CustomMinimumSize = new Vector2(188, 182)
        };
        routePanel.AddChild(_route);
        body.AddChild(routePanel);

        var encounter = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(205, 190)
        };
        encounter.AddThemeConstantOverride("separation", 3);
        _enemyIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(104, 72),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        _enemyName = ThemeFactory.Label(size: 12, color: ThemeFactory.Gold);
        _enemyName.HorizontalAlignment = HorizontalAlignment.Center;
        _enemyHealth = ThemeFactory.Label(size: 10);
        _enemyHealth.HorizontalAlignment = HorizontalAlignment.Center;
        encounter.AddChild(_enemyIcon);
        encounter.AddChild(_enemyName);
        encounter.AddChild(_enemyHealth);
        var weaponRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        foreach (var weapon in StarfallRuinsTrialCatalog.Weapons)
        {
            var button = ThemeFactory.Button("");
            button.CustomMinimumSize = new Vector2(60, 30);
            button.Icon = DeepMineArt.WeaponIcon(weapon.ItemId);
            button.ExpandIcon = true;
            button.ToggleMode = true;
            button.TooltipText = locale.Tr(
                DataCatalog.Item(weapon.ItemId).NameKey
            );
            button.Pressed += () => SelectWeapon(weapon.ItemId);
            weaponRow.AddChild(button);
            _weaponButtons[weapon.ItemId] = button;
        }
        encounter.AddChild(weaponRow);
        body.AddChild(encounter);

        var actions = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(190, 190)
        };
        actions.AddThemeConstantOverride("separation", 3);
        _attack = ActionButton(actions, Attack);
        _dodge = ActionButton(actions, Dodge);
        _excavate = ActionButton(actions, Excavate);
        _advance = ActionButton(actions, Advance);
        _skills = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _skills.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _skills.CustomMinimumSize = new Vector2(185, 34);
        actions.AddChild(_skills);
        var specializationRow = new GridContainer { Columns = 2 };
        actions.AddChild(specializationRow);
        foreach (var specializationId in SpecializationIds())
        {
            var button = ThemeFactory.Button("");
            button.CustomMinimumSize = new Vector2(91, 22);
            ThemeFactory.SetFontSize(button, 8);
            button.Pressed += () => ChooseSpecialization(specializationId);
            specializationRow.AddChild(button);
            _specializationButtons[specializationId] = button;
        }
        body.AddChild(actions);

        _notice = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(596, 14);
        root.AddChild(_notice);
        _leave = ThemeFactory.Button("");
        _leave.CustomMinimumSize = new Vector2(150, 22);
        _leave.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _leave.Pressed += Leave;
        root.AddChild(_leave);

        session.DeepMine.Changed += Refresh;
        session.Combat.Changed += Refresh;
        locale.LocaleChanged += Refresh;
        Refresh();
        _attack.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? ProgressChanged;
    public event Action? DefeatRequested;
    public event Action<ImmediateFeedbackDomain, ActionResult>?
        FeedbackRequested;

    public override void _Process(double delta)
    {
        _session.AdvanceDeepMineCombat((float)Math.Min(delta, 0.05));
        if (!_defeatReported && _session.Combat.IsDefeated)
        {
            _defeatReported = true;
            DefeatRequested?.Invoke();
        }
    }

    public override void _ExitTree()
    {
        _session.DeepMine.Changed -= Refresh;
        _session.Combat.Changed -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private static Button ActionButton(Container parent, Action action)
    {
        var button = ThemeFactory.Button("");
        button.CustomMinimumSize = new Vector2(185, 24);
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    private void SelectWeapon(string itemId)
    {
        if (_session.Inventory.Count(itemId) <= 0)
        {
            _notice.Text = _locale.Tr("combat.weapon.not_owned");
            return;
        }

        _session.Inventory.PromoteToHotbar(itemId);
        _notice.Text = _locale.Tr(
            "combat.weapon.equipped",
            _locale.Tr(DataCatalog.Item(itemId).NameKey)
        );
        Refresh();
    }

    private void Attack()
    {
        var result = _session.AttackDeepMineEnemy();
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.DamageTaken > 0 || result.PlayerDefeated)
        {
            FeedbackRequested?.Invoke(
                ImmediateFeedbackDomain.Damage,
                ActionResult.Success(messageKey: result.MessageKey)
            );
        }
        if (result.Succeeded)
        {
            ProgressChanged?.Invoke();
        }
        Refresh();
    }

    private void Dodge()
    {
        var result = _session.DodgeInDeepMine();
        _notice.Text = _locale.Tr(result.MessageKey);
        FeedbackRequested?.Invoke(
            ImmediateFeedbackDomain.Dodge,
            new ActionResult(result.Succeeded, MessageKey: result.MessageKey)
        );
        if (result.Succeeded)
        {
            ProgressChanged?.Invoke();
        }
        Refresh();
    }

    private void Excavate()
    {
        var result = _session.ExcavateDeepMineRoom();
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            ProgressChanged?.Invoke();
        }
        Refresh();
    }

    private void Advance()
    {
        var result = _session.AdvanceDeepMineRoom();
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            ProgressChanged?.Invoke();
        }
        if (!_session.DeepMine.Active)
        {
            CloseRequested?.Invoke();
            return;
        }
        Refresh();
    }

    private void ChooseSpecialization(string specializationId)
    {
        var kind = specializationId.StartsWith(
            "mining_",
            StringComparison.Ordinal
        )
            ? AdventureSkillKind.CrystalMining
            : AdventureSkillKind.Nightwatch;
        var result = _session.ChooseAdventureSpecialization(
            kind,
            specializationId
        );
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            ProgressChanged?.Invoke();
        }
        Refresh();
    }

    private void Leave()
    {
        _session.DeepMine.Leave();
        CloseRequested?.Invoke();
    }

    private void Refresh()
    {
        var snapshot = _session.DeepMine.Snapshot();
        _title.Text = _locale.Tr("deep_mine.title");
        _room.Text = _locale.Tr(
            "deep_mine.room",
            snapshot.Room,
            DeepMineCatalog.MaximumRoom,
            snapshot.StableAnchorRoom
        );
        _health.Text = _locale.Tr(
            "combat.hud.health",
            _session.Combat.CurrentHealth,
            CombatSystem.MaxHealth
        );
        _route.SetSnapshot(snapshot);
        if (!snapshot.Active)
        {
            return;
        }

        _enemyIcon.Texture = DeepMineArt.EnemyIcon(snapshot.EnemyId);
        _enemyName.Text = _locale.Tr(
            $"enemy.{snapshot.EnemyId["enemy_".Length..]}.name"
        );
        _enemyHealth.Text = _locale.Tr(
            "deep_mine.enemy_health",
            snapshot.EnemyHealth,
            snapshot.EnemyMaxHealth
        );
        _attack.Text = _locale.Tr("deep_mine.action.attack");
        _dodge.Text = _locale.Tr("deep_mine.action.dodge");
        _excavate.Text = _locale.Tr("deep_mine.action.excavate");
        _advance.Text = _locale.Tr("deep_mine.action.advance");
        _leave.Text = _locale.Tr("deep_mine.action.leave");
        _attack.Disabled = snapshot.EnemyDefeated ||
            !StarfallRuinsTrialCatalog.TryWeapon(
                _session.Inventory.Selected.ItemId,
                out _
            );
        _dodge.Disabled = snapshot.EnemyDefeated;
        _excavate.Disabled = !snapshot.EnemyDefeated ||
            snapshot.RoomExcavated;
        _advance.Disabled = !snapshot.RoomExcavated;

        foreach (var pair in _weaponButtons)
        {
            pair.Value.Disabled = _session.Inventory.Count(pair.Key) <= 0;
            pair.Value.ButtonPressed =
                _session.Inventory.Selected.ItemId == pair.Key;
        }

        var mining = _session.DeepMine.CrystalMiningSkill;
        var nightwatch = _session.DeepMine.NightwatchSkill;
        _skills.Text = _locale.Tr(
            "deep_mine.skills",
            mining.Level,
            mining.Experience,
            nightwatch.Level,
            nightwatch.Experience
        );
        foreach (var pair in _specializationButtons)
        {
            var skill = pair.Key.StartsWith("mining_", StringComparison.Ordinal)
                ? mining
                : nightwatch;
            var chosen = skill.SpecializationId == pair.Key;
            pair.Value.Text = _locale.Tr(
                $"{pair.Key}.short"
            );
            pair.Value.Disabled = chosen || !skill.CanChooseSpecialization;
        }
    }

    private static IReadOnlyList<string> SpecializationIds() =>
    [
        AdventureSkillCatalog.ForgekeeperSpecializationId,
        AdventureSkillCatalog.GemseekerSpecializationId,
        AdventureSkillCatalog.GuardianSpecializationId,
        AdventureSkillCatalog.SpellbladeSpecializationId
    ];
}

internal sealed partial class DeepMineRouteView : Control
{
    private DeepMineSnapshot _snapshot = new(
        false,
        0,
        0,
        0,
        0,
        string.Empty,
        0,
        0,
        false,
        false,
        string.Empty,
        string.Empty
    );

    public void SetSnapshot(DeepMineSnapshot snapshot)
    {
        _snapshot = snapshot;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, Size.X, Size.Y), new Color("#071126"));
        var points = new List<Vector2>();
        for (var room = 1; room <= DeepMineCatalog.MaximumRoom; room++)
        {
            var column = (room - 1) % 3;
            var row = (room - 1) / 3;
            var point = new Vector2(
                32 + column * 62 + (row % 2 == 1 ? 12 : 0),
                24 + row * 48
            );
            points.Add(point);
            if (room > 1)
            {
                DrawLine(points[^2], point, new Color("#31516f"), 2);
            }
        }

        for (var index = 0; index < points.Count; index++)
        {
            var room = index + 1;
            var reached = room <= _snapshot.DeepestRoom;
            var anchor = room % DeepMineCatalog.AnchorInterval == 0;
            var color = new Color("#39445f");
            if (reached)
            {
                color = ThemeFactory.Mint;
            }
            if (room == _snapshot.Room)
            {
                color = ThemeFactory.Gold;
            }
            DrawCircle(points[index], anchor ? 10 : 7, color);
            if (anchor)
            {
                DrawArc(
                    points[index],
                    13,
                    0,
                    Mathf.Tau,
                    20,
                    new Color(color, 0.55f),
                    1
                );
            }
        }
    }
}
