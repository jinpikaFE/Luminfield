using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed record StarfallWatchPatrolDisplayItem(
    string PatrolId,
    string Name,
    string Description,
    string Reward,
    string StatusKey,
    string ButtonText
);

public sealed record StarfallWatchBountyDisplayItem(
    string BountyId,
    string Name,
    string Description,
    string Reward,
    int Progress,
    int RequiredCount,
    string StatusKey
);

public sealed record StarfallWatchPreparationDisplayItem(
    string PreparationId,
    string Name,
    string Description,
    string StatusKey,
    string ButtonText
);

public sealed partial class StarfallWatchOverlay : FullScreenUi
{
    public const string TitleKey = "starfall_watch.ui.title";
    public const string DayProgressKey = "starfall_watch.ui.day_progress";
    public const string SubtitleKey = "starfall_watch.ui.active_hint";
    public const string PatrolTitleKey = "starfall_watch.ui.patrol.title";
    public const string BountyTitleKey = "starfall_watch.ui.bounty.title";
    public const string PreparationTitleKey =
        "starfall_watch.ui.preparation.title";
    public const string PatrolButtonKey =
        "starfall_watch.ui.patrol.button";
    public const string PreparationButtonKey =
        "starfall_watch.ui.preparation.button";
    public const string RewardCoinsKey = "starfall_watch.ui.reward.coins";
    public const string RewardItemKey = "starfall_watch.ui.reward.item";
    public const string RewardCoinsAndItemKey =
        "starfall_watch.ui.reward.coins_and_item";
    public const string RewardNoneKey = "starfall_watch.ui.reward.none";
    public const string BountyProgressKey = "starfall_watch.ui.progress";
    public const string StateOfferedKey = "starfall_watch.ui.state.offered";
    public const string StateActiveKey = "starfall_watch.ui.state.active";
    public const string StateReadyKey = "starfall_watch.ui.state.ready";
    public const string StateCompletedKey =
        "starfall_watch.ui.state.completed";
    public const string StateFailedKey = "starfall_watch.ui.state.failed";
    public const string StateLockedKey = "starfall_watch.ui.state.locked";
    public const string StateSelectedKey = "starfall_watch.ui.state.selected";
    public const string StateConsumedKey = "starfall_watch.ui.state.consumed";
    public const string ActionAcceptKey = "starfall_watch.ui.action.accept";
    public const string ActionClaimKey = "starfall_watch.ui.action.claim";
    public const string ActionSelectKey = "starfall_watch.ui.action.select";
    public const string EmptyKey = "starfall_watch.ui.empty";
    public const string CloseKey = "menu.back";

