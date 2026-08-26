using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main
{
    private void OpenFestivalMemories()
    {
        if (_festivalMemoriesOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _festivalMemoriesOverlay = new FestivalMemoriesOverlay(
            _theme,
            _session,
            _locale
        );
        _festivalMemoriesOverlay.CloseRequested += CloseFestivalMemories;
        _uiLayer.AddChild(_festivalMemoriesOverlay);
    }

    private void CloseFestivalMemories()
    {
        FreeUi(_festivalMemoriesOverlay);
        _festivalMemoriesOverlay = null;
        SaveNow(false);
        if (RestorePauseAfterChild())
        {
            return;
        }
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void TryEnterStarharvestMarket()
    {
        var result = _session.TryEnterStarharvestMarket(
            StarharvestMarketLayout.WorldEntryCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowStarharvestMarket(true);
    }

    private void TryEnterGleamrisePlantingFestival()
    {
        var result = _session.TryEnterFestival(
            FestivalCatalog.GleamrisePlantingFestivalId,
            GleamrisePlantingFestivalLayout.WorldEntryCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowGleamrisePlantingFestival(true);
    }

    private void TryEnterLongnightLanternFeast()
    {
        var result = _session.TryEnterFestival(
            FestivalCatalog.LongnightLanternFeastFestivalId,
            LongnightLanternFeastLayout.WorldEntryCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowLongnightLanternFeast(true);
    }

    private void TryEnterFireflyTide()
    {
        var result = _session.TryEnterFestival(
            FestivalCatalog.FireflyTideFestivalId,
            FireflyTideLayout.WorldEntryCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowFireflyTide(true);
    }

    private void OpenFestivalShowcase()
    {
        if (_festivalShowcaseOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _festivalShowcaseOverlay = new FestivalShowcaseOverlay(
            _theme,
            _session,
            _locale
        );
        _festivalShowcaseOverlay.CloseRequested +=
            CloseFestivalShowcase;
        _festivalShowcaseOverlay.SubmissionCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_festivalShowcaseOverlay);
    }

    private void CloseFestivalShowcase()
    {
        FreeUi(_festivalShowcaseOverlay);
        _festivalShowcaseOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenFestivalShop()
    {
        if (_festivalShopOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _festivalShopOverlay = new FestivalShopOverlay(
            _theme,
            _session,
            _locale
        );
        _festivalShopOverlay.CloseRequested += CloseFestivalShop;
        _festivalShopOverlay.PurchaseCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_festivalShopOverlay);
    }

    private void CloseFestivalShop()
    {
        FreeUi(_festivalShopOverlay);
        _festivalShopOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenGleamrisePlanting()
    {
        if (_gleamrisePlantingOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _gleamrisePlantingOverlay = new GleamrisePlantingOverlay(
            _theme,
            _session,
            _locale
        );
        _gleamrisePlantingOverlay.CloseRequested +=
            CloseGleamrisePlanting;
        _uiLayer.AddChild(_gleamrisePlantingOverlay);
    }

    private void CloseGleamrisePlanting()
    {
        FreeUi(_gleamrisePlantingOverlay);
        _gleamrisePlantingOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenGleamriseSeedExchange()
    {
        if (_gleamriseSeedExchangeOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _gleamriseSeedExchangeOverlay = new GleamriseSeedExchangeOverlay(
            _theme,
            _session,
            _locale
        );
        _gleamriseSeedExchangeOverlay.CloseRequested +=
            CloseGleamriseSeedExchange;
        _uiLayer.AddChild(_gleamriseSeedExchangeOverlay);
    }

    private void CloseGleamriseSeedExchange()
    {
        FreeUi(_gleamriseSeedExchangeOverlay);
        _gleamriseSeedExchangeOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenLongnightFeast(GridPosition sourceCell)
    {
        if (_longnightFeastOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _longnightFeastOverlay = new LongnightLanternFeastOverlay(
            _theme,
            _session,
            _locale,
            sourceCell
        );
        _longnightFeastOverlay.CloseRequested += CloseLongnightFeast;
        _longnightFeastOverlay.ParticipationCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_longnightFeastOverlay);
    }

    private void CloseLongnightFeast()
    {
        FreeUi(_longnightFeastOverlay);
        _longnightFeastOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenLongnightStall()
    {
        if (_longnightStallOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _longnightStallOverlay = new LongnightLanternStallOverlay(
            _theme,
            _session,
            _locale,
            LongnightLanternFeastLayout.StallCell
        );
        _longnightStallOverlay.CloseRequested += CloseLongnightStall;
        _longnightStallOverlay.PurchaseCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_longnightStallOverlay);
    }

    private void CloseLongnightStall()
    {
        FreeUi(_longnightStallOverlay);
        _longnightStallOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenFireflyTideActivity(GridPosition sourceCell)
    {
        if (_fireflyTideOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _fireflyTideOverlay = new FireflyTideOverlay(
            _theme,
            _session,
            _locale,
            sourceCell
        );
        _fireflyTideOverlay.CloseRequested += CloseFireflyTideActivity;
        _fireflyTideOverlay.ParticipationCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_fireflyTideOverlay);
    }

    private void CloseFireflyTideActivity()
    {
        FreeUi(_fireflyTideOverlay);
        _fireflyTideOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenFireflyTideShop()
    {
        if (_fireflyTideShopOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _fireflyTideShopOverlay = new FireflyTideShopOverlay(
            _theme,
            _session,
            _locale,
            FireflyTideLayout.ShopCell
        );
        _fireflyTideShopOverlay.CloseRequested += CloseFireflyTideShop;
        _fireflyTideShopOverlay.PurchaseCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_fireflyTideShopOverlay);
    }

    private void CloseFireflyTideShop()
    {
        FreeUi(_fireflyTideShopOverlay);
        _fireflyTideShopOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }
}
