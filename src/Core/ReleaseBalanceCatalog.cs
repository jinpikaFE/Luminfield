namespace Luminfield.Core;

public static class ReleaseBalanceCatalog
{
    public const string ProfileId = "release_balance_v1";
    public const int MinimumDailyCommissionReward = 40;
    public const int MaximumDailyCommissionReward = 90;
    public const int MaximumDailyCommissionRequirement = 4;
    public const int MinimumFestivalCurrencyReward = 4;
    public const int MaximumFestivalCurrencyReward = 10;
    public const int MinimumConstructionNights = 2;
    public const int MaximumConstructionNights = 5;

    public static IReadOnlyList<string> Audit()
    {
        var issues = new List<string>();
        AuditEconomy(issues);
        AuditDropsAndGrowth(issues);
        AuditTasks(issues);
        AuditFestivals(issues);
        AuditConstruction(issues);
        return issues;
    }

    private static void AuditEconomy(List<string> issues)
    {
        foreach (var itemId in DataCatalog.SellableItemIds)
        {
            var item = DataCatalog.Item(itemId);
            if (item.SellPrice <= 0)
            {
                issues.Add($"sell_price:{itemId}");
            }
        }

        foreach (var crop in DataCatalog.Crops.Values)
        {
            var seed = DataCatalog.Item(crop.SeedItemId);
            var harvest = DataCatalog.Item(crop.HarvestItemId);
            var expectedHarvests = crop.RegrowthNights > 0 ? 3 : 1;
            if (seed.BuyPrice > 0 &&
                harvest.SellPrice * expectedHarvests <= seed.BuyPrice)
            {
                issues.Add($"crop_margin:{crop.Id}");
            }
        }
    }

    private static void AuditDropsAndGrowth(List<string> issues)
    {
        var respawnDays = new[]
        {
            WorldResourceSystem.CrystalRespawnDays,
            WorldResourceSystem.TreeRespawnDays,
            WorldResourceSystem.RenewedWoodlandTreeRespawnDays
        };
        if (!respawnDays.SequenceEqual(
                [2, CalendarSystem.DaysPerWeek, 4]
            ))
        {
            issues.Add("resource_respawn");
        }

        var baseDrops = new[]
        {
            ForageSystem.BaseCollectionYield,
            WorldResourceSystem.BaseTreeYield,
            WorldResourceSystem.BaseCrystalYield,
            DeepMineSystem.BaseEnemyMineralDrop,
            DeepMineSystem.BaseExcavationYield
        };
        if (!baseDrops.SequenceEqual([1, 2, 1, 1, 1]) ||
            DeepMineSystem.GemseekerExcavationYield != 2)
        {
            issues.Add("resource_drops");
        }

        var maximumSkillExperience = new[]
        {
            FarmingSkillCatalog.Levels[^1].RequiredExperience,
            GatheringSkillCatalog.LevelThresholds[^1],
            FishingProgressionCatalog.LevelThresholds[^1],
            AdventureSkillCatalog.LevelThresholds[^1]
        };
        if (maximumSkillExperience.Any(value => value is < 250 or > 450))
        {
            issues.Add("skill_growth");
        }
    }

    private static void AuditTasks(List<string> issues)
    {
        if (DataCatalog.DailyCommissionRotation.Count != 3 ||
            DataCatalog.DailyCommissionRotation
                .Select(definition => definition.Kind)
                .Distinct()
                .Count() != Enum.GetValues<DailyCommissionKind>().Length)
        {
            issues.Add("daily_frequency");
        }

        foreach (var commission in DataCatalog.DailyCommissionRotation)
        {
            if (commission.RequiredCount is < 1 or > MaximumDailyCommissionRequirement ||
                commission.RewardCoins is < MinimumDailyCommissionReward or
                    > MaximumDailyCommissionReward)
            {
                issues.Add($"daily_reward:{commission.Id}");
            }
        }

        if (DataCatalog.WeeklyCommission.Stages.Count != 3 ||
            DataCatalog.WeeklyCommission.RewardCoins is < 100 or > 180)
        {
            issues.Add("weekly_reward");
        }
    }

    private static void AuditFestivals(List<string> issues)
    {
        var awardGroups = new[]
        {
            FestivalCatalog.Awards,
            FestivalCatalog.GleamriseAwards,
            FestivalCatalog.LongnightAwards,
            FestivalCatalog.FireflyAwards
        };
        foreach (var awards in awardGroups)
        {
            var rewards = awards.Select(award => award.ScripReward).ToArray();
            if (!rewards.SequenceEqual(
                    rewards.Order()
                ) ||
                rewards.First() != MinimumFestivalCurrencyReward ||
                rewards.Last() != MaximumFestivalCurrencyReward)
            {
                issues.Add("festival_awards");
            }
        }

        var offerGroups = new[]
        {
            FestivalCatalog.StarharvestOffers.Values,
            FestivalCatalog.GleamriseOffers.Values,
            FestivalCatalog.LongnightOffers.Values,
            FestivalCatalog.FireflyOffers.Values
        };
        if (offerGroups.SelectMany(group => group)
            .Any(offer => offer.ScripCost is < 2 or > 6))
        {
            issues.Add("festival_offer_cost");
        }
    }

    private static void AuditConstruction(List<string> issues)
    {
        foreach (var project in ConstructionCatalog.Projects)
        {
            if (project.CoinCost <= 0 ||
                project.RequiredNights is < MinimumConstructionNights or
                    > MaximumConstructionNights ||
                project.Materials.Count == 0 ||
                project.Materials.Any(material => material.Count <= 0))
            {
                issues.Add($"construction:{project.Id}");
            }
        }

        if (ConstructionCatalog.SixfoldStarGate.CoinCost <=
            ConstructionCatalog.CottageSecondUpgrade.CoinCost)
        {
            issues.Add("construction_finale_cost");
        }
    }
}
