using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public enum PixelSound
{
    Till,
    Water,
    Plant,
    Harvest,
    Step,
    Chime,
    Sleep,
    Error,
    ResourceBlocked,
    ToolMismatch,
    Pickup,
    Damage,
    Dodge,
    FishBite,
    Reward
}

public sealed partial class PixelAudio : Node
{
    private const int SampleRate = PixelAudioProfile.SampleRate;
    private PixelAmbientContext _ambientContext = PixelAmbientContext.HomesteadClear;
    private AmbientCrossfade? _ambientCrossfade;
    private AudioStreamPlayer? _ambientPlayer;
    private bool _enabled;
    private float _masterVolumeDb;
    private float _ambientVolumeDb;
    private float _effectsVolumeDb;

    private sealed class AmbientCrossfade
    {
        public AmbientCrossfade(
            AudioStreamPlayer outgoing,
            AudioStreamPlayer incoming,
            float outgoingStartVolumeDb,
            float incomingTargetVolumeDb
        )
        {
            Outgoing = outgoing;
            Incoming = incoming;
            OutgoingStartVolumeDb = outgoingStartVolumeDb;
            IncomingTargetVolumeDb = incomingTargetVolumeDb;
        }

        public AudioStreamPlayer Outgoing { get; }
        public AudioStreamPlayer Incoming { get; }
        public float OutgoingStartVolumeDb { get; }
        public float IncomingTargetVolumeDb { get; set; }
        public double ElapsedSeconds { get; set; }
    }

    public override void _Ready()
    {
        if (DisplayServer.GetName() == "headless")
        {
            return;
        }

        LoadPersistedSettings();
        _enabled = true;
        var profile = PixelAudioProfile.Ambient(_ambientContext);
        _ambientPlayer = new AudioStreamPlayer
        {
            Stream = BuildAmbient(_ambientContext),
            VolumeDb = AmbientVolumeDb(profile)
        };
        AddChild(_ambientPlayer);
        _ambientPlayer.Play();
    }

    public override void _Process(double delta)
    {
        if (_ambientCrossfade is null)
        {
            return;
        }

        _ambientCrossfade.ElapsedSeconds += delta;
        var elapsed = _ambientCrossfade.ElapsedSeconds;
        var duration = PixelAudioProfile.AmbientCrossfadeSeconds;
        _ambientCrossfade.Outgoing.VolumeDb = PixelAudioProfile.CrossfadeVolumeDb(
            elapsed,
            duration,
            _ambientCrossfade.OutgoingStartVolumeDb,
            PixelAudioProfile.SilentVolumeDb
        );
        _ambientCrossfade.Incoming.VolumeDb = PixelAudioProfile.CrossfadeVolumeDb(
            elapsed,
            duration,
            PixelAudioProfile.SilentVolumeDb,
            _ambientCrossfade.IncomingTargetVolumeDb
        );

        if (PixelAudioProfile.CrossfadeProgress(elapsed, duration) < 1)
        {
            return;
        }

        StopAndFree(_ambientCrossfade.Outgoing);
        _ambientCrossfade.Incoming.VolumeDb =
            _ambientCrossfade.IncomingTargetVolumeDb;
        _ambientCrossfade = null;
    }

    public void SetAmbientContext(PixelAmbientContext context)
    {
        if (_ambientContext == context)
        {
            return;
        }

        _ambientContext = context;
        if (!_enabled || _ambientPlayer is null)
        {
            return;
        }

        var profile = PixelAudioProfile.Ambient(context);
        var incoming = new AudioStreamPlayer
        {
            Stream = BuildAmbient(context),
            VolumeDb = PixelAudioProfile.SilentVolumeDb
        };
        if (_ambientCrossfade is not null)
        {
            StopAndFree(_ambientCrossfade.Outgoing);
            _ambientCrossfade = null;
        }

        var outgoing = _ambientPlayer;
        AddChild(incoming);
        incoming.Play();
        _ambientPlayer = incoming;
        _ambientCrossfade = new AmbientCrossfade(
            outgoing,
            incoming,
            outgoing.VolumeDb,
            AmbientVolumeDb(profile)
        );
    }

    public void SetMasterVolumeDb(float volumeDb)
    {
        _masterVolumeDb = NormalizeVolumeDb(volumeDb);
        RefreshAmbientVolume();
    }

    public void SetMasterVolumePercent(int percent) =>
        SetMasterVolumeDb(PixelAudioProfile.VolumeDbForPercent(percent));

    public void SetAmbientVolumePercent(int percent)
    {
        _ambientVolumeDb = PixelAudioProfile.VolumeDbForPercent(percent);
        RefreshAmbientVolume();
    }

