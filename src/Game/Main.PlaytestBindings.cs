using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main
{
    private PlaytestScenarioRegistry CreatePlaytestScenarioRegistry() =>
        new(
            new Dictionary<PlaytestScenarioId, Action>
            {
                [PlaytestScenarioId.Door] = StartDoorPlaytest,
                [PlaytestScenarioId.Cottage] = StartCottagePlaytest,
                [PlaytestScenarioId.CottageUpgradeReady] =
                    StartCottageUpgradeReadyPlaytest,
                [PlaytestScenarioId.CottageUpgradeInProgress] =
                    StartCottageUpgradeInProgressPlaytest,
                [PlaytestScenarioId.CottageUpgradeCompleted] =
                    StartCottageUpgradeCompletedPlaytest,
                [PlaytestScenarioId.CottageSecondUpgradeReady] =
                    StartCottageSecondUpgradeReadyPlaytest,
                [PlaytestScenarioId.CottageSecondUpgradeInProgress] =
                    StartCottageSecondUpgradeInProgressPlaytest,
                [PlaytestScenarioId.CottageKitchen] =
                    StartCottageKitchenPlaytest,
                [PlaytestScenarioId.CottageKitchenPanel] =
                    StartCottageKitchenPanelPlaytest,
                [PlaytestScenarioId.CottagePantry] =
                    StartCottagePantryPlaytest,
                [PlaytestScenarioId.CottagePantryPanel] =
                    StartCottagePantryPanelPlaytest,
                [PlaytestScenarioId.CottageMealsEnglish] =
                    StartCottageMealsEnglishPlaytest,
                [PlaytestScenarioId.HomesteadWorkshopReady] =
                    StartHomesteadWorkshopReadyPlaytest,
                [PlaytestScenarioId.HomesteadWorkshopInProgress] =
                    StartHomesteadWorkshopInProgressPlaytest,
                [PlaytestScenarioId.HomesteadWorkshopCompleted] =
                    StartHomesteadWorkshopCompletedPlaytest,
                [PlaytestScenarioId.GreenhouseReady] =
                    StartGreenhouseReadyPlaytest,
                [PlaytestScenarioId.GreenhouseInProgress] =
                    StartGreenhouseInProgressPlaytest,
                [PlaytestScenarioId.GreenhouseExteriorCompleted] =
                    StartGreenhouseExteriorCompletedPlaytest,
                [PlaytestScenarioId.GreenhouseCompleted] =
                    StartGreenhouseCompletedPlaytest,
                [PlaytestScenarioId.GreenhouseCistern] =
                    StartGreenhouseCisternPlaytest,
                [PlaytestScenarioId.StarfeatherCoopReady] =
                    StartStarfeatherCoopReadyPlaytest,
                [PlaytestScenarioId.StarfeatherCoopInProgress] =
                    StartStarfeatherCoopInProgressPlaytest,
                [PlaytestScenarioId.StarfeatherCoopGrazing] =
                    StartStarfeatherCoopGrazingPlaytest,
                [PlaytestScenarioId.StarfeatherCoopChick] =
                    StartStarfeatherCoopChickPlaytest,
                [PlaytestScenarioId.StarfeatherCoopAdult] =
                    StartStarfeatherCoopAdultPlaytest,
                [PlaytestScenarioId.StarfeatherCoopNestBlockedEnglish] =
                    StartStarfeatherCoopNestBlockedEnglishPlaytest,
                [PlaytestScenarioId.MoonfleeceBarnReady] =
                    StartMoonfleeceBarnReadyPlaytest,
                [PlaytestScenarioId.MoonfleeceBarnInProgress] =
                    StartMoonfleeceBarnInProgressPlaytest,
                [PlaytestScenarioId.MoonfleeceBarnGrazing] =
                    StartMoonfleeceBarnGrazingPlaytest,
                [PlaytestScenarioId.MoonfleeceBarnJuvenile] =
                    StartMoonfleeceBarnJuvenilePlaytest,
                [PlaytestScenarioId.MoonfleeceBarnRackBlockedEnglish] =
                    StartMoonfleeceBarnRackBlockedEnglishPlaytest,
                [PlaytestScenarioId.DewhornGrazing] =
                    StartDewhornGrazingPlaytest,
                [PlaytestScenarioId.DewhornMilkingBlockedEnglish] =
                    StartDewhornMilkingBlockedEnglishPlaytest,
                [PlaytestScenarioId.LivestockAutomationConsole] =
                    StartLivestockAutomationConsolePlaytest,
                [PlaytestScenarioId.LivestockAutomationPanel] =
                    StartLivestockAutomationPanelPlaytest,
                [PlaytestScenarioId.LivestockAutomationPanelEnglish] =
                    StartLivestockAutomationPanelEnglishPlaytest,
                [PlaytestScenarioId.LivestockAutomationConstruction] =
                    StartLivestockAutomationConstructionPlaytest,
                [PlaytestScenarioId.Crops] = StartCropPlaytest,
                [PlaytestScenarioId.GleamriseCrops] =
                    StartGleamriseCropPlaytest,
                [PlaytestScenarioId.GleamriseSeason] =
                    StartGleamriseSeasonPlaytest,
                [PlaytestScenarioId.RainveilCrops] =
                    StartRainveilCropPlaytest,
                [PlaytestScenarioId.StarharvestCrops] =
                    StartStarharvestCropPlaytest,
                [PlaytestScenarioId.StarharvestMarketGate] =
                    StartStarharvestMarketGatePlaytest,
                [PlaytestScenarioId.StarharvestMarket] =
                    StartStarharvestMarketPlaytest,
                [PlaytestScenarioId.StarharvestMarketShowcase] =
                    StartStarharvestMarketShowcasePlaytest,
                [PlaytestScenarioId.StarharvestMarketResult] =
                    StartStarharvestMarketResultPlaytest,
                [PlaytestScenarioId.StarharvestMarketShop] =
                    StartStarharvestMarketShopPlaytest,
                [PlaytestScenarioId.StarharvestMarketShowcaseEnglish] =
                    StartStarharvestMarketShowcaseEnglishPlaytest,
                [PlaytestScenarioId.GleamriseFestivalGate] =
                    StartGleamriseFestivalGatePlaytest,
                [PlaytestScenarioId.GleamriseFestival] =
                    StartGleamriseFestivalPlaytest,
                [PlaytestScenarioId.GleamriseFestivalChallenge] =
                    StartGleamriseFestivalChallengePlaytest,
                [PlaytestScenarioId.GleamriseFestivalResult] =
                    StartGleamriseFestivalResultPlaytest,
                [PlaytestScenarioId.GleamriseFestivalExchange] =
                    StartGleamriseFestivalExchangePlaytest,
                [PlaytestScenarioId.GleamriseFestivalChallengeEnglish] =
                    StartGleamriseFestivalChallengeEnglishPlaytest,
                [PlaytestScenarioId.LongnightFeastGate] =
                    StartLongnightFeastGatePlaytest,
                [PlaytestScenarioId.LongnightFeast] =
                    StartLongnightFeastPlaytest,
                [PlaytestScenarioId.LongnightFeastActivity] =
                    StartLongnightFeastActivityPlaytest,
                [PlaytestScenarioId.LongnightFeastResult] =
                    StartLongnightFeastResultPlaytest,
                [PlaytestScenarioId.LongnightFeastStall] =
                    StartLongnightFeastStallPlaytest,
                [PlaytestScenarioId.LongnightFeastActivityEnglish] =
                    StartLongnightFeastActivityEnglishPlaytest,
                [PlaytestScenarioId.LongnightFeastWrongTool] =
                    StartLongnightFeastWrongToolPlaytest,
                [PlaytestScenarioId.FireflyTideGate] =
                    StartFireflyTideGatePlaytest,
                [PlaytestScenarioId.FireflyTide] =
                    StartFireflyTidePlaytest,
                [PlaytestScenarioId.FireflyTideActivity] =
                    StartFireflyTideActivityPlaytest,
                [PlaytestScenarioId.FireflyTideResult] =
                    StartFireflyTideResultPlaytest,
                [PlaytestScenarioId.FireflyTideShop] =
                    StartFireflyTideShopPlaytest,
                [PlaytestScenarioId.FireflyTideActivityEnglish] =
                    StartFireflyTideActivityEnglishPlaytest,
                [PlaytestScenarioId.FireflyTideWrongTool] =
                    StartFireflyTideWrongToolPlaytest,
                [PlaytestScenarioId.LongnightHomestead] =
                    StartLongnightHomesteadPlaytest,
                [PlaytestScenarioId.LongnightEmporium] =
                    StartLongnightEmporiumPlaytest,
                [PlaytestScenarioId.LongnightSnowForecast] =
                    StartLongnightSnowForecastPlaytest,
                [PlaytestScenarioId.LongnightSnow] =
                    StartLongnightSnowPlaytest,
                [PlaytestScenarioId.LongnightSnowIndoor] =
                    StartLongnightSnowIndoorPlaytest,
                [PlaytestScenarioId.LongnightSnowClear] =
                    StartLongnightSnowClearPlaytest,
                [PlaytestScenarioId.Economy] = StartEconomyPlaytest,
                [PlaytestScenarioId.Processor] = StartProcessorPlaytest,
                [PlaytestScenarioId.MultiProcessorBatch] =
                    StartMultiProcessorBatchPlaytest,
                [PlaytestScenarioId.MoonpearlEggPress] =
                    StartMoonpearlEggPressPlaytest,
                [PlaytestScenarioId.ArchiveGift] = StartArchiveGiftPlaytest,
                [PlaytestScenarioId.Archive] = StartArchivePlaytest,
                [PlaytestScenarioId.ArchiveDoor] = StartArchiveDoorPlaytest,
                [PlaytestScenarioId.CropCodexDesk] =
                    StartCropCodexDeskPlaytest,
                [PlaytestScenarioId.CropCodexPartial] =
                    StartCropCodexPartialPlaytest,
                [PlaytestScenarioId.CropCodexRewardReady] =
                    StartCropCodexRewardReadyPlaytest,
                [PlaytestScenarioId.CropCodexRewardClaimedEnglish] =
                    StartCropCodexRewardClaimedEnglishPlaytest,
                [PlaytestScenarioId.CropCodexWrongTool] =
                    StartCropCodexWrongToolPlaytest,
                [PlaytestScenarioId.CropCodexDiscountShop] =
                    StartCropCodexDiscountShopPlaytest,
                [PlaytestScenarioId.CookingCodexUnknown] =
                    StartCookingCodexUnknownPlaytest,
                [PlaytestScenarioId.CookingCodexPartial] =
                    StartCookingCodexPartialPlaytest,
                [PlaytestScenarioId.CookingCodexRewardReady] =
                    StartCookingCodexRewardReadyPlaytest,
                [PlaytestScenarioId.CookingCodexRewardClaimedEnglish] =
                    StartCookingCodexRewardClaimedEnglishPlaytest,
                [PlaytestScenarioId.CookingCodexRewardMealsEnglish] =
                    StartCookingCodexRewardMealsEnglishPlaytest,
                [PlaytestScenarioId.ArtisanCodexUnknown] =
                    StartArtisanCodexUnknownPlaytest,
                [PlaytestScenarioId.ArtisanCodexPartial] =
                    StartArtisanCodexPartialPlaytest,
                [PlaytestScenarioId.ArtisanCodexRewardReady] =
                    StartArtisanCodexRewardReadyPlaytest,
                [PlaytestScenarioId.ArtisanCodexRewardClaimedEnglish] =
                    StartArtisanCodexRewardClaimedEnglishPlaytest,
                [PlaytestScenarioId.ArtisanCodexRewardShippingEnglish] =
                    StartArtisanCodexRewardShippingEnglishPlaytest,
                [PlaytestScenarioId.SeasonalForage] =
                    StartSeasonalForagePlaytest,
                [PlaytestScenarioId.SeasonalForageWrongTool] =
                    StartSeasonalForageWrongToolPlaytest,
                [PlaytestScenarioId.SeasonalForageStardustMap] =
                    StartSeasonalForageStardustMapPlaytest,
                [PlaytestScenarioId.ForageCodexPartial] =
                    StartForageCodexPartialPlaytest,
                [PlaytestScenarioId.ForageCodexRewardReady] =
                    StartForageCodexRewardReadyPlaytest,
                [PlaytestScenarioId.ForageCodexRewardClaimedEnglish] =
                    StartForageCodexRewardClaimedEnglishPlaytest,
                [PlaytestScenarioId.Fishing] = StartFishingPlaytest,
                [PlaytestScenarioId.FishingMinigame] =
                    StartFishingMinigamePlaytest,
                [PlaytestScenarioId.FishingGear] =
                    StartFishingGearPlaytest,
                [PlaytestScenarioId.FishingCollection] =
                    StartFishingCollectionPlaytest,
                [PlaytestScenarioId.FishingDonation] =
                    StartFishingDonationPlaytest,
                [PlaytestScenarioId.FishCodexPartial] =
                    StartFishCodexPartialPlaytest,
                [PlaytestScenarioId.FishCodexCompleteEnglish] =
                    StartFishCodexCompleteEnglishPlaytest,
                [PlaytestScenarioId.CrystalGrottoEntry] =
                    StartCrystalGrottoEntryPlaytest,
                [PlaytestScenarioId.CrystalGrottoBasic] =
                    StartCrystalGrottoBasicPlaytest,
                [PlaytestScenarioId.CrystalGrottoUpgrade] =
                    StartCrystalGrottoUpgradePlaytest,
                [PlaytestScenarioId.CrystalGrottoDeep] =
                    StartCrystalGrottoDeepPlaytest,
                [PlaytestScenarioId.DeepMine] = StartDeepMinePlaytest,
                [PlaytestScenarioId.MineralCodexCompleteEnglish] =
                    StartMineralCodexCompleteEnglishPlaytest,
                [PlaytestScenarioId.CrystalValeStarlightPanel] =
                    StartCrystalValeStarlightPanelPlaytest,
                [PlaytestScenarioId.CrystalValeStarlightRestored] =
                    StartCrystalValeStarlightRestoredPlaytest,
                [PlaytestScenarioId.StarfallRuinsEntry] =
                    StartStarfallRuinsEntryPlaytest,
                [PlaytestScenarioId.StarfallRuinsCombat] =
                    StartStarfallRuinsCombatPlaytest,
                [PlaytestScenarioId.StarfallRuinsArtifacts] =
                    StartStarfallRuinsArtifactsPlaytest,
                [PlaytestScenarioId.ArtifactCodexDonationEnglish] =
                    StartArtifactCodexDonationEnglishPlaytest,
                [PlaytestScenarioId.StarfallRuinsStarlightPanel] =
                    StartStarfallRuinsStarlightPanelPlaytest,
                [PlaytestScenarioId.StarfallRuinsStarlightRestored] =
                    StartStarfallRuinsStarlightRestoredPlaytest,
                [PlaytestScenarioId.SixfoldStarGate] =
                    StartSixfoldStarGatePlaytest,
                [PlaytestScenarioId.SixfoldStarGatePanel] =
                    StartSixfoldStarGatePanelPlaytest,
                [PlaytestScenarioId.StellarConvergence] =
                    StartStellarConvergencePlaytest,
                [PlaytestScenarioId.AccessibilitySettings] =
                    StartAccessibilitySettingsPlaytest,
                [PlaytestScenarioId.LioraEventOne] =
                    StartLioraEventOnePlaytest,
                [PlaytestScenarioId.LioraEventTwo] =
                    StartLioraEventTwoPlaytest,
                [PlaytestScenarioId.TaviEventOne] =
                    StartTaviEventOnePlaytest,
                [PlaytestScenarioId.TaviEventTwo] =
                    StartTaviEventTwoPlaytest,
                [PlaytestScenarioId.NemiEventOne] =
                    StartNemiEventOnePlaytest,
                [PlaytestScenarioId.NemiEventTwo] =
                    StartNemiEventTwoPlaytest,
                [PlaytestScenarioId.KaelEventOne] =
                    StartKaelEventOnePlaytest,
                [PlaytestScenarioId.KaelEventTwo] =
                    StartKaelEventTwoPlaytest,
                [PlaytestScenarioId.SelaEventOne] =
                    StartSelaEventOnePlaytest,
                [PlaytestScenarioId.SelaEventTwo] =
                    StartSelaEventTwoPlaytest,
                [PlaytestScenarioId.OrinEventOne] =
                    StartOrinEventOnePlaytest,
                [PlaytestScenarioId.OrinEventTwo] =
                    StartOrinEventTwoPlaytest,
                [PlaytestScenarioId.ElowenEventOne] =
                    StartElowenEventOnePlaytest,
                [PlaytestScenarioId.ElowenEventTwo] =
                    StartElowenEventTwoPlaytest,
                [PlaytestScenarioId.VessaEventOne] =
                    StartVessaEventOnePlaytest,
                [PlaytestScenarioId.VessaEventTwo] =
                    StartVessaEventTwoPlaytest,
                [PlaytestScenarioId.VessaEventWrongTool] =
                    StartVessaEventWrongToolPlaytest,
                [PlaytestScenarioId.RelationshipMailsEnglish] =
                    StartRelationshipMailsEnglishPlaytest,
                [PlaytestScenarioId.VillageExpansionWave3] =
                    StartVillageExpansionWave3Playtest,
                [PlaytestScenarioId.VillageExpansionWave3Indoor] =
                    StartVillageExpansionWave3IndoorPlaytest,
                [PlaytestScenarioId.VillageExpansionWave3DialogueEnglish] =
                    StartVillageExpansionWave3DialogueEnglishPlaytest,
                [PlaytestScenarioId.VillageExpansionWave3WrongTool] =
                    StartVillageExpansionWave3WrongToolPlaytest,
                [PlaytestScenarioId.YvaraEventOne] =
                    StartYvaraEventOnePlaytest,
                [PlaytestScenarioId.YvaraEventTwo] =
                    StartYvaraEventTwoPlaytest,
                [PlaytestScenarioId.Wave3RelationshipMailsEnglish] =
                    StartWave3RelationshipMailsEnglishPlaytest,
                [PlaytestScenarioId.WorkshopTavi] =
                    StartWorkshopTaviPlaytest,
                [PlaytestScenarioId.Workshop] = StartWorkshopPlaytest,
                [PlaytestScenarioId.WorkshopDoor] =
                    StartWorkshopDoorPlaytest,
                [PlaytestScenarioId.TeaHouseVessa] =
                    StartTeaHouseVessaPlaytest,
                [PlaytestScenarioId.TeaHouse] =
                    StartTeaHousePlaytest,
                [PlaytestScenarioId.TeaHouseDoor] =
                    StartTeaHouseDoorPlaytest,
                [PlaytestScenarioId.EmporiumOrin] =
                    StartEmporiumOrinPlaytest,
                [PlaytestScenarioId.Emporium] =
                    StartEmporiumPlaytest,
                [PlaytestScenarioId.EmporiumDoor] =
                    StartEmporiumDoorPlaytest,
                [PlaytestScenarioId.EmporiumRotation] =
                    StartEmporiumRotationPlaytest,
                [PlaytestScenarioId.EmporiumRestdayDoor] =
                    StartEmporiumRestdayDoorPlaytest,
                [PlaytestScenarioId.StarlightPostNemi] =
                    StartStarlightPostNemiPlaytest,
                [PlaytestScenarioId.StarlightPost] =
                    StartStarlightPostPlaytest,
                [PlaytestScenarioId.StarlightPostDelivery] =
                    StartStarlightPostDeliveryPlaytest,
                [PlaytestScenarioId.StarlightPostWrongTool] =
                    StartStarlightPostWrongToolPlaytest,
                [PlaytestScenarioId.StarlightPostDoor] =
                    StartStarlightPostDoorPlaytest,
                [PlaytestScenarioId.StarfallWatchKael] =
                    StartStarfallWatchKaelPlaytest,
                [PlaytestScenarioId.StarfallWatch] =
                    StartStarfallWatchPlaytest,
                [PlaytestScenarioId.StarfallWatchBoard] =
                    StartStarfallWatchBoardPlaytest,
                [PlaytestScenarioId.StarfallWatchWrongTool] =
                    StartStarfallWatchWrongToolPlaytest,
                [PlaytestScenarioId.StarfallWatchDoor] =
                    StartStarfallWatchDoorPlaytest,
                [PlaytestScenarioId.VillageDialogue] =
                    StartVillageDialoguePlaytest,
                [PlaytestScenarioId.SelaDialogue] =
                    StartSelaDialoguePlaytest,
                [PlaytestScenarioId.VillageExpansion] =
                    StartVillageExpansionPlaytest,
                [PlaytestScenarioId.VillageExpansionArchive] =
                    StartVillageExpansionArchivePlaytest,
                [PlaytestScenarioId.VillageExpansionDialogueEnglish] =
                    StartVillageExpansionDialogueEnglishPlaytest,
                [PlaytestScenarioId.VillageExpansionWrongTool] =
                    StartVillageExpansionWrongToolPlaytest,
                [PlaytestScenarioId.NpcPathfinding] =
                    StartNpcPathfindingPlaytest,
                [PlaytestScenarioId.VillageRestdayEnglish] =
                    StartVillageRestdayEnglishPlaytest,
                [PlaytestScenarioId.VillageRainSchedule] =
                    StartVillageRainSchedulePlaytest,
                [PlaytestScenarioId.VillageRainveilSchedule] =
                    StartVillageRainveilSchedulePlaytest,
                [PlaytestScenarioId.Village] = StartVillagePlaytest,
                [PlaytestScenarioId.WorldAspectBoundary] =
                    StartWorldAspectBoundaryPlaytest,
                [PlaytestScenarioId.RainveilWorldAspect] =
                    StartRainveilWorldAspectPlaytest,
                [PlaytestScenarioId.StarharvestWorldAspect] =
                    StartStarharvestWorldAspectPlaytest,
                [PlaytestScenarioId.LongnightWorldAspect] =
                    StartLongnightWorldAspectPlaytest,
                [PlaytestScenarioId.RainveilWorldTreeRain] =
                    StartRainveilWorldTreeRainPlaytest,
                [PlaytestScenarioId.StarharvestWorldCrystalStardust] =
                    StartStarharvestWorldCrystalStardustPlaytest,
                [PlaytestScenarioId.WorldBeginnerArch] =
                    StartWorldBeginnerArchPlaytest,
                [PlaytestScenarioId.WorldWoodsGrove] =
                    StartWorldWoodsGrovePlaytest,
                [PlaytestScenarioId.WorldMeadowCircle] =
                    StartWorldMeadowCirclePlaytest,
                [PlaytestScenarioId.WorldCrystalRidge] =
                    StartWorldCrystalRidgePlaytest,
                [PlaytestScenarioId.WorldWetlandIslet] =
                    StartWorldWetlandIsletPlaytest,
                [PlaytestScenarioId.WorldRuinsColonnade] =
                    StartWorldRuinsColonnadePlaytest,
                [PlaytestScenarioId.WorldFacilitiesGateway] =
                    StartWorldFacilitiesGatewayPlaytest,
                [PlaytestScenarioId.World] = StartWorldPlaytest,
                [PlaytestScenarioId.Gate] = StartGatePlaytest,
                [PlaytestScenarioId.Backpack] = StartBackpackPlaytest,
                [PlaytestScenarioId.Resource] = StartResourcePlaytest,
                [PlaytestScenarioId.Target] = StartTargetPreviewPlaytest,
                [PlaytestScenarioId.PhaseA] = StartPhaseAPlaytest,
                [PlaytestScenarioId.PhaseASummary] =
                    StartPhaseASummaryPlaytest,
                [PlaytestScenarioId.PhaseARain] =
                    StartPhaseARainPlaytest,
                [PlaytestScenarioId.ResourceRespawn] =
                    StartResourceRespawnPlaytest,
                [PlaytestScenarioId.Crafting] = StartCraftingPlaytest,
                [PlaytestScenarioId.Placeables] =
                    StartFarmPlaceablesPlaytest,
                [PlaytestScenarioId.ChestPlacement] =
                    StartChestPlacementPlaytest,
                [PlaytestScenarioId.Storage] = StartStoragePlaytest,
                [PlaytestScenarioId.CommissionOffer] =
                    StartCommissionOfferPlaytest,
                [PlaytestScenarioId.CommissionReady] =
                    StartCommissionReadyPlaytest,
                [PlaytestScenarioId.CommissionReadyEnglish] =
                    StartCommissionReadyEnglishPlaytest,
                [PlaytestScenarioId.CommissionMap] =
                    StartCommissionMapPlaytest,
                [PlaytestScenarioId.WeeklyCommissionOffer] =
                    StartWeeklyCommissionOfferPlaytest,
                [PlaytestScenarioId.WeeklyCommissionStageReady] =
                    StartWeeklyCommissionStageReadyPlaytest,
                [PlaytestScenarioId.WeeklyCommissionRewardReady] =
                    StartWeeklyCommissionRewardReadyPlaytest,
                [PlaytestScenarioId.WeeklyCommissionMap] =
                    StartWeeklyCommissionMapPlaytest,
                [PlaytestScenarioId.MailboxUnread] =
                    StartMailboxUnreadPlaytest,
                [PlaytestScenarioId.MailPanel] =
                    StartMailPanelPlaytest,
                [PlaytestScenarioId.MailReward] =
                    StartMailRewardPlaytest,
                [PlaytestScenarioId.StarlightMap] =
                    StartStarlightMapPlaytest,
                [PlaytestScenarioId.StarlightMapRestored] =
                    StartStarlightRestoredMapPlaytest,
                [PlaytestScenarioId.StarlightPanel] =
                    StartStarlightPanelPlaytest,
                [PlaytestScenarioId.StarlightRestored] =
                    StartStarlightRestoredPlaytest,
                [PlaytestScenarioId.StarlightRestoredEnglish] =
                    StartStarlightRestoredEnglishPlaytest,
                [PlaytestScenarioId.HomesteadStarlightDormant] =
                    StartHomesteadStarlightDormantPlaytest,
                [PlaytestScenarioId.HomesteadStarlightWrongTool] =
                    StartHomesteadStarlightWrongToolPlaytest,
                [PlaytestScenarioId.HomesteadStarlightRestored] =
                    StartHomesteadStarlightRestoredPlaytest,
                [PlaytestScenarioId.HomesteadStarlightPanel] =
                    StartHomesteadStarlightPanelPlaytest,
                [PlaytestScenarioId.HomesteadStarlightPanelEnglish] =
                    StartHomesteadStarlightPanelEnglishPlaytest,
                [PlaytestScenarioId.MeadowStarlightDormant] =
                    StartMeadowStarlightDormantPlaytest,
                [PlaytestScenarioId.MeadowStarlightRestored] =
                    StartMeadowStarlightRestoredPlaytest,
                [PlaytestScenarioId.MeadowStarlightPanel] =
                    StartMeadowStarlightPanelPlaytest,
                [PlaytestScenarioId.MeadowStarlightPanelEnglish] =
                    StartMeadowStarlightPanelEnglishPlaytest,
                [PlaytestScenarioId.MeadowPollination] =
                    StartMeadowPollinationPlaytest,
                [PlaytestScenarioId.MoonwaterStarlightPanel] =
                    StartMoonwaterStarlightPanelPlaytest,
                [PlaytestScenarioId.QualityCrafting] =
                    StartQualityCraftingPlaytest,
                [PlaytestScenarioId.QualityBackpackEnglish] =
                    StartQualityBackpackEnglishPlaytest,
                [PlaytestScenarioId.QualityBackpack] =
                    StartQualityBackpackPlaytest,
                [PlaytestScenarioId.Quality] = StartQualityPlaytest,
                [PlaytestScenarioId.OrchardHives] =
                    StartOrchardHivesPlaytest,
                [PlaytestScenarioId.FarmingSpecialization] =
                    StartFarmingSpecializationPlaytest,
                [PlaytestScenarioId.Story01WoodlandDiscovery] =
                    StartStory01WoodlandDiscoveryPlaytest,
                [PlaytestScenarioId.Story01WoodlandRestoration] =
                    StartStory01WoodlandRestorationPlaytest,
                [PlaytestScenarioId.Story01WoodlandResponse] =
                    StartStory01WoodlandResponsePlaytest,
                [PlaytestScenarioId.Story01WoodlandRevisitEnglish] =
                    StartStory01WoodlandRevisitEnglishPlaytest,
                [PlaytestScenarioId.Story01FinalRevisitEnglish] =
                    StartStory01FinalRevisitEnglishPlaytest,
                [PlaytestScenarioId.Story01FinalRevisitPageTwoEnglish] =
                    () => StartStory01FinalRevisitEnglishPlaytest(2),
                [PlaytestScenarioId.Story01FinalRevisitPageThreeEnglish] =
                    () => StartStory01FinalRevisitEnglishPlaytest(3),
                [PlaytestScenarioId.Story01HomesteadResponse] =
                    StartStory01HomesteadResponsePlaytest,
                [PlaytestScenarioId.Story01MeadowResponse] =
                    StartStory01MeadowResponsePlaytest,
                [PlaytestScenarioId.Story01MoonwaterResponse] =
                    StartStory01MoonwaterResponsePlaytest,
                [PlaytestScenarioId.Story01CrystalValeResponse] =
                    StartStory01CrystalValeResponsePlaytest,
                [PlaytestScenarioId.Story01StarfallRuinsResponse] =
                    StartStory01StarfallRuinsResponsePlaytest,
                [PlaytestScenarioId.LioraEventThree] =
                    StartLioraEventThreePlaytest,
                [PlaytestScenarioId.LioraEventFour] =
                    StartLioraEventFourPlaytest,
                [PlaytestScenarioId.TaviEventThree] =
                    StartTaviEventThreePlaytest,
                [PlaytestScenarioId.TaviEventFour] =
                    StartTaviEventFourPlaytest,
                [PlaytestScenarioId.NemiEventThree] =
                    StartNemiEventThreePlaytest,
                [PlaytestScenarioId.NemiEventFour] =
                    StartNemiEventFourPlaytest,
                [PlaytestScenarioId.KaelEventThree] =
                    StartKaelEventThreePlaytest,
                [PlaytestScenarioId.KaelEventFour] =
                    StartKaelEventFourPlaytest,
                [PlaytestScenarioId.SelaEventThree] =
                    StartSelaEventThreePlaytest,
                [PlaytestScenarioId.SelaEventFour] =
                    StartSelaEventFourPlaytest,
                [PlaytestScenarioId.HaldenEventThree] =
                    StartHaldenEventThreePlaytest,
                [PlaytestScenarioId.HaldenEventFour] =
                    StartHaldenEventFourPlaytest,
                [PlaytestScenarioId.OrinEventThree] =
                    StartOrinEventThreePlaytest,
                [PlaytestScenarioId.OrinEventFour] =
                    StartOrinEventFourPlaytest,
                [PlaytestScenarioId.VessaEventThree] =
                    StartVessaEventThreePlaytest,
                [PlaytestScenarioId.VessaEventFour] =
                    StartVessaEventFourPlaytest,
                [PlaytestScenarioId.NpcALioraRainResponse] =
                    StartNpcALioraRainResponsePlaytest,
                [PlaytestScenarioId.NpcATaviLongnightResponse] =
                    StartNpcATaviLongnightResponsePlaytest,
                [PlaytestScenarioId.NpcAVessaStardustResponse] =
                    StartNpcAVessaStardustResponsePlaytest,
                [PlaytestScenarioId.NpcAOrinLongnightSnowResponse] =
                    StartNpcAOrinLongnightSnowResponsePlaytest,
                [PlaytestScenarioId.NpcAGroupEvent] =
                    StartNpcAGroupEventPlaytest,
                [PlaytestScenarioId.NpcAGroupEventEnglish] =
                    StartNpcAGroupEventEnglishPlaytest,
                [PlaytestScenarioId.NpcAGroupEventPageTwoEnglish] =
                    () => StartNpcAGroupEventPlaytest(
                        LocaleService.English,
                        page: 2
                    ),
                [PlaytestScenarioId.NpcAGroupEventPageThreeEnglish] =
                    () => StartNpcAGroupEventPlaytest(
                        LocaleService.English,
                        page: 3
                    ),
                [PlaytestScenarioId.NpcAGroupEventPageFourEnglish] =
                    () => StartNpcAGroupEventPlaytest(
                        LocaleService.English,
                        page: 4
                    ),
                [PlaytestScenarioId.NpcAGroupEventPageFiveEnglish] =
                    () => StartNpcAGroupEventPlaytest(
                        LocaleService.English,
                        page: 5
                    ),
                [PlaytestScenarioId.NpcAGroupEventWrongTool] =
                    () => StartNpcAGroupEventPlaytest(
                        LocaleService.SimplifiedChinese,
                        wrongTool: true
                    ),
                [PlaytestScenarioId.NpcBNemiStardustResponse] =
                    StartNpcBNemiStardustResponsePlaytest,
                [PlaytestScenarioId.NpcBKaelLongnightResponse] =
                    StartNpcBKaelLongnightResponsePlaytest,
                [PlaytestScenarioId.NpcBSelaStarharvestResponse] =
                    StartNpcBSelaStarharvestResponsePlaytest,
                [PlaytestScenarioId.NpcBHaldenStardustResponse] =
                    StartNpcBHaldenStardustResponsePlaytest,
                [PlaytestScenarioId.NpcBGroupEvent] =
                    StartNpcBGroupEventPlaytest,
                [PlaytestScenarioId.NpcBGroupEventEnglish] =
                    StartNpcBGroupEventEnglishPlaytest,
                [PlaytestScenarioId.NpcBGroupEventPageTwoEnglish] =
                    () => StartNpcBGroupEventPlaytest(
                        LocaleService.English,
                        page: 2
                    ),
                [PlaytestScenarioId.NpcBGroupEventPageThreeEnglish] =
                    () => StartNpcBGroupEventPlaytest(
                        LocaleService.English,
                        page: 3
                    ),
                [PlaytestScenarioId.NpcBGroupEventPageFourEnglish] =
                    () => StartNpcBGroupEventPlaytest(
                        LocaleService.English,
                        page: 4
                    ),
                [PlaytestScenarioId.NpcBGroupEventPageFiveEnglish] =
                    () => StartNpcBGroupEventPlaytest(
                        LocaleService.English,
                        page: 5
                    ),
                [PlaytestScenarioId.NpcBGroupEventWrongTool] =
                    () => StartNpcBGroupEventPlaytest(
                        LocaleService.SimplifiedChinese,
                        wrongTool: true
                    ),
                [PlaytestScenarioId.ElowenEventThree] =
                    StartElowenEventThreePlaytest,
                [PlaytestScenarioId.ElowenEventFour] =
                    StartElowenEventFourPlaytest,
                [PlaytestScenarioId.MaveaEventThree] =
                    StartMaveaEventThreePlaytest,
                [PlaytestScenarioId.MaveaEventFour] =
                    StartMaveaEventFourPlaytest,
                [PlaytestScenarioId.SivrenEventThree] =
                    StartSivrenEventThreePlaytest,
                [PlaytestScenarioId.SivrenEventFour] =
                    StartSivrenEventFourPlaytest,
                [PlaytestScenarioId.DorrikEventThree] =
                    StartDorrikEventThreePlaytest,
                [PlaytestScenarioId.DorrikEventFour] =
                    StartDorrikEventFourPlaytest,
                [PlaytestScenarioId.NpcCElowenRainveilResponse] =
                    StartNpcCElowenRainveilResponsePlaytest,
                [PlaytestScenarioId.NpcCMaveaRainResponse] =
                    StartNpcCMaveaRainResponsePlaytest,
                [PlaytestScenarioId.NpcCSivrenStarharvestResponse] =
                    StartNpcCSivrenStarharvestResponsePlaytest,
                [PlaytestScenarioId.NpcCDorrikRainveilResponse] =
                    StartNpcCDorrikRainveilResponsePlaytest,
                [PlaytestScenarioId.NpcCGroupEvent] =
                    StartNpcCGroupEventPlaytest,
                [PlaytestScenarioId.NpcCGroupEventEnglish] =
                    StartNpcCGroupEventEnglishPlaytest,
                [PlaytestScenarioId.NpcCGroupEventPageTwoEnglish] =
                    () => StartNpcCGroupEventPlaytest(
                        LocaleService.English,
                        page: 2
                    ),
                [PlaytestScenarioId.NpcCGroupEventPageThreeEnglish] =
                    () => StartNpcCGroupEventPlaytest(
                        LocaleService.English,
                        page: 3
                    ),
                [PlaytestScenarioId.NpcCGroupEventPageFourEnglish] =
                    () => StartNpcCGroupEventPlaytest(
                        LocaleService.English,
                        page: 4
                    ),
                [PlaytestScenarioId.NpcCGroupEventPageFiveEnglish] =
                    () => StartNpcCGroupEventPlaytest(
                        LocaleService.English,
                        page: 5
                    ),
                [PlaytestScenarioId.NpcCGroupEventWrongTool] =
                    () => StartNpcCGroupEventPlaytest(
                        LocaleService.SimplifiedChinese,
                        wrongTool: true
                    ),
                [PlaytestScenarioId.YvaraEventThree] =
                    StartYvaraEventThreePlaytest,
                [PlaytestScenarioId.YvaraEventFour] =
                    StartYvaraEventFourPlaytest,
                [PlaytestScenarioId.BrialEventThree] =
                    StartBrialEventThreePlaytest,
                [PlaytestScenarioId.BrialEventFour] =
                    StartBrialEventFourPlaytest,
                [PlaytestScenarioId.PavriEventThree] =
                    StartPavriEventThreePlaytest,
                [PlaytestScenarioId.PavriEventFour] =
                    StartPavriEventFourPlaytest,
                [PlaytestScenarioId.RovenEventThree] =
                    StartRovenEventThreePlaytest,
                [PlaytestScenarioId.RovenEventFour] =
                    StartRovenEventFourPlaytest,
                [PlaytestScenarioId.NpcDYvaraRainResponse] =
                    StartNpcDYvaraRainResponsePlaytest,
                [PlaytestScenarioId.NpcDBrialLongnightResponse] =
                    StartNpcDBrialLongnightResponsePlaytest,
                [PlaytestScenarioId.NpcDPavriRainveilResponse] =
                    StartNpcDPavriRainveilResponsePlaytest,
                [PlaytestScenarioId.NpcDRovenRainResponse] =
                    StartNpcDRovenRainResponsePlaytest,
                [PlaytestScenarioId.NpcDGroupEvent] =
                    StartNpcDGroupEventPlaytest,
                [PlaytestScenarioId.NpcDGroupEventEnglish] =
                    StartNpcDGroupEventEnglishPlaytest,
                [PlaytestScenarioId.NpcDGroupEventPageTwoEnglish] =
                    () => StartNpcDGroupEventPlaytest(
                        LocaleService.English,
                        page: 2
                    ),
                [PlaytestScenarioId.NpcDGroupEventPageThreeEnglish] =
                    () => StartNpcDGroupEventPlaytest(
                        LocaleService.English,
                        page: 3
                    ),
                [PlaytestScenarioId.NpcDGroupEventPageFourEnglish] =
                    () => StartNpcDGroupEventPlaytest(
                        LocaleService.English,
                        page: 4
                    ),
                [PlaytestScenarioId.NpcDGroupEventPageFiveEnglish] =
                    () => StartNpcDGroupEventPlaytest(
                        LocaleService.English,
                        page: 5
                    ),
                [PlaytestScenarioId.NpcDGroupEventWrongTool] =
                    () => StartNpcDGroupEventPlaytest(
                        LocaleService.SimplifiedChinese,
                        wrongTool: true
                    ),
                [PlaytestScenarioId.Farm] = StartNewGame
            }
        );
}
