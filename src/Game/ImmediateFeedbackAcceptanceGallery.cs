using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public enum ImmediateFeedbackAcceptanceMotionMode
{
    Standard,
    Reduced
}

public sealed record ImmediateFeedbackAcceptanceScenario(
    string SceneId,
    ImmediateFeedbackDomain Domain,
    string DomainId,
    ImmediateFeedbackOutcome ExpectedOutcome,
    string OutcomeId,
    ImmediateFeedbackAcceptanceMotionMode Motion,
    string MotionId,
    string MessageKey,
    TargetPreviewKind PreviewKind,
    bool UsesToolMismatchPreview,
    string? GrantedItemId = null,
    int GrantedItemCount = 0
)
{
    public bool ExpectsReducedEffects =>
        Motion == ImmediateFeedbackAcceptanceMotionMode.Reduced;
}

public sealed record ImmediateFeedbackAcceptanceTile(
    ImmediateFeedbackAcceptanceScenario Scenario,
    ImmediateFeedbackCue Cue
);

public static class ImmediateFeedbackAcceptanceGallery
{
    public const string OpenFlag = "--ui-feedback-gallery";
    public const string DomainFlagPrefix = "--ui-feedback-gallery-domain=";
    public const string SceneIdPrefix = "feel.acceptance";
    public const int DomainCount = 9;
    public const int OutcomeCount = 4;
    public const int MotionModeCount = 2;
    public const int ExpectedScenarioCount =
        DomainCount * OutcomeCount * MotionModeCount;

    private static readonly IReadOnlyList<FeedbackDomainSpec> DomainSpecs =
    [
        new(
            ImmediateFeedbackDomain.Tool,
            "tool",
            TargetPreviewKind.Ground,
            "target.action.till",
            "notice.no_energy",
            "notice.needs_shovel",
            "notice.nothing_to_interact"
        ),
        new(
            ImmediateFeedbackDomain.Watering,
            "watering",
            TargetPreviewKind.Water,
            "target.action.water",
            "notice.needs_water",
            "target.need.bucket_or_rod",
            "notice.not_tillable"
        ),
        new(
            ImmediateFeedbackDomain.Harvest,
            "harvest",
            TargetPreviewKind.Crop,
            "target.action.harvest",
            "notice.inventory_full",
            "notice.needs_hand",
            "notice.not_ready",
            DataCatalog.StarbudId,
            1
        ),
        new(
            ImmediateFeedbackDomain.Pickup,
            "pickup",
            TargetPreviewKind.Forage,
            "notice.forage_collected",
            "notice.inventory_full",
            "notice.needs_hand",
            "notice.resource_depleted",
            DataCatalog.LumenwoodId,
            1
        ),
        new(
            ImmediateFeedbackDomain.Processing,
            "processor",
            TargetPreviewKind.Station,
            "processor.started",
            "crafting.missing_ingredients",
            "notice.needs_hand",
            "processor.busy"
        ),
        new(
            ImmediateFeedbackDomain.Fishing,
            "fishing",
            TargetPreviewKind.Water,
            "notice.fish_caught",
            "fishing.crab_pot.needs_bait",
            "target.need.bucket_or_rod",
            "fishing.minigame.failed"
        ),
        new(
            ImmediateFeedbackDomain.Damage,
            "damage",
            TargetPreviewKind.RuinsEnemy,
            "combat.player_hit",
            "notice.no_energy",
            "combat.requires_weapon",
            "combat.player_invulnerable"
        ),
        new(
            ImmediateFeedbackDomain.Dodge,
            "dodge",
            TargetPreviewKind.RuinsEnemy,
            "combat.dodge.started",
            "notice.no_energy",
            "combat.requires_weapon",
            "combat.dodge.cooldown"
        ),
        new(
            ImmediateFeedbackDomain.Reward,
            "reward",
            TargetPreviewKind.CommissionBoard,
            "feedback.reward.claimed",
            "notice.inventory_full",
            "notice.needs_hand",
            "collection.reward.already_claimed",
            DataCatalog.CrystalShardId,
            1
        )
    ];

    private static readonly IReadOnlyList<FeedbackOutcomeSpec> OutcomeSpecs =
    [
        new(ImmediateFeedbackOutcome.Success, "success"),
        new(ImmediateFeedbackOutcome.ResourceBlocked, "resource-blocked"),
        new(ImmediateFeedbackOutcome.ToolMismatch, "tool-mismatch"),
        new(ImmediateFeedbackOutcome.Failure, "failure")
    ];

    private static readonly IReadOnlyList<ImmediateFeedbackAcceptanceMotionMode>
        MotionModes =
        [
            ImmediateFeedbackAcceptanceMotionMode.Standard,
            ImmediateFeedbackAcceptanceMotionMode.Reduced
        ];

    public static IReadOnlyList<ImmediateFeedbackAcceptanceScenario> Scenarios { get; } =
        CreateScenarios();

