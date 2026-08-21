using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class StarfallRuinsTrialView : Node2D
{
    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;
    private readonly StarfallRuinsCombatLayer _combatLayer;
    private bool _defeatReported;

    public StarfallRuinsTrialView(GameSession session, LocaleService locale)
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new StarfallRuinsTrialBackdrop());

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideStarfallRuinsTrial
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(StarfallRuinsTrialLayout.SafeArrivalCell),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.StarfallRuinsTrial
            );

        _combatLayer = new StarfallRuinsCombatLayer(
            session,
            () => _player.Position,
            OnCombatNotice
        )
        {
            ZIndex = 7
        };
        AddChild(_combatLayer);
        AddChild(_player);

        _cursor = new TargetCursor(ResolveTargetPreview, locale)
        {
            ZIndex = 20
        };
        AddChild(_cursor);
        AddChild(new StarfallCombatHud(session, locale)
        {
            ZIndex = 90
        });
    }

    public bool ControlsEnabled
    {
        get => _player.ControlsEnabled;
        set
        {
            _player.ControlsEnabled = value;
            _cursor.Visible = value;
        }
    }

    public event Action? ExitRequested;
    public event Action? DefeatRequested;
    public event Action? ProgressChanged;
    public event Action<string>? NoticeRequested;
    public event Action? StepRequested
    {
        add => _player.Stepped += value;
        remove => _player.Stepped -= value;
    }

    public override void _Process(double delta)
    {
        _session.AdvanceStarfallCombat((float)delta);
        if (!_defeatReported && _session.Combat.IsDefeated)
        {
            _defeatReported = true;
            ControlsEnabled = false;
            DefeatRequested?.Invoke();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!ControlsEnabled)
        {
            return;
        }

        if (@event.IsActionPressed(InputSetup.CombatDodge))
        {
            TryDodge();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!@event.IsActionPressed(InputSetup.Interact))
        {
            return;
        }

        if (ResolveAttackTarget() is { } enemy)
        {
            var result = _session.AttackStarfallEnemy(
                enemy.InstanceId,
                enemy.Cell
            );
            NoticeRequested?.Invoke(result.MessageKey);
            if (result.Succeeded)
            {
                ProgressChanged?.Invoke();
                _combatLayer.ShowPlayerSlash(
                    _player.Position,
                    _player.Facing
                );
                if (!string.IsNullOrEmpty(result.ClearedRoomId))
                {
                    NoticeRequested?.Invoke("combat.room.cleared");
                }
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        var actual = ResolveActualTarget(
            _player.TargetCell,
            _player.CurrentCell
        );
        if (actual is not { } target)
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        var action = _session.UseSelected(target);
        if (!action.Succeeded)
        {
            NoticeRequested?.Invoke(action.MessageKey);
        }
        else if (target == StarfallRuinsTrialLayout.ExitCell)
        {
            ExitRequested?.Invoke();
        }
        else
        {
            ProgressChanged?.Invoke();
            NoticeRequested?.Invoke(action.MessageKey);
        }
        GetViewport().SetInputAsHandled();
    }

    private void TryDodge()
    {
        var available = _player.AvailableForwardDisplacement(
            CombatSystem.DodgeDistancePixels
        );
        if (available <= 0.01f)
        {
            NoticeRequested?.Invoke("combat.enemy_move.blocked");
            return;
        }

        var result = _session.DodgeInStarfallRuinsTrial();
        if (!result.Succeeded)
        {
            NoticeRequested?.Invoke(result.MessageKey);
            return;
        }

        _player.TryDisplaceForward(Math.Min(available, result.DistancePixels));
        _combatLayer.ShowDodgeSpark(_player.Position);
    }

    private TargetPreview ResolveTargetPreview()
    {
        if (ResolveAttackTarget() is { } enemy)
        {
            return _session.PreviewSelectedTarget(enemy.Cell) with
            {
                Target = enemy.Cell
            };
        }

        var target = _player.TargetCell;
        return ResolveActualTarget(target, _player.CurrentCell) is { } actual
            ? _session.PreviewSelectedTarget(actual)
            : TargetPreview.Neutral(target);
    }

    private StarfallTrialEnemySnapshot? ResolveAttackTarget()
    {
        var facing = new Vector2(_player.Facing.X, _player.Facing.Y);
        return _session.VisibleStarfallTrialEnemies
            .Where(enemy => !enemy.Defeated)
            .Select(enemy =>
            {
                var delta = new Vector2(
                    enemy.CurrentX,
                    enemy.CurrentY
                ) - _player.Position;
                return new
                {
                    Enemy = enemy,
                    Forward = delta.Dot(facing),
                    Side = Math.Abs(delta.Cross(facing)),
                    Distance = delta.Length()
                };
            })
            .Where(candidate => candidate.Forward >= 0 &&
                candidate.Forward <=
                    StarfallRuinsTrialCatalog.MoonsteelShortblade.RangePixels &&
                candidate.Side <= 12)
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.Enemy.InstanceId,
                StringComparer.Ordinal)
            .Select(candidate => candidate.Enemy)
            .FirstOrDefault();
    }

    private GridPosition? ResolveActualTarget(
        GridPosition target,
        GridPosition player
    )
    {
        var fixedTargets = new List<GridPosition>
        {
            StarfallRuinsTrialLayout.ExitCell,
            StarfallRuinsTrialLayout.WeaponRackCell,
            StarfallRuinsTrialLayout.FirstSealCell,
            StarfallRuinsTrialLayout.SecondSealCell
        };
        fixedTargets.AddRange(
            StarfallRuinsTrialCatalog.Artifacts.Select(artifact => artifact.Cell)
        );

        if (fixedTargets.Contains(target))
        {
            return target;
        }

        return fixedTargets
            .Where(cell => IsAdjacent(player, cell))
            .OrderBy(cell => Distance(cell, target))
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .Cast<GridPosition?>()
            .FirstOrDefault();
    }

    private bool CanOccupy(Vector2 worldPosition)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        );
        return _session.IsStarfallRuinsTrialCellWalkable(cell);
    }

    private void OnCombatNotice(string key)
    {
        NoticeRequested?.Invoke(key);
    }

    private static bool IsAdjacent(GridPosition first, GridPosition second) =>
        Distance(first, second) == 1;

    private static int Distance(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);
}

