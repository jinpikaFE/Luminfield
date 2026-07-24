using Godot;

namespace Luminfield.Game;

public static class TilePaletteFactory
{
    public const int TileSize = 16;
    public const int Grass = 0;
    public const int GrassAlt = 1;
    public const int DrySoil = 2;
    public const int WateredSoil = 3;
    public const int MoonstonePath = 4;
    public const int Water = 5;
    public const int WoodFloor = 6;
    public const int InteriorWall = 7;
    public const int FarmField = 8;
    public const int FlowerMeadow = 9;
    public const int Hedge = 10;
    public const int PondBank = 11;
    public const int Doorstep = 12;
    public const int MoonstonePathAlt = 13;
    public const int FarmFieldAlt = 14;

    public static TileSet CreateEnvironment()
    {
        var texture = GD.Load<Texture2D>("res://assets/pixel/tiles.svg");
        return CreateAtlas(texture, 15);
    }

    public static TileSet CreateCrops()
    {
        var texture = GD.Load<Texture2D>("res://assets/pixel/crops.svg");
        return CreateAtlas(texture, 8);
    }

    private static TileSet CreateAtlas(Texture2D texture, int tileCount)
    {
        var tileSet = new TileSet { TileSize = new Vector2I(TileSize, TileSize) };
        var source = new TileSetAtlasSource
        {
            Texture = texture,
            TextureRegionSize = new Vector2I(TileSize, TileSize)
        };

        for (var index = 0; index < tileCount; index++)
        {
            source.CreateTile(new Vector2I(index, 0));
        }

        tileSet.AddSource(source, 0);
        return tileSet;
    }
}
