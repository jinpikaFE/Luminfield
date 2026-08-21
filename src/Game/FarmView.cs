using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class FarmView : Node2D
{
    public static readonly GridPosition MiraCell = FarmLayout.MiraCell;
    public static readonly GridPosition CottageDoorCell = FarmLayout.CottageDoorCell;
    public static readonly GridPosition ShopCell = FarmLayout.ShopCell;
    public static readonly GridPosition ProcessorCell = FarmLayout.ProcessorCell;
    public static readonly GridPosition ShippingCell = FarmLayout.ShippingCell;
    public static readonly GridPosition CommissionBoardCell =
        FarmLayout.CommissionBoardCell;
    public static readonly GridPosition StarlightMailboxCell =
        FarmLayout.StarlightMailboxCell;
    public static readonly GridPosition HomesteadWorkbenchCell =
        FarmLayout.HomesteadWorkbenchCell;
    public static readonly GridPosition GreenhouseDoorCell =
        FarmLayout.GreenhouseDoorCell;
    public static readonly GridPosition StarfeatherCoopDoorCell =
        FarmLayout.StarfeatherCoopDoorCell;
    public static readonly GridPosition MoonfleeceBarnDoorCell =
        FarmLayout.MoonfleeceBarnDoorCell;
    public static readonly GridPosition HomesteadStarlightCell =
        FarmLayout.HomesteadStarlightCell;
    public static readonly GridPosition StarGateCell = FarmLayout.StarGateCell;
    public static readonly GridPosition WoodlandStarlightCell =
        WorldDefinition.WoodlandStarlightCell;
    public static readonly GridPosition MeadowStarlightCell =
        WorldDefinition.MeadowStarlightCell;
    public static readonly GridPosition MoonlitArchiveDoorCell =
        VillageCatalog.MoonlitArchiveDoorCell;
    public static readonly GridPosition MoonstoneWorkshopDoorCell =
        VillageCatalog.MoonstoneWorkshopDoorCell;
    public static readonly GridPosition StarweaverTeaHouseDoorCell =
        VillageCatalog.StarweaverTeaHouseDoorCell;
    public static readonly GridPosition TwilightEmporiumDoorCell =
        VillageCatalog.TwilightEmporiumDoorCell;
    public static readonly GridPosition StarlightPostDoorCell =
        VillageCatalog.StarlightPostDoorCell;
    public static readonly GridPosition StarfallWatchDoorCell =
        VillageCatalog.StarfallWatchDoorCell;
    public static readonly GridPosition StarharvestMarketEntryCell =
        StarharvestMarketLayout.WorldEntryCell;

    private readonly GameSession _session;
    private readonly TileMapLayer _baseLayer;
    private readonly TileMapLayer _soilLayer;
    private readonly TileMapLayer _cropLayer;
    private readonly TileMapLayer _propLayer;
    private readonly CanvasModulate _canvasModulate;
    private readonly TargetCursor _cursor;
    private readonly PlayerController _player;
    private readonly WorldChunkStreamer _worldStreamer;
    private readonly Sprite2D _shippingBin;
    private readonly Sprite2D _commissionBoard;
    private readonly Sprite2D _starlightMailbox;
    private readonly Dictionary<string, Sprite2D> _processorSprites =
        new(StringComparer.Ordinal);
    private readonly Node2D _storageChestLayer;
    private readonly Node2D _farmObjectLayer;
    private readonly Node2D _crabPotLayer;
    private GridPosition? _openStorageChest;
    private bool _commissionBoardOpen;
    private double _toolRepeatTimer;

    public FarmView(GameSession session, LocaleService locale)
    {
        _session = session;
        YSortEnabled = true;

        var environmentTiles = TilePaletteFactory.CreateEnvironment();
        _baseLayer = Layer("Base", environmentTiles, -20);
        _soilLayer = Layer("Soil", environmentTiles, -10);
        _cropLayer = Layer("Crops", TilePaletteFactory.CreateCrops(), 0);
        _propLayer = Layer("Props", environmentTiles, 5);
        _baseLayer.Visible = false;
        _soilLayer.Visible = false;
        _cropLayer.Visible = false;
        _propLayer.Visible = false;

        _worldStreamer = new WorldChunkStreamer(session);
        _worldStreamer.RegionEntered += key => RegionEntered?.Invoke(key);
        AddChild(_worldStreamer);
        AddChild(new FarmBackdrop(session));
        AddChild(new SouthernWorldGate());
        _canvasModulate = new CanvasModulate { Color = Colors.White };
        AddChild(_canvasModulate);
        AddChild(new FarmWeatherOverlay(session));

        AddChild(new FarmSoilStateLayer(session.Farm));
        AddChild(new GeneratedCropLayer(session.Farm));
        AddChild(new CropGlowLayer(session.Farm));
        AddChild(new GeneratedOrchardLayer(session));
        AddChild(new HomesteadWorkshopVisual(session)
        {
            Position = CellCenter(HomesteadWorkbenchCell) +
                new Vector2(0, 8),
            ZIndex = 7
        });
        AddChild(new HomesteadGreenhouseVisual(session)
        {
            Position = CellCenter(GreenhouseDoorCell) +
                new Vector2(0, 8),
            ZIndex = 7
        });
        AddChild(new HomesteadStarfeatherCoopVisual(session)
        {
            Position = CellCenter(FarmLayout.StarfeatherCoopReturnCell) +
                new Vector2(0, 8),
            ZIndex = 7
        });
        AddChild(new StarfeatherChickenVisual(
            session,
            worldProjection: true
        ));
        AddChild(new HomesteadMoonfleeceBarnVisual(session)
        {
            Position = CellCenter(FarmLayout.MoonfleeceBarnDoorCell) +
                new Vector2(0, 8),
            ZIndex = 7
        });
        AddChild(new MoonfleeceSheepVisual(
            session,
            worldProjection: true
        ));
        AddChild(new DewhornVisual(session, worldProjection: true));
        AddChild(new HomesteadStarlightVisual(session)
        {
            Position = CellCenter(HomesteadStarlightCell) +
                new Vector2(0, 7),
            ZIndex = 7
        });
        AddChild(new StarGateVisual(session)
        {
            Position = CellCenter(StarGateCell) + new Vector2(0, -28),
            ZIndex = 7
        });
        AddChild(new MoteField(new Rect2(0, 0, FarmSystem.MapWidth * 16, FarmSystem.MapHeight * 16)));

        var mira = GeneratedArt.CreateMiraSprite();
        mira.Name = "Mira";
        mira.Position = CellCenter(MiraCell);
        mira.ZIndex = 8;
        mira.AddChild(new ActorShadow
        {
            Position = new Vector2(0, 9),
            ZIndex = -1,
        });
        AddChild(mira);
        AddChild(new MiraBeacon(session)
        {
            Position = CellCenter(MiraCell),
            ZIndex = 24,
        });

        var marketStall = GeneratedArt.CreateMarketStallSprite();
        marketStall.Name = "TwilightMarket";
        marketStall.Position = CellCenter(ShopCell) + new Vector2(0, 8);
        marketStall.ZIndex = 7;
        AddChild(marketStall);

        foreach (var definition in ProcessorCatalog.Machines.Values)
        {
            var machine = GeneratedArt.CreateProcessorMachineSprite(
                definition.Id,
                session.Processor.Machine(definition.Id)
            );
            machine.Name = definition.Id;
            machine.Position = CellCenter(definition.Position) + new Vector2(0, 8);
            machine.ZIndex = 7;
            machine.SetMeta("entity_id", definition.Id);
            machine.AddChild(new ActorShadow
            {
                Position = new Vector2(0, 1),
                ZIndex = -1
            });
            _processorSprites[definition.Id] = machine;
            AddChild(machine);
        }

        _shippingBin = GeneratedArt.CreateShippingBinSprite(
            session.Shipping.PendingItemCount > 0
        );
        _shippingBin.Name = "StarShippingBin";
        _shippingBin.Position = CellCenter(ShippingCell) + new Vector2(0, 8);
        _shippingBin.ZIndex = 7;
        AddChild(_shippingBin);

        _commissionBoard = GeneratedArt.CreateCommissionBoardSprite(
            HasActiveCommission(session)
        );
        _commissionBoard.Name = "DailyCommissionBoard";
        _commissionBoard.Position =
            CellCenter(CommissionBoardCell) + new Vector2(0, 8);
        _commissionBoard.ZIndex = 7;
        _commissionBoard.AddChild(new ActorShadow
        {
            Position = new Vector2(0, 1),
            ZIndex = -1
        });
        AddChild(_commissionBoard);

        _starlightMailbox = GeneratedArt.CreateStarlightMailboxSprite(
            session.Mail.HasUnread
        );
        _starlightMailbox.Name = MailCatalog.MailboxId;
        _starlightMailbox.Position =
            CellCenter(StarlightMailboxCell) + new Vector2(0, 8);
        _starlightMailbox.ZIndex = 7;
        _starlightMailbox.SetMeta("entity_id", MailCatalog.MailboxId);
        _starlightMailbox.AddChild(new ActorShadow
        {
            Position = new Vector2(0, 1),
            ZIndex = -1
        });
        AddChild(_starlightMailbox);

        _storageChestLayer = new Node2D
        {
            Name = "PlacedStorageChests",
            ZIndex = 7,
            YSortEnabled = true
        };
        AddChild(_storageChestLayer);
        RebuildStorageChests();

        _farmObjectLayer = new Node2D
        {
            Name = "PlacedFarmObjects",
            ZIndex = 6,
            YSortEnabled = true
        };
        AddChild(_farmObjectLayer);
        RebuildFarmObjects();

        _crabPotLayer = new Node2D
        {
            Name = "MoonreedCrabPots",
            ZIndex = 6,
            YSortEnabled = true
        };
        AddChild(_crabPotLayer);
        RebuildCrabPots();

        _player = new PlayerController(
            CanOccupy,
            () => session.PlayerMovementMultiplier
        )
        {
            Name = "Player",
            Position = new Vector2(session.PlayerX, session.PlayerY),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
        {
            _session.SetPlayerState(position.X, position.Y, false);
            _worldStreamer.UpdatePlayer(position);
        };
        AddChild(_player);
        AddChild(new StationBeacon(() => _player.CurrentCell, ShopCell, ThemeFactory.Gold)
        {
            Position = CellCenter(ShopCell) + new Vector2(0, 8),
            ZIndex = 26
        });
        foreach (var definition in ProcessorCatalog.Machines.Values)
        {
            AddChild(new StationBeacon(
                () => _player.CurrentCell,
                definition.Position,
                ThemeFactory.Mint
            )
            {
                Position = CellCenter(definition.Position) + new Vector2(0, 8),
                ZIndex = 26
            });
        }
        AddChild(new StationBeacon(() => _player.CurrentCell, ShippingCell, ThemeFactory.Gold)
        {
            Position = CellCenter(ShippingCell) + new Vector2(0, 8),
            ZIndex = 26
        });
        AddChild(new CottageEntranceBeacon(() => _player.CurrentCell)
        {
            Position = CellCenter(CottageDoorCell),
            ZIndex = 30
        });
        AddChild(new VillageEntranceBeacon(
            () => _player.CurrentCell,
            MoonlitArchiveDoorCell
        )
        {
            Position = CellCenter(MoonlitArchiveDoorCell),
            ZIndex = 30
        });
        AddChild(new VillageEntranceBeacon(
            () => _player.CurrentCell,
            MoonstoneWorkshopDoorCell
        )
        {
            Position = CellCenter(MoonstoneWorkshopDoorCell),
            ZIndex = 30
        });
        AddChild(new VillageEntranceBeacon(
            () => _player.CurrentCell,
            StarweaverTeaHouseDoorCell
        )
        {
            Position = CellCenter(StarweaverTeaHouseDoorCell),
            ZIndex = 30
        });

        var camera = new Camera2D
        {
            Zoom = Vector2.One,
            PositionSmoothingEnabled = false,
            LimitLeft = 0,
            LimitTop = 0,
            LimitRight = WorldDefinition.Width * 16,
            LimitBottom = WorldDefinition.Height * 16
        };
        _player.AddChild(camera);
        _worldStreamer.UpdatePlayer(_player.Position);

        _cursor = new TargetCursor(ResolveTargetPreview, locale);
        _cursor.ZIndex = 20;
        var cursorLayer = new CanvasLayer
        {
            Layer = 60,
            FollowViewportEnabled = true
        };
        cursorLayer.AddChild(_cursor);
        AddChild(cursorLayer);

        BuildBaseMap();
        RefreshAllFarmTiles();
        session.Farm.TileChanged += RefreshFarmTile;
        session.Clock.TimeChanged += UpdateLighting;
        session.Weather.Changed += UpdateLighting;
        session.Shipping.Changed += RefreshShippingBin;
        session.Storage.Changed += RefreshStorageChests;
        session.FarmObjects.Changed += RefreshFarmObjects;
        session.CrabPots.Changed += RebuildCrabPots;
        session.Commission.Changed += RefreshCommissionBoard;
        session.WeeklyCommission.Changed += RefreshCommissionBoard;
        session.Mail.Changed += RefreshStarlightMailbox;
        session.Processor.Changed += RefreshProcessorMachines;
        UpdateLighting();
    }

    public bool ControlsEnabled
    {
        get => _player.ControlsEnabled;
        set
        {
            _player.ControlsEnabled = value;
            _cursor.Visible = value;
        }
    }

    public event Action<GridPosition>? UseRequested;
    public event Action? MiraRequested;
    public event Action? EnterCottageRequested;
    public event Action? EnterGreenhouseRequested;
    public event Action? EnterStarfeatherCoopRequested;
    public event Action? EnterMoonfleeceBarnRequested;
    public event Action? EnterArchiveRequested;
    public event Action? EnterWorkshopRequested;
    public event Action? EnterTeaHouseRequested;
    public event Action? EnterTwilightEmporiumRequested;
    public event Action? EnterStarlightPostRequested;
    public event Action? EnterStarfallWatchRequested;
    public event Action? EnterStarharvestMarketRequested;
    public event Action? EnterGleamrisePlantingFestivalRequested;
    public event Action? EnterLongnightLanternFeastRequested;
    public event Action? EnterFireflyTideRequested;
    public event Action? EnterCrystalGrottoRequested;
    public event Action? EnterStarfallRuinsRequested;
    public event Action? ShopRequested;
    public event Action<string>? ProcessorRequested;
    public event Action? ShippingRequested;
    public event Action? CommissionRequested;
    public event Action? MailRequested;
    public event Action<string>? StarlightRequested;
    public event Action<GridPosition>? VillagerRequested;
    public event Action<GridPosition>? StorageRequested;
    public event Action<GridPosition>? HomesteadWorkbenchRequested;
    public event Action<string>? NoticeRequested;
    public event Action<string>? RegionEntered;
    public event Action? StepRequested
    {
        add => _player.Stepped += value;
        remove => _player.Stepped -= value;
    }

    public Vector2 PlayerPosition => _player.Position;

    private TargetPreview ResolveTargetPreview()
    {
        var target = _player.TargetCell;
        var player = _player.CurrentCell;
        if (FestivalCatalog.FestivalOnDay(_session.Clock.Day) is { } festival &&
            FestivalSpatialCatalog.TryByFestivalId(
                festival.Id,
                out var festivalSpatial
            ) && (target == festivalSpatial.WorldEntryCell ||
             IsAdjacent(player, festivalSpatial.WorldEntryCell)))
        {
            return _session.PreviewSelectedTarget(
                festivalSpatial.WorldEntryCell
            );
        }

        if (target == StarfallRuinsTrialLayout.WorldEntryCell ||
            IsAdjacent(player, StarfallRuinsTrialLayout.WorldEntryCell))
        {
            return _session.PreviewSelectedTarget(
                StarfallRuinsTrialLayout.WorldEntryCell
            );
        }

        if (target == CrystalGrottoSurveyLayout.WorldEntryCell ||
            IsAdjacent(player, CrystalGrottoSurveyLayout.WorldEntryCell))
        {
            return _session.PreviewSelectedTarget(
                CrystalGrottoSurveyLayout.WorldEntryCell
            );
        }

        // A forage node is an exact real-world target. Resolve it before
        // nearby doors, animals, villagers, or facilities can absorb the
        // interaction merely because they are adjacent to the player.
        if (_session.Forage.SpawnAt(target) is not null)
        {
            return _session.PreviewSelectedTarget(target);
        }

        if (target == GreenhouseDoorCell ||
            IsAdjacent(player, GreenhouseDoorCell))
        {
            return _session.PreviewSelectedTarget(GreenhouseDoorCell);
        }

        // Facing a grazing animal from its building step must select the
        // animal itself; adjacency to the door is only the fallback target.
        var exactAnimal = _session.VisibleAnimalProjections
            .FirstOrDefault(projection => projection.Cell == target);
        if (exactAnimal is not null)
        {
            return _session.PreviewSelectedTarget(exactAnimal.Cell);
        }

        var animalDoor = AnimalBuildingSpatialCatalog.Definitions
            .FirstOrDefault(definition =>
                target == definition.WorldDoorCell ||
                IsAdjacent(player, definition.WorldDoorCell)
            );
        if (animalDoor is not null)
        {
            return _session.PreviewSelectedTarget(
                animalDoor.WorldDoorCell
            );
        }

        if (target == MoonlitArchiveDoorCell)
        {
            return _session.PreviewSelectedTarget(
                MoonlitArchiveDoorCell
            );
        }

        if (target == MoonstoneWorkshopDoorCell)
        {
            return _session.PreviewSelectedTarget(
                MoonstoneWorkshopDoorCell
            );
        }

        if (target == StarweaverTeaHouseDoorCell)
        {
            return _session.PreviewSelectedTarget(
                StarweaverTeaHouseDoorCell
            );
        }

        if (target == TwilightEmporiumDoorCell)
        {
            return _session.PreviewSelectedTarget(
                TwilightEmporiumDoorCell
            );
        }

        if (target == StarlightPostDoorCell)
        {
            return _session.PreviewSelectedTarget(
                StarlightPostDoorCell
            );
        }

        if (target == StarfallWatchDoorCell)
        {
            return _session.PreviewSelectedTarget(
                StarfallWatchDoorCell
            );
        }

        var nearbyStarlight = StarlightSpatialCatalog.Pedestals
            .FirstOrDefault(definition =>
                target == definition.Cell ||
                IsAdjacent(player, definition.Cell)
            );
        if (nearbyStarlight is not null)
        {
            return _session.PreviewSelectedTarget(nearbyStarlight.Cell);
        }

        if (target == StarGateCell || IsAdjacent(player, StarGateCell))
        {
            return _session.PreviewSelectedTarget(StarGateCell);
        }

        var nearbyAnimal = _session.VisibleAnimalProjections
            .FirstOrDefault(projection =>
                target == projection.Cell ||
                IsAdjacent(player, projection.Cell)
            );
        if (nearbyAnimal is not null)
        {
            return _session.PreviewSelectedTarget(nearbyAnimal.Cell);
        }

        var villager = ResolveVillageNpcTarget(target, player);
        if (villager is not null)
        {
            return _session.PreviewSelectedTarget(villager.Position);
        }

        if (FarmLayout.IsCommissionBoardCell(target) ||
            IsNearCommissionBoard(player))
        {
            return _session.PreviewSelectedTarget(CommissionBoardCell);
        }

        if (target == StarlightMailboxCell ||
            IsAdjacent(player, StarlightMailboxCell))
        {
            return _session.PreviewSelectedTarget(StarlightMailboxCell);
        }

        if (target == HomesteadWorkbenchCell ||
            IsAdjacent(player, HomesteadWorkbenchCell))
        {
            return _session.PreviewSelectedTarget(HomesteadWorkbenchCell);
        }

        var storageTarget = ResolveStorageTarget(target, player);
        if (storageTarget is { } chest)
        {
            return PreviewHandInteraction(
                chest,
                TargetPreviewKind.StorageChest,
                "target.action.open_storage"
            );
        }

        var orchardTarget = ResolveOrchardTarget(target, player);
        if (orchardTarget is { } orchardCell)
        {
            return _session.PreviewSelectedTarget(orchardCell);
        }

        if (target == MiraCell || IsAdjacent(player, MiraCell))
        {
            return PreviewHandInteraction(
                MiraCell,
                TargetPreviewKind.Character,
                "target.action.talk"
            );
        }

        if (target == CottageDoorCell || IsAdjacent(player, CottageDoorCell))
        {
            return PreviewHandInteraction(
                CottageDoorCell,
                TargetPreviewKind.Door,
                "target.action.enter"
            );
        }

        if (IsAdjacent(player, MoonlitArchiveDoorCell))
        {
            return _session.PreviewSelectedTarget(
                MoonlitArchiveDoorCell
            );
        }

        if (IsAdjacent(player, MoonstoneWorkshopDoorCell))
        {
            return _session.PreviewSelectedTarget(
                MoonstoneWorkshopDoorCell
            );
        }

        if (IsAdjacent(player, StarweaverTeaHouseDoorCell))
        {
            return _session.PreviewSelectedTarget(
                StarweaverTeaHouseDoorCell
            );
        }

        if (IsAdjacent(player, TwilightEmporiumDoorCell))
        {
            return _session.PreviewSelectedTarget(
                TwilightEmporiumDoorCell
            );
        }

        if (IsAdjacent(player, StarlightPostDoorCell))
        {
            return _session.PreviewSelectedTarget(
                StarlightPostDoorCell
            );
        }

        if (IsAdjacent(player, StarfallWatchDoorCell))
        {
            return _session.PreviewSelectedTarget(
                StarfallWatchDoorCell
            );
        }

        if (target == ShopCell || IsAdjacent(player, ShopCell))
        {
            return PreviewHandInteraction(
                ShopCell,
                TargetPreviewKind.Station,
                "target.action.trade"
            );
        }

        var processorTarget = ResolveProcessorTarget(target, player);
        if (processorTarget is not null)
        {
            return _session.PreviewProcessorMachine(processorTarget.Id);
        }

        if (target == ShippingCell || IsAdjacent(player, ShippingCell))
        {
            return PreviewHandInteraction(
                ShippingCell,
                TargetPreviewKind.Station,
                "target.action.ship"
            );
        }

        return _session.PreviewSelectedTarget(target);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!ControlsEnabled || !@event.IsActionPressed(InputSetup.Interact))
        {
            return;
        }

        _toolRepeatTimer = 0.28;

        var target = _player.TargetCell;
        if (FestivalCatalog.FestivalOnDay(_session.Clock.Day) is { } festival &&
            FestivalSpatialCatalog.TryByFestivalId(
                festival.Id,
                out var festivalSpatial
            ) && (target == festivalSpatial.WorldEntryCell ||
             IsAdjacent(_player.CurrentCell, festivalSpatial.WorldEntryCell)))
        {
            if (festival.Id == FestivalCatalog.GleamrisePlantingFestivalId)
            {
                EnterGleamrisePlantingFestivalRequested?.Invoke();
            }
            else if (festival.Id ==
                FestivalCatalog.LongnightLanternFeastFestivalId)
            {
                EnterLongnightLanternFeastRequested?.Invoke();
            }
            else if (festival.Id == FestivalCatalog.FireflyTideFestivalId)
            {
                EnterFireflyTideRequested?.Invoke();
            }
            else
            {
                EnterStarharvestMarketRequested?.Invoke();
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_session.Forage.SpawnAt(target) is not null)
        {
            UseRequested?.Invoke(target);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (target == StarfallRuinsTrialLayout.WorldEntryCell ||
            IsAdjacent(
                _player.CurrentCell,
                StarfallRuinsTrialLayout.WorldEntryCell
            ))
        {
            EnterStarfallRuinsRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (target == CrystalGrottoSurveyLayout.WorldEntryCell ||
            IsAdjacent(
                _player.CurrentCell,
                CrystalGrottoSurveyLayout.WorldEntryCell
            ))
        {
            EnterCrystalGrottoRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (target == GreenhouseDoorCell ||
            IsAdjacent(_player.CurrentCell, GreenhouseDoorCell))
        {
            EnterGreenhouseRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        var exactAnimal = _session.VisibleAnimalProjections
            .FirstOrDefault(projection => projection.Cell == target);
        if (exactAnimal is not null)
        {
            UseRequested?.Invoke(exactAnimal.Cell);
            GetViewport().SetInputAsHandled();
            return;
        }

        var animalDoor = AnimalBuildingSpatialCatalog.Definitions
            .FirstOrDefault(definition =>
                target == definition.WorldDoorCell ||
                IsAdjacent(_player.CurrentCell, definition.WorldDoorCell)
            );
        if (animalDoor is not null)
        {
            if (animalDoor.BuildingId == AnimalCatalog.MoonfleeceBarnId)
            {
                EnterMoonfleeceBarnRequested?.Invoke();
            }
            else
            {
                EnterStarfeatherCoopRequested?.Invoke();
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (target == MoonlitArchiveDoorCell)
        {
            EnterArchiveRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (target == MoonstoneWorkshopDoorCell)
        {
            EnterWorkshopRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (target == StarweaverTeaHouseDoorCell)
        {
            EnterTeaHouseRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (target == TwilightEmporiumDoorCell)
        {
            EnterTwilightEmporiumRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (target == StarlightPostDoorCell)
        {
            EnterStarlightPostRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (target == StarfallWatchDoorCell)
        {
            EnterStarfallWatchRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        var nearbyStarlight = StarlightSpatialCatalog.Pedestals
            .FirstOrDefault(definition =>
                target == definition.Cell ||
                IsAdjacent(_player.CurrentCell, definition.Cell)
            );
        if (nearbyStarlight is not null)
        {
            RequestStarlight(
                nearbyStarlight.PedestalId,
                nearbyStarlight.Cell
            );
            GetViewport().SetInputAsHandled();
            return;
        }

        if (target == StarGateCell ||
            IsAdjacent(_player.CurrentCell, StarGateCell))
        {
            UseRequested?.Invoke(StarGateCell);
            GetViewport().SetInputAsHandled();
            return;
        }

        var villager = ResolveVillageNpcTarget(
            target,
            _player.CurrentCell
        );
        var storageTarget = ResolveStorageTarget(target, _player.CurrentCell);
        var orchardTarget = ResolveOrchardTarget(target, _player.CurrentCell);
        if (villager is not null)
        {
            VillagerRequested?.Invoke(villager.Position);
        }
        else if (FarmLayout.IsCommissionBoardCell(target) ||
            IsNearCommissionBoard(_player.CurrentCell))
        {
            RequestCommissionBoard();
        }
        else if (target == StarlightMailboxCell ||
            IsAdjacent(_player.CurrentCell, StarlightMailboxCell))
        {
            RequestStarlightMail();
        }
        else if (target == HomesteadWorkbenchCell ||
            IsAdjacent(_player.CurrentCell, HomesteadWorkbenchCell))
        {
            HomesteadWorkbenchRequested?.Invoke(
                HomesteadWorkbenchCell
            );
        }
        else if (_session.VisibleAnimalProjections.FirstOrDefault(projection =>
                     target == projection.Cell ||
                     IsAdjacent(_player.CurrentCell, projection.Cell)
                 ) is { } animalProjection)
        {
            UseRequested?.Invoke(animalProjection.Cell);
        }
        else if (storageTarget is { } chest)
        {
            RequestHandInteraction(() => StorageRequested?.Invoke(chest));
        }
        else if (orchardTarget is { } orchardCell)
        {
            UseRequested?.Invoke(orchardCell);
        }
        else if (target == MiraCell || IsAdjacent(_player.CurrentCell, MiraCell))
        {
            RequestHandInteraction(MiraRequested);
        }
        else if (target == CottageDoorCell || IsAdjacent(_player.CurrentCell, CottageDoorCell))
        {
            RequestHandInteraction(EnterCottageRequested);
        }
        else if (IsAdjacent(
                     _player.CurrentCell,
                     MoonlitArchiveDoorCell
                 ))
        {
            EnterArchiveRequested?.Invoke();
        }
        else if (IsAdjacent(
                     _player.CurrentCell,
                     MoonstoneWorkshopDoorCell
                 ))
        {
            EnterWorkshopRequested?.Invoke();
        }
        else if (IsAdjacent(
                     _player.CurrentCell,
                     StarweaverTeaHouseDoorCell
                 ))
        {
            EnterTeaHouseRequested?.Invoke();
        }
        else if (IsAdjacent(
                     _player.CurrentCell,
                     TwilightEmporiumDoorCell
                 ))
        {
            EnterTwilightEmporiumRequested?.Invoke();
        }
        else if (IsAdjacent(
                     _player.CurrentCell,
                     StarlightPostDoorCell
                 ))
        {
            EnterStarlightPostRequested?.Invoke();
        }
        else if (IsAdjacent(
                     _player.CurrentCell,
                     StarfallWatchDoorCell
                 ))
        {
            EnterStarfallWatchRequested?.Invoke();
        }
        else if (target == ShopCell || IsAdjacent(_player.CurrentCell, ShopCell))
        {
            RequestHandInteraction(ShopRequested);
        }
        else if (ResolveProcessorTarget(target, _player.CurrentCell) is { } processor)
        {
            RequestProcessor(processor.Id);
        }
        else if (target == ShippingCell || IsAdjacent(_player.CurrentCell, ShippingCell))
        {
            RequestHandInteraction(ShippingRequested);
        }
        else
        {
            UseRequested?.Invoke(target);
        }

        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!ControlsEnabled ||
            !AccessibilityRuntime.Settings.HoldToRepeatTools ||
            !Input.IsActionPressed(InputSetup.Interact) ||
            !IsRepeatableTool(_session.Inventory.Selected.ItemId))
        {
            _toolRepeatTimer = 0;
            return;
        }

        _toolRepeatTimer -= delta;
        if (_toolRepeatTimer > 0)
        {
            return;
        }

        _toolRepeatTimer = 0.22;
        UseRequested?.Invoke(_player.TargetCell);
    }

    private static bool IsRepeatableTool(string itemId) =>
        itemId is DataCatalog.ShovelId or
            DataCatalog.MacheteId or
            DataCatalog.WateringCanId or
            DataCatalog.BucketId;

    public void RefreshFarmTile(GridPosition position)
    {
        _soilLayer.EraseCell(new Vector2I(position.X, position.Y));
        _cropLayer.EraseCell(new Vector2I(position.X, position.Y));

        if (!_session.Farm.Tiles.TryGetValue(position, out var tile))
        {
            return;
        }

        var soilAtlas = tile.Watered
            ? TilePaletteFactory.WateredSoil
            : TilePaletteFactory.DrySoil;
        _soilLayer.SetCell(new Vector2I(position.X, position.Y), 0, new Vector2I(soilAtlas, 0));
        if (string.IsNullOrWhiteSpace(tile.CropId))
        {
            return;
        }
        // GeneratedCropLayer owns all crop silhouettes. Keeping the legacy tile
        // layer empty avoids showing a Starbud placeholder beneath expanded crops.
    }

    public override void _ExitTree()
    {
        _session.Farm.TileChanged -= RefreshFarmTile;
        _session.Clock.TimeChanged -= UpdateLighting;
        _session.Weather.Changed -= UpdateLighting;
        _session.Shipping.Changed -= RefreshShippingBin;
        _session.Storage.Changed -= RefreshStorageChests;
        _session.FarmObjects.Changed -= RefreshFarmObjects;
        _session.Commission.Changed -= RefreshCommissionBoard;
        _session.WeeklyCommission.Changed -= RefreshCommissionBoard;
        _session.Mail.Changed -= RefreshStarlightMailbox;
        _session.Processor.Changed -= RefreshProcessorMachines;
    }

    private TileMapLayer Layer(string name, TileSet tileSet, int zIndex)
    {
        var layer = new TileMapLayer
        {
            Name = name,
            TileSet = tileSet,
            ZIndex = zIndex,
            TextureFilter = TextureFilterEnum.Nearest
        };
        AddChild(layer);
        return layer;
    }

    private void BuildBaseMap()
    {
        for (var y = 0; y < FarmSystem.MapHeight; y++)
        {
            for (var x = 0; x < FarmSystem.MapWidth; x++)
            {
                var position = new GridPosition(x, y);
                var atlas = TilePaletteFactory.Grass;
                if (x >= 37 && y >= 20)
                {
                    atlas = TilePaletteFactory.Water;
                }
                else if ((x == 36 && y >= 19) || (y == 19 && x >= 36))
                {
                    atlas = TilePaletteFactory.PondBank;
                }
                else if (IsMoonstonePath(position))
                {
                    atlas = (x + y) % 2 == 0
                        ? TilePaletteFactory.MoonstonePath
                        : TilePaletteFactory.MoonstonePathAlt;
                }
                else if (FarmVisualLayout.IsPlantingBed(position))
                {
                    atlas = (x + y * 2) % 3 == 0
                        ? TilePaletteFactory.FarmFieldAlt
                        : TilePaletteFactory.FarmField;
                }
                else if ((x * 3 + y * 5) % 23 == 0)
                {
                    atlas = TilePaletteFactory.FlowerMeadow;
                }
                else if ((x + y * 2) % 7 == 0)
                {
                    atlas = TilePaletteFactory.GrassAlt;
                }

                _baseLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(atlas, 0));

                if (x is 0 or FarmSystem.MapWidth - 1 || y is 0 or FarmSystem.MapHeight - 1)
                {
                    _propLayer.SetCell(
                        new Vector2I(x, y),
                        0,
                        new Vector2I(TilePaletteFactory.Hedge, 0)
                    );
                }

            }
        }

        _propLayer.SetCell(
            new Vector2I(CottageDoorCell.X, CottageDoorCell.Y),
            0,
            new Vector2I(TilePaletteFactory.Doorstep, 0)
        );
    }

    private void RefreshAllFarmTiles()
    {
        foreach (var position in _session.Farm.Tiles.Keys)
        {
            RefreshFarmTile(position);
        }
    }

    private void UpdateLighting()
    {
        var progress = (_session.Clock.MinuteOfDay - GameClock.StartMinute) /
            (float)(GameClock.EndMinute - GameClock.StartMinute);
        var daylight = Mathf.Sin(progress * Mathf.Pi);
        var red = 0.78f + daylight * 0.17f;
        var green = 0.80f + daylight * 0.15f;
        var blue = 0.95f + daylight * 0.05f;
        if (_session.Weather.CurrentId == DataCatalog.RainWeatherId)
        {
            red *= 0.77f;
            green *= 0.84f;
            blue *= 0.94f;
        }
        else if (_session.Weather.CurrentId == DataCatalog.StardustWindWeatherId)
        {
            red *= 0.9f;
            green *= 0.94f;
            blue = Math.Min(1f, blue * 1.05f);
        }
        else if (_session.Weather.CurrentId ==
            DataCatalog.LongnightSnowWeatherId)
        {
            red *= 0.82f;
            green *= 0.9f;
            blue = Math.Min(1f, blue * 1.04f);
        }

        _canvasModulate.Color = new Color(red, green, blue);
    }

    private TargetPreview PreviewHandInteraction(
        GridPosition target,
        TargetPreviewKind kind,
        string actionKey
    ) => _session.Inventory.Selected.ItemId == DataCatalog.HandId
        ? TargetPreview.Available(target, kind, actionKey)
        : TargetPreview.NeedsTool(target, kind, "target.need.hand");

    private void RequestHandInteraction(Action? action)
    {
        if (_session.Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            NoticeRequested?.Invoke("notice.needs_hand");
            return;
        }

        action?.Invoke();
    }

    private void RequestProcessor(string machineId)
    {
        var preview = _session.PreviewProcessorMachine(machineId);
        if (!preview.IsAvailable)
        {
            NoticeRequested?.Invoke(preview.LabelKey);
            return;
        }

        ProcessorRequested?.Invoke(machineId);
    }

    private void RefreshShippingBin()
    {
        GeneratedArt.SetShippingBinState(
            _shippingBin,
            _session.Shipping.PendingItemCount > 0
        );
    }

    private void RefreshProcessorMachines()
    {
        foreach (var pair in _processorSprites)
        {
            GeneratedArt.SetProcessorMachineState(
                pair.Value,
                pair.Key,
                _session.Processor.Machine(pair.Key)
            );
        }
    }

    public void SetCommissionBoardOpen(bool open)
    {
        _commissionBoardOpen = open;
        RefreshCommissionBoard();
    }

    private void RefreshCommissionBoard()
    {
        var active = _commissionBoardOpen ||
            HasActiveCommission(_session);
        GeneratedArt.SetCommissionBoardState(_commissionBoard, active);
    }

    private static bool HasActiveCommission(GameSession session) =>
        (session.Commission.Accepted && !session.Commission.Claimed) ||
        (session.WeeklyCommission.Accepted &&
            !session.WeeklyCommission.Claimed);

    private void RequestCommissionBoard()
    {
        var result = _session.UseSelected(CommissionBoardCell);
        if (!result.Succeeded)
        {
            NoticeRequested?.Invoke(result.MessageKey);
            return;
        }

        CommissionRequested?.Invoke();
    }

    private void RefreshStarlightMailbox()
    {
        GeneratedArt.SetStarlightMailboxState(
            _starlightMailbox,
            _session.Mail.HasUnread
        );
    }

    private void RequestStarlightMail()
    {
        var result = _session.UseSelected(StarlightMailboxCell);
        if (!result.Succeeded)
        {
            NoticeRequested?.Invoke(result.MessageKey);
            return;
        }

        MailRequested?.Invoke();
    }

    private void RequestStarlight(
        string pedestalId,
        GridPosition pedestalCell
    )
    {
        var result = _session.UseSelected(pedestalCell);
        if (!result.Succeeded)
        {
            NoticeRequested?.Invoke(result.MessageKey);
            return;
        }

        StarlightRequested?.Invoke(pedestalId);
    }

    public void SetStorageChestOpen(GridPosition? position)
    {
        _openStorageChest = position;
        RebuildStorageChests();
    }

    private void RefreshStorageChests(GridPosition position)
    {
        _ = position;
        RebuildStorageChests();
    }

    private void RebuildStorageChests()
    {
        foreach (var child in _storageChestLayer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var pair in _session.Storage.Chests)
        {
            var sprite = GeneratedArt.CreateStarwovenChestSprite(
                _openStorageChest == pair.Key
            );
            sprite.Name = $"StarwovenChest_{pair.Key.X}_{pair.Key.Y}";
            sprite.Position = CellCenter(pair.Key) + new Vector2(0, 8);
            sprite.ZIndex = pair.Key.Y;
            sprite.AddChild(new ActorShadow
            {
                Position = new Vector2(0, 1),
                ZIndex = -1
            });
            _storageChestLayer.AddChild(sprite);
        }
    }

    private void RefreshFarmObjects(GridPosition position)
    {
        _ = position;
        RebuildFarmObjects();
    }

    private void RebuildFarmObjects()
    {
        foreach (var child in _farmObjectLayer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var pair in _session.FarmObjects.Objects)
        {
            if (pair.Value == DataCatalog.GlowcombHiveId)
            {
                continue;
            }

            var definition = DataCatalog.FarmObject(pair.Value);
            var sprite = GeneratedArt.CreateFarmObjectSprite(pair.Value);
            sprite.Name = $"FarmObject_{pair.Value}_{pair.Key.X}_{pair.Key.Y}";
            sprite.Position = CellCenter(pair.Key);
            if (definition.Kind != FarmObjectKind.Path)
            {
                sprite.Position += new Vector2(0, 8);
                sprite.ZIndex = pair.Key.Y;
                sprite.AddChild(new ActorShadow
                {
                    Position = new Vector2(0, 1),
                    ZIndex = -2
                });
            }
            else
            {
                sprite.ZIndex = -8;
            }

            if (definition.Kind is FarmObjectKind.Torch or
                FarmObjectKind.Sprinkler)
            {
                sprite.AddChild(new FarmObjectGlow(definition.Kind)
                {
                    Position = definition.Kind == FarmObjectKind.Torch
                        ? new Vector2(0, -19)
                        : new Vector2(0, -7),
                    ZIndex = -1
                });
            }

            _farmObjectLayer.AddChild(sprite);
        }
    }

    private void RebuildCrabPots()
    {
        foreach (var child in _crabPotLayer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var state in _session.CrabPots.Pots.Values)
        {
            var sprite = FishingGearArt.CreateCrabPotSprite(state);
            sprite.Name = $"MoonreedCrabPot_{state.Position.X}_{state.Position.Y}";
            sprite.Position = CellCenter(state.Position) + new Vector2(0, 6);
            sprite.ZIndex = state.Position.Y;
            _crabPotLayer.AddChild(sprite);
        }
    }

    private GridPosition? ResolveOrchardTarget(
        GridPosition target,
        GridPosition player
    )
    {
        if (_session.Orchard.HasFruitTree(target) ||
            _session.FarmObjects.ItemAt(target) == DataCatalog.GlowcombHiveId)
        {
            return target;
        }

        return _session.Orchard.InteractiveCells
            .Concat(_session.FarmObjects.Objects
                .Where(pair => pair.Value == DataCatalog.GlowcombHiveId)
                .Select(pair => pair.Key))
            .Distinct()
            .Where(cell => IsAdjacent(player, cell))
            .OrderBy(cell =>
                Math.Abs(cell.X - target.X) +
                Math.Abs(cell.Y - target.Y)
            )
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .Cast<GridPosition?>()
            .FirstOrDefault();
    }

    private GridPosition? ResolveStorageTarget(
        GridPosition target,
        GridPosition player
    )
    {
        if (_session.Storage.HasChest(target))
        {
            return target;
        }

        return _session.Storage.Chests.Keys
            .Where(cell => IsAdjacent(player, cell))
            .OrderBy(cell => Math.Abs(cell.X - target.X) + Math.Abs(cell.Y - target.Y))
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .Cast<GridPosition?>()
            .FirstOrDefault();
    }

    private static ProcessorMachineDefinition? ResolveProcessorTarget(
        GridPosition target,
        GridPosition player
    )
    {
        var exactId = FarmLayout.ProcessorMachineIdAt(target);
        if (exactId is not null)
        {
            return ProcessorCatalog.Machine(exactId);
        }

        return ProcessorCatalog.Machines.Values
            .Where(machine => IsAdjacent(player, machine.Position))
            .OrderBy(machine =>
                Math.Abs(machine.Position.X - target.X) +
                Math.Abs(machine.Position.Y - target.Y)
            )
            .ThenBy(machine => machine.Position.Y)
            .ThenBy(machine => machine.Position.X)
            .FirstOrDefault();
    }

    private static bool IsMoonstonePath(GridPosition position) =>
        (position.Y == 11 && position.X is >= 7 and <= 34) ||
        (position.X is >= 7 and <= 9 && position.Y is >= 10 and <= 13) ||
        (position.X is >= 29 and <= 33 && position.Y is >= 9 and <= 12);

    private bool CanOccupy(Vector2 worldPosition)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        );
        if (!WorldDefinition.IsInBounds(cell))
        {
            return false;
        }

        if (!WorldDefinition.IsHomeCell(cell))
        {
            if (WorldDefinition.IsBoundaryCell(cell))
            {
                return false;
            }

            if (_session.Village.NpcAt(
                    cell,
                    _session.Clock.Day,
                    _session.Clock.MinuteOfDay,
                    PlayerLocationIds.World,
                    _player.CurrentCell
                ) is not null)
            {
                return false;
            }

            if (_session.Forage.SpawnAt(cell) is not null)
            {
                return false;
            }

            return _session.Resources.IsRemoved(cell) ||
                !WorldDefinition.IsBlocked(cell);
        }

        // Older saves may legitimately contain a chest, tree, or placeable on
        // an animal-building approach. Keep that data, and keep water-built
        // boardwalk approaches traversable so they cannot seal the only door.
        if (FarmLayout.IsAnimalBuildingApproachCell(cell))
        {
            return true;
        }

        if (WorldDefinition.IsBlocked(cell))
        {
            return false;
        }

        if (FarmLayout.IsStaticBlocked(cell) ||
            _session.Farm.IsReserved(cell) ||
            _session.Storage.HasChest(cell) ||
            _session.FarmObjects.BlocksMovement(cell) ||
            _session.Orchard.BlocksMovement(cell))
        {
            return false;
        }

        return true;
    }

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private VillageNpcState? ResolveVillageNpcTarget(
        GridPosition target,
        GridPosition player
    )
    {
        var current = _session.Village.CurrentNpcs(
            _session.Clock.Day,
            _session.Clock.MinuteOfDay,
            PlayerLocationIds.World,
            player
        );
        var exact = current.FirstOrDefault(
            npc => npc.Position == target
        );
        if (exact is not null)
        {
            return exact;
        }

        return current
            .Where(npc => IsAdjacent(player, npc.Position))
            .OrderBy(npc =>
                Math.Abs(npc.Position.X - target.X) +
                Math.Abs(npc.Position.Y - target.Y)
            )
            .ThenBy(npc => npc.Position.Y)
            .ThenBy(npc => npc.Position.X)
            .FirstOrDefault();
    }

    private static bool IsAdjacent(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) <= 1;

    private static bool IsNearCommissionBoard(GridPosition player) =>
        Enumerable.Range(26, 3)
            .Select(x => new GridPosition(x, CommissionBoardCell.Y))
            .Any(cell => IsAdjacent(player, cell));
}

internal sealed partial class FarmObjectGlow : Node2D
{
    private readonly FarmObjectKind _kind;
    private double _time;

    public FarmObjectGlow(FarmObjectKind kind)
    {
        _kind = kind;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var pulse = 0.82f + Mathf.Sin((float)_time * 2.8f) * 0.18f;
        var color = _kind == FarmObjectKind.Torch
            ? ThemeFactory.Gold
            : ThemeFactory.Mint;
        DrawCircle(Vector2.Zero, 12 * pulse, new Color(color, 0.035f));
        DrawCircle(Vector2.Zero, 7 * pulse, new Color(color, 0.075f));
        DrawCircle(Vector2.Zero, 2.2f * pulse, new Color(color, 0.28f));
    }
}

internal sealed partial class StationBeacon : Node2D
{
    private readonly Func<GridPosition> _playerCell;
    private readonly GridPosition _stationCell;
    private readonly Color _accent;
    private double _time;

    public StationBeacon(
        Func<GridPosition> playerCell,
        GridPosition stationCell,
        Color accent
    )
    {
        _playerCell = playerCell;
        _stationCell = stationCell;
        _accent = accent;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var player = _playerCell();
        var distance = Math.Abs(player.X - _stationCell.X) +
            Math.Abs(player.Y - _stationCell.Y);
        var nearby = distance <= 2;
        var pulse = 0.64f + Mathf.Sin((float)_time * 4.2f) * 0.2f;
        var accent = new Color(_accent, nearby ? pulse + 0.15f : pulse * 0.4f);

        DrawArc(new Vector2(0, 2), 11 + pulse * 2, 0, Mathf.Tau, 24, accent, nearby ? 2 : 1);
        DrawCircle(new Vector2(0, -49 + Mathf.Sin((float)_time * 3.8f) * 2), 2.3f, accent);
        if (!nearby)
        {
            return;
        }

        DrawRect(new Rect2(-8, -63, 16, 11), new Color("#07132bee"), true);
        DrawRect(new Rect2(-8, -63, 16, 11), accent, false, 1);
        DrawString(
            GD.Load<Font>("res://assets/fonts/NotoSansCJKsc-Regular.otf"),
            new Vector2(-3.5f, -54),
            "E",
            HorizontalAlignment.Left,
            -1,
            8,
            ThemeFactory.Ink
        );
    }
}

internal sealed partial class TargetCursor : Node2D
{
    private static readonly Font LabelFont =
        GD.Load<Font>("res://assets/fonts/NotoSansCJKsc-Regular.otf");

    private readonly Func<TargetPreview> _preview;
    private readonly LocaleService _locale;
    private double _time;

    public TargetCursor(Func<TargetPreview> preview, LocaleService locale)
    {
        _preview = preview;
        _locale = locale;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var preview = _preview();
        var origin = new Vector2(preview.Target.X * 16, preview.Target.Y * 16);
        var pulse = AccessibilityRuntime.Settings.ScreenShakePercent == 0
            ? 0.72f
            : 0.62f + Mathf.Sin((float)_time * 4) * 0.18f;
        var accent = PreviewColor(preview.State);
        var active = preview.State != TargetPreviewState.Neutral;
        DrawRect(
            new Rect2(origin + new Vector2(2, 2), new Vector2(12, 12)),
            new Color(accent, active ? 0.16f + pulse * 0.1f : 0.045f),
            true
        );
        DrawObjectHighlight(preview, origin, accent, pulse);

        var outline = new Color(
            accent,
            active ? Math.Min(1, pulse + 0.28f) : 0.48f
        );
        const float edge = 5;
        var lineWidth = AccessibilityRuntime.Settings.TargetCues ==
            TargetCueMode.HighContrast ? 2.5f : 1.5f;
        DrawPolyline([origin + new Vector2(1, edge), origin + Vector2.One, origin + new Vector2(edge, 1)], outline, lineWidth);
        DrawPolyline([origin + new Vector2(15 - edge, 1), origin + new Vector2(15, 1), origin + new Vector2(15, edge)], outline, lineWidth);
        DrawPolyline([origin + new Vector2(1, 15 - edge), origin + new Vector2(1, 15), origin + new Vector2(edge, 15)], outline, lineWidth);
        DrawPolyline([origin + new Vector2(15 - edge, 15), origin + new Vector2(15, 15), origin + new Vector2(15, 15 - edge)], outline, lineWidth);
        DrawCircle(origin + new Vector2(8, 8), active ? 1.7f : 1.1f, outline);

        if (active && !string.IsNullOrWhiteSpace(preview.LabelKey))
        {
            DrawActionLabel(preview, origin, accent);
        }
    }

    private void DrawObjectHighlight(
        TargetPreview preview,
        Vector2 origin,
        Color accent,
        float pulse
    )
    {
        var fill = new Color(accent, 0.07f + pulse * 0.035f);
        var line = new Color(accent, 0.56f + pulse * 0.32f);
        switch (preview.Kind)
        {
            case TargetPreviewKind.Tree:
                DrawRect(new Rect2(origin + new Vector2(-21, -56), new Vector2(58, 72)), fill);
                DrawRect(new Rect2(origin + new Vector2(-21, -56), new Vector2(58, 72)), line, false, 1.5f);
                DrawArc(origin + new Vector2(8, -25), 29 + pulse * 2, 0, Mathf.Tau, 28, line, 1.5f);
                break;
            case TargetPreviewKind.Crystal:
                DrawRect(new Rect2(origin + new Vector2(-13, -32), new Vector2(42, 48)), fill);
                DrawRect(new Rect2(origin + new Vector2(-13, -32), new Vector2(42, 48)), line, false, 1.5f);
                DrawCircle(origin + new Vector2(8, -8), 22 + pulse * 2, new Color(accent, 0.055f));
                break;
            case TargetPreviewKind.MineralVein:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-18, -38),
                        new Vector2(52, 54)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-18, -38),
                        new Vector2(52, 54)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.CrystalGrottoPortal:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-24, -50),
                        new Vector2(64, 66)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-24, -50),
                        new Vector2(64, 66)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.CrystalGrottoExit:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-24, -46),
                        new Vector2(64, 62)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-24, -46),
                        new Vector2(64, 62)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.StarfallRuinsPortal:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-20, -48),
                        new Vector2(56, 64)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-20, -48),
                        new Vector2(56, 64)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.StarfallRuinsExit:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-24, -26),
                        new Vector2(64, 42)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-24, -26),
                        new Vector2(64, 42)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.RuinsWeaponRack:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-20, -38),
                        new Vector2(56, 54)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-20, -38),
                        new Vector2(56, 54)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.RuinsEnemy:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-18, -40),
                        new Vector2(52, 56)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-18, -40),
                        new Vector2(52, 56)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.RuinsArtifact:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-16, -36),
                        new Vector2(48, 52)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-16, -36),
                        new Vector2(48, 52)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.RuinsSeal:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-21, -40),
                        new Vector2(58, 56)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-21, -40),
                        new Vector2(58, 56)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.ToolUpgradeBench:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-46, -42),
                        new Vector2(108, 58)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-46, -42),
                        new Vector2(108, 58)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.MineDepthAnchor:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-18, -38),
                        new Vector2(52, 54)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-18, -38),
                        new Vector2(52, 54)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.GrottoSeal:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-21, -43),
                        new Vector2(58, 59)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-21, -43),
                        new Vector2(58, 59)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.Forage:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-11, -20),
                        new Vector2(38, 36)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-11, -20),
                        new Vector2(38, 36)
                    ),
                    line,
                    false,
                    1.5f
                );
                DrawArc(
                    origin + new Vector2(8, -3),
                    18 + pulse,
                    0,
                    Mathf.Tau,
                    24,
                    line,
                    1.5f
                );
                break;
            case TargetPreviewKind.Water:
                DrawCircle(origin + new Vector2(8, 8), 10 + pulse * 2, fill);
                DrawArc(origin + new Vector2(8, 8), 7 + pulse, 0, Mathf.Tau, 20, line, 1.5f);
                DrawArc(origin + new Vector2(8, 8), 12 + pulse * 2, 0, Mathf.Tau, 24, new Color(accent, 0.35f), 1);
                break;
            case TargetPreviewKind.CrabPot:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-11, -20),
                        new Vector2(38, 36)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-11, -20),
                        new Vector2(38, 36)
                    ),
                    line,
                    false,
                    1.6f
                );
                DrawArc(
                    origin + new Vector2(8, 3),
                    18 + pulse,
                    0,
                    Mathf.Tau,
                    24,
                    line,
                    1.3f
                );
                break;
            case TargetPreviewKind.Crop:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-7, -16),
                        new Vector2(30, 32)
                    ),
                    fill
                );
                DrawArc(
                    origin + new Vector2(8, 0),
                    16 + pulse * 2,
                    0,
                    Mathf.Tau,
                    26,
                    line,
                    1.5f
                );
                break;
            case TargetPreviewKind.Character:
                DrawRect(new Rect2(origin + new Vector2(-9, -48), new Vector2(34, 64)), fill);
                DrawRect(new Rect2(origin + new Vector2(-9, -48), new Vector2(34, 64)), line, false, 1.5f);
                break;
            case TargetPreviewKind.Door:
                DrawRect(new Rect2(origin + new Vector2(-2, -34), new Vector2(20, 50)), fill);
                DrawRect(new Rect2(origin + new Vector2(-2, -34), new Vector2(20, 50)), line, false, 2);
                break;
            case TargetPreviewKind.Station:
                DrawRect(new Rect2(origin + new Vector2(-25, -54), new Vector2(66, 70)), fill);
                DrawRect(new Rect2(origin + new Vector2(-25, -54), new Vector2(66, 70)), line, false, 1.5f);
                break;
            case TargetPreviewKind.ArchiveResearchDesk:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-64, -40),
                        new Vector2(128, 88)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-64, -40),
                        new Vector2(128, 88)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.HomesteadWorkshop:
                var workshopRect = WorkshopHighlightRect(
                    origin,
                    preview
                );
                DrawRect(workshopRect, fill);
                DrawRect(workshopRect, line, false, 1.7f);
                break;
            case TargetPreviewKind.GreenhousePortal:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-20, -60),
                        new Vector2(56, 76)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-20, -60),
                        new Vector2(56, 76)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.AnimalBuildingPortal:
                var coopFacade = preview.LabelKey is
                    "construction.homestead_starfeather_coop.not_started" or
                    "construction.homestead_starfeather_coop.in_progress";
                var coopRect = coopFacade
                    ? new Rect2(
                        origin + new Vector2(-24, -24),
                        new Vector2(64, 58)
                    )
                    : new Rect2(
                        origin + new Vector2(-2, -14),
                        new Vector2(20, 46)
                    );
                DrawRect(coopRect, fill);
                DrawRect(coopRect, line, false, 1.8f);
                break;
            case TargetPreviewKind.MoonfleeceBarnPortal:
                var barnFacade = preview.LabelKey is
                    "construction.homestead_moonfleece_barn.not_started" or
                    "construction.homestead_moonfleece_barn.in_progress";
                var barnRect = barnFacade
                    ? new Rect2(
                        origin + new Vector2(-26, -42),
                        new Vector2(68, 66)
                    )
                    : new Rect2(
                        origin + new Vector2(-3, -30),
                        new Vector2(22, 48)
                    );
                DrawRect(barnRect, fill);
                DrawRect(barnRect, line, false, 1.8f);
                break;
            case TargetPreviewKind.AnimalBuildingExit:
            case TargetPreviewKind.MoonfleeceBarnExit:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-48, -58),
                        new Vector2(112, 74)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-48, -58),
                        new Vector2(112, 74)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.AnimalFeedTrough:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-61, -56),
                        new Vector2(126, 56)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-61, -56),
                        new Vector2(126, 56)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.AnimalNest:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-45, -94),
                        new Vector2(121, 94)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-45, -94),
                        new Vector2(121, 94)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.AnimalProductStation:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-62, -61),
                        new Vector2(124, 65)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-62, -61),
                        new Vector2(124, 65)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.DewhornMilkingStation:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-24, -47),
                        new Vector2(48, 48)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-24, -47),
                        new Vector2(48, 48)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.Animal:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-6, -15),
                        new Vector2(28, 30)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-6, -15),
                        new Vector2(28, 30)
                    ),
                    line,
                    false,
                    1.6f
                );
                break;
            case TargetPreviewKind.MoonfleeceSheep:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-11, -31),
                        new Vector2(38, 35)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-11, -31),
                        new Vector2(38, 35)
                    ),
                    line,
                    false,
                    1.6f
                );
                break;
            case TargetPreviewKind.Dewhorn:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-10, -32),
                        new Vector2(36, 35)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-10, -32),
                        new Vector2(36, 35)
                    ),
                    line,
                    false,
                    1.6f
                );
                break;
            case TargetPreviewKind.AnimalAutomationStation:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-16, -36),
                        new Vector2(48, 52)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-16, -36),
                        new Vector2(48, 52)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.GreenhouseExit:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-53, -51),
                        new Vector2(106, 91)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-53, -51),
                        new Vector2(106, 91)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.Cistern:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-2, -14),
                        new Vector2(40, 32)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-2, -14),
                        new Vector2(40, 32)
                    ),
                    line,
                    false,
                    1.6f
                );
                DrawArc(
                    origin + new Vector2(18, 2),
                    18 + pulse,
                    0,
                    Mathf.Tau,
                    24,
                    new Color(accent, 0.28f),
                    1
                );
                break;
            case TargetPreviewKind.KitchenReserve:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-10, -66),
                        new Vector2(144, 128)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-10, -66),
                        new Vector2(144, 128)
                    ),
                    line,
                    false,
                    1.5f
                );
                break;
            case TargetPreviewKind.KitchenStation:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-42, -66),
                        new Vector2(106, 128)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-42, -66),
                        new Vector2(106, 128)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.IngredientPantry:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-16, -66),
                        new Vector2(48, 128)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-16, -66),
                        new Vector2(48, 128)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.CommissionBoard:
                DrawRect(new Rect2(origin + new Vector2(-20, -42), new Vector2(56, 58)), fill);
                DrawRect(new Rect2(origin + new Vector2(-20, -42), new Vector2(56, 58)), line, false, 1.7f);
                DrawArc(origin + new Vector2(8, -18), 24 + pulse * 2, 0, Mathf.Tau, 28, new Color(accent, 0.34f), 1);
                break;
            case TargetPreviewKind.Mailbox:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-22, -54),
                        new Vector2(60, 70)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-22, -54),
                        new Vector2(60, 70)
                    ),
                    line,
                    false,
                    1.7f
                );
                DrawArc(
                    origin + new Vector2(8, -21),
                    27 + pulse * 2,
                    0,
                    Mathf.Tau,
                    28,
                    new Color(accent, 0.34f),
                    1
                );
                break;
            case TargetPreviewKind.StorageChest:
                DrawRect(new Rect2(origin + new Vector2(-13, -31), new Vector2(42, 47)), fill);
                DrawRect(new Rect2(origin + new Vector2(-13, -31), new Vector2(42, 47)), line, false, 1.5f);
                DrawArc(origin + new Vector2(8, -8), 20 + pulse, 0, Mathf.Tau, 24, new Color(accent, 0.32f), 1);
                break;
            case TargetPreviewKind.Path:
                DrawRect(new Rect2(origin + new Vector2(0, 0), new Vector2(16, 16)), fill);
                DrawRect(new Rect2(origin + new Vector2(0, 0), new Vector2(16, 16)), line, false, 1.5f);
                break;
            case TargetPreviewKind.Fence:
                DrawRect(new Rect2(origin + new Vector2(-4, -16), new Vector2(24, 32)), fill);
                DrawRect(new Rect2(origin + new Vector2(-4, -16), new Vector2(24, 32)), line, false, 1.5f);
                break;
            case TargetPreviewKind.Torch:
                DrawRect(new Rect2(origin + new Vector2(1, -23), new Vector2(14, 39)), fill);
                DrawRect(new Rect2(origin + new Vector2(1, -23), new Vector2(14, 39)), line, false, 1.5f);
                DrawCircle(origin + new Vector2(8, -15), 11 + pulse, new Color(accent, 0.06f));
                break;
            case TargetPreviewKind.Sprinkler:
                DrawRect(new Rect2(origin + new Vector2(-2, -7), new Vector2(20, 23)), fill);
                DrawRect(new Rect2(origin + new Vector2(-2, -7), new Vector2(20, 23)), line, false, 1.5f);
                DrawArc(origin + new Vector2(8, 3), 12 + pulse, 0, Mathf.Tau, 24, new Color(accent, 0.3f), 1);
                break;
            case TargetPreviewKind.FruitTree:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-21, -60),
                        new Vector2(58, 76)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-21, -60),
                        new Vector2(58, 76)
                    ),
                    line,
                    false,
                    1.5f
                );
                DrawArc(
                    origin + new Vector2(8, -27),
                    30 + pulse * 2,
                    0,
                    Mathf.Tau,
                    28,
                    new Color(accent, 0.34f),
                    1.2f
                );
                break;
            case TargetPreviewKind.Beehive:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-18, -39),
                        new Vector2(52, 55)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-18, -39),
                        new Vector2(52, 55)
                    ),
                    line,
                    false,
                    1.5f
                );
                DrawArc(
                    origin + new Vector2(8, -12),
                    24 + pulse * 2,
                    0,
                    Mathf.Tau,
                    28,
                    new Color(accent, 0.32f),
                    1
                );
                break;
            case TargetPreviewKind.Landmark:
                DrawRect(new Rect2(origin + new Vector2(-18, -42), new Vector2(52, 58)), fill);
                DrawRect(new Rect2(origin + new Vector2(-18, -42), new Vector2(52, 58)), line, false, 1.5f);
                break;
            case TargetPreviewKind.StarlightPedestal:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-31, -63),
                        new Vector2(78, 78)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-31, -63),
                        new Vector2(78, 78)
                    ),
                    line,
                    false,
                    1.8f
                );
                DrawArc(
                    origin + new Vector2(8, -25),
                    39 + pulse * 2,
                    0,
                    Mathf.Tau,
                    32,
                    new Color(accent, 0.38f),
                    1.2f
                );
                break;
            case TargetPreviewKind.StarGate:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-34, -70),
                        new Vector2(84, 86)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-34, -70),
                        new Vector2(84, 86)
                    ),
                    line,
                    false,
                    1.8f
                );
                DrawArc(
                    origin + new Vector2(8, -29),
                    41 + pulse * 2,
                    0,
                    Mathf.Tau,
                    32,
                    new Color(accent, 0.38f),
                    1.2f
                );
                break;
            case TargetPreviewKind.Bed:
                DrawRect(new Rect2(origin + new Vector2(-8, -10), new Vector2(32, 26)), fill);
                DrawRect(new Rect2(origin + new Vector2(-8, -10), new Vector2(32, 26)), line, false, 1.5f);
                break;
            case TargetPreviewKind.FestivalPortal:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-32, -60),
                        new Vector2(80, 76)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-32, -60),
                        new Vector2(80, 76)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.FestivalExit:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-24, -26),
                        new Vector2(64, 42)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-24, -26),
                        new Vector2(64, 42)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.FestivalExhibit:
            case TargetPreviewKind.FestivalFishBasin:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-38, -56),
                        new Vector2(92, 72)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-38, -56),
                        new Vector2(92, 72)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.FestivalBidBoard:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-20, -48),
                        new Vector2(56, 64)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-20, -48),
                        new Vector2(56, 64)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.FestivalShop:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-28, -62),
                        new Vector2(72, 78)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-28, -62),
                        new Vector2(72, 78)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.FestivalPlantingPlot:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-2, -5),
                        new Vector2(20, 20)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-2, -5),
                        new Vector2(20, 20)
                    ),
                    line,
                    false,
                    1.6f
                );
                break;
            case TargetPreviewKind.FestivalSeedRack:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-20, -28),
                        new Vector2(56, 44)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-20, -28),
                        new Vector2(56, 44)
                    ),
                    line,
                    false,
                    1.7f
                );
                break;
            case TargetPreviewKind.FestivalSeedExchange:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-28, -62),
                        new Vector2(72, 78)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-28, -62),
                        new Vector2(72, 78)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.FestivalFeastTable:
            case TargetPreviewKind.FestivalLanternLaunch:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-38, -56),
                        new Vector2(92, 72)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-38, -56),
                        new Vector2(92, 72)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.FestivalGiftExchange:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-23, -54),
                        new Vector2(62, 70)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-23, -54),
                        new Vector2(62, 70)
                    ),
                    line,
                    false,
                    1.8f
                );
                break;
            case TargetPreviewKind.FestivalRitual:
            case TargetPreviewKind.FestivalTideAltar:
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-31, -62),
                        new Vector2(78, 78)
                    ),
                    fill
                );
                DrawRect(
                    new Rect2(
                        origin + new Vector2(-31, -62),
                        new Vector2(78, 78)
                    ),
                    line,
                    false,
                    1.8f
                );
                DrawArc(
                    origin + new Vector2(8, -23),
                    39,
                    0,
                    Mathf.Tau,
                    32,
                    new Color(line, 0.34f),
                    1.1f
                );
                break;
        }
    }

    private void DrawActionLabel(
        TargetPreview preview,
        Vector2 origin,
        Color accent
    )
    {
        var translated = _locale.Tr(preview.LabelKey);
        var label = preview.IsAvailable ? $"E · {translated}" : translated;
        var measured = LabelFont.GetStringSize(
            label,
            HorizontalAlignment.Left,
            -1,
            8
        );
        var width = Math.Clamp(measured.X + 12, 38, 118);
        var top = origin.Y + LabelOffset(preview.Kind);
        var panel = new Rect2(
            origin.X + LabelCenterOffset(preview.Kind) - width / 2,
            top,
            width,
            13
        );
        DrawRect(panel, new Color("#07132bee"), true);
        DrawRect(panel, new Color(accent, 0.96f), false, 1);
        DrawString(
            LabelFont,
            panel.Position + new Vector2(6, 9.5f),
            label,
            HorizontalAlignment.Center,
            panel.Size.X - 12,
            8,
            ThemeFactory.Ink
        );
    }

    private static float LabelOffset(TargetPreviewKind kind) => kind switch
    {
        TargetPreviewKind.Tree => -72,
        TargetPreviewKind.Station => -68,
        TargetPreviewKind.ArchiveResearchDesk => -54,
        TargetPreviewKind.HomesteadWorkshop => -58,
        TargetPreviewKind.GreenhousePortal => -76,
        TargetPreviewKind.AnimalBuildingPortal => -62,
        TargetPreviewKind.MoonfleeceBarnPortal => -70,
        TargetPreviewKind.AnimalBuildingExit => -68,
        TargetPreviewKind.MoonfleeceBarnExit => -68,
        TargetPreviewKind.AnimalFeedTrough => -64,
        TargetPreviewKind.AnimalNest => 4,
        TargetPreviewKind.AnimalProductStation => -66,
        TargetPreviewKind.DewhornMilkingStation => -54,
        TargetPreviewKind.Animal => -38,
        TargetPreviewKind.MoonfleeceSheep => -43,
        TargetPreviewKind.Dewhorn => -43,
        TargetPreviewKind.AnimalAutomationStation => -54,
        TargetPreviewKind.GreenhouseExit => -66,
        TargetPreviewKind.Cistern => -44,
        TargetPreviewKind.KitchenReserve => -72,
        TargetPreviewKind.KitchenStation => -72,
        TargetPreviewKind.IngredientPantry => -72,
        TargetPreviewKind.CommissionBoard => -58,
        TargetPreviewKind.Mailbox => -67,
        TargetPreviewKind.StorageChest => -48,
        TargetPreviewKind.Path => -22,
        TargetPreviewKind.Fence => -39,
        TargetPreviewKind.Torch => -48,
        TargetPreviewKind.Sprinkler => -34,
        TargetPreviewKind.FruitTree => -74,
        TargetPreviewKind.Beehive => -54,
        TargetPreviewKind.Character => -62,
        TargetPreviewKind.Door => -49,
        TargetPreviewKind.Crystal => -47,
        TargetPreviewKind.MineralVein => -52,
        TargetPreviewKind.CrystalGrottoPortal => -66,
        TargetPreviewKind.CrystalGrottoExit => -62,
        TargetPreviewKind.StarfallRuinsPortal => -66,
        TargetPreviewKind.StarfallRuinsExit => -60,
        TargetPreviewKind.RuinsWeaponRack => -56,
        TargetPreviewKind.RuinsEnemy => -58,
        TargetPreviewKind.RuinsArtifact => -54,
        TargetPreviewKind.RuinsSeal => -58,
        TargetPreviewKind.ToolUpgradeBench => -60,
        TargetPreviewKind.MineDepthAnchor => -54,
        TargetPreviewKind.GrottoSeal => -58,
        TargetPreviewKind.Forage => -39,
        TargetPreviewKind.CrabPot => -42,
        TargetPreviewKind.Landmark => -56,
        TargetPreviewKind.StarlightPedestal => -62,
        TargetPreviewKind.StarGate => 30,
        TargetPreviewKind.Crop => -34,
        TargetPreviewKind.Bed => -25,
        TargetPreviewKind.FestivalPortal => -76,
        TargetPreviewKind.FestivalExit => -62,
        TargetPreviewKind.FestivalExhibit => -66,
        TargetPreviewKind.FestivalBidBoard => -58,
        TargetPreviewKind.FestivalShop => -68,
        TargetPreviewKind.FestivalPlantingPlot => -34,
        TargetPreviewKind.FestivalSeedRack => -50,
        TargetPreviewKind.FestivalSeedExchange => -68,
        TargetPreviewKind.FestivalFeastTable => -66,
        TargetPreviewKind.FestivalGiftExchange => -64,
        TargetPreviewKind.FestivalRitual => 66,
        TargetPreviewKind.FestivalLanternLaunch => -66,
        TargetPreviewKind.FestivalFishBasin => -62,
        TargetPreviewKind.FestivalTideAltar => 66,
        _ => -18
    };

    private static float LabelCenterOffset(TargetPreviewKind kind) =>
        kind == TargetPreviewKind.AnimalNest ? -70 : 8;

    private static Color PreviewColor(TargetPreviewState state)
    {
        if (AccessibilityRuntime.Settings.TargetCues ==
            TargetCueMode.HighContrast)
        {
            return state switch
            {
                TargetPreviewState.Available => Colors.White,
                TargetPreviewState.NeedsTool => new Color("#ffe100"),
                TargetPreviewState.Blocked => new Color("#ff4a4a"),
                _ => new Color("#9ca9bd")
            };
        }

        if (AccessibilityRuntime.Settings.TargetCues ==
            TargetCueMode.Deuteranopia)
        {
            return state switch
            {
                TargetPreviewState.Available => new Color("#55b8ff"),
                TargetPreviewState.NeedsTool => new Color("#ffd166"),
                TargetPreviewState.Blocked => new Color("#d98cff"),
                _ => new Color("#8ca0b8")
            };
        }

        return state switch
        {
            TargetPreviewState.Available => ThemeFactory.Mint,
            TargetPreviewState.NeedsTool => ThemeFactory.Gold,
            TargetPreviewState.Blocked => new Color("#e58a9f"),
            _ => new Color("#8294b8")
        };
    }

    private static Rect2 WorkshopHighlightRect(
        Vector2 origin,
        TargetPreview preview
    )
    {
        if (preview.LabelKey ==
            "construction.homestead_workshop.not_started")
        {
            return new Rect2(
                origin + new Vector2(-13, -16),
                new Vector2(42, 32)
            );
        }

        if (preview.LabelKey ==
            "construction.homestead_workshop.in_progress")
        {
            return new Rect2(
                origin + new Vector2(-17, -28),
                new Vector2(50, 44)
            );
        }

        return new Rect2(
            origin + new Vector2(-20, -32),
            new Vector2(56, 48)
        );
    }
}

