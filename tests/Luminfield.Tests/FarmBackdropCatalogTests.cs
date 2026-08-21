using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class FarmBackdropCatalogTests
{
    [Theory]
    [InlineData(14, FarmBackdropCatalog.DefaultTexturePath)]
    [InlineData(15, FarmBackdropCatalog.RainveilTexturePath)]
    [InlineData(28, FarmBackdropCatalog.RainveilTexturePath)]
    [InlineData(29, FarmBackdropCatalog.StarharvestTexturePath)]
    [InlineData(42, FarmBackdropCatalog.StarharvestTexturePath)]
    [InlineData(43, FarmBackdropCatalog.LongnightTexturePath)]
    [InlineData(56, FarmBackdropCatalog.LongnightTexturePath)]
    [InlineData(57, FarmBackdropCatalog.DefaultTexturePath)]
    [InlineData(85, FarmBackdropCatalog.StarharvestTexturePath)]
    [InlineData(99, FarmBackdropCatalog.LongnightTexturePath)]
    public void SelectsSeasonalBackdropFromAbsoluteDay(
        int day,
        string expectedTexturePath
    )
    {
        Assert.Equal(
            expectedTexturePath,
            FarmBackdropCatalog.TexturePathForDay(day)
        );
    }
}
