using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class FestivalMemoriesOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _notice;
    private readonly VBoxContainer _results;
    private readonly Button _close;

    public FestivalMemoriesOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.008f, 0.014f, 0.065f, 0.91f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(548, 338)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#08152ffa"),
                ThemeFactory.Gold,
                2,
                8
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 5);
        panel.AddChild(column);
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Gold);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(510, 238),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        _results = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(494, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _results.AddThemeConstantOverride("separation", 5);
        scroll.AddChild(_results);
        column.AddChild(scroll);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(510, 18);
        column.AddChild(_notice);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(150, 24);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        _session.Changed += Refresh;
        _locale.LocaleChanged += Refresh;
        Refresh();
        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private void Refresh()
    {
        _title.Text = _locale.Tr("festival.replay.panel.title");
        _close.Text = _locale.Tr("menu.back");
        foreach (var child in _results.GetChildren())
        {
            child.QueueFree();
        }

        var results = _session.Festival.Results
            .OrderByDescending(result => result.Year)
            .ThenBy(result => result.FestivalId, StringComparer.Ordinal)
            .ToArray();
        if (results.Length == 0)
        {
            _results.AddChild(ThemeFactory.Label(
                _locale.Tr("festival.replay.panel.empty"),
                10,
                ThemeFactory.MutedInk
            ));
            return;
        }

        foreach (var result in results)
        {
            _results.AddChild(ResultPanel(result));
        }
    }

    private Control ResultPanel(FestivalYearResultSave result)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#101f3dd8"),
                ThemeFactory.Mint,
                1,
                5
            )
        );
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 3);
        panel.AddChild(column);

        var festival = FestivalCatalog.Festivals[result.FestivalId];
        var rule = FestivalCatalog.ReplayRules.GetValueOrDefault(
            result.RuleVariantId,
            FestivalCatalog.ReplayRules[FestivalCatalog.ClassicRuleId]
        );
        column.AddChild(ThemeFactory.Label(
            _locale.Tr(
                "festival.replay.panel.result",
                _locale.Tr(festival.NameKey),
                result.Year,
                result.Score,
                _locale.Tr(rule.NameKey)
            ),
            10,
            ThemeFactory.Gold
        ));
        var ruleDescription = ThemeFactory.Label(
            _locale.Tr(rule.DescriptionKey),
            8,
            ThemeFactory.MutedInk
        );
        ruleDescription.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(ruleDescription);

        if (result.RewardClaimed &&
            FestivalCatalog.RewardChoices.TryGetValue(
                result.RewardChoiceId,
                out var claimedChoice
            ))
        {
            column.AddChild(ThemeFactory.Label(
                _locale.Tr(
                    "festival.replay.reward.claimed_summary",
                    _locale.Tr(DataCatalog.Item(claimedChoice.ItemId).NameKey),
                    claimedChoice.Count
                ),
                9,
                ThemeFactory.Mint
            ));
            return panel;
        }

        var actions = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actions.AddThemeConstantOverride("separation", 6);
        foreach (var choice in FestivalCatalog.RewardChoicesFor(
                     result.FestivalId
                 ))
        {
            var itemName = _locale.Tr(DataCatalog.Item(choice.ItemId).NameKey);
            var button = ThemeFactory.Button(
                _locale.Tr(
                    "festival.replay.reward.choice",
                    itemName,
                    choice.Count
                )
            );
            button.CustomMinimumSize = new Vector2(225, 25);
            button.Pressed += () => Claim(result, choice);
            actions.AddChild(button);
        }
        column.AddChild(actions);
        return panel;
    }

    private void Claim(
        FestivalYearResultSave result,
        FestivalRewardChoiceDefinition choice
    )
    {
        var claim = _session.ClaimFestivalReplayReward(
            result.FestivalId,
            result.Year,
            choice.Id
        );
        _notice.Text = _locale.Tr(claim.MessageKey);
        Refresh();
    }
}
