using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class WorldSeasonVisualCatalogTests
{
    [Theory]
    [InlineData(14, WorldSeasonVisualVariant.Default)]
    [InlineData(15, WorldSeasonVisualVariant.Rainveil)]
    [InlineData(28, WorldSeasonVisualVariant.Rainveil)]
    [InlineData(29, WorldSeasonVisualVariant.Starharvest)]
    [InlineData(42, WorldSeasonVisualVariant.Starharvest)]
    [InlineData(43, WorldSeasonVisualVariant.Longnight)]
    [InlineData(56, WorldSeasonVisualVariant.Longnight)]
    [InlineData(57, WorldSeasonVisualVariant.Default)]
    [InlineData(71, WorldSeasonVisualVariant.Rainveil)]
    [InlineData(85, WorldSeasonVisualVariant.Starharvest)]
    public void SelectsWorldAspectFromAbsoluteDay(
        int day,
        WorldSeasonVisualVariant expected
    )
    {
        Assert.Equal(expected, WorldSeasonVisualCatalog.ForDay(day).Variant);
    }

    [Fact]
    public void ProfilesUseDistinctAtlasesAndStaticInstances()
    {
        var baseline = WorldSeasonVisualCatalog.ForDay(14);
        var rainveil = WorldSeasonVisualCatalog.ForDay(15);
        var starharvest = WorldSeasonVisualCatalog.ForDay(29);
        var longnight = WorldSeasonVisualCatalog.ForDay(43);

        Assert.Equal(
            WorldSeasonVisualCatalog.DefaultPropAtlasTexturePath,
            baseline.PropAtlasTexturePath
        );
        Assert.Equal(
            WorldSeasonVisualCatalog.RainveilPropAtlasTexturePath,
            rainveil.PropAtlasTexturePath
        );
        Assert.Equal(
            WorldSeasonVisualCatalog.StarharvestPropAtlasTexturePath,
            starharvest.PropAtlasTexturePath
        );
        Assert.Equal(
            WorldSeasonVisualCatalog.LongnightPropAtlasTexturePath,
            longnight.PropAtlasTexturePath
        );
        Assert.NotEqual(baseline.GroundModulate, rainveil.GroundModulate);
        Assert.NotEqual(rainveil.GroundModulate, starharvest.GroundModulate);
        Assert.NotEqual(starharvest.GroundModulate, longnight.GroundModulate);
        Assert.Same(rainveil, WorldSeasonVisualCatalog.ForDay(28));
        Assert.Same(starharvest, WorldSeasonVisualCatalog.ForDay(42));
        Assert.Same(longnight, WorldSeasonVisualCatalog.ForDay(56));
    }

    [Fact]
    public void AtlasContractKeepsAllExistingPropIndices()
    {
        Assert.Equal(4, WorldSeasonVisualCatalog.GroundAtlasColumns);
        Assert.Equal(8, WorldSeasonVisualCatalog.GroundAtlasRows);
        Assert.Equal(16, WorldSeasonVisualCatalog.GroundAtlasCellSize);
        Assert.Equal(7, WorldSeasonVisualCatalog.WaterAtlasRow);
        Assert.Equal(4, WorldSeasonVisualCatalog.ShoreAtlasColumns);
        Assert.Equal(4, WorldSeasonVisualCatalog.ShoreAtlasRows);
        Assert.Equal(
            "res://assets/generated/world/terrain/world_ground_biomes.png",
            WorldSeasonVisualCatalog.GroundAtlasTexturePath
        );
        Assert.Equal(
            "res://assets/generated/world/terrain/world_shore_tiles.png",
            WorldSeasonVisualCatalog.ShoreAtlasTexturePath
        );
        Assert.Equal(4, WorldSeasonVisualCatalog.PropAtlasColumns);
        Assert.Equal(4, WorldSeasonVisualCatalog.PropAtlasRows);
        Assert.Equal(16, WorldSeasonVisualCatalog.PropAtlasEntryCount);
        Assert.Equal(313.5f, WorldSeasonVisualCatalog.PropAtlasCellSize);
    }

    [Fact]
    public void GroundRowsAndShoreMasksFollowStableWorldTopology()
    {
        foreach (var biome in Enum.GetValues<WorldBiome>())
        {
            Assert.Equal((int)biome, WorldSeasonVisualCatalog.GroundAtlasRow(biome));
        }

        var waterCells = Enumerable.Range(0, WorldDefinition.Width)
            .SelectMany(x => Enumerable.Range(0, WorldDefinition.Height)
                .Select(y => new GridPosition(x, y)))
            .Where(WorldDefinition.IsWater)
            .ToArray();

        Assert.NotEmpty(waterCells);
        Assert.Contains(waterCells, cell =>
            WorldSeasonVisualCatalog.ShoreMaskAt(cell) == 0);
        Assert.Contains(waterCells, cell =>
            WorldSeasonVisualCatalog.ShoreMaskAt(cell) != 0);
        Assert.All(waterCells, cell => Assert.InRange(
            WorldSeasonVisualCatalog.ShoreMaskAt(cell),
            0,
            15
        ));
    }

    [Fact]
    public void SeasonProfilesDoNotChangeWorldResourceSemantics()
    {
        var cells = Enumerable.Range(0, WorldDefinition.Width)
            .SelectMany(x => Enumerable.Range(0, WorldDefinition.Height)
                .Select(y => new GridPosition(x, y)))
            .Where(cell =>
                WorldDefinition.ResourceAt(cell) is
                    WorldResourceKind.Tree or WorldResourceKind.Crystal)
            .Take(24)
            .ToArray();

        Assert.NotEmpty(cells);
        var snapshot = cells.Select(cell => new
        {
            Cell = cell,
            AtlasIndex = WorldDefinition.PropAtlasIndex(cell),
            Resource = WorldDefinition.ResourceAt(cell),
            Blocked = WorldDefinition.IsBlocked(cell),
            Water = WorldDefinition.IsWater(cell),
            Path = WorldDefinition.IsPath(cell)
        }).ToArray();

        foreach (var day in new[] { 14, 15, 29, 43, 71, 85 })
        {
            _ = WorldSeasonVisualCatalog.ForDay(day);
            foreach (var expected in snapshot)
            {
                Assert.Equal(
                    expected.AtlasIndex,
                    WorldDefinition.PropAtlasIndex(expected.Cell)
                );
                Assert.Equal(
                    expected.Resource,
                    WorldDefinition.ResourceAt(expected.Cell)
                );
                Assert.Equal(
                    expected.Blocked,
                    WorldDefinition.IsBlocked(expected.Cell)
                );
                Assert.Equal(
                    expected.Water,
                    WorldDefinition.IsWater(expected.Cell)
                );
                Assert.Equal(
                    expected.Path,
                    WorldDefinition.IsPath(expected.Cell)
                );
            }
        }
    }

    [Fact]
    public void WorldAspectNeedsNoSaveSchemaMigration()
    {
        Assert.Equal(1, SaveService.CurrentSchemaVersion);
        Assert.Equal(
            WorldSeasonVisualVariant.Rainveil,
            WorldSeasonVisualCatalog.ForDay(15).Variant
        );
        Assert.Equal(
            WorldSeasonVisualVariant.Starharvest,
            WorldSeasonVisualCatalog.ForDay(29).Variant
        );
        Assert.Equal(
            WorldSeasonVisualVariant.Longnight,
            WorldSeasonVisualCatalog.ForDay(43).Variant
        );
    }
}
