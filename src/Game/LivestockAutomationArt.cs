using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class LivestockAutomationArt
{
    private const int CellSize = 627;

    private static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/animals/shared/livestock_automation_console.png"
    );

    public static AtlasTexture ConsoleTexture(
        AnimalAutomationState state
    )
    {
        var column = state.StoredFeed > 0 ? 1 : 0;
        var row = state.StoredProductCount > 0 ? 1 : 0;
        return Cell(row, column);
    }

    public static AtlasTexture ProjectIconTexture() => Cell(1, 1);

    private static AtlasTexture Cell(int row, int column) => new()
    {
        Atlas = Atlas,
        Region = new Rect2(
            column * CellSize,
            row * CellSize,
            CellSize,
            CellSize
        ),
        FilterClip = true
    };
}

internal sealed partial class LivestockAutomationConsoleVisual : Sprite2D
{
    private readonly GameSession _session;
    private readonly string _buildingId;

    public LivestockAutomationConsoleVisual(
        GameSession session,
        string buildingId,
        GridPosition cell
    )
    {
        _session = session;
        _buildingId = buildingId;
        Name = $"LivestockAutomationConsole_{buildingId}";
        Position = new Vector2(cell.X * 16 + 8, cell.Y * 16 + 15);
        Offset = new Vector2(0, -276.5f);
        Scale = Vector2.One * 0.10f;
        TextureFilter = TextureFilterEnum.Nearest;
        ZIndex = 6;
        session.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
    }

    private void Refresh()
    {
        Texture = LivestockAutomationArt.ConsoleTexture(
            _session.AnimalAutomationFor(_buildingId)
        );
        Modulate = _session.LivestockAutomationUnlocked
            ? Colors.White
            : new Color(0.52f, 0.56f, 0.72f, 0.72f);
    }
}