internal sealed partial class StarfallRuinsTrialBackdrop : Node2D
{
    private static readonly Texture2D Background = GD.Load<Texture2D>(
        "res://assets/generated/activities/combat/starfall_ruins_trial_interior.png"
    );

    public StarfallRuinsTrialBackdrop()
    {
        ZIndex = -100;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            Background,
            new Rect2(0, 0, 640, 360),
            new Rect2(0, 80, 1536, 864)
        );
    }
}

internal sealed partial class StarfallRuinsCombatLayer : Node2D
{
    private sealed class EnemyRuntime
    {
        public float Cooldown;
        public float Windup;
        public bool Attacking;
        public bool Moving;
        public RuinsSpriteFacing Facing = RuinsSpriteFacing.Down;
    }

    private sealed class Projectile
    {
        public required string EnemyInstanceId { get; init; }
        public required Vector2 Direction { get; init; }
        public Vector2 Position { get; set; }
    }

    private readonly GameSession _session;
    private readonly Func<Vector2> _playerPosition;
    private readonly Action<string> _notice;
    private readonly Dictionary<string, EnemyRuntime> _runtime =
        new(StringComparer.Ordinal);
    private readonly List<Projectile> _projectiles = [];
    private float _slashRemaining;
    private Vector2 _slashPosition;
    private Vector2I _slashFacing;
    private float _dodgeSparkRemaining;
    private Vector2 _dodgeSparkPosition;
    private double _animationTime;

