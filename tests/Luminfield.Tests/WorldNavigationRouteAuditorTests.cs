using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class WorldNavigationRouteAuditorTests
{
    [Fact]
    public void AuditsEveryRouteContractAsOneContinuousWalkableRoute()
    {
        var audits = WorldNavigationRouteAuditor.AuditAll();
        var contracts = WorldNavigationGuideCatalog.RouteContracts
            .ToDictionary(contract => contract.Id, StringComparer.Ordinal);

        Assert.Equal(contracts.Count, audits.Count);

        foreach (var audit in audits)
        {
            var contract = contracts[audit.ContractId];

            Assert.Equal(contract.FromRegion, audit.FromRegion);
            Assert.Equal(contract.ToRegion, audit.ToRegion);
            Assert.Equal(contract.Start, audit.Start);
            Assert.Equal(contract.End, audit.End);
            Assert.Equal(audit.Start, audit.Path[0]);
            Assert.Equal(audit.End, audit.Path[^1]);
            Assert.InRange(
                audit.MaximumUnguidedDistance,
                0,
                contract.MaximumUnguidedDistance
            );
            Assert.True(audit.PathLength >= audit.VisibleGuides.Count);

            foreach (var cell in audit.Path)
            {
                Assert.True(WorldDefinition.IsInBounds(cell), audit.ContractId);
                Assert.False(WorldDefinition.IsBlocked(cell), audit.ContractId);
            }

            foreach (var pair in audit.Path.Zip(audit.Path.Skip(1)))
            {
                Assert.Equal(1, ManhattanDistance(pair.First, pair.Second));
            }
        }
    }

    [Fact]
    public void RouteAuditsPreserveGuideOrderAndPathIndexes()
    {
        var guides = WorldNavigationGuideCatalog.Guides
            .ToDictionary(guide => guide.Id, StringComparer.Ordinal);

        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var contract = WorldNavigationGuideCatalog.RouteContracts
                .Single(route => route.Id == audit.ContractId);

            Assert.Equal(
                contract.GuideIds,
                audit.VisibleGuides.Select(guide => guide.GuideId)
            );

            foreach (var visibleGuide in audit.VisibleGuides)
            {
                var guide = guides[visibleGuide.GuideId];

                Assert.True(
                    WorldDefinition.IsPath(guide.Position),
                    visibleGuide.GuideId
                );
                Assert.False(
                    WorldDefinition.IsBlocked(guide.Position),
                    visibleGuide.GuideId
                );
                Assert.Equal(guide.Kind, visibleGuide.Kind);
                Assert.Equal(guide.Region, visibleGuide.Region);
                Assert.Equal(guide.Position, visibleGuide.Position);
                Assert.Equal(
                    guide.Position,
                    audit.Path[visibleGuide.PathIndex]
                );
            }

            foreach (var segment in audit.Segments)
            {
                Assert.True(WorldDefinition.IsPath(segment.Start), segment.FromId);
                Assert.True(WorldDefinition.IsPath(segment.End), segment.ToId);
                Assert.False(WorldDefinition.IsBlocked(segment.Start), segment.FromId);
                Assert.False(WorldDefinition.IsBlocked(segment.End), segment.ToId);
                Assert.Equal(segment.Start, audit.Path[segment.StartIndex]);
                Assert.Equal(segment.End, audit.Path[segment.EndIndex]);
                Assert.Equal(
                    segment.StepCount,
                    segment.EndIndex - segment.StartIndex
                );
                Assert.InRange(
                    segment.StepCount,
                    0,
                    contract.MaximumUnguidedDistance
                );
            }
        }
    }

    [Fact]
    public void RouteAuditsRecordStableAcceptanceMetrics()
    {
        var expectations = new Dictionary<string, RouteMetric>(
            StringComparer.Ordinal
        )
        {
            ["route_home_to_lumen_village"] = new(159, 7, 18),
            ["route_lumen_village_to_whispering_woods"] = new(174, 1, 18),
            ["route_lumen_village_to_starfall_meadow"] = new(64, 2, 17),
            ["route_lumen_village_to_crystal_vale"] = new(126, 3, 18),
            ["route_lumen_village_to_moonwater_wetlands"] = new(138, 11, 18),
            ["route_lumen_village_to_starfall_ruins"] = new(123, 8, 18)
        };

        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var expected = expectations[audit.ContractId];

            Assert.Equal(expected.PathLength, audit.PathLength);
            Assert.Equal(expected.TurnCount, audit.TurnPoints.Count);
            Assert.Equal(
                expected.MaximumUnguidedDistance,
                audit.MaximumUnguidedDistance
            );
        }
    }

    [Fact]
    public void RouteAuditsKeepDocumentedStartAndEndRegions()
    {
        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            Assert.Equal(audit.FromRegion, WorldDefinition.GetBiome(audit.Start));
            Assert.Equal(audit.ToRegion, WorldDefinition.GetBiome(audit.End));
        }
    }

    private static int ManhattanDistance(
        GridPosition left,
        GridPosition right
    ) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

    private readonly record struct RouteMetric(
        int PathLength,
        int TurnCount,
        int MaximumUnguidedDistance
    );
}
