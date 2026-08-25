namespace Luminfield.Core;

public sealed record JourneyRecapStarlightSnapshot(
    string PedestalId,
    bool Restored,
    int? RestorationStoryDay
);

public sealed record JourneyRecapCompanionSnapshot(
    string NpcId,
    int RelationshipPoints
);

public sealed record JourneyRecapSnapshot(
    IReadOnlyList<JourneyRecapStarlightSnapshot> Starlights,
    int MetNpcCount,
    int NewAcquaintanceCount,
    int TrustedFriendCount,
    int KindredLightCount,
    IReadOnlyList<JourneyRecapCompanionSnapshot> TopCompanions,
    int ExploredChunkCount,
    int TotalChunkCount,
    int ExploredRegionCount,
    int TotalRegionCount,
    int CompletedCharacterEventCount,
    int TotalCharacterEventCount,
    int CompletedStarlightStoryBeatCount,
    int TotalStarlightStoryBeatCount,
    bool MainStoryCompleted
)
{
    public IReadOnlyList<string> RestoredPedestalIds => Starlights
        .Where(starlight => starlight.Restored)
        .Select(starlight => starlight.PedestalId)
        .ToArray();

    public int TotalPedestalCount => Starlights.Count;

    public string? HighestRelationshipNpcId =>
        TopCompanions.FirstOrDefault()?.NpcId;

    public int HighestRelationshipPoints =>
        TopCompanions.FirstOrDefault()?.RelationshipPoints ?? 0;
}
