using System.Text;
using Luminfield.Core;

namespace Luminfield.Game;

public enum PixelAmbientContext
{
    HomesteadClear,
    VillageClear,
    WildsClear,
    RainveilRain,
    StardustWind,
    LongnightSnow,
    Festival,
    Combat
}

public enum PixelEffectFamily
{
    Ground,
    Water,
    Growth,
    Pickup,
    Ui,
    Rest,
    Alert,
    Impact,
    Dodge,
    Fishing
}

public readonly record struct PixelAudioRuntimeContext(
    string? LocationId,
    int Day,
    string? WeatherId,
    bool CombatActive = false,
    bool FestivalActive = false,
    WorldBiome? Region = null
);

public readonly record struct PixelEffectProfile(
    double DurationSeconds,
    double StartFrequency,
    double EndFrequency,
    double Noise,
    float VolumeDb,
    PixelEffectFamily Family,
    double PulseRate = 0
);

public readonly record struct PixelAmbientLayer(
    double Frequency,
    double Amplitude,
    double PhaseRadians = 0
);

public readonly record struct PixelAmbientNote(
    double StartSeconds,
    double DurationSeconds,
    double Frequency,
    double Amplitude
);

public sealed record PixelAmbientProfile(
    PixelAmbientContext Context,
    double DurationSeconds,
    IReadOnlyList<PixelAmbientLayer> PadLayers,
    IReadOnlyList<PixelAmbientNote> MotifNotes,
    double NoiseAmount,
    double PulseRate,
    float VolumeDb
);

public readonly record struct PixelAudioTechnicalReport(
    int SampleCount,
    double DurationSeconds,
    double PeakAmplitude,
    double RmsAmplitude,
    double LoopEdgeAmplitude
);

public readonly record struct PixelAudioBandEnergy(
    double LowRatio,
    double MidRatio,
    double HighRatio
);

public readonly record struct PixelAudioQualityReport(
    string SourceId,
    int SampleCount,
    double DurationSeconds,
    double PeakAmplitude,
    double RmsAmplitude,
    double RmsDb,
    double CrestFactor,
    double ClippedSampleRatio,
    PixelAudioBandEnergy BandEnergy,
    double LoopEdgeAmplitude,
    double LoopWindowMaxDelta,
    double LoopWindowRmsDelta,
    double EnvelopeVariation,
    double ZeroCrossingRate
);

public readonly record struct PixelAudioCrossfadeQualityReport(
    string TransitionId,
    double StartSeconds,
    double DurationSeconds,
    double PeakAmplitude,
    double RmsAmplitude,
    double MaxAdjacentDelta,
    double RmsAdjacentDelta
);

public readonly record struct PixelAudioTourCue(
    PixelSound Sound,
    double OffsetSeconds
);

public sealed record PixelAudioTourSegment(
    PixelAmbientContext Context,
    double StartSeconds,
    double DurationSeconds,
    IReadOnlyList<PixelAudioTourCue> Cues
);

public readonly record struct PixelAudioTourTransition(
    PixelAmbientContext From,
    PixelAmbientContext To,
    double StartSeconds,
    double DurationSeconds
);

public sealed record PixelAudioTourPlan(
    IReadOnlyList<PixelAudioTourSegment> Segments,
    IReadOnlyList<PixelAudioTourTransition> Transitions,
    double DurationSeconds
);

public static class PixelAudioProfile
{
    public const int SampleRate = 22050;
    public const double LoopFadeSeconds = 2.0;
    public const float SilentVolumeDb = -80f;
    public const double AmbientCrossfadeSeconds = 0.45;

    public static PixelEffectProfile Effect(PixelSound sound) => sound switch
    {
        PixelSound.Till => new(
            0.14,
            122,
            68,
            0.36,
            -9,
            PixelEffectFamily.Ground
        ),
        PixelSound.Water => new(
            0.26,
            460,
            230,
            0.24,
            -10,
            PixelEffectFamily.Water
        ),
        PixelSound.Plant => new(
            0.18,
            330,
            560,
            0.08,
            -10,
            PixelEffectFamily.Growth
        ),
        PixelSound.Harvest => new(
            0.32,
            520,
            1040,
            0.04,
            -8,
            PixelEffectFamily.Pickup
        ),
        PixelSound.Step => new(
            0.065,
            95,
            72,
            0.45,
            -20,
            PixelEffectFamily.Ground
        ),
        PixelSound.Chime => new(
            0.62,
            660,
            990,
            0.01,
            -9,
            PixelEffectFamily.Ui
        ),
        PixelSound.Sleep => new(
            0.95,
            440,
            165,
            0.02,
            -10,
            PixelEffectFamily.Rest
        ),
        PixelSound.Error => new(
            0.22,
            180,
            120,
            0.12,
            -11,
            PixelEffectFamily.Alert,
            18
        ),
        PixelSound.ResourceBlocked => new(
            0.34,
            260,
            155,
            0.22,
            -10,
            PixelEffectFamily.Alert,
            10
        ),
        PixelSound.ToolMismatch => new(
            0.30,
            760,
            310,
            0.07,
            -10,
            PixelEffectFamily.Alert,
            24
        ),
        PixelSound.Pickup => new(
            0.24,
            420,
            760,
            0.05,
            -10,
            PixelEffectFamily.Pickup
        ),
        PixelSound.Damage => new(
            0.18,
            150,
            80,
            0.32,
            -8,
            PixelEffectFamily.Impact
        ),
        PixelSound.Dodge => new(
            0.16,
            620,
            360,
            0.18,
            -12,
            PixelEffectFamily.Dodge
        ),
        PixelSound.FishBite => new(
            0.28,
            300,
            820,
            0.16,
            -10,
            PixelEffectFamily.Fishing,
            9
        ),
        PixelSound.Reward => new(
            0.82,
            520,
            1180,
            0.02,
            -8,
            PixelEffectFamily.Ui
        ),
        _ => new(
            0.18,
            300,
            300,
            0.05,
            -10,
            PixelEffectFamily.Ui
        )
    };