    public static IReadOnlyList<string> RequiredLocalizationKeys
    {
        get
        {
            var keys = new List<string>
            {
                TitleKey,
                DayProgressKey,
                SubtitleKey,
                PatrolTitleKey,
                BountyTitleKey,
                PreparationTitleKey,
                PatrolButtonKey,
                PreparationButtonKey,
                RewardCoinsKey,
                RewardItemKey,
                RewardCoinsAndItemKey,
                RewardNoneKey,
                BountyProgressKey,
                StateOfferedKey,
                StateActiveKey,
                StateReadyKey,
                StateCompletedKey,
                StateFailedKey,
                StateLockedKey,
                StateSelectedKey,
                StateConsumedKey,
                ActionAcceptKey,
                ActionClaimKey,
                ActionSelectKey,
                EmptyKey,
                CloseKey,
                "watch.board.opened",
                "starfall_watch.patrol.unavailable",
                "starfall_watch.patrol.already_completed",
                "starfall_watch.patrol.daily_limit_reached",
                "starfall_watch.patrol.active_exists",
                "starfall_watch.patrol.ready",
                "starfall_watch.patrol.accepted",
                "starfall_watch.patrol.no_active",
                "starfall_watch.patrol.target_not_reached",
                "starfall_watch.patrol.target_reached",
                "starfall_watch.patrol.completed",
                "starfall_watch.bounty.unavailable",
                "starfall_watch.bounty.already_completed",
                "starfall_watch.bounty.failed_today",
                "starfall_watch.bounty.active_exists",
                "starfall_watch.bounty.ready",
                "starfall_watch.bounty.accepted",
                "starfall_watch.bounty.no_active",
                "starfall_watch.bounty.not_complete",
                "starfall_watch.bounty.progressed",
                "starfall_watch.bounty.completed",
                "starfall_watch.prep.unavailable",
                "starfall_watch.prep.already_selected",
                "starfall_watch.prep.ready",
                "starfall_watch.prep.consumed",
                "starfall_watch.prep.selected",
                "notice.inventory_full",
                "notice.needs_hand",
                "notice.starfall_watch_closed",
                "notice.nothing_to_interact"
            };

            foreach (var patrol in StarfallWatchSystem.Patrols)
            {
                keys.Add(patrol.NameKey);
                keys.Add(patrol.DescriptionKey);
            }

            foreach (var bounty in StarfallWatchSystem.Bounties)
            {
                keys.Add(bounty.NameKey);
                keys.Add(bounty.DescriptionKey);
            }

            foreach (var preparation in StarfallWatchSystem.Preparations)
            {
                keys.Add(preparation.NameKey);
                keys.Add(preparation.DescriptionKey);
            }

            return keys;
        }
    }

    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly VBoxContainer _patrolList;
    private readonly VBoxContainer _preparationList;
    private readonly Dictionary<string, Button> _patrolButtons =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> _preparationButtons =
        new(StringComparer.Ordinal);
    private readonly Label _title;
    private readonly Label _day;
    private readonly Label _subtitle;
    private readonly Label _patrolTitle;
    private readonly Label _patrolName;
    private readonly Label _patrolDescription;
    private readonly Label _patrolReward;
    private readonly Label _patrolState;
    private readonly Button _patrolAction;
    private readonly Label _bountyTitle;
    private readonly Label _bountyName;
    private readonly Label _bountyDescription;
    private readonly Label _bountyReward;
    private readonly Label _bountyProgress;
    private readonly Label _bountyState;
    private readonly Button _bountyAction;
    private readonly Label _preparationTitle;
    private readonly Label _preparationName;
    private readonly Label _preparationDescription;
    private readonly Label _preparationState;
    private readonly Button _preparationAction;
    private readonly Label _notice;
    private readonly Button _close;
    private string? _selectedPatrolId;
    private string? _selectedPreparationId;

    public StarfallWatchOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;

        AddChild(Dim(new Color(0.008f, 0.014f, 0.058f, 0.88f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(586, 328)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0b1734fc"),
                ThemeFactory.Mint,
                2,
                10
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 5);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 9);
        header.AddChild(Icon(
            GeneratedArt.CreateStarlightNodeSealIcon(),
            new Vector2(48, 40)
        ));

        var headerText = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _title = ThemeFactory.Label(size: 20, color: ThemeFactory.Mint);
        _day = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        headerText.AddChild(_title);
        headerText.AddChild(_day);
        header.AddChild(headerText);

        _subtitle = ThemeFactory.Label(size: 10, color: ThemeFactory.MutedInk);
        _subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _subtitle.CustomMinimumSize = new Vector2(206, 36);
        _subtitle.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_subtitle);
        column.AddChild(header);

        var content = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(548, 202),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", 7);
        column.AddChild(content);

        var patrol = Section(
            content,
            ThemeFactory.PanelEdge,
            new Vector2(208, 202)
        );
        _patrolTitle = SectionTitle();
        _patrolList = new VBoxContainer();
        _patrolList.AddThemeConstantOverride("separation", 4);
        _patrolName = DetailLabel(11, ThemeFactory.Gold, 24);
        _patrolDescription = DetailLabel(8, ThemeFactory.MutedInk, 28);
        _patrolReward = DetailLabel(9, ThemeFactory.Mint, 14);
        _patrolState = DetailLabel(9, ThemeFactory.Gold, 14);
        _patrolAction = ActionButton(174);
        _patrolAction.Pressed += PressPatrolAction;
        patrol.AddChild(_patrolTitle);
        patrol.AddChild(_patrolList);
        patrol.AddChild(_patrolName);
        patrol.AddChild(_patrolDescription);
        patrol.AddChild(_patrolReward);
        patrol.AddChild(_patrolState);
        patrol.AddChild(_patrolAction);

