using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal sealed partial class WorldChunkStreamer : Node2D
{
    private readonly GameSession _session;
    private readonly Dictionary<ChunkPosition, WorldChunk> _loaded = [];
    private WorldSeasonVisualProfile _seasonVisual;
    private Texture2D _seasonPropAtlas;
    private ChunkPosition? _currentChunk;
    private WorldBiome? _currentBiome;

    public WorldChunkStreamer(GameSession session)
    {
        _session = session;
        _seasonVisual = WorldSeasonVisualCatalog.ForDay(session.Clock.Day);
        _seasonPropAtlas = LoadPropAtlas(_seasonVisual);
        ZIndex = -95;
        AddChild(new NpcActorLayer(
            session,
            PlayerLocationIds.World,
            zIndex: 6
        ));
        session.Clock.TimeChanged += RefreshSeasonVisual;
    }

    public int LoadedChunkCount => _loaded.Count;
    public IReadOnlyCollection<ChunkPosition> LoadedChunks => _loaded.Keys;

    public event Action<string>? RegionEntered;

    public override void _ExitTree()
    {
        _session.Clock.TimeChanged -= RefreshSeasonVisual;
    }

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

            var node = new WorldChunk(
                chunk,
                _session,
                _seasonPropAtlas
            );
            _loaded[chunk] = node;
            AddChild(node);
        }

        foreach (var chunk in _loaded.Keys.Where(chunk => !desired.Contains(chunk)).ToArray())
        {
            _loaded[chunk].QueueFree();
            _loaded.Remove(chunk);
        }
    }

    private void RefreshSeasonVisual()
    {
        var next = WorldSeasonVisualCatalog.ForDay(_session.Clock.Day);
        if (next.Variant == _seasonVisual.Variant)
        {
            return;
        }

        var atlas = LoadPropAtlas(next);
        _seasonVisual = next;
        _seasonPropAtlas = atlas;
        foreach (var chunk in _loaded.Values)
        {
            chunk.ApplySeasonVisual(atlas);
        }
    }

    private static Texture2D LoadPropAtlas(
        WorldSeasonVisualProfile profile
    ) => GD.Load<Texture2D>(profile.PropAtlasTexturePath);
}

internal sealed partial class WorldChunk : Node2D
{
    private readonly WorldChunkProps _props;

    public WorldChunk(
        ChunkPosition chunk,
        GameSession session,
        Texture2D seasonPropAtlas
    )
    {
        Position = new Vector2(
            chunk.X * WorldDefinition.ChunkSize * 16,
            chunk.Y * WorldDefinition.ChunkSize * 16
        );
        _props = new WorldChunkProps(
            chunk,
            session,
            seasonPropAtlas
        );
        AddChild(_props);
        AddChild(new WorldChunkForage(chunk, session));
        AddChild(new WorldVillageChunk(chunk));
    }

    public void ApplySeasonVisual(Texture2D propAtlas)
    {
        _props.SetVisual(propAtlas);
    }
}

internal sealed partial class WorldChunkProps : Node2D
{
    private const float AtlasCell =
        WorldSeasonVisualCatalog.PropAtlasCellSize;

    private readonly ChunkPosition _chunk;
    private readonly GameSession _session;
    private Texture2D _atlas;

    public WorldChunkProps(
        ChunkPosition chunk,
        GameSession session,
        Texture2D atlas
    )
    {
        _chunk = chunk;
        _session = session;
        _atlas = atlas;
        ZIndex = 3;
        TextureFilter = TextureFilterEnum.Nearest;
        session.Resources.Changed += OnResourceChanged;
        session.Starlight.Changed += OnStarlightChanged;
    }

