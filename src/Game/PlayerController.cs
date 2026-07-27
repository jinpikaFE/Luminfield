using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class PlayerController : CharacterBody2D
{
    private const float Speed = 72;
    private const float FootContactY = 8;
    private readonly Func<Vector2, bool> _canOccupy;
    private readonly ActorShadow _shadow;
    private readonly Sprite2D _sprite;
    private Vector2I _facing = Vector2I.Down;
    private bool _isWalking;
    private double _walkAnimation;
    private double _stepTimer;
    private int _walkFrame;

    public PlayerController(Func<Vector2, bool> canOccupy)
    {
        _canOccupy = canOccupy;
        _shadow = new ActorShadow
        {
            Position = new Vector2(0, 8),
            ZIndex = -1,
        };
        AddChild(_shadow);

        _sprite = GeneratedArt.CreatePlayerSprite();
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
        _isWalking = true;
        _walkFrame = 1 - _walkFrame;
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
            _isWalking = true;
            AnimateWalking(delta);
            PositionChanged?.Invoke(Position);
        }
        else
        {
            _isWalking = false;
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
        GeneratedArt.SetPlayerFrame(_sprite, _facing, _isWalking, _walkFrame);
        if (_isWalking)
        {
            var stride = _walkFrame == 0 ? -1f : 1f;
            _sprite.Position = new Vector2(stride * 0.25f, FootContactY);
            _sprite.Rotation = _facing.X == 0 ? 0 : stride * 0.018f;
            _sprite.Scale *= _walkFrame == 0
                ? new Vector2(1.012f, 0.988f)
                : new Vector2(0.992f, 1.008f);
        }
        else
        {
            _sprite.Position = new Vector2(0, FootContactY);
            _sprite.Rotation = 0;
        }

        _shadow.SetWalkState(_isWalking, _walkFrame);
    }

    private static GridPosition WorldToGrid(Vector2 world) =>
        new(
            Mathf.FloorToInt(world.X / TilePaletteFactory.TileSize),
            Mathf.FloorToInt(world.Y / TilePaletteFactory.TileSize)
        );
}

internal sealed partial class ActorShadow : Node2D
{
    private bool _isWalking;
    private int _walkFrame;

    public void SetWalkState(bool isWalking, int walkFrame)
    {
        if (_isWalking == isWalking && _walkFrame == walkFrame)
        {
            return;
        }

        _isWalking = isWalking;
        _walkFrame = walkFrame;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var stride = _isWalking ? (_walkFrame == 0 ? -0.7f : 0.7f) : 0;
        var radius = _isWalking ? (_walkFrame == 0 ? 7.1f : 7.8f) : 7.5f;
        DrawSetTransform(new Vector2(stride, 0), 0, new Vector2(1.0f, 0.38f));
        DrawCircle(Vector2.Zero, radius, new Color(0.03f, 0.06f, 0.11f, 0.58f));
        DrawArc(
            Vector2.Zero,
            radius,
            0,
            Mathf.Tau,
            24,
            new Color(0.31f, 0.83f, 0.73f, 0.2f),
            1.0f
        );
    }
}
