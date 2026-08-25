using Luminfield.Core;

namespace Luminfield.Game;

public static class RouteGuidanceOriginPresenter
{
    public static WorldBiome Resolve(
        string playerLocationId,
        GridPosition playerCell
    )
    {
        if (playerLocationId == PlayerLocationIds.World)
        {
            return WorldDefinition.GetBiome(playerCell);
        }

        if (IsHomesteadInterior(playerLocationId))
        {
            return WorldBiome.Home;
        }

        if (IsVillageInterior(playerLocationId))
        {
            return WorldBiome.LumenVillage;
        }

        if (FestivalSpatialCatalog.TryByLocationId(
                playerLocationId,
                out var festival
            ))
        {
            return WorldDefinition.GetBiome(festival.WorldReturnCell);
        }

        return playerLocationId switch
        {
            PlayerLocationIds.CrystalGrottoSurvey =>
                WorldBiome.CrystalVale,
            PlayerLocationIds.StarfallRuinsTrial =>
                WorldBiome.StarfallRuins,
            _ => WorldDefinition.GetBiome(playerCell)
        };
    }

    private static bool IsHomesteadInterior(string locationId) =>
        locationId is PlayerLocationIds.Cottage or
            PlayerLocationIds.Greenhouse or
            PlayerLocationIds.StarfeatherCoop or
            PlayerLocationIds.MoonfleeceBarn;

    private static bool IsVillageInterior(string locationId) =>
        locationId is PlayerLocationIds.MoonlitArchive or
            PlayerLocationIds.MoonstoneWorkshop or
            PlayerLocationIds.StarweaverTeaHouse or
            PlayerLocationIds.TwilightEmporium or
            PlayerLocationIds.StarlightPost or
            PlayerLocationIds.StarfallWatch;
}