        var bounty = Section(
            content,
            ThemeFactory.Gold,
            new Vector2(162, 202)
        );
        _bountyTitle = SectionTitle();
        _bountyName = DetailLabel(11, ThemeFactory.Gold, 24);
        _bountyDescription = DetailLabel(8, ThemeFactory.MutedInk, 38);
        _bountyReward = DetailLabel(9, ThemeFactory.Mint, 14);
        _bountyProgress = DetailLabel(9, ThemeFactory.Mint, 14);
        _bountyState = DetailLabel(9, ThemeFactory.Gold, 14);
        _bountyAction = ActionButton(134);
        _bountyAction.Pressed += PressBountyAction;
        bounty.AddChild(_bountyTitle);
        bounty.AddChild(Icon(
            GeneratedArt.CreateCommissionParchmentIcon(),
            new Vector2(34, 24)
        ));
        bounty.AddChild(_bountyName);
        bounty.AddChild(_bountyDescription);
        bounty.AddChild(_bountyReward);
        bounty.AddChild(_bountyProgress);
        bounty.AddChild(_bountyState);
        bounty.AddChild(_bountyAction);

        var preparation = Section(
            content,
            ThemeFactory.Violet,
            new Vector2(164, 202)
        );
        _preparationTitle = SectionTitle();
        _preparationList = new VBoxContainer();
        _preparationList.AddThemeConstantOverride("separation", 4);
        _preparationName = DetailLabel(11, ThemeFactory.Gold, 24);
        _preparationDescription = DetailLabel(8, ThemeFactory.MutedInk, 32);
        _preparationState = DetailLabel(9, ThemeFactory.Gold, 14);
        _preparationAction = ActionButton(136);
        _preparationAction.Pressed += PressPreparationAction;
        preparation.AddChild(_preparationTitle);
        preparation.AddChild(_preparationList);
        preparation.AddChild(_preparationName);
        preparation.AddChild(_preparationDescription);
        preparation.AddChild(_preparationState);
        preparation.AddChild(_preparationAction);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(548, 15);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 26);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        FocusInitialControl();
    }

    public event Action? CloseRequested;
    public event Action? ActionCompleted;

    public void ShowNotice(string messageKey)
    {
        _notice.Text = _locale.Tr(messageKey);
    }

    public void SelectPatrolForPlaytest(string patrolId)
    {
        _selectedPatrolId = patrolId;
        RefreshText(keepNotice: true);
    }

    public void PressPatrolActionForPlaytest()
    {
        _patrolAction.EmitSignal(Button.SignalName.Pressed);
    }

    public void PressBountyActionForPlaytest()
    {
        _bountyAction.EmitSignal(Button.SignalName.Pressed);
    }

    public void SelectPreparationForPlaytest(string preparationId)
    {
        _selectedPreparationId = preparationId;
        RefreshText(keepNotice: true);
    }

    public void PressPreparationActionForPlaytest()
    {
        _preparationAction.EmitSignal(Button.SignalName.Pressed);
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    public void RefreshText() => RefreshText(keepNotice: false);

    public static IReadOnlyList<StarfallWatchPatrolDisplayItem>
        CreatePatrolItems(
            StarfallWatchBoardSnapshot board,
            LocaleService locale
        ) => board.PatrolOffers
            .Select(patrol => CreatePatrolItem(board, patrol, locale))
            .ToArray();

    public static StarfallWatchBountyDisplayItem? CreateBountyItem(
        StarfallWatchBoardSnapshot board,
        LocaleService locale
    )
    {
        if (board.BountyOffer is null)
        {
            return null;
        }

        var bounty = board.BountyOffer;
        return new StarfallWatchBountyDisplayItem(
            bounty.Id,
            locale.Tr(bounty.NameKey),
            locale.Tr(bounty.DescriptionKey),
            RewardText(
                bounty.RewardCoins,
                bounty.RewardItemId,
                bounty.RewardItemCount,
                locale
            ),
            board.ActiveBountyProgress,
            bounty.RequiredCount,
            BountyStatusKey(board, bounty)
        );
    }

    public static IReadOnlyList<StarfallWatchPreparationDisplayItem>
        CreatePreparationItems(
            StarfallWatchBoardSnapshot board,
            LocaleService locale
        ) => board.Preparations
            .Select(preparation =>
                CreatePreparationItem(board, preparation, locale))
            .ToArray();

    private static StarfallWatchPatrolDisplayItem CreatePatrolItem(
        StarfallWatchBoardSnapshot board,
        StarfallWatchPatrolDefinition patrol,
        LocaleService locale
    )
    {
        var statusKey = PatrolStatusKey(board, patrol);
        var name = locale.Tr(patrol.NameKey);
        var status = locale.Tr(statusKey);
        return new StarfallWatchPatrolDisplayItem(
            patrol.Id,
            name,
            locale.Tr(patrol.DescriptionKey),
            RewardText(
                patrol.RewardCoins,
                patrol.RewardItemId,
                patrol.RewardItemCount,
                locale
            ),
            statusKey,
            locale.Tr(PatrolButtonKey, name, status)
        );
    }

    private static StarfallWatchPreparationDisplayItem
        CreatePreparationItem(
            StarfallWatchBoardSnapshot board,
            StarfallWatchPreparationDefinition preparation,
            LocaleService locale
        )
    {
        var statusKey = PreparationStatusKey(board, preparation);
        var name = locale.Tr(preparation.NameKey);
        var status = locale.Tr(statusKey);
        return new StarfallWatchPreparationDisplayItem(
            preparation.Id,
            name,
            locale.Tr(preparation.DescriptionKey),
            statusKey,
            locale.Tr(PreparationButtonKey, name, status)
        );
    }

    private void RefreshText(bool keepNotice)
    {
        var board = _session.TodayStarfallWatchBoard;
        EnsureSelections(board);
        var patrolItems = CreatePatrolItems(board, _locale);
        var bountyItem = CreateBountyItem(board, _locale);
        var preparationItems = CreatePreparationItems(board, _locale);

        _title.Text = _locale.Tr(TitleKey);
        _day.Text = _locale.Tr(DayProgressKey, board.Day);
        _subtitle.Text = _locale.Tr(SubtitleKey);
        _patrolTitle.Text = _locale.Tr(PatrolTitleKey);
        _bountyTitle.Text = _locale.Tr(BountyTitleKey);
        _preparationTitle.Text = _locale.Tr(PreparationTitleKey);
        _close.Text = _locale.Tr(CloseKey);
        if (!keepNotice)
        {
            _notice.Text = string.Empty;
        }

        RebuildPatrolList(patrolItems);
        RebuildPreparationList(preparationItems);
        RefreshSelectedPatrol(board, patrolItems);
        RefreshBounty(board, bountyItem);
        RefreshSelectedPreparation(board, preparationItems);
    }

    private void RebuildPatrolList(
        IReadOnlyList<StarfallWatchPatrolDisplayItem> items
    )
    {
        foreach (var child in _patrolList.GetChildren())
        {
            child.QueueFree();
        }
        _patrolButtons.Clear();

        foreach (var item in items)
        {
            var button = ThemeFactory.Button(item.ButtonText);
            button.CustomMinimumSize = new Vector2(174, 26);
            button.ToggleMode = true;
            button.ButtonPressed = item.PatrolId == _selectedPatrolId;
            button.TooltipText = item.Description;
            ThemeFactory.SetFontSize(button, 8);
            button.Pressed += () => SelectPatrol(item.PatrolId);
            _patrolButtons[item.PatrolId] = button;
            _patrolList.AddChild(button);
        }
    }

    private void RebuildPreparationList(
        IReadOnlyList<StarfallWatchPreparationDisplayItem> items
    )
    {
        foreach (var child in _preparationList.GetChildren())
        {
            child.QueueFree();
        }
        _preparationButtons.Clear();

        foreach (var item in items)
        {
            var button = ThemeFactory.Button(item.ButtonText);
            button.CustomMinimumSize = new Vector2(136, 22);
            button.ToggleMode = true;
            button.ButtonPressed =
                item.PreparationId == _selectedPreparationId;
            button.TooltipText = item.Description;
            ThemeFactory.SetFontSize(button, 8);
            button.Pressed += () => SelectPreparation(item.PreparationId);
            _preparationButtons[item.PreparationId] = button;
            _preparationList.AddChild(button);
        }
    }

    private void RefreshSelectedPatrol(
        StarfallWatchBoardSnapshot board,
        IReadOnlyList<StarfallWatchPatrolDisplayItem> items
    )
    {
        var selected = items.FirstOrDefault(item =>
            item.PatrolId == _selectedPatrolId
        );
        if (selected is null)
        {
            _patrolName.Text = _locale.Tr(EmptyKey);
            _patrolDescription.Text = string.Empty;
            _patrolReward.Text = string.Empty;
            _patrolState.Text = string.Empty;
            _patrolAction.Text = _locale.Tr(StateLockedKey);
            _patrolAction.Disabled = true;
            return;
        }

        _patrolName.Text = selected.Name;
        _patrolDescription.Text = selected.Description;
        _patrolReward.Text = selected.Reward;
        _patrolState.Text = _locale.Tr(selected.StatusKey);
        _patrolState.AddThemeColorOverride(
            "font_color",
            StatusColor(selected.StatusKey)
        );
        RefreshPatrolAction(selected);
    }

    private void RefreshBounty(
        StarfallWatchBoardSnapshot board,
        StarfallWatchBountyDisplayItem? item
    )
    {
        if (item is null)
        {
            _bountyName.Text = _locale.Tr(EmptyKey);
            _bountyDescription.Text = string.Empty;
            _bountyReward.Text = string.Empty;
            _bountyProgress.Text = string.Empty;
            _bountyState.Text = string.Empty;
            _bountyAction.Text = _locale.Tr(StateLockedKey);
            _bountyAction.Disabled = true;
            return;
        }

        _bountyName.Text = item.Name;
        _bountyDescription.Text = item.Description;
        _bountyReward.Text = item.Reward;
        _bountyProgress.Text = _locale.Tr(
            BountyProgressKey,
            item.Progress,
            item.RequiredCount
        );
        _bountyState.Text = _locale.Tr(item.StatusKey);
        _bountyState.AddThemeColorOverride(
            "font_color",
            StatusColor(item.StatusKey)
        );
        RefreshBountyAction(item);
    }

    private void RefreshSelectedPreparation(
        StarfallWatchBoardSnapshot board,
        IReadOnlyList<StarfallWatchPreparationDisplayItem> items
    )
    {
        var selected = items.FirstOrDefault(item =>
            item.PreparationId == _selectedPreparationId
        );
        if (selected is null)
        {
            _preparationName.Text = _locale.Tr(EmptyKey);
            _preparationDescription.Text = string.Empty;
            _preparationState.Text = string.Empty;
            _preparationAction.Text = _locale.Tr(StateLockedKey);
            _preparationAction.Disabled = true;
            return;
        }

        _preparationName.Text = selected.Name;
        _preparationDescription.Text = selected.Description;
        _preparationState.Text = _locale.Tr(selected.StatusKey);
        _preparationState.AddThemeColorOverride(
            "font_color",
            StatusColor(selected.StatusKey)
        );
        RefreshPreparationAction(selected);
    }

    private void RefreshPatrolAction(
        StarfallWatchPatrolDisplayItem item
    )
    {
        _patrolAction.Disabled = true;

        if (item.StatusKey == StateReadyKey)
        {
            _patrolAction.Text = _locale.Tr(ActionClaimKey);
            _patrolAction.Disabled = false;
            return;
        }

        if (item.StatusKey == StateOfferedKey)
        {
            _patrolAction.Text = _locale.Tr(ActionAcceptKey);
            _patrolAction.Disabled = false;
            return;
        }

        _patrolAction.Text = _locale.Tr(item.StatusKey);
    }

    private void RefreshBountyAction(
        StarfallWatchBountyDisplayItem item
    )
    {
        _bountyAction.Disabled = true;

        if (item.StatusKey == StateReadyKey)
        {
            _bountyAction.Text = _locale.Tr(ActionClaimKey);
            _bountyAction.Disabled = false;
            return;
        }

        if (item.StatusKey == StateOfferedKey)
        {
            _bountyAction.Text = _locale.Tr(ActionAcceptKey);
            _bountyAction.Disabled = false;
            return;
        }

        _bountyAction.Text = _locale.Tr(item.StatusKey);
    }

    private void RefreshPreparationAction(
        StarfallWatchPreparationDisplayItem item
    )
    {
        _preparationAction.Disabled = true;

        if (item.StatusKey == StateOfferedKey)
        {
            _preparationAction.Text = _locale.Tr(ActionSelectKey);
            _preparationAction.Disabled = false;
            return;
        }

        _preparationAction.Text = _locale.Tr(item.StatusKey);
    }

    private void SelectPatrol(string patrolId)
    {
        _selectedPatrolId = patrolId;
        RefreshText(keepNotice: true);
    }

    private void SelectPreparation(string preparationId)
    {
        _selectedPreparationId = preparationId;
        RefreshText(keepNotice: true);
    }

    private void PressPatrolAction()
    {
        if (_selectedPatrolId is null)
        {
            return;
        }

        var board = _session.TodayStarfallWatchBoard;
        var selected = board.PatrolOffers.FirstOrDefault(patrol =>
            patrol.Id == _selectedPatrolId
        );
        if (selected is null)
        {
            return;
        }

        var statusKey = PatrolStatusKey(board, selected);
        if (statusKey == StateReadyKey)
        {
            var claim = _session.ClaimStarfallWatchPatrolReward(
                out _
            );
            ApplyResult(claim);
            return;
        }

        if (statusKey == StateOfferedKey)
        {
            var accept = _session.AcceptStarfallWatchPatrol(
                _selectedPatrolId
            );
            ApplyResult(accept);
        }
    }

    private void PressBountyAction()
    {
        var board = _session.TodayStarfallWatchBoard;
        if (board.BountyOffer is null)
        {
            return;
        }

        var statusKey = BountyStatusKey(board, board.BountyOffer);
        if (statusKey == StateReadyKey)
        {
            var claim = _session.ClaimStarfallWatchBountyReward(
                out _
            );
            ApplyResult(claim);
            return;
        }

        if (statusKey == StateOfferedKey)
        {
            var accept = _session.AcceptStarfallWatchBounty(
                board.BountyOffer.Id
            );
            ApplyResult(accept);
        }
    }

    private void PressPreparationAction()
    {
        if (_selectedPreparationId is null)
        {
            return;
        }

        var board = _session.TodayStarfallWatchBoard;
        var selected = board.Preparations.FirstOrDefault(
            preparation => preparation.Id == _selectedPreparationId
        );
        if (selected is null)
        {
            return;
        }

        var statusKey = PreparationStatusKey(board, selected);
        if (statusKey != StateOfferedKey)
        {
            return;
        }

        var result = _session.SelectStarfallWatchPreparation(
            _selectedPreparationId
        );
        ApplyResult(result);
    }

    private void ApplyResult(ActionResult result)
    {
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            ActionCompleted?.Invoke();
        }

        RefreshText(keepNotice: true);
        FocusInitialControl();
    }

    private void EnsureSelections(StarfallWatchBoardSnapshot board)
    {
        EnsurePatrolSelection(board);
        EnsurePreparationSelection(board);
    }

    private void EnsurePatrolSelection(StarfallWatchBoardSnapshot board)
    {
        if (_selectedPatrolId is not null &&
            board.PatrolOffers.Any(patrol => patrol.Id == _selectedPatrolId))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(board.ActivePatrolId) &&
            board.PatrolOffers.Any(patrol =>
                patrol.Id == board.ActivePatrolId))
        {
            _selectedPatrolId = board.ActivePatrolId;
            return;
        }

        var available = board.PatrolOffers.FirstOrDefault(patrol =>
            !board.CompletedPatrolIds.Contains(patrol.Id)
        );
        _selectedPatrolId =
            available?.Id ?? board.PatrolOffers.FirstOrDefault()?.Id;
    }

    private void EnsurePreparationSelection(StarfallWatchBoardSnapshot board)
    {
        var preparations = board.Preparations;
        if (_selectedPreparationId is not null &&
            preparations.Any(preparation =>
                preparation.Id == _selectedPreparationId))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(board.PreparationId) &&
            preparations.Any(preparation =>
                preparation.Id == board.PreparationId))
        {
            _selectedPreparationId = board.PreparationId;
            return;
        }

        _selectedPreparationId = preparations.FirstOrDefault()?.Id;
    }

    private void FocusInitialControl()
    {
        if (_selectedPatrolId is not null &&
            _patrolButtons.TryGetValue(_selectedPatrolId, out var patrol))
        {
            patrol.CallDeferred(Control.MethodName.GrabFocus);
            return;
        }

        if (_selectedPreparationId is not null &&
            _preparationButtons.TryGetValue(
                _selectedPreparationId,
                out var preparation
            ))
        {
            preparation.CallDeferred(Control.MethodName.GrabFocus);
            return;
        }

        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    private static string PatrolStatusKey(
        StarfallWatchBoardSnapshot board,
        StarfallWatchPatrolDefinition patrol
    )
    {
        if (board.CompletedPatrolIds.Contains(patrol.Id))
        {
            return StateCompletedKey;
        }

        if (board.ActivePatrolId == patrol.Id)
        {
            if (board.PatrolTargetReached)
            {
                return StateReadyKey;
            }

            return StateActiveKey;
        }

        if (!string.IsNullOrWhiteSpace(board.ActivePatrolId))
        {
            return StateLockedKey;
        }

        return StateOfferedKey;
    }

    private static string BountyStatusKey(
        StarfallWatchBoardSnapshot board,
        StarfallWatchBountyDefinition bounty
    )
    {
        if (board.FailedBountyId == bounty.Id)
        {
            return StateFailedKey;
        }

        if (board.CompletedBountyIds.Contains(bounty.Id))
        {
            return StateCompletedKey;
        }

        if (board.ActiveBountyId == bounty.Id)
        {
            if (board.ActiveBountyProgress >= bounty.RequiredCount)
            {
                return StateReadyKey;
            }

            return StateActiveKey;
        }

        if (!string.IsNullOrWhiteSpace(board.ActiveBountyId))
        {
            return StateLockedKey;
        }

        return StateOfferedKey;
    }

    private static string PreparationStatusKey(
        StarfallWatchBoardSnapshot board,
        StarfallWatchPreparationDefinition preparation
    )
    {
        if (board.PreparationConsumed)
        {
            return StateConsumedKey;
        }

        if (board.PreparationId == preparation.Id)
        {
            return StateSelectedKey;
        }

        if (!string.IsNullOrWhiteSpace(board.PreparationId))
        {
            return StateLockedKey;
        }

        return StateOfferedKey;
    }

    private static Color StatusColor(string statusKey)
    {
        if (statusKey == StateCompletedKey ||
            statusKey == StateConsumedKey)
        {
            return ThemeFactory.MutedInk;
        }

        if (statusKey == StateReadyKey ||
            statusKey == StateOfferedKey ||
            statusKey == StateSelectedKey)
        {
            return ThemeFactory.Mint;
        }

        if (statusKey == StateFailedKey)
        {
            return new Color("#e96a9c");
        }

        return ThemeFactory.Gold;
    }

    private static string RewardText(
        int coins,
        string itemId,
        int itemCount,
        LocaleService locale
    )
    {
        var hasCoins = coins > 0;
        var hasItem = itemCount > 0 &&
            !string.IsNullOrWhiteSpace(itemId);
        if (hasCoins && hasItem)
        {
            return locale.Tr(
                RewardCoinsAndItemKey,
                coins,
                ItemName(itemId, locale),
                itemCount
            );
        }

        if (hasCoins)
        {
            return locale.Tr(RewardCoinsKey, coins);
        }

        if (hasItem)
        {
            return locale.Tr(
                RewardItemKey,
                ItemName(itemId, locale),
                itemCount
            );
        }

        return locale.Tr(RewardNoneKey);
    }

    private static string ItemName(string itemId, LocaleService locale)
    {
        if (DataCatalog.Items.TryGetValue(itemId, out var item))
        {
            return locale.Tr(item.NameKey);
        }

        return itemId;
    }

    private static VBoxContainer Section(
        Container parent,
        Color border,
        Vector2 size
    )
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = size,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#101f3ff2"),
                border,
                1,
                6,
                4
            )
        );

        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        column.AddThemeConstantOverride("separation", 3);
        panel.AddChild(column);
        parent.AddChild(panel);
        return column;
    }

    private static Label SectionTitle()
    {
        var label = ThemeFactory.Label(size: 11, color: ThemeFactory.Mint);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.CustomMinimumSize = new Vector2(120, 16);
        return label;
    }

    private static Label DetailLabel(int size, Color color, float height)
    {
        var label = ThemeFactory.Label(size: size, color: color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(120, height);
        return label;
    }

    private static Button ActionButton(float width)
    {
        var button = ThemeFactory.Button("");
        button.CustomMinimumSize = new Vector2(width, 22);
        button.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        ThemeFactory.SetFontSize(button, 9);
        return button;
    }

    private static TextureRect Icon(Texture2D texture, Vector2 size) => new()
    {
        Texture = texture,
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };
}
