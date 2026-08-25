using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private void StartFishingPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerState(38 * 16 + 8, 20 * 16 + 8, false);
        _session.Inventory.Select(5);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartFishingMinigamePlaytest()
    {
        StartFishingPlaytest();
        Callable.From(() => OpenFishingMinigame(new GridPosition(38, 21)))
            .CallDeferred();
    }

    private void StartFishingGearPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Coins = 1800;
        save.Fishing.Experience = 150;
        save.Fishing.Level = 3;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenFishingGear).CallDeferred();
    }

    private void StartFishingCollectionPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Fishing.CaughtFishIds = DataCatalog.FishItemIds
            .Take(8)
            .ToList();
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenFishingCollection).CallDeferred();
    }

    private void StartFishingDonationPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 17 * 16 + 8;
        save.Player.SelectedSlot = 0;
        var previewFishIds = DataCatalog.FishItemIds.Take(3).ToArray();
        save.Fishing.CaughtFishIds = previewFishIds.ToList();
        save.Fishing.DonatedFishIds = [previewFishIds[0]];
        _session.Restore(save);
        _session.Inventory.Add(previewFishIds[1], 1);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(OpenFishingDonation).CallDeferred();
    }

    private void StartFishCodexPartialPlaytest() =>
        StartFishCodexPlaytest(12);

    private void StartFishCodexCompleteEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartFishCodexPlaytest(CompendiumCatalog.FishEntries.Count);
    }

    private void StartFishCodexPlaytest(int discoveredCount)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var discovered = CompendiumCatalog.FishEntries
            .Take(Math.Clamp(
                discoveredCount,
                0,
                CompendiumCatalog.FishEntries.Count
            ))
            .Select(entry => entry.Id)
            .ToList();
        var save = _session.Capture();
        save.Day = 15;
        save.MinuteOfDay = 10 * 60;
        save.Fishing.CaughtFishIds = discovered.ToList();
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = discovered
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
            CollectionCategoryIds.Fish
        )).CallDeferred();
    }

    private void StartCrystalGrottoEntryPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Select(0);
        _session.SetPlayerLocation(
            CrystalGrottoSurveyLayout.WorldReturnCell.X * 16 + 8,
            CrystalGrottoSurveyLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartCrystalGrottoBasicPlaytest()
    {
        PrepareCrystalGrottoPlaytest(
            new GridPosition(23, 15),
            selectedSlot: 1
        );
    }

    private void StartCrystalGrottoUpgradePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Coins = 600;
        save.Player.LocationId = PlayerLocationIds.CrystalGrottoSurvey;
        save.Player.X = 17 * 16 + 8;
        save.Player.Y = 15 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _session.Inventory.Add(DataCatalog.LumenSlateOreId, 6);
        _session.Inventory.Add(DataCatalog.MoonveinOreId, 3);
        _playing = true;
        EnsureHud();
        ShowCrystalGrotto(false);
        Callable.From(() => OpenToolUpgrade(
            CrystalGrottoSurveyLayout.UpgradeBenchCell
        )).CallDeferred();
    }

    private void StartCrystalGrottoDeepPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Player.LocationId = PlayerLocationIds.CrystalGrottoSurvey;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 7 * 16 + 8;
        save.Player.SelectedSlot = 1;
        save.Mining = new MiningSave { DeepestRoomReached = 4 };
        save.ToolProgression = CompletedBronzeStarShovelSave();
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowCrystalGrotto(false);
    }

    private void StartDeepMinePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerLocation(
            CrystalGrottoSurveyLayout.DepthAnchorCell.X * 16 + 8,
            (CrystalGrottoSurveyLayout.DepthAnchorCell.Y + 1) * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        _session.DeepMine.Start(_session.Clock.Day, _session.Inventory);
        _playing = true;
        EnsureHud();
        ShowCrystalGrotto(false);
        Callable.From(OpenDeepMine).CallDeferred();
    }

    private void StartMineralCodexCompleteEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = CompendiumCatalog.MineralEntries
                .Select(entry => entry.Id)
                .ToList()
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
            CollectionCategoryIds.Minerals
        )).CallDeferred();
    }

    private void StartCrystalValeStarlightPanelPlaytest()
    {
        PrepareCrystalValeStarlightPlaytest(restored: false);
        Callable.From(() => OpenStarlightPedestal(
            DataCatalog.CrystalValeStarlightId
        )).CallDeferred();
    }

    private void StartCrystalValeStarlightRestoredPlaytest() =>
        PrepareCrystalValeStarlightPlaytest(restored: true);

    private void StartStarfallRuinsEntryPlaytest()
    {
        var save = PrepareStarfallRuinsPlaytestSave();
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = StarfallRuinsTrialLayout.WorldReturnCell.X * 16 + 8;
        save.Player.Y = StarfallRuinsTrialLayout.WorldReturnCell.Y * 16 + 8;
        save.Player.SelectedSlot = 0;
        RestoreWorldPlaytest(save);
    }

    private void StartStarfallRuinsCombatPlaytest()
    {
        var save = PrepareStarfallRuinsPlaytestSave();
        save.Player.LocationId = PlayerLocationIds.StarfallRuinsTrial;
        save.Player.X = 6 * 16 + 8;
        save.Player.Y = 15 * 16 + 8;
        save.StarfallRuinsTrial.WeaponClaimed = true;
        RestoreStarfallRuinsPlaytest(save);
        _session.Inventory.Add(DataCatalog.MoonsteelShortbladeId, 1);
        _session.Inventory.PromoteToHotbar(
            DataCatalog.MoonsteelShortbladeId
        );
    }

    private void StartStarfallRuinsArtifactsPlaytest()
    {
        var save = PrepareStarfallRuinsPlaytestSave();
        save.Player.LocationId = PlayerLocationIds.StarfallRuinsTrial;
        save.Player.X = 22 * 16 + 8;
        save.Player.Y = 17 * 16 + 8;
        save.StarfallRuinsTrial.WeaponClaimed = true;
        save.StarfallRuinsTrial.ClearedRoomIds =
            StarfallRuinsTrialCatalog.Rooms
                .Select(room => room.Id)
                .ToList();
        RestoreStarfallRuinsPlaytest(save);
        _session.Inventory.Add(DataCatalog.MoonsteelShortbladeId, 1);
        _session.Inventory.Select(0);
    }

    private void StartArtifactCodexDonationEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var artifactIds = CompendiumCatalog.ArtifactEntries
            .Select(entry => entry.Id)
            .ToList();
        var save = _session.Capture();
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = artifactIds,
            DonatedEntryIds = artifactIds.Skip(2).ToList()
        };
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        foreach (var artifactId in artifactIds.Take(2))
        {
            _session.Inventory.Add(artifactId, 1);
        }
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(() => OpenCompendium(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionCategoryIds.Artifacts
        )).CallDeferred();
    }

    private void StartStarfallRuinsStarlightPanelPlaytest()
    {
        PrepareStarfallRuinsStarlightPlaytest(restored: false);
        Callable.From(() => OpenStarlightPedestal(
            DataCatalog.StarfallRuinsStarlightId
        )).CallDeferred();
    }

    private void StartStarfallRuinsStarlightRestoredPlaytest() =>
        PrepareStarfallRuinsStarlightPlaytest(restored: true);

    private void StartSixfoldStarGatePlaytest() =>
        PrepareSixfoldStarGatePlaytest(openPanel: false);

    private void StartSixfoldStarGatePanelPlaytest() =>
        PrepareSixfoldStarGatePlaytest(openPanel: true);

    private void StartStellarConvergencePlaytest()
    {
        PrepareSixfoldStarGatePlaytest(openPanel: false);
        var save = _session.Capture();
        save.FarmingSkill.Experience = FarmingSkillCatalog.Levels[^1]
            .RequiredExperience;
        save.GatheringSkill.Experience = GatheringSkillCatalog
            .LevelThresholds[^1];
        save.Fishing.Experience = FishingProgressionCatalog
            .LevelThresholds[^1];
        save.Mining.CrystalMiningSkill.Experience = AdventureSkillCatalog
            .LevelThresholds[^1];
        save.Mining.NightwatchSkill.Experience = AdventureSkillCatalog
            .LevelThresholds[^1];
        _session.Restore(save);
        BeginMainStoryFinale();
    }

    private void StartAccessibilitySettingsPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _settings.FontScalePercent = 120;
        AccessibilityRuntime.Apply(_settings, GetTree().Root);
        _theme = ThemeFactory.CreateTheme();
        _session.NewGame(_locale.CurrentLocale);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenSettings).CallDeferred();
    }

    private void PrepareSixfoldStarGatePlaytest(bool openPanel)
    {
        PrepareStarfallRuinsStarlightPlaytest(restored: true);
        var save = _session.Capture();
        save.Construction = new ConstructionSave
        {
            Projects =
            [
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                    Completed = true
                },
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadWorkshopProjectId,
                    Completed = true
                },
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadGreenhouseProjectId,
                    Completed = true
                },
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog.CottageSecondUpgradeId,
                    Completed = true
                },
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .SixfoldStarGateProjectId,
                    Completed = true
                }
            ]
        };
        save.StarGate = new StarGateSave { Activated = true };
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = FarmLayout.StarGateCell.X * 16 + 8;
        save.Player.Y = (FarmLayout.StarGateCell.Y - 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        ShowFarm(false);
        if (openPanel)
        {
            Callable.From(OpenStarGate).CallDeferred();
        }
    }

    private GameSaveV1 PrepareStarfallRuinsPlaytestSave()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 29;
        save.MinuteOfDay = 18 * 60;
        save.Mining = new MiningSave
        {
            DeepestRoomReached = CrystalGrottoSurveyLayout.RoomCount
        };
        save.ToolProgression = CompletedBronzeStarShovelSave();
        save.Festival.Results =
        [
            new FestivalYearResultSave
            {
                FestivalId =
                    FestivalCatalog.GleamrisePlantingFestivalId,
                Year = 1,
                Score = 1
            }
        ];
        save.Starlight = new StarlightSave
        {
            Pedestals = DataCatalog.StarlightPedestals.Values
                .Where(definition => definition.Id !=
                    DataCatalog.StarfallRuinsStarlightId)
                .Select(CompletedPedestalSave)
                .ToList()
        };
        return save;
    }

    private void PrepareStarfallRuinsStarlightPlaytest(bool restored)
    {
        var save = PrepareStarfallRuinsPlaytestSave();
        var artifactIds = CompendiumCatalog.ArtifactEntries
            .Select(entry => entry.Id)
            .ToList();
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = artifactIds
                .Concat(CompendiumCatalog.EnemyEntries.Select(entry => entry.Id))
                .ToList(),
            DonatedEntryIds = artifactIds.Take(3).ToList()
        };
        save.Village = new VillageSave
        {
            MetNpcIds = [VillageCatalog.KaelId, VillageCatalog.LioraId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.KaelId,
                    Points = 60
                },
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = 60
                }
            ]
        };
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = WorldDefinition.StarfallRuinsStarlightCell.X * 16 + 8;
        save.Player.Y =
            (WorldDefinition.StarfallRuinsStarlightCell.Y + 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        RestoreWorldPlaytest(save);
        _session.Starlight.Discover(DataCatalog.StarfallRuinsStarlightId);
        if (restored)
        {
            _session.ActivateStarlightPedestal(
                DataCatalog.StarfallRuinsStarlightId,
                WorldDefinition.StarfallRuinsStarlightCell
            );
        }
    }

    private static StarlightPedestalSave CompletedPedestalSave(
        StarlightPedestalDefinition definition
    ) => new()
    {
        PedestalId = definition.Id,
        Discovered = true,
        RewardUnlocked = true,
        Nodes = definition.Nodes
            .Where(node => node.SourceKind ==
                StarlightNodeSourceKind.Inventory)
            .Select(node =>
            {
                var remaining = node.RequiredCount;
                var contributions = new List<StarlightContributionSave>();
                foreach (var option in node.Options)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }
                    var count = Math.Min(remaining, option.MaximumCount);
                    contributions.Add(new StarlightContributionSave
                    {
                        ItemId = option.ItemId,
                        Count = count
                    });
                    remaining -= count;
                }
                return new StarlightNodeSave
                {
                    NodeId = node.Id,
                    Contributions = contributions
                };
            })
            .ToList()
    };

    private void RestoreWorldPlaytest(GameSaveV1 save)
    {
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void RestoreStarfallRuinsPlaytest(GameSaveV1 save)
    {
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowStarfallRuinsTrial(false);
    }

    private void PrepareCrystalGrottoPlaytest(
        GridPosition playerCell,
        int selectedSlot
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerLocation(
            playerCell.X * 16 + 8,
            playerCell.Y * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        _session.Inventory.Select(selectedSlot);
        _playing = true;
        EnsureHud();
        ShowCrystalGrotto(false);
    }

    private void PrepareCrystalValeStarlightPlaytest(bool restored)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Mining = new MiningSave
        {
            DeepestRoomReached = CrystalGrottoSurveyLayout.RoomCount
        };
        save.ToolProgression = CompletedBronzeStarShovelSave();
        _session.Restore(save);
        _session.Starlight.Discover(DataCatalog.CrystalValeStarlightId);
        foreach (var itemId in MiningCatalog.Minerals.Select(
                     mineral => mineral.ItemId
                 ))
        {
            _session.Inventory.Add(itemId, 1);
        }

        if (restored)
        {
            _session.ContributeToStarlightNode(
                DataCatalog.CrystalValeStarlightId,
                DataCatalog.CrystalValeMineralChorusNodeId
            );
        }

        _session.Inventory.Select(0);
        _session.SetPlayerLocation(
            WorldDefinition.CrystalWellCell.X * 16 + 8,
            (WorldDefinition.CrystalWellCell.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private static ToolProgressionSave CompletedBronzeStarShovelSave() =>
        new()
        {
            Tools =
            [
                new ToolProgressionEntrySave
                {
                    ToolId = DataCatalog.ShovelId,
                    TierId = ToolProgressionCatalog.BronzeStarTierId
                }
            ]
        };

    private void StartMoonwaterStarlightPanelPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 15;
        save.MinuteOfDay = 18 * 60;
        save.Weather = new WeatherSave
        {
            Day = 15,
            CurrentId = DataCatalog.RainWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        _session.Restore(save);
        _session.Starlight.Discover(DataCatalog.MoonwaterStarlightId);
        foreach (var itemId in new[]
        {
            DataCatalog.MoonwaterMinnowId,
            DataCatalog.MarshveilKilliId,
            DataCatalog.RainveilLampreyId
        })
        {
            _session.Inventory.Add(itemId, 1);
        }
        _session.ContributeToStarlightNode(
            DataCatalog.MoonwaterStarlightId,
            DataCatalog.MoonwaterLocalFishNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.MoonwaterStarlightId,
            DataCatalog.MoonwaterWeatherFishNodeId
        );
        _session.SetPlayerState(
            WorldDefinition.MoonwaterStarlightCell.X * 16 + 8,
            (WorldDefinition.MoonwaterStarlightCell.Y + 1) * 16 + 8,
            false
        );
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(() => OpenStarlightPedestal(
            DataCatalog.MoonwaterStarlightId
        )).CallDeferred();
    }

    private void StartLioraEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.LioraFadedReturnRouteId
        );
    }

    private void StartLioraEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.LioraRememberedWayHomeId
        );
    }

    private void StartArchivePlaytest(
        bool openDialogue,
        bool giveGift
    )
    {
        const int day = 1;
        const int minuteOfDay = 10 * 60;
        var liora = VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            day,
            minuteOfDay
        );
        if (liora is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            17 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
        liora = PlacePlayerAdjacentForPlaytest(liora);
        _session.Inventory.Select(0);
        if (giveGift)
        {
            _session.Inventory.Add(DataCatalog.MoonrootId, 2);
            _session.Inventory.PromoteToHotbar(
                DataCatalog.MoonrootId
            );
        }

        _playing = true;
        EnsureHud();
        ShowArchive(false);
        if (openDialogue)
        {
            Callable.From(
                () => TalkToVillager(liora.Position)
            ).CallDeferred();
        }
    }

    private void StartWorkshopDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            10 * 60,
            new GridPosition(
                VillageCatalog.MoonstoneWorkshopDoorCell.X,
                VillageCatalog.MoonstoneWorkshopDoorCell.Y + 1
            )
        );
    }

    private void StartTaviEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.TaviCrackedMoonRuneId
        );
    }

    private void StartTaviEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.TaviMendedLightId
        );
    }

    private void StartNemiEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NemiUndeliverableLetterId
        );
    }

    private void StartNemiEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NemiStarChartRouteId
        );
    }

    private void StartKaelEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.KaelBrokenBlueRuneId
        );
    }

    private void StartKaelEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.KaelSafeReturnRouteId
        );
    }

    private void StartSelaEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.SelaTemperedStarlightId
        );
    }

    private void StartSelaEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.SelaSharedForgeRhythmId
        );
    }

    private void StartOrinEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.OrinUnpricedWaybillId
        );
    }

    private void StartOrinEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.OrinSharedLanternRouteId
        );
    }

    private void StartElowenEventOnePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.ElowenTideMarksAtTheWellId
        );

    private void StartElowenEventTwoPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.ElowenWaterlineReadTogetherId
        );

    private void StartVessaEventOnePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.VessaBitterLeafWarmCupId
        );

    private void StartVessaEventTwoPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.VessaPathThatListensBackId
        );

    private void StartVessaEventWrongToolPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.VessaBitterLeafWarmCupId,
            wrongTool: true
        );

    private void StartRelationshipMailsEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareMailPlaytest(
            [
                RelationshipMail(MailCatalog.KaelKindredId),
                RelationshipMail(MailCatalog.SelaKindredId),
                RelationshipMail(MailCatalog.ElowenKindredId),
                RelationshipMail(MailCatalog.VessaKindredId),
                RelationshipMail(MailCatalog.OrinKindredId)
            ]
        );
        StartMailPlaytestWorld(true);
    }

    private void StartVillageExpansionWave3Playtest()
    {
        StartVillagePlaytestWorld(
            1,
            14 * 60,
            new GridPosition(97, 55)
        );
    }

    private void StartVillageExpansionWave3IndoorPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(1, 11 * 60);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        _session.Inventory.Select(0);
        var yvara = _session.Village.CurrentNpcs(
                1,
                11 * 60,
                PlayerLocationIds.TwilightEmporium,
                _session.PlayerCell
            )
            .FirstOrDefault(state =>
                state.Definition.Id == VillageCatalog.YvaraId
            );
        if (yvara is not null)
        {
            PlacePlayerAdjacentForPlaytest(yvara);
        }
        _playing = true;
        EnsureHud();
        ShowTwilightEmporium(false);
    }

    private void StartVillageExpansionWave3DialogueEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartVillageExpansionWave3FocusPlaytest(
            openDialogue: true,
            wrongTool: false
        );
    }

    private void StartVillageExpansionWave3WrongToolPlaytest() =>
        StartVillageExpansionWave3FocusPlaytest(
            openDialogue: false,
            wrongTool: true
        );

    private void StartVillageExpansionWave3FocusPlaytest(
        bool openDialogue,
        bool wrongTool
    )
    {
        const int day = 1;
        const int minuteOfDay = 14 * 60;
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerState(
            97 * 16 + 8,
            55 * 16 + 8,
            false
        );
        var yvara = _session.Village.CurrentNpcs(
                day,
                minuteOfDay,
                PlayerLocationIds.World,
                _session.PlayerCell
            )
            .FirstOrDefault(state =>
                state.Definition.Id == VillageCatalog.YvaraId
            );
        if (yvara is null)
        {
            StartVillagePlaytest();
            return;
        }
        yvara = PlacePlayerAdjacentForPlaytest(yvara);
        _session.Inventory.Select(wrongTool ? 1 : 0);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openDialogue)
        {
            Callable.From(
                () => TalkToVillager(yvara.Position)
            ).CallDeferred();
        }
    }

    private void StartYvaraEventOnePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.YvaraSeedsBeyondTheCalendarId
        );

    private void StartYvaraEventTwoPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.YvaraASeasonCarriedGentlyId
        );

    private void StartWave3RelationshipMailsEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareMailPlaytest(
            [
                RelationshipMail(MailCatalog.YvaraKindredId),
                RelationshipMail(MailCatalog.BrialKindredId),
                RelationshipMail(MailCatalog.PavriKindredId),
                RelationshipMail(MailCatalog.RovenKindredId)
            ]
        );
        StartMailPlaytestWorld(true);
    }

    private static MailEntrySave RelationshipMail(string mailId) => new()
    {
        MailId = mailId,
        DeliveredDay = 3
    };

    private void StartCatalogCharacterEventPlaytest(
        string eventId,
        bool wrongTool = false
    )
    {
        var definition = CharacterEventCatalog.ById[eventId];
        var trigger = FindCharacterEventTrigger(definition);
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = trigger.Day;
        save.MinuteOfDay = trigger.Minute;
        save.Player.LocationId = definition.RequiredLocationId;
        save.Player.X = 8;
        save.Player.Y = 8;
        save.Village = new VillageSave
        {
            MetNpcIds = [definition.NpcId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = definition.NpcId,
                    Points = definition.RequiredRelationshipPoints,
                    LastTalkDay = trigger.Day
                }
            ]
        };
        var prerequisiteIds = CharacterEventPrerequisiteIds(definition);
        if (prerequisiteIds.Count > 0)
        {
            save.CharacterEvents = new CharacterEventSave
            {
                Entries = prerequisiteIds.Select((id, index) =>
                    new CharacterEventEntrySave
                    {
                        EventId = id,
                        CompletedDay = trigger.Day -
                            prerequisiteIds.Count + index
                    }
                ).ToList()
            };
        }
        _session.Restore(save);
        _session.Inventory.Select(wrongTool ? 1 : 0);

        var npc = _session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId
            )
            .FirstOrDefault(state =>
                state.Definition.Id == definition.NpcId
            );
        if (npc is null)
        {
            StartVillagePlaytest();
            return;
        }
        npc = PlacePlayerAdjacentForPlaytest(npc);

        _playing = true;
        EnsureHud();
        ShowCharacterEventLocation(definition.RequiredLocationId);
        if (!wrongTool)
        {
            var target = npc.Position;
            Callable.From(() => TalkToVillager(target)).CallDeferred();
        }
    }

    private VillageNpcState PlacePlayerAdjacentForPlaytest(
        VillageNpcState npc
    )
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var current = _session.Village.CurrentNpcs(
                    _session.Clock.Day,
                    _session.Clock.MinuteOfDay,
                    npc.LocationId,
                    _session.PlayerCell
                )
                .Single(state =>
                    state.Definition.Id == npc.Definition.Id
                );
            var occupied = _session.Village.CurrentNpcs(
                    _session.Clock.Day,
                    _session.Clock.MinuteOfDay,
                    npc.LocationId,
                    _session.PlayerCell
                )
                .Where(state =>
                    state.Definition.Id != npc.Definition.Id
                )
                .Select(state => state.Position)
                .ToHashSet();
            var approach = new[]
                {
                    new GridPosition(
                        current.Position.X,
                        current.Position.Y + 1
                    ),
                    new GridPosition(
                        current.Position.X - 1,
                        current.Position.Y
                    ),
                    new GridPosition(
                        current.Position.X + 1,
                        current.Position.Y
                    ),
                    new GridPosition(
                        current.Position.X,
                        current.Position.Y - 1
                    )
                }
                .First(candidate =>
                    NpcNavigationMap.IsWalkableGeometry(
                        npc.LocationId,
                        candidate
                    ) &&
                    !NpcNavigationMap.IsCriticalEntranceCell(
                        npc.LocationId,
                        candidate
                    ) &&
                    !occupied.Contains(candidate)
                );
            _session.SetPlayerLocation(
                approach.X * 16 + 8,
                approach.Y * 16 + 8,
                npc.LocationId
            );
            var projected = _session.Village.CurrentNpcs(
                    _session.Clock.Day,
                    _session.Clock.MinuteOfDay,
                    npc.LocationId,
                    _session.PlayerCell
                )
                .Single(state =>
                    state.Definition.Id == npc.Definition.Id
                );
            if (Math.Abs(_session.PlayerCell.X - projected.Position.X) +
                Math.Abs(_session.PlayerCell.Y - projected.Position.Y) == 1)
            {
                return projected;
            }
        }

        throw new InvalidOperationException(
            $"Could not place player adjacent to {npc.Definition.Id}."
        );
    }

    private void ShowCharacterEventLocation(string locationId)
    {
        switch (locationId)
        {
            case PlayerLocationIds.World:
                ShowFarm(false);
                break;
            case PlayerLocationIds.MoonlitArchive:
                ShowArchive(false);
                break;
            case PlayerLocationIds.MoonstoneWorkshop:
                ShowWorkshop(false);
                break;
            case PlayerLocationIds.StarweaverTeaHouse:
                ShowTeaHouse(false);
                break;
            case PlayerLocationIds.TwilightEmporium:
                ShowTwilightEmporium(false);
                break;
            case PlayerLocationIds.StarlightPost:
                ShowStarlightPost(false);
                break;
            case PlayerLocationIds.StarfallWatch:
                ShowStarfallWatch(false);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported character event location: {locationId}."
                );
        }
    }

    private static (int Day, int Minute) FindCharacterEventTrigger(
        CharacterEventDefinition definition
    )
    {
        var firstDay = Math.Max(
            5,
            CharacterEventPrerequisiteIds(definition).Count + 1
        );
        var npc = VillageCatalog.Npcs[definition.NpcId];
        for (var day = firstDay;
             day <= CalendarSystem.DaysPerYear;
             day++)
        {
            var weatherId = WeatherSystem.WeatherForDay(day);
            for (var minute = GameClock.StartMinute;
                 minute < GameClock.EndMinute;
                 minute += GameClock.MinutesPerTick)
            {
                var entry = NpcScheduleSystem.SelectEntry(
                    npc,
                    day,
                    minute,
                    weatherId
                );
                if (entry?.LocationId == definition.RequiredLocationId &&
                    entry.DialogueKey == definition.RequiredNpcDialogueKey &&
                    minute >= entry.StartMinute + 60)
                {
                    return (day, minute);
                }
            }
        }

        throw new InvalidOperationException(
            $"No schedule trigger exists for {definition.Id}."
        );
    }

    private void StartWorkshopPlaytest()
    {
        StartWorkshopPlaytest(false);
    }

    private void StartWorkshopTaviPlaytest()
    {
        StartWorkshopPlaytest(true);
    }

    private void StartWorkshopPlaytest(bool giveGift)
    {
        const int day = 1;
        const int minuteOfDay = 10 * 60;
        var tavi = VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            day,
            minuteOfDay
        );
        if (tavi is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.MoonstoneWorkshop
        );
        tavi = PlacePlayerAdjacentForPlaytest(tavi);
        _session.Inventory.Select(0);
        if (giveGift)
        {
            _session.Inventory.Add(DataCatalog.LumenwoodId, 2);
            _session.Inventory.PromoteToHotbar(
                DataCatalog.LumenwoodId
            );
        }

        _playing = true;
        EnsureHud();
        ShowWorkshop(false);
        if (giveGift)
        {
            Callable.From(
                () => TalkToVillager(tavi.Position)
            ).CallDeferred();
        }
    }

    private void StartTeaHouseDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            10 * 60,
            new GridPosition(
                VillageCatalog.StarweaverTeaHouseDoorCell.X,
                VillageCatalog.StarweaverTeaHouseDoorCell.Y + 1
            )
        );
    }

    private void StartTeaHousePlaytest()
    {
        StartTeaHousePlaytest(false);
    }

    private void StartTeaHouseVessaPlaytest()
    {
        StartTeaHousePlaytest(true);
    }

    private void StartTeaHousePlaytest(bool giveGift)
    {
        const int day = 1;
        const int minuteOfDay = 10 * 60;
        var vessa = VillageCatalog.CurrentNpc(
            VillageCatalog.VessaId,
            day,
            minuteOfDay
        );
        if (vessa is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        var playtestSave = _session.Capture();
        playtestSave.Coins = 500;
        _session.Restore(playtestSave);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            10 * 16 + 8,
            PlayerLocationIds.StarweaverTeaHouse
        );
        vessa = PlacePlayerAdjacentForPlaytest(vessa);
        _session.Inventory.Select(0);
        if (giveGift)
        {
            _session.Inventory.Add(DataCatalog.CloudleafId, 2);
            _session.Inventory.PromoteToHotbar(
                DataCatalog.CloudleafId
            );
        }

        _playing = true;
        EnsureHud();
        ShowTeaHouse(false);
        if (giveGift)
        {
            Callable.From(
                () => TalkToVillager(vessa.Position)
            ).CallDeferred();
        }
        else
        {
            Callable.From(
                () => OpenShop(ShopOverlayMode.StarweaverTeaHouse)
            ).CallDeferred();
        }
    }

    private void StartEmporiumDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            10 * 60,
            new GridPosition(
                VillageCatalog.TwilightEmporiumDoorCell.X,
                VillageCatalog.TwilightEmporiumDoorCell.Y + 1
            )
        );
    }

    private void StartEmporiumRestdayDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            CalendarSystem.DaysPerWeek,
            10 * 60,
            new GridPosition(
                VillageCatalog.TwilightEmporiumDoorCell.X,
                VillageCatalog.TwilightEmporiumDoorCell.Y + 1
            )
        );
    }

    private void StartEmporiumRotationPlaytest()
    {
        StartEmporiumPlaytest(false);
        Callable.From(InspectTravelManifest).CallDeferred();
    }

    private void StartEmporiumPlaytest()
    {
        StartEmporiumPlaytest(false);
    }

    private void StartEmporiumOrinPlaytest()
    {
        StartEmporiumPlaytest(true);
    }

    private void StartEmporiumPlaytest(bool openOrinDialogue)
    {
        const int day = 1;
        const int minuteOfDay = 10 * 60;
        var orin = VillageCatalog.CurrentNpc(
            VillageCatalog.OrinId,
            day,
            minuteOfDay
        );
        if (orin is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        orin = PlacePlayerAdjacentForPlaytest(orin);
        _session.Inventory.Select(0);

        _playing = true;
        EnsureHud();
        ShowTwilightEmporium(false);
        if (openOrinDialogue)
        {
            Callable.From(
                () => TalkToVillager(orin.Position)
            ).CallDeferred();
        }
    }

    private void StartStarlightPostDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            9 * 60,
            new GridPosition(
                VillageCatalog.StarlightPostDoorCell.X,
                VillageCatalog.StarlightPostDoorCell.Y + 1
            )
        );
    }

    private void StartStarlightPostPlaytest()
    {
        StartStarlightPostPlaytest(false, 0);
    }

    private void StartStarlightPostDeliveryPlaytest()
    {
        StartStarlightPostPlaytest(false, 0);
        Callable.From(OpenPostDeliveryBoard).CallDeferred();
    }

    private void StartStarlightPostWrongToolPlaytest()
    {
        StartStarlightPostPlaytest(false, 1);
    }

    private void StartStarlightPostNemiPlaytest()
    {
        StartStarlightPostPlaytest(true, 0);
    }

    private void StartStarlightPostPlaytest(
        bool openNemiDialogue,
        int selectedSlot
    )
    {
        const int day = 1;
        const int minuteOfDay = 12 * 60;
        var nemi = VillageCatalog.CurrentNpc(
            VillageCatalog.NemiId,
            day,
            minuteOfDay
        );
        if (nemi is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            14 * 16 + 8,
            PlayerLocationIds.StarlightPost
        );
        nemi = PlacePlayerAdjacentForPlaytest(nemi);
        _session.Inventory.Select(selectedSlot);

        _playing = true;
        EnsureHud();
        ShowStarlightPost(false);
        if (openNemiDialogue)
        {
            Callable.From(
                () => TalkToVillager(nemi.Position)
            ).CallDeferred();
        }
    }

    private void StartStarfallWatchDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            9 * 60,
            new GridPosition(
                VillageCatalog.StarfallWatchDoorCell.X,
                VillageCatalog.StarfallWatchDoorCell.Y + 1
            )
        );
    }

    private void StartStarfallWatchPlaytest()
    {
        StartStarfallWatchPlaytest(false, 0);
    }

    private void StartStarfallWatchBoardPlaytest()
    {
        StartStarfallWatchPlaytest(false, 0);
        Callable.From(OpenStarfallWatchBoard).CallDeferred();
    }

    private void StartStarfallWatchWrongToolPlaytest()
    {
        StartStarfallWatchPlaytest(false, 1);
    }

    private void StartStarfallWatchKaelPlaytest()
    {
        StartStarfallWatchPlaytest(true, 0);
    }

    private void StartStarfallWatchPlaytest(
        bool openKaelDialogue,
        int selectedSlot
    )
    {
        const int day = 1;
        const int minuteOfDay = 12 * 60;
        var kael = VillageCatalog.CurrentNpc(
            VillageCatalog.KaelId,
            day,
            minuteOfDay
        );
        if (kael is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            14 * 16 + 8,
            PlayerLocationIds.StarfallWatch
        );
        kael = PlacePlayerAdjacentForPlaytest(kael);
        _session.Inventory.Select(selectedSlot);

        _playing = true;
        EnsureHud();
        ShowStarfallWatch(false);
        if (openKaelDialogue)
        {
            Callable.From(
                () => TalkToVillager(kael.Position)
            ).CallDeferred();
        }
    }

    private void StartVillageRestdayEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartVillagePlaytestWorld(
            CalendarSystem.DaysPerWeek,
            14 * 60,
            new GridPosition(97, 50)
        );
    }

    private void StartVillageRainSchedulePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = save.Day,
            CurrentId = DataCatalog.RainWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 27 * 16 + 8;
        save.Player.Y = 13 * 16 + 8;
        save.Village.MetNpcIds = [VillageCatalog.SelaId];
        _session.Restore(save);
        var sela = _session.Village.CurrentNpcs(
                _session.Clock.Day,
                _session.Clock.MinuteOfDay,
                PlayerLocationIds.MoonstoneWorkshop
            )
            .SingleOrDefault(npc =>
                npc.Definition.Id == VillageCatalog.SelaId
            );

        _playing = true;
        EnsureHud();
        ShowWorkshop(false);
        if (sela is not null)
        {
            sela = PlacePlayerAdjacentForPlaytest(sela);
            Callable.From(
                () => TalkToVillager(sela.Position)
            ).CallDeferred();
        }
    }

    private void StartVillageRainveilSchedulePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = CalendarSystem.DaysPerSeason + 1;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = save.Day,
            CurrentId = DataCatalog.ClearWeatherId,
            ForecastId = DataCatalog.RainWeatherId
        };
        save.Player.LocationId = PlayerLocationIds.World;
        save.Village.MetNpcIds = [VillageCatalog.VessaId];
        _session.Restore(save);
        var vessa = _session.Village.CurrentNpcs(
                _session.Clock.Day,
                _session.Clock.MinuteOfDay,
                PlayerLocationIds.World
            )
            .SingleOrDefault(npc =>
                npc.Definition.Id == VillageCatalog.VessaId
            );
        if (vessa is not null)
        {
            vessa = PlacePlayerAdjacentForPlaytest(vessa);
        }

        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (vessa is not null)
        {
            Callable.From(
                () => TalkToVillager(vessa.Position)
            ).CallDeferred();
        }
    }

    private void StartVillagePlaytestWorld(
        int day,
        int minuteOfDay,
        GridPosition playerCell
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerState(
            playerCell.X * 16 + 8,
            playerCell.Y * 16 + 8,
            false
        );
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }


    private void StartStory01WoodlandDiscoveryPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _locale.SetLocale(LocaleService.SimplifiedChinese);
        _session.NewGame(_locale.CurrentLocale);
        _session.Quest.Restore(new QuestSave
        {
            Stage = QuestStage.Complete
        });
        var pedestal = WorldDefinition.WoodlandStarlightCell;
        _session.SetPlayerLocation(
            pedestal.X * 16 + 8,
            (pedestal.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (!_session.UseSelected(pedestal).Succeeded)
        {
            GD.PushError("Could not prepare STORY-01 woodland discovery.");
            return;
        }
        Callable.From(() => OpenStarlightPedestalFromWorld(
            DataCatalog.WoodlandStarlightId
        )).CallDeferred();
    }

    private void StartStory01WoodlandRestorationPlaytest()
    {
        _locale.SetLocale(LocaleService.SimplifiedChinese);
        var save = PrepareStory01WoodlandSave(day: 1);
        save.StarlightStory = Story01Save(
            (StarlightStoryCatalog.WoodlandDiscoveryId, 1)
        );
        RestoreWorldPlaytest(save);
        Callable.From(() =>
        {
            var story = _session.BeginStarlightRestorationStory(
                DataCatalog.WoodlandStarlightId
            );
            if (story is null)
            {
                GD.PushError("Could not prepare STORY-01 restoration story.");
                return;
            }
            ShowStarlightStory(story);
        }).CallDeferred();
    }

    private void StartStory01WoodlandResponsePlaytest()
    {
        _locale.SetLocale(LocaleService.SimplifiedChinese);
        var save = PrepareStory01WoodlandSave(day: 2);
        save.StarlightStory = Story01Save(
            (StarlightStoryCatalog.WoodlandDiscoveryId, 1),
            (StarlightStoryCatalog.WoodlandRestorationId, 1)
        );
        RestoreWorldPlaytest(save);
        Callable.From(() =>
        {
            if (_dialogueOverlay is null)
            {
                GD.PushError("Could not prepare STORY-01 woodland response.");
            }
        }).CallDeferred();
    }

    private void StartStory01WoodlandRevisitEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        var save = PrepareStory01WoodlandSave(day: 2);
        save.MinuteOfDay = 10 * 60;
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Village = new VillageSave
        {
            MetNpcIds = [VillageCatalog.LioraId]
        };
        save.StarlightStory = Story01Save(
            (StarlightStoryCatalog.WoodlandDiscoveryId, 1),
            (StarlightStoryCatalog.WoodlandRestorationId, 1),
            (StarlightStoryCatalog.WoodlandResponseId, 2)
        );
        _session.Restore(save);
        _session.Inventory.Select(0);
        var liora = VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            save.Day,
            save.MinuteOfDay
        );
        if (liora is null || liora.LocationId != PlayerLocationIds.MoonlitArchive)
        {
            GD.PushError("Could not resolve Liora for STORY-01 revisit.");
            return;
        }
        liora = PlacePlayerAdjacentForPlaytest(liora);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        var target = liora.Position;
        Callable.From(() => TalkToVillager(target)).CallDeferred();
    }

    private void StartStory01FinalRevisitEnglishPlaytest() =>
        StartStory01FinalRevisitEnglishPlaytest(1);

    private void StartStory01FinalRevisitEnglishPlaytest(int page)
    {
        _locale.SetLocale(LocaleService.English);
        PrepareStarfallRuinsStarlightPlaytest(restored: true);
        var save = _session.Capture();
        save.Day = 30;
        save.MinuteOfDay = 10 * 60;
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        var orderedNpcs = VillageCatalog.Npcs.Values
            .OrderBy(npc => npc.ScheduleOrder)
            .ToArray();
        save.Village = new VillageSave
        {
            MetNpcIds = orderedNpcs.Select(npc => npc.Id).ToList(),
            Relationships = orderedNpcs.Select((npc, index) =>
                new VillageRelationshipSave
                {
                    NpcId = npc.Id,
                    Points = Math.Max(1, 100 - index)
                }
            ).ToList()
        };
        save.Exploration = new ExplorationSave
        {
            DiscoveredChunks = Enumerable.Range(
                    0,
                    WorldDefinition.ChunkRows
                )
                .SelectMany(y => Enumerable.Range(
                    0,
                    WorldDefinition.ChunkColumns
                ).Select(x => WorldDefinition.ChunkId(
                    new ChunkPosition(x, y)
                )))
                .ToList()
        };
        save.StarlightStory = Story01Save(
            (StarlightStoryCatalog.StarfallRuinsDiscoveryId, 29),
            (StarlightStoryCatalog.StarfallRuinsRestorationId, 29),
            (StarlightStoryCatalog.StarfallRuinsResponseId, 30)
        );
        _session.Restore(save);
        _session.Inventory.Select(0);
        var liora = VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            save.Day,
            save.MinuteOfDay
        );
        if (liora is null || liora.LocationId != PlayerLocationIds.MoonlitArchive)
        {
            GD.PushError("Could not resolve final STORY-01 Liora revisit.");
            return;
        }
        liora = PlacePlayerAdjacentForPlaytest(liora);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        var target = liora.Position;
        Callable.From(() =>
        {
            TalkToVillager(target);
            for (var index = 1; index < page; index++)
            {
                _dialogueOverlay?.AdvanceOnePageForPlaytest();
            }
        }).CallDeferred();
    }

    private void StartStory01HomesteadResponsePlaytest() =>
        StartStory01ResponsePlaytest(
            DataCatalog.HomesteadStarlightId,
            WorldBiome.Home,
            FarmLayout.HomesteadStoryResponseCell,
            StarlightStoryCatalog.HomesteadDiscoveryId,
            StarlightStoryCatalog.HomesteadRestorationId
        );

    private void StartStory01MeadowResponsePlaytest() =>
        StartStory01ResponsePlaytest(
            DataCatalog.MeadowStarlightId,
            WorldBiome.StarfallMeadow,
            WorldDefinition.MeadowStarlightCell,
            StarlightStoryCatalog.MeadowDiscoveryId,
            StarlightStoryCatalog.MeadowRestorationId
        );

    private void StartStory01MoonwaterResponsePlaytest() =>
        StartStory01ResponsePlaytest(
            DataCatalog.MoonwaterStarlightId,
            WorldBiome.MoonwaterWetlands,
            WorldDefinition.MoonwaterStarlightCell,
            StarlightStoryCatalog.MoonwaterDiscoveryId,
            StarlightStoryCatalog.MoonwaterRestorationId
        );

    private void StartStory01CrystalValeResponsePlaytest() =>
        StartStory01ResponsePlaytest(
            DataCatalog.CrystalValeStarlightId,
            WorldBiome.StarfallRuins,
            StarfallRuinsTrialLayout.WorldEntryCell,
            StarlightStoryCatalog.CrystalValeDiscoveryId,
            StarlightStoryCatalog.CrystalValeRestorationId
        );

    private void StartStory01StarfallRuinsResponsePlaytest() =>
        StartStory01ResponsePlaytest(
            DataCatalog.StarfallRuinsStarlightId,
            WorldBiome.LumenVillage,
            FarmLayout.StarGateCell,
            StarlightStoryCatalog.StarfallRuinsDiscoveryId,
            StarlightStoryCatalog.StarfallRuinsRestorationId
        );

    private void StartStory01ResponsePlaytest(
        string pedestalId,
        WorldBiome biome,
        GridPosition responseCell,
        string discoveryId,
        string restorationId
    )
    {
        FreeUi(_title);
        _title = null;
        _locale.SetLocale(LocaleService.SimplifiedChinese);
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 2;
        save.MinuteOfDay = 10 * 60;
        save.Quest.Stage = QuestStage.Complete;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = responseCell.X * 16 + 8;
        save.Player.Y = responseCell.Y * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);

        var definition = DataCatalog.StarlightPedestal(pedestalId);
        _session.Starlight.Restore(
            new StarlightSave
            {
                Pedestals = [CompletedPedestalSave(definition)]
            },
            CompletedStarlightProgressContext(definition)
        );
        _session.SetPlayerLocation(
            responseCell.X * 16 + 8,
            responseCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        _session.StarlightStory.Restore(
            Story01Save((discoveryId, 1), (restorationId, 1)),
            _session.Clock.Day,
            new StarlightStoryProgressContext(
                _session.Clock.Day,
                PlayerLocationIds.World,
                biome,
                new HashSet<string>(
                    [pedestalId],
                    StringComparer.Ordinal
                ),
                new HashSet<string>(
                    [pedestalId],
                    StringComparer.Ordinal
                ),
                new HashSet<string>(StringComparer.Ordinal),
                StarlightStoryProgressContext.ExploredBiomesFrom(
                    _session.Exploration.DiscoveredChunks
                ),
                new HashSet<string>(StringComparer.Ordinal),
                false,
                responseCell
            )
        );

        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private GameSaveV1 PrepareStory01WoodlandSave(int day)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        var pedestal = WorldDefinition.WoodlandStarlightCell;
        save.Day = day;
        save.MinuteOfDay = 10 * 60;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = pedestal.X * 16 + 8;
        save.Player.Y = (pedestal.Y + 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        save.Quest.Stage = QuestStage.Complete;
        save.Starlight = new StarlightSave
        {
            Pedestals =
            [
                CompletedPedestalSave(DataCatalog.WoodlandStarlight)
            ]
        };
        return save;
    }

    private static StarlightStorySave Story01Save(
        params (string BeatId, int CompletedDay)[] entries
    ) => new()
    {
        Entries = entries.Select(entry => new StarlightStoryEntrySave
        {
            BeatId = entry.BeatId,
            CompletedDay = entry.CompletedDay
        }).ToList()
    };

    private static StarlightProgressContext CompletedStarlightProgressContext(
        StarlightPedestalDefinition definition
    ) => new(
        definition.Nodes
            .Where(node =>
                node.SourceKind == StarlightNodeSourceKind.FestivalResults
            )
            .SelectMany(node => node.SourceIds ?? [])
            .ToHashSet(StringComparer.Ordinal),
        definition.Nodes
            .Where(node => node.SourceKind == StarlightNodeSourceKind.Milestones)
            .SelectMany(node => node.SourceIds ?? [])
            .ToHashSet(StringComparer.Ordinal),
        definition.Nodes
            .Where(node =>
                node.SourceKind == StarlightNodeSourceKind.PedestalRewards
            )
            .SelectMany(node => node.SourceIds ?? [])
            .ToHashSet(StringComparer.Ordinal)
    );

    private void StartLioraEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcALioraMarginOfLivingRoutesId
        );

    private void StartLioraEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcALioraFirstUncopiedChartId
        );

    private void StartTaviEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcATaviStoneThatAnswersFootstepsId
        );

    private void StartTaviEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcATaviJointWithRoomToMoveId
        );

    private void StartNemiEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcBNemiDeliveryThatNeededNoAnswerId
        );

    private void StartNemiEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcBNemiHookForHerOwnMailbagId
        );

    private void StartKaelEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcBKaelPatrolLeftUnfinishedOnPurposeId
        );

    private void StartKaelEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcBKaelLastMarkerOnTheReturnBoardId
        );

    private void StartSelaEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcBSelaInstructionsBeyondHerHandsId
        );

    private void StartSelaEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcBSelaHammerFittedToHerHandId
        );

    private void StartHaldenEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcBHaldenBowlThatDidNotNeedEmptyingId
        );

    private void StartHaldenEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcBHaldenBellHeChoseNotToRingId
        );

    private void StartOrinEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcAOrinOrderHeDeclinedId
        );

    private void StartOrinEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcAOrinCaseHeUnpackedId
        );

    private void StartElowenEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcCElowenWaterWithTwoHonestNamesId
        );

    private void StartElowenEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcCElowenMarkerAllowedToDriftId
        );

    private void StartMaveaEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcCMaveaRecipeThatChangedWithTheTableId
        );

    private void StartMaveaEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcCMaveaLastJarOpenedOnAnOrdinaryDayId
        );

    private void StartSivrenEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcCSivrenTwoMemoriesUnderOneDateId
        );

    private void StartSivrenEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcCSivrenFirstPersonFootnoteId
        );

    private void StartDorrikEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcCDorrikMaintenancePathBehindTheBraceId
        );

    private void StartDorrikEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcCDorrikPlanReturnedToItsUsersId
        );

    private void StartYvaraEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcDYvaraTheDaySheLeftTheCaseClosedId
        );

    private void StartYvaraEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcDYvaraASeedRecordInTwoHandsId
        );

    private void StartBrialEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcDBrialTheOrchardRoundWithAnEmptyBasketId
        );

    private void StartBrialEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcDBrialThePruningMarkHeErasedId
        );

    private void StartPavriEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcDPavriTheCuffTestedInMotionId
        );

    private void StartPavriEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcDPavriOneStitchBesideTheOldId
        );

    private void StartRovenEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcDRovenTheCornerPeopleAlreadyChoseId
        );

    private void StartRovenEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcDRovenARouteForAnOrdinaryDayId
        );

    private void StartVessaEventThreePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcAVessaPatchLeftUngatheredId
        );

    private void StartVessaEventFourPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NpcAVessaCupBrewedForHerselfId
        );

    private void StartNpcALioraRainResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.LioraId,
            day: 1,
            DataCatalog.RainWeatherId,
            "village.npc.liora.weather_rain"
        );

    private void StartNpcATaviLongnightResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.TaviId,
            day: 43,
            DataCatalog.ClearWeatherId,
            "village.npc.tavi.season_longnight"
        );

    private void StartNpcAVessaStardustResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.VessaId,
            day: 1,
            DataCatalog.StardustWindWeatherId,
            "village.npc.vessa.weather_stardust"
        );

    private void StartNpcAOrinLongnightSnowResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.OrinId,
            day: 43,
            DataCatalog.LongnightSnowWeatherId,
            "village.npc.orin.weather_longnight_snow"
        );

    private void StartNpcBNemiStardustResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.NemiId,
            day: 1,
            DataCatalog.StardustWindWeatherId,
            "village.npc.nemi.weather_stardust"
        );

    private void StartNpcBKaelLongnightResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.KaelId,
            day: 43,
            DataCatalog.ClearWeatherId,
            "village.npc.kael.season_longnight"
        );

    private void StartNpcBSelaStarharvestResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.SelaId,
            day: 29,
            DataCatalog.ClearWeatherId,
            "village.npc.sela.season_starharvest"
        );

    private void StartNpcBHaldenStardustResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.HaldenId,
            day: 1,
            DataCatalog.StardustWindWeatherId,
            "village.npc.halden.weather_stardust"
        );

    private void StartNpcCElowenRainveilResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.ElowenId,
            day: 15,
            DataCatalog.ClearWeatherId,
            "village.npc.elowen.season_rainveil"
        );

    private void StartNpcCMaveaRainResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.MaveaId,
            day: 1,
            DataCatalog.RainWeatherId,
            "village.npc.mavea.weather_rain"
        );

    private void StartNpcCSivrenStarharvestResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.SivrenId,
            day: 29,
            DataCatalog.ClearWeatherId,
            "village.npc.sivren.season_starharvest"
        );

    private void StartNpcCDorrikRainveilResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.DorrikId,
            day: 15,
            DataCatalog.ClearWeatherId,
            "village.npc.dorrik.season_rainveil"
        );

    private void StartNpcDYvaraRainResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.YvaraId,
            day: 1,
            DataCatalog.RainWeatherId,
            "village.npc.yvara.weather_rain",
            minuteOfDay: 11 * 60
        );

    private void StartNpcDBrialLongnightResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.BrialId,
            day: 43,
            DataCatalog.ClearWeatherId,
            "village.npc.brial.season_longnight",
            minuteOfDay: 10 * 60
        );

    private void StartNpcDPavriRainveilResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.PavriId,
            day: 15,
            DataCatalog.ClearWeatherId,
            "village.npc.pavri.season_rainveil",
            minuteOfDay: 10 * 60
        );

    private void StartNpcDRovenRainResponsePlaytest() =>
        StartNpcConditionResponsePlaytest(
            VillageCatalog.RovenId,
            day: 1,
            DataCatalog.RainWeatherId,
            "village.npc.roven.weather_rain"
        );

    private void StartNpcConditionResponsePlaytest(
        string npcId,
        int day,
        string weatherId,
        string expectedDialogueKey,
        int minuteOfDay = 14 * 60
    )
    {
        FreeUi(_title);
        _title = null;
        _locale.SetLocale(LocaleService.SimplifiedChinese);
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Locale = LocaleService.SimplifiedChinese;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Village = new VillageSave { MetNpcIds = [npcId] };
        var catalogNpc = VillageCatalog.CurrentNpc(
            npcId,
            day,
            minuteOfDay,
            weatherId
        );
        if (catalogNpc is null ||
            catalogNpc.DialogueKey != expectedDialogueKey)
        {
            GD.PushError(
                $"Could not resolve NPC condition response {expectedDialogueKey}."
            );
            return;
        }
        save.Player.LocationId = catalogNpc.LocationId;
        save.Player.X = 8;
        save.Player.Y = 8;
        _session.Restore(save);
        _session.Inventory.Select(0);
        var npc = _session.Village.CurrentNpcs(
                day,
                minuteOfDay,
                catalogNpc.LocationId,
                _session.PlayerCell
            )
            .SingleOrDefault(state => state.Definition.Id == npcId);
        if (npc is null || npc.DialogueKey != expectedDialogueKey)
        {
            GD.PushError(
                $"Could not project NPC condition response {expectedDialogueKey}."
            );
            return;
        }
        npc = PlacePlayerAdjacentForPlaytest(npc);
        _playing = true;
        EnsureHud();
        ShowCharacterEventLocation(catalogNpc.LocationId);
        var target = npc.Position;
        Callable.From(() => TalkToVillager(target)).CallDeferred();
    }

    private void StartNpcAGroupEventPlaytest() =>
        StartNpcAGroupEventPlaytest(LocaleService.SimplifiedChinese);

    private void StartNpcAGroupEventEnglishPlaytest() =>
        StartNpcAGroupEventPlaytest(LocaleService.English);

    private void StartNpcAGroupEventPlaytest(
        string locale,
        int page = 1,
        bool wrongTool = false
    ) => StartGroupCharacterEventPlaytest(
        GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId,
        locale,
        page,
        wrongTool
    );

    private void StartNpcBGroupEventPlaytest() =>
        StartNpcBGroupEventPlaytest(LocaleService.SimplifiedChinese);

    private void StartNpcBGroupEventEnglishPlaytest() =>
        StartNpcBGroupEventPlaytest(LocaleService.English);

    private void StartNpcBGroupEventPlaytest(
        string locale,
        int page = 1,
        bool wrongTool = false
    ) => StartGroupCharacterEventPlaytest(
        GroupCharacterEventCatalog.NpcBLastLampWaitsForReturnId,
        locale,
        page,
        wrongTool
    );

    private void StartNpcCGroupEventPlaytest() =>
        StartNpcCGroupEventPlaytest(LocaleService.SimplifiedChinese);

    private void StartNpcCGroupEventEnglishPlaytest() =>
        StartNpcCGroupEventPlaytest(LocaleService.English);

    private void StartNpcCGroupEventPlaytest(
        string locale,
        int page = 1,
        bool wrongTool = false
    ) => StartGroupCharacterEventPlaytest(
        GroupCharacterEventCatalog.NpcCOneOpenCornerFourUsesId,
        locale,
        page,
        wrongTool
    );

    private void StartNpcDGroupEventPlaytest() =>
        StartNpcDGroupEventPlaytest(LocaleService.SimplifiedChinese);

    private void StartNpcDGroupEventEnglishPlaytest() =>
        StartNpcDGroupEventPlaytest(LocaleService.English);

    private void StartNpcDGroupEventPlaytest(
        string locale,
        int page = 1,
        bool wrongTool = false
    ) => StartGroupCharacterEventPlaytest(
        GroupCharacterEventCatalog.NpcDOneBenchFourKindsOfReadinessId,
        locale,
        page,
        wrongTool
    );

    private void StartGroupCharacterEventPlaytest(
        string eventId,
        string locale,
        int page,
        bool wrongTool
    )
    {
        var definition = GroupCharacterEventCatalog.ById[eventId];
        FreeUi(_title);
        _title = null;
        _locale.SetLocale(locale);
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 7;
        save.MinuteOfDay = definition.RequiredStartMinute + 60;
        save.Locale = locale;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.SelectedSlot = 0;
        save.Weather = new WeatherSave
        {
            Day = save.Day,
            CurrentId = DataCatalog.ClearWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Village = new VillageSave
        {
            MetNpcIds = definition.ParticipantNpcIds.ToList(),
            Relationships = definition.ParticipantNpcIds.Select(npcId =>
                new VillageRelationshipSave
                {
                    NpcId = npcId,
                    Points = definition.RequiredRelationshipPoints,
                    LastTalkDay = save.Day
                }
            ).ToList()
        };
        save.CharacterEvents = new CharacterEventSave
        {
            Entries = definition.ParticipantNpcIds.SelectMany(npcId =>
                CharacterEventCatalog.Definitions
                    .Where(eventDefinition =>
                        eventDefinition.NpcId == npcId
                    )
                    .OrderBy(eventDefinition =>
                        eventDefinition.RequiredRelationshipPoints)
                    .Select((eventDefinition, index) =>
                        new CharacterEventEntrySave
                        {
                            EventId = eventDefinition.Id,
                            CompletedDay = index + 1
                        }
                    )
            ).ToList()
        };
        _session.Restore(save);
        _session.Inventory.Select(wrongTool ? 1 : 0);
        var triggerNpc = _session.Village.CurrentNpcs(
                save.Day,
                save.MinuteOfDay,
                PlayerLocationIds.World
            )
            .Single(npc => npc.Definition.Id == definition.TriggerNpcId);
        triggerNpc = PlacePlayerAdjacentForPlaytest(triggerNpc);

        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (!wrongTool)
        {
            var target = triggerNpc.Position;
            Callable.From(() =>
            {
                TalkToVillager(target);
                for (var index = 1; index < page; index++)
                {
                    _dialogueOverlay?.AdvanceOnePageForPlaytest();
                }
            }).CallDeferred();
        }
    }

    private static IReadOnlyList<string> CharacterEventPrerequisiteIds(
        CharacterEventDefinition definition
    )
    {
        var reversed = new List<string>();
        var prerequisiteId = definition.RequiredPreviousEventId;
        while (prerequisiteId is not null)
        {
            reversed.Add(prerequisiteId);
            prerequisiteId = CharacterEventCatalog.ById[
                prerequisiteId
            ].RequiredPreviousEventId;
        }

        reversed.Reverse();
        return reversed;
    }
}
