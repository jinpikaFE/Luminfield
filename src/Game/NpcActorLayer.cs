using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal partial class NpcActorLayer : Node2D
{
    private const float TileSize = 16;
    private const float MoveDuration = 0.48f;

    private readonly GameSession _session;
    private readonly string _locationId;
    private readonly Dictionary<string, NpcVisualTrack> _tracks =
        new(StringComparer.Ordinal);

    public NpcActorLayer(
        GameSession session,
        string locationId,
        int zIndex = 8
    )
    {
        _session = session;
        _locationId = locationId;
        ZIndex = zIndex;
        TextureFilter = TextureFilterEnum.Nearest;
        Refresh(snap: true);
        session.Clock.TimeChanged += OnWorldStateChanged;
        session.Weather.Changed += OnWorldStateChanged;
        session.Village.Changed += OnWorldStateChanged;
    }

    public override void _Process(double delta)
    {
        var isAnimating = false;
        foreach (var track in _tracks.Values)
        {
            if (!track.IsMoving)
            {
                continue;
            }

            track.Elapsed = Math.Min(
                MoveDuration,
                track.Elapsed + (float)delta
            );
            if (track.Elapsed >= MoveDuration)
            {
                track.StartAnchor = track.TargetAnchor;
            }
            else
            {
                isAnimating = true;
            }
        }

        if (isAnimating)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        foreach (var track in _tracks.Values
                     .OrderBy(CurrentAnchorY)
                     .ThenBy(value => value.State.Position.X))
        {
            DrawNpc(track);
        }
    }

    public override void _ExitTree()
    {
        _session.Clock.TimeChanged -= OnWorldStateChanged;
        _session.Weather.Changed -= OnWorldStateChanged;
        _session.Village.Changed -= OnWorldStateChanged;
    }

    private void OnWorldStateChanged() => Refresh(snap: false);

    private void Refresh(bool snap)
    {
        var current = _session.Village.CurrentNpcs(
            _session.Clock.Day,
            _session.Clock.MinuteOfDay,
            _locationId,
            _session.PlayerCell
        );
        var visibleIds = current
            .Select(npc => npc.Definition.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var missingId in _tracks.Keys
                     .Where(id => !visibleIds.Contains(id))
                     .ToArray())
        {
            _tracks.Remove(missingId);
        }

        foreach (var npc in current)
        {
            var target = AnchorFor(npc.Position);
            if (!_tracks.TryGetValue(npc.Definition.Id, out var track))
            {
                _tracks[npc.Definition.Id] = new NpcVisualTrack(
                    npc,
                    target,
                    target,
                    MoveDuration
                );
                continue;
            }

            var distance = ManhattanDistance(
                track.State.Position,
                npc.Position
            );
            var animateStep = !snap && distance == 1 &&
                track.State.LocationId == npc.LocationId;
            var start = animateStep ? CurrentAnchor(track) : target;
            track.State = npc;
            track.StartAnchor = start;
            track.TargetAnchor = target;
            track.Elapsed = animateStep ? 0 : MoveDuration;
        }

        QueueRedraw();
    }

    private void DrawNpc(NpcVisualTrack track)
    {
        var art = NpcArtCatalog.Resolve(
            track.State.Definition.Id,
            track.State.Facing
        );
        var source = art.Region;
        var height = art.TargetHeight;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = CurrentAnchor(track);
        var moving = track.IsMoving;
        var progress = MotionProgress(track);
        var stride = moving
            ? Mathf.Sin(progress * Mathf.Pi * 4)
            : 0;
        var bob = moving
            ? -Mathf.Abs(Mathf.Sin(progress * Mathf.Pi * 2)) * 0.8f
            : 0;

        DrawSetTransform(
            anchor + new Vector2(stride * 0.7f, -1),
            0,
            new Vector2(1, moving ? 0.34f : 0.38f)
        );
        DrawCircle(
            Vector2.Zero,
            moving ? 6.5f : 7,
            new Color(0.01f, 0.03f, 0.08f, 0.44f)
        );

        var rotation = 0f;
        if (moving && track.State.Facing is NpcFacing.Left or NpcFacing.Right)
        {
            rotation = stride * 0.018f;
        }
        var scale = Vector2.One;
        if (moving)
        {
            scale = stride >= 0
                ? new Vector2(0.992f, 1.008f)
                : new Vector2(1.012f, 0.988f);
        }
        DrawSetTransform(
            anchor + new Vector2(stride * 0.25f, bob),
            rotation,
            scale
        );
        DrawTextureRectRegion(
            art.Texture,
            new Rect2(
                new Vector2(-width / 2, -height),
                new Vector2(width, height)
            ),
            source
        );
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private static Vector2 AnchorFor(GridPosition position) => new(
        position.X * TileSize + 8,
        position.Y * TileSize + 15
    );

    private static float CurrentAnchorY(NpcVisualTrack track) =>
        CurrentAnchor(track).Y;

    private static Vector2 CurrentAnchor(NpcVisualTrack track)
    {
        if (!track.IsMoving)
        {
            return track.TargetAnchor;
        }

        var progress = MotionProgress(track);
        var eased = progress * progress * (3 - 2 * progress);
        var position = track.StartAnchor.Lerp(track.TargetAnchor, eased);
        return new Vector2(
            Mathf.Round(position.X),
            Mathf.Round(position.Y)
        );
    }

    private static float MotionProgress(NpcVisualTrack track) =>
        Math.Clamp(track.Elapsed / MoveDuration, 0, 1);

    private static int ManhattanDistance(
        GridPosition left,
        GridPosition right
    ) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

    private sealed class NpcVisualTrack(
        VillageNpcState state,
        Vector2 startAnchor,
        Vector2 targetAnchor,
        float elapsed
    )
    {
        public VillageNpcState State { get; set; } = state;
        public Vector2 StartAnchor { get; set; } = startAnchor;
        public Vector2 TargetAnchor { get; set; } = targetAnchor;
        public float Elapsed { get; set; } = elapsed;
        public bool IsMoving => Elapsed < MoveDuration &&
            !StartAnchor.IsEqualApprox(TargetAnchor);
    }
}