    public static PixelAmbientProfile Ambient(PixelAmbientContext context) =>
        context switch
        {
            PixelAmbientContext.VillageClear => new(
                context,
                34,
                [
                    new(196, 0.062),
                    new(294, 0.047, Math.PI / 5),
                    new(392, 0.031, Math.PI / 7),
                    new(784, 0.017, Math.PI / 9)
                ],
                [
                    new(4.0, 1.4, 882, 0.044),
                    new(11.0, 1.8, 1176, 0.036),
                    new(20.0, 1.6, 735, 0.038),
                    new(28.0, 1.5, 1029, 0.032)
                ],
                0.009,
                0.12,
                -17
            ),
            PixelAmbientContext.WildsClear => new(
                context,
                38,
                [
                    new(82, 0.072),
                    new(123, 0.045, Math.PI / 3),
                    new(205, 0.029, Math.PI / 6),
                    new(410, 0.014, Math.PI / 10)
                ],
                [
                    new(8.0, 2.2, 492, 0.036),
                    new(19.0, 2.8, 328, 0.031),
                    new(30.0, 2.0, 656, 0.034)
                ],
                0.028,
                0.06,
                -17
            ),
            PixelAmbientContext.RainveilRain => new(
                context,
                36,
                [
                    new(72, 0.07),
                    new(108, 0.052, Math.PI / 3),
                    new(216, 0.024, Math.PI / 5),
                    new(432, 0.012, Math.PI / 7)
                ],
                [
                    new(7.5, 1.8, 540, 0.042),
                    new(15.5, 2.2, 432, 0.038),
                    new(25.0, 1.6, 648, 0.034)
                ],
                0.052,
                0,
                -16
            ),
            PixelAmbientContext.StardustWind => new(
                context,
                36,
                [
                    new(90, 0.056),
                    new(135, 0.046, Math.PI / 4),
                    new(270, 0.028, Math.PI / 6),
                    new(540, 0.015, Math.PI / 8)
                ],
                [
                    new(5.0, 2.6, 810, 0.048),
                    new(17.0, 2.0, 1080, 0.035),
                    new(27.5, 2.4, 675, 0.04)
                ],
                0.018,
                0.18,
                -17
            ),
            PixelAmbientContext.LongnightSnow => new(
                context,
                40,
                [
                    new(55, 0.078),
                    new(110, 0.045, Math.PI / 2),
                    new(165, 0.032, Math.PI / 6),
                    new(330, 0.012, Math.PI / 9)
                ],
                [
                    new(10.0, 3.0, 440, 0.032),
                    new(24.0, 3.5, 330, 0.028),
                    new(32.0, 2.4, 550, 0.022)
                ],
                0.026,
                0.08,
                -15
            ),
            PixelAmbientContext.Festival => new(
                context,
                32,
                [
                    new(132, 0.068),
                    new(198, 0.05, Math.PI / 5),
                    new(264, 0.04, Math.PI / 8),
                    new(528, 0.022, Math.PI / 11)
                ],
                [
                    new(3.0, 1.2, 660, 0.052),
                    new(7.0, 1.1, 825, 0.05),
                    new(14.0, 1.6, 990, 0.046),
                    new(23.0, 2.0, 792, 0.04)
                ],
                0.012,
                0.35,
                -14
            ),
            PixelAmbientContext.Combat => new(
                context,
                24,
                [
                    new(60, 0.092),
                    new(90, 0.05, Math.PI / 4),
                    new(180, 0.034, Math.PI / 6),
                    new(300, 0.022, Math.PI / 9)
                ],
                [
                    new(4.0, 0.9, 360, 0.046),
                    new(10.0, 0.9, 420, 0.044),
                    new(16.0, 1.1, 300, 0.05)
                ],
                0.018,
                0.72,
                -13
            ),
            _ => new(
                PixelAmbientContext.HomesteadClear,
                32,
                [
                    new(110, 0.075),
                    new(165, 0.048, Math.PI / 4),
                    new(220, 0.028, Math.PI / 6),
                    new(330, 0.015, Math.PI / 8)
                ],
                [
                    new(6.0, 1.5, 660, 0.04),
                    new(13.0, 2.0, 880, 0.032),
                    new(22.5, 1.8, 550, 0.034)
                ],
                0.006,
                0,
                -18
            )
        };

    public static int SampleCount(double durationSeconds) =>
        Math.Max(1, (int)(SampleRate * durationSeconds));

    public static int SampleOffset(double seconds) =>
        Math.Max(0, (int)(SampleRate * seconds));

    public static float VolumeDbForPercent(int percent)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        if (clamped == 0)
        {
            return SilentVolumeDb;
        }

        return 20f * MathF.Log10(clamped / 100f);
    }

    public static float MasterVolumeDbForPercent(int percent) =>
        VolumeDbForPercent(percent);

    public static float MixedVolumeDb(
        int masterPercent,
        int channelPercent
    )
    {
        var masterVolumeDb = VolumeDbForPercent(masterPercent);
        var channelVolumeDb = VolumeDbForPercent(channelPercent);
        if (masterVolumeDb <= SilentVolumeDb ||
            channelVolumeDb <= SilentVolumeDb)
        {
            return SilentVolumeDb;
        }

        return Math.Max(SilentVolumeDb, masterVolumeDb + channelVolumeDb);
    }

    public static double CrossfadeProgress(
        double elapsedSeconds,
        double durationSeconds
    )
    {
        if (durationSeconds <= 0)
        {
            return 1;
        }

        return Math.Clamp(elapsedSeconds / durationSeconds, 0, 1);
    }

    public static float CrossfadeVolumeDb(
        double elapsedSeconds,
        double durationSeconds,
        float fromVolumeDb,
        float toVolumeDb
    )
    {
        var progress = CrossfadeProgress(elapsedSeconds, durationSeconds);
        return fromVolumeDb + (toVolumeDb - fromVolumeDb) * (float)progress;
    }

    public static double LoopFadeMultiplier(double time, double durationSeconds)
    {
        var fadeSeconds = Math.Min(LoopFadeSeconds, durationSeconds / 4);
        var fadeIn = time / fadeSeconds;
        var fadeOut = (durationSeconds - time) / fadeSeconds;
        return Math.Clamp(Math.Min(fadeIn, fadeOut), 0, 1);
    }

    public static double NoteEnvelope(
        double noteTime,
        double noteDurationSeconds
    )
    {
        if (noteTime < 0 || noteTime > noteDurationSeconds)
        {
            return 0;
        }

        var edge = Math.Min(0.18, noteDurationSeconds / 3);
        var attack = noteTime / edge;
        var release = (noteDurationSeconds - noteTime) / edge;
        return Math.Clamp(Math.Min(attack, release), 0, 1);
    }
}