    public void SetVisual(Texture2D atlas)
    {
        _atlas = atlas;
        QueueRedraw();
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

                if (WorldDefinition.IsMeadowStarlightCell(cell))
                {
                    DrawMeadowStarlight(localX, localY);
                    continue;
                }

                if (WorldDefinition.IsMoonwaterStarlightCell(cell))
                {
                    DrawMoonwaterStarlight(localX, localY);
                    continue;
                }

                if (cell == WorldDefinition.CrystalWellCell)
                {
                    DrawCrystalValeStarlight(localX, localY);
                    continue;
                }

                if (cell == WorldDefinition.StarfallRuinsStarlightCell)
                {
                    DrawStarfallRuinsStarlight(localX, localY);
                    continue;
                }

                if (WorldDefinition.IsCrystalGrottoSurveyEntryCell(cell))
                {
                    DrawCrystalGrottoEntrance(localX, localY);
                    continue;
                }

                if (WorldDefinition.IsStarfallRuinsTrialEntryCell(cell))
                {
                    DrawStarfallRuinsTrialGate(localX, localY);
                    continue;
                }

                if (WorldDefinition.IsFireflyTideGateCell(cell))
                {
                    DrawFireflyTideGate(localX, localY);
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
                DrawTextureRectRegion(_atlas, destination, source);
            }
        }

        DrawNavigationGuides();
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
        if (StarlightSpatialCatalog.Pedestals.Any(definition =>
                WorldDefinition.GetChunk(definition.Cell) == _chunk))
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

    private void DrawMeadowStarlight(int localX, int localY)
    {
        var source = MeadowStarlightArt.PedestalRegion(
            _session.Starlight.MeadowPollinationUnlocked
        );
        const float height = 78f;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
        var destination = new Rect2(
            anchor - new Vector2(width / 2, height),
            new Vector2(width, height)
        );
        DrawTextureRectRegion(
            MeadowStarlightArt.Atlas,
            destination,
            source
        );
    }

    private void DrawMoonwaterStarlight(int localX, int localY)
    {
        var source = MoonwaterStarlightArt.PedestalRegion(
            _session.Starlight.MoonwaterTideUnlocked
        );
        const float height = 78f;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
        var destination = new Rect2(
            anchor - new Vector2(width / 2, height),
            new Vector2(width, height)
        );
        DrawTextureRectRegion(
            MoonwaterStarlightArt.Atlas,
            destination,
            source
        );
    }

    private void DrawCrystalValeStarlight(int localX, int localY)
    {
        var source = CrystalValeStarlightArt.PedestalRegion(
            _session.Starlight.CrystalRuinsPassageUnlocked
        );
        const float height = 78f;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
        DrawTextureRectRegion(
            CrystalValeStarlightArt.Atlas,
            new Rect2(
                anchor - new Vector2(width / 2, height),
                new Vector2(width, height)
            ),
            source
        );
    }

    private void DrawStarfallRuinsStarlight(int localX, int localY)
    {
        var source = StarfallRuinsArt.RuinsStarlightRegion(
            _session.Starlight.StarfallSixfoldConvergenceUnlocked
        );
        const float height = 78f;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
        DrawTextureRectRegion(
            StarfallRuinsArt.StarlightAtlas,
            new Rect2(
                anchor - new Vector2(width / 2, height),
                new Vector2(width, height)
            ),
            source
        );
    }

    private void DrawCrystalGrottoEntrance(int localX, int localY)
    {
        const float height = 64f;
        var source = CrystalGrottoArt.EntranceRegion;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
        DrawTextureRectRegion(
            CrystalGrottoArt.Atlas,
            new Rect2(
                anchor - new Vector2(width / 2, height),
                new Vector2(width, height)
            ),
            source
        );
    }

    private void DrawStarfallRuinsTrialGate(int localX, int localY)
    {
        const float height = 64f;
        var source = StarfallRuinsArt.TrialGateRegion(
            _session.Starlight.CrystalRuinsPassageUnlocked
        );
        var width = height * source.Size.X / source.Size.Y;
        var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
        DrawTextureRectRegion(
            StarfallRuinsArt.ArtifactAtlas,
            new Rect2(
                anchor - new Vector2(width / 2, height),
                new Vector2(width, height)
            ),
            source
        );
    }

