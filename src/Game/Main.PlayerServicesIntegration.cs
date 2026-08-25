using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main
{
    private void OpenShop()
    {
        OpenShop(ShopOverlayMode.FarmStall);
    }

    private void OpenShop(ShopOverlayMode mode)
    {
        if (_shopOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _shopOverlay = new ShopOverlay(
            _theme,
            _session,
            _locale,
            mode
        );
        _shopOverlay.CloseRequested += CloseShop;
        _shopOverlay.TransactionSucceeded += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_shopOverlay);
    }

    private void CloseShop()
    {
        FreeUi(_shopOverlay);
        _shopOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenProcessor(string machineId)
    {
        if (_processorOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _processorOverlay = new ProcessorOverlay(
            _theme,
            _session,
            _locale,
            machineId
        );
        _processorOverlay.CloseRequested += CloseProcessor;
        _processorOverlay.FeedbackRequested += result =>
            ShowImmediateFeedback(
                ImmediateFeedbackDomain.Processing,
                result
            );
        _processorOverlay.ProcessingSucceeded += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_processorOverlay);
    }

    private void CloseProcessor()
    {
        FreeUi(_processorOverlay);
        _processorOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenShipping()
    {
        if (_shippingOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _shippingOverlay = new ShippingOverlay(_theme, _session, _locale);
        _shippingOverlay.CloseRequested += CloseShipping;
        _shippingOverlay.ShippingChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_shippingOverlay);
    }

    private void CloseShipping()
    {
        FreeUi(_shippingOverlay);
        _shippingOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenCommissionBoard()
    {
        OpenCommissionBoard(CommissionBoardPage.Daily);
    }

    private void OpenWeeklyCommissionBoard()
    {
        OpenCommissionBoard(CommissionBoardPage.Weekly);
    }

    private void OpenCommissionBoard(CommissionBoardPage initialPage)
    {
        if (_commissionOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _farm?.SetCommissionBoardOpen(true);
        _commissionOverlay = new CommissionBoardOverlay(
            _theme,
            _session,
            _locale,
            initialPage
        );
        _commissionOverlay.CloseRequested += CloseCommissionBoard;
        _commissionOverlay.CommissionChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _commissionOverlay.RewardClaimed += messageKey =>
        {
            ShowRewardFeedback(messageKey);
            SaveNow(false);
        };
        _uiLayer.AddChild(_commissionOverlay);
    }

    private void CloseCommissionBoard()
    {
        FreeUi(_commissionOverlay);
        _commissionOverlay = null;
        _farm?.SetCommissionBoardOpen(false);
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenStarlightMail()
    {
        if (_mailOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _mailOverlay = new StarlightMailOverlay(
            _theme,
            _session,
            _locale
        );
        _mailOverlay.CloseRequested += CloseStarlightMail;
        _mailOverlay.MailChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            if (!_mailPlaytest)
            {
                SaveNow(false);
            }
        };
        _mailOverlay.AttachmentClaimed += result =>
        {
            ShowRewardFeedback(result);
            if (!_mailPlaytest)
            {
                SaveNow(false);
            }
        };
        _uiLayer.AddChild(_mailOverlay);
    }

    private void CloseStarlightMail()
    {
        FreeUi(_mailOverlay);
        _mailOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenStarlightPedestal() => OpenStarlightPedestal(
        DataCatalog.WoodlandStarlightId
    );

    private void OpenStarlightPedestal(string pedestalId)
    {
        if (_starlightOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _session.Starlight.Discover(pedestalId);
        _starlightOverlay = new StarlightPedestalOverlay(
            _theme,
            _session,
            _locale,
            pedestalId
        );
        _starlightOverlay.CloseRequested += CloseStarlightPedestal;
        _starlightOverlay.StarlightChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_starlightOverlay);
    }

    private void CloseStarlightPedestal()
    {
        FreeUi(_starlightOverlay);
        _starlightOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenBackpack()
    {
        if (_backpackOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _backpackOverlay = new BackpackOverlay(_theme, _session, _locale);
        _backpackOverlay.CloseRequested += CloseBackpack;
        _backpackOverlay.CraftingRequested += () =>
        {
            CloseBackpack();
            OpenCrafting();
        };
        _backpackOverlay.MealsRequested += () =>
        {
            CloseBackpack();
            OpenCookedDishes();
        };
        _uiLayer.AddChild(_backpackOverlay);
    }

    private void CloseBackpack()
    {
        FreeUi(_backpackOverlay);
        _backpackOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenCrafting()
    {
        if (_craftingOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _craftingOverlay = new CraftingOverlay(_theme, _session, _locale);
        _craftingOverlay.CloseRequested += CloseCrafting;
        _craftingOverlay.Crafted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_craftingOverlay);
    }

    private void CloseCrafting()
    {
        FreeUi(_craftingOverlay);
        _craftingOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenStorage(GridPosition position)
    {
        if (_storageOverlay is not null ||
            _session.Storage.ChestAt(position) is null)
        {
            return;
        }

        SetWorldControls(false);
        _farm?.SetStorageChestOpen(position);
        _storageOverlay = new StorageOverlay(_theme, _session, _locale, position);
        _storageOverlay.CloseRequested += CloseStorage;
        _storageOverlay.StorageChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_storageOverlay);
    }

    private void CloseStorage()
    {
        FreeUi(_storageOverlay);
        _storageOverlay = null;
        _farm?.SetStorageChestOpen(null);
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }
}
