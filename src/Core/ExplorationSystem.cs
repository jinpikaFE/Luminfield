namespace Luminfield.Core;

public sealed class ExplorationSystem
{
    private readonly HashSet<string> _discoveredChunks = new(StringComparer.Ordinal);

    public IReadOnlySet<string> DiscoveredChunks => _discoveredChunks;

    public event Action? Changed;

    public void Reset()
    {
        _discoveredChunks.Clear();
        _discoveredChunks.Add(WorldDefinition.ChunkId(new ChunkPosition(0, 0)));
        Changed?.Invoke();
    }

    public void Restore(ExplorationSave? save)
    {
        _discoveredChunks.Clear();
        foreach (var id in save?.DiscoveredChunks ?? [])
        {
            if (WorldDefinition.TryParseChunkId(id, out _))
            {
                _discoveredChunks.Add(id);
            }
        }

        if (_discoveredChunks.Count == 0)
        {
            _discoveredChunks.Add(WorldDefinition.ChunkId(new ChunkPosition(0, 0)));
        }

        Changed?.Invoke();
    }

    public bool Discover(GridPosition cell)
    {
        if (!WorldDefinition.IsInBounds(cell))
        {
            return false;
        }

        var added = _discoveredChunks.Add(
            WorldDefinition.ChunkId(WorldDefinition.GetChunk(cell))
        );
        if (added)
        {
            Changed?.Invoke();
        }

        return added;
    }

    public bool IsDiscovered(ChunkPosition chunk) =>
        _discoveredChunks.Contains(WorldDefinition.ChunkId(chunk));

    public ExplorationSave Capture() => new()
    {
        DiscoveredChunks = _discoveredChunks.Order(StringComparer.Ordinal).ToList()
    };
}
