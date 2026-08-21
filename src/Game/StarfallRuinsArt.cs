using Godot;

namespace Luminfield.Game;

internal enum RuinsSpriteFacing
{
    Down,
    Up,
    Left,
    Right
}

internal static class StarfallRuinsArt
{
    private const float CellSize = 256;

    public static readonly Texture2D CombatAtlas = GD.Load<Texture2D>(
        "res://assets/generated/starfall_ruins_combat.png"
    );

    public static readonly Texture2D ArtifactAtlas = GD.Load<Texture2D>(
        "res://assets/generated/starfall_ruins_artifacts.png"
    );

    public static readonly Texture2D StarlightAtlas = GD.Load<Texture2D>(
        "res://assets/generated/starfall_ruins_starlight_pedestal.png"
    );

    private static readonly IReadOnlyDictionary<string, int> EnemyRows =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["enemy_shardling"] = 0,
            ["enemy_prism_wisp"] = 1,
            ["enemy_hollow_sentinel"] = 2
        };

    private static readonly IReadOnlyDictionary<string, int> ArtifactColumns =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["artifact_dawnpath_compass"] = 0,
            ["artifact_tideglass_tablet"] = 1,
            ["artifact_hushed_gleambell"] = 2,
            ["artifact_starweave_spindle"] = 3
        };

    public static Rect2 EnemyRegion(
        string enemyId,
        RuinsSpriteFacing facing,
        bool moving
    ) => CombatRegion(FacingColumn(facing) + (moving ? 1 : 0),
        EnemyRow(enemyId));

    public static Rect2 EnemyIconRegion(string enemyId) =>
        CombatRegion(0, EnemyRow(enemyId));

    public static Rect2 ShortbladeIconRegion => CombatRegion(0, 3);
    public static Rect2 WeaponRackRegion => CombatRegion(1, 3);
    public static Rect2 SlashRegion(bool alternate) =>
        CombatRegion(alternate ? 3 : 2, 3);
    public static Rect2 PrismProjectileRegion => CombatRegion(4, 3);
    public static Rect2 SentinelWarningRegion => CombatRegion(5, 3);
    public static Rect2 HealthCoreRegion => CombatRegion(6, 3);
    public static Rect2 DodgeSparkRegion => CombatRegion(7, 3);

    public static Rect2 ArtifactWorldRegion(string itemId) =>
        ArtifactRegion(ArtifactColumn(itemId), 0);

    public static Rect2 ArtifactIconRegion(string itemId) =>
        ArtifactRegion(ArtifactColumn(itemId), 1);

    public static Rect2 TrialGateRegion(bool open) =>
        ArtifactRegion(open ? 1 : 0, 2);

    public static Rect2 ArchiveDisplayRegion(bool complete) =>
        ArtifactRegion(complete ? 3 : 2, 2);

    public static Rect2 RuinsStarlightRegion(bool restored) => restored
        ? new Rect2(680, 134, 520, 456)
        : new Rect2(53, 134, 520, 456);

    public static AtlasTexture RuinsStarlightNodeTexture() => new()
    {
        Atlas = StarlightAtlas,
        Region = new Rect2(132, 705, 363, 470),
        FilterClip = true
    };

    public static AtlasTexture SixfoldConvergenceTexture() => new()
    {
        Atlas = StarlightAtlas,
        Region = new Rect2(786, 705, 308, 470),
        FilterClip = true
    };

    public static bool TryItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = ArtifactAtlas;
        if (ArtifactColumns.TryGetValue(itemId, out var column))
        {
            region = ArtifactRegion(column, 1);
            return true;
        }

        texture = CombatAtlas;
        if (itemId == "moonsteel_shortblade")
        {
            region = ShortbladeIconRegion;
            return true;
        }

        region = default;
        return false;
    }

    private static int FacingColumn(RuinsSpriteFacing facing) => facing switch
    {
        RuinsSpriteFacing.Down => 0,
        RuinsSpriteFacing.Up => 2,
        RuinsSpriteFacing.Left => 4,
        RuinsSpriteFacing.Right => 6,
        _ => 0
    };

    private static int EnemyRow(string enemyId) =>
        EnemyRows.TryGetValue(enemyId, out var row)
            ? row
            : throw new KeyNotFoundException(
                $"Missing Starfall Ruins enemy art for '{enemyId}'."
            );

    private static int ArtifactColumn(string itemId) =>
        ArtifactColumns.TryGetValue(itemId, out var column)
            ? column
            : throw new KeyNotFoundException(
                $"Missing Starfall Ruins artifact art for '{itemId}'."
            );

    private static Rect2 CombatRegion(int column, int row) => new(
        column * CellSize,
        row * CellSize,
        CellSize,
        CellSize
    );

    private static Rect2 ArtifactRegion(int column, int row) => new(
        column * CellSize,
        row * CellSize,
        CellSize,
        CellSize
    );
}
