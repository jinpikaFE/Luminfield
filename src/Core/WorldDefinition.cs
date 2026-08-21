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

public sealed record WorldScenicLandmark(
    string Id,
    GridPosition Position,
    int AtlasIndex,
    GridArea ReservedArea,
    IReadOnlyList<GridArea> CollisionAreas
);

public sealed record WorldPropPlacement(
    GridPosition Position,
    int AtlasIndex
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
    public const string MeadowStarlightLandmarkId =
        "meadow_starlight";
    public static readonly GridPosition MeadowStarlightCell = new(74, 26);
    public static readonly GridArea MeadowStarlightReservedArea =
        new(72, 22, 76, 26);
    public const string MoonwaterStarlightLandmarkId = "wetland_monolith";
    public static readonly GridPosition MoonwaterStarlightCell = new(164, 43);
    public const string CrystalWellLandmarkId = "crystal_well";
    public static readonly GridPosition CrystalWellCell = new(78, 105);
    public const string FireflyTideGateLandmarkId =
        "firefly_tide_gate";
    public static readonly GridPosition FireflyTideGateCell = new(162, 60);
    public const string StarfallRuinsStarlightLandmarkId = "ruins_pillar";
    public static readonly GridPosition StarfallRuinsStarlightCell =
        new(121, 105);
    public const string StarfallRuinsTrialGateLandmarkId =
        "starfall_ruins_trial_gate";

    public static readonly IReadOnlyList<WorldLandmark> Landmarks =
    [
        new(
            WoodlandStarlightLandmarkId,
            WoodlandStarlightCell,
            8,
            "world.landmark.woods_lantern"
        ),
        new(
            MeadowStarlightLandmarkId,
            MeadowStarlightCell,
            -1,
            "world.landmark.meadow_starlight"
        ),
        new(
            VillageCatalog.VillageGateLandmarkId,
            VillageCatalog.VillageGateCell,
            -1,
            "world.landmark.village_gate"
        ),
        new(
            CrystalWellLandmarkId,
            CrystalWellCell,
            9,
            "world.landmark.crystal_well"
        ),
        new(
            MoonwaterStarlightLandmarkId,
            MoonwaterStarlightCell,
            15,
            "world.landmark.wetland_monolith"
        ),
        new(
            FireflyTideGateLandmarkId,
            FireflyTideGateCell,
            -1,
            "world.landmark.firefly_tide_gate"
        ),
        new(
            StarfallRuinsStarlightLandmarkId,
            StarfallRuinsStarlightCell,
            7,
            "world.landmark.ruins_pillar"
        ),
        new(
            StarfallRuinsTrialGateLandmarkId,
            StarfallRuinsTrialLayout.WorldEntryCell,
            -1,
            "world.landmark.starfall_ruins_trial_gate"
        ),
        new("southern_cache", new GridPosition(173, 104), 12, "world.landmark.southern_cache")
    ];

    public static readonly IReadOnlyList<WorldScenicLandmark>
        ScenicLandmarks =
        [
            new(
                "woods_moonroot_grove",
                new GridPosition(45, 52),
                0,
                new GridArea(39, 43, 51, 52),
                [new GridArea(41, 49, 49, 52)]
            ),
            new(
                "meadow_moonflower_circle",
                new GridPosition(108, 14),
                1,
                new GridArea(103, 6, 113, 14),
                [new GridArea(104, 12, 112, 14)]
            ),
            new(
                "crystal_stepped_ridge",
                new GridPosition(86, 112),
                2,
                new GridArea(80, 101, 92, 112),
                [new GridArea(81, 108, 91, 112)]
            ),
            new(
                "wetland_boardwalk_islet",
                new GridPosition(148, 48),
                3,
                new GridArea(142, 40, 154, 48),
                [new GridArea(145, 46, 151, 48)]
            ),
            new(
                "ruins_broken_colonnade",
                new GridPosition(145, 124),
                4,
                new GridArea(139, 114, 151, 124),
                [new GridArea(140, 120, 150, 124)]
            ),
            new(
                "village_orchard_pergola",
                new GridPosition(121, 70),
                5,
                new GridArea(115, 62, 127, 70),
                [new GridArea(117, 67, 125, 70)]
            ),
            new(
                "village_transit_pavilion",
                VillageCatalog.VillageCenterCell,
                6,
                new GridArea(92, 57, 100, 64),
                []
            ),
            new(
                "east_wayfinding_cairn",
                new GridPosition(132, 58),
                7,
                new GridArea(128, 51, 136, 58),
                [new GridArea(130, 55, 134, 58)]
            )
        ];

    public static readonly IReadOnlyList<WorldPropPlacement> CuratedProps =
    [
        new(new GridPosition(27, 46), 0),
        new(new GridPosition(31, 50), 4),
        new(new GridPosition(54, 43), 1),
        new(new GridPosition(50, 76), 5),
        new(new GridPosition(29, 91), 8),
        new(new GridPosition(44, 92), 1),
        new(new GridPosition(56, 101), 4),
        new(new GridPosition(26, 110), 11),
        new(new GridPosition(54, 18), 13),
        new(new GridPosition(61, 24), 4),
        new(new GridPosition(82, 14), 13),
        new(new GridPosition(91, 27), 13),
        new(new GridPosition(106, 24), 8),
        new(new GridPosition(118, 18), 5),
        new(new GridPosition(66, 106), 2),
        new(new GridPosition(74, 115), 3),
        new(new GridPosition(100, 114), 2),
        new(new GridPosition(80, 118), 7),
        new(new GridPosition(139, 24), 14),
        new(new GridPosition(171, 19), 15),
        new(new GridPosition(134, 79), 14),
        new(new GridPosition(177, 75), 1),
        new(new GridPosition(145, 86), 8),
        new(new GridPosition(171, 88), 5),
        new(new GridPosition(184, 52), 14),
        new(new GridPosition(117, 116), 7),
        new(new GridPosition(132, 118), 3),
        new(new GridPosition(158, 102), 7),
        new(new GridPosition(169, 117), 13),
        new(new GridPosition(182, 118), 7),
        new(new GridPosition(66, 55), 13),
        new(new GridPosition(123, 38), 13),
        new(new GridPosition(67, 91), 13),
        new(new GridPosition(123, 91), 13)
    ];

    private static readonly IReadOnlyDictionary<GridPosition, int>
        CuratedPropByCell = CuratedProps.ToDictionary(
            placement => placement.Position,
            placement => placement.AtlasIndex
        );

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

        if (VillageCatalog.IsVillageCell(cell))
        {
            return WorldBiome.LumenVillage;
        }

        if (cell.X < 64 && cell.Y >= 32)
        {
            return WorldBiome.WhisperingWoods;
        }

        if (cell.X < 128 && cell.Y < 32)
        {
            return WorldBiome.StarfallMeadow;
        }

        if (cell.X >= 128 && cell.Y < 96)
        {
            return WorldBiome.MoonwaterWetlands;
        }

        if (cell.X >= 112 && cell.Y >= 96)
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
        var farmGate = cell.X is >= 18 and <= 20 &&
            cell.Y is >= 29 and <= 65;
        var eastWestRoad = cell.Y is >= 62 and <= 65 &&
            cell.X is >= 18 and <= 176;
        var northSouthRoad = cell.X is >= 94 and <= 97 &&
            cell.Y is >= 16 and <= 118;
        var meadowLoop = cell.Y is >= 20 and <= 23 &&
            cell.X is >= 48 and <= 126;
        var southernRoad = cell.Y is >= 96 and <= 99 && cell.X is >= 36 and <= 176;
        var crystalValeBranch = cell.X is >= 76 and <= 79 &&
            cell.Y is >= 96 and <= 107;
        var crystalGrottoLink = cell.Y is >= 104 and <= 107 &&
            cell.X is >= 70 and <= 79;
        var wetlandRoad = cell.X is >= 159 and <= 162 &&
            cell.Y is >= 47 and <= 108;
        var wetlandCauseway = cell.Y is >= 70 and <= 73 &&
            cell.X is >= 126 and <= 162;
        var monolithBranch = cell.Y is >= 42 and <= 48 &&
            cell.X is >= 159 and <= 166;
        var woodsBranch = cell.Y is >= 72 and <= 75 &&
            cell.X is >= 19 and <= 63;
        var woodsLoop = cell.X is >= 40 and <= 43 &&
            cell.Y is >= 64 and <= 99;
        var ruinsBranch = cell.Y is >= 108 and <= 111 &&
            cell.X is >= 96 and <= 176;
        return farmGate || eastWestRoad || northSouthRoad || meadowLoop ||
            southernRoad || crystalValeBranch || crystalGrottoLink ||
            wetlandRoad || wetlandCauseway ||
            monolithBranch || woodsBranch || woodsLoop || ruinsBranch ||
            VillageCatalog.IsVillagePath(cell);
    }

    public static bool IsWater(GridPosition cell)
    {
        if (IsPath(cell))
        {
            return false;
        }

        if (IsScenicReservedCell(cell))
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
            var streamX = 83 + (int)MathF.Round(MathF.Sin(cell.Y * 0.16f) * 4);
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

    public static WorldScenicLandmark? ScenicLandmarkAt(
        GridPosition cell
    ) => ScenicLandmarks.FirstOrDefault(value => value.Position == cell);

    public static bool IsScenicReservedCell(GridPosition cell) =>
        ScenicLandmarks.Any(landmark => landmark.ReservedArea.Contains(cell));

    public static bool IsScenicBlocked(GridPosition cell) =>
        ScenicLandmarks
            .SelectMany(landmark => landmark.CollisionAreas)
            .Any(area => area.Contains(cell));

    public static bool IsWoodlandStarlightCell(GridPosition cell) =>
        cell == WoodlandStarlightCell;

    public static bool IsMeadowStarlightCell(GridPosition cell) =>
        cell == MeadowStarlightCell;

    public static bool IsMoonwaterStarlightCell(GridPosition cell) =>
        cell == MoonwaterStarlightCell;

    public static bool IsFireflyTideGateCell(GridPosition cell) =>
        cell == FireflyTideGateCell;

    public static bool IsCrystalGrottoSurveyEntryCell(GridPosition cell) =>
        cell == CrystalGrottoSurveyLayout.WorldEntryCell;

    public static bool IsStarfallRuinsTrialEntryCell(GridPosition cell) =>
        cell == StarfallRuinsTrialLayout.WorldEntryCell;

    public static bool IsMeadowStarlightReservedCell(GridPosition cell) =>
        MeadowStarlightReservedArea.Contains(cell);

    public static int PropAtlasIndex(GridPosition cell)
    {
        if (!IsInBounds(cell) || IsHomeCell(cell))
        {
            return -1;
        }

        if (IsMeadowStarlightReservedCell(cell) ||
            IsScenicReservedCell(cell))
        {
            return -1;
        }

        var landmark = Landmarks.FirstOrDefault(value => value.Position == cell);
        if (landmark is not null)
        {
            return landmark.AtlasIndex;
        }

        if (CuratedPropByCell.TryGetValue(cell, out var curatedIndex))
        {
            return curatedIndex;
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
                < 4 => 0,
                < 7 => 1,
                < 11 => 4,
                < 14 => 5,
                < 16 => 3,
                _ => -1
            },
            WorldBiome.StarfallMeadow => roll switch
            {
                < 2 => 0,
                < 5 => 4,
                < 11 => 13,
                < 13 => 2,
                < 15 => 5,
                _ => -1
            },
            WorldBiome.CrystalVale => roll switch
            {
                < 6 => 2,
                < 10 => 3,
                < 13 => 13,
                < 15 => 0,
                _ => -1
            },
            WorldBiome.MoonwaterWetlands => roll switch
            {
                < 7 => 14,
                < 10 => 1,
                < 13 => 3,
                < 16 => 5,
                _ => -1
            },
            WorldBiome.StarfallRuins => roll switch
            {
                < 4 => 7,
                < 7 => 3,
                < 10 => 5,
                < 13 => 13,
                < 15 => 2,
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

        if (IsScenicBlocked(cell))
        {
            return true;
        }

        if (IsMeadowStarlightCell(cell))
        {
            return true;
        }

        if (IsFireflyTideGateCell(cell))
        {
            return true;
        }

        if (IsCrystalGrottoSurveyEntryCell(cell))
        {
            return true;
        }

        if (IsStarfallRuinsTrialEntryCell(cell))
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
            IsWater(cell) ||
            IsMeadowStarlightReservedCell(cell) ||
            IsScenicReservedCell(cell) ||
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

    public static GridPosition NearestWalkableCell(GridPosition requested)
    {
        var clamped = new GridPosition(
            Math.Clamp(requested.X, 1, Width - 2),
            Math.Clamp(requested.Y, 1, Height - 2)
        );
        if (!IsBlocked(clamped))
        {
            return clamped;
        }

        for (var distance = 1; distance <= 16; distance++)
        {
            for (var offsetY = -distance;
                 offsetY <= distance;
                 offsetY++)
            {
                var offsetX = distance - Math.Abs(offsetY);
                var left = new GridPosition(
                    clamped.X - offsetX,
                    clamped.Y + offsetY
                );
                if (IsInBounds(left) && !IsBlocked(left))
                {
                    return left;
                }

                if (offsetX == 0)
                {
                    continue;
                }

                var right = new GridPosition(
                    clamped.X + offsetX,
                    clamped.Y + offsetY
                );
                if (IsInBounds(right) && !IsBlocked(right))
                {
                    return right;
                }
            }
        }

        return new GridPosition(
            VillageCatalog.VillageGateCell.X,
            VillageCatalog.VillageGateCell.Y - 1
        );
    }

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
