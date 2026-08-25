# AUDIO-01 procedural preview report

> Automated waveform proxy for pre-listening acceptance; this does not replace human audition.

## Quality gates

- Peak must stay below 0.95; clipped sample ratio must be 0.
- Ambient RMS must stay between -34 dB and -20 dB; effects between -18 dB and -8 dB; tour between -24 dB and -16 dB.
- Crest factor must stay in 1.4–12; ambient loop 50 ms window max/rms deltas must stay under 0.01/0.004.
- Ambient fingerprint distance must stay above 0.12; 0.45 s crossfade adjacent max/rms deltas must stay under 0.05/0.016.

## Source quality

| Source | Seconds | Samples | Peak | RMS | RMS dB | Crest | Clip % | Low | Mid | High | Loop edge | Loop max | Loop RMS |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ambient-homestead-clear | 32 | 705600 | 0.1811 | 0.0648 | -23.8 | 2.80 | 0 | 0.795 | 0.195 | 0.009 | 0.000000 | 0.003082 | 0.001445 |
| ambient-village-clear | 34 | 749700 | 0.1507 | 0.0447 | -27.0 | 3.37 | 0 | 0.532 | 0.427 | 0.042 | 0.000000 | 0.002503 | 0.000938 |
| ambient-wilds-clear | 38 | 837900 | 0.1772 | 0.0504 | -26.0 | 3.52 | 0 | 0.789 | 0.148 | 0.063 | 0.000000 | 0.003174 | 0.001000 |
| ambient-rainveil-rain | 36 | 793800 | 0.2303 | 0.0693 | -23.2 | 3.32 | 0 | 0.765 | 0.126 | 0.109 | 0.000000 | 0.004303 | 0.001545 |
| ambient-stardust-wind | 36 | 793800 | 0.1877 | 0.0432 | -27.3 | 4.34 | 0 | 0.735 | 0.214 | 0.051 | 0.000000 | 0.002197 | 0.000997 |
| ambient-longnight-snow | 40 | 882000 | 0.1679 | 0.0522 | -25.7 | 3.22 | 0 | 0.847 | 0.104 | 0.049 | 0.000000 | 0.002869 | 0.001272 |
| ambient-festival | 32 | 705600 | 0.1899 | 0.0505 | -25.9 | 3.76 | 0 | 0.667 | 0.301 | 0.032 | 0.000000 | 0.003265 | 0.001303 |
| ambient-combat | 24 | 529200 | 0.2002 | 0.0578 | -24.8 | 3.46 | 0 | 0.877 | 0.103 | 0.020 | 0.000000 | 0.003265 | 0.001227 |
| effect-till | 0.14 | 3087 | 0.5476 | 0.1938 | -14.3 | 2.83 | 0 | 0.828 | 0.070 | 0.102 | 0.000000 | 0.527085 | 0.217847 |
| effect-water | 0.26 | 5733 | 0.5435 | 0.2163 | -13.3 | 2.51 | 0 | 0.597 | 0.349 | 0.054 | 0.000275 | 0.307047 | 0.146855 |
| effect-plant | 0.18 | 3969 | 0.5398 | 0.2534 | -11.9 | 2.13 | 0 | 0.214 | 0.691 | 0.095 | 0.000397 | 0.444258 | 0.236105 |
| effect-harvest | 0.32 | 7056 | 0.5479 | 0.2641 | -11.6 | 2.07 | 0 | 0.079 | 0.659 | 0.262 | 0.000214 | 0.262307 | 0.145940 |
| effect-step | 0.065 | 1433 | 0.5386 | 0.1816 | -14.8 | 2.97 | 0 | 0.724 | 0.083 | 0.193 | 0.000214 | 0.674520 | 0.277727 |
| effect-chime | 0.62 | 13671 | 0.5491 | 0.2723 | -11.3 | 2.02 | 0 | 0.081 | 0.675 | 0.244 | 0.000122 | 0.137425 | 0.079236 |
| effect-sleep | 0.95 | 20947 | 0.5479 | 0.2698 | -11.4 | 2.03 | 0 | 0.748 | 0.242 | 0.010 | 0.000061 | 0.089572 | 0.051044 |
| effect-error | 0.22 | 4851 | 0.5268 | 0.1692 | -15.4 | 3.11 | 0 | 0.840 | 0.149 | 0.011 | 0.000092 | 0.360820 | 0.135870 |
| effect-resource-blocked | 0.34 | 7497 | 0.4932 | 0.1526 | -16.3 | 3.23 | 0 | 0.743 | 0.220 | 0.037 | 0.000183 | 0.230354 | 0.090172 |
| effect-tool-mismatch | 0.3 | 6615 | 0.5277 | 0.1802 | -14.9 | 2.93 | 0 | 0.540 | 0.425 | 0.035 | 0.000000 | 0.268380 | 0.127746 |
| effect-pickup | 0.24 | 5292 | 0.5461 | 0.2617 | -11.6 | 2.09 | 0 | 0.132 | 0.707 | 0.161 | 0.000244 | 0.333171 | 0.188833 |
| effect-damage | 0.18 | 3969 | 0.5461 | 0.2002 | -14.0 | 2.73 | 0 | 0.839 | 0.085 | 0.076 | 0.000092 | 0.434675 | 0.181915 |
| effect-dodge | 0.16 | 3528 | 0.5473 | 0.2287 | -12.8 | 2.39 | 0 | 0.402 | 0.539 | 0.059 | 0.000183 | 0.481796 | 0.236573 |
| effect-fish-bite | 0.28 | 6174 | 0.5370 | 0.1610 | -15.9 | 3.34 | 0 | 0.122 | 0.683 | 0.195 | 0.000000 | 0.270943 | 0.134248 |
| effect-reward | 0.82 | 18081 | 0.5490 | 0.2696 | -11.4 | 2.04 | 0 | 0.064 | 0.624 | 0.312 | 0.000000 | 0.103122 | 0.059384 |
| acceptance-tour | 60.85 | 1341742 | 0.7375 | 0.0891 | -21.0 | 8.28 | 0 | 0.377 | 0.468 | 0.155 | 0.009430 | 0.088290 | 0.037639 |