public static class PixelAudioContextResolver
{
    public static PixelAmbientContext Resolve(PixelAudioRuntimeContext context)
    {
        if (context.CombatActive ||
            context.LocationId == PlayerLocationIds.StarfallRuinsTrial)
        {
            return PixelAmbientContext.Combat;
        }

        if (context.FestivalActive || IsFestivalLocation(context.LocationId))
        {
            return PixelAmbientContext.Festival;
        }

        var day = Math.Max(1, context.Day);
        var weatherId = NormalizeWeather(day, context.WeatherId);
        if (weatherId == DataCatalog.LongnightSnowWeatherId)
        {
            return PixelAmbientContext.LongnightSnow;
        }

        if (weatherId == DataCatalog.StardustWindWeatherId)
        {
            return PixelAmbientContext.StardustWind;
        }

        if (weatherId == DataCatalog.RainWeatherId ||
            CalendarSystem.SeasonId(day) == CalendarSystem.RainveilSeasonId)
        {
            return PixelAmbientContext.RainveilRain;
        }

        return ResolveClearContext(context);
    }

    private static PixelAmbientContext ResolveClearContext(
        PixelAudioRuntimeContext context
    )
    {
        if (context.LocationId == PlayerLocationIds.World)
        {
            return context.Region switch
            {
                WorldBiome.LumenVillage => PixelAmbientContext.VillageClear,
                WorldBiome.Home => PixelAmbientContext.HomesteadClear,
                _ => PixelAmbientContext.WildsClear
            };
        }

        if (IsVillageInterior(context.LocationId))
        {
            return PixelAmbientContext.VillageClear;
        }

        if (context.LocationId == PlayerLocationIds.CrystalGrottoSurvey)
        {
            return PixelAmbientContext.WildsClear;
        }

        return PixelAmbientContext.HomesteadClear;
    }

    private static string NormalizeWeather(int day, string? weatherId)
    {
        if (!string.IsNullOrWhiteSpace(weatherId) &&
            DataCatalog.WeatherDefinitions.ContainsKey(weatherId))
        {
            return weatherId;
        }

        return WeatherSystem.WeatherForDay(day);
    }

    private static bool IsFestivalLocation(string? locationId) =>
        locationId is PlayerLocationIds.StarharvestMarket or
            PlayerLocationIds.GleamrisePlantingFestival or
            PlayerLocationIds.LongnightLanternFeast or
            PlayerLocationIds.FireflyTide;

    private static bool IsVillageInterior(string? locationId) =>
        locationId is PlayerLocationIds.MoonlitArchive or
            PlayerLocationIds.MoonstoneWorkshop or
            PlayerLocationIds.StarweaverTeaHouse or
            PlayerLocationIds.TwilightEmporium or
            PlayerLocationIds.StarlightPost or
            PlayerLocationIds.StarfallWatch;
}

public static class PixelAudioSynthesis
{
    public static byte[] RenderAmbientPcm16(PixelAmbientContext context)
    {
        var profile = PixelAudioProfile.Ambient(context);
        return RenderPcm16(profile.DurationSeconds, sampleIndex =>
            AmbientSample(context, sampleIndex, profile.DurationSeconds, true)
        );
    }

    public static byte[] RenderAmbientExcerptPcm16(
        PixelAmbientContext context,
        double durationSeconds
    ) => RenderPcm16(durationSeconds, sampleIndex =>
        AmbientSample(context, sampleIndex, durationSeconds, false)
    );

    public static byte[] RenderEffectPcm16(PixelSound sound)
    {
        var profile = PixelAudioProfile.Effect(sound);
        var sampleCount = PixelAudioProfile.SampleCount(profile.DurationSeconds);
        return RenderPcm16(profile.DurationSeconds, sampleIndex =>
            EffectSample(sound, sampleIndex, sampleCount)
        );
    }

    public static PixelAudioTechnicalReport AnalyzePcm16(
        byte[] pcm16,
        double durationSeconds
    )
    {
        if (pcm16.Length == 0)
        {
            return new PixelAudioTechnicalReport(0, 0, 0, 0, 0);
        }

        var samples = pcm16.Length / 2;
        var peak = 0.0;
        var sumSquares = 0.0;
        var first = SampleAtPcm16(pcm16, 0);
        var last = SampleAtPcm16(pcm16, samples - 1);
        for (var sample = 0; sample < samples; sample++)
        {
            var value = SampleAtPcm16(pcm16, sample);
            peak = Math.Max(peak, Math.Abs(value));
            sumSquares += value * value;
        }

        return new PixelAudioTechnicalReport(
            samples,
            durationSeconds,
            peak,
            Math.Sqrt(sumSquares / samples),
            Math.Abs(first - last)
        );
    }

    public static double SampleAtPcm16(byte[] pcm16, int sampleIndex)
    {
        var offset = sampleIndex * 2;
        if (sampleIndex < 0 || offset + 1 >= pcm16.Length)
        {
            return 0;
        }

        var signed = (short)(pcm16[offset] | (pcm16[offset + 1] << 8));
        return signed / (double)short.MaxValue;
    }