    public void SetEffectsVolumePercent(int percent) =>
        _effectsVolumeDb = PixelAudioProfile.VolumeDbForPercent(percent);

    public void SetMixVolumePercents(
        int masterPercent,
        int ambientPercent,
        int effectsPercent
    )
    {
        _masterVolumeDb = PixelAudioProfile.VolumeDbForPercent(masterPercent);
        _ambientVolumeDb = PixelAudioProfile.VolumeDbForPercent(ambientPercent);
        _effectsVolumeDb = PixelAudioProfile.VolumeDbForPercent(effectsPercent);
        RefreshAmbientVolume();
    }

    public void ApplySettings(AccessibilitySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Normalize();
        SetMixVolumePercents(
            settings.MasterVolumePercent,
            settings.AmbientVolumePercent,
            settings.EffectsVolumePercent
        );
    }

    public void ApplyRuntimeContext(PixelAudioRuntimeContext context) =>
        SetAmbientContext(PixelAudioContextResolver.Resolve(context));

    public void Play(PixelSound sound)
    {
        if (!_enabled)
        {
            return;
        }

        var player = new AudioStreamPlayer
        {
            Stream = BuildEffect(sound),
            VolumeDb = EffectVolumeDb(PixelAudioProfile.Effect(sound))
        };
        AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }

    public override void _ExitTree()
    {
        if (_ambientCrossfade is not null)
        {
            StopAndFree(_ambientCrossfade.Outgoing);
            _ambientCrossfade = null;
        }

        if (_ambientPlayer is null)
        {
            return;
        }

        StopAndFree(_ambientPlayer);
        _ambientPlayer = null;
    }

    private AudioStreamWav BuildEffect(PixelSound sound)
    {
        var data = PixelAudioSynthesis.RenderEffectPcm16(sound);
        return BuildWav(data);
    }

    private AudioStreamWav BuildAmbient(PixelAmbientContext context)
    {
        var profile = PixelAudioProfile.Ambient(context);
        var data = PixelAudioSynthesis.RenderAmbientPcm16(context);
        var stream = BuildWav(data);
        stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        stream.LoopBegin = 0;
        stream.LoopEnd = PixelAudioProfile.SampleCount(
            profile.DurationSeconds
        );
        return stream;
    }

    private static AudioStreamWav BuildWav(byte[] data) =>
        new()
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = SampleRate,
            Stereo = false,
            Data = data
        };

    private void LoadPersistedSettings()
    {
        var path = ProjectSettings.GlobalizePath("user://settings.json");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        ApplySettings(new AccessibilitySettingsService(path).Load());
    }

    private void RefreshAmbientVolume()
    {
        var profile = PixelAudioProfile.Ambient(_ambientContext);
        var volumeDb = AmbientVolumeDb(profile);
        if (_ambientCrossfade is not null)
        {
            _ambientCrossfade.IncomingTargetVolumeDb = volumeDb;
            return;
        }

        if (_ambientPlayer is not null)
        {
            _ambientPlayer.VolumeDb = volumeDb;
        }
    }

    private float AmbientVolumeDb(PixelAmbientProfile profile) =>
        ProfileVolumeDb(profile.VolumeDb, _ambientVolumeDb);

    private float EffectVolumeDb(PixelEffectProfile profile) =>
        ProfileVolumeDb(profile.VolumeDb, _effectsVolumeDb);

    private float ProfileVolumeDb(float profileVolumeDb, float channelVolumeDb)
    {
        var mixVolumeDb = MixVolumeDb(channelVolumeDb);
        if (mixVolumeDb <= PixelAudioProfile.SilentVolumeDb)
        {
            return PixelAudioProfile.SilentVolumeDb;
        }

        return Math.Max(
            PixelAudioProfile.SilentVolumeDb,
            profileVolumeDb + mixVolumeDb
        );
    }

    private float MixVolumeDb(float channelVolumeDb)
    {
        if (_masterVolumeDb <= PixelAudioProfile.SilentVolumeDb ||
            channelVolumeDb <= PixelAudioProfile.SilentVolumeDb)
        {
            return PixelAudioProfile.SilentVolumeDb;
        }

        return Math.Max(
            PixelAudioProfile.SilentVolumeDb,
            _masterVolumeDb + channelVolumeDb
        );
    }

    private static float NormalizeVolumeDb(float volumeDb)
    {
        if (!float.IsFinite(volumeDb))
        {
            return 0;
        }

        return Math.Clamp(volumeDb, PixelAudioProfile.SilentVolumeDb, 0);
    }

    private static void StopAndFree(AudioStreamPlayer player)
    {
        player.Stop();
        player.Stream = null!;
        player.QueueFree();
    }
}
