using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public enum ImmediateFeedbackDomain
{
    Tool,
    Watering,
    Harvest,
    Pickup,
    Processing,
    Damage,
    Dodge,
    Fishing,
    Reward
}

public enum ImmediateFeedbackOutcome
{
    Success,
    Failure,
    ResourceBlocked,
    ToolMismatch
}

public sealed record ImmediateFeedbackCue(
    ImmediateFeedbackDomain Domain,
    ImmediateFeedbackOutcome Outcome,
    string MessageKey,
    string? IconItemId,
    Color AccentColor,
    float DurationSeconds,
    float PulseScale,
    float ShakePixels,
    int BorderWidth,
    bool ReducedEffects
)
{
    public bool HasMessage => !string.IsNullOrWhiteSpace(MessageKey);
}

public static class ImmediateFeedbackPresenter
{
    private static readonly HashSet<string> ToolMismatchKeys = new(
        [
            "notice.needs_hand",
            "notice.needs_shovel",
            "notice.needs_machete",
            "notice.needs_bucket",
            "notice.needs_watering_can",
            "notice.needs_fishing_rod",
            "deep_mine.shovel_tier_low",
            "mining.requires_bronze_star_shovel",
            "combat.requires_weapon",
            "target.need.bucket",
            "target.need.bucket_or_rod",
            "target.need.hand",
            "target.need.machete",
            "target.need.seed",
            "target.need.shovel_mine",
            "target.need.shovel_till",
            "target.need.watering_can",
            "target.need.weapon"
        ],
        StringComparer.Ordinal
    );

    private static readonly HashSet<string> ResourceBlockedKeys = new(
        [
            "animal.automation.feed_capacity",
            "animal.automation.no_feed_stored",
            "animal.feed.insufficient_fodder",
            "collection.donation.missing_item",
            "collection.reward.inventory_full",
            "cooking.backpack_full",
            "crafting.backpack_full",
            "crafting.missing_ingredients",
            "festival.shop.backpack_full",
            "fishing.crab_pot.limit",
            "fishing.crab_pot.needs_bait",
            "fishing.gear.bait_missing",
            "fishing.gear.materials_missing",
            "kitchen.pantry.backpack_full",
            "kitchen.pantry.full",
            "kitchen.pantry.none_in_backpack",
            "kitchen.pantry.none_stored",
            "mail.notice.backpack_full",
            "notice.backpack_full",
            "notice.inventory_full",
            "notice.needs_water",
            "notice.no_chest_item",
            "notice.no_energy",
            "notice.no_placeable_item",
            "notice.no_sapling",
            "notice.no_seed",
            "notice.water_full",
            "notice.watering_can_empty",
            "processor.missing_ingredients",
            "shop.not_enough_coins",
            "storage.chest_full",
            "storage.none_in_backpack",
            "storage.none_in_chest",
            "tool.upgrade.insufficient_coins",
            "tool.upgrade.insufficient_materials",
            "village.gift.missing_item"
        ],
        StringComparer.Ordinal
    );

    public static ImmediateFeedbackCue FromActionResult(
        ImmediateFeedbackDomain domain,
        ActionResult result,
        AccessibilitySettings? settings = null,
        TargetPreview? preview = null
    )
    {
        var outcome = Classify(result, preview);
        var messageKey = !string.IsNullOrWhiteSpace(result.MessageKey)
            ? result.MessageKey
            : preview?.LabelKey ?? string.Empty;
        var iconItemId = !string.IsNullOrWhiteSpace(result.GrantedItemId)
            ? result.GrantedItemId
            : DefaultIconItemId(domain);
        return CreateCue(domain, outcome, messageKey, iconItemId, settings);
    }

    public static ImmediateFeedbackCue FromTargetPreview(
        ImmediateFeedbackDomain domain,
        TargetPreview preview,
        AccessibilitySettings? settings = null
    )
    {
        var outcome = preview.State switch
        {
            TargetPreviewState.Available => ImmediateFeedbackOutcome.Success,
            TargetPreviewState.NeedsTool => ImmediateFeedbackOutcome.ToolMismatch,
            TargetPreviewState.Blocked => ImmediateFeedbackOutcome.ResourceBlocked,
            _ => ImmediateFeedbackOutcome.Failure
        };
        return CreateCue(
            domain,
            outcome,
            preview.LabelKey,
            DefaultIconItemId(domain),
            settings
        );
    }

