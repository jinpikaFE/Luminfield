using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class PlayerController : CharacterBody2D
{
    private const float Speed = 72;
    private readonly Func<Vector2, bool> _canOccupy;
    private readonly Sprite2D _sprite;
    private Vector2I _facing = Vector2I.Down;
    private double _walkAnimation;
    private double _stepTimer;
    private int _walkFrame;

    public PlayerController(Func<Vector2, bool> canOccupy)
    {
        _canOccupy = canOccupy;
        AddChild(new ActorShadow
        {
            Position = new Vector2(0, 8),
            ZIndex = -1,
        });

        _sprite = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://assets/pixel/characters.svg"),
            RegionEnabled = true,
            RegionRect = new Rect2(0, 0, 16, 24),
            Position = new Vector2(0, -9),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        AddChild(_sprite);
    }

    public bool ControlsEnabled { get; set; } = true;
    public Vector2I Facing => _facing;
    public GridPosition CurrentCell => WorldToGrid(Position);
    public GridPosition TargetCell => new(CurrentCell.X + _facing.X, CurrentCell.Y + _facing.Y);

    public event Action? Stepped;
    public event Action<Vector2>? PositionChanged;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!ControlsEnabled || @event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        var direction = @event.IsActionPressed(InputSetup.MoveLeft)
            ? Vector2.Left
            : @event.IsActionPressed(InputSetup.MoveRight)
                ? Vector2.Right
                : @event.IsActionPressed(InputSetup.MoveUp)
                    ? Vector2.Up
                    : @event.IsActionPressed(InputSetup.MoveDown)
                        ? Vector2.Down
                        : Vector2.Zero;

        if (direction == Vector2.Zero)
        {
            return;
        }

        UpdateFacing(direction);
        MoveWithGridCollision(direction * 4);
        UpdateSprite();
        PositionChanged?.Invoke(Position);
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = ControlsEnabled
            ? Input.GetVector(
                InputSetup.MoveLeft,
                InputSetup.MoveRight,
                InputSetup.MoveUp,
                InputSetup.MoveDown
            )
            : Vector2.Zero;

        if (input.LengthSquared() > 0.01f)
        {
            UpdateFacing(input);
            MoveWithGridCollision(input.Normalized() * Speed * (float)delta);
            AnimateWalking(delta);
            PositionChanged?.Invoke(Position);
        }
        else
        {
            _walkFrame = 0;
            _walkAnimation = 0;
            _stepTimer = 0;
            UpdateSprite();
        }
    }

    private void MoveWithGridCollision(Vector2 movement)
    {
        var horizontal = Position + new Vector2(movement.X, 0);
        if (_canOccupy(horizontal))
        {
            Position = horizontal;
        }

        var vertical = Position + new Vector2(0, movement.Y);
        if (_canOccupy(vertical))
        {
            Position = vertical;
        }
    }

    private void UpdateFacing(Vector2 input)
    {
        if (Math.Abs(input.X) > Math.Abs(input.Y))
        {
            _facing = input.X < 0 ? Vector2I.Left : Vector2I.Right;
        }
        else
        {
            _facing = input.Y < 0 ? Vector2I.Up : Vector2I.Down;
        }
    }

    private void AnimateWalking(double delta)
    {
        _walkAnimation += delta;
        _stepTimer += delta;
        if (_walkAnimation >= 0.18)
        {
            _walkAnimation = 0;
            _walkFrame = 1 - _walkFrame;
            UpdateSprite();
        }

        if (_stepTimer >= 0.32)
        {
            _stepTimer = 0;
            Stepped?.Invoke();
        }
    }

    private void UpdateSprite()
    {
        var baseFrame = _facing switch
        {
            var facing when facing == Vector2I.Down => 0,
            var facing when facing == Vector2I.Up => 2,
            var facing when facing == Vector2I.Right => 4,
            _ => 6
        };
        _sprite.RegionRect = new Rect2((baseFrame + _walkFrame) * 16, 0, 16, 24);
    }

    private static GridPosition WorldToGrid(Vector2 world) =>
        new(
            Mathf.FloorToInt(world.X / TilePaletteFactory.TileSize),
            Mathf.FloorToInt(world.Y / TilePaletteFactory.TileSize)
        );
}

internal sealed partial class ActorShadow : Node2D
{
    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1.0f, 0.38f));
        DrawCircle(Vector2.Zero, 7.5f, new Color(0.03f, 0.06f, 0.11f, 0.58f));
        DrawArc(
            Vector2.Zero,
            7.5f,
            0,
            Mathf.Tau,
            24,
            new Color(0.31f, 0.83f, 0.73f, 0.2f),
            1.0f
        );
    }
}
