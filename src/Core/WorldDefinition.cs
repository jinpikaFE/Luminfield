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
    private static readonly GridPosition[] CardinalDirections =
    [
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1)
    ];
    private static readonly Lazy<HashSet<GridPosition>>
        IsolatedWalkableCells = new(BuildIsolatedWalkableCells);

    public const int Width = 256;
    public const int Height = 192;
    public const int ChunkSize = 32;
    public const int ChunkColumns = Width / ChunkSize;
    public const int ChunkRows = Height / ChunkSize;
    public static readonly GridArea BeginnerZoneBounds = new(0, 0, 63, 63);
    public const string WoodlandStarlightLandmarkId = "woods_lantern";
    public static readonly GridPosition WoodlandStarlightCell = new(34, 104);
    public const string MeadowStarlightLandmarkId =
        "meadow_starlight";
    public static readonly GridPosition MeadowStarlightCell = new(82, 24);
    public static readonly GridArea MeadowStarlightReservedArea =
        new(80, 20, 84, 24);
    public const string MoonwaterStarlightLandmarkId = "wetland_monolith";
    public static readonly GridPosition MoonwaterStarlightCell = new(230, 48);
    public const string CrystalWellLandmarkId = "crystal_well";
    public static readonly GridPosition CrystalWellCell = new(84, 158);
    public const string FireflyTideGateLandmarkId =
        "firefly_tide_gate";
    public static readonly GridPosition FireflyTideGateCell = new(226, 70);
    public const string StarfallRuinsStarlightLandmarkId = "ruins_pillar";
    public static readonly GridPosition StarfallRuinsStarlightCell =
        new(160, 158);
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
        new(
            "southern_cache",
            new GridPosition(226, 172),
            12,
            "world.landmark.southern_cache"
        )
    ];

    public static readonly IReadOnlyList<WorldScenicLandmark>
        ScenicLandmarks =
        [
            new(
                "beginner_moon_lantern_arch",
                new GridPosition(52, 49),
                0,
                new GridArea(44, 39, 60, 49),
                [
                    new GridArea(45, 42, 48, 45),
                    new GridArea(56, 42, 59, 45)
                ]
            ),
            new(
                "meadow_moonflower_circle",
                new GridPosition(136, 14),
                1,
                new GridArea(131, 6, 141, 14),
                [new GridArea(132, 12, 140, 14)]
            ),
            new(
                "woods_moonroot_grove",
                new GridPosition(52, 112),
                2,
                new GridArea(46, 103, 58, 112),
                [new GridArea(48, 109, 56, 112)]
            ),
            new(
                "wetland_boardwalk_islet",
                new GridPosition(214, 56),
                3,
                new GridArea(208, 48, 220, 56),
                [new GridArea(211, 54, 217, 56)]
            ),
            new(
                "crystal_stepped_ridge",
                new GridPosition(92, 174),
                4,
                new GridArea(86, 163, 98, 174),
                [new GridArea(87, 170, 97, 174)]
            ),
            new(
                "ruins_broken_colonnade",
                new GridPosition(184, 178),
                5,
                new GridArea(178, 168, 190, 178),
                [new GridArea(179, 174, 189, 178)]
            ),
            new(
                "city_civic_garden_pavilion",
                new GridPosition(148, 54),
                6,
                new GridArea(140, 45, 156, 54),
                [
                    new GridArea(141, 49, 144, 54),
                    new GridArea(152, 49, 155, 54)
                ]
            ),
            new(
                "city_facilities_gateway",
                new GridPosition(128, 112),
                7,
                new GridArea(119, 102, 137, 112),
                [
                    new GridArea(120, 106, 123, 112),
                    new GridArea(133, 106, 136, 112)
                ]
            )
        ];

    public static readonly IReadOnlyList<WorldPropPlacement> CuratedProps =
    [
        new(new GridPosition(28, 88), 0),
        new(new GridPosition(52, 88), 5),
        new(new GridPosition(29, 121), 8),
        new(new GridPosition(48, 134), 1),
        new(new GridPosition(56, 165), 4),
        new(new GridPosition(26, 176), 11),
        new(new GridPosition(72, 14), 13),
        new(new GridPosition(106, 26), 4),
        new(new GridPosition(154, 14), 13),
        new(new GridPosition(184, 28), 5),
        new(new GridPosition(70, 154), 2),
        new(new GridPosition(76, 178), 3),
        new(new GridPosition(108, 166), 2),
        new(new GridPosition(119, 183), 7),
        new(new GridPosition(202, 22), 14),
        new(new GridPosition(242, 18), 15),
        new(new GridPosition(198, 28), 14),
        new(new GridPosition(244, 76), 1),
        new(new GridPosition(214, 92), 8),
        new(new GridPosition(238, 88), 5),
        new(new GridPosition(248, 24), 14),
        new(new GridPosition(138, 146), 7),
        new(new GridPosition(154, 182), 3),
        new(new GridPosition(202, 134), 7),
        new(new GridPosition(222, 166), 13),
        new(new GridPosition(244, 182), 7),
        new(new GridPosition(68, 70), 13),
        new(new GridPosition(188, 42), 13),
        new(new GridPosition(68, 112), 13),
        new(new GridPosition(188, 104), 13)
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

    public static bool IsBeginnerZoneCell(GridPosition cell) =>
        BeginnerZoneBounds.Contains(cell);

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
        if (IsBeginnerZoneCell(cell))
        {
            return WorldBiome.Home;
        }

        if (VillageCatalog.IsVillageCell(cell))
        {
            return WorldBiome.LumenVillage;
        }

        if (cell.X < 64 && cell.Y >= 64)
        {
            return WorldBiome.WhisperingWoods;
        }

        if (cell.X is >= 64 and < 192 && cell.Y < 32)
        {
            return WorldBiome.StarfallMeadow;
        }

        if (cell.X >= 192 && cell.Y < 96)
        {
            return WorldBiome.MoonwaterWetlands;
        }

        if ((cell.X >= 192 && cell.Y >= 96) ||
            (cell.X >= 128 && cell.Y >= 128))
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
        if (IsScenicBlocked(cell))
        {
            return false;
        }

        var farmGate = cell.X is >= 17 and <= 21 &&
            cell.Y is >= 29 and <= 48;
        var beginnerLink = cell.Y is >= 44 and <= 51 &&
            cell.X is >= 18 and <= 72;
        var eastWestRoad = cell.Y is >= 74 and <= 85 &&
            cell.X is >= 18 and <= 238;
        var northSouthRoad = cell.X is >= 122 and <= 133 &&
            cell.Y is >= 16 and <= 176;
        var meadowLoop = cell.Y is >= 18 and <= 25 &&
            cell.X is >= 64 and <= 190;
        var southernRoad = cell.Y is >= 138 and <= 145 &&
            cell.X is >= 36 and <= 238;
        var crystalValeBranch = cell.X is >= 80 and <= 87 &&
            cell.Y is >= 128 and <= 158;
        var crystalGrottoLink = cell.Y is >= 138 and <= 145 &&
            cell.X is >= 70 and <= 85;
        var wetlandRoad = cell.X is >= 222 and <= 229 &&
            cell.Y is >= 36 and <= 112;
        var wetlandCauseway = cell.Y is >= 76 and <= 83 &&
            cell.X is >= 190 and <= 227;
        var monolithBranch = cell.Y is >= 44 and <= 51 &&
            cell.X is >= 224 and <= 232;
        var woodsBranch = cell.Y is >= 76 and <= 83 &&
            cell.X is >= 19 and <= 63;
        var woodsLoop = cell.X is >= 38 and <= 45 &&
            cell.Y is >= 64 and <= 176;
        var ruinsBranch = cell.Y is >= 154 and <= 161 &&
            cell.X is >= 126 and <= 238;
        return farmGate || eastWestRoad || northSouthRoad || meadowLoop ||
            beginnerLink || southernRoad || crystalValeBranch ||
            crystalGrottoLink ||
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

        if (IsScenicReservedCell(cell) &&
            GetBiome(cell) != WorldBiome.MoonwaterWetlands)
        {
            return false;
        }

        if (GetBiome(cell) == WorldBiome.MoonwaterWetlands)
        {
            if (IsWetlandScenicIslet(cell))
            {
                return false;
            }

            var dx = (cell.X - 224) / 27f;
            var dy = (cell.Y - 45) / 22f;
            if (dx * dx + dy * dy < 1)
            {
                return true;
            }

            var hash = Hash(cell.X, cell.Y);
            return cell.Y > 48 && hash % 11 < 3;
        }

        if (GetBiome(cell) == WorldBiome.CrystalVale)
        {
            var streamX = 83 +
                (int)MathF.Round(MathF.Sin(cell.Y * 0.16f) * 4);
            return Math.Abs(cell.X - streamX) <= 2;
        }

        return false;
    }

    private static bool IsWetlandScenicIslet(GridPosition cell)
    {
        var dx = (cell.X - 214) / 7f;
        var dy = (cell.Y - 54) / 5f;
        return dx * dx + dy * dy <= 1f;
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
            IsScenicReservedCell(cell) ||
            CityExpansionLayout.IsReserved(cell))
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

            return -1;
        }

        return GetBiome(cell) switch
        {
            WorldBiome.WhisperingWoods => roll switch
            {
                < 1 => 0,
                < 2 => 1,
                < 3 => 4,
                < 4 => 5,
                < 5 => 3,
                _ => -1
            },
            WorldBiome.StarfallMeadow => roll switch
            {
                < 1 => 0,
                < 2 => 4,
                < 3 => 13,
                < 4 => 5,
                _ => -1
            },
            WorldBiome.CrystalVale => roll switch
            {
                < 2 => 2,
                < 3 => 3,
                < 4 => 13,
                < 5 => 0,
                _ => -1
            },
            WorldBiome.MoonwaterWetlands => roll switch
            {
                < 2 => 14,
                < 3 => 1,
                < 4 => 3,
                < 5 => 5,
                _ => -1
            },
            WorldBiome.StarfallRuins => roll switch
            {
                < 1 => 7,
                < 2 => 3,
                < 3 => 5,
                < 4 => 13,
                < 5 => 2,
                _ => -1
            },
            _ => -1
        };
    }

    public static bool IsBlocked(GridPosition cell)
    {
        if (IsBaseBlocked(cell))
        {
            return true;
        }

        // Procedural water and prop noise can otherwise leave a one-cell
        // pocket that is marked walkable despite having no cardinal entry.
        // Treat that pocket as terrain so navigation and safe-position repair
        // share one connected static world graph.
        return IsolatedWalkableCells.Value.Contains(cell);
    }

    private static HashSet<GridPosition> BuildIsolatedWalkableCells()
    {
        var isolated = new HashSet<GridPosition>();
        for (var y = 1; y < Height - 1; y++)
        {
            for (var x = 1; x < Width - 1; x++)
            {
                var cell = new GridPosition(x, y);
                if (IsBaseBlocked(cell))
                {
                    continue;
                }

                var hasCardinalEntry = CardinalDirections.Any(direction =>
                    !IsBaseBlocked(new GridPosition(
                        cell.X + direction.X,
                        cell.Y + direction.Y
                    ))
                );
                if (!hasCardinalEntry)
                {
                    isolated.Add(cell);
                }
            }
        }

        return isolated;
    }

    private static bool IsBaseBlocked(GridPosition cell)
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
                cell.X is not (>= 17 and <= 21);
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

        if (CityExpansionLayout.IsBlocked(cell))
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
            CityExpansionLayout.IsReserved(cell) ||
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