internal sealed partial class CottageEntranceBeacon : Node2D
{
    private readonly Func<GridPosition> _playerCell;
    private double _time;

    public CottageEntranceBeacon(Func<GridPosition> playerCell)
    {
        _playerCell = playerCell;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var player = _playerCell();
        var distance = Math.Abs(player.X - FarmView.CottageDoorCell.X) +
            Math.Abs(player.Y - FarmView.CottageDoorCell.Y);
        var nearby = distance <= 3;
        var pulse = 0.72f + Mathf.Sin((float)_time * 3.6f) * 0.18f;
        var alpha = nearby ? pulse : pulse * 0.58f;
        var mint = new Color(ThemeFactory.Mint, alpha);
        var gold = new Color(ThemeFactory.Gold, Math.Min(alpha + 0.12f, 1));

        DrawLine(new Vector2(-9, -27), new Vector2(-9, 3), mint, nearby ? 2 : 1);
        DrawLine(new Vector2(9, -27), new Vector2(9, 3), mint, nearby ? 2 : 1);
        DrawLine(new Vector2(-9, -27), new Vector2(9, -27), gold, nearby ? 2 : 1);
        DrawArc(new Vector2(0, 6), 11 + pulse * 2, 0, Mathf.Tau, 24, mint, 1.4f);
        DrawArc(new Vector2(0, 6), 5 + pulse, 0, Mathf.Tau, 16, new Color(gold, alpha * 0.6f), 1);

        var arrowY = -35 + Mathf.Sin((float)_time * 4.2f) * 2;
        DrawColoredPolygon(
            [
                new Vector2(0, arrowY + 5),
                new Vector2(-5, arrowY),
                new Vector2(0, arrowY + 2),
                new Vector2(5, arrowY),
            ],
            gold
        );

        if (!nearby)
        {
            return;
        }

        DrawRect(new Rect2(-7, -47, 14, 10), new Color("#07132be6"), true);
        DrawRect(new Rect2(-7, -47, 14, 10), gold, false, 1);
        DrawString(
            GD.Load<Font>("res://assets/fonts/NotoSansCJKsc-Regular.otf"),
            new Vector2(-3.5f, -39),
            "E",
            HorizontalAlignment.Left,
            -1,
            8,
            ThemeFactory.Ink
        );
    }
}

