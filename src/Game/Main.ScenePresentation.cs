using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private void EnsureHud()
    {
        if (_hud is not null)
        {
            return;
        }

        _hud = new HudView(_theme, _session, _locale);
        _uiLayer.AddChild(_hud);
        EnsureRouteGuidanceHud();
    }

    private void ShowFarm(
        bool fromCottage,
        bool fromArchive = false,
        bool fromWorkshop = false,
        bool fromTeaHouse = false,
        bool fromTwilightEmporium = false,
        bool fromStarlightPost = false,
        bool fromStarfallWatch = false,
        bool fromGreenhouse = false,
        bool fromStarfeatherCoop = false,
        bool fromMoonfleeceBarn = false,
        bool fromStarharvestMarket = false,
        bool fromGleamrisePlantingFestival = false,
        bool fromLongnightLanternFeast = false,
        bool fromFireflyTide = false,
        bool fromCrystalGrotto = false,
        bool fromStarfallRuinsTrial = false
    )
    {
        ClearWorld();
        if (fromCottage)
        {
            _session.SetPlayerState(
                FarmView.CottageDoorCell.X * 16 + 8,
                (FarmView.CottageDoorCell.Y + 1) * 16 + 8,
                false
            );
        }
        else if (fromGreenhouse)
        {
            _session.SetPlayerLocation(
                FarmLayout.GreenhouseReturnCell.X * 16 + 8,
                FarmLayout.GreenhouseReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromStarfeatherCoop)
        {
            _session.SetPlayerLocation(
                FarmLayout.StarfeatherCoopReturnCell.X * 16 + 8,
                FarmLayout.StarfeatherCoopReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromMoonfleeceBarn)
        {
            _session.SetPlayerLocation(
                FarmLayout.MoonfleeceBarnReturnCell.X * 16 + 8,
                FarmLayout.MoonfleeceBarnReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromArchive)
        {
            _session.SetPlayerLocation(
                VillageCatalog.MoonlitArchiveDoorCell.X * 16 + 8,
                (VillageCatalog.MoonlitArchiveDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromWorkshop)
        {
            _session.SetPlayerLocation(
                VillageCatalog.MoonstoneWorkshopDoorCell.X * 16 + 8,
                (VillageCatalog.MoonstoneWorkshopDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromTeaHouse)
        {
            _session.SetPlayerLocation(
                VillageCatalog.StarweaverTeaHouseDoorCell.X * 16 + 8,
                (VillageCatalog.StarweaverTeaHouseDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromTwilightEmporium)
        {
            _session.SetPlayerLocation(
                VillageCatalog.TwilightEmporiumDoorCell.X * 16 + 8,
                (VillageCatalog.TwilightEmporiumDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromStarlightPost)
        {
            _session.SetPlayerLocation(
                VillageCatalog.StarlightPostDoorCell.X * 16 + 8,
                (VillageCatalog.StarlightPostDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromStarfallWatch)
        {
            _session.SetPlayerLocation(
                VillageCatalog.StarfallWatchDoorCell.X * 16 + 8,
                (VillageCatalog.StarfallWatchDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromStarharvestMarket)
        {
            _session.SetPlayerLocation(
                StarharvestMarketLayout.WorldReturnCell.X * 16 + 8,
                StarharvestMarketLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromGleamrisePlantingFestival)
        {
            _session.SetPlayerLocation(
                GleamrisePlantingFestivalLayout.WorldReturnCell.X * 16 + 8,
                GleamrisePlantingFestivalLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromLongnightLanternFeast)
        {
            _session.SetPlayerLocation(
                LongnightLanternFeastLayout.WorldReturnCell.X * 16 + 8,
                LongnightLanternFeastLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromFireflyTide)
        {
            _session.SetPlayerLocation(
                FireflyTideLayout.WorldReturnCell.X * 16 + 8,
                FireflyTideLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromCrystalGrotto)
        {
            _session.SetPlayerLocation(
                CrystalGrottoSurveyLayout.WorldReturnCell.X * 16 + 8,
                CrystalGrottoSurveyLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromStarfallRuinsTrial)
        {
            _session.SetPlayerLocation(
                StarfallRuinsTrialLayout.WorldReturnCell.X * 16 + 8,
                StarfallRuinsTrialLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }

        _farm = new FarmView(_session, _locale);
        _farm.UseRequested += UseFarmTarget;
        _farm.MiraRequested += TalkToMira;
        _farm.EnterCottageRequested += () => ShowCottage(true);
        _farm.EnterGreenhouseRequested += TryEnterGreenhouse;
        _farm.EnterStarfeatherCoopRequested += TryEnterStarfeatherCoop;
        _farm.EnterMoonfleeceBarnRequested += TryEnterMoonfleeceBarn;
        _farm.EnterArchiveRequested += TryEnterMoonlitArchive;
        _farm.EnterWorkshopRequested += TryEnterMoonstoneWorkshop;
        _farm.EnterTeaHouseRequested += TryEnterStarweaverTeaHouse;
        _farm.EnterTwilightEmporiumRequested +=
            TryEnterTwilightEmporium;
        _farm.EnterStarlightPostRequested += TryEnterStarlightPost;
        _farm.EnterStarfallWatchRequested += TryEnterStarfallWatch;
        _farm.EnterStarharvestMarketRequested +=
            TryEnterStarharvestMarket;
        _farm.EnterGleamrisePlantingFestivalRequested +=
            TryEnterGleamrisePlantingFestival;
        _farm.EnterLongnightLanternFeastRequested +=
            TryEnterLongnightLanternFeast;
        _farm.EnterFireflyTideRequested += TryEnterFireflyTide;
        _farm.EnterCrystalGrottoRequested += TryEnterCrystalGrotto;
        _farm.EnterStarfallRuinsRequested += TryEnterStarfallRuinsTrial;
        _farm.ShopRequested += OpenShop;
        _farm.ProcessorRequested += OpenProcessor;
        _farm.ShippingRequested += OpenShipping;
        _farm.CommissionRequested += OpenCommissionBoard;
        _farm.MailRequested += OpenStarlightMail;
        _farm.StarlightRequested += OpenStarlightPedestal;
        _farm.VillagerRequested += TalkToVillager;
        _farm.StorageRequested += OpenStorage;
        _farm.HomesteadWorkbenchRequested +=
            OpenHomesteadConstructionPanel;
        _farm.NoticeRequested += key => _hud?.ShowNotice(key);
        _farm.RegionEntered += key => _hud?.ShowNotice(key, 2.6);
        _farm.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _farm;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromCottage)
        {
            _hud?.ShowNotice("notice.leave_cottage");
        }
        else if (fromArchive)
        {
            _hud?.ShowNotice("notice.leave_archive");
        }
        else if (fromWorkshop)
        {
            _hud?.ShowNotice("notice.leave_workshop");
        }
        else if (fromTeaHouse)
        {
            _hud?.ShowNotice("notice.leave_tea_house");
        }
        else if (fromTwilightEmporium)
        {
            _hud?.ShowNotice("notice.leave_emporium");
        }
        else if (fromStarlightPost)
        {
            _hud?.ShowNotice("notice.leave_starlight_post");
        }
        else if (fromStarfallWatch)
        {
            _hud?.ShowNotice("notice.leave_starfall_watch");
        }
        else if (fromGreenhouse)
        {
            _hud?.ShowNotice("notice.leave_greenhouse");
        }
        else if (fromStarfeatherCoop)
        {
            _hud?.ShowNotice("notice.leave_starfeather_coop");
        }
        else if (fromMoonfleeceBarn)
        {
            _hud?.ShowNotice("notice.leave_moonfleece_barn");
        }
        else if (fromStarharvestMarket)
        {
            _hud?.ShowNotice("notice.leave_starharvest_market");
        }
        else if (fromLongnightLanternFeast)
        {
            _hud?.ShowNotice("notice.leave_longnight_feast");
        }
        else if (fromFireflyTide)
        {
            _hud?.ShowNotice("notice.leave_firefly_tide");
        }
        else if (fromCrystalGrotto)
        {
            _hud?.ShowNotice("mining.survey.exit");
        }
        else if (fromStarfallRuinsTrial)
        {
            _hud?.ShowNotice("ruins.trial.exit");
        }
    }

    private void ShowCottage(bool fromFarm)
    {
        ClearWorld();
        if (fromFarm)
        {
            _session.SetPlayerState(20 * 16 + 8, 17 * 16 + 8, true);
        }

        _cottage = new CottageView(_session, _locale);
        _cottage.SleepRequested += EndDay;
        _cottage.ExitRequested += () => ShowFarm(true);
        _cottage.KitchenReserveRequested += InspectKitchenReserve;
        _cottage.KitchenRequested += OpenKitchen;
        _cottage.IngredientPantryRequested += OpenIngredientPantry;
        _cottage.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _cottage;
        AddChild(_world);
        MoveChild(_world, 1);
        _hud?.ShowNotice(fromFarm ? "notice.enter_cottage" : string.Empty);
    }

    private void ShowGreenhouse(bool fromFarm)
    {
        ClearWorld();
        if (fromFarm)
        {
            _session.SetPlayerLocation(
                GreenhouseLayout.SafeArrivalCell.X * 16 + 8,
                GreenhouseLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.Greenhouse
            );
        }

        _greenhouse = new GreenhouseView(_session, _locale);
        _greenhouse.UseRequested += UseFarmTarget;
        _greenhouse.ExitRequested += () =>
            ShowFarm(false, fromGreenhouse: true);
        _greenhouse.NoticeRequested += key => _hud?.ShowNotice(key);
        _greenhouse.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _greenhouse;
        AddChild(_world);
        MoveChild(_world, 1);
        _hud?.ShowNotice(
            fromFarm ? "notice.enter_greenhouse" : string.Empty
        );
    }

    private void ShowCrystalGrotto(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                CrystalGrottoSurveyLayout.SafeArrivalCell.X * 16 + 8,
                CrystalGrottoSurveyLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.CrystalGrottoSurvey
            );
        }

        _crystalGrotto = new CrystalGrottoView(_session, _locale);
        _crystalGrotto.UseRequested += UseFarmTarget;
        _crystalGrotto.UpgradeRequested += OpenToolUpgrade;
        _crystalGrotto.ExitRequested += () => ShowFarm(
            false,
            fromCrystalGrotto: true
        );
        _crystalGrotto.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _crystalGrotto.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _crystalGrotto;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("mining.survey.enter");
        }
    }

    private void ShowStarfallRuinsTrial(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                StarfallRuinsTrialLayout.SafeArrivalCell.X * 16 + 8,
                StarfallRuinsTrialLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.StarfallRuinsTrial
            );
        }

        _starfallRuinsTrial = new StarfallRuinsTrialView(
            _session,
            _locale
        );
        _starfallRuinsTrial.ExitRequested += () =>
        {
            SaveNow(false);
            ShowFarm(false, fromStarfallRuinsTrial: true);
        };
        _starfallRuinsTrial.DefeatRequested += () =>
            ResolveStarfallTrialDefeat(forcedByClosingTime: false);
        _starfallRuinsTrial.ProgressChanged += () => SaveNow(false);
        _starfallRuinsTrial.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _starfallRuinsTrial.FeedbackRequested += (domain, result) =>
            ShowImmediateFeedback(domain, result);
        _starfallRuinsTrial.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _starfallRuinsTrial;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("ruins.trial.enter");
        }
    }

    private void ShowStarfeatherCoop(bool fromFarm)
    {
        ClearWorld();
        if (fromFarm)
        {
            _session.SetPlayerLocation(
                StarfeatherCoopLayout.SafeArrivalCell.X * 16 + 8,
                StarfeatherCoopLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.StarfeatherCoop
            );
        }

        _starfeatherCoop = new StarfeatherCoopView(_session, _locale);
        _starfeatherCoop.UseRequested += UseFarmTarget;
        _starfeatherCoop.ExitRequested += () => ShowFarm(
            false,
            fromStarfeatherCoop: true
        );
        _starfeatherCoop.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _starfeatherCoop.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _starfeatherCoop;
        AddChild(_world);
        MoveChild(_world, 1);
        _hud?.ShowNotice(
            fromFarm ? "notice.enter_starfeather_coop" : string.Empty
        );
    }

    private void ShowMoonfleeceBarn(bool fromFarm)
    {
        ClearWorld();
        if (fromFarm)
        {
            _session.SetPlayerLocation(
                MoonfleeceBarnLayout.SafeArrivalCell.X * 16 + 8,
                MoonfleeceBarnLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.MoonfleeceBarn
            );
        }

        _moonfleeceBarn = new MoonfleeceBarnView(_session, _locale);
        _moonfleeceBarn.UseRequested += UseFarmTarget;
        _moonfleeceBarn.ExitRequested += () => ShowFarm(
            false,
            fromMoonfleeceBarn: true
        );
        _moonfleeceBarn.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _moonfleeceBarn.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _moonfleeceBarn;
        AddChild(_world);
        MoveChild(_world, 1);
        _hud?.ShowNotice(
            fromFarm ? "notice.enter_moonfleece_barn" : string.Empty
        );
    }

    private void ShowStarharvestMarket(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                StarharvestMarketLayout.SafeArrivalCell.X * 16 + 8,
                StarharvestMarketLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.StarharvestMarket
            );
        }

        _starharvestMarket = new StarharvestMarketView(
            _session,
            _locale
        );
        _starharvestMarket.ExitRequested += () => ShowFarm(
            false,
            fromStarharvestMarket: true
        );
        _starharvestMarket.ClosedRequested += () =>
        {
            CloseFestivalShowcase();
            CloseFestivalShop();
            ShowFarm(false);
            _hud?.ShowNotice("festival.starharvest.closed");
        };
        _starharvestMarket.ShowcaseRequested += OpenFestivalShowcase;
        _starharvestMarket.ShopRequested += OpenFestivalShop;
        _starharvestMarket.VillagerRequested += TalkToVillager;
        _starharvestMarket.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _starharvestMarket.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _starharvestMarket;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_starharvest_market");
        }
    }

    private void ShowGleamrisePlantingFestival(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                GleamrisePlantingFestivalLayout.SafeArrivalCell.X * 16 + 8,
                GleamrisePlantingFestivalLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.GleamrisePlantingFestival
            );
        }

        _gleamrisePlantingFestival = new GleamrisePlantingFestivalView(
            _session,
            _locale
        );
        _gleamrisePlantingFestival.ExitRequested += () => ShowFarm(
            false,
            fromGleamrisePlantingFestival: true
        );
        _gleamrisePlantingFestival.ClosedRequested += () =>
        {
            CloseGleamrisePlanting();
            CloseGleamriseSeedExchange();
            ShowFarm(false);
            _hud?.ShowNotice("festival.gleamrise.closed");
        };
        _gleamrisePlantingFestival.ActivityRequested +=
            OpenGleamrisePlanting;
        _gleamrisePlantingFestival.ExchangeRequested +=
            OpenGleamriseSeedExchange;
        _gleamrisePlantingFestival.VillagerRequested += TalkToVillager;
        _gleamrisePlantingFestival.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _gleamrisePlantingFestival.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _gleamrisePlantingFestival;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_gleamrise_festival");
        }
    }

    private void ShowLongnightLanternFeast(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                LongnightLanternFeastLayout.SafeArrivalCell.X * 16 + 8,
                LongnightLanternFeastLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.LongnightLanternFeast
            );
        }

        _longnightLanternFeast = new LongnightLanternFeastView(
            _session,
            _locale
        );
        _longnightLanternFeast.ExitRequested += () => ShowFarm(
            false,
            fromLongnightLanternFeast: true
        );
        _longnightLanternFeast.ClosedRequested += () =>
        {
            CloseLongnightFeast();
            CloseLongnightStall();
            ShowFarm(false);
            _hud?.ShowNotice("festival.longnight.closed");
        };
        _longnightLanternFeast.ActivityRequested += OpenLongnightFeast;
        _longnightLanternFeast.StallRequested += OpenLongnightStall;
        _longnightLanternFeast.VillagerRequested += TalkToVillager;
        _longnightLanternFeast.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _longnightLanternFeast.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _longnightLanternFeast;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_longnight_feast");
        }
    }

    private void ShowFireflyTide(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                FireflyTideLayout.SafeArrivalCell.X * 16 + 8,
                FireflyTideLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.FireflyTide
            );
        }

        _fireflyTide = new FireflyTideView(_session, _locale);
        _fireflyTide.ExitRequested += () => ShowFarm(
            false,
            fromFireflyTide: true
        );
        _fireflyTide.ClosedRequested += () =>
        {
            CloseFireflyTideActivity();
            CloseFireflyTideShop();
            ShowFarm(false);
            _hud?.ShowNotice("festival.firefly.closed");
        };
        _fireflyTide.ActivityRequested += OpenFireflyTideActivity;
        _fireflyTide.ShopRequested += OpenFireflyTideShop;
        _fireflyTide.VillagerRequested += TalkToVillager;
        _fireflyTide.NoticeRequested += key => _hud?.ShowNotice(key);
        _fireflyTide.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _fireflyTide;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_firefly_tide");
        }
    }

    private void ShowArchive(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                20 * 16 + 8,
                17 * 16 + 8,
                PlayerLocationIds.MoonlitArchive
            );
        }

        _archive = new ArchiveView(_session, _locale);
        _archive.ExitRequested += TryLeaveMoonlitArchive;
        _archive.DeskRequested += OpenCropCodex;
        _archive.VillagerRequested += TalkToVillager;
        _archive.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _archive;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_archive");
        }
    }

    private void ShowWorkshop(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                20 * 16 + 8,
                18 * 16 + 8,
                PlayerLocationIds.MoonstoneWorkshop
            );
        }

        _workshop = new WorkshopView(_session, _locale);
        _workshop.ExitRequested += TryLeaveMoonstoneWorkshop;
        _workshop.WorkbenchRequested += OpenConstructionPanel;
        _workshop.VillagerRequested += TalkToVillager;
        _workshop.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _workshop;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_workshop");
        }
    }

    private void ShowTeaHouse(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                20 * 16 + 8,
                18 * 16 + 8,
                PlayerLocationIds.StarweaverTeaHouse
            );
        }

        _teaHouse = new TeaHouseView(_session, _locale);
        _teaHouse.ExitRequested += TryLeaveStarweaverTeaHouse;
        _teaHouse.TeaCounterRequested += InspectStarwovenTeaCounter;
        _teaHouse.VillagerRequested += TalkToVillager;
        _teaHouse.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _teaHouse;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_tea_house");
        }
    }

    private void ShowTwilightEmporium(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                20 * 16 + 8,
                18 * 16 + 8,
                PlayerLocationIds.TwilightEmporium
            );
        }

        _twilightEmporium = new TwilightEmporiumView(
            _session,
            _locale
        );
        _twilightEmporium.ExitRequested += TryLeaveTwilightEmporium;
        _twilightEmporium.ManifestRequested += InspectTravelManifest;
        _twilightEmporium.VillagerRequested += TalkToVillager;
        _twilightEmporium.StepRequested +=
            () => _audio.Play(PixelSound.Step);
        _world = _twilightEmporium;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_emporium");
        }
    }

    private void ShowStarlightPost(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                20 * 16 + 8,
                18 * 16 + 8,
                PlayerLocationIds.StarlightPost
            );
        }

        _starlightPost = new StarlightPostView(
            _session,
            _locale
        );
        _starlightPost.ExitRequested += TryLeaveStarlightPost;
        _starlightPost.SortingCounterRequested +=
            InspectRouteSortingCounter;
        _starlightPost.VillagerRequested += TalkToVillager;
        _starlightPost.StepRequested +=
            () => _audio.Play(PixelSound.Step);
        _world = _starlightPost;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_starlight_post");
        }
    }

    private void ShowStarfallWatch(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                19 * 16 + 8,
                18 * 16 + 8,
                PlayerLocationIds.StarfallWatch
            );
        }

        _starfallWatch = new StarfallWatchView(
            _session,
            _locale
        );
        _starfallWatch.ExitRequested += TryLeaveStarfallWatch;
        _starfallWatch.SealRouteTableRequested +=
            InspectSealRouteTable;
        _starfallWatch.VillagerRequested += TalkToVillager;
        _starfallWatch.StepRequested +=
            () => _audio.Play(PixelSound.Step);
        _world = _starfallWatch;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_starfall_watch");
        }
    }

}
