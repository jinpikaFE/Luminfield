namespace Luminfield.Core;

public sealed record StarGateDestinationDefinition(
    string Id,
    string NameKey,
    GridPosition ArrivalCell
);

public static class StarGateCatalog
{
    public const string HomesteadId = "homestead";
    public const string WhisperingWoodsId = "whispering_woods";
    public const string StarfallMeadowId = "starfall_meadow";
    public const string MoonwaterWetlandsId = "moonwater_wetlands";
    public const string CrystalValeId = "crystal_vale";
    public const string StarfallRuinsId = "starfall_ruins";

    public static IReadOnlyList<StarGateDestinationDefinition> Destinations
        { get; } = Array.AsReadOnly(
        new StarGateDestinationDefinition[]
        {
            new(
                HomesteadId,
                "star_gate.destination.homestead",
                FarmLayout.StarGateArrivalCell
            ),
            new(
                WhisperingWoodsId,
                "star_gate.destination.whispering_woods",
                WorldDefinition.NearestWalkableCell(
                    Below(WorldDefinition.WoodlandStarlightCell)
                )
            ),
            new(
                StarfallMeadowId,
                "star_gate.destination.starfall_meadow",
                WorldDefinition.NearestWalkableCell(
                    Below(WorldDefinition.MeadowStarlightCell)
                )
            ),
            new(
                MoonwaterWetlandsId,
                "star_gate.destination.moonwater_wetlands",
                WorldDefinition.NearestWalkableCell(
                    Below(WorldDefinition.MoonwaterStarlightCell)
                )
            ),
            new(
                CrystalValeId,
                "star_gate.destination.crystal_vale",
                WorldDefinition.NearestWalkableCell(
                    Below(WorldDefinition.CrystalWellCell)
                )
            ),
            new(
                StarfallRuinsId,
                "star_gate.destination.starfall_ruins",
                WorldDefinition.NearestWalkableCell(
                    Below(WorldDefinition.StarfallRuinsStarlightCell)
                )
            )
        });

    private static readonly IReadOnlyDictionary<
        string,
        StarGateDestinationDefinition
    > DestinationsById = Destinations.ToDictionary(
        destination => destination.Id,
        StringComparer.Ordinal
    );

    public static bool TryDestination(
        string? destinationId,
        out StarGateDestinationDefinition destination
    ) => DestinationsById.TryGetValue(
        destinationId ?? string.Empty,
        out destination!
    );

    private static GridPosition Below(GridPosition cell) =>
        new(cell.X, cell.Y + 1);
}

public sealed class StarGateSystem
{
    public bool Activated { get; private set; }
    public string LastDestinationId { get; private set; } = string.Empty;
    public int TravelCount { get; private set; }

    public event Action? Changed;

    public void Reset()
    {
        Activated = false;
        LastDestinationId = string.Empty;
        TravelCount = 0;
        Changed?.Invoke();
    }

    public void Restore(StarGateSave? save, bool constructionCompleted)
    {
        var normalized = NormalizeSave(save, constructionCompleted);
        Activated = normalized.Activated;
        LastDestinationId = normalized.LastDestinationId;
        TravelCount = normalized.TravelCount;
        Changed?.Invoke();
    }

    public ActionResult CheckActivation(bool constructionCompleted)
    {
        if (!constructionCompleted)
        {
            return ActionResult.Fail("star_gate.construction_required");
        }

        if (Activated)
        {
            return ActionResult.Fail("star_gate.already_activated");
        }

        return ActionResult.Success(messageKey: "star_gate.ready_to_activate");
    }

    public ActionResult Activate(bool constructionCompleted)
    {
        var check = CheckActivation(constructionCompleted);
        if (!check.Succeeded)
        {
            return check;
        }

        Activated = true;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "star_gate.activated");
    }

    public ActionResult CheckTravel(string destinationId)
    {
        if (!Activated)
        {
            return ActionResult.Fail("star_gate.not_activated");
        }

        if (!StarGateCatalog.TryDestination(destinationId, out _))
        {
            return ActionResult.Fail("star_gate.unknown_destination");
        }

        return ActionResult.Success(messageKey: "star_gate.ready_to_travel");
    }

    public ActionResult Travel(string destinationId)
    {
        var check = CheckTravel(destinationId);
        if (!check.Succeeded)
        {
            return check;
        }

        LastDestinationId = destinationId;
        TravelCount++;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "star_gate.travelled");
    }

    public StarGateSave Capture() => new()
    {
        Activated = Activated,
        LastDestinationId = LastDestinationId,
        TravelCount = TravelCount
    };

    public static StarGateSave NormalizeSave(
        StarGateSave? save,
        bool constructionCompleted
    )
    {
        if (!constructionCompleted)
        {
            return new StarGateSave();
        }

        var activated = save?.Activated == true;
        var destinationId = save?.LastDestinationId ?? string.Empty;
        if (!StarGateCatalog.TryDestination(destinationId, out _))
        {
            destinationId = string.Empty;
        }

        return new StarGateSave
        {
            Activated = activated,
            LastDestinationId = activated ? destinationId : string.Empty,
            TravelCount = activated ? Math.Max(0, save?.TravelCount ?? 0) : 0
        };
    }
}
