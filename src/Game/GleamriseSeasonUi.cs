using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class GleamriseSeasonOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _seasonLine;
    private readonly Label _summary;
    private readonly VBoxContainer _goalList;
    private readonly Label _notice;
    private readonly Button _close;

    public GleamriseSeasonOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        MouseFilter = MouseFilterEnum.Stop;
        AddChild(Dim(new Color(0.014f, 0.02f, 0.075f, 0.86f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(536, 340)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0d1837fa"),
                ThemeFactory.Mint,
                2,
                9
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 5);
        panel.AddChild(column);

        _title = ThemeFactory.Label(size: 21, color: ThemeFactory.Gold);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _seasonLine = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _seasonLine.HorizontalAlignment = HorizontalAlignment.Center;
        _summary = ThemeFactory.Label(size: 10);
        _summary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _summary.CustomMinimumSize = new Vector2(500, 30);
        _summary.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);
        column.AddChild(_seasonLine);
        column.AddChild(_summary);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(504, 204),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        _goalList = new VBoxContainer();
        _goalList.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_goalList);
        column.AddChild(scroll);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(500, 14);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 24);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action<string>? GoalClaimed;

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    public void RefreshText()
    {
        var snapshots = _session.GleamriseSeasonGoals();
        var completed = snapshots.Count(snapshot =>
            snapshot.Status is GleamriseGoalStatus.Ready or
                GleamriseGoalStatus.Claimed
        );
        var final = snapshots.Single(snapshot =>
            snapshot.Definition.IsSeasonFinal
        );

        _title.Text = _locale.Tr("gleamrise.board.title");
        _seasonLine.Text = _locale.Tr(
            "gleamrise.board.season",
            _session.GleamriseSeason.Year,
            _session.Clock.Day,
            CalendarSystem.SeasonDay(_session.Clock.Day)
        );
        _summary.Text = SummaryText(completed, final);
        _close.Text = _locale.Tr("menu.back");

        foreach (var child in _goalList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var snapshot in snapshots)
        {
            _goalList.AddChild(GoalRow(snapshot));
        }
    }

    private string SummaryText(
        int completed,
        GleamriseGoalSnapshot final
    )
    {
        if (final.Status == GleamriseGoalStatus.Claimed)
        {
            return _locale.Tr(
                "gleamrise.board.summary_complete",
                completed,
                GleamriseSeasonGoalSystem.SeasonCompletionRequiredGoals
            );
        }

        if (CalendarSystem.SeasonId(_session.Clock.Day) !=
            CalendarSystem.GleamriseSeasonId &&
            final.Status != GleamriseGoalStatus.Ready)
        {
            return _locale.Tr(
                "gleamrise.board.summary_incomplete",
                completed,
                GleamriseSeasonGoalSystem.SeasonCompletionRequiredGoals
            );
        }

        return _locale.Tr(
            "gleamrise.board.summary",
            completed,
            GleamriseSeasonGoalSystem.SeasonCompletionRequiredGoals
        );
    }

    private Control GoalRow(GleamriseGoalSnapshot snapshot)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(488, 54)
        };
        var border = snapshot.Status == GleamriseGoalStatus.Ready
            ? ThemeFactory.Mint
            : ThemeFactory.Gold;
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#172746f2"),
                border,
                1,
                6,
                6
            )
        );

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        panel.AddChild(row);

        var text = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        text.AddThemeConstantOverride("separation", 2);
        var title = ThemeFactory.Label(size: 12, color: ThemeFactory.Gold);
        title.Text = _locale.Tr(
            "gleamrise.goal.row_title",
            snapshot.Definition.SeasonDay,
            _locale.Tr(snapshot.Definition.TitleKey)
        );
        var description = ThemeFactory.Label(size: 9);
        description.Text = _locale.Tr(snapshot.Definition.DescriptionKey);
        description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        var hint = ThemeFactory.Label(size: 8, color: ThemeFactory.MutedInk);
        hint.Text = _locale.Tr(snapshot.Definition.HintKey);
        text.AddChild(title);
        text.AddChild(description);
        text.AddChild(hint);
        row.AddChild(text);

        var side = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(116, 48)
        };
        side.AddThemeConstantOverride("separation", 3);
        var progress = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        progress.HorizontalAlignment = HorizontalAlignment.Right;
        progress.Text = _locale.Tr(
            "gleamrise.goal.progress",
            snapshot.Progress,
            snapshot.RequiredCount
        );
        var action = ThemeFactory.Button(StatusText(snapshot));
        action.CustomMinimumSize = new Vector2(110, 22);
        action.Disabled = snapshot.Status != GleamriseGoalStatus.Ready;
        var goalId = snapshot.Definition.Id;
        action.Pressed += () => Claim(goalId);
        side.AddChild(progress);
        side.AddChild(action);
        row.AddChild(side);

        return panel;
    }

    private string StatusText(GleamriseGoalSnapshot snapshot)
    {
        return snapshot.Status switch
        {
            GleamriseGoalStatus.Claimed => _locale.Tr(
                "gleamrise.goal.status.claimed"
            ),
            GleamriseGoalStatus.Ready => _locale.Tr(
                "gleamrise.goal.action.claim"
            ),
            GleamriseGoalStatus.Locked => _locale.Tr(
                "gleamrise.goal.status.locked"
            ),
            _ => _locale.Tr("gleamrise.goal.status.open")
        };
    }

    private void Claim(string goalId)
    {
        var result = _session.ClaimGleamriseSeasonGoal(goalId);
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            GoalClaimed?.Invoke(result.MessageKey);
        }

        RefreshText();
    }
}
