using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private void OpenStarfallWatchBoard()
    {
        if (_starfallWatchOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _starfallWatchOverlay = new StarfallWatchOverlay(
            _theme,
            _session,
            _locale
        );
        _starfallWatchOverlay.CloseRequested += CloseStarfallWatchBoard;
        _starfallWatchOverlay.ActionCompleted += () =>
        {
            _audio.Play(PixelSound.Reward);
            SaveNow(false);
        };
        _uiLayer.AddChild(_starfallWatchOverlay);
    }

    private void CloseStarfallWatchBoard()
    {
        FreeUi(_starfallWatchOverlay);
        _starfallWatchOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }
}
