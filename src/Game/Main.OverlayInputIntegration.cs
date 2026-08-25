using Godot;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private bool TryClosePlayerOverlay(
        InputEvent @event,
        bool overlayCancelPressed
    )
    {
        if (overlayCancelPressed &&
            _settingsOverlay is not null)
        {
            CloseSettings();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _fishingMinigameOverlay is not null)
        {
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _fishingGearOverlay is not null)
        {
            CloseFishingGear();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _deepMineOverlay is not null)
        {
            CloseDeepMine();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _starGateOverlay is not null)
        {
            CloseStarGate();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _stellarResonanceOverlay is not null)
        {
            CloseStellarResonance();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _mainStoryEndingOverlay is not null)
        {
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _compendiumOverlay is not null)
        {
            CloseCropCodex();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _festivalShowcaseOverlay is not null)
        {
            CloseFestivalShowcase();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _festivalShopOverlay is not null)
        {
            CloseFestivalShop();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _gleamrisePlantingOverlay is not null)
        {
            CloseGleamrisePlanting();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _gleamriseSeedExchangeOverlay is not null)
        {
            CloseGleamriseSeedExchange();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed && _shopOverlay is not null)
        {
            CloseShop();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed && _processorOverlay is not null)
        {
            CloseProcessor();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed && _shippingOverlay is not null)
        {
            CloseShipping();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed && _commissionOverlay is not null)
        {
            CloseCommissionBoard();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _constructionOverlay is not null)
        {
            CloseConstructionPanel();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _livestockAutomationOverlay is not null)
        {
            CloseLivestockAutomation();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _mailOverlay is not null)
        {
            CloseStarlightMail();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _postDeliveryOverlay is not null)
        {
            ClosePostDeliveryBoard();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _starfallWatchOverlay is not null)
        {
            CloseStarfallWatchBoard();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _starlightOverlay is not null)
        {
            CloseStarlightPedestal();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _kitchenOverlay is not null)
        {
            CloseKitchen();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _ingredientPantryOverlay is not null)
        {
            CloseIngredientPantry();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _cookedDishOverlay is not null)
        {
            CloseCookedDishes();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if ((overlayCancelPressed ||
             @event.IsActionPressed(InputSetup.Crafting)) &&
            _craftingOverlay is not null)
        {
            CloseCrafting();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _farmingSpecializationOverlay is not null)
        {
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed && _storageOverlay is not null)
        {
            CloseStorage();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed && _nightlySummaryOverlay is not null)
        {
            CloseNightlySummary();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if ((overlayCancelPressed ||
             @event.IsActionPressed(InputSetup.Backpack)) &&
            _backpackOverlay is not null)
        {
            CloseBackpack();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _fishingCollectionOverlay is not null)
        {
            CloseFishingCollection();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _fishingDonationOverlay is not null)
        {
            CloseFishingDonation();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (overlayCancelPressed &&
            _gleamriseSeasonOverlay is not null)
        {
            CloseGleamriseSeasonGoals();
            GetViewport().SetInputAsHandled();
            return true;
        }

        return false;
    }
}