    public static byte[] ToPcm16(IReadOnlyList<double> samples)
    {
        var data = new byte[samples.Count * 2];
        for (var index = 0; index < samples.Count; index++)
        {
            WriteSample(data, index, samples[index]);
        }

        return data;
    }

    public static byte[] ToWavBytes(byte[] pcm16)
    {
        const int channels = 1;
        const int bitsPerSample = 16;
        var byteRate = PixelAudioProfile.SampleRate * channels *
            bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var bytes = new byte[44 + pcm16.Length];
        WriteAscii(bytes, 0, "RIFF");
        WriteInt32(bytes, 4, 36 + pcm16.Length);
        WriteAscii(bytes, 8, "WAVE");
        WriteAscii(bytes, 12, "fmt ");
        WriteInt32(bytes, 16, 16);
        WriteInt16(bytes, 20, 1);
        WriteInt16(bytes, 22, channels);
        WriteInt32(bytes, 24, PixelAudioProfile.SampleRate);
        WriteInt32(bytes, 28, byteRate);
        WriteInt16(bytes, 32, blockAlign);
        WriteInt16(bytes, 34, bitsPerSample);
        WriteAscii(bytes, 36, "data");
        WriteInt32(bytes, 40, pcm16.Length);
        Buffer.BlockCopy(pcm16, 0, bytes, 44, pcm16.Length);
        return bytes;
    }

    private static double AmbientSample(
        PixelAmbientContext context,
        int sampleIndex,
        double durationSeconds,
        bool applyLoopFade
    )
    {
        var profile = PixelAudioProfile.Ambient(context);
        var time = sampleIndex / (double)PixelAudioProfile.SampleRate;
        var pad = 0.0;
        foreach (var layer in profile.PadLayers)
        {
            pad += Math.Sin(
                2 * Math.PI * layer.Frequency * time +
                layer.PhaseRadians
            ) * layer.Amplitude;
        }

        var motif = 0.0;
        foreach (var note in profile.MotifNotes)
        {
            var noteTime = time - note.StartSeconds;
            motif += Math.Sin(2 * Math.PI * note.Frequency * time) *
                note.Amplitude *
                PixelAudioProfile.NoteEnvelope(
                    noteTime,
                    note.DurationSeconds
                );
        }

        var noise = StableNoise(sampleIndex, (int)context + 71024) *
            profile.NoiseAmount;
        var pulse = profile.PulseRate <= 0
            ? 1.0
            : 0.72 + 0.28 *
                Math.Sin(2 * Math.PI * profile.PulseRate * time);
        var fade = applyLoopFade
            ? PixelAudioProfile.LoopFadeMultiplier(time, durationSeconds)
            : 1.0;
        return ((pad * pulse) + motif + noise) * fade;
    }

    private static double EffectSample(
        PixelSound sound,
        int sampleIndex,
        int sampleCount
    )
    {
        var profile = PixelAudioProfile.Effect(sound);
        var progress = sampleIndex / (double)sampleCount;
        var frequency = Lerp(
            profile.StartFrequency,
            profile.EndFrequency,
            progress
        );
        var envelope = Math.Sin(Math.PI * progress);
        var tone = Math.Sin(
            2 * Math.PI * frequency * sampleIndex /
            PixelAudioProfile.SampleRate
        );
        var noise = StableNoise(sampleIndex, (int)sound + 4009) *
            profile.Noise;
        var pulse = profile.PulseRate <= 0
            ? 1.0
            : 0.65 + 0.35 *
                Math.Sin(
                    2 * Math.PI * profile.PulseRate *
                    sampleIndex / PixelAudioProfile.SampleRate
                );
        return ((tone * (1 - profile.Noise) + noise) * envelope * 0.55) *
            pulse;
    }

    private static byte[] RenderPcm16(
        double durationSeconds,
        Func<int, double> sample
    )
    {
        var sampleCount = PixelAudioProfile.SampleCount(durationSeconds);
        var data = new byte[sampleCount * 2];
        for (var index = 0; index < sampleCount; index++)
        {
            WriteSample(data, index, sample(index));
        }

        return data;
    }

    private static double StableNoise(int sampleIndex, int seed)
    {
        unchecked
        {
            var value = (uint)(sampleIndex + seed * 374761393);
            value = (value ^ (value >> 13)) * 1274126177u;
            value ^= value >> 16;
            return value / (double)uint.MaxValue * 2 - 1;
        }
    }

    private static double Lerp(double start, double end, double amount) =>
        start + (end - start) * amount;

    private static void WriteSample(
        byte[] target,
        int sampleIndex,
        double value
    )
    {
        var clamped = Math.Clamp(value, -1, 1);
        var signed = (short)(clamped * short.MaxValue);
        target[sampleIndex * 2] = (byte)(signed & 0xff);
        target[sampleIndex * 2 + 1] = (byte)((signed >> 8) & 0xff);
    }

    private static void WriteAscii(byte[] target, int offset, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, target, offset, bytes.Length);
    }

    private static void WriteInt16(byte[] target, int offset, int value)
    {
        target[offset] = (byte)(value & 0xff);
        target[offset + 1] = (byte)((value >> 8) & 0xff);
    }

    private static void WriteInt32(byte[] target, int offset, int value)
    {
        target[offset] = (byte)(value & 0xff);
        target[offset + 1] = (byte)((value >> 8) & 0xff);
        target[offset + 2] = (byte)((value >> 16) & 0xff);
        target[offset + 3] = (byte)((value >> 24) & 0xff);
    }
}

