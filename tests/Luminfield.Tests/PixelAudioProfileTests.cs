using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class PixelAudioProfileTests
{
    [Theory]
    [InlineData(PixelAmbientContext.HomesteadClear, 32)]
    [InlineData(PixelAmbientContext.VillageClear, 34)]
    [InlineData(PixelAmbientContext.WildsClear, 38)]
    [InlineData(PixelAmbientContext.RainveilRain, 36)]
    [InlineData(PixelAmbientContext.StardustWind, 36)]
    [InlineData(PixelAmbientContext.LongnightSnow, 40)]
    [InlineData(PixelAmbientContext.Festival, 32)]
    [InlineData(PixelAmbientContext.Combat, 24)]
    public void AmbientContextsAreLongerLoopableProceduralLayers(
        PixelAmbientContext context,
        double expectedDuration
    )
    {
        var profile = PixelAudioProfile.Ambient(context);

        Assert.Equal(expectedDuration, profile.DurationSeconds);
        Assert.True(profile.PadLayers.Count >= 4);
        Assert.True(profile.MotifNotes.Count >= 3);
        Assert.Equal(
            (int)(PixelAudioProfile.SampleRate * expectedDuration),
            PixelAudioProfile.SampleCount(profile.DurationSeconds)
        );
    }

    [Fact]
    public void WeatherAndActivityAmbiencesHaveDistinctLayerProfiles()
    {
        var clear = PixelAudioProfile.Ambient(PixelAmbientContext.HomesteadClear);
        var rain = PixelAudioProfile.Ambient(PixelAmbientContext.RainveilRain);
        var festival = PixelAudioProfile.Ambient(PixelAmbientContext.Festival);
        var combat = PixelAudioProfile.Ambient(PixelAmbientContext.Combat);

        Assert.NotEqual(clear.PadLayers[0].Frequency, rain.PadLayers[0].Frequency);
        Assert.True(festival.MotifNotes.Count > clear.MotifNotes.Count);
        Assert.True(combat.PulseRate > clear.PulseRate);
    }

    [Theory]
    [InlineData(PixelSound.Till, PixelEffectFamily.Ground)]
    [InlineData(PixelSound.Water, PixelEffectFamily.Water)]
    [InlineData(PixelSound.Harvest, PixelEffectFamily.Pickup)]
    [InlineData(PixelSound.Error, PixelEffectFamily.Alert)]
    [InlineData(PixelSound.ResourceBlocked, PixelEffectFamily.Alert)]
    [InlineData(PixelSound.ToolMismatch, PixelEffectFamily.Alert)]
    [InlineData(PixelSound.Damage, PixelEffectFamily.Impact)]
    [InlineData(PixelSound.FishBite, PixelEffectFamily.Fishing)]
    public void EffectProfilesDeclareActionFamilies(
        PixelSound sound,
        PixelEffectFamily expectedFamily
    )
    {
        var profile = PixelAudioProfile.Effect(sound);

        Assert.Equal(expectedFamily, profile.Family);
        Assert.InRange(profile.DurationSeconds, 0.05, 1.0);
        Assert.InRange(profile.VolumeDb, -24, -6);
    }

    [Fact]
    public void NegativeFeedbackSoundsKeepDistinctProfilesAndWaveformFingerprints()
    {
        var sounds = new[]
        {
            PixelSound.Error,
            PixelSound.ResourceBlocked,
            PixelSound.ToolMismatch
        };
        var profiles = sounds
            .Select(sound => (Sound: sound, Profile: PixelAudioProfile.Effect(sound)))
            .ToArray();
        var reports = sounds
            .Select(sound =>
            {
                var profile = PixelAudioProfile.Effect(sound);
                var pcm = PixelAudioSynthesis.RenderEffectPcm16(sound);
                return PixelAudioQualityAnalyzer.AnalyzePcm16(
                    sound.ToString(),
                    pcm,
                    profile.DurationSeconds
                );
            })
            .ToArray();

        Assert.Equal(
            sounds.Length,
            profiles
                .Select(entry => (
                    entry.Profile.DurationSeconds,
                    entry.Profile.StartFrequency,
                    entry.Profile.EndFrequency,
                    entry.Profile.Noise,
                    entry.Profile.PulseRate
                ))
                .Distinct()
                .Count()
        );

        foreach (var pair in reports
                     .SelectMany((left, index) => reports
                         .Skip(index + 1)
                         .Select(right => (Left: left, Right: right))))
        {
            Assert.True(
                PixelAudioQualityAnalyzer.DistinguishabilityDistance(
                    pair.Left,
                    pair.Right
                ) >= 0.2,
                $"{pair.Left.SourceId} and {pair.Right.SourceId} are too similar"
            );
        }
    }

    [Theory]
    [InlineData(-1, 32, 0)]
    [InlineData(1, 32, 0.5)]
    [InlineData(16, 32, 1)]
    [InlineData(31, 32, 0.5)]
    public void LoopFadeKeepsGeneratedAmbientLoopsSeamReady(
        double time,
        double duration,
        double expected
    )
    {
        Assert.Equal(
            expected,
            PixelAudioProfile.LoopFadeMultiplier(time, duration),
            3
        );
    }

    [Theory]
    [InlineData(-20, -80)]
    [InlineData(0, -80)]
    [InlineData(50, -6.02)]
    [InlineData(100, 0)]
    [InlineData(150, 0)]
    public void MasterVolumePercentConvertsToSafeDecibels(
        int percent,
        float expectedVolumeDb
    )
    {
        Assert.Equal(
            expectedVolumeDb,
            PixelAudioProfile.MasterVolumeDbForPercent(percent),
            2
        );
    }

    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(50, 100, -6.02)]
    [InlineData(100, 25, -12.04)]
    [InlineData(50, 50, -12.04)]
    [InlineData(0, 100, -80)]
    [InlineData(100, 0, -80)]
    public void IndependentAudioVolumesCombineMasterAndChannelSafely(
        int masterPercent,
        int channelPercent,
        float expectedVolumeDb
    )
    {
        Assert.Equal(
            expectedVolumeDb,
            PixelAudioProfile.MixedVolumeDb(masterPercent, channelPercent),
            2
        );
    }

    [Theory]
    [InlineData(-0.1, 0)]
    [InlineData(0, 0)]
    [InlineData(0.225, 0.5)]
    [InlineData(0.45, 1)]
    [InlineData(1.0, 1)]
    public void CrossfadeProgressUsesShortBoundedAmbientRamp(
        double elapsedSeconds,
        double expectedProgress
    )
    {
        Assert.InRange(PixelAudioProfile.AmbientCrossfadeSeconds, 0.1, 1);
        Assert.Equal(
            expectedProgress,
            PixelAudioProfile.CrossfadeProgress(
                elapsedSeconds,
                PixelAudioProfile.AmbientCrossfadeSeconds
            ),
            3
        );
    }

    [Fact]
    public void AcceptanceTourOrdersEveryAmbientContextForAudition()
    {
        var plan = PixelAudioAcceptanceTour.Plan();

        Assert.Equal(
            [
                PixelAmbientContext.HomesteadClear,
                PixelAmbientContext.VillageClear,
                PixelAmbientContext.WildsClear,
                PixelAmbientContext.RainveilRain,
                PixelAmbientContext.StardustWind,
                PixelAmbientContext.LongnightSnow,
                PixelAmbientContext.Festival,
                PixelAmbientContext.Combat
            ],
            plan.Segments.Select(segment => segment.Context).ToArray()
        );
        Assert.Equal(
            Enum.GetValues<PixelAmbientContext>().Length,
            plan.Segments.Count
        );
        Assert.All(plan.Segments, segment =>
        {
            Assert.Equal(
                PixelAudioAcceptanceTour.SegmentDurationSeconds,
                segment.DurationSeconds
            );
            Assert.True(segment.Cues.Count >= 2);
        });
    }

    [Fact]
    public void AcceptanceTourDurationAccountsForCrossfadeOverlap()
    {
        var plan = PixelAudioAcceptanceTour.Plan();
        var expectedDuration =
            PixelAudioAcceptanceTour.SegmentDurationSeconds *
            plan.Segments.Count -
            PixelAudioProfile.AmbientCrossfadeSeconds *
            plan.Transitions.Count;

        Assert.Equal(expectedDuration, plan.DurationSeconds, 6);
        Assert.Equal(60.85, plan.DurationSeconds, 3);
        Assert.Equal(
            PixelAudioProfile.SampleCount(expectedDuration) * 2,
            PixelAudioAcceptanceTour.RenderPcm16().Length
        );
    }

    [Fact]
    public void AcceptanceTourAmbientExcerptsRemainContextDistinct()
    {
        var fingerprints = Enum.GetValues<PixelAmbientContext>()
            .Select(context =>
            {
                var pcm = PixelAudioSynthesis.RenderAmbientExcerptPcm16(
                    context,
                    PixelAudioAcceptanceTour.SegmentDurationSeconds
                );
                var report = PixelAudioSynthesis.AnalyzePcm16(
                    pcm,
                    PixelAudioAcceptanceTour.SegmentDurationSeconds
                );
                return (
                    Context: context,
                    report.RmsAmplitude,
                    FirstSample: PixelAudioSynthesis.SampleAtPcm16(pcm, 12)
                );
            })
            .ToArray();

        Assert.Equal(
            fingerprints.Length,
            fingerprints
                .Select(entry => Math.Round(entry.RmsAmplitude, 4))
                .Distinct()
                .Count()
        );
        Assert.All(fingerprints, entry =>
            Assert.InRange(entry.RmsAmplitude, 0.01, 0.12)
        );
    }

    [Fact]
    public void AcceptanceTourHasHeadroomEnergyAndNoLongSilentWindow()
    {
        var plan = PixelAudioAcceptanceTour.Plan();
        var pcm = PixelAudioAcceptanceTour.RenderPcm16();
        var report = PixelAudioSynthesis.AnalyzePcm16(
            pcm,
            plan.DurationSeconds
        );

        Assert.InRange(report.PeakAmplitude, 0.05, 0.95);
        Assert.InRange(report.RmsAmplitude, 0.01, 0.35);
        foreach (var windowStart in WindowStarts(plan.DurationSeconds, 0.5))
        {
            Assert.True(
                WindowRms(pcm, windowStart, 0.25) > 0.001,
                $"Silent acceptance-tour window at {windowStart:0.00}s"
            );
        }
    }

    [Fact]
    public void AcceptanceTourTransitionsUseCrossfadeBoundaries()
    {
        var plan = PixelAudioAcceptanceTour.Plan();
        var pcm = PixelAudioAcceptanceTour.RenderPcm16();

        Assert.Equal(plan.Segments.Count - 1, plan.Transitions.Count);
        Assert.All(plan.Transitions, transition =>
        {
            Assert.Equal(
                PixelAudioProfile.AmbientCrossfadeSeconds,
                transition.DurationSeconds
            );
            Assert.InRange(
                WindowPeak(
                    pcm,
                    transition.StartSeconds,
                    transition.DurationSeconds
                ),
                0.001,
                0.75
            );
            Assert.InRange(
                MaxAdjacentDelta(
                    pcm,
                    transition.StartSeconds,
                    transition.DurationSeconds
                ),
                0,
                0.05
            );
        });
    }

    [Fact]
    public void CrossfadeVolumeMovesBetweenSilentAndTarget()
    {
        Assert.Equal(
            -50,
            PixelAudioProfile.CrossfadeVolumeDb(
                PixelAudioProfile.AmbientCrossfadeSeconds / 2,
                PixelAudioProfile.AmbientCrossfadeSeconds,
                PixelAudioProfile.SilentVolumeDb,
                -20
            ),
            2
        );
    }

    [Fact]
    public void AccessibilitySettingsPersistIndependentAudioVolumes()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-audio-settings-{Guid.NewGuid():N}"
        );
        try
        {
            var path = Path.Combine(directory, "settings.json");
            var service = new AccessibilitySettingsService(path);
            service.Save(new AccessibilitySettings
            {
                MasterVolumePercent = 42,
                AmbientVolumePercent = -10,
                EffectsVolumePercent = 133
            });

            var loaded = service.Load();

            Assert.Equal(42, loaded.MasterVolumePercent);
            Assert.Equal(0, loaded.AmbientVolumePercent);
            Assert.Equal(100, loaded.EffectsVolumePercent);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Theory]
    [InlineData(PlayerLocationIds.World, 15, DataCatalog.RainWeatherId, false,
        false, PixelAmbientContext.RainveilRain)]
    [InlineData(PlayerLocationIds.World, 29, DataCatalog.StardustWindWeatherId,
        false, false, PixelAmbientContext.StardustWind)]
    [InlineData(PlayerLocationIds.World, 43, DataCatalog.LongnightSnowWeatherId,
        false, false, PixelAmbientContext.LongnightSnow)]
    [InlineData(PlayerLocationIds.FireflyTide, 15, DataCatalog.ClearWeatherId,
        false, false, PixelAmbientContext.Festival)]
    [InlineData(PlayerLocationIds.World, 29, DataCatalog.ClearWeatherId, true,
        false, PixelAmbientContext.Combat)]
    public void RuntimeContextResolverSelectsTheIntegrationAmbient(
        string locationId,
        int day,
        string weatherId,
        bool combatActive,
        bool festivalActive,
        PixelAmbientContext expected
    )
    {
        var context = new PixelAudioRuntimeContext(
            locationId,
            day,
            weatherId,
            combatActive,
            festivalActive
        );

        Assert.Equal(expected, PixelAudioContextResolver.Resolve(context));
    }

    [Theory]
    [InlineData(WorldBiome.Home, PixelAmbientContext.HomesteadClear)]
    [InlineData(WorldBiome.LumenVillage, PixelAmbientContext.VillageClear)]
    [InlineData(WorldBiome.WhisperingWoods, PixelAmbientContext.WildsClear)]
    [InlineData(WorldBiome.StarfallMeadow, PixelAmbientContext.WildsClear)]
    [InlineData(WorldBiome.CrystalVale, PixelAmbientContext.WildsClear)]
    [InlineData(WorldBiome.MoonwaterWetlands, PixelAmbientContext.WildsClear)]
    [InlineData(WorldBiome.StarfallRuins, PixelAmbientContext.WildsClear)]
    public void ClearWorldRegionsHaveDistinctAmbientFamilies(
        WorldBiome region,
        PixelAmbientContext expected
    )
    {
        var context = new PixelAudioRuntimeContext(
            PlayerLocationIds.World,
            1,
            DataCatalog.ClearWeatherId,
            Region: region
        );

        Assert.Equal(expected, PixelAudioContextResolver.Resolve(context));
    }

    [Theory]
    [InlineData(PlayerLocationIds.Cottage, PixelAmbientContext.HomesteadClear)]
    [InlineData(PlayerLocationIds.Greenhouse, PixelAmbientContext.HomesteadClear)]
    [InlineData(PlayerLocationIds.MoonlitArchive, PixelAmbientContext.VillageClear)]
    [InlineData(PlayerLocationIds.StarweaverTeaHouse, PixelAmbientContext.VillageClear)]
    [InlineData(PlayerLocationIds.CrystalGrottoSurvey, PixelAmbientContext.WildsClear)]
    public void ClearInteriorsKeepTheirAreaAmbientFamily(
        string locationId,
        PixelAmbientContext expected
    )
    {
        var context = new PixelAudioRuntimeContext(
            locationId,
            1,
            DataCatalog.ClearWeatherId
        );

        Assert.Equal(expected, PixelAudioContextResolver.Resolve(context));
    }

    [Theory]
    [InlineData(PixelAmbientContext.HomesteadClear)]
    [InlineData(PixelAmbientContext.VillageClear)]
    [InlineData(PixelAmbientContext.WildsClear)]
    [InlineData(PixelAmbientContext.RainveilRain)]
    [InlineData(PixelAmbientContext.StardustWind)]
    [InlineData(PixelAmbientContext.LongnightSnow)]
    [InlineData(PixelAmbientContext.Festival)]
    [InlineData(PixelAmbientContext.Combat)]
    public void AmbientWaveformsHaveHeadroomEnergyAndSoftLoopEdges(
        PixelAmbientContext context
    )
    {
        var profile = PixelAudioProfile.Ambient(context);
        var pcm = PixelAudioSynthesis.RenderAmbientPcm16(context);
        var report = PixelAudioSynthesis.AnalyzePcm16(
            pcm,
            profile.DurationSeconds
        );

        Assert.Equal(
            PixelAudioProfile.SampleCount(profile.DurationSeconds),
            report.SampleCount
        );
        Assert.InRange(report.PeakAmplitude, 0.01, 0.95);
        Assert.InRange(report.RmsAmplitude, 0.005, 0.35);
        Assert.InRange(report.LoopEdgeAmplitude, 0, 0.002);
    }

    [Theory]
    [InlineData(PixelAmbientContext.HomesteadClear)]
    [InlineData(PixelAmbientContext.VillageClear)]
    [InlineData(PixelAmbientContext.WildsClear)]
    [InlineData(PixelAmbientContext.RainveilRain)]
    [InlineData(PixelAmbientContext.StardustWind)]
    [InlineData(PixelAmbientContext.LongnightSnow)]
    [InlineData(PixelAmbientContext.Festival)]
    [InlineData(PixelAmbientContext.Combat)]
    public void AutomatedQualityReportKeepsAmbientLoopsInsidePreListeningGates(
        PixelAmbientContext context
    )
    {
        var profile = PixelAudioProfile.Ambient(context);
        var pcm = PixelAudioSynthesis.RenderAmbientPcm16(context);
        var report = PixelAudioQualityAnalyzer.AnalyzePcm16(
            context.ToString(),
            pcm,
            profile.DurationSeconds
        );

        Assert.Equal(
            PixelAudioProfile.SampleCount(profile.DurationSeconds),
            report.SampleCount
        );
        Assert.InRange(
            report.PeakAmplitude,
            0.01,
            PixelAudioQualityThresholds.MaxPeakAmplitude
        );
        Assert.Equal(
            PixelAudioQualityThresholds.MaxClippedSampleRatio,
            report.ClippedSampleRatio
        );
        Assert.InRange(
            report.RmsDb,
            PixelAudioQualityThresholds.MinAmbientRmsDb,
            PixelAudioQualityThresholds.MaxAmbientRmsDb
        );
        Assert.InRange(
            report.CrestFactor,
            PixelAudioQualityThresholds.MinCrestFactor,
            PixelAudioQualityThresholds.MaxCrestFactor
        );
        AssertAmbientBandDistribution(report);
        Assert.InRange(
            report.LoopWindowMaxDelta,
            0,
            PixelAudioQualityThresholds.MaxLoopWindowMaxDelta
        );
        Assert.InRange(
            report.LoopWindowRmsDelta,
            0,
            PixelAudioQualityThresholds.MaxLoopWindowRmsDelta
        );
    }

    [Fact]
    public void AutomatedQualityReportKeepsEffectsInsideLoudnessAndHeadroomGates()
    {
        foreach (var sound in Enum.GetValues<PixelSound>())
        {
            var profile = PixelAudioProfile.Effect(sound);
            var pcm = PixelAudioSynthesis.RenderEffectPcm16(sound);
            var report = PixelAudioQualityAnalyzer.AnalyzePcm16(
                sound.ToString(),
                pcm,
                profile.DurationSeconds
            );

            Assert.InRange(
                report.PeakAmplitude,
                0.05,
                PixelAudioQualityThresholds.MaxPeakAmplitude
            );
            Assert.Equal(
                PixelAudioQualityThresholds.MaxClippedSampleRatio,
                report.ClippedSampleRatio
            );
            Assert.InRange(
                report.RmsDb,
                PixelAudioQualityThresholds.MinEffectRmsDb,
                PixelAudioQualityThresholds.MaxEffectRmsDb
            );
            Assert.InRange(
                report.CrestFactor,
                PixelAudioQualityThresholds.MinCrestFactor,
                PixelAudioQualityThresholds.MaxCrestFactor
            );
            AssertBandRatiosSumToOne(report);
        }
    }

    [Fact]
    public void AcceptanceTourQualityHasHeadroomCrestAndFrequencySpread()
    {
        var plan = PixelAudioAcceptanceTour.Plan();
        var pcm = PixelAudioAcceptanceTour.RenderPcm16();
        var report = PixelAudioQualityAnalyzer.AnalyzePcm16(
            "acceptance-tour",
            pcm,
            plan.DurationSeconds
        );

        Assert.InRange(
            report.PeakAmplitude,
            0.05,
            PixelAudioQualityThresholds.MaxPeakAmplitude
        );
        Assert.Equal(
            PixelAudioQualityThresholds.MaxClippedSampleRatio,
            report.ClippedSampleRatio
        );
        Assert.InRange(
            report.RmsDb,
            PixelAudioQualityThresholds.MinTourRmsDb,
            PixelAudioQualityThresholds.MaxTourRmsDb
        );
        Assert.InRange(
            report.CrestFactor,
            PixelAudioQualityThresholds.MinCrestFactor,
            PixelAudioQualityThresholds.MaxCrestFactor
        );
        AssertBandRatiosSumToOne(report);
        Assert.InRange(report.BandEnergy.LowRatio, 0.3, 0.7);
        Assert.InRange(report.BandEnergy.MidRatio, 0.25, 0.6);
        Assert.InRange(report.BandEnergy.HighRatio, 0.05, 0.25);
    }

    [Fact]
    public void AmbientContextFingerprintsStaySeparated()
    {
        var reports = Enum.GetValues<PixelAmbientContext>()
            .Select(context =>
            {
                var profile = PixelAudioProfile.Ambient(context);
                var pcm = PixelAudioSynthesis.RenderAmbientPcm16(context);
                return PixelAudioQualityAnalyzer.AnalyzePcm16(
                    context.ToString(),
                    pcm,
                    profile.DurationSeconds
                );
            })
            .ToArray();

        foreach (var pair in reports
                     .SelectMany((left, index) => reports
                         .Skip(index + 1)
                         .Select(right => (Left: left, Right: right))))
        {
            Assert.True(
                PixelAudioQualityAnalyzer.DistinguishabilityDistance(
                    pair.Left,
                    pair.Right
                ) >= PixelAudioQualityThresholds.MinAmbientSeparationDistance,
                $"{pair.Left.SourceId} and {pair.Right.SourceId} are too similar"
            );
        }
    }

    [Fact]
    public void AcceptanceTourCrossfadesStayContinuousAcrossBoundaries()
    {
        var plan = PixelAudioAcceptanceTour.Plan();
        var pcm = PixelAudioAcceptanceTour.RenderPcm16();

        Assert.All(plan.Transitions, transition =>
        {
            var report = PixelAudioQualityAnalyzer.AnalyzeCrossfadeWindow(
                $"{transition.From}->{transition.To}",
                pcm,
                transition.StartSeconds,
                transition.DurationSeconds
            );

            Assert.Equal(
                PixelAudioProfile.AmbientCrossfadeSeconds,
                report.DurationSeconds
            );
            Assert.InRange(report.PeakAmplitude, 0.001, 0.75);
            Assert.InRange(
                report.MaxAdjacentDelta,
                0,
                PixelAudioQualityThresholds.MaxCrossfadeAdjacentDelta
            );
            Assert.InRange(
                report.RmsAdjacentDelta,
                0,
                PixelAudioQualityThresholds.MaxCrossfadeRmsAdjacentDelta
            );
        });
    }

    [Fact]
    public void WaveExportUsesPcmRiffHeaders()
    {
        var pcm = PixelAudioSynthesis.RenderEffectPcm16(PixelSound.Reward);
        var wav = PixelAudioSynthesis.ToWavBytes(pcm);

        Assert.Equal((byte)'R', wav[0]);
        Assert.Equal((byte)'I', wav[1]);
        Assert.Equal((byte)'F', wav[2]);
        Assert.Equal((byte)'F', wav[3]);
        Assert.Equal((byte)'W', wav[8]);
        Assert.Equal((byte)'A', wav[9]);
        Assert.Equal((byte)'V', wav[10]);
        Assert.Equal((byte)'E', wav[11]);
        Assert.True(wav.Length > pcm.Length);
    }

    [Fact]
    public void ExportsPreviewWavsWhenDirectoryIsProvided()
    {
        var directory = Environment.GetEnvironmentVariable(
            "LUMINFIELD_AUDIO_PREVIEW_DIR"
        );
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        var paths = PixelAudioPreviewExporter.ExportAll(directory);

        Assert.Contains(paths, path =>
            path.EndsWith("ambient-homestead-clear.wav", StringComparison.Ordinal)
        );
        Assert.Contains(paths, path =>
            path.EndsWith("effect-reward.wav", StringComparison.Ordinal)
        );
        Assert.Contains(paths, path =>
            path.EndsWith(
                "audio-01-technical-report.md",
                StringComparison.Ordinal
            )
        );
        Assert.Contains(paths, path =>
            path.EndsWith(
                PixelAudioAcceptanceTour.FileName,
                StringComparison.Ordinal
            )
        );
        Assert.All(paths, path => Assert.True(File.Exists(path), path));
    }

    [Fact]
    public void PreviewReportExportsAutomatedQualitySections()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-audio-quality-{Guid.NewGuid():N}"
        );
        try
        {
            var paths = PixelAudioPreviewExporter.ExportAll(directory);
            var reportPath = paths.Single(path =>
                path.EndsWith(
                    "audio-01-technical-report.md",
                    StringComparison.Ordinal
                )
            );
            var report = File.ReadAllText(reportPath);

            Assert.Contains("Automated waveform proxy", report);
            Assert.Contains("## Quality gates", report);
            Assert.Contains("## Source quality", report);
            Assert.Contains("## Crossfade continuity", report);
            Assert.Contains("## Ambient distinguishability", report);
            Assert.Contains("Crest", report);
            Assert.Contains("Low", report);
            Assert.Contains("Max adjacent delta", report);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static void AssertAmbientBandDistribution(
        PixelAudioQualityReport report
    )
    {
        AssertBandRatiosSumToOne(report);
        Assert.InRange(
            report.BandEnergy.LowRatio,
            PixelAudioQualityThresholds.MinLowEnergyRatio,
            PixelAudioQualityThresholds.MaxLowEnergyRatio
        );
        Assert.InRange(
            report.BandEnergy.MidRatio,
            PixelAudioQualityThresholds.MinMidEnergyRatio,
            0.5
        );
        Assert.InRange(
            report.BandEnergy.HighRatio,
            PixelAudioQualityThresholds.MinHighEnergyRatio,
            PixelAudioQualityThresholds.MaxHighEnergyRatio
        );
    }

    private static void AssertBandRatiosSumToOne(PixelAudioQualityReport report)
    {
        Assert.Equal(
            1,
            report.BandEnergy.LowRatio +
            report.BandEnergy.MidRatio +
            report.BandEnergy.HighRatio,
            6
        );
    }

    private static IEnumerable<double> WindowStarts(
        double durationSeconds,
        double strideSeconds
    )
    {
        for (var start = 0.0; start < durationSeconds - 0.25; start +=
            strideSeconds)
        {
            yield return start;
        }
    }

    private static double WindowRms(
        byte[] pcm,
        double startSeconds,
        double durationSeconds
    )
    {
        var startSample = PixelAudioProfile.SampleOffset(startSeconds);
        var sampleCount = PixelAudioProfile.SampleCount(durationSeconds);
        var sumSquares = 0.0;
        for (var index = 0; index < sampleCount; index++)
        {
            var value = PixelAudioSynthesis.SampleAtPcm16(
                pcm,
                startSample + index
            );
            sumSquares += value * value;
        }

        return Math.Sqrt(sumSquares / sampleCount);
    }

    private static double WindowPeak(
        byte[] pcm,
        double startSeconds,
        double durationSeconds
    )
    {
        var startSample = PixelAudioProfile.SampleOffset(startSeconds);
        var sampleCount = PixelAudioProfile.SampleCount(durationSeconds);
        var peak = 0.0;
        for (var index = 0; index < sampleCount; index++)
        {
            peak = Math.Max(
                peak,
                Math.Abs(PixelAudioSynthesis.SampleAtPcm16(
                    pcm,
                    startSample + index
                ))
            );
        }

        return peak;
    }

    private static double MaxAdjacentDelta(
        byte[] pcm,
        double startSeconds,
        double durationSeconds
    )
    {
        var startSample = PixelAudioProfile.SampleOffset(startSeconds);
        var sampleCount = PixelAudioProfile.SampleCount(durationSeconds);
        var maxDelta = 0.0;
        var previous = PixelAudioSynthesis.SampleAtPcm16(pcm, startSample);
        for (var index = 1; index < sampleCount; index++)
        {
            var current = PixelAudioSynthesis.SampleAtPcm16(
                pcm,
                startSample + index
            );
            maxDelta = Math.Max(maxDelta, Math.Abs(current - previous));
            previous = current;
        }

        return maxDelta;
    }
}
