using Luminfield.Core;

namespace Luminfield.Tests;

internal static class NpcTestPositioning
{
    public static VillageNpcState PlacePlayerAdjacent(
        GameSession session,
        VillageNpcState npc
    )
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var current = session.Village.CurrentNpcs(
                    session.Clock.Day,
                    session.Clock.MinuteOfDay,
                    npc.LocationId,
                    session.PlayerCell
                )
                .Single(state => state.Definition.Id == npc.Definition.Id);
            var occupied = session.Village.CurrentNpcs(
                    session.Clock.Day,
                    session.Clock.MinuteOfDay,
                    npc.LocationId,
                    session.PlayerCell
                )
                .Where(state => state.Definition.Id != npc.Definition.Id)
                .Select(state => state.Position)
                .ToHashSet();
            var approach = new[]
                {
                    new GridPosition(
                        current.Position.X,
                        current.Position.Y + 1
                    ),
                    new GridPosition(
                        current.Position.X - 1,
                        current.Position.Y
                    ),
                    new GridPosition(
                        current.Position.X + 1,
                        current.Position.Y
                    ),
                    new GridPosition(
                        current.Position.X,
                        current.Position.Y - 1
                    )
                }
                .First(candidate =>
                    NpcNavigationMap.IsWalkableGeometry(
                        npc.LocationId,
                        candidate
                    ) &&
                    !NpcNavigationMap.IsCriticalEntranceCell(
                        npc.LocationId,
                        candidate
                    ) &&
                    !occupied.Contains(candidate)
                );

            session.SetPlayerLocation(
                approach.X * 16 + 8,
                approach.Y * 16 + 8,
                npc.LocationId
            );
            var projected = session.Village.CurrentNpcs(
                    session.Clock.Day,
                    session.Clock.MinuteOfDay,
                    npc.LocationId,
                    session.PlayerCell
                )
                .Single(state => state.Definition.Id == npc.Definition.Id);
            if (Distance(session.PlayerCell, projected.Position) == 1)
            {
                return projected;
            }
        }

        throw new InvalidOperationException(
            $"Could not place player adjacent to {npc.Definition.Id}."
        );
    }

    private static int Distance(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
}
