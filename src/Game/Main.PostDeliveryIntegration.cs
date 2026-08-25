using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private void OpenPostDeliveryBoard()
    {
        if (_postDeliveryOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _postDeliveryOverlay = new PostDeliveryOverlay(
            _theme,
            _session,
            _locale
        );
        _postDeliveryOverlay.CloseRequested += ClosePostDeliveryBoard;
        _postDeliveryOverlay.RouteAccepted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_postDeliveryOverlay);
    }

    private void ClosePostDeliveryBoard()
    {
        FreeUi(_postDeliveryOverlay);
        _postDeliveryOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void DeliverPostToVillager(GridPosition target)
    {
        var result = _session.DeliverPostToVillager(
            target,
            out var completion
        );
        if (!result.Succeeded || completion is null)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        SaveNow(false);
        _audio.Play(PixelSound.Reward);
        var recipient = VillageCatalog.Npcs[completion.Route.TargetNpcId];
        ShowDialogue(
            recipient.NameKey,
            completion.Route.ResponseKey,
            () => _hud?.ShowNoticeFormatted(
                "post.delivery.completed.notice",
                2.8,
                completion.RewardCoins,
                completion.RelationshipPoints
            ),
            GeneratedArt.RelationshipIcon(
                VillageSystem.TierFor(
                    _session.Village.Relationship(
                        completion.Route.TargetNpcId
                    ).Points
                )
            )
        );
    }
}