    public static ImmediateFeedbackOutcome Classify(
        ActionResult result,
        TargetPreview? preview = null
    )
    {
        if (result.Succeeded)
        {
            return ImmediateFeedbackOutcome.Success;
        }

        if (preview?.State == TargetPreviewState.NeedsTool)
        {
            return ImmediateFeedbackOutcome.ToolMismatch;
        }

        if (IsToolMismatchKey(result.MessageKey))
        {
            return ImmediateFeedbackOutcome.ToolMismatch;
        }

        if (IsResourceBlockedKey(result.MessageKey))
        {
            return ImmediateFeedbackOutcome.ResourceBlocked;
        }

        if (preview?.State == TargetPreviewState.Blocked &&
            string.IsNullOrWhiteSpace(result.MessageKey))
        {
            return ImmediateFeedbackOutcome.ResourceBlocked;
        }

        return ImmediateFeedbackOutcome.Failure;
    }

    private static ImmediateFeedbackCue CreateCue(
        ImmediateFeedbackDomain domain,
        ImmediateFeedbackOutcome outcome,
        string messageKey,
        string? iconItemId,
        AccessibilitySettings? settings
    )
    {
        var screenShake = NormalizedScreenShakePercent(settings);
        var reduced = screenShake == 0;
        return new ImmediateFeedbackCue(
            domain,
            outcome,
            messageKey,
            iconItemId,
            AccentFor(outcome, settings),
            DurationFor(outcome, reduced),
            reduced ? 1f : PulseFor(outcome),
            ShakeFor(domain, outcome, screenShake),
            BorderWidthFor(settings),
            reduced
        );
    }

    private static bool IsToolMismatchKey(string messageKey)
    {
        if (string.IsNullOrWhiteSpace(messageKey))
        {
            return false;
        }

        return ToolMismatchKeys.Contains(messageKey);
    }

    private static bool IsResourceBlockedKey(string messageKey)
    {
        if (string.IsNullOrWhiteSpace(messageKey))
        {
            return false;
        }

        return ResourceBlockedKeys.Contains(messageKey);
    }

    private static string? DefaultIconItemId(ImmediateFeedbackDomain domain) =>
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

    private static int NormalizedScreenShakePercent(AccessibilitySettings? settings)
    {
        var value = settings?.ScreenShakePercent ?? 100;
        return value is 0 or 50 or 100 ? value : 100;
    }

    private static int BorderWidthFor(AccessibilitySettings? settings) =>
        settings?.TargetCues == TargetCueMode.HighContrast ? 2 : 1;

    private static Color AccentFor(
        ImmediateFeedbackOutcome outcome,
        AccessibilitySettings? settings
    )
    {
        if (settings?.TargetCues == TargetCueMode.Deuteranopia)
        {
            return outcome switch
            {
                ImmediateFeedbackOutcome.Success => new Color("#79dff0"),
                ImmediateFeedbackOutcome.ToolMismatch => ThemeFactory.Gold,
                ImmediateFeedbackOutcome.ResourceBlocked => new Color("#d998ff"),
                _ => ThemeFactory.Violet
            };
        }

        return outcome switch
        {
            ImmediateFeedbackOutcome.Success => ThemeFactory.Mint,
            ImmediateFeedbackOutcome.ToolMismatch => ThemeFactory.Gold,
            ImmediateFeedbackOutcome.ResourceBlocked => new Color("#ff5f9a"),
            _ => ThemeFactory.Violet
        };
    }

    private static float DurationFor(ImmediateFeedbackOutcome outcome, bool reduced)
    {
        if (reduced)
        {
            return 1.1f;
        }

        return outcome switch
        {
            ImmediateFeedbackOutcome.Success => 1.15f,
            ImmediateFeedbackOutcome.ResourceBlocked => 1.45f,
            _ => 1.3f
        };
    }

    private static float PulseFor(ImmediateFeedbackOutcome outcome) =>
        outcome switch
        {
            ImmediateFeedbackOutcome.Success => 1.12f,
            ImmediateFeedbackOutcome.ResourceBlocked => 1.06f,
            ImmediateFeedbackOutcome.ToolMismatch => 1.07f,
            _ => 1.04f
        };

    private static float ShakeFor(
        ImmediateFeedbackDomain domain,
        ImmediateFeedbackOutcome outcome,
        int screenShakePercent
    )
    {
        if (screenShakePercent == 0)
        {
            return 0f;
        }

        var multiplier = screenShakePercent / 100f;
        if (domain == ImmediateFeedbackDomain.Damage)
        {
            return 5f * multiplier;
        }

        return outcome switch
        {
            ImmediateFeedbackOutcome.ResourceBlocked => 2.5f * multiplier,
            ImmediateFeedbackOutcome.ToolMismatch => 1.8f * multiplier,
            ImmediateFeedbackOutcome.Failure => 1.4f * multiplier,
            _ => 0f
        };
    }
}

