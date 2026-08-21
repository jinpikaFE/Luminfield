namespace Luminfield.Core;

public sealed record FestivalStationDefinition(
    string Id,
    GridPosition Cell,
    TargetPreviewKind PreviewKind
);

public sealed record FestivalSpatialDefinition(
    string FestivalId,
    string LocationId,
    GridPosition WorldEntryCell,
    GridPosition WorldReturnCell,
    GridPosition ExitCell,
    GridPosition SafeArrivalCell,
    IReadOnlyList<FestivalStationDefinition> Stations,
    Func<GridPosition, bool> IsWalkable
);

public static class FestivalSpatialCatalog
{
    public static readonly FestivalSpatialDefinition StarharvestMarket = new(
        FestivalCatalog.StarharvestMarketFestivalId,
        PlayerLocationIds.StarharvestMarket,
        StarharvestMarketLayout.WorldEntryCell,
        StarharvestMarketLayout.WorldReturnCell,
        StarharvestMarketLayout.ExitCell,
        StarharvestMarketLayout.SafeArrivalCell,
        [
            new(
                FestivalCatalog.StarharvestShowcaseActivityId,
                StarharvestMarketLayout.ExhibitCell,
                TargetPreviewKind.FestivalExhibit
            ),
            new(
                "festival_starharvest_bid_board",
                StarharvestMarketLayout.BidBoardCell,
                TargetPreviewKind.FestivalBidBoard
            ),
            new(
                FestivalCatalog.StarharvestShopId,
                StarharvestMarketLayout.ShopCell,
                TargetPreviewKind.FestivalShop
            )
        ],
        StarharvestMarketLayout.IsWalkable
    );

    public static readonly FestivalSpatialDefinition GleamrisePlanting = new(
        FestivalCatalog.GleamrisePlantingFestivalId,
        PlayerLocationIds.GleamrisePlantingFestival,
        GleamrisePlantingFestivalLayout.WorldEntryCell,
        GleamrisePlantingFestivalLayout.WorldReturnCell,
        GleamrisePlantingFestivalLayout.ExitCell,
        GleamrisePlantingFestivalLayout.SafeArrivalCell,
        [
            new(
                FestivalCatalog.GleamriseSharedBloomfieldActivityId,
                GleamrisePlantingFestivalLayout.ActivityTableCell,
                TargetPreviewKind.FestivalSeedRack
            ),
            new(
                FestivalCatalog.GleamriseSeedExchangeId,
                GleamrisePlantingFestivalLayout.SeedExchangeCell,
                TargetPreviewKind.FestivalSeedExchange
            )
        ],
        GleamrisePlantingFestivalLayout.IsWalkable
    );

    public static readonly FestivalSpatialDefinition LongnightLanternFeast =
        new(
            FestivalCatalog.LongnightLanternFeastFestivalId,
            PlayerLocationIds.LongnightLanternFeast,
            LongnightLanternFeastLayout.WorldEntryCell,
            LongnightLanternFeastLayout.WorldReturnCell,
            LongnightLanternFeastLayout.ExitCell,
            LongnightLanternFeastLayout.SafeArrivalCell,
            [
                new(
                    FestivalCatalog.LongnightSharedTableId,
                    LongnightLanternFeastLayout.SharedTableCell,
                    TargetPreviewKind.FestivalFeastTable
                ),
                new(
                    FestivalCatalog.LongnightGiftExchangeId,
                    LongnightLanternFeastLayout.GiftExchangeCell,
                    TargetPreviewKind.FestivalGiftExchange
                ),
                new(
                    FestivalCatalog.LongnightLanternStallId,
                    LongnightLanternFeastLayout.StallCell,
                    TargetPreviewKind.FestivalShop
                ),
                new(
                    FestivalCatalog.LongnightStarlightRiteId,
                    LongnightLanternFeastLayout.RitualCell,
                    TargetPreviewKind.FestivalRitual
                )
            ],
            LongnightLanternFeastLayout.IsWalkable
        );

    public static readonly FestivalSpatialDefinition FireflyTide = new(
        FestivalCatalog.FireflyTideFestivalId,
        PlayerLocationIds.FireflyTide,
        FireflyTideLayout.WorldEntryCell,
        FireflyTideLayout.WorldReturnCell,
        FireflyTideLayout.ExitCell,
        FireflyTideLayout.SafeArrivalCell,
        [
            new(
                FestivalCatalog.FireflyLanternLaunchId,
                FireflyTideLayout.LanternLaunchCell,
                TargetPreviewKind.FestivalLanternLaunch
            ),
            new(
                FestivalCatalog.FireflyFishBasinId,
                FireflyTideLayout.FishBasinCell,
                TargetPreviewKind.FestivalFishBasin
            ),
            new(
                FestivalCatalog.FireflyGlowshopId,
                FireflyTideLayout.ShopCell,
                TargetPreviewKind.FestivalShop
            ),
            new(
                FestivalCatalog.FireflyTideAltarId,
                FireflyTideLayout.TideAltarCell,
                TargetPreviewKind.FestivalTideAltar
            )
        ],
        FireflyTideLayout.IsWalkable
    );

    public static readonly IReadOnlyList<FestivalSpatialDefinition> All =
    [
        StarharvestMarket,
        GleamrisePlanting,
        LongnightLanternFeast,
        FireflyTide
    ];

    public static bool TryByFestivalId(
        string festivalId,
        out FestivalSpatialDefinition definition
    )
    {
        definition = All.FirstOrDefault(entry =>
            entry.FestivalId == festivalId) !;
        return definition is not null;
    }

    public static bool TryByLocationId(
        string locationId,
        out FestivalSpatialDefinition definition
    )
    {
        definition = All.FirstOrDefault(entry =>
            entry.LocationId == locationId) !;
        return definition is not null;
    }
}
