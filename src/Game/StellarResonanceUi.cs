using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class StellarResonanceOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _story;
    private readonly Label _rank;
    private readonly Label _effect;
    private readonly Label _notice;
    private readonly Button _groveWarden;
    private readonly Button _starseeker;
    private readonly Button _close;
    private readonly Dictionary<StellarSkillKind, Label> _skillLabels = [];

    public StellarResonanceOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.01f, 0.02f, 0.08f, 0.9f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(470, 330)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0b1734fa"),
                ThemeFactory.Mint,
                2,
                12
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        panel.AddChild(column);

        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Mint);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);

        var skills = new GridContainer { Columns = 1 };
        skills.AddThemeConstantOverride("v_separation", 3);
        foreach (var snapshot in session.StellarSkillSnapshots())
        {
            var label = ThemeFactory.Label(size: 10);
            label.CustomMinimumSize = new Vector2(410, 18);
            _skillLabels[snapshot.Kind] = label;
            skills.AddChild(label);
        }
        column.AddChild(skills);

        var specializationRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        specializationRow.AddThemeConstantOverride("separation", 8);
        _groveWarden = ThemeFactory.Button("");
        _starseeker = ThemeFactory.Button("");
        _groveWarden.CustomMinimumSize = new Vector2(194, 24);
        _starseeker.CustomMinimumSize = new Vector2(194, 24);
        specializationRow.AddChild(_groveWarden);
        specializationRow.AddChild(_starseeker);
        column.AddChild(specializationRow);

        _story = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _story.HorizontalAlignment = HorizontalAlignment.Center;
        _story.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _story.CustomMinimumSize = new Vector2(420, 34);
        column.AddChild(_story);

        _rank = ThemeFactory.Label(size: 11, color: ThemeFactory.Mint);
        _rank.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_rank);

        _effect = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _effect.HorizontalAlignment = HorizontalAlignment.Center;
        _effect.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _effect.CustomMinimumSize = new Vector2(420, 32);
        column.AddChild(_effect);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(420, 16);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(150, 24);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(_close);

        _groveWarden.Pressed += () => ChooseGatheringSpecialization(
            GatheringSkillCatalog.GroveWardenId
        );
        _starseeker.Pressed += () => ChooseGatheringSpecialization(
            GatheringSkillCatalog.StarseekerId
        );
        _close.Pressed += () => CloseRequested?.Invoke();
        _locale.LocaleChanged += RefreshText;
        _session.Changed += RefreshText;
        RefreshText();
        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;

    private void ChooseGatheringSpecialization(string specializationId)
    {
        var result = _session.ChooseGatheringSpecialization(specializationId);
        _notice.Text = _locale.Tr(result.MessageKey);
        RefreshText(keepNotice: true);
    }

    private void RefreshText() => RefreshText(keepNotice: false);

    private void RefreshText(bool keepNotice)
    {
        _title.Text = _locale.Tr("stellar.panel.title");
        foreach (var snapshot in _session.StellarSkillSnapshots())
        {
            _skillLabels[snapshot.Kind].Text = _locale.Tr(
                "stellar.panel.skill_row",
                _locale.Tr(snapshot.NameKey),
                snapshot.Level,
                snapshot.MaximumLevel,
                snapshot.IsMaximumLevel
                    ? _locale.Tr("stellar.panel.max_ready")
                    : _locale.Tr("stellar.panel.growing")
            );
        }

        var gathering = _session.GatheringSkill;
        _groveWarden.Text = SpecializationText(
            GatheringSkillCatalog.GroveWardenId
        );
        _starseeker.Text = SpecializationText(
            GatheringSkillCatalog.StarseekerId
        );
        _groveWarden.Disabled = !gathering.CanChooseSpecialization;
        _starseeker.Disabled = !gathering.CanChooseSpecialization;
        _groveWarden.TooltipText = _locale.Tr(
            GatheringSkillCatalog.Specializations[
                GatheringSkillCatalog.GroveWardenId
            ].DescriptionKey
        );
        _starseeker.TooltipText = _locale.Tr(
            GatheringSkillCatalog.Specializations[
                GatheringSkillCatalog.StarseekerId
            ].DescriptionKey
        );

        var readiness = _session.CheckMainStoryCompletion();
        _story.Text = _session.StellarResonance.MainStoryCompleted
            ? _locale.Tr(
                "stellar.panel.story_completed",
                _session.StellarResonance.CompletionDay
            )
            : _locale.Tr(readiness.MessageKey);

        var resonance = _session.StellarResonance;
        _rank.Text = _locale.Tr(
            "stellar.panel.rank",
            resonance.Rank,
            resonance.MaximumRank,
            resonance.Experience
        );
        _effect.Text = _locale.Tr(
            $"stellar.rank.{resonance.Rank}.description"
        );
        _close.Text = _locale.Tr("menu.back");
        if (!keepNotice)
        {
            _notice.Text = string.Empty;
        }
    }

    private string SpecializationText(string specializationId)
    {
        var definition = GatheringSkillCatalog.Specializations[
            specializationId
        ];
        if (_session.GatheringSkill.SpecializationId == specializationId)
        {
            return _locale.Tr(
                "gathering.specialization.chosen_label",
                _locale.Tr(definition.NameKey)
            );
        }

        return _locale.Tr(
            "gathering.specialization.choose_label",
            _locale.Tr(definition.NameKey)
        );
    }

    public override void _ExitTree()
    {
        _locale.LocaleChanged -= RefreshText;
        _session.Changed -= RefreshText;
    }
}

