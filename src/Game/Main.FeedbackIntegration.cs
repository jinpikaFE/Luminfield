using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private void UpdateAudioContext()
    {
        _audio.ApplyRuntimeContext(new PixelAudioRuntimeContext(
            _session.PlayerLocationId,
            _session.Clock.Day,
            _session.Weather.CurrentId,
            CombatActive: _deepMineOverlay is not null,
            FestivalActive: _session.CurrentFestivalId is not null,
            Region: _session.PlayerLocationId == PlayerLocationIds.World
                ? WorldDefinition.GetBiome(_session.PlayerCell)
                : null
        ));
    }

    private void ShowImmediateFeedback(
        ImmediateFeedbackDomain domain,
        ActionResult result,
        TargetPreview? preview = null
    )
    {
        var cue = ImmediateFeedbackPresenter.FromActionResult(
            domain,
            result,
            _settings,
            preview
        );
        _hud?.ShowImmediateFeedback(cue);
        if (ImmediateFeedbackAudio.SoundFor(cue) is { } sound)
        {
            _audio.Play(sound);
        }
    }

    private void ShowRewardFeedback(ActionResult result)
    {
        if (!result.Succeeded)
        {
            ShowImmediateFeedback(ImmediateFeedbackDomain.Reward, result);
            return;
        }

        var feedbackResult = new ActionResult(
            true,
            MessageKey: "feedback.reward.claimed",
            GrantedItemId: result.GrantedItemId,
            GrantedItemCount: result.GrantedItemCount
        );
        ShowImmediateFeedback(ImmediateFeedbackDomain.Reward, feedbackResult);
        _audio.Play(PixelSound.Reward);
    }

    private void ShowRewardFeedback(string _) =>
        ShowRewardFeedback(
            ActionResult.Success(messageKey: "feedback.reward.claimed")
        );

    private static ImmediateFeedbackDomain FarmFeedbackDomain(
        ActionResult result,
        string selectedItemId
    )
    {
        if (selectedItemId == DataCatalog.WateringCanId)
        {
            return ImmediateFeedbackDomain.Watering;
        }

        if (selectedItemId == DataCatalog.FishingRodId)
        {
            return ImmediateFeedbackDomain.Fishing;
        }

        if (!string.IsNullOrWhiteSpace(result.GrantedItemId))
        {
            var baseItemId = DataCatalog.BaseItemId(result.GrantedItemId);
            return DataCatalog.CropIds.Contains(baseItemId, StringComparer.Ordinal)
                ? ImmediateFeedbackDomain.Harvest
                : ImmediateFeedbackDomain.Pickup;
        }

        return ImmediateFeedbackDomain.Tool;
    }
}
