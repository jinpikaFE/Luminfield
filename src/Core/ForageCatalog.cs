namespace Luminfield.Core;

public sealed record ForageDefinition(
    string ItemId,
    string SeasonId,
    WorldBiome Biome,
    int SellPrice
);

public sealed record ForageSlotDefinition(
    string Id,
    WorldBiome Biome,
    int Ordinal,
    bool RequiresStardustWind
);

public static class ForageCatalog
{
    public const string WoodsSlotOneId = "forage_slot_woods_1";
    public const string WoodsSlotTwoId = "forage_slot_woods_2";
    public const string MeadowSlotOneId = "forage_slot_meadow_1";
    public const string MeadowSlotTwoId = "forage_slot_meadow_2";

    public static readonly IReadOnlyList<ForageDefinition> Definitions =
    [
        new(
            DataCatalog.WhisperbloomId,
            CalendarSystem.GleamriseSeasonId,
            WorldBiome.WhisperingWoods,
            18
        ),
        new(
            DataCatalog.DewglassCloverId,
            CalendarSystem.GleamriseSeasonId,
            WorldBiome.StarfallMeadow,
            24
        ),
        new(
            DataCatalog.RainbellMossId,
            CalendarSystem.RainveilSeasonId,
            WorldBiome.WhisperingWoods,
            22
        ),
        new(
            DataCatalog.MistcoilFernId,
            CalendarSystem.RainveilSeasonId,
            WorldBiome.StarfallMeadow,
            30
        ),
        new(
            DataCatalog.GloamgoldBerryId,
            CalendarSystem.StarharvestSeasonId,
            WorldBiome.WhisperingWoods,
            28
        ),
        new(
            DataCatalog.SunwispPodId,
            CalendarSystem.StarharvestSeasonId,
            WorldBiome.StarfallMeadow,
            38
        ),
        new(
            DataCatalog.NightlampLichenId,
            CalendarSystem.LongnightSeasonId,
            WorldBiome.WhisperingWoods,
            34
        ),
        new(
            DataCatalog.FrostwickRootId,
            CalendarSystem.LongnightSeasonId,
            WorldBiome.StarfallMeadow,
            44
        )
    ];

    public static readonly IReadOnlyList<ForageSlotDefinition> Slots =
    [
        new(WoodsSlotOneId, WorldBiome.WhisperingWoods, 0, false),
        new(MeadowSlotOneId, WorldBiome.StarfallMeadow, 1, false),
        new(WoodsSlotTwoId, WorldBiome.WhisperingWoods, 2, true),
        new(MeadowSlotTwoId, WorldBiome.StarfallMeadow, 3, true)
    ];

    public static readonly IReadOnlyDictionary<string, ForageDefinition> ByItemId =
        Definitions.ToDictionary(
            definition => definition.ItemId,
            StringComparer.Ordinal
        );

    public static ForageDefinition ForSeasonAndBiome(
        string seasonId,
        WorldBiome biome
    ) => Definitions.Single(definition =>
        definition.SeasonId == seasonId && definition.Biome == biome
    );

    public static IReadOnlyList<ForageSlotDefinition> ActiveSlots(
        string weatherId
    ) => Slots
        .Where(slot =>
            !slot.RequiresStardustWind ||
            weatherId == DataCatalog.StardustWindWeatherId
        )
        .ToArray();
}