public static class ImmediateFeedbackAudio
{
    public static PixelSound? SoundFor(ImmediateFeedbackCue cue) =>
        cue.Outcome switch
        {
            ImmediateFeedbackOutcome.Failure => PixelSound.Error,
            ImmediateFeedbackOutcome.ResourceBlocked =>
                PixelSound.ResourceBlocked,
            ImmediateFeedbackOutcome.ToolMismatch => PixelSound.ToolMismatch,
            ImmediateFeedbackOutcome.Success
                when cue.Domain == ImmediateFeedbackDomain.Damage =>
                    PixelSound.Damage,
            ImmediateFeedbackOutcome.Success
                when cue.Domain == ImmediateFeedbackDomain.Dodge =>
                    PixelSound.Dodge,
            _ => null
        };
}

public static class ImmediateFeedbackStartup
{
    public const string DemoFlag = "--ui-feedback-demo";

    public static bool ShouldShowDemo(IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            if (argument == DemoFlag)
            {
                return true;
            }
        }

        return false;
    }
}

public sealed partial class ImmediateFeedbackOverlay : PanelContainer
{
    private readonly LocaleService _locale;
    private readonly TextureRect _icon;
    private readonly Label _message;
    private ImmediateFeedbackCue? _cue;
    private Vector2 _restPosition;
    private double _elapsed;
    private double _remaining;

    public ImmediateFeedbackOverlay(LocaleService locale)
    {
        _locale = locale;
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        CustomMinimumSize = new Vector2(210, 34);
        AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(ThemeFactory.Panel, ThemeFactory.Mint, 1, 6, 5)
        );

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        AddChild(row);

        _icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(24, 24),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddChild(_icon);

        _message = ThemeFactory.Label(size: 9, color: ThemeFactory.Ink);
        _message.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _message.VerticalAlignment = VerticalAlignment.Center;
        _message.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _message.MaxLinesVisible = 2;
        row.AddChild(_message);
    }

    public void Show(ImmediateFeedbackCue cue)
    {
        _cue = cue;
        _elapsed = 0;
        _remaining = cue.DurationSeconds;
        _restPosition = Position;
        PivotOffset = Size / 2;
        Visible = true;
        Modulate = Colors.White;
        Scale = Vector2.One;
        AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#101a31f2"),
                cue.AccentColor,
                cue.BorderWidth,
                6,
                5
            )
        );
        _icon.Texture = IconTexture(cue);
        _message.Text = cue.HasMessage ? _locale.Tr(cue.MessageKey) : string.Empty;
        _message.Visible = cue.HasMessage;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_cue is null || _remaining <= 0)
        {
            return;
        }

        _elapsed += delta;
        _remaining -= delta;
        if (_remaining <= 0)
        {
            Visible = false;
            _cue = null;
            Scale = Vector2.One;
            Position = _restPosition;
            return;
        }

        var cue = _cue;
        var progress = Math.Clamp(
            _remaining / Math.Max(0.01, cue.DurationSeconds),
            0,
            1
        );
        Modulate = new Color(1, 1, 1, (float)Math.Min(1, progress + 0.2));
        if (cue.ReducedEffects)
        {
            Scale = Vector2.One;
            Position = _restPosition;
            return;
        }

        var wave = Mathf.Sin((float)_elapsed * 13f) * 0.5f + 0.5f;
        var scale = 1f + (cue.PulseScale - 1f) * wave * (float)progress;
        Scale = Vector2.One * scale;
        Position = _restPosition + new Vector2(
            Mathf.Sin((float)_elapsed * 37f) * cue.ShakePixels * (float)progress,
            0
        );
    }

    private static Texture2D IconTexture(ImmediateFeedbackCue cue)
    {
        if (!string.IsNullOrWhiteSpace(cue.IconItemId))
        {
            return ItemTexture(cue.IconItemId);
        }

        return cue.Domain switch
        {
            ImmediateFeedbackDomain.Damage => Atlas(
                StarfallRuinsArt.CombatAtlas,
                StarfallRuinsArt.HealthCoreRegion
            ),
            ImmediateFeedbackDomain.Dodge => Atlas(
                StarfallRuinsArt.CombatAtlas,
                StarfallRuinsArt.DodgeSparkRegion
            ),
            ImmediateFeedbackDomain.Fishing => FishingGearArt.HookedFishIcon(),
            ImmediateFeedbackDomain.Processing => GeneratedArt.CreateCraftingIcon(),
            ImmediateFeedbackDomain.Reward => GeneratedArt.CreateCommissionRewardIcon(),
            _ => GeneratedArt.CreateCommissionParchmentIcon()
        };
    }

    private static Texture2D ItemTexture(string itemId)
    {
        if (HotbarSlotContent.TryGetIconRegion(
                itemId,
                out var texture,
                out var region
            ))
        {
            return Atlas(texture, region);
        }

        return GeneratedArt.CreateCommissionRewardIcon();
    }

    private static AtlasTexture Atlas(Texture2D texture, Rect2 region) => new()
    {
        Atlas = texture,
        Region = region,
        FilterClip = true
    };
}
