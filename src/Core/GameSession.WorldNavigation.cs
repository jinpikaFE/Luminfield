namespace Luminfield.Core;

public sealed partial class GameSession
{
    public bool CanOccupyWorldCell(GridPosition cell) =>
        CanOccupyWorldCell(cell, PlayerCell);

    public bool CanOccupyWorldCell(
        GridPosition cell,
        GridPosition playerCell
    )
    {
        if (!WorldDefinition.IsInBounds(cell) ||
            FarmLayout.IsStaticBlocked(cell))
        {
            return false;
        }

        if (!WorldDefinition.IsHomeCell(cell))
        {
            if (WorldDefinition.IsBoundaryCell(cell))
            {
                return false;
            }

            if (Village.NpcAt(
                    cell,
                    Clock.Day,
                    Clock.MinuteOfDay,
                    PlayerLocationIds.World,
                    playerCell
                ) is not null ||
                Forage.SpawnAt(cell) is not null)
            {
                return false;
            }

            return Resources.IsRemoved(cell) ||
                !WorldDefinition.IsBlocked(cell);
        }

        // Older saves may legitimately contain a chest, tree, or placeable on
        // an animal-building approach. Keep that data, and keep water-built
        // boardwalk approaches traversable so they cannot seal the only door.
        if (FarmLayout.IsAnimalBuildingApproachCell(cell))
        {
            return true;
        }

        if (WorldDefinition.IsBlocked(cell))
        {
            return false;
        }

        return !Farm.IsReserved(cell) &&
            !Storage.HasChest(cell) &&
            !FarmObjects.BlocksMovement(cell) &&
            !Orchard.BlocksMovement(cell);
    }

    public bool CanOccupyNavigationCell(
        string locationId,
        GridPosition cell
    )
    {
        if (locationId == PlayerLocationIds.World)
        {
            return CanOccupyWorldCell(cell);
        }

        var geometryWalkable = locationId switch
        {
            PlayerLocationIds.Cottage => CottageLayout.IsWalkable(
                cell,
                Construction.IsCompleted
            ),
            PlayerLocationIds.Greenhouse =>
                GreenhouseLayout.IsWalkable(cell),
            PlayerLocationIds.StarfeatherCoop =>
                StarfeatherCoopLayout.IsWalkable(cell),
            PlayerLocationIds.MoonfleeceBarn =>
                MoonfleeceBarnLayout.IsWalkable(cell),
            PlayerLocationIds.CrystalGrottoSurvey =>
                CrystalGrottoSurveyLayout.IsWalkable(cell),
            PlayerLocationIds.StarfallRuinsTrial =>
                StarfallRuinsTrialLayout.IsWalkable(cell),
            _ => NpcNavigationMap.IsWalkableGeometry(locationId, cell)
        };
        if (!geometryWalkable)
        {
            return false;
        }

        return Village.NpcAt(
            cell,
            Clock.Day,
            Clock.MinuteOfDay,
            locationId,
            PlayerCell
        ) is null;
    }
}
