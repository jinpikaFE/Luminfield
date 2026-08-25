using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class ImmediateFeedbackPresenterTests
{
    private static readonly ImmediateFeedbackDomain[] AllFeedbackDomains =
    [
        ImmediateFeedbackDomain.Tool,
        ImmediateFeedbackDomain.Watering,
        ImmediateFeedbackDomain.Harvest,
        ImmediateFeedbackDomain.Pickup,
        ImmediateFeedbackDomain.Processing,
        ImmediateFeedbackDomain.Fishing,
        ImmediateFeedbackDomain.Damage,
        ImmediateFeedbackDomain.Dodge,
        ImmediateFeedbackDomain.Reward
    ];

    public static TheoryData<
        ImmediateFeedbackDomain,
        ImmediateFeedbackOutcome,
        string
    > FeedbackOutcomeMatrix
    {
        get
        {
            var matrix = new TheoryData<
                ImmediateFeedbackDomain,
                ImmediateFeedbackOutcome,
                string
            >();

            foreach (var domain in AllFeedbackDomains)
            {
                matrix.Add(
                    domain,
                    ImmediateFeedbackOutcome.Success,
                    SuccessKeyFor(domain)
                );
                matrix.Add(
                    domain,
                    ImmediateFeedbackOutcome.ResourceBlocked,
                    ResourceBlockedKeyFor(domain)
                );
                matrix.Add(
                    domain,
                    ImmediateFeedbackOutcome.ToolMismatch,
                    ToolMismatchKeyFor(domain)
                );
                matrix.Add(
                    domain,
                    ImmediateFeedbackOutcome.Failure,
                    FailureKeyFor(domain)
                );
            }

            return matrix;
        }
    }

    [Theory]
    [MemberData(nameof(FeedbackOutcomeMatrix))]
    public void FeedbackMatrixCoversEveryDomainAndOutcome(
        ImmediateFeedbackDomain domain,
        ImmediateFeedbackOutcome expectedOutcome,
        string messageKey
    )
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            domain,
            ResultFor(expectedOutcome, messageKey)
        );

        Assert.Equal(domain, cue.Domain);
        Assert.Equal(expectedOutcome, cue.Outcome);
        Assert.Equal(messageKey, cue.MessageKey);
        Assert.Equal(ExpectedDefaultIcon(domain), cue.IconItemId);
        Assert.False(cue.ReducedEffects);
        Assert.True(cue.DurationSeconds > 0);
        Assert.True(cue.PulseScale > 1f);
        Assert.True(cue.BorderWidth >= 1);
        if (expectedOutcome == ImmediateFeedbackOutcome.Success &&
            domain != ImmediateFeedbackDomain.Damage)
        {
            Assert.Equal(0f, cue.ShakePixels);
        }
        else
        {
            Assert.True(cue.ShakePixels > 0);
        }
    }

    [Theory]
    [MemberData(nameof(FeedbackOutcomeMatrix))]
    public void ReducedEffectsMatrixRemovesMotionForEveryDomainAndOutcome(
        ImmediateFeedbackDomain domain,
        ImmediateFeedbackOutcome expectedOutcome,
        string messageKey
    )
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            domain,
            ResultFor(expectedOutcome, messageKey),
            new AccessibilitySettings { ScreenShakePercent = 0 }
        );

        Assert.Equal(domain, cue.Domain);
        Assert.Equal(expectedOutcome, cue.Outcome);
        Assert.True(cue.ReducedEffects);
        Assert.Equal(0f, cue.ShakePixels);
        Assert.Equal(1f, cue.PulseScale);
        Assert.Equal(1.1f, cue.DurationSeconds);
    }

    [Theory]
    [MemberData(nameof(FeedbackOutcomeMatrix))]
    public void FeedbackAudioMappingCoversEveryDomainAndOutcomeWithoutDuplicatingSuccess(
        ImmediateFeedbackDomain domain,
        ImmediateFeedbackOutcome expectedOutcome,
        string messageKey
    )
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            domain,
            ResultFor(expectedOutcome, messageKey)
        );

        Assert.Equal(
            ExpectedSoundFor(domain, expectedOutcome),
            ImmediateFeedbackAudio.SoundFor(cue)
        );
    }

    [Theory]
    [MemberData(nameof(FeedbackOutcomeMatrix))]
    public void ReducedEffectsKeepSemanticFeedbackSounds(
        ImmediateFeedbackDomain domain,
        ImmediateFeedbackOutcome expectedOutcome,
        string messageKey
    )
    {
        var standard = ImmediateFeedbackPresenter.FromActionResult(
            domain,
            ResultFor(expectedOutcome, messageKey)
        );
        var reduced = ImmediateFeedbackPresenter.FromActionResult(
            domain,
            ResultFor(expectedOutcome, messageKey),
            new AccessibilitySettings { ScreenShakePercent = 0 }
        );

        Assert.True(reduced.ReducedEffects);
        Assert.Equal(
            ImmediateFeedbackAudio.SoundFor(standard),
            ImmediateFeedbackAudio.SoundFor(reduced)
        );
    }

    [Theory]
    [InlineData(ImmediateFeedbackDomain.Tool, DataCatalog.HandId)]
    [InlineData(ImmediateFeedbackDomain.Watering, DataCatalog.WateringCanId)]
    [InlineData(ImmediateFeedbackDomain.Harvest, DataCatalog.StarbudId)]
    [InlineData(ImmediateFeedbackDomain.Pickup, DataCatalog.LumenwoodId)]
    [InlineData(ImmediateFeedbackDomain.Processing, DataCatalog.StarbudPreserveId)]
    [InlineData(ImmediateFeedbackDomain.Fishing, DataCatalog.FishingRodId)]
    [InlineData(ImmediateFeedbackDomain.Reward, DataCatalog.CrystalShardId)]
    public void DomainSuccessUsesStableDefaultIcon(
        ImmediateFeedbackDomain domain,
        string expectedIconItemId
    )
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            domain,
            ActionResult.Success(messageKey: "notice.saved")
        );

        Assert.Equal(ImmediateFeedbackOutcome.Success, cue.Outcome);
        Assert.Equal(expectedIconItemId, cue.IconItemId);
        Assert.Equal("notice.saved", cue.MessageKey);
        Assert.False(cue.ReducedEffects);
    }

    [Theory]
    [InlineData(ImmediateFeedbackDomain.Damage)]
    [InlineData(ImmediateFeedbackDomain.Dodge)]
    public void CombatDomainsCanUseAtlasOnlyIcons(ImmediateFeedbackDomain domain)
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            domain,
            ActionResult.Success(messageKey: "combat.attack.ready")
        );

        Assert.Equal(ImmediateFeedbackOutcome.Success, cue.Outcome);
        Assert.Null(cue.IconItemId);
    }

    [Fact]
    public void GrantedItemOverridesDefaultIconForHarvestPickupAndRewards()
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Harvest,
            ActionResult.Grant(
                DataCatalog.MoonrootId,
                1,
                0,
                "target.action.harvest"
            )
        );

        Assert.Equal(ImmediateFeedbackOutcome.Success, cue.Outcome);
        Assert.Equal(DataCatalog.MoonrootId, cue.IconItemId);
    }

    [Fact]
    public void TargetPreviewNeedsToolBecomesToolMismatch()
    {
        var preview = TargetPreview.NeedsTool(
            new GridPosition(3, 4),
            TargetPreviewKind.Soil,
            "notice.needs_shovel"
        );

        var cue = ImmediateFeedbackPresenter.FromTargetPreview(
            ImmediateFeedbackDomain.Tool,
            preview
        );

        Assert.Equal(ImmediateFeedbackOutcome.ToolMismatch, cue.Outcome);
        Assert.Equal("notice.needs_shovel", cue.MessageKey);
    }

    [Theory]
    [InlineData("notice.no_energy")]
    [InlineData("notice.needs_water")]
    [InlineData("notice.inventory_full")]
    [InlineData("crafting.missing_ingredients")]
    [InlineData("fishing.crab_pot.needs_bait")]
    public void ResourceAndCapacityFailuresBecomeResourceBlocked(
        string messageKey
    )
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Watering,
            ActionResult.Fail(messageKey)
        );

        Assert.Equal(ImmediateFeedbackOutcome.ResourceBlocked, cue.Outcome);
        Assert.True(cue.ShakePixels > 0);
    }

    [Theory]
    [InlineData("animal.automation.feed_capacity")]
    [InlineData("animal.automation.no_feed_stored")]
    [InlineData("animal.feed.insufficient_fodder")]
    [InlineData("collection.donation.missing_item")]
    [InlineData("collection.reward.inventory_full")]
    [InlineData("cooking.backpack_full")]
    [InlineData("crafting.backpack_full")]
    [InlineData("festival.shop.backpack_full")]
    [InlineData("fishing.gear.bait_missing")]
    [InlineData("fishing.gear.materials_missing")]
    [InlineData("kitchen.pantry.full")]
    [InlineData("kitchen.pantry.none_stored")]
    [InlineData("mail.notice.backpack_full")]
    [InlineData("notice.no_chest_item")]
    [InlineData("notice.no_seed")]
    [InlineData("notice.water_full")]
    [InlineData("notice.watering_can_empty")]
    [InlineData("shop.not_enough_coins")]
    [InlineData("storage.chest_full")]
    [InlineData("storage.none_in_chest")]
    [InlineData("tool.upgrade.insufficient_materials")]
    [InlineData("village.gift.missing_item")]
    public void ActualResourceAndCapacityKeysUseResourceBlockedSound(
        string messageKey
    )
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Tool,
            ActionResult.Fail(messageKey)
        );

        Assert.Equal(ImmediateFeedbackOutcome.ResourceBlocked, cue.Outcome);
        Assert.Equal(PixelSound.ResourceBlocked, ImmediateFeedbackAudio.SoundFor(cue));
    }

    [Theory]
    [InlineData("notice.needs_hand")]
    [InlineData("deep_mine.shovel_tier_low")]
    [InlineData("combat.requires_weapon")]
    public void ToolKeysBecomeToolMismatch(string messageKey)
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Tool,
            ActionResult.Fail(messageKey)
        );

        Assert.Equal(ImmediateFeedbackOutcome.ToolMismatch, cue.Outcome);
    }

    [Theory]
    [InlineData("mining.requires_bronze_star_shovel")]
    [InlineData("target.need.bucket")]
    [InlineData("target.need.bucket_or_rod")]
    [InlineData("target.need.hand")]
    [InlineData("target.need.machete")]
    [InlineData("target.need.seed")]
    [InlineData("target.need.shovel_mine")]
    [InlineData("target.need.shovel_till")]
    [InlineData("target.need.watering_can")]
    [InlineData("target.need.weapon")]
    public void ActualTargetPreviewToolKeysUseToolMismatchSound(
        string messageKey
    )
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Tool,
            ActionResult.Fail(messageKey)
        );

        Assert.Equal(ImmediateFeedbackOutcome.ToolMismatch, cue.Outcome);
        Assert.Equal(PixelSound.ToolMismatch, ImmediateFeedbackAudio.SoundFor(cue));
    }

    [Fact]
    public void GenericFailureStaysFailure()
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Pickup,
            ActionResult.Fail("notice.nothing_to_interact")
        );

        Assert.Equal(ImmediateFeedbackOutcome.Failure, cue.Outcome);
    }

    [Theory]
    [InlineData("combat.attack.cooldown")]
    [InlineData("collection.reward.already_claimed")]
    [InlineData("fishing.crab_pot.occupied")]
    [InlineData("fishing.gear.level_locked")]
    [InlineData("mail.notice.already_claimed")]
    [InlineData("notice.not_water_source")]
    [InlineData("notice.nothing_to_interact")]
    [InlineData("processor.busy")]
    [InlineData("weekly_commission.not_ready")]
    public void RepresentativeOrdinaryFailuresStayFailureSound(
        string messageKey
    )
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Tool,
            ActionResult.Fail(messageKey)
        );

        Assert.Equal(ImmediateFeedbackOutcome.Failure, cue.Outcome);
        Assert.Equal(PixelSound.Error, ImmediateFeedbackAudio.SoundFor(cue));
    }

    [Theory]
    [InlineData("unknown.missing_supply")]
    [InlineData("unknown.insufficient_signal")]
    [InlineData("unknown.needs_tool_that_does_not_exist")]
    public void UnknownFailureKeysStayGenericFailure(string messageKey)
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Tool,
            ActionResult.Fail(messageKey)
        );

        Assert.Equal(ImmediateFeedbackOutcome.Failure, cue.Outcome);
        Assert.Equal(PixelSound.Error, ImmediateFeedbackAudio.SoundFor(cue));
    }

    [Fact]
    public void ReducedEffectsRemovePulseAndShake()
    {
        var settings = new AccessibilitySettings { ScreenShakePercent = 0 };

        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Damage,
            ActionResult.Success(messageKey: "combat.enemy_move.moved"),
            settings
        );

        Assert.True(cue.ReducedEffects);
        Assert.Equal(0f, cue.ShakePixels);
        Assert.Equal(1f, cue.PulseScale);
    }

    [Fact]
    public void ScreenShakePercentScalesDamageFeedback()
    {
        var settings = new AccessibilitySettings { ScreenShakePercent = 50 };

        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Damage,
            ActionResult.Success(messageKey: "combat.enemy_move.moved"),
            settings
        );

        Assert.Equal(2.5f, cue.ShakePixels);
        Assert.False(cue.ReducedEffects);
    }

    [Fact]
    public void HighContrastTargetCuesIncreaseBorderWeight()
    {
        var settings = new AccessibilitySettings
        {
            TargetCues = TargetCueMode.HighContrast
        };

        var cue = ImmediateFeedbackPresenter.FromActionResult(
            ImmediateFeedbackDomain.Reward,
            ActionResult.Success(messageKey: "collection.reward.claimed"),
            settings
        );

        Assert.Equal(2, cue.BorderWidth);
    }

    [Fact]
    public void DemoFlagIsExplicit()
    {
        Assert.False(ImmediateFeedbackStartup.ShouldShowDemo([]));
        Assert.True(
            ImmediateFeedbackStartup.ShouldShowDemo(
                [ImmediateFeedbackStartup.DemoFlag]
            )
        );
    }

    private static ActionResult ResultFor(
        ImmediateFeedbackOutcome outcome,
        string messageKey
    )
    {
        if (outcome == ImmediateFeedbackOutcome.Success)
        {
            return ActionResult.Success(messageKey: messageKey);
        }

        return ActionResult.Fail(messageKey);
    }

    private static string SuccessKeyFor(ImmediateFeedbackDomain domain) =>
        domain switch
        {
            ImmediateFeedbackDomain.Tool => "target.action.use",
            ImmediateFeedbackDomain.Watering => "target.action.water",
            ImmediateFeedbackDomain.Harvest => "target.action.harvest",
            ImmediateFeedbackDomain.Pickup => "target.action.pickup",
            ImmediateFeedbackDomain.Processing => "processor.started",
            ImmediateFeedbackDomain.Fishing => "fishing.catch.success",
            ImmediateFeedbackDomain.Damage => "combat.enemy_move.moved",
            ImmediateFeedbackDomain.Dodge => "combat.dodge.ready",
            ImmediateFeedbackDomain.Reward => "collection.reward.claimed",
            _ => "notice.saved"
        };

    private static string ResourceBlockedKeyFor(ImmediateFeedbackDomain domain) =>
        domain switch
        {
            ImmediateFeedbackDomain.Watering => "notice.needs_water",
            ImmediateFeedbackDomain.Harvest => "notice.inventory_full",
            ImmediateFeedbackDomain.Pickup => "notice.backpack_full",
            ImmediateFeedbackDomain.Processing => "crafting.missing_ingredients",
            ImmediateFeedbackDomain.Fishing => "fishing.crab_pot.needs_bait",
            ImmediateFeedbackDomain.Reward => "collection.reward.inventory_full",
            _ => "notice.no_energy"
        };

    private static string ToolMismatchKeyFor(ImmediateFeedbackDomain domain) =>
        domain switch
        {
            ImmediateFeedbackDomain.Tool => "notice.needs_shovel",
            ImmediateFeedbackDomain.Watering => "notice.needs_watering_can",
            ImmediateFeedbackDomain.Fishing => "notice.needs_fishing_rod",
            _ => "notice.needs_hand"
        };

    private static string FailureKeyFor(ImmediateFeedbackDomain domain) =>
        domain switch
        {
            ImmediateFeedbackDomain.Processing => "processor.busy",
            ImmediateFeedbackDomain.Fishing => "fishing.catch.missed",
            ImmediateFeedbackDomain.Damage => "combat.enemy_move.blocked",
            ImmediateFeedbackDomain.Dodge => "combat.dodge.cooldown",
            ImmediateFeedbackDomain.Reward => "collection.reward.already_claimed",
            _ => "notice.nothing_to_interact"
        };

    private static string? ExpectedDefaultIcon(ImmediateFeedbackDomain domain) =>
        domain switch
        {
            ImmediateFeedbackDomain.Tool => DataCatalog.HandId,
            ImmediateFeedbackDomain.Watering => DataCatalog.WateringCanId,
            ImmediateFeedbackDomain.Harvest => DataCatalog.StarbudId,
            ImmediateFeedbackDomain.Pickup => DataCatalog.LumenwoodId,
            ImmediateFeedbackDomain.Processing => DataCatalog.StarbudPreserveId,
            ImmediateFeedbackDomain.Fishing => DataCatalog.FishingRodId,
            ImmediateFeedbackDomain.Reward => DataCatalog.CrystalShardId,
            _ => null
        };

    private static PixelSound? ExpectedSoundFor(
        ImmediateFeedbackDomain domain,
        ImmediateFeedbackOutcome outcome
    ) =>
        outcome switch
        {
            ImmediateFeedbackOutcome.Failure => PixelSound.Error,
            ImmediateFeedbackOutcome.ResourceBlocked =>
                PixelSound.ResourceBlocked,
            ImmediateFeedbackOutcome.ToolMismatch => PixelSound.ToolMismatch,
            ImmediateFeedbackOutcome.Success
                when domain == ImmediateFeedbackDomain.Damage =>
                    PixelSound.Damage,
            ImmediateFeedbackOutcome.Success
                when domain == ImmediateFeedbackDomain.Dodge =>
                    PixelSound.Dodge,
            _ => null
        };
}