    private void DrawFireflyTideGate(int localX, int localY)
    {
        const float height = 76f;
        var source = FireflyTideArt.TideAltarRegion;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
        DrawTextureRectRegion(
            FireflyTideArt.Atlas,
            new Rect2(
                anchor - new Vector2(width / 2, height),
                new Vector2(width, height)
            ),
            source
        );
    }

    private void DrawNavigationGuides()
    {
        foreach (var guide in WorldNavigationGuideCatalog.ForChunk(_chunk)
                     .OrderBy(value => value.Position.Y)
                     .ThenBy(value => value.Position.X))
        {
            DrawNavigationGuide(guide);
        }
    }

    private void DrawNavigationGuide(WorldNavigationGuide guide)
    {
        var localX = guide.Position.X -
            _chunk.X * WorldDefinition.ChunkSize;
        var localY = guide.Position.Y -
            _chunk.Y * WorldDefinition.ChunkSize;
        var anchor = new Vector2(localX * 16 + 8, localY * 16 + 15);
        var size = NavigationGuideSize(guide);
        var source = new Rect2(
            guide.AtlasIndex % 4 * AtlasCell,
            guide.AtlasIndex / 4 * AtlasCell,
            AtlasCell,
            AtlasCell
        );
        DrawTextureRectRegion(
            _atlas,
            new Rect2(
                anchor - new Vector2(size.X / 2, size.Y),
                size
            ),
            source
        );
    }

    private static Vector2 NavigationGuideSize(
        WorldNavigationGuide guide
    ) => guide.Kind switch
    {
        WorldNavigationGuideKind.RegionThreshold => new Vector2(26, 34),
        WorldNavigationGuideKind.LandmarkApproach => new Vector2(24, 32),
        _ => new Vector2(20, 28)
    };

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

    public WorldVillageChunk(ChunkPosition chunk)
    {
        _chunk = chunk;
        ZIndex = 5;
        TextureFilter = TextureFilterEnum.Nearest;
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

    }

    private void DrawLandmark(VillageLandmarkDefinition landmark)
    {
        if (landmark.Id == VillageCatalog.TwilightEmporiumLandmarkId)
        {
            DrawTwilightEmporium(landmark.Anchor);
            return;
        }

        if (landmark.Id == VillageCatalog.StarlightPostLandmarkId)
        {
            DrawStarlightPost(landmark.Anchor);
            return;
        }

        if (landmark.Id == VillageCatalog.StarfallWatchLandmarkId)
        {
            DrawStarfallWatch(landmark.Anchor);
            return;
        }

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

    private void DrawTwilightEmporium(GridPosition cell)
    {
        var source = GeneratedArt.TwilightEmporiumExteriorTextureRegion;
        const float height = 100;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = LocalAnchor(cell);
        DrawTextureRectRegion(
            GeneratedArt.TwilightEmporiumExteriorTexture,
            new Rect2(
                anchor - new Vector2(width / 2, height),
                new Vector2(width, height)
            ),
            source
        );
    }

    private void DrawStarlightPost(GridPosition cell)
    {
        var source = GeneratedArt.StarlightPostExteriorTextureRegion;
        const float height = 100;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = LocalAnchor(cell);
        DrawTextureRectRegion(
            GeneratedArt.StarlightPostExteriorTexture,
            new Rect2(
                anchor - new Vector2(width / 2, height),
                new Vector2(width, height)
            ),
            source
        );
    }

    private void DrawStarfallWatch(GridPosition cell)
    {
        var source = GeneratedArt.StarfallWatchExteriorTextureRegion;
        const float height = 100;
        var width = height * source.Size.X / source.Size.Y;
        var anchor = LocalAnchor(cell);
        DrawTextureRectRegion(
            GeneratedArt.StarfallWatchExteriorTexture,
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
        8 => 100,
        9 => 100,
        10 => 100,
        _ => 48
    };

    private void OnTimeChanged()
    {
        QueueRedraw();
    }
}