    public StarfallRuinsCombatLayer(
        GameSession session,
        Func<Vector2> playerPosition,
        Action<string> notice
    )
    {
        _session = session;
        _playerPosition = playerPosition;
        _notice = notice;
        TextureFilter = TextureFilterEnum.Nearest;
        foreach (var instance in StarfallRuinsTrialCatalog.EnemyInstances)
        {
            _runtime[instance.InstanceId] = new EnemyRuntime
            {
                Cooldown = InitialCooldown(instance.InstanceId)
            };
        }
        session.StarfallRuinsTrial.Changed += QueueRedraw;
        session.Combat.Changed += QueueRedraw;
    }

    public override void _Process(double delta)
    {
        var seconds = (float)Math.Min(delta, 0.05);
        _animationTime += delta;
        _slashRemaining = Math.Max(0, _slashRemaining - seconds);
        _dodgeSparkRemaining = Math.Max(0, _dodgeSparkRemaining - seconds);
        if (!_session.Combat.IsDefeated)
        {
            AdvanceEnemies(seconds);
            AdvanceProjectiles(seconds);
        }
        QueueRedraw();
    }

    public void ShowPlayerSlash(Vector2 position, Vector2I facing)
    {
        _slashPosition = position;
        _slashFacing = facing;
        _slashRemaining = 0.18f;
    }

    public void ShowDodgeSpark(Vector2 position)
    {
        _dodgeSparkPosition = position;
        _dodgeSparkRemaining = 0.2f;
    }

    public override void _Draw()
    {
        DrawStaticEntities();
        foreach (var enemy in _session.StarfallRuinsTrial.Enemies()
                     .Where(enemy => !enemy.Defeated))
        {
            DrawEnemy(enemy, _runtime[enemy.InstanceId]);
        }
        DrawCombatEffects();
    }

    public override void _ExitTree()
    {
        _session.StarfallRuinsTrial.Changed -= QueueRedraw;
        _session.Combat.Changed -= QueueRedraw;
    }

    private void AdvanceEnemies(float delta)
    {
        foreach (var enemy in _session.StarfallRuinsTrial.Enemies()
                     .Where(enemy => !enemy.Defeated))
        {
            var state = _runtime[enemy.InstanceId];
            if (state.Attacking)
            {
                state.Windup -= delta;
                if (state.Windup <= 0)
                {
                    ResolveEnemyAttack(enemy, state);
                }
                continue;
            }

            state.Cooldown = Math.Max(0, state.Cooldown - delta);
            var enemyPosition = new Vector2(enemy.CurrentX, enemy.CurrentY);
            var deltaToPlayer = _playerPosition() - enemyPosition;
            var distance = deltaToPlayer.Length();
            if (state.Cooldown <= 0 && CanBeginAttack(enemy.EnemyId, distance))
            {
                state.Attacking = true;
                state.Windup = StarfallRuinsTrialCatalog.Enemy(
                    enemy.EnemyId
                ).WindupSeconds;
                state.Moving = false;
                continue;
            }

            var direction = MovementDirection(enemy.EnemyId, deltaToPlayer);
            state.Moving = direction != Vector2.Zero &&
                TryMoveEnemy(enemy, direction, delta);
            if (direction != Vector2.Zero)
            {
                state.Facing = Facing(direction);
            }
        }
    }

    private bool TryMoveEnemy(
        StarfallTrialEnemySnapshot enemy,
        Vector2 direction,
        float delta
    )
    {
        var speed = StarfallRuinsTrialCatalog.Enemy(enemy.EnemyId)
            .MovementSpeedPixelsPerSecond;
        var movement = direction.Normalized() * speed * delta;
        var horizontal = Math.Abs(movement.X) > 0.001f
            ? _session.MoveStarfallEnemyChecked(
                enemy.InstanceId,
                enemy.CurrentX + movement.X,
                enemy.CurrentY
            )
            : ActionResult.Fail("combat.enemy_move.blocked");
        var current = _session.StarfallRuinsTrial.Enemy(enemy.InstanceId);
        var vertical = Math.Abs(movement.Y) > 0.001f
            ? _session.MoveStarfallEnemyChecked(
                enemy.InstanceId,
                current.CurrentX,
                current.CurrentY + movement.Y
            )
            : ActionResult.Fail("combat.enemy_move.blocked");
        return horizontal.Succeeded || vertical.Succeeded;
    }

