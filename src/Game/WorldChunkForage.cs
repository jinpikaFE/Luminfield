using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal sealed partial class WorldChunkForage : Node2D
{
    private readonly ChunkPosition _chunk;
    private readonly GameSession _session;

    public WorldChunkForage(ChunkPosition chunk, GameSession session)
    {
        _chunk = chunk;
        _session = session;
        ZIndex = 4;
        TextureFilter = TextureFilterEnum.Nearest;
        session.Forage.Changed += OnForageChanged;
    }

    public override void _Draw()
    {
        foreach (var spawn in _session.Forage.ActiveSpawns
                     .Where(spawn => WorldDefinition.GetChunk(spawn.Cell) == _chunk)
                     .OrderBy(spawn => spawn.Cell.Y)
                     .ThenBy(spawn => spawn.Cell.X))
        {
            var size = SeasonalForageArt.WorldSize(spawn.ItemId);
            var localX = spawn.Cell.X - _chunk.X * WorldDefinition.ChunkSize;
            var localY = spawn.Cell.Y - _chunk.Y * WorldDefinition.ChunkSize;
            var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
            DrawTextureRectRegion(
                SeasonalForageArt.Atlas,
                new Rect2(
                    anchor - new Vector2(size.X / 2, size.Y),
                    size
                ),
                SeasonalForageArt.WorldRegion(spawn.ItemId)
            );
        }
    }

    public override void _ExitTree()
    {
        _session.Forage.Changed -= OnForageChanged;
    }

    private void OnForageChanged(GridPosition cell)
    {
        if (WorldDefinition.GetChunk(cell) == _chunk)
        {
            QueueRedraw();
        }
    }
}
