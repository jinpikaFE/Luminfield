namespace Luminfield.Core;

public enum WorldBiome
{
    Home,
    WhisperingWoods,
    StarfallMeadow,
    LumenVillage,
    CrystalVale,
    MoonwaterWetlands,
    StarfallRuins
}

public enum WorldResourceKind
{
    None,
    Tree,
    Crystal
}

public readonly record struct ChunkPosition(int X, int Y);

public sealed record WorldLandmark(
    string Id,
    GridPosition Position,
    int AtlasIndex,
    string NameKey
);

public static class WorldDefinition
{
    public const int Width = 192;
    public const int Height = 128;
    public const int ChunkSize = 32;
    public const int ChunkColumns = Width / ChunkSize;
    public const int ChunkRows = Height / ChunkSize;
    public const string WoodlandStarlightLandmarkId = "woods_lantern";
    public static readonly GridPosition WoodlandStarlightCell = new(34, 72);

    public static readonly IReadOnlyList<WorldLandmark> Landmarks =
    [
        new(
            WoodlandStarlightLandmarkId,
            WoodlandStarlightCell,
            8,
            "world.landmark.woods_lantern"
        ),
        new(
            VillageCatalog.VillageGateLandmarkId,
            VillageCatalog.VillageGateCell,
            -1,
            "world.landmark.village_gate"
        ),
        new("crystal_well", new GridPosition(77, 84), 9, "world.landmark.crystal_well"),
        new("wetland_monolith", new GridPosition(164, 43), 15, "world.landmark.wetland_monolith"),
        new("ruins_pillar", new GridPosition(121, 105), 7, "world.landmark.ruins_pillar"),
        new("southern_cache", new GridPosition(173, 104), 12, "world.landmark.southern_cache")
    ];

    public static bool IsInBounds(GridPosition cell) =>
        cell.X is >= 0 and < Width && cell.Y is >= 0 and < Height;

    public static bool IsBoundaryCell(GridPosition cell) =>
        cell.X is 0 or Width - 1 ||
        cell.Y is 0 or Height - 1;

    public static IReadOnlyCollection<ChunkPosition> StreamingNeighborhood(
        ChunkPosition center
    )
    {
        var chunks = new HashSet<ChunkPosition>();
        for (var y = center.Y - 1; y <= center.Y + 1; y++)
        {
            for (var x = center.X - 1; x <= center.X + 1; x++)
            {
                var chunk = new ChunkPosition(x, y);
                if (IsValidChunk(chunk))
                {
                    chunks.Add(chunk);
                }
            }
        }

        return chunks;
    }

    public static bool IsHomeCell(GridPosition cell) =>
        cell.X is >= 0 and < FarmSystem.MapWidth &&
        cell.Y is >= 0 and < FarmSystem.MapHeight;

    public static ChunkPosition GetChunk(GridPosition cell) =>
        new(cell.X / ChunkSize, cell.Y / ChunkSize);

    public static string ChunkId(ChunkPosition chunk) => $"{chunk.X}:{chunk.Y}";

    public static string CellId(GridPosition cell) => $"{cell.X}:{cell.Y}";