    public static IReadOnlyList<string> RequiredLocalizationKeys { get; } =
        Scenarios
            .Select(scenario => scenario.MessageKey)
            .Concat([
                "feedback.gallery.title",
                "feedback.gallery.subtitle",
                "feedback.gallery.all_domains",
                "menu.back"
            ])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<string> DomainIds { get; } = DomainSpecs
        .Select(spec => spec.Id)
        .ToArray();

    public static bool ShouldOpen(IEnumerable<string> arguments) =>
        arguments.Contains(OpenFlag, StringComparer.Ordinal);

    public static string? SelectedDomain(IEnumerable<string> arguments)
    {
        var argument = arguments.FirstOrDefault(value =>
            value.StartsWith(DomainFlagPrefix, StringComparison.Ordinal)
        );
        if (string.IsNullOrWhiteSpace(argument))
        {
            return null;
        }

        var domainId = argument[DomainFlagPrefix.Length..];
        return DomainIds.Contains(domainId, StringComparer.Ordinal)
            ? domainId
            : null;
    }

    public static IReadOnlyList<ImmediateFeedbackAcceptanceTile> BuildTiles(
        AccessibilitySettings? visualSettings = null,
        string? domainId = null
    ) => FilterScenarios(domainId)
        .Select(scenario => new ImmediateFeedbackAcceptanceTile(
            scenario,
            CreateCue(scenario, visualSettings)
        ))
        .ToArray();

    private static IEnumerable<ImmediateFeedbackAcceptanceScenario>
        FilterScenarios(string? domainId)
    {
        if (string.IsNullOrWhiteSpace(domainId) ||
            !DomainIds.Contains(domainId, StringComparer.Ordinal))
        {
            return Scenarios;
        }

        return Scenarios.Where(scenario =>
            scenario.DomainId == domainId
        );
    }

    public static ImmediateFeedbackCue CreateCue(
        ImmediateFeedbackAcceptanceScenario scenario,
        AccessibilitySettings? visualSettings = null
    ) =>
        ImmediateFeedbackPresenter.FromActionResult(
            scenario.Domain,
            ResultFor(scenario),
            SettingsFor(scenario, visualSettings),
            PreviewFor(scenario)
        );

    private static IReadOnlyList<ImmediateFeedbackAcceptanceScenario>
        CreateScenarios()
    {
        var scenarios = new List<ImmediateFeedbackAcceptanceScenario>(
            ExpectedScenarioCount
        );

        foreach (var domain in DomainSpecs)
        {
            foreach (var outcome in OutcomeSpecs)
            {
                foreach (var motion in MotionModes)
                {
                    scenarios.Add(CreateScenario(domain, outcome, motion));
                }
            }
        }

        return scenarios;
    }

    private static ImmediateFeedbackAcceptanceScenario CreateScenario(
        FeedbackDomainSpec domain,
        FeedbackOutcomeSpec outcome,
        ImmediateFeedbackAcceptanceMotionMode motion
    )
    {
        var motionId = MotionIdFor(motion);
        var messageKey = MessageKeyFor(domain, outcome.Outcome);
        var grantedItemId = GrantedItemIdFor(domain, outcome.Outcome);
        var grantedItemCount = GrantedItemCountFor(domain, outcome.Outcome);
        return new ImmediateFeedbackAcceptanceScenario(
            $"{SceneIdPrefix}.{domain.Id}.{outcome.Id}.{motionId}",
            domain.Domain,
            domain.Id,
            outcome.Outcome,
            outcome.Id,
            motion,
            motionId,
            messageKey,
            domain.PreviewKind,
            outcome.Outcome == ImmediateFeedbackOutcome.ToolMismatch,
            grantedItemId,
            grantedItemCount
        );
    }

    private static string MessageKeyFor(
        FeedbackDomainSpec domain,
        ImmediateFeedbackOutcome outcome
    )
    {
        return outcome switch
        {
            ImmediateFeedbackOutcome.Success => domain.SuccessKey,
            ImmediateFeedbackOutcome.ResourceBlocked => domain.ResourceBlockedKey,
            ImmediateFeedbackOutcome.ToolMismatch => domain.ToolMismatchKey,
            _ => domain.FailureKey
        };
    }

    private static string? GrantedItemIdFor(
        FeedbackDomainSpec domain,
        ImmediateFeedbackOutcome outcome
    )
    {
        if (outcome != ImmediateFeedbackOutcome.Success)
        {
            return null;
        }

        return domain.SuccessGrantedItemId;
    }

    private static int GrantedItemCountFor(
        FeedbackDomainSpec domain,
        ImmediateFeedbackOutcome outcome
    )
    {
        if (outcome != ImmediateFeedbackOutcome.Success)
        {
            return 0;
        }

        return domain.SuccessGrantedItemCount;
    }

    private static string MotionIdFor(
        ImmediateFeedbackAcceptanceMotionMode motion
    ) => motion switch
    {
        ImmediateFeedbackAcceptanceMotionMode.Reduced => "reduced",
        _ => "standard"
    };