public static class PixelAudioQualityThresholds
{
    public const double MaxPeakAmplitude = 0.95;
    public const double MaxClippedSampleRatio = 0;
    public const double MinAmbientRmsDb = -34;
    public const double MaxAmbientRmsDb = -20;
    public const double MinEffectRmsDb = -18;
    public const double MaxEffectRmsDb = -8;
    public const double MinTourRmsDb = -24;
    public const double MaxTourRmsDb = -16;
    public const double MinCrestFactor = 1.4;
    public const double MaxCrestFactor = 12;
    public const double MinLowEnergyRatio = 0.35;
    public const double MaxLowEnergyRatio = 0.94;
    public const double MinMidEnergyRatio = 0.05;
    public const double MinHighEnergyRatio = 0.001;
    public const double MaxHighEnergyRatio = 0.25;
    public const double MaxLoopWindowMaxDelta = 0.01;
    public const double MaxLoopWindowRmsDelta = 0.004;
    public const double MinAmbientSeparationDistance = 0.12;
    public const double MaxCrossfadeAdjacentDelta = 0.05;
    public const double MaxCrossfadeRmsAdjacentDelta = 0.016;
}

public static class PixelAudioQualityAnalyzer
{
    public const double LowBandCutoffHz = 250;
    public const double HighBandCutoffHz = 2000;
    public const double LoopComparisonWindowSeconds = 0.05;
    public const double EnvelopeWindowSeconds = 0.5;

    public static PixelAudioQualityReport AnalyzePcm16(
        string sourceId,
        byte[] pcm16,
        double durationSeconds
    )
    {
        ArgumentNullException.ThrowIfNull(pcm16);
        var sampleCount = pcm16.Length / 2;
        if (sampleCount <= 0)
        {
            return new PixelAudioQualityReport(
                sourceId,
                0,
                0,
                0,
                0,
                PixelAudioProfile.SilentVolumeDb,
                0,
                0,
                new PixelAudioBandEnergy(0, 0, 0),
                0,
                0,
                0,
                0,
                0
            );
        }

        var peak = 0.0;
        var sumSquares = 0.0;
        var clippedSamples = 0;
        var first = PixelAudioSynthesis.SampleAtPcm16(pcm16, 0);
        var last = PixelAudioSynthesis.SampleAtPcm16(pcm16, sampleCount - 1);
        var previous = first;
        var zeroCrossings = 0;
        var lowPass = 0.0;
        var highPassSource = 0.0;
        var lowBandEnergy = 0.0;
        var midBandEnergy = 0.0;
        var highBandEnergy = 0.0;
        var lowAlpha = LowPassAlpha(LowBandCutoffHz);
        var highAlpha = LowPassAlpha(HighBandCutoffHz);
        for (var index = 0; index < sampleCount; index++)
        {
            var sample = PixelAudioSynthesis.SampleAtPcm16(pcm16, index);
            var absolute = Math.Abs(sample);
            peak = Math.Max(peak, absolute);
            sumSquares += sample * sample;
            if (absolute >= 0.999)
            {
                clippedSamples++;
            }

            if (index > 0 &&
                ((previous < 0 && sample >= 0) ||
                    (previous >= 0 && sample < 0)))
            {
                zeroCrossings++;
            }

            lowPass += lowAlpha * (sample - lowPass);
            highPassSource += highAlpha * (sample - highPassSource);
            var lowBand = lowPass;
            var midBand = highPassSource - lowPass;
            var highBand = sample - highPassSource;
            lowBandEnergy += lowBand * lowBand;
            midBandEnergy += midBand * midBand;
            highBandEnergy += highBand * highBand;
            previous = sample;
        }

        var rms = Math.Sqrt(sumSquares / sampleCount);
        var totalBandEnergy = lowBandEnergy + midBandEnergy + highBandEnergy;
        var bandEnergy = totalBandEnergy <= 0
            ? new PixelAudioBandEnergy(0, 0, 0)
            : new PixelAudioBandEnergy(
                lowBandEnergy / totalBandEnergy,
                midBandEnergy / totalBandEnergy,
                highBandEnergy / totalBandEnergy
            );
        var loopDelta = LoopWindowDelta(pcm16, sampleCount);
        return new PixelAudioQualityReport(
            sourceId,
            sampleCount,
            durationSeconds,
            peak,
            rms,
            AmplitudeDb(rms),
            rms <= 0 ? 0 : peak / rms,
            clippedSamples / (double)sampleCount,
            bandEnergy,
            Math.Abs(first - last),
            loopDelta.MaxDelta,
            loopDelta.RmsDelta,
            EnvelopeVariation(pcm16, sampleCount),
            zeroCrossings / (double)sampleCount
        );
    }

    public static PixelAudioCrossfadeQualityReport AnalyzeCrossfadeWindow(
        string transitionId,
        byte[] pcm16,
        double startSeconds,
        double durationSeconds
    )
    {
        ArgumentNullException.ThrowIfNull(pcm16);
        var totalSamples = pcm16.Length / 2;
        var startSample = PixelAudioProfile.SampleOffset(startSeconds);
        var requestedSamples = PixelAudioProfile.SampleCount(durationSeconds);
        var sampleCount = Math.Clamp(
            requestedSamples,
            1,
            Math.Max(1, totalSamples - startSample)
        );
        var peak = 0.0;
        var sumSquares = 0.0;
        var maxAdjacentDelta = 0.0;
        var sumDeltaSquares = 0.0;
        var previous = PixelAudioSynthesis.SampleAtPcm16(pcm16, startSample);
        for (var index = 0; index < sampleCount; index++)
        {
            var sample = PixelAudioSynthesis.SampleAtPcm16(
                pcm16,
                startSample + index
            );
            peak = Math.Max(peak, Math.Abs(sample));
            sumSquares += sample * sample;
            if (index <= 0)
            {
                continue;
            }

            var delta = Math.Abs(sample - previous);
            maxAdjacentDelta = Math.Max(maxAdjacentDelta, delta);
            sumDeltaSquares += delta * delta;
            previous = sample;
        }

        return new PixelAudioCrossfadeQualityReport(
            transitionId,
            startSeconds,
            durationSeconds,
            peak,
            Math.Sqrt(sumSquares / sampleCount),
            maxAdjacentDelta,
            Math.Sqrt(sumDeltaSquares / Math.Max(1, sampleCount - 1))
        );
    }

