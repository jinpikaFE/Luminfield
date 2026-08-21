using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class DeepMineArt
{
    private const float Cell = 256;
    public static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/activities/mining/deep_mine_combat.png"
    );

    public static bool IsExpansionEnemy(string enemyId) => enemyId is
        StarfallRuinsTrialCatalog.MoonshardMiteEnemyId or
        StarfallRuinsTrialCatalog.VeilwingBatEnemyId or
        StarfallRuinsTrialCatalog.StarironBurrowerEnemyId;

    public static Texture2D EnemyTexture(string enemyId) =>
        IsExpansionEnemy(enemyId) ? Atlas : StarfallRuinsArt.CombatAtlas;

    public static Rect2 EnemyRegion(
        string enemyId,
        RuinsSpriteFacing facing = RuinsSpriteFacing.Down,
        bool moving = false
    )
    {
        if (!IsExpansionEnemy(enemyId))
        {
            return StarfallRuinsArt.EnemyRegion(enemyId, facing, moving);
        }

        var row = enemyId switch
        {
            StarfallRuinsTrialCatalog.VeilwingBatEnemyId => 1,
            StarfallRuinsTrialCatalog.StarironBurrowerEnemyId => 2,
            _ => 0
        };
        var column = FacingColumn(facing) + (moving ? 1 : 0);
        return Region(column, row);
    }

    public static Texture2D EnemyIcon(string enemyId) => new AtlasTexture
    {
        Atlas = EnemyTexture(enemyId),
        Region = EnemyRegion(enemyId),
        FilterClip = true
    };

    public static bool TryWeaponIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = Atlas;
        region = itemId switch
        {
            DataCatalog.MoonsteelShortbladeId => Region(0, 3),
            DataCatalog.CrystalPikeId => Region(1, 3),
            DataCatalog.MoonarcBowId => Region(2, 3),
            _ => default
        };
        return region.Size != Vector2.Zero;
    }

    public static Texture2D WeaponIcon(string itemId)
    {
        if (!TryWeaponIcon(itemId, out var texture, out var region))
        {
            throw new KeyNotFoundException(
                $"Missing deep-mine weapon art for '{itemId}'."
            );
        }
        return new AtlasTexture
        {
            Atlas = texture,
            Region = region,
            FilterClip = true
        };
    }

    public static Texture2D MiningSkillIcon() => Icon(4);
    public static Texture2D NightwatchSkillIcon() => Icon(5);
    public static Texture2D AnchorIcon() => Icon(6);
    public static Texture2D DropIcon() => Icon(7);

    private static AtlasTexture Icon(int column) => new()
    {
        Atlas = Atlas,
        Region = Region(column, 3),
        FilterClip = true
    };

    private static int FacingColumn(RuinsSpriteFacing facing) => facing switch
    {
        RuinsSpriteFacing.Down => 0,
        RuinsSpriteFacing.Up => 2,
        RuinsSpriteFacing.Left => 4,
        RuinsSpriteFacing.Right => 6,
        _ => 0
    };

    private static Rect2 Region(int column, int row) => new(
        column * Cell,
        row * Cell,
        Cell,
        Cell
    );
}