    private void ResolveEnemyAttack(
        StarfallTrialEnemySnapshot enemy,
        EnemyRuntime state
    )
    {
        state.Attacking = false;
        state.Cooldown = AttackCooldown(enemy.EnemyId);
        var definition = StarfallRuinsTrialCatalog.Enemy(enemy.EnemyId);
        var enemyPosition = new Vector2(enemy.CurrentX, enemy.CurrentY);
        var toPlayer = _playerPosition() - enemyPosition;
        switch (definition.AttackKind)
        {
            case EnemyAttackKind.Melee:
                if (toPlayer.Length() <= 18)
                {
                    ApplyEnemyHit(enemy.InstanceId);
                }
                break;
            case EnemyAttackKind.Projectile:
                if (toPlayer.LengthSquared() > 0.01f)
                {
                    _projectiles.Add(new Projectile
                    {
                        EnemyInstanceId = enemy.InstanceId,
                        Position = enemyPosition,
                        Direction = toPlayer.Normalized()
                    });
                }
                break;
            case EnemyAttackKind.AreaOfEffect:
                if (toPlayer.Length() <= definition.AreaRadiusPixels)
                {
                    ApplyEnemyHit(enemy.InstanceId);
                }
                break;
        }
    }

    private void AdvanceProjectiles(float delta)
    {
        for (var index = _projectiles.Count - 1; index >= 0; index--)
        {
            var projectile = _projectiles[index];
            projectile.Position += projectile.Direction * 80 * delta;
            var cell = new GridPosition(
                Mathf.FloorToInt(projectile.Position.X / 16),
                Mathf.FloorToInt(projectile.Position.Y / 16)
            );
            if (!StarfallRuinsTrialLayout.IsWalkable(cell))
            {
                _projectiles.RemoveAt(index);
                continue;
            }
            if (projectile.Position.DistanceTo(_playerPosition()) <= 6)
            {
                ApplyEnemyHit(projectile.EnemyInstanceId);
                _projectiles.RemoveAt(index);
            }
        }
    }

    private void ApplyEnemyHit(string instanceId)
    {
        var result = _session.ReceiveStarfallEnemyHit(instanceId);
        if (result.Succeeded)
        {
            _notice(result.MessageKey);
        }
    }

    private void DrawStaticEntities()
    {
        if (!_session.StarfallRuinsTrial.WeaponClaimed)
        {
            DrawAt(
                StarfallRuinsTrialLayout.WeaponRackCell,
                StarfallRuinsArt.CombatAtlas,
                StarfallRuinsArt.WeaponRackRegion,
                new Vector2(46, 46)
            );
        }

        foreach (var artifact in StarfallRuinsTrialCatalog.Artifacts)
        {
            if (_session.StarfallRuinsTrial.RecoveredArtifactIds.Contains(
                    artifact.ItemId
                ) || !string.IsNullOrEmpty(artifact.RequiredClearedRoomId) &&
                !_session.StarfallRuinsTrial.IsRoomCleared(
                    artifact.RequiredClearedRoomId
                ))
            {
                continue;
            }
            DrawAt(
                artifact.Cell,
                StarfallRuinsArt.ArtifactAtlas,
                StarfallRuinsArt.ArtifactWorldRegion(artifact.ItemId),
                new Vector2(46, 46)
            );
        }

        foreach (var seal in new[]
                 {
                     StarfallRuinsTrialLayout.FirstSealCell,
                     StarfallRuinsTrialLayout.SecondSealCell
                 })
        {
            if (_session.StarfallRuinsTrial.IsSealOpen(seal))
            {
                continue;
            }
            DrawAt(
                seal,
                CrystalGrottoArt.Atlas,
                CrystalGrottoArt.SealRegion,
                new Vector2(52, 52)
            );
        }
    }

