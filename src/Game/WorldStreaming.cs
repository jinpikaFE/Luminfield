using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal sealed partial class WorldChunkStreamer : Node2D
{
    private readonly GameSession _session;
    private readonly Dictionary<ChunkPosition, WorldChunk> _loaded = [];
    private ChunkPosition? _currentChunk;
    private WorldBiome? _currentBiome;

    public WorldChunkStreamer(GameSession session)
    {
        _session = session;
        ZIndex = -95;
    }

    public int LoadedChunkCount => _loaded.Count;
    public IReadOnlyCollection<ChunkPosition> LoadedChunks => _loaded.Keys;

    public event Action<string>? RegionEntered;

    public void UpdatePlayer(Vector2 worldPosition)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        );
        if (!WorldDefinition.IsInBounds(cell))
        {
            return;
        }

        var chunk = WorldDefinition.GetChunk(cell);
        if (_currentChunk != chunk)
        {
            _currentChunk = chunk;
            RefreshLoadedChunks(chunk);
            _session.Exploration.Discover(cell);
        }

        var biome = WorldDefinition.GetBiome(cell);
        if (_currentBiome == biome)
        {
            return;
        }

        _currentBiome = biome;
        RegionEntered?.Invoke(WorldDefinition.RegionNameKey(biome));
    }

    private void RefreshLoadedChunks(ChunkPosition center)
    {
        var desired = WorldDefinition.StreamingNeighborhood(center).ToHashSet();

        foreach (var chunk in desired)
        {
            if (_loaded.ContainsKey(chunk))
            {
                continue;
            }

            var node = new WorldChunk(chunk, _session);
            _loaded[chunk] = node;
            AddChild(node);
        }

        foreach (var chunk in _loaded.Keys.Where(chunk => !desired.Contains(chunk)).ToArray())
        {
            _loaded[chunk].QueueFree();
            _loaded.Remove(chunk);
        }
    }
}

internal sealed partial class WorldChunk : Node2D
{
    public WorldChunk(ChunkPosition chunk, GameSession session)
    {
        Position = new Vector2(
            chunk.X * WorldDefinition.ChunkSize * 16,
            chunk.Y * WorldDefinition.ChunkSize * 16
        );
        AddChild(new WorldChunkGround(chunk));
        AddChild(new WorldChunkProps(chunk, session));
        AddChild(new WorldVillageChunk(chunk, session));
    }
}

internal sealed partial class WorldChunkGround : Node2D
{
    private const float PathAtlasCell = 627;
    private static readonly Texture2D PathAtlas =
        GD.Load<Texture2D>("res://assets/generated/moonstone_path_tiles.png");

    private readonly ChunkPosition _chunk;

    public WorldChunkGround(ChunkPosition chunk)
    {
        _chunk = chunk;
        ZIndex = -2;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        for (var localY = 0; localY < WorldDefinition.ChunkSize; localY++)
        {
            for (var localX = 0; localX < WorldDefinition.ChunkSize; localX++)
            {
                var cell = new GridPosition(
                    _chunk.X * WorldDefinition.ChunkSize + localX,
                    _chunk.Y * WorldDefinition.ChunkSize + localY
                );
                if (WorldDefinition.IsHomeCell(cell))
                {
                    continue;
                }

                var origin = new Vector2(localX * 16, localY * 16);
                var rect = new Rect2(origin, new Vector2(16, 16));
                var hash = WorldDefinition.Hash(cell.X, cell.Y);
                var biome = WorldDefinition.GetBiome(cell);
                DrawRect(rect, GroundColor(biome, hash));

                if (WorldDefinition.IsWater(cell))
                {
                    DrawWater(origin, hash);
                }
                else if (WorldDefinition.IsPath(cell))
                {
                    DrawPath(origin, hash);
                }
                else
                {
                    DrawGroundDetails(origin, biome, hash);
                }
            }
        }
    }

    private void DrawWater(Vector2 origin, uint hash)
    {
        DrawRect(new Rect2(origin, new Vector2(16, 16)), new Color("#0b4965"));
        DrawLine(
            origin + new Vector2(2 + hash % 4, 5),
            origin + new Vector2(11 + hash % 3, 5),
            new Color("#2ca5ad"),
            1
        );
        if (hash % 3 == 0)
        {
            DrawLine(
                origin + new Vector2(5, 11),
                origin + new Vector2(14, 11),
                new Color("#65d9c3"),
                1
            );
        }
    }