    public static double DistinguishabilityDistance(
        PixelAudioQualityReport left,
        PixelAudioQualityReport right
    )
    {
        var low = left.BandEnergy.LowRatio - right.BandEnergy.LowRatio;
        var mid = left.BandEnergy.MidRatio - right.BandEnergy.MidRatio;
        var high = left.BandEnergy.HighRatio - right.BandEnergy.HighRatio;
        var rms = (left.RmsAmplitude - right.RmsAmplitude) * 4;
        var peak = left.PeakAmplitude - right.PeakAmplitude;
        var envelope = (left.EnvelopeVariation - right.EnvelopeVariation) * 8;
        var zeroCrossing =
            (left.ZeroCrossingRate - right.ZeroCrossingRate) * 10;
        return Math.Sqrt(
            low * low +
            mid * mid +
            high * high +
            rms * rms +
            peak * peak +
            envelope * envelope +
            zeroCrossing * zeroCrossing
        );
    }

    public static double AmplitudeDb(double amplitude) =>
        amplitude <= 0
            ? PixelAudioProfile.SilentVolumeDb
            : 20 * Math.Log10(amplitude);

    private static (double MaxDelta, double RmsDelta) LoopWindowDelta(
        byte[] pcm16,
        int sampleCount
    )
    {
        var window = Math.Min(
            sampleCount / 2,
            PixelAudioProfile.SampleCount(LoopComparisonWindowSeconds)
        );
        if (window <= 0)
        {
            return (0, 0);
        }

        var maxDelta = 0.0;
        var sumSquares = 0.0;
        var tailStart = sampleCount - window;
        for (var index = 0; index < window; index++)
        {
            var head = PixelAudioSynthesis.SampleAtPcm16(pcm16, index);
            var tail = PixelAudioSynthesis.SampleAtPcm16(
                pcm16,
                tailStart + index
            );
            var delta = Math.Abs(head - tail);
            maxDelta = Math.Max(maxDelta, delta);
            sumSquares += delta * delta;
        }

        return (maxDelta, Math.Sqrt(sumSquares / window));
    }

    private static double EnvelopeVariation(byte[] pcm16, int sampleCount)
    {
        var window = PixelAudioProfile.SampleCount(EnvelopeWindowSeconds);
        if (sampleCount < window)
        {
            return 0;
        }

        var windows = new List<double>();
        for (var start = 0; start + window <= sampleCount; start += window)
        {
            var sumSquares = 0.0;
            for (var index = 0; index < window; index++)
            {
                var sample = PixelAudioSynthesis.SampleAtPcm16(
                    pcm16,
                    start + index
                );
                sumSquares += sample * sample;
            }

            windows.Add(Math.Sqrt(sumSquares / window));
        }

        var average = windows.Average();
        var variance = windows
            .Select(value => (value - average) * (value - average))
            .Average();
        return Math.Sqrt(variance);
    }

    private static double LowPassAlpha(double cutoffHz)
    {
        var rc = 1.0 / (2 * Math.PI * cutoffHz);
        var dt = 1.0 / PixelAudioProfile.SampleRate;
        return dt / (rc + dt);
    }
}

public static class PixelAudioAcceptanceTour
{
    public const double SegmentDurationSeconds = 8.0;
    public const string FileName = "audio-01-acceptance-tour.wav";
    private const float OutputGainDb = 10f;

    private static readonly PixelAmbientContext[] ContextOrder =
    [
        PixelAmbientContext.HomesteadClear,
        PixelAmbientContext.VillageClear,
        PixelAmbientContext.WildsClear,
        PixelAmbientContext.RainveilRain,
        PixelAmbientContext.StardustWind,
        PixelAmbientContext.LongnightSnow,
        PixelAmbientContext.Festival,
        PixelAmbientContext.Combat
    ];

    public static double DurationSeconds =>
        SegmentDurationSeconds * ContextOrder.Length -
        PixelAudioProfile.AmbientCrossfadeSeconds *
        (ContextOrder.Length - 1);

    private static readonly PixelSound[][] CueOrder =
    [
        [PixelSound.Step, PixelSound.Till, PixelSound.Water],
        [PixelSound.Pickup, PixelSound.Chime],
        [PixelSound.Plant, PixelSound.Harvest],
        [PixelSound.ResourceBlocked, PixelSound.Error],
        [PixelSound.ToolMismatch, PixelSound.FishBite],
        [PixelSound.Sleep, PixelSound.Chime],
        [PixelSound.Reward, PixelSound.Pickup],
        [
            PixelSound.Dodge,
            PixelSound.Damage
        ]
    ];

    public static PixelAudioTourPlan Plan()
    {
        var segments = new List<PixelAudioTourSegment>();
        var transitions = new List<PixelAudioTourTransition>();
        var crossfadeSeconds = PixelAudioProfile.AmbientCrossfadeSeconds;
        var strideSeconds = SegmentDurationSeconds - crossfadeSeconds;
        for (var index = 0; index < ContextOrder.Length; index++)
        {
            var startSeconds = index * strideSeconds;
            segments.Add(new PixelAudioTourSegment(
                ContextOrder[index],
                startSeconds,
                SegmentDurationSeconds,
                CuesFor(index)
            ));
            if (index == 0)
            {
                continue;
            }

            transitions.Add(new PixelAudioTourTransition(
                ContextOrder[index - 1],
                ContextOrder[index],
                startSeconds,
                crossfadeSeconds
            ));
        }

        return new PixelAudioTourPlan(
            segments,
            transitions,
            DurationSeconds
        );
    }

    public static byte[] RenderPcm16()
    {
        var plan = Plan();
        var outputSamples = new double[
            PixelAudioProfile.SampleCount(plan.DurationSeconds)
        ];
        foreach (var segment in plan.Segments)
        {
            MixAmbientSegment(outputSamples, segment);
            MixCues(outputSamples, segment);
        }

        return PixelAudioSynthesis.ToPcm16(outputSamples);
    }