internal sealed partial class VillageEntranceBeacon : Node2D
{
    private readonly Func<GridPosition> _playerCell;
    private readonly GridPosition _target;
    private double _time;

    public VillageEntranceBeacon(
        Func<GridPosition> playerCell,
        GridPosition target
    )
    {
        _playerCell = playerCell;
        _target = target;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var player = _playerCell();
        var distance = Math.Abs(player.X - _target.X) +
            Math.Abs(player.Y - _target.Y);
        var nearby = distance <= 3;
        var pulse = 0.68f + Mathf.Sin((float)_time * 3.9f) * 0.2f;
        var alpha = nearby ? pulse : pulse * 0.46f;
        var mint = new Color(ThemeFactory.Mint, alpha);
        var gold = new Color(ThemeFactory.Gold, Math.Min(alpha + 0.1f, 1));

        DrawRect(
            new Rect2(-10, -34, 20, 38),
            new Color(mint, alpha * 0.08f),
            true
        );
        DrawRect(new Rect2(-10, -34, 20, 38), mint, false, nearby ? 2 : 1);
        DrawArc(new Vector2(0, -34), 10, Mathf.Pi, Mathf.Tau, 18, gold, nearby ? 2 : 1);
        DrawArc(new Vector2(0, 5), 12 + pulse * 2, 0, Mathf.Tau, 24, mint, 1.4f);

        if (!nearby)
        {
            return;
        }

        var sparkle = new Vector2(0, -45 + Mathf.Sin((float)_time * 4.3f) * 2);
        DrawLine(sparkle + new Vector2(-4, 0), sparkle + new Vector2(4, 0), gold, 1.5f);
        DrawLine(sparkle + new Vector2(0, -4), sparkle + new Vector2(0, 4), gold, 1.5f);
    }
}

