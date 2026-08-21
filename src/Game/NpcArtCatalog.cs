using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed record NpcArtDefinition(
    string AtlasId,
    int Row,
    float TargetHeight
);

public readonly record struct NpcSpriteProjection(
    Texture2D Texture,
    Rect2 Region,
    float TargetHeight
);

public static class NpcArtCatalog
{
    public const string BaseAtlasId = "village_npcs_base";
    public const string ExpansionAtlasId = "village_npcs_expansion";
    public const string Wave2AtlasId = "village_npcs_wave_2";
    public const string Wave3AtlasId = "village_npcs_wave_3";

    private static readonly IReadOnlyDictionary<string, NpcArtDefinition>
        Definitions = new Dictionary<string, NpcArtDefinition>(
            StringComparer.Ordinal
        )
        {
            [VillageCatalog.LioraId] = new(BaseAtlasId, 0, 54),
            [VillageCatalog.TaviId] = new(BaseAtlasId, 1, 52),
            [VillageCatalog.NemiId] = new(BaseAtlasId, 2, 52),
            [VillageCatalog.SelaId] = new(ExpansionAtlasId, 0, 52),
            [VillageCatalog.ElowenId] = new(ExpansionAtlasId, 1, 52),
            [VillageCatalog.VessaId] = new(ExpansionAtlasId, 2, 52),
            [VillageCatalog.OrinId] = new(ExpansionAtlasId, 3, 52),
            [VillageCatalog.KaelId] = new(ExpansionAtlasId, 4, 52),
            [VillageCatalog.HaldenId] = new(Wave2AtlasId, 0, 52),
            [VillageCatalog.MaveaId] = new(Wave2AtlasId, 1, 52),
            [VillageCatalog.SivrenId] = new(Wave2AtlasId, 2, 52),
            [VillageCatalog.DorrikId] = new(Wave2AtlasId, 3, 52),
            [VillageCatalog.YvaraId] = new(Wave3AtlasId, 0, 52),
            [VillageCatalog.BrialId] = new(Wave3AtlasId, 1, 52),
            [VillageCatalog.PavriId] = new(Wave3AtlasId, 2, 52),
            [VillageCatalog.RovenId] = new(Wave3AtlasId, 3, 52)
        };

    public static IReadOnlyDictionary<string, NpcArtDefinition> All =>
        Definitions;

    public static NpcArtDefinition DefinitionFor(string npcId)
    {
        if (!Definitions.TryGetValue(npcId, out var definition))
        {
            throw new KeyNotFoundException(
                $"Village NPC art is not registered: {npcId}."
            );
        }

        return definition;
    }

    public static NpcSpriteProjection Resolve(
        string npcId,
        NpcFacing facing
    )
    {
        var definition = DefinitionFor(npcId);

        return new NpcSpriteProjection(
            GeneratedArt.VillageNpcTexture(definition.AtlasId),
            GeneratedArt.VillageNpcRegion(
                definition.AtlasId,
                definition.Row,
                facing
            ),
            definition.TargetHeight
        );
    }
}
