using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private void StartCommissionOfferPlaytest()
    {
        StartCommissionPlaytest();
    }

    private void StartCommissionReadyPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.EndDay();
        _session.AcceptDailyCommission();
        _session.Commission.RecordGather(DataCatalog.LumenwoodId, 3);
        StartCommissionPlaytestWorld();
    }

    private void StartCommissionReadyEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartCommissionReadyPlaytest();
    }

    private void StartCommissionMapPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.EndDay();
        _session.AcceptDailyCommission();
        _session.Commission.RecordGather(DataCatalog.LumenwoodId, 2);
        StartCommissionPlaytestWorld(false);
    }

    private void StartCommissionPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        StartCommissionPlaytestWorld();
    }

    private void StartCommissionPlaytestWorld(bool openBoard = true)
    {
        _session.Inventory.Select(0);
        _session.SetPlayerState(
            FarmView.CommissionBoardCell.X * 16 + 8,
            (FarmView.CommissionBoardCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openBoard)
        {
            Callable.From(OpenCommissionBoard).CallDeferred();
        }
    }

    private void StartWeeklyCommissionOfferPlaytest()
    {
        PrepareWeeklyCommissionPlaytest();
    }

    private void StartWeeklyCommissionStageReadyPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.AcceptWeeklyCommission();
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        StartWeeklyCommissionPlaytestWorld();
    }

    private void StartWeeklyCommissionRewardReadyPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.AcceptWeeklyCommission();
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        _session.AdvanceWeeklyCommissionStage();
        _session.WeeklyCommission.RecordGather(
            DataCatalog.LumenwoodId,
            4
        );
        _session.AdvanceWeeklyCommissionStage();
        _session.Inventory.Add(DataCatalog.CrystalShardId, 3);
        StartWeeklyCommissionPlaytestWorld();
    }

    private void StartWeeklyCommissionMapPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.AcceptWeeklyCommission();
        StartCommissionPlaytestWorld(false);
    }

    private void PrepareWeeklyCommissionPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        StartWeeklyCommissionPlaytestWorld();
    }

    private void StartWeeklyCommissionPlaytestWorld()
    {
        StartCommissionPlaytestWorld(false);
        Callable.From(OpenWeeklyCommissionBoard).CallDeferred();
    }

    private void StartMailboxUnreadPlaytest()
    {
        PrepareMailPlaytest(
            [
                new MailEntrySave
                {
                    MailId = MailCatalog.NemiWelcomeId,
                    DeliveredDay = 2
                }
            ]
        );
        StartMailPlaytestWorld(false);
    }

    private void StartMailPanelPlaytest()
    {
        PrepareMailPlaytest(
            [
                new MailEntrySave
                {
                    MailId = MailCatalog.NemiWelcomeId,
                    DeliveredDay = 4
                },
                new MailEntrySave
                {
                    MailId = MailCatalog.LioraTrustedId,
                    DeliveredDay = 3
                },
                new MailEntrySave
                {
                    MailId = MailCatalog.TaviTrustedId,
                    DeliveredDay = 3,
                    IsRead = true,
                    AttachmentClaimed = true
                }
            ]
        );
        StartMailPlaytestWorld(true);
    }

    private void StartMailRewardPlaytest()
    {
        PrepareMailPlaytest(
            [
                new MailEntrySave
                {
                    MailId = MailCatalog.LioraTrustedId,
                    DeliveredDay = 5
                },
                new MailEntrySave
                {
                    MailId = MailCatalog.NemiWelcomeId,
                    DeliveredDay = 2,
                    IsRead = true
                }
            ]
        );
        StartMailPlaytestWorld(true, true);
    }

    private void PrepareMailPlaytest(IReadOnlyList<MailEntrySave> entries)
    {
        FreeUi(_title);
        _title = null;
        _mailPlaytest = true;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = entries.Max(entry => entry.DeliveredDay);
        save.Mail = new MailSave
        {
            Entries = entries.ToList()
        };
        _session.Restore(save);
    }

    private void StartMailPlaytestWorld(
        bool openPanel,
        bool claimAttachment = false
    )
    {
        _session.Inventory.Select(0);
        _session.SetPlayerState(
            FarmView.StarlightMailboxCell.X * 16 + 8,
            (FarmView.StarlightMailboxCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openPanel)
        {
            Callable.From(() =>
            {
                OpenStarlightMail();
                if (claimAttachment)
                {
                    _mailOverlay?.PressClaimForPlaytest();
                }
            }).CallDeferred();
        }
    }

    private void StartStarlightMapPlaytest()
    {
        StartStarlightPlaytestWorld(false);
    }

    private void StartStarlightPanelPlaytest()
    {
        PrepareStarlightPlaytest();
        _session.Inventory.Add(DataCatalog.StarbudId, 1);
        _session.Inventory.Add(DataCatalog.MoonrootId, 1);
        _session.ContributeToStarlightNode(
            DataCatalog.WoodlandHarvestNodeId
        );
        _session.Inventory.Add(DataCatalog.LumenwoodId, 3);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 1);
        _session.ContributeToStarlightNode(
            DataCatalog.WoodlandMaterialsNodeId
        );
        StartStarlightPlaytestWorld();
    }

    private void StartStarlightRestoredPlaytest()
    {
        PrepareRestoredStarlightPlaytest();
        StartStarlightPlaytestWorld();
    }

    private void StartStarlightRestoredMapPlaytest()
    {
        PrepareRestoredStarlightPlaytest();
        StartStarlightPlaytestWorld(false);
    }

    private void PrepareRestoredStarlightPlaytest()
    {
        PrepareStarlightPlaytest();
        _session.Inventory.Add(DataCatalog.StarbudId, 1);
        _session.Inventory.Add(DataCatalog.MoonrootId, 1);
        _session.Inventory.Add(DataCatalog.CloudleafId, 1);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 6);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 2);
        _session.Inventory.Add(DataCatalog.StarbudPreserveId, 1);
        _session.Inventory.Add(DataCatalog.MoonrootTonicId, 1);
        _session.Inventory.Add(DataCatalog.StarwovenChestId, 1);
        _session.ContributeToStarlightNode(
            DataCatalog.WoodlandHarvestNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.WoodlandMaterialsNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.WoodlandCraftNodeId
        );
    }

    private void StartStarlightRestoredEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartStarlightRestoredPlaytest();
    }

    private void StartHomesteadStarlightDormantPlaytest()
    {
        PrepareHomesteadStarlightPlaytest(restored: false);
        StartHomesteadStarlightPlaytestWorld(openPanel: false);
    }

    private void StartHomesteadStarlightWrongToolPlaytest()
    {
        PrepareHomesteadStarlightPlaytest(restored: false);
        StartHomesteadStarlightPlaytestWorld(
            openPanel: false,
            selectedSlot: 1
        );
    }

    private void StartHomesteadStarlightRestoredPlaytest()
    {
        PrepareHomesteadStarlightPlaytest(restored: true);
        StartHomesteadStarlightPlaytestWorld(openPanel: false);
    }

    private void StartHomesteadStarlightPanelPlaytest()
    {
        PrepareHomesteadStarlightPlaytest(restored: true);
        StartHomesteadStarlightPlaytestWorld(openPanel: true);
    }

    private void StartHomesteadStarlightPanelEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartHomesteadStarlightPanelPlaytest();
    }

    private void PrepareHomesteadStarlightPlaytest(bool restored)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Starlight.Discover(DataCatalog.HomesteadStarlightId);
        if (!restored)
        {
            return;
        }

        var crops = DataCatalog.CropIds.Take(4).ToArray();
        foreach (var cropId in crops)
        {
            _session.Inventory.Add(cropId, 1);
        }
        foreach (var itemId in new[]
        {
            DataCatalog.StarbudPreserveId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.CloudleafTeaId,
            DataCatalog.MoonstonePathId,
            DataCatalog.StarwoodFenceId,
            DataCatalog.StarlightTorchId
        })
        {
            _session.Inventory.Add(itemId, 1);
        }
        _session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadHarvestNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadArtisanNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadBuildingNodeId
        );
    }

    private void StartHomesteadStarlightPlaytestWorld(
        bool openPanel,
        int selectedSlot = 0
    )
    {
        _session.Inventory.Select(selectedSlot);
        _session.SetPlayerState(
            FarmView.HomesteadStarlightCell.X * 16 + 8,
            (FarmView.HomesteadStarlightCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openPanel)
        {
            Callable.From(() => OpenStarlightPedestal(
                DataCatalog.HomesteadStarlightId
            )).CallDeferred();
        }
    }

    private void StartMeadowStarlightDormantPlaytest()
    {
        PrepareMeadowStarlightPlaytest();
        StartMeadowStarlightPlaytestWorld(openPanel: false);
    }

    private void StartMeadowStarlightRestoredPlaytest()
    {
        PrepareMeadowStarlightPlaytest(complete: true);
        StartMeadowStarlightPlaytestWorld(openPanel: false);
    }

    private void StartMeadowStarlightPanelPlaytest()
    {
        PrepareMeadowStarlightPlaytest(partial: true);
        StartMeadowStarlightPlaytestWorld(openPanel: true);
    }

    private void StartMeadowStarlightPanelEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartMeadowStarlightPanelPlaytest();
    }

    private void PrepareMeadowStarlightPlaytest(
        bool complete = false,
        bool partial = false
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Festival.Restore(new FestivalSave
        {
            Results =
            [
                new FestivalYearResultSave
                {
                    FestivalId =
                        FestivalCatalog.StarharvestMarketFestivalId,
                    Year = 1,
                    ItemIds =
                    [
                        DataCatalog.AuricShootId,
                        DataCatalog.SunvaultGourdId,
                        DataCatalog.CrownstarSaffronId
                    ],
                    Score = 30,
                    AwardId = FestivalCatalog.GoldenCrownAwardId
                }
            ]
        });
        _session.Starlight.Discover(DataCatalog.MeadowStarlightId);
        if (!complete && !partial)
        {
            return;
        }

        var blooms = complete
            ? new[]
            {
                DataCatalog.DawnlaceId,
                DataCatalog.EmberbellId,
                DataCatalog.DuskbellId
            }
            : new[]
            {
                DataCatalog.DawnlaceId,
                DataCatalog.EmberbellId
            };
        foreach (var itemId in blooms)
        {
            _session.Inventory.Add(itemId, 1);
        }

        var bounty = complete
            ? new[]
            {
                DataCatalog.StarhoneyId,
                DataCatalog.StarfeatherEggId,
                DataCatalog.MoonfleeceId,
                DataCatalog.DewhornMilkId
            }
            : new[]
            {
                DataCatalog.StarhoneyId,
                DataCatalog.StarfeatherEggId
            };
        foreach (var itemId in bounty)
        {
            _session.Inventory.Add(itemId, 1);
        }

        _session.ContributeToStarlightNode(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowBloomsNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowBountyNodeId
        );
        _session.Starlight.RefreshRewardUnlocks(new StarlightProgressContext(
            new HashSet<string>(
                [FestivalCatalog.StarharvestMarketFestivalId],
                StringComparer.Ordinal
            )
        ));
    }

    private void StartMeadowStarlightPlaytestWorld(bool openPanel)
    {
        _session.Inventory.Select(0);
        _session.SetPlayerState(
            FarmView.MeadowStarlightCell.X * 16 + 8,
            (FarmView.MeadowStarlightCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openPanel)
        {
            Callable.From(() => OpenStarlightPedestal(
                DataCatalog.MeadowStarlightId
            )).CallDeferred();
        }
    }

    private void StartMeadowPollinationPlaytest()
    {
        PrepareMeadowStarlightPlaytest(complete: true);
        var hiveCell = new GridPosition(27, 13);
        var treeCell = new GridPosition(21, 13);
        var save = _session.Capture();
        save.Orchard.FruitTrees =
        [
            new FruitTreeSave
            {
                X = treeCell.X,
                Y = treeCell.Y,
                TreeId = DataCatalog.MoonplumTreeId,
                AgeNights = DataCatalog.FruitTree(
                    DataCatalog.MoonplumTreeId
                ).MatureAfterNights,
                FruitReady = true
            }
        ];
        save.FarmObjects.Objects =
        [
            new PlacedFarmObjectSave
            {
                X = hiveCell.X,
                Y = hiveCell.Y,
                ItemId = DataCatalog.GlowcombHiveId
            }
        ];
        save.Orchard.Beehives =
        [
            new BeehiveSave
            {
                X = hiveCell.X,
                Y = hiveCell.Y,
                ProgressNights = 1
            }
        ];
        _session.Restore(save);
        _session.Inventory.Select(0);
        _session.SetPlayerState(
            hiveCell.X * 16 + 8,
            (hiveCell.Y - 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void PrepareStarlightPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Starlight.Discover();
    }

    private void StartStarlightPlaytestWorld(bool openPedestal = true)
    {
        if (_title is not null)
        {
            PrepareStarlightPlaytest();
        }

        _session.Inventory.Select(0);
        _session.SetPlayerState(
            FarmView.WoodlandStarlightCell.X * 16 + 8,
            (FarmView.WoodlandStarlightCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openPedestal)
        {
            Callable.From(OpenStarlightPedestal).CallDeferred();
        }
    }

    private void StartEconomyPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        foreach (var cropId in DataCatalog.CropIds)
        {
            _session.Inventory.Add(cropId, 4);
        }
        _session.SetPlayerState(
            FarmView.ShopCell.X * 16 + 8,
            (FarmView.ShopCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenShop).CallDeferred();
    }

    private void StartVillagePlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            10 * 60,
            new GridPosition(97, 45)
        );
    }

    private void StartVillageDialoguePlaytest()
    {
        StartArchivePlaytest(true, false);
    }

    private void StartSelaDialoguePlaytest()
    {
        const int day = 1;
        const int minuteOfDay = 10 * 60;
        var sela = VillageCatalog.CurrentNpc(
            VillageCatalog.SelaId,
            day,
            minuteOfDay
        );
        if (sela is null)
        {
            StartVillagePlaytest();
            return;
        }

        StartVillagePlaytestWorld(
            day,
            minuteOfDay,
            new GridPosition(sela.Position.X, sela.Position.Y + 1)
        );
        sela = PlacePlayerAdjacentForPlaytest(
            _session.Village.CurrentNpcs(
                    day,
                    minuteOfDay,
                    sela.LocationId,
                    _session.PlayerCell
                )
                .Single(state =>
                    state.Definition.Id == VillageCatalog.SelaId
                )
        );
        Callable.From(
            () => TalkToVillager(sela.Position)
        ).CallDeferred();
    }

    private void StartVillageExpansionPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            14 * 60,
            new GridPosition(97, 55)
        );
    }

    private void StartVillageExpansionArchivePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(1, 12 * 60);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
    }

    private void StartVillageExpansionDialogueEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartVillageExpansionFocusPlaytest(openDialogue: true,
            wrongTool: false);
    }

    private void StartVillageExpansionWrongToolPlaytest() =>
        StartVillageExpansionFocusPlaytest(
            openDialogue: false,
            wrongTool: true
        );

    private void StartVillageExpansionFocusPlaytest(
        bool openDialogue,
        bool wrongTool
    )
    {
        const int day = 1;
        const int minuteOfDay = 17 * 60;
        StartVillagePlaytestWorld(
            day,
            minuteOfDay,
            new GridPosition(97, 55)
        );
        var dorrik = _session.Village.CurrentNpcs(
                day,
                minuteOfDay,
                PlayerLocationIds.World,
                _session.PlayerCell
            )
            .FirstOrDefault(state =>
                state.Definition.Id == VillageCatalog.DorrikId
            );
        if (dorrik is null)
        {
            return;
        }
        dorrik = PlacePlayerAdjacentForPlaytest(dorrik);

        _session.Inventory.Select(wrongTool ? 1 : 0);
        if (openDialogue)
        {
            Callable.From(
                () => TalkToVillager(dorrik.Position)
            ).CallDeferred();
        }
    }

    private void StartNpcPathfindingPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            13 * 60 + 30,
            new GridPosition(104, 61)
        );
    }

    private void StartArchivePlaytest()
    {
        StartArchivePlaytest(false, false);
    }

    private void StartArchiveGiftPlaytest()
    {
        StartArchivePlaytest(true, true);
    }

    private void StartArchiveDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            10 * 60,
            new GridPosition(
                VillageCatalog.MoonlitArchiveDoorCell.X,
                VillageCatalog.MoonlitArchiveDoorCell.Y + 1
            )
        );
    }

    private void StartCropCodexDeskPlaytest() =>
        StartCropCodexPlaytest(
            discoveredCount: 7,
            rewardClaimed: false,
            openPanel: false,
            wrongTool: false
        );

    private void StartCropCodexPartialPlaytest() =>
        StartCropCodexPlaytest(
            discoveredCount: 7,
            rewardClaimed: false,
            openPanel: true,
            wrongTool: false
        );

    private void StartCropCodexRewardReadyPlaytest() =>
        StartCropCodexPlaytest(
            discoveredCount: CompendiumCatalog.CropEntries.Count,
            rewardClaimed: false,
            openPanel: true,
            wrongTool: false
        );

    private void StartCropCodexRewardClaimedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartCropCodexPlaytest(
            discoveredCount: CompendiumCatalog.CropEntries.Count,
            rewardClaimed: true,
            openPanel: true,
            wrongTool: false
        );
    }

    private void StartCropCodexWrongToolPlaytest() =>
        StartCropCodexPlaytest(
            discoveredCount: 7,
            rewardClaimed: false,
            openPanel: false,
            wrongTool: true
        );

    private void StartCropCodexDiscountShopPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Coins = 500;
        save.Collection = CompletedCropCodexSave(rewardClaimed: true);
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = FarmView.ShopCell.X * 16 + 8;
        save.Player.Y = (FarmView.ShopCell.Y + 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenShop).CallDeferred();
    }

    private void StartCropCodexPlaytest(
        int discoveredCount,
        bool rewardClaimed,
        bool openPanel,
        bool wrongTool
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = CompendiumCatalog.CropEntries
                .Take(Math.Clamp(
                    discoveredCount,
                    0,
                    CompendiumCatalog.CropEntries.Count
                ))
                .Select(entry => entry.Id)
                .ToList(),
            ClaimedRewardIds = rewardClaimed
                ? [CollectionRewardIds.MoonlitAlmanac]
                : []
        };
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = wrongTool ? 1 : 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        if (openPanel)
        {
            Callable.From(() => OpenCropCodex(
                VillageCatalog.MoonlitArchiveDeskCell
            )).CallDeferred();
        }
    }

    private static CollectionSave CompletedCropCodexSave(
        bool rewardClaimed
    ) => new()
    {
        Initialized = true,
        InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
        DiscoveredEntryIds = CompendiumCatalog.CropEntries
            .Select(entry => entry.Id)
            .ToList(),
        ClaimedRewardIds = rewardClaimed
            ? [CollectionRewardIds.MoonlitAlmanac]
            : []
    };

    private void StartCookingCodexUnknownPlaytest() =>
        StartCookingCodexPlaytest(0, rewardClaimed: false);

    private void StartCookingCodexPartialPlaytest() =>
        StartCookingCodexPlaytest(2, rewardClaimed: false);

    private void StartCookingCodexRewardReadyPlaytest() =>
        StartCookingCodexPlaytest(
            CompendiumCatalog.CookingEntries.Count,
            rewardClaimed: false
        );

    private void StartCookingCodexRewardClaimedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartCookingCodexPlaytest(
            CompendiumCatalog.CookingEntries.Count,
            rewardClaimed: true
        );
    }

    private void StartCookingCodexRewardMealsEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareCottageSecondUpgradePlaytest(
            CompletedCottageSecondUpgradeProject()
        );
        foreach (var entry in CompendiumCatalog.CookingEntries)
        {
            _session.Collection.RecordObtainedItem(entry.ItemId);
        }
        _ = _session.Collection.ClaimReward(
            CollectionRewardIds.MoonhearthRecipeJournal
        );
        PositionAtCottageKitchen();
        ShowCottage(false);
        Callable.From(OpenCookedDishes).CallDeferred();
    }

    private void StartCookingCodexPlaytest(
        int discoveredCount,
        bool rewardClaimed
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = CompendiumCatalog.CookingEntries
                .Take(Math.Clamp(
                    discoveredCount,
                    0,
                    CompendiumCatalog.CookingEntries.Count
                ))
                .Select(entry => entry.Id)
                .ToList(),
            ClaimedRewardIds = rewardClaimed
                ? [CollectionRewardIds.MoonhearthRecipeJournal]
                : []
        };
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(() => OpenCompendium(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionCategoryIds.Cooking
        )).CallDeferred();
    }

    private void StartArtisanCodexUnknownPlaytest() =>
        StartArtisanCodexPlaytest(0, rewardClaimed: false);

    private void StartArtisanCodexPartialPlaytest() =>
        StartArtisanCodexPlaytest(2, rewardClaimed: false);

    private void StartArtisanCodexRewardReadyPlaytest() =>
        StartArtisanCodexPlaytest(
            CompendiumCatalog.ArtisanEntries.Count,
            rewardClaimed: false
        );

    private void StartArtisanCodexRewardClaimedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartArtisanCodexPlaytest(
            CompendiumCatalog.ArtisanEntries.Count,
            rewardClaimed: true
        );
    }

    private void StartArtisanCodexRewardShippingEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Collection = ArtisanCodexSave(
            CompendiumCatalog.ArtisanEntries.Count,
            rewardClaimed: true
        );
        save.Shipping.Pending = CompendiumCatalog.ArtisanEntries
            .Select(entry => new ShippingEntrySave
            {
                ItemId = entry.ItemId,
                Count = 1
            })
            .ToList();
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = FarmView.ShippingCell.X * 16 + 8;
        save.Player.Y = (FarmView.ShippingCell.Y + 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenShipping).CallDeferred();
    }

    private void StartArtisanCodexPlaytest(
        int discoveredCount,
        bool rewardClaimed
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Collection = ArtisanCodexSave(
            discoveredCount,
            rewardClaimed
        );
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(() => OpenCompendium(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionCategoryIds.Artisan
        )).CallDeferred();
    }

    private static CollectionSave ArtisanCodexSave(
        int discoveredCount,
        bool rewardClaimed
    ) => new()
    {
        Initialized = true,
        InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
        DiscoveredEntryIds = CompendiumCatalog.ArtisanEntries
            .Take(Math.Clamp(
                discoveredCount,
                0,
                CompendiumCatalog.ArtisanEntries.Count
            ))
            .Select(entry => entry.Id)
            .ToList(),
        ClaimedRewardIds = rewardClaimed
            ? [CollectionRewardIds.StarlitAppraisalLedger]
            : []
    };

    private void StartSeasonalForagePlaytest() =>
        StartSeasonalForagePlaytest(
            day: 1,
            weatherId: DataCatalog.ClearWeatherId,
            wrongTool: false,
            mapUnlocked: false
        );

    private void StartSeasonalForageWrongToolPlaytest() =>
        StartSeasonalForagePlaytest(
            day: 15,
            weatherId: DataCatalog.RainWeatherId,
            wrongTool: true,
            mapUnlocked: false
        );

    private void StartSeasonalForageStardustMapPlaytest() =>
        StartSeasonalForagePlaytest(
            day: 29,
            weatherId: DataCatalog.StardustWindWeatherId,
            wrongTool: false,
            mapUnlocked: true
        );

    private void StartSeasonalForagePlaytest(
        int day,
        string weatherId,
        bool wrongTool,
        bool mapUnlocked
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var spawns = ForageSystem.Generate(day, weatherId);
        var focus = spawns[0];
        var approach = new[]
        {
            new GridPosition(focus.Cell.X, focus.Cell.Y - 1),
            new GridPosition(focus.Cell.X + 1, focus.Cell.Y),
            new GridPosition(focus.Cell.X, focus.Cell.Y + 1),
            new GridPosition(focus.Cell.X - 1, focus.Cell.Y)
        }.First(cell => !WorldDefinition.IsBlocked(cell));
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = weatherId
        };
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = mapUnlocked
                ? CompendiumCatalog.ForageEntries
                    .Select(entry => entry.Id)
                    .ToList()
                : [],
            ClaimedRewardIds = mapUnlocked
                ? [CollectionRewardIds.StarpathForagersGuide]
                : []
        };
        save.Exploration.DiscoveredChunks = spawns
            .Select(spawn => WorldDefinition.ChunkId(
                WorldDefinition.GetChunk(spawn.Cell)
            ))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = approach.X * 16 + 8;
        save.Player.Y = approach.Y * 16 + 8;
        save.Player.SelectedSlot = wrongTool ? 1 : 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartForageCodexPartialPlaytest() =>
        StartForageCodexPlaytest(4, rewardClaimed: false);

    private void StartForageCodexRewardReadyPlaytest() =>
        StartForageCodexPlaytest(
            CompendiumCatalog.ForageEntries.Count,
            rewardClaimed: false
        );

    private void StartForageCodexRewardClaimedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartForageCodexPlaytest(
            CompendiumCatalog.ForageEntries.Count,
            rewardClaimed: true
        );
    }

    private void StartForageCodexPlaytest(
        int discoveredCount,
        bool rewardClaimed
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = CompendiumCatalog.ForageEntries
                .Take(Math.Clamp(
                    discoveredCount,
                    0,
                    CompendiumCatalog.ForageEntries.Count
                ))
                .Select(entry => entry.Id)
                .ToList(),
            ClaimedRewardIds = rewardClaimed
                ? [CollectionRewardIds.StarpathForagersGuide]
                : []
        };
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(() => OpenCompendium(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionCategoryIds.Forage
        )).CallDeferred();
    }

}