    public static bool TryParseCellId(string id, out GridPosition cell)
    {
        cell = default;
        var parts = id.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var x) ||
            !int.TryParse(parts[1], out var y))
        {
            return false;
        }

        cell = new GridPosition(x, y);
        return IsInBounds(cell);
    }

    public static bool TryParseChunkId(string id, out ChunkPosition chunk)
    {
        chunk = default;
        var parts = id.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var x) ||
            !int.TryParse(parts[1], out var y))
        {
            return false;
        }

        chunk = new ChunkPosition(x, y);
        return x is >= 0 and < ChunkColumns && y is >= 0 and < ChunkRows;
    }

    public static WorldBiome GetBiome(GridPosition cell)
    {
        if (IsHomeCell(cell))
        {
            return WorldBiome.Home;
        }

        if (cell.X < 64 && cell.Y >= 32)
        {
            return WorldBiome.WhisperingWoods;
        }

        if (VillageCatalog.IsVillageCell(cell))
        {
            return WorldBiome.LumenVillage;
        }

        if (cell.X < 128 && cell.Y < 68)
        {
            return WorldBiome.StarfallMeadow;
        }

        if (cell.X >= 128 && cell.Y < 82)
        {
            return WorldBiome.MoonwaterWetlands;
        }

        if (cell.X >= 96 && cell.Y >= 82)
        {
            return WorldBiome.StarfallRuins;
        }

        return WorldBiome.CrystalVale;
    }

    public static string RegionNameKey(WorldBiome biome) => biome switch
    {
        WorldBiome.Home => "world.region.home",
        WorldBiome.WhisperingWoods => "world.region.woods",
        WorldBiome.StarfallMeadow => "world.region.meadow",
        WorldBiome.LumenVillage => "world.region.village",
        WorldBiome.CrystalVale => "world.region.crystal",
        WorldBiome.MoonwaterWetlands => "world.region.wetlands",
        WorldBiome.StarfallRuins => "world.region.ruins",
        _ => "world.region.home"
    };

    public static bool IsPath(GridPosition cell)
    {
        var farmGate = cell.X is >= 18 and <= 20 && cell.Y is >= 29 and <= 65;
        var northernRoad = cell.Y is >= 62 and <= 65 && cell.X is >= 18 and <= 176;
        var crystalRoad = cell.X is >= 95 and <= 98 && cell.Y is >= 33 and <= 118;
        var southernRoad = cell.Y is >= 96 and <= 99 && cell.X is >= 36 and <= 176;
        var wetlandRoad = cell.X is >= 159 and <= 162 && cell.Y is >= 47 and <= 108;
        var monolithBranch = cell.Y is >= 42 and <= 48 && cell.X is >= 159 and <= 166;
        var woodsBranch = cell.Y is >= 71 and <= 73 && cell.X is >= 19 and <= 48;
        return farmGate || northernRoad || crystalRoad || southernRoad ||
            wetlandRoad || monolithBranch || woodsBranch ||
            VillageCatalog.IsVillagePath(cell);
    }

    public static bool IsWater(GridPosition cell)
    {
        if (IsPath(cell))
        {
            return false;
        }

        if (GetBiome(cell) == WorldBiome.MoonwaterWetlands)
        {
            var dx = (cell.X - 160) / 29f;
            var dy = (cell.Y - 39) / 21f;
            if (dx * dx + dy * dy < 1)
            {
                return true;
            }

            var hash = Hash(cell.X, cell.Y);
            return cell.Y > 48 && hash % 11 < 3;
        }

        if (GetBiome(cell) == WorldBiome.CrystalVale)
        {
            var streamX = 51 + (int)MathF.Round(MathF.Sin(cell.Y * 0.16f) * 4);
            return Math.Abs(cell.X - streamX) <= 2;
        }

        return false;
    }

    public static bool IsWaterSource(GridPosition cell) =>
        IsHomeCell(cell)
            ? cell.X >= 37 && cell.Y >= 20
            : IsWater(cell);

    public static WorldLandmark? LandmarkAt(GridPosition cell) =>
        Landmarks.FirstOrDefault(value => value.Position == cell);

    public static bool IsWoodlandStarlightCell(GridPosition cell) =>
        cell == WoodlandStarlightCell;

    public static int PropAtlasIndex(GridPosition cell)
    {
        if (!IsInBounds(cell) || IsHomeCell(cell))
        {
            return -1;
        }

        var landmark = Landmarks.FirstOrDefault(value => value.Position == cell);
        if (landmark is not null)
        {
            return landmark.AtlasIndex;
        }

        if (IsPath(cell) || IsWater(cell))
        {
            return -1;
        }

        var roll = (int)(Hash(cell.X, cell.Y) % 100);
        if (VillageCatalog.IsVillageCell(cell))
        {
            if (VillageCatalog.IsBlocked(cell))
            {
                return -1;
            }

            return roll switch
            {
                < 2 => 13,
                < 4 => 4,
                < 5 => 5,
                _ => -1
            };
        }

        return GetBiome(cell) switch
        {
            WorldBiome.WhisperingWoods => roll switch
            {
                < 8 => 0,
                < 14 => 1,
                < 20 => 4,
                < 25 => 5,
                < 29 => 3,
                _ => -1
            },
            WorldBiome.StarfallMeadow => roll switch
            {
                < 5 => 0,
                < 11 => 4,
                < 20 => 13,
                < 24 => 2,
                < 27 => 5,
                _ => -1
            },
            WorldBiome.CrystalVale => roll switch
            {
                < 12 => 2,
                < 18 => 3,
                < 24 => 13,
                < 28 => 0,
                _ => -1
            },
            WorldBiome.MoonwaterWetlands => roll switch
            {
                < 13 => 14,
                < 19 => 1,
                < 24 => 3,
                < 29 => 5,
                _ => -1
            },
            WorldBiome.StarfallRuins => roll switch
            {
                < 8 => 7,
                < 14 => 3,
                < 19 => 5,
                < 25 => 13,
                < 28 => 2,
                _ => -1
            },
            _ => -1
        };
    }

    public static bool IsBlocked(GridPosition cell)
    {
        if (!IsInBounds(cell) || IsBoundaryCell(cell))
        {
            return true;
        }

        if (IsHomeCell(cell))
        {
            var blockedRightEdge = cell.X == FarmSystem.MapWidth - 1;
            var blockedBottomEdge =
                cell.Y == FarmSystem.MapHeight - 1 &&
                cell.X is not (>= 18 and <= 20);
            return blockedRightEdge || blockedBottomEdge;
        }

        if (IsWater(cell))
        {
            return true;
        }

        if (VillageCatalog.IsBlocked(cell))
        {
            return true;
        }

        var prop = PropAtlasIndex(cell);
        return prop is 0 or 1 or 2 or 3 or 6 or 7 or 8 or 9 or 11 or 12 or 15;
    }

    public static WorldResourceKind ResourceAt(GridPosition cell)
    {
        if (!IsInBounds(cell) ||
            IsBoundaryCell(cell) ||
            IsHomeCell(cell) ||
            LandmarkAt(cell) is not null)
        {
            return WorldResourceKind.None;
        }

        return PropAtlasIndex(cell) switch
        {
            0 or 1 => WorldResourceKind.Tree,
            2 => WorldResourceKind.Crystal,
            _ => WorldResourceKind.None
        };
    }

    public static bool IsValidChunk(ChunkPosition chunk) =>
        chunk.X is >= 0 and < ChunkColumns &&
        chunk.Y is >= 0 and < ChunkRows;

    public static uint Hash(int x, int y)
    {
        unchecked
        {
            var value = (uint)(x * 374761393 + y * 668265263);
            value = (value ^ (value >> 13)) * 1274126177u;
            return value ^ (value >> 16);
        }
    }
}