    private void DrawEnemy(
        StarfallTrialEnemySnapshot enemy,
        EnemyRuntime state
    )
    {
        var source = StarfallRuinsArt.EnemyRegion(
            enemy.EnemyId,
            state.Facing,
            state.Moving && ((int)(_animationTime / 0.18) & 1) == 1
        );
        var height = enemy.EnemyId ==
            StarfallRuinsTrialCatalog.HollowSentinelEnemyId
                ? 48f
                : enemy.EnemyId ==
                    StarfallRuinsTrialCatalog.ShardlingEnemyId
                    ? 34f
                    : 36f;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = new Vector2(enemy.CurrentX, enemy.CurrentY + 7);
        DrawTextureRectRegion(
            StarfallRuinsArt.CombatAtlas,
            new Rect2(
                anchor - new Vector2(width / 2, height),
                new Vector2(width, height)
            ),
            source
        );

        if (enemy.CurrentHealth < enemy.MaxHealth)
        {
            var ratio = enemy.CurrentHealth / (float)enemy.MaxHealth;
            DrawRect(
                new Rect2(anchor + new Vector2(-17, -height - 5),
                    new Vector2(34, 3)),
                new Color("#07132b")
            );
            DrawRect(
                new Rect2(anchor + new Vector2(-16, -height - 4),
                    new Vector2(32 * ratio, 1)),
                ThemeFactory.Mint
            );
        }

        if (!state.Attacking)
        {
            return;
        }
        var definition = StarfallRuinsTrialCatalog.Enemy(enemy.EnemyId);
        var radius = definition.AttackKind switch
        {
            EnemyAttackKind.Melee => 18,
            EnemyAttackKind.Projectile => 12,
            _ => definition.AreaRadiusPixels
        };
        var progress = 1f - Math.Clamp(
            state.Windup / definition.WindupSeconds,
            0,
            1
        );
        DrawArc(
            new Vector2(enemy.CurrentX, enemy.CurrentY),
            radius,
            -Mathf.Pi / 2,
            -Mathf.Pi / 2 + Mathf.Tau * progress,
            28,
            definition.AttackKind == EnemyAttackKind.AreaOfEffect
                ? ThemeFactory.Gold
                : ThemeFactory.Mint,
            2
        );
    }

    private void DrawCombatEffects()
    {
        foreach (var projectile in _projectiles)
        {
            DrawTextureRectRegion(
                StarfallRuinsArt.CombatAtlas,
                new Rect2(projectile.Position - new Vector2(7, 5),
                    new Vector2(14, 10)),
                StarfallRuinsArt.PrismProjectileRegion
            );
        }

        if (_slashRemaining > 0)
        {
            var direction = new Vector2(_slashFacing.X, _slashFacing.Y);
            DrawTextureRectRegion(
                StarfallRuinsArt.CombatAtlas,
                new Rect2(
                    _slashPosition + direction * 15 - new Vector2(15, 15),
                    new Vector2(30, 30)
                ),
                StarfallRuinsArt.SlashRegion(_slashRemaining < 0.09f)
            );
        }

        if (_dodgeSparkRemaining > 0)
        {
            DrawTextureRectRegion(
                StarfallRuinsArt.CombatAtlas,
                new Rect2(_dodgeSparkPosition - new Vector2(12, 10),
                    new Vector2(24, 20)),
                StarfallRuinsArt.DodgeSparkRegion
            );
        }
    }

    private void DrawAt(
        GridPosition cell,
        Texture2D atlas,
        Rect2 source,
        Vector2 size
    )
    {
        var anchor = new Vector2(cell.X * 16 + 8, cell.Y * 16 + 15);
        DrawTextureRectRegion(
            atlas,
            new Rect2(anchor - new Vector2(size.X / 2, size.Y), size),
            source
        );
    }

    private static bool CanBeginAttack(string enemyId, float distance) =>
        enemyId switch
        {
            StarfallRuinsTrialCatalog.ShardlingEnemyId => distance <= 18,
            StarfallRuinsTrialCatalog.PrismWispEnemyId => distance <= 96,
            StarfallRuinsTrialCatalog.HollowSentinelEnemyId => distance <= 36,
            _ => false
        };