    private static IReadOnlyList<PixelAudioTourCue> CuesFor(int segmentIndex)
    {
        var sounds = CueOrder[segmentIndex];
        var offsets = sounds.Length switch
        {
            4 => [1.15, 2.95, 4.75, 6.35],
            3 => [1.4, 3.55, 5.85],
            _ => new[] { 2.1, 5.45 }
        };
        return sounds
            .Select((sound, index) => new PixelAudioTourCue(
                sound,
                offsets[index]
            ))
            .ToArray();
    }

    private static void MixAmbientSegment(
        double[] outputSamples,
        PixelAudioTourSegment segment
    )
    {
        var pcm = PixelAudioSynthesis.RenderAmbientExcerptPcm16(
            segment.Context,
            segment.DurationSeconds
        );
        var profile = PixelAudioProfile.Ambient(segment.Context);
        var gain = LinearGain(profile.VolumeDb + OutputGainDb);
        var startSample = PixelAudioProfile.SampleOffset(segment.StartSeconds);
        var sampleCount = pcm.Length / 2;
        for (var index = 0; index < sampleCount; index++)
        {
            var outputIndex = startSample + index;
            if (outputIndex >= outputSamples.Length)
            {
                break;
            }

            var localTime = index / (double)PixelAudioProfile.SampleRate;
            outputSamples[outputIndex] +=
                PixelAudioSynthesis.SampleAtPcm16(pcm, index) *
                gain *
                SegmentEnvelope(localTime, segment);
        }
    }

    private static void MixCues(
        double[] outputSamples,
        PixelAudioTourSegment segment
    )
    {
        foreach (var cue in segment.Cues)
        {
            var pcm = PixelAudioSynthesis.RenderEffectPcm16(cue.Sound);
            var profile = PixelAudioProfile.Effect(cue.Sound);
            var gain = LinearGain(profile.VolumeDb + OutputGainDb);
            var startSeconds = segment.StartSeconds + cue.OffsetSeconds;
            var startSample = PixelAudioProfile.SampleOffset(startSeconds);
            var sampleCount = pcm.Length / 2;
            for (var index = 0; index < sampleCount; index++)
            {
                var outputIndex = startSample + index;
                if (outputIndex >= outputSamples.Length)
                {
                    break;
                }

                outputSamples[outputIndex] +=
                    PixelAudioSynthesis.SampleAtPcm16(pcm, index) * gain;
            }
        }
    }

    private static double SegmentEnvelope(
        double localTime,
        PixelAudioTourSegment segment
    )
    {
        var crossfadeSeconds = PixelAudioProfile.AmbientCrossfadeSeconds;
        var fadeIn = segment.StartSeconds <= 0
            ? 1.0
            : PixelAudioProfile.CrossfadeProgress(
                localTime,
                crossfadeSeconds
            );
        var hasOutgoingFade =
            segment.StartSeconds + segment.DurationSeconds < DurationSeconds;
        if (!hasOutgoingFade)
        {
            return fadeIn;
        }

        var remaining = segment.DurationSeconds - localTime;
        var fadeOut = PixelAudioProfile.CrossfadeProgress(
            remaining,
            crossfadeSeconds
        );
        return Math.Min(fadeIn, fadeOut);
    }

    private static double LinearGain(float volumeDb)
    {
        if (volumeDb <= PixelAudioProfile.SilentVolumeDb)
        {
            return 0;
        }

        return Math.Pow(10, volumeDb / 20.0);
    }
}

public static class PixelAudioAcceptanceTourExporter
{
    public static string Export(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, PixelAudioAcceptanceTour.FileName);
        File.WriteAllBytes(
            path,
            PixelAudioSynthesis.ToWavBytes(
                PixelAudioAcceptanceTour.RenderPcm16()
            )
        );
        return path;
    }
}

public static class PixelAudioPreviewExporter
{
    public static IReadOnlyList<string> ExportAll(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        var paths = new List<string>();
        foreach (var context in Enum.GetValues<PixelAmbientContext>())
        {
            var pcm = PixelAudioSynthesis.RenderAmbientPcm16(context);
            paths.Add(WriteWav(
                directory,
                $"ambient-{ToKebabCase(context.ToString())}.wav",
                pcm
            ));
        }

        foreach (var sound in Enum.GetValues<PixelSound>())
        {
            var pcm = PixelAudioSynthesis.RenderEffectPcm16(sound);
            paths.Add(WriteWav(
                directory,
                $"effect-{ToKebabCase(sound.ToString())}.wav",
                pcm
            ));
        }

        paths.Add(PixelAudioAcceptanceTourExporter.Export(directory));
        paths.Add(WriteReport(directory));
        return paths;
    }

