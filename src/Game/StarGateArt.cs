using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class StarGateArt
{
    private const float CellSize = 627;

    public static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/features/starlights/starfall_sixfold_gate.png"
    );

    public static Rect2 StateRegion(int stateIndex) => new(
        stateIndex % 2 * CellSize,
        stateIndex / 2 * CellSize,
        CellSize,
        CellSize
    );

    public static AtlasTexture ProjectIconTexture() => new()
    {
        Atlas = Atlas,
        Region = StateRegion(3),
        FilterClip = true
    };
}

internal sealed partial class StarGateVisual : Sprite2D
{
    private readonly GameSession _session;

    public StarGateVisual(GameSession session)
    {
        _session = session;
        Name = "SixfoldStarGate";
        Texture = StarGateArt.Atlas;
        RegionEnabled = true;
        TextureFilter = TextureFilterEnum.Nearest;
        Scale = Vector2.One * 0.14f;
        session.Construction.Changed += Refresh;
        session.Starlight.Changed += Refresh;
        session.StarGate.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        _session.Construction.Changed -= Refresh;
        _session.Starlight.Changed -= Refresh;
        _session.StarGate.Changed -= Refresh;
    }

    private void Refresh()
    {
        Visible = _session.StarGateVisible;
        if (!Visible)
        {
            return;
        }

        var stateIndex = GateStateIndex();
        RegionRect = StarGateArt.StateRegion(stateIndex);
    }

    private int GateStateIndex()
    {
        var phase = _session.Construction.PhaseFor(
            ConstructionCatalog.SixfoldStarGateProjectId
        );
        if (phase == ConstructionPhase.NotStarted)
        {
            return 0;
        }

        if (phase == ConstructionPhase.InProgress)
        {
            return 1;
        }

        return _session.StarGate.Activated ? 3 : 2;
    }
}