internal sealed partial class FarmDecor : Node2D
{
    public FarmDecor()
    {
        ZIndex = 6;
    }

    public override void _Draw()
    {
        // Clearly bounded planting field with a broken fence at the path entrance.
        var fieldEdge = new Color("#8d7181");
        DrawLine(new Vector2(108, 202), new Vector2(580, 202), fieldEdge, 2);
        DrawLine(new Vector2(108, 202), new Vector2(108, 450), fieldEdge, 2);
        DrawLine(new Vector2(580, 202), new Vector2(580, 450), fieldEdge, 2);
        DrawLine(new Vector2(108, 450), new Vector2(580, 450), fieldEdge, 2);
        for (var x = 116; x <= 572; x += 32)
        {
            DrawRect(new Rect2(x, 197, 3, 11), new Color("#c29b78"));
            DrawCircle(new Vector2(x + 1.5f, 197), 2, new Color("#f2c66d"));
        }
        DrawRect(new Rect2(119, 191, 38, 9), new Color("#24344c"));
        DrawRect(new Rect2(121, 193, 34, 5), new Color("#8d7181"));
        DrawCircle(new Vector2(128, 195), 2, new Color("#f2c66d"));
        DrawCircle(new Vector2(148, 195), 2, new Color("#8ee6be"));

        // Cottage body, roof, windows, door, and warm entrance lanterns.
        DrawRect(new Rect2(64, 70, 128, 90), new Color("#332d49"));
        DrawRect(new Rect2(69, 76, 118, 79), new Color("#765165"));
        for (var y = 83; y <= 145; y += 11)
        {
            DrawLine(new Vector2(70, y), new Vector2(186, y), new Color("#986579"), 1);
        }
        DrawColoredPolygon(
            [new Vector2(52, 76), new Vector2(204, 76), new Vector2(172, 38), new Vector2(84, 38)],
            new Color("#171f48")
        );
        DrawPolyline(
            [new Vector2(52, 76), new Vector2(84, 38), new Vector2(172, 38), new Vector2(204, 76)],
            new Color("#7f75c8"),
            3
        );
        // Layered shingles, chimney, and roof-edge highlights.
        DrawRect(new Rect2(164, 25, 13, 34), new Color("#252841"));
        DrawRect(new Rect2(166, 28, 9, 29), new Color("#5d4d62"));
        DrawRect(new Rect2(162, 24, 17, 5), new Color("#8f7181"));
        foreach (var segment in new[]
                 {
                     new Vector4(80, 45, 176, 45),
                     new Vector4(70, 53, 186, 53),
                     new Vector4(62, 61, 194, 61),
                     new Vector4(56, 69, 200, 69),
                 })
        {
            DrawLine(
                new Vector2(segment.X, segment.Y),
                new Vector2(segment.Z, segment.W),
                new Color("#30345a"),
                2
            );
        }
        for (var x = 71; x <= 186; x += 14)
        {
            DrawLine(new Vector2(x, 53), new Vector2(x + 5, 58), new Color("#4a4670"), 1);
            DrawLine(new Vector2(x - 6, 61), new Vector2(x, 66), new Color("#4a4670"), 1);
        }
        DrawLine(new Vector2(61, 70), new Vector2(195, 70), new Color("#b795dd"), 2);
        DrawRect(new Rect2(80, 97, 23, 22), new Color("#302f52"));
        DrawRect(new Rect2(84, 101, 15, 14), new Color("#f3ca78"));
        DrawLine(new Vector2(91.5f, 101), new Vector2(91.5f, 115), new Color("#fff0ac"), 1);
        DrawRect(new Rect2(153, 97, 23, 22), new Color("#302f52"));
        DrawRect(new Rect2(157, 101, 15, 14), new Color("#f3ca78"));
        DrawLine(new Vector2(164.5f, 101), new Vector2(164.5f, 115), new Color("#fff0ac"), 1);
        DrawRect(new Rect2(116, 118, 24, 42), new Color("#252744"));
        DrawRect(new Rect2(121, 124, 14, 36), new Color("#56445f"));
        DrawCircle(new Vector2(132, 142), 1.5f, new Color("#f3ca78"));
        DrawCircle(new Vector2(128, 51), 6, new Color("#8ee6be"));
        DrawColoredPolygon(
            [new Vector2(128, 43), new Vector2(131, 49), new Vector2(138, 51), new Vector2(131, 54), new Vector2(128, 61), new Vector2(125, 54), new Vector2(118, 51), new Vector2(125, 49)],
            new Color("#f3ca78")
        );
        DrawCircle(new Vector2(107, 151), 4, new Color(0.95f, 0.71f, 0.3f, 0.2f));
        DrawCircle(new Vector2(149, 151), 4, new Color(0.95f, 0.71f, 0.3f, 0.2f));
        DrawCircle(new Vector2(107, 151), 1.7f, new Color("#f3ca78"));
        DrawCircle(new Vector2(149, 151), 1.7f, new Color("#f3ca78"));
        // Flower boxes and a covered side porch make the cottage read as a home.
        DrawRect(new Rect2(78, 118, 27, 5), new Color("#5b3e50"));
        DrawRect(new Rect2(151, 118, 27, 5), new Color("#5b3e50"));
        foreach (var flower in new[] { new Vector2(83, 117), new Vector2(90, 116), new Vector2(98, 117), new Vector2(157, 117), new Vector2(165, 116), new Vector2(172, 117) })
        {
            DrawLine(flower, flower + new Vector2(0, -4), new Color("#62b781"), 1);
            DrawCircle(flower + new Vector2(0, -5), 1.6f, (Mathf.FloorToInt(flower.X) & 1) == 0 ? new Color("#b795dd") : new Color("#8ee6be"));
        }
        DrawColoredPolygon(
            [new Vector2(47, 112), new Vector2(75, 112), new Vector2(82, 99), new Vector2(53, 99)],
            new Color("#222c4b")
        );
        DrawLine(new Vector2(49, 111), new Vector2(77, 111), new Color("#6e6d99"), 2);
        DrawRect(new Rect2(53, 111, 3, 43), new Color("#624957"));
        DrawRect(new Rect2(73, 111, 3, 43), new Color("#624957"));

        // Well, buckets, and planters form a small working yard.
        DrawCircle(new Vector2(231, 143), 14, new Color("#292b43"));
        DrawCircle(new Vector2(231, 140), 12, new Color("#766476"));
        DrawCircle(new Vector2(231, 138), 8, new Color("#173a50"));
        DrawArc(new Vector2(231, 137), 8, 3.2f, 5.9f, 12, new Color("#64c6be"), 1);
        DrawLine(new Vector2(219, 142), new Vector2(219, 121), new Color("#825d5d"), 3);
        DrawLine(new Vector2(243, 142), new Vector2(243, 121), new Color("#825d5d"), 3);
        DrawLine(new Vector2(218, 121), new Vector2(244, 121), new Color("#b27b68"), 3);
        DrawCircle(new Vector2(231, 121), 3, new Color("#f3ca78"));
        DrawRect(new Rect2(205, 145, 11, 12), new Color("#7c555d"));
        DrawLine(new Vector2(206, 149), new Vector2(215, 149), new Color("#c08a70"), 1);
        DrawRect(new Rect2(247, 145, 15, 9), new Color("#5c4052"));
        DrawCircle(new Vector2(251, 144), 2, new Color("#8ee6be"));
        DrawCircle(new Vector2(257, 143), 2, new Color("#b795dd"));

        // Greenhouse with bright glass bays, plants, and a distinct mint doorway.
        DrawRect(new Rect2(544, 78, 128, 98), new Color("#132b3a"));
        DrawRect(new Rect2(550, 81, 116, 89), new Color(0.3f, 0.8f, 0.72f, 0.16f));
        DrawArc(new Vector2(608, 80), 61, Mathf.Pi, Mathf.Tau, 32, new Color("#63ded0"), 3);
        DrawLine(new Vector2(547, 80), new Vector2(669, 80), new Color("#63ded0"), 3);
        DrawLine(new Vector2(547, 80), new Vector2(547, 173), new Color("#428b94"), 3);
        DrawLine(new Vector2(669, 80), new Vector2(669, 173), new Color("#428b94"), 3);
        DrawLine(new Vector2(608, 19), new Vector2(608, 128), new Color("#67d9cf"), 2);
        DrawLine(new Vector2(608, 20), new Vector2(559, 80), new Color("#397d8c"), 2);
        DrawLine(new Vector2(608, 20), new Vector2(657, 80), new Color("#397d8c"), 2);
        DrawArc(new Vector2(608, 80), 43, Mathf.Pi, Mathf.Tau, 24, new Color("#397d8c"), 1);
        DrawArc(new Vector2(608, 80), 24, Mathf.Pi, Mathf.Tau, 20, new Color("#397d8c"), 1);
        DrawCircle(new Vector2(608, 19), 7, new Color(0.45f, 0.9f, 0.82f, 0.18f));
        DrawColoredPolygon(
            [new Vector2(608, 9), new Vector2(613, 18), new Vector2(608, 26), new Vector2(603, 18)],
            new Color("#8ee6be")
        );
        DrawLine(new Vector2(608, 11), new Vector2(608, 22), new Color("#e8fff0"), 1);
        for (var x = 560; x <= 656; x += 16)
        {
            DrawLine(new Vector2(x, 82), new Vector2(x, 169), new Color("#327985"), 1);
        }
        for (var x = 562; x <= 650; x += 22)
        {
            DrawLine(new Vector2(x, 157), new Vector2(x - 4, 143), new Color("#6fd69d"), 2);
            DrawLine(new Vector2(x, 150), new Vector2(x - 8, 147), new Color("#9bf0ba"), 2);
            DrawLine(new Vector2(x, 151), new Vector2(x + 7, 145), new Color("#9bf0ba"), 2);
        }
        DrawRect(new Rect2(598, 128, 22, 42), new Color("#244f5e"));
        DrawRect(new Rect2(602, 132, 14, 38), new Color(0.55f, 0.94f, 0.75f, 0.24f));
        DrawCircle(new Vector2(613, 150), 1.5f, new Color("#f3ca78"));
        foreach (var shimmer in new[] { new Vector2(572, 93), new Vector2(590, 61), new Vector2(628, 47), new Vector2(650, 101) })
        {
            DrawLine(shimmer + new Vector2(-3, 0), shimmer + new Vector2(3, 0), new Color(0.78f, 1, 0.94f, 0.55f), 1);
            DrawLine(shimmer + new Vector2(0, -3), shimmer + new Vector2(0, 3), new Color(0.78f, 1, 0.94f, 0.55f), 1);
        }
        // Workbench and specimen jars beside the greenhouse.
        DrawRect(new Rect2(675, 137, 31, 5), new Color("#95675f"));
        DrawRect(new Rect2(678, 142, 3, 18), new Color("#5a4050"));
        DrawRect(new Rect2(700, 142, 3, 18), new Color("#5a4050"));
        DrawRect(new Rect2(680, 128, 7, 9), new Color("#31596c"));
        DrawRect(new Rect2(681, 130, 5, 6), new Color("#72d9c2"));
        DrawRect(new Rect2(690, 125, 8, 12), new Color("#4d3e67"));
        DrawRect(new Rect2(692, 128, 4, 8), new Color("#b795dd"));

        // Pond glints, lily pads, and luminous crystal bank.
        DrawArc(new Vector2(673, 356), 10, 0.15f, 2.5f, 16, new Color(0.55f, 0.9f, 0.75f, 0.45f), 1);
        DrawArc(new Vector2(720, 389), 14, 0.2f, 2.8f, 16, new Color(0.55f, 0.9f, 0.75f, 0.36f), 1);
        DrawCircle(new Vector2(650, 428), 6, new Color("#315f63"));
        DrawLine(new Vector2(650, 428), new Vector2(655, 424), new Color("#9bf0ba"), 1);
        DrawCircle(new Vector2(700, 422), 8, new Color(0.55f, 0.9f, 0.75f, 0.28f));
        DrawColoredPolygon(
            [new Vector2(694, 430), new Vector2(700, 407), new Vector2(706, 430)],
            new Color("#8ee6be")
        );
        DrawColoredPolygon(
            [new Vector2(708, 432), new Vector2(714, 415), new Vector2(720, 432)],
            new Color("#7f75c8")
        );

        // Two-tone trees with lit edges instead of flat silhouettes.
        foreach (var center in new[]
                 {
                     new Vector2(42, 92), new Vector2(700, 74), new Vector2(74, 430),
                     new Vector2(476, 58), new Vector2(454, 466)
                 })
        {
            DrawRect(new Rect2(center.X - 4, center.Y + 8, 8, 17), new Color("#6e4d55"));
            DrawCircle(center + new Vector2(-6, 1), 13, new Color("#213e50"));
            DrawCircle(center + new Vector2(6, -2), 15, new Color("#2c5c61"));
            DrawCircle(center + new Vector2(10, 2), 8, new Color("#3e7870"));
            DrawArc(center + new Vector2(5, -3), 15, 3.5f, 5.8f, 16, new Color("#73c996"), 2);
            DrawCircle(center + new Vector2(11, -4), 1.5f, new Color("#f3ca78"));
        }

        // Path lamps lead the eye from the cottage to Mira and the farm entrance.
        foreach (var lamp in new[] { new Vector2(151, 176), new Vector2(338, 176), new Vector2(535, 176) })
        {
            DrawLine(lamp, lamp + new Vector2(0, -12), new Color("#72596b"), 2);
            DrawCircle(lamp + new Vector2(0, -14), 5, new Color(0.95f, 0.78f, 0.4f, 0.18f));
            DrawCircle(lamp + new Vector2(0, -14), 2, new Color("#f3ca78"));
        }
    }
}