    private static string WriteWav(
        string directory,
        string fileName,
        byte[] pcm16
    )
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, PixelAudioSynthesis.ToWavBytes(pcm16));
        return path;
    }

    private static string WriteReport(string directory)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# AUDIO-01 procedural preview report");
        builder.AppendLine();
        builder.AppendLine(
            "> Automated waveform proxy for pre-listening acceptance; this does not replace human audition."
        );
        builder.AppendLine();
        builder.AppendLine("## Quality gates");
        builder.AppendLine();
        builder.AppendLine(
            $"- Peak must stay below {PixelAudioQualityThresholds.MaxPeakAmplitude:0.00}; clipped sample ratio must be {PixelAudioQualityThresholds.MaxClippedSampleRatio:0.###}."
        );
        builder.AppendLine(
            $"- Ambient RMS must stay between {PixelAudioQualityThresholds.MinAmbientRmsDb:0.#} dB and {PixelAudioQualityThresholds.MaxAmbientRmsDb:0.#} dB; effects between {PixelAudioQualityThresholds.MinEffectRmsDb:0.#} dB and {PixelAudioQualityThresholds.MaxEffectRmsDb:0.#} dB; tour between {PixelAudioQualityThresholds.MinTourRmsDb:0.#} dB and {PixelAudioQualityThresholds.MaxTourRmsDb:0.#} dB."
        );
        builder.AppendLine(
            $"- Crest factor must stay in {PixelAudioQualityThresholds.MinCrestFactor:0.#}–{PixelAudioQualityThresholds.MaxCrestFactor:0.#}; ambient loop 50 ms window max/rms deltas must stay under {PixelAudioQualityThresholds.MaxLoopWindowMaxDelta:0.###}/{PixelAudioQualityThresholds.MaxLoopWindowRmsDelta:0.###}."
        );
        builder.AppendLine(
            $"- Ambient fingerprint distance must stay above {PixelAudioQualityThresholds.MinAmbientSeparationDistance:0.###}; 0.45 s crossfade adjacent max/rms deltas must stay under {PixelAudioQualityThresholds.MaxCrossfadeAdjacentDelta:0.###}/{PixelAudioQualityThresholds.MaxCrossfadeRmsAdjacentDelta:0.###}."
        );
        builder.AppendLine();
        builder.AppendLine("## Source quality");
        builder.AppendLine();
        builder.AppendLine(
            "| Source | Seconds | Samples | Peak | RMS | RMS dB | Crest | Clip % | Low | Mid | High | Loop edge | Loop max | Loop RMS |"
        );
        builder.AppendLine(
            "| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |"
        );
        var ambientReports = new List<PixelAudioQualityReport>();
        foreach (var context in Enum.GetValues<PixelAmbientContext>())
        {
            var profile = PixelAudioProfile.Ambient(context);
            var pcm = PixelAudioSynthesis.RenderAmbientPcm16(context);
            var report = PixelAudioQualityAnalyzer.AnalyzePcm16(
                $"ambient-{ToKebabCase(context.ToString())}",
                pcm,
                profile.DurationSeconds
            );
            ambientReports.Add(report);
            builder.AppendLine(QualityRow(report));
        }

        foreach (var sound in Enum.GetValues<PixelSound>())
        {
            var profile = PixelAudioProfile.Effect(sound);
            var pcm = PixelAudioSynthesis.RenderEffectPcm16(sound);
            var report = PixelAudioQualityAnalyzer.AnalyzePcm16(
                $"effect-{ToKebabCase(sound.ToString())}",
                pcm,
                profile.DurationSeconds
            );
            builder.AppendLine(QualityRow(report));
        }

        var tourPcm = PixelAudioAcceptanceTour.RenderPcm16();
        var tourReport = PixelAudioQualityAnalyzer.AnalyzePcm16(
            "acceptance-tour",
            tourPcm,
            PixelAudioAcceptanceTour.Plan().DurationSeconds
        );
        builder.AppendLine(QualityRow(tourReport));
        builder.AppendLine();
        builder.AppendLine("## Crossfade continuity");
        builder.AppendLine();
        builder.AppendLine(
            "| Transition | Start | Seconds | Peak | RMS | Max adjacent delta | RMS adjacent delta |"
        );
        builder.AppendLine(
            "| --- | ---: | ---: | ---: | ---: | ---: | ---: |"
        );
        foreach (var transition in PixelAudioAcceptanceTour.Plan().Transitions)
        {
            builder.AppendLine(CrossfadeRow(
                PixelAudioQualityAnalyzer.AnalyzeCrossfadeWindow(
                    $"{transition.From}->{transition.To}",
                    tourPcm,
                    transition.StartSeconds,
                    transition.DurationSeconds
                )
            ));
        }

        builder.AppendLine();
        builder.AppendLine("## Ambient distinguishability");
        builder.AppendLine();
        builder.AppendLine("| Source | Nearest source | Distance |");
        builder.AppendLine("| --- | --- | ---: |");
        foreach (var report in ambientReports)
        {
            var nearest = ambientReports
                .Where(candidate => candidate.SourceId != report.SourceId)
                .Select(candidate => new
                {
                    candidate.SourceId,
                    Distance = PixelAudioQualityAnalyzer
                        .DistinguishabilityDistance(report, candidate)
                })
                .OrderBy(candidate => candidate.Distance)
                .First();
            builder.AppendLine(SeparationRow(
                report.SourceId,
                nearest.SourceId,
                nearest.Distance
            ));
        }

        var path = Path.Combine(directory, "audio-01-technical-report.md");
        File.WriteAllText(path, builder.ToString());
        return path;
    }

    private static string QualityRow(PixelAudioQualityReport report) =>
        string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        "| {0} | {1:0.###} | {2} | {3:0.0000} | {4:0.0000} | {5:0.0} | {6:0.00} | {7:0.####} | {8:0.000} | {9:0.000} | {10:0.000} | {11:0.000000} | {12:0.000000} | {13:0.000000} |",
        report.SourceId,
        report.DurationSeconds,
        report.SampleCount,
        report.PeakAmplitude,
        report.RmsAmplitude,
        report.RmsDb,
        report.CrestFactor,
        report.ClippedSampleRatio,
        report.BandEnergy.LowRatio,
        report.BandEnergy.MidRatio,
        report.BandEnergy.HighRatio,
        report.LoopEdgeAmplitude,
        report.LoopWindowMaxDelta,
        report.LoopWindowRmsDelta
    );

    private static string CrossfadeRow(PixelAudioCrossfadeQualityReport report) =>
        string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "| {0} | {1:0.###} | {2:0.###} | {3:0.0000} | {4:0.0000} | {5:0.00000} | {6:0.00000} |",
            report.TransitionId,
            report.StartSeconds,
            report.DurationSeconds,
            report.PeakAmplitude,
            report.RmsAmplitude,
            report.MaxAdjacentDelta,
            report.RmsAdjacentDelta
        );

    private static string SeparationRow(
        string sourceId,
        string nearestSourceId,
        double distance
    ) => string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        "| {0} | {1} | {2:0.000} |",
        sourceId,
        nearestSourceId,
        distance
    );

    private static string ToKebabCase(string value)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0)
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