## Crossfade continuity

| Transition | Start | Seconds | Peak | RMS | Max adjacent delta | RMS adjacent delta |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| HomesteadClear->VillageClear | 7.55 | 0.45 | 0.0545 | 0.0197 | 0.01074 | 0.00265 |
| VillageClear->WildsClear | 15.1 | 0.45 | 0.0528 | 0.0160 | 0.02283 | 0.00612 |
| WildsClear->RainveilRain | 22.65 | 0.45 | 0.0863 | 0.0249 | 0.04785 | 0.01338 |
| RainveilRain->StardustWind | 30.2 | 0.45 | 0.0975 | 0.0253 | 0.04852 | 0.01321 |
| StardustWind->LongnightSnow | 37.75 | 0.45 | 0.0617 | 0.0221 | 0.02261 | 0.00857 |
| LongnightSnow->Festival | 45.3 | 0.45 | 0.0880 | 0.0258 | 0.02707 | 0.00828 |
| Festival->Combat | 52.85 | 0.45 | 0.1018 | 0.0363 | 0.02545 | 0.00787 |

## Ambient distinguishability

| Source | Nearest source | Distance |
| --- | --- | ---: |
| ambient-homestead-clear | ambient-festival | 0.382 |
| ambient-village-clear | ambient-festival | 0.191 |
| ambient-wilds-clear | ambient-rainveil-rain | 0.423 |
| ambient-rainveil-rain | ambient-wilds-clear | 0.423 |
| ambient-stardust-wind | ambient-longnight-snow | 0.165 |
| ambient-longnight-snow | ambient-stardust-wind | 0.165 |
| ambient-festival | ambient-village-clear | 0.191 |
| ambient-combat | ambient-festival | 0.293 |