    private void DrawPath(Vector2 origin, uint hash)
    {
        var variant = (int)(hash % 4);
        var source = new Rect2(
            variant % 2 * PathAtlasCell,
            variant / 2 * PathAtlasCell,
            PathAtlasCell,
            PathAtlasCell
        );
        DrawTextureRectRegion(
            PathAtlas,
            new Rect2(origin, new Vector2(16, 16)),
            source,
            new Color(0.84f, 0.86f, 1f, 1f)
        );
    }

    private void DrawGroundDetails(Vector2 origin, WorldBiome biome, uint hash)
    {
        if (hash % 5 == 0)
        {
            var accent = biome switch
            {
                WorldBiome.StarfallMeadow => new Color("#9de7ad"),
                WorldBiome.LumenVillage => new Color("#e7c87d"),
                WorldBiome.MoonwaterWetlands => new Color("#4cc9bf"),
                WorldBiome.StarfallRuins => new Color("#9d83cf"),
                _ => new Color("#397568")
            };
            DrawLine(
                origin + new Vector2(5, 13),
                origin + new Vector2(4, 8),
                accent,
                1
            );
            DrawLine(
                origin + new Vector2(9, 14),
                origin + new Vector2(10, 10),
                accent,
                1
            );
        }

        if (hash % 17 == 0)
        {
            DrawCircle(
                origin + new Vector2(3 + hash % 10, 3 + (hash >> 4) % 9),
                1,
                hash % 2 == 0 ? ThemeFactory.Mint : ThemeFactory.Violet
            );
        }
    }

    private static Color GroundColor(WorldBiome biome, uint hash)
    {
        var alternate = hash % 4 == 0;
        return biome switch
        {
            WorldBiome.WhisperingWoods =>
                alternate ? new Color("#102f38") : new Color("#123743"),
            WorldBiome.StarfallMeadow =>
                alternate ? new Color("#1c4b49") : new Color("#205350"),
            WorldBiome.LumenVillage =>
                alternate ? new Color("#243f4c") : new Color("#294854"),
            WorldBiome.CrystalVale =>
                alternate ? new Color("#183d4e") : new Color("#1c4656"),
            WorldBiome.MoonwaterWetlands =>
                alternate ? new Color("#133c4b") : new Color("#164655"),
            WorldBiome.StarfallRuins =>
                alternate ? new Color("#242f4b") : new Color("#293652"),
            _ => new Color("#102d3a")
        };
    }
}

internal sealed partial class WorldChunkProps : Node2D
{
    private const float AtlasCell = 313.5f;
    private static readonly Texture2D Atlas =
        GD.Load<Texture2D>("res://assets/generated/exploration_props_chroma.png");

    private readonly ChunkPosition _chunk;
    private readonly GameSession _session;

    public WorldChunkProps(ChunkPosition chunk, GameSession session)
    {
        _chunk = chunk;
        _session = session;
        ZIndex = 3;
        TextureFilter = TextureFilterEnum.Nearest;
        Material = GeneratedArt.CreateChromaKeyMaterial();
        session.Resources.Changed += OnResourceChanged;
        session.Starlight.Changed += OnStarlightChanged;
    }

    public override void _Draw()
    {
        for (var localY = 0; localY < WorldDefinition.ChunkSize; localY++)
        {
            for (var localX = 0; localX < WorldDefinition.ChunkSize; localX++)
            {
                var cell = new GridPosition(
                    _chunk.X * WorldDefinition.ChunkSize + localX,
                    _chunk.Y * WorldDefinition.ChunkSize + localY
                );
                if (WorldDefinition.IsWoodlandStarlightCell(cell))
                {
                    DrawWoodlandStarlight(localX, localY);
                    continue;
                }

                var atlasIndex = WorldDefinition.PropAtlasIndex(cell);
                if (atlasIndex < 0 || _session.Resources.IsRemoved(cell))
                {
                    continue;
                }

                var size = PropSize(atlasIndex);
                var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
                var destination = new Rect2(
                    anchor - new Vector2(size.X / 2, size.Y),
                    size
                );
                var source = new Rect2(
                    atlasIndex % 4 * AtlasCell,
                    atlasIndex / 4 * AtlasCell,
                    AtlasCell,
                    AtlasCell
                );
                DrawTextureRectRegion(Atlas, destination, source);
            }
        }
    }

    public override void _ExitTree()
    {
        _session.Resources.Changed -= OnResourceChanged;
        _session.Starlight.Changed -= OnStarlightChanged;
    }

    private void OnResourceChanged(GridPosition cell)
    {
        if (WorldDefinition.GetChunk(cell) == _chunk)
        {
            QueueRedraw();
        }
    }

