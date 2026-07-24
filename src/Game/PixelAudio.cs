using Godot;

namespace Luminfield.Game;

public enum PixelSound
{
    Till,
    Water,
    Plant,
    Harvest,
    Step,
    Chime,
    Sleep
}

public sealed partial class PixelAudio : Node
{
    private const int SampleRate = 22050;
    private readonly Random _random = new(71024);
    private AudioStreamPlayer? _ambientPlayer;
    private bool _enabled;

    public override void _Ready()
    {
        if (DisplayServer.GetName() == "headless")
        {
            return;
        }

        _enabled = true;
        _ambientPlayer = new AudioStreamPlayer
        {
            Stream = BuildAmbient(),
            VolumeDb = -18,
            Autoplay = true
        };
        AddChild(_ambientPlayer);
    }

    public void Play(PixelSound sound)
    {
        if (!_enabled)
        {
            return;
        }

        var player = new AudioStreamPlayer
        {
            Stream = BuildEffect(sound),
            VolumeDb = sound == PixelSound.Step ? -19 : -9
        };
        AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }

    public override void _ExitTree()
    {
        if (_ambientPlayer is null)
        {
            return;
        }

        _ambientPlayer.Stop();
        _ambientPlayer.Stream = null!;
        _ambientPlayer = null;
    }

    private AudioStreamWav BuildEffect(PixelSound sound)
    {
        var (duration, startFrequency, endFrequency, noise) = sound switch
        {
            PixelSound.Till => (0.12, 115.0, 70.0, 0.35),
            PixelSound.Water => (0.22, 430.0, 240.0, 0.22),
            PixelSound.Plant => (0.12, 320.0, 520.0, 0.08),
            PixelSound.Harvest => (0.28, 540.0, 980.0, 0.04),
            PixelSound.Step => (0.06, 95.0, 75.0, 0.45),
            PixelSound.Chime => (0.55, 660.0, 990.0, 0.01),
            PixelSound.Sleep => (0.75, 440.0, 180.0, 0.02),
            _ => (0.15, 300.0, 300.0, 0.05)
        };

        return BuildWav(duration, (sample, total) =>
        {
            var progress = sample / (double)total;
            var frequency = Mathf.Lerp((float)startFrequency, (float)endFrequency, (float)progress);
            var envelope = Math.Sin(Math.PI * progress);
            var tone = Math.Sin(2 * Math.PI * frequency * sample / SampleRate);
            var random = (_random.NextDouble() * 2 - 1) * noise;
            return (tone * (1 - noise) + random) * envelope * 0.55;
        });
    }

    private AudioStreamWav BuildAmbient()
    {
        const double duration = 4.0;
        var stream = BuildWav(duration, (sample, total) =>
        {
            var time = sample / (double)SampleRate;
            var fade = Math.Min(1, Math.Min(time / 0.4, (duration - time) / 0.4));
            var pad =
                Math.Sin(2 * Math.PI * 110 * time) * 0.08 +
                Math.Sin(2 * Math.PI * 165 * time) * 0.05 +
                Math.Sin(2 * Math.PI * 220 * time) * 0.025;
            var chime = time is > 1.5 and < 2.2
                ? Math.Sin(2 * Math.PI * 880 * time) * Math.Exp(-(time - 1.5) * 5) * 0.08
                : 0;
            return (pad + chime) * fade;
        });
        stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        stream.LoopBegin = 0;
        stream.LoopEnd = stream.Data.Length / 2;
        return stream;
    }

    private static AudioStreamWav BuildWav(double duration, Func<int, int, double> sample)
    {
        var sampleCount = Math.Max(1, (int)(SampleRate * duration));
        var data = new byte[sampleCount * 2];
        for (var index = 0; index < sampleCount; index++)
        {
            var value = Math.Clamp(sample(index, sampleCount), -1, 1);
            var signed = (short)(value * short.MaxValue);
            data[index * 2] = (byte)(signed & 0xff);
            data[index * 2 + 1] = (byte)((signed >> 8) & 0xff);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = SampleRate,
            Stereo = false,
            Data = data
        };
    }
}
