using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class StarlightPedestalOverlay : FullScreenUi
{
    private sealed class NodeRow
    {
        public required StarlightNodeDefinition Definition { get; init; }
        public required Label Title { get; init; }
        public required Label Description { get; init; }
        public required Label Progress { get; init; }
        public required Button Action { get; init; }
    }

    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly string _pedestalId;
    private readonly StarlightPedestalDefinition _pedestal;
    private readonly Label _title;
    private readonly Label _region;
    private readonly Label _overall;
    private readonly List<NodeRow> _rows = [];
    private readonly Label _rewardTitle;
    private readonly Label _rewardDescription;
    private readonly Label _notice;
    private readonly Button _activate;
    private readonly Button _close;

    public StarlightPedestalOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        string pedestalId
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _pedestalId = pedestalId;
        _pedestal = session.Starlight.Definition(pedestalId);
        AddChild(Dim(new Color(0.008f, 0.014f, 0.065f, 0.87f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(548, 348)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#08152ffa"),
                ThemeFactory.Mint,
                2,
                8
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);

        var header = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(510, 42)
        };
        header.AddThemeConstantOverride("separation", 8);
        header.AddChild(Icon(
            NodeSealTexture(pedestalId),
            new Vector2(44, 40)
        ));

        var headerText = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Mint);
        _region = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        headerText.AddChild(_title);
        headerText.AddChild(_region);
        header.AddChild(headerText);

        _overall = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _overall.HorizontalAlignment = HorizontalAlignment.Right;
        _overall.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_overall);
        column.AddChild(header);

        var nodeScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(510, 150),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        var nodeColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        nodeColumn.AddThemeConstantOverride("separation", 4);
        foreach (var node in _pedestal.Nodes)
        {
            nodeColumn.AddChild(BuildNode(node));
        }
        nodeScroll.AddChild(nodeColumn);
        column.AddChild(nodeScroll);

        var rewardPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(510, 42)
        };
        rewardPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#122742e8"),
                ThemeFactory.Gold,
                1,
                6,
                4
            )
        );
        var rewardRow = new HBoxContainer();
        rewardRow.AddThemeConstantOverride("separation", 7);
        rewardRow.AddChild(Icon(
            RewardTexture(pedestalId),
            new Vector2(36, 36)
        ));
        var rewardText = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _rewardTitle = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _rewardDescription = ThemeFactory.Label(
            size: 8,
            color: ThemeFactory.MutedInk
        );
        _rewardDescription.AutowrapMode =
            TextServer.AutowrapMode.WordSmart;
        rewardText.AddChild(_rewardTitle);
        rewardText.AddChild(_rewardDescription);
        rewardRow.AddChild(rewardText);
        rewardPanel.AddChild(rewardRow);
        column.AddChild(rewardPanel);

        _notice = ThemeFactory.Label(size: 8, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(510, 12);
        column.AddChild(_notice);

        var actions = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actions.AddThemeConstantOverride("separation", 8);
        column.AddChild(actions);

        _activate = ThemeFactory.Button("");
        _activate.CustomMinimumSize = new Vector2(206, 24);
        _activate.Pressed += Activate;
        actions.AddChild(_activate);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(166, 24);
        _close.Pressed += () => CloseRequested?.Invoke();
        actions.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _rows[0].Action.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? StarlightChanged;
    public event Action<string>? StoryBeatRequested;

    public void RefreshText()
    {
        _title.Text = _locale.Tr(_pedestal.NameKey);
        _region.Text = _locale.Tr(_pedestal.RegionKey);
        _overall.Text = _session.Starlight.IsRewardUnlocked(_pedestalId)
            ? _locale.Tr("starlight.state.restored")
            : _locale.Tr(
                "starlight.state.progress",
                _session.CompletedStarlightNodeCount(_pedestalId),
                _pedestal.Nodes.Count
            );
        _close.Text = _locale.Tr("menu.back");
        var activation = _session.CheckActivateStarlightPedestal(
            _pedestalId,
            StarlightSpatialCatalog.ForPedestal(_pedestalId).Cell
        );
        _activate.Visible = _pedestal.RequiresManualActivation &&
            !_session.Starlight.IsRewardUnlocked(_pedestalId);
        _activate.Disabled = !activation.Succeeded;
        _activate.Text = _locale.Tr(
            activation.Succeeded
                ? "starlight.action.activate"
                : "starlight.action.activation_locked"
        );

        foreach (var row in _rows)
        {
            var progress = _session.StarlightNodeProgress(
                _pedestalId,
                row.Definition.Id
            );
            var complete = _session.IsStarlightNodeComplete(
                _pedestalId,
                row.Definition.Id
            );
            row.Title.Text = _locale.Tr(row.Definition.TitleKey);
            row.Description.Text = Description(row.Definition);
            row.Progress.Text = _locale.Tr(
                "starlight.node.progress",
                progress,
                row.Definition.RequiredCount
            );
            if (complete)
            {
                row.Action.Text = _locale.Tr(
                    "starlight.node.action.complete"
                );
                row.Action.Disabled = true;
                continue;
            }

            if (row.Definition.SourceKind !=
                StarlightNodeSourceKind.Inventory)
            {
                row.Action.Disabled = true;
                row.Action.Text = _locale.Tr(
                    row.Definition.SourceKind ==
                        StarlightNodeSourceKind.FestivalResults
                            ? "starlight.node.action.festival_missing"
                            : "starlight.node.action.milestone_missing"
                );
                continue;
            }

            row.Action.Disabled = false;
            row.Action.Text = _session.CanContributeToStarlightNode(
                _pedestalId,
                row.Definition.Id
            )
                ? _locale.Tr("starlight.node.action.contribute")
                : _locale.Tr("starlight.node.action.missing");
        }

        _rewardTitle.Text = _session.Starlight.IsRewardUnlocked(_pedestalId)
            ? _locale.Tr("starlight.reward.unlocked")
            : _locale.Tr(_pedestal.RewardTitleKey);
        _rewardDescription.Text = _locale.Tr(
            _pedestal.RewardDescriptionKey,
            RewardDescriptionArgs()
        );
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private Control BuildNode(StarlightNodeDefinition definition)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(510, 58)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#111f3be8"),
                new Color("#526e89"),
                1,
                5,
                4
            )
        );

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        panel.AddChild(row);

        var text = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        text.AddThemeConstantOverride("separation", 1);
        var title = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        var description = ThemeFactory.Label(size: 8);
        description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        description.CustomMinimumSize = new Vector2(340, 28);
        text.AddChild(title);
        text.AddChild(description);
        row.AddChild(text);

        var actionColumn = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(132, 50)
        };
        actionColumn.AddThemeConstantOverride("separation", 2);
        var progress = ThemeFactory.Label(size: 8, color: ThemeFactory.Mint);
        progress.HorizontalAlignment = HorizontalAlignment.Center;
        var action = ThemeFactory.Button("");
        action.CustomMinimumSize = new Vector2(132, 27);
        action.Pressed += () => Contribute(definition.Id);
        actionColumn.AddChild(progress);
        actionColumn.AddChild(action);
        row.AddChild(actionColumn);

        _rows.Add(new NodeRow
        {
            Definition = definition,
            Title = title,
            Description = description,
            Progress = progress,
            Action = action
        });
        return panel;
    }

    private string Description(StarlightNodeDefinition definition)
    {
        if (definition.SourceKind ==
            StarlightNodeSourceKind.FestivalResults)
        {
            return _locale.Tr(definition.DescriptionKey);
        }

        if (definition.Id is DataCatalog.WoodlandHarvestNodeId or
            DataCatalog.HomesteadHarvestNodeId)
        {
            return _locale.Tr(definition.DescriptionKey);
        }

        var names = definition.Options
            .Select(option => _locale.Tr(
                DataCatalog.Item(option.ItemId).NameKey
            ))
            .Cast<object>()
            .ToArray();
        return _locale.Tr(definition.DescriptionKey, names);
    }

    private void Contribute(string nodeId)
    {
        var result = _session.ContributeToStarlightNode(
            _pedestalId,
            nodeId
        );
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            StarlightChanged?.Invoke();
            if (result.Activated)
            {
                StoryBeatRequested?.Invoke(_pedestalId);
            }
        }
        RefreshText();
    }

    private void Activate()
    {
        var result = _session.ActivateStarlightPedestal(
            _pedestalId,
            StarlightSpatialCatalog.ForPedestal(_pedestalId).Cell
        );
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            StarlightChanged?.Invoke();
            StoryBeatRequested?.Invoke(_pedestalId);
        }
        RefreshText();
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

    private static Texture2D NodeSealTexture(string pedestalId) =>
        pedestalId switch
        {
            DataCatalog.HomesteadStarlightId =>
                HomesteadStarlightArt.NodeSealTexture(),
            DataCatalog.MeadowStarlightId =>
                MeadowStarlightArt.NodeSealTexture(),
            DataCatalog.MoonwaterStarlightId =>
                MoonwaterStarlightArt.NodeSealTexture(),
            DataCatalog.CrystalValeStarlightId =>
                CrystalValeStarlightArt.NodeSealTexture(),
            DataCatalog.StarfallRuinsStarlightId =>
                StarfallRuinsArt.RuinsStarlightNodeTexture(),
            _ => GeneratedArt.CreateStarlightNodeSealIcon()
        };

    private static Texture2D RewardTexture(string pedestalId) =>
        pedestalId switch
        {
            DataCatalog.HomesteadStarlightId =>
                HomesteadStarlightArt.IrrigationBlessingTexture(),
            DataCatalog.MeadowStarlightId =>
                MeadowStarlightArt.PollinationBlessingTexture(),
            DataCatalog.MoonwaterStarlightId =>
                MoonwaterStarlightArt.TideBlessingTexture(),
            DataCatalog.CrystalValeStarlightId =>
                CrystalValeStarlightArt.PassageRewardTexture(),
            DataCatalog.StarfallRuinsStarlightId =>
                StarfallRuinsArt.SixfoldConvergenceTexture(),
            _ => GeneratedArt.CreateWoodlandRenewalIcon()
        };

    private object[] RewardDescriptionArgs() => _pedestalId switch
    {
        DataCatalog.WoodlandStarlightId =>
        [
            WorldResourceSystem.TreeRespawnDays,
            WorldResourceSystem.RenewedWoodlandTreeRespawnDays
        ],
        DataCatalog.MeadowStarlightId =>
        [
            OrchardSystem.BeehivePollinationRange,
            OrchardSystem.FarReachingBeehivePollinationRange
        ],
        _ => []
    };
}