    private void OnStarlightChanged()
    {
        if (WorldDefinition.GetChunk(
                WorldDefinition.WoodlandStarlightCell
            ) == _chunk)
        {
            QueueRedraw();
        }
    }

    private void DrawWoodlandStarlight(int localX, int localY)
    {
        var source = GeneratedArt.WoodlandStarlightRegion(
            _session.Starlight.RewardUnlocked
        );
        var height = 78f;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
        var destination = new Rect2(
            anchor - new Vector2(width / 2, height),
            new Vector2(width, height)
        );
        DrawTextureRectRegion(
            GeneratedArt.WoodlandStarlightTexture,
            destination,
            source
        );
    }

    private static Vector2 PropSize(int index) => index switch
    {
        0 => new Vector2(58, 72),
        1 => new Vector2(64, 62),
        2 => new Vector2(42, 48),
        3 => new Vector2(42, 36),
        4 => new Vector2(42, 36),
        5 => new Vector2(38, 34),
        6 => new Vector2(64, 66),
        7 => new Vector2(38, 54),
        8 => new Vector2(34, 45),
        9 => new Vector2(58, 44),
        10 => new Vector2(62, 38),
        11 => new Vector2(34, 43),
        12 => new Vector2(42, 36),
        13 => new Vector2(42, 34),
        14 => new Vector2(38, 44),
        15 => new Vector2(42, 64),
        _ => new Vector2(32, 32)
    };
}

internal sealed partial class WorldVillageChunk : Node2D
{
    private readonly ChunkPosition _chunk;
    private readonly GameSession _session;

    public WorldVillageChunk(ChunkPosition chunk, GameSession session)
    {
        _chunk = chunk;
        _session = session;
        ZIndex = 5;
        TextureFilter = TextureFilterEnum.Nearest;
        session.Clock.TimeChanged += OnTimeChanged;
    }

    public override void _Draw()
    {
        foreach (var landmark in VillageCatalog.Landmarks
                     .Where(value =>
                         WorldDefinition.GetChunk(value.Anchor) == _chunk
                     )
                     .OrderBy(value => value.Anchor.Y)
                     .ThenBy(value => value.Anchor.X))
        {
            DrawLandmark(landmark);
        }

        foreach (var npc in _session.Village
                     .CurrentNpcs(
                         _session.Clock.Day,
                         _session.Clock.MinuteOfDay,
                         PlayerLocationIds.World
                     )
                     .Where(value =>
                         WorldDefinition.GetChunk(value.Position) == _chunk
                     )
                     .OrderBy(value => value.Position.Y)
                     .ThenBy(value => value.Position.X))
        {
            DrawNpc(npc);
        }
    }

    public override void _ExitTree()
    {
        _session.Clock.TimeChanged -= OnTimeChanged;
    }

    private void DrawLandmark(VillageLandmarkDefinition landmark)
    {
        var source = GeneratedArt.VillageLandmarkRegion(
            landmark.AtlasIndex
        );
        var height = LandmarkHeight(landmark.AtlasIndex);
        var width = height * source.Size.X / source.Size.Y;
        var anchor = LocalAnchor(landmark.Anchor);
        DrawTextureRectRegion(
            GeneratedArt.VillageLandmarkTexture,
            new Rect2(
                anchor - new Vector2(width / 2, height),
                new Vector2(width, height)
            ),
            source
        );
    }

    private void DrawNpc(VillageNpcState npc)
    {
        var source = GeneratedArt.VillageNpcRegion(
            npc.Definition.AtlasRow,
            npc.Facing
        );
        var height = npc.Definition.AtlasRow == 0 ? 54f : 52f;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = LocalAnchor(npc.Position);
        DrawCircle(
            anchor - new Vector2(0, 1),
            7,
            new Color(0.01f, 0.03f, 0.08f, 0.44f)
        );
        DrawTextureRectRegion(
            GeneratedArt.VillageNpcTexture,
            new Rect2(
                anchor - new Vector2(width / 2, height),
                new Vector2(width, height)
            ),
            source
        );
    }

    private Vector2 LocalAnchor(GridPosition cell) => new(
        (cell.X - _chunk.X * WorldDefinition.ChunkSize) * 16 + 8,
        (cell.Y - _chunk.Y * WorldDefinition.ChunkSize) * 16 + 15
    );

    private static float LandmarkHeight(int atlasIndex) => atlasIndex switch
    {
        0 => 100,
        1 => 90,
        2 => 98,
        3 => 76,
        4 => 82,
        5 => 48,
        6 => 44,
        7 => 46,
        _ => 48
    };

    private void OnTimeChanged()
    {
        QueueRedraw();
    }
}