    private static Vector2 MovementDirection(
        string enemyId,
        Vector2 deltaToPlayer
    )
    {
        var distance = deltaToPlayer.Length();
        if (distance <= 0.01f)
        {
            return Vector2.Zero;
        }
        if (enemyId == StarfallRuinsTrialCatalog.PrismWispEnemyId)
        {
            return distance > 78
                ? deltaToPlayer.Normalized()
                : distance < 48
                    ? -deltaToPlayer.Normalized()
                    : Vector2.Zero;
        }
        var preferred = enemyId ==
            StarfallRuinsTrialCatalog.HollowSentinelEnemyId
                ? 30
                : 14;
        return distance > preferred
            ? deltaToPlayer.Normalized()
            : Vector2.Zero;
    }

    private static RuinsSpriteFacing Facing(Vector2 direction)
    {
        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
        {
            return direction.X < 0
                ? RuinsSpriteFacing.Left
                : RuinsSpriteFacing.Right;
        }
        return direction.Y < 0
            ? RuinsSpriteFacing.Up
            : RuinsSpriteFacing.Down;
    }

    private static float AttackCooldown(string enemyId) => enemyId switch
    {
        StarfallRuinsTrialCatalog.ShardlingEnemyId => 1.25f,
        StarfallRuinsTrialCatalog.PrismWispEnemyId => 1.8f,
        StarfallRuinsTrialCatalog.HollowSentinelEnemyId => 2.4f,
        _ => 2
    };

    private static float InitialCooldown(string instanceId)
    {
        var stableBucket = 0;
        foreach (var character in instanceId)
        {
            stableBucket = (stableBucket * 31 + character) % 5;
        }
        return 0.35f + stableBucket * 0.12f;
    }
}

internal sealed partial class StarfallCombatHud : Node2D
{
    private static readonly Font Font = GD.Load<Font>(
        "res://assets/fonts/NotoSansCJKsc-Regular.otf"
    );
    private readonly GameSession _session;
    private readonly LocaleService _locale;

    public StarfallCombatHud(GameSession session, LocaleService locale)
    {
        _session = session;
        _locale = locale;
        session.Combat.Changed += QueueRedraw;
        locale.LocaleChanged += QueueRedraw;
    }

    public override void _Process(double delta)
    {
        _ = delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var panel = new Rect2(8, 164, 184, 31);
        DrawRect(panel, new Color("#07132bdd"));
        DrawRect(panel, ThemeFactory.Mint, false, 1);
        DrawTextureRectRegion(
            StarfallRuinsArt.CombatAtlas,
            new Rect2(12, 168, 18, 18),
            StarfallRuinsArt.HealthCoreRegion
        );
        var ratio = _session.Combat.CurrentHealth /
            (float)CombatSystem.MaxHealth;
        DrawRect(new Rect2(34, 171, 150, 7), new Color("#10182f"));
        DrawRect(new Rect2(35, 172, 148 * ratio, 5), ThemeFactory.Mint);
        DrawString(
            Font,
            new Vector2(34, 188),
            _locale.Tr(
                "combat.hud.health",
                _session.Combat.CurrentHealth,
                CombatSystem.MaxHealth
            ),
            HorizontalAlignment.Left,
            150,
            8,
            ThemeFactory.Ink
        );
        var controlsPanel = new Rect2(196, 164, 276, 31);
        DrawRect(controlsPanel, new Color("#07132bdd"));
        DrawRect(controlsPanel, ThemeFactory.Mint, false, 1);
        DrawString(
            Font,
            new Vector2(202, 184),
            _locale.Tr("combat.hud.controls"),
            HorizontalAlignment.Left,
            410,
            8,
            ThemeFactory.MutedInk
        );
    }

    public override void _ExitTree()
    {
        _session.Combat.Changed -= QueueRedraw;
        _locale.LocaleChanged -= QueueRedraw;
    }
}