public sealed partial class JourneyRecapOverlay : FullScreenUi
{
    public JourneyRecapOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        var snapshot = session.JourneyRecap();
        AddChild(Dim(new Color(0.005f, 0.012f, 0.055f, 0.94f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(520, 332)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#101a3afa"),
                ThemeFactory.Gold,
                2,
                14
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);

        var title = ThemeFactory.Label(
            locale.Tr("story01.recap.title"),
            22,
            ThemeFactory.Mint
        );
        title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(title);

        var subtitle = ThemeFactory.Label(
            locale.Tr("story01.recap.subtitle"),
            11,
            ThemeFactory.Gold
        );
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(subtitle);

        AddSummaryLine(
            column,
            locale.Tr(
                "story01.recap.lights",
                snapshot.RestoredPedestalIds.Count,
                snapshot.TotalPedestalCount
            )
        );
        var lights = new GridContainer
        {
            Columns = 2,
            CustomMinimumSize = new Vector2(470, 66)
        };
        lights.AddThemeConstantOverride("h_separation", 8);
        lights.AddThemeConstantOverride("v_separation", 2);
        column.AddChild(lights);
        foreach (var starlight in snapshot.Starlights)
        {
            var name = locale.Tr(DataCatalog.StarlightPedestal(
                starlight.PedestalId
            ).NameKey);
            var text = starlight.RestorationStoryDay is { } day
                ? locale.Tr("story01.recap.light.recorded", name, day)
                : locale.Tr(
                    starlight.Restored
                        ? "story01.recap.light.legacy"
                        : "story01.recap.light.unrestored",
                    name
                );
            var label = ThemeFactory.Label(text, 9);
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.CustomMinimumSize = new Vector2(228, 20);
            lights.AddChild(label);
        }
        AddSummaryLine(
            column,
            locale.Tr(
                "story01.recap.relationships",
                snapshot.MetNpcCount,
                snapshot.TrustedFriendCount,
                snapshot.KindredLightCount
            )
        );
        var companions = snapshot.TopCompanions.Count == 0
            ? locale.Tr("story01.recap.companions.none")
            : string.Join(
                locale.Tr("story01.recap.list.separator"),
                snapshot.TopCompanions.Select(companion => locale.Tr(
                    "story01.recap.companion.entry",
                    locale.Tr(VillageCatalog.Npcs[
                        companion.NpcId
                    ].NameKey),
                    companion.RelationshipPoints
                ))
            );
        AddSummaryLine(
            column,
            locale.Tr("story01.recap.companions", companions)
        );
        AddSummaryLine(
            column,
            locale.Tr(
                "story01.recap.exploration",
                snapshot.ExploredChunkCount,
                snapshot.TotalChunkCount,
                snapshot.ExploredRegionCount,
                snapshot.TotalRegionCount
            )
        );
        AddSummaryLine(
            column,
            locale.Tr(
                "story01.recap.events",
                snapshot.CompletedCharacterEventCount,
                snapshot.TotalCharacterEventCount,
                snapshot.CompletedStarlightStoryBeatCount,
                snapshot.TotalStarlightStoryBeatCount
            )
        );

        var buttons = new HBoxContainer();
        buttons.Alignment = BoxContainer.AlignmentMode.Center;
        buttons.AddThemeConstantOverride("separation", 10);
        column.AddChild(buttons);

        var back = ThemeFactory.Button(locale.Tr("story01.recap.back"));
        back.CustomMinimumSize = new Vector2(170, 28);
        back.Pressed += () => CloseRequested?.Invoke();
        buttons.AddChild(back);

        var confirm = ThemeFactory.Button(locale.Tr("story01.recap.confirm"));
        confirm.CustomMinimumSize = new Vector2(190, 28);
        confirm.Pressed += () => ConfirmRequested?.Invoke();
        buttons.AddChild(confirm);
        confirm.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? ConfirmRequested;
    public event Action? CloseRequested;

    private static void AddSummaryLine(
        VBoxContainer column,
        string text
    )
    {
        var label = ThemeFactory.Label(text, 11);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(470, 22);
        column.AddChild(label);
    }
}

public sealed partial class MainStoryEndingOverlay : FullScreenUi
{
    public MainStoryEndingOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        AddChild(Dim(new Color(0.005f, 0.012f, 0.055f, 0.94f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(500, 318)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#101a3afa"),
                ThemeFactory.Gold,
                2,
                14
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 9);
        panel.AddChild(column);

        var title = ThemeFactory.Label(
            locale.Tr("stellar.ending.title"),
            24,
            ThemeFactory.Mint
        );
        title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(title);

        var subtitle = ThemeFactory.Label(
            locale.Tr("stellar.ending.subtitle"),
            13,
            ThemeFactory.Gold
        );
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(subtitle);

        var body = ThemeFactory.Label(
            locale.Tr("stellar.ending.body"),
            11
        );
        body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        body.HorizontalAlignment = HorizontalAlignment.Center;
        body.CustomMinimumSize = new Vector2(450, 112);
        column.AddChild(body);

        var summary = ThemeFactory.Label(
            locale.Tr(
                "stellar.ending.summary",
                session.Clock.Day,
                CalendarSystem.YearNumber(session.Clock.Day),
                session.StarGate.TravelCount
            ),
            10,
            ThemeFactory.MutedInk
        );
        summary.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(summary);

        var continueButton = ThemeFactory.Button(
            locale.Tr("stellar.ending.continue")
        );
        continueButton.CustomMinimumSize = new Vector2(220, 28);
        continueButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        continueButton.Pressed += () => ContinueRequested?.Invoke();
        column.AddChild(continueButton);
        continueButton.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? ContinueRequested;
}
