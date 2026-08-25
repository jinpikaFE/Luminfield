using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class WorldNavigationJourneyPlannerTests
{
    private static readonly WorldBiome[] Regions =
    [
        WorldBiome.Home,
        WorldBiome.LumenVillage,
        WorldBiome.WhisperingWoods,
        WorldBiome.StarfallMeadow,
        WorldBiome.CrystalVale,
        WorldBiome.MoonwaterWetlands,
        WorldBiome.StarfallRuins
    ];

    [Fact]
    public void CreatesAStablePlanForEveryKnownRegionPair()
    {
        foreach (var origin in Regions)
        {
            foreach (var destination in Regions)
            {
                Assert.True(
                    WorldNavigationJourneyPlanner.TryCreate(
                        origin,
                        destination,
                        out var plan
                    )
                );
                Assert.NotNull(plan);
                Assert.Equal(origin, plan.Origin);
                Assert.Equal(destination, plan.Destination);
                AssertSegmentsAreContinuous(plan);
                Assert.Equal(
                    ExpectedSegmentCount(origin, destination),
                    plan.SegmentCount
                );
            }
        }
    }

    [Fact]
    public void SameRegionPlanHasNoSegments()
    {
        foreach (var region in Regions)
        {
            Assert.True(
                WorldNavigationJourneyPlanner.TryCreate(
                    region,
                    region,
                    out var plan
                )
            );

            Assert.NotNull(plan);
            Assert.True(plan.IsSameRegion);
            Assert.Empty(plan.Segments);
        }
    }

    [Fact]
    public void VillageToEveryOuterRegionUsesOneForwardSegment()
    {
        foreach (var destination in PeripheralRegions())
        {
            Assert.True(
                WorldNavigationJourneyPlanner.TryCreate(
                    WorldBiome.LumenVillage,
                    destination,
                    out var plan
                )
            );

            var segment = Assert.Single(plan?.Segments ?? []);

            Assert.False(segment.IsReversed);
            Assert.Equal(WorldBiome.LumenVillage, segment.FromRegion);
            Assert.Equal(destination, segment.ToRegion);
        }
    }

    [Fact]
    public void HomeToEveryPeripheralRegionGoesThroughVillage()
    {
        foreach (var destination in PeripheralRegions())
        {
            Assert.True(
                WorldNavigationJourneyPlanner.TryCreate(
                    WorldBiome.Home,
                    destination,
                    out var plan
                )
            );

            Assert.NotNull(plan);
            Assert.Equal(2, plan.SegmentCount);
            Assert.Equal(WorldBiome.Home, plan.Segments[0].FromRegion);
            Assert.Equal(WorldBiome.LumenVillage, plan.Segments[0].ToRegion);
            Assert.Equal(WorldBiome.LumenVillage, plan.Segments[1].FromRegion);
            Assert.Equal(destination, plan.Segments[1].ToRegion);
        }
    }

    [Fact]
    public void PeripheralToPeripheralPlansUseTwoSegmentsThroughVillage()
    {
        foreach (var origin in PeripheralRegions())
        {
            foreach (var destination in PeripheralRegions())
            {
                if (origin == destination)
                {
                    continue;
                }

                Assert.True(
                    WorldNavigationJourneyPlanner.TryCreate(
                        origin,
                        destination,
                        out var plan
                    )
                );

                Assert.NotNull(plan);
                Assert.Equal(2, plan.SegmentCount);
                Assert.True(plan.Segments[0].IsReversed);
                Assert.Equal(origin, plan.Segments[0].FromRegion);
                Assert.Equal(
                    WorldBiome.LumenVillage,
                    plan.Segments[0].ToRegion
                );
                Assert.False(plan.Segments[1].IsReversed);
                Assert.Equal(
                    WorldBiome.LumenVillage,
                    plan.Segments[1].FromRegion
                );
                Assert.Equal(destination, plan.Segments[1].ToRegion);
            }
        }
    }

    [Fact]
    public void PlansUseExistingStableRouteOptions()
    {
        var knownRoutes = WorldNavigationRouteSelection.Routes
            .ToDictionary(route => route.RouteId, StringComparer.Ordinal);

        foreach (var origin in Regions)
        {
            foreach (var destination in Regions)
            {
                Assert.True(
                    WorldNavigationJourneyPlanner.TryCreate(
                        origin,
                        destination,
                        out var plan
                    )
                );

                Assert.NotNull(plan);
                foreach (var segment in plan.Segments)
                {
                    Assert.True(knownRoutes.TryGetValue(
                        segment.RouteId,
                        out var knownRoute
                    ));
                    Assert.Same(knownRoute, segment);
                }
            }
        }
    }

    [Fact]
    public void RejectsUnknownRegionsWithoutReturningAPlan()
    {
        var unknown = (WorldBiome)999;

        Assert.False(
            WorldNavigationJourneyPlanner.TryCreate(
                unknown,
                WorldBiome.Home,
                out var fromUnknown
            )
        );
        Assert.Null(fromUnknown);

        Assert.False(
            WorldNavigationJourneyPlanner.TryCreate(
                WorldBiome.Home,
                unknown,
                out var toUnknown
            )
        );
        Assert.Null(toUnknown);
    }

    [Fact]
    public void ReturnedSegmentListsAreIndependentSnapshots()
    {
        Assert.True(
            WorldNavigationJourneyPlanner.TryCreate(
                WorldBiome.Home,
                WorldBiome.CrystalVale,
                out var first
            )
        );
        Assert.True(
            WorldNavigationJourneyPlanner.TryCreate(
                WorldBiome.Home,
                WorldBiome.CrystalVale,
                out var second
            )
        );

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first.Segments, second.Segments);
        Assert.Equal(
            first.Segments.Select(segment => segment.RouteId),
            second.Segments.Select(segment => segment.RouteId)
        );
    }

    private static void AssertSegmentsAreContinuous(
        WorldNavigationJourneyPlan plan
    )
    {
        if (plan.Segments.Count == 0)
        {
            Assert.Equal(plan.Origin, plan.Destination);
            return;
        }

        Assert.Equal(plan.Origin, plan.Segments[0].FromRegion);
        Assert.Equal(
            plan.Destination,
            plan.Segments[^1].ToRegion
        );

        for (var index = 1; index < plan.Segments.Count; index++)
        {
            Assert.Equal(
                plan.Segments[index - 1].ToRegion,
                plan.Segments[index].FromRegion
            );
        }
    }

    private static int ExpectedSegmentCount(
        WorldBiome origin,
        WorldBiome destination
    )
    {
        if (origin == destination)
        {
            return 0;
        }

        if (origin == WorldBiome.LumenVillage ||
            destination == WorldBiome.LumenVillage)
        {
            return 1;
        }

        if (origin == WorldBiome.Home && destination != WorldBiome.Home)
        {
            return 2;
        }

        if (destination == WorldBiome.Home && origin != WorldBiome.Home)
        {
            return 2;
        }

        return 2;
    }

    private static IEnumerable<WorldBiome> PeripheralRegions() =>
        Regions.Where(region =>
            region != WorldBiome.Home &&
            region != WorldBiome.LumenVillage
        );
}
