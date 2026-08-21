using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class PhaseGBalanceTests
{
    [Fact]
    public void ReleaseCandidateBalanceProfileHasNoCatalogViolations()
    {
        Assert.Equal("release_balance_v1", ReleaseBalanceCatalog.ProfileId);
        Assert.Empty(ReleaseBalanceCatalog.Audit());
    }

    [Fact]
    public void SkillCapsRemainInOneReleaseCandidateProgressionBand()
    {
        var caps = new[]
        {
            FarmingSkillCatalog.Levels[^1].RequiredExperience,
            GatheringSkillCatalog.LevelThresholds[^1],
            FishingProgressionCatalog.LevelThresholds[^1],
            AdventureSkillCatalog.LevelThresholds[^1]
        };

        Assert.All(caps, cap => Assert.InRange(cap, 250, 450));
        Assert.InRange(caps.Max() - caps.Min(), 0, 180);
    }

    [Fact]
    public void TaskCadenceCoversPlantGatherDeliverWithoutResourceSpikes()
    {
        Assert.Equal(
            Enum.GetValues<DailyCommissionKind>().Order(),
            DataCatalog.DailyCommissionRotation
                .Select(definition => definition.Kind)
                .Order()
        );
        Assert.All(
            DataCatalog.DailyCommissionRotation,
            commission =>
            {
                Assert.InRange(commission.RequiredCount, 1, 4);
                Assert.InRange(commission.RewardCoins, 40, 90);
            }
        );
        Assert.Equal(3, DataCatalog.WeeklyCommission.Stages.Count);
    }

    [Fact]
    public void ResourceDropsStayInsideTheReleaseCandidateProfile()
    {
        Assert.Equal(1, ForageSystem.BaseCollectionYield);
        Assert.Equal(2, WorldResourceSystem.BaseTreeYield);
        Assert.Equal(1, WorldResourceSystem.BaseCrystalYield);
        Assert.Equal(1, DeepMineSystem.BaseEnemyMineralDrop);
        Assert.Equal(1, DeepMineSystem.BaseExcavationYield);
        Assert.Equal(2, DeepMineSystem.GemseekerExcavationYield);
    }
}