internal sealed partial class MiraBeacon : Node2D
{
    private readonly GameSession _session;
    private double _time;

    public MiraBeacon(GameSession session)
    {
        _session = session;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var actionable = _session.Quest.Stage is QuestStage.TalkToMira or QuestStage.ReturnToMira;
        var bob = Mathf.Sin((float)_time * 3) * 2;
        var marker = new Vector2(0, -27 + bob);
        var glow = actionable ? new Color("#f3ca78") : new Color("#8ee6be");
        DrawCircle(marker, actionable ? 7 : 5, new Color(glow, 0.18f));
        DrawColoredPolygon(
            [
                marker + new Vector2(0, -5),
                marker + new Vector2(5, 0),
                marker + new Vector2(0, 5),
                marker + new Vector2(-5, 0),
            ],
            new Color("#17243d")
        );
        DrawPolyline(
            [
                marker + new Vector2(0, -5),
                marker + new Vector2(5, 0),
                marker + new Vector2(0, 5),
                marker + new Vector2(-5, 0),
                marker + new Vector2(0, -5),
            ],
            glow,
            1.5f
        );
        if (actionable)
        {
            DrawLine(marker + new Vector2(0, -2.5f), marker + new Vector2(0, 1), glow, 1.5f);
            DrawCircle(marker + new Vector2(0, 3), 0.9f, glow);
        }
        else
        {
            DrawCircle(marker, 1.5f, glow);
        }
    }
}