    private static ActionResult ResultFor(
        ImmediateFeedbackAcceptanceScenario scenario
    )
    {
        if (scenario.ExpectedOutcome != ImmediateFeedbackOutcome.Success)
        {
            return ActionResult.Fail(scenario.MessageKey);
        }

        if (!string.IsNullOrWhiteSpace(scenario.GrantedItemId))
        {
            return ActionResult.Grant(
                scenario.GrantedItemId,
                Math.Max(1, scenario.GrantedItemCount),
                0,
                scenario.MessageKey
            );
        }

        return ActionResult.Success(messageKey: scenario.MessageKey);
    }

    private static AccessibilitySettings SettingsFor(
        ImmediateFeedbackAcceptanceScenario scenario,
        AccessibilitySettings? visualSettings
    )
    {
        var screenShakePercent = 100;
        if (scenario.ExpectsReducedEffects)
        {
            screenShakePercent = 0;
        }

        return new AccessibilitySettings
        {
            ScreenShakePercent = screenShakePercent,
            TargetCues = visualSettings?.TargetCues ?? TargetCueMode.Standard,
            FontScalePercent = visualSettings?.FontScalePercent ?? 100
        };
    }

    private static TargetPreview? PreviewFor(
        ImmediateFeedbackAcceptanceScenario scenario
    )
    {
        if (!scenario.UsesToolMismatchPreview)
        {
            return null;
        }

        return TargetPreview.NeedsTool(
            new GridPosition(0, 0),
            scenario.PreviewKind,
            scenario.MessageKey
        );
    }

    private sealed record FeedbackDomainSpec(
        ImmediateFeedbackDomain Domain,
        string Id,
        TargetPreviewKind PreviewKind,
        string SuccessKey,
        string ResourceBlockedKey,
        string ToolMismatchKey,
        string FailureKey,
        string? SuccessGrantedItemId = null,
        int SuccessGrantedItemCount = 0
    );

    private sealed record FeedbackOutcomeSpec(
        ImmediateFeedbackOutcome Outcome,
        string Id
    );
}

public sealed partial class ImmediateFeedbackAcceptanceGalleryOverlay :
    FullScreenUi
{
    private readonly LocaleService _locale;
    private readonly AccessibilitySettings? _visualSettings;
    private readonly string? _domainId;
    private readonly GridContainer _grid;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Button _close;

    public ImmediateFeedbackAcceptanceGalleryOverlay(
        Theme theme,
        LocaleService locale,
        AccessibilitySettings? visualSettings = null,
        string? domainId = null
    ) : base(theme)
    {
        _locale = locale;
        _visualSettings = visualSettings;
        _domainId = domainId;

        AddChild(Dim(new Color(0.008f, 0.015f, 0.06f, 0.9f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(596, 334)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0c1836fc"),
                ThemeFactory.Mint,
                2,
                10
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);

        _title = ThemeFactory.Label(
            "",
            18,
            ThemeFactory.Gold
        );
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);

        _subtitle = ThemeFactory.Label(
            "",
            9,
            ThemeFactory.MutedInk
        );
        _subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_subtitle);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(560, 242),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        _grid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _grid.AddThemeConstantOverride("h_separation", 5);
        _grid.AddThemeConstantOverride("v_separation", 5);
        scroll.AddChild(_grid);
        column.AddChild(scroll);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(124, 24);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        locale.LocaleChanged += Refresh;
        Refresh();
    }

    public event Action? CloseRequested;

    public override void _ExitTree()
    {
        _locale.LocaleChanged -= Refresh;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed(InputSetup.UiCancel))
        {
            return;
        }

        CloseRequested?.Invoke();
        GetViewport().SetInputAsHandled();
    }

    public void Refresh()
    {
        var domainLabel = string.IsNullOrWhiteSpace(_domainId)
            ? _locale.Tr("feedback.gallery.all_domains")
            : _domainId;
        _title.Text = _locale.Tr("feedback.gallery.title");
        _subtitle.Text = _locale.Tr(
            "feedback.gallery.subtitle",
            domainLabel
        );
        _close.Text = _locale.Tr("menu.back");

        foreach (var child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var tile in ImmediateFeedbackAcceptanceGallery
                     .BuildTiles(_visualSettings, _domainId))
        {
            _grid.AddChild(TileCard(tile));
        }
    }

    private Control TileCard(ImmediateFeedbackAcceptanceTile tile)
    {
        var cue = tile.Cue;
        var scenario = tile.Scenario;
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(270, 48)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                ThemeFactory.PanelLight,
                cue.AccentColor,
                cue.BorderWidth,
                5,
                5
            )
        );

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 1);
        panel.AddChild(column);

        column.AddChild(ThemeFactory.Label(
            $"{scenario.OutcomeId} · {scenario.MotionId}",
            9,
            cue.AccentColor
        ));
        column.AddChild(ThemeFactory.Label(
            $"pulse {cue.PulseScale:0.00} · shake {cue.ShakePixels:0.0}",
            7,
            ThemeFactory.MutedInk
        ));
        column.AddChild(ThemeFactory.Label(_locale.Tr(cue.MessageKey), 8));
        return panel;
    }
}
