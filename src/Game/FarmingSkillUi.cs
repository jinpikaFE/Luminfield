using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class FarmingSpecializationOverlay : FullScreenUi
{
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _body;
    private readonly Label _dewkeeperDescription;
    private readonly Label _resonanceDescription;
    private readonly Label _warning;
    private readonly Button _dewkeeper;
    private readonly Button _resonanceScholar;

    public FarmingSpecializationOverlay(
        Theme theme,
        LocaleService locale
    ) : base(theme)
    {
        _locale = locale;
        MouseFilter = MouseFilterEnum.Stop;
        AddChild(Dim(new Color(0.02f, 0.03f, 0.1f, 0.82f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(520, 282)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#101a3afb"),
                ThemeFactory.Gold,
                2,
                10
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);

        _title = ThemeFactory.Label(size: 23, color: ThemeFactory.Gold);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _body = ThemeFactory.Label(size: 11, color: ThemeFactory.Mint);
        _body.HorizontalAlignment = HorizontalAlignment.Center;
        _body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _body.CustomMinimumSize = new Vector2(464, 32);
        column.AddChild(_title);
        column.AddChild(_body);

        var choices = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        choices.AddThemeConstantOverride("separation", 18);
        column.AddChild(choices);

        var dewkeeperCard = ChoiceCard(
            out _dewkeeper,
            out _dewkeeperDescription
        );
        var resonanceCard = ChoiceCard(
            out _resonanceScholar,
            out _resonanceDescription
        );
        choices.AddChild(dewkeeperCard);
        choices.AddChild(resonanceCard);

        _warning = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _warning.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_warning);

        _dewkeeper.Pressed += () => SelectionRequested?.Invoke(
            FarmingSkillCatalog.DewkeeperId
        );
        _resonanceScholar.Pressed += () => SelectionRequested?.Invoke(
            FarmingSkillCatalog.ResonanceScholarId
        );
        RefreshText();
        _dewkeeper.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action<string>? SelectionRequested;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("farming.specialization.title");
        _body.Text = _locale.Tr("farming.specialization.body");
        SetChoiceText(
            _dewkeeper,
            _dewkeeperDescription,
            FarmingSkillCatalog.Specializations[
                FarmingSkillCatalog.DewkeeperId
            ]
        );
        SetChoiceText(
            _resonanceScholar,
            _resonanceDescription,
            FarmingSkillCatalog.Specializations[
                FarmingSkillCatalog.ResonanceScholarId
            ]
        );
        _warning.Text = _locale.Tr("farming.specialization.warning");
    }

    private static VBoxContainer ChoiceCard(
        out Button button,
        out Label description
    )
    {
        var card = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(216, 126)
        };
        card.AddThemeConstantOverride("separation", 5);
        button = ThemeFactory.Button("");
        button.CustomMinimumSize = new Vector2(216, 40);
        description = ThemeFactory.Label(size: 10);
        description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        description.HorizontalAlignment = HorizontalAlignment.Center;
        description.VerticalAlignment = VerticalAlignment.Center;
        description.CustomMinimumSize = new Vector2(216, 74);
        card.AddChild(button);
        card.AddChild(description);
        return card;
    }

    private void SetChoiceText(
        Button button,
        Label description,
        FarmingSpecializationDefinition definition
    )
    {
        button.Text = _locale.Tr(definition.NameKey);
        description.Text = _locale.Tr(definition.DescriptionKey);
    }
}