internal sealed partial class MoteField : Node2D
{
    private readonly Rect2 _bounds;
    private readonly Vector2[] _points;
    private double _time;

    public MoteField(Rect2 bounds)
    {
        _bounds = bounds;
        ZIndex = 30;
        var random = new Random(2407);
        _points = Enumerable.Range(0, 58)
            .Select(_ => new Vector2(
                (float)(random.NextDouble() * bounds.Size.X),
                (float)(random.NextDouble() * bounds.Size.Y)
            ))
            .ToArray();
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (var index = 0; index < _points.Length; index++)
        {
            var basePoint = _points[index];
            var point = new Vector2(
                basePoint.X + Mathf.Sin((float)_time + index) * 3,
                Mathf.PosMod(basePoint.Y - (float)_time * (2 + index % 3), _bounds.Size.Y)
            );
            var alpha = 0.22f + Mathf.Sin((float)_time * 2 + index) * 0.14f;
            var color = (index % 7) switch
            {
                0 => new Color(0.72f, 0.58f, 0.95f, alpha),
                1 => new Color(0.98f, 0.8f, 0.42f, alpha),
                _ => new Color(0.55f, 0.95f, 0.8f, alpha),
            };
            DrawCircle(point, index % 5 == 0 ? 1.5f : 1, color);
        }
    }
}
