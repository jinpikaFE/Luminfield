using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class GameLocaleBootstrapTests
{
    [Fact]
    public void LoadDefaultLoadsStableResourcesAndSelectsSimplifiedChinese()
    {
        var locale = new LocaleService();
        var requestedPaths = new List<string>();
        var resources = new Dictionary<string, string>
        {
            [GameLocaleBootstrap.EnglishResourcePath] =
                "{\"sample\":\"English\"}",
            [GameLocaleBootstrap.SimplifiedChineseResourcePath] =
                "{\"sample\":\"简体中文\"}"
        };

        GameLocaleBootstrap.LoadDefault(
            locale,
            path =>
            {
                requestedPaths.Add(path);
                return resources[path];
            }
        );

        Assert.Equal(
            [
                GameLocaleBootstrap.EnglishResourcePath,
                GameLocaleBootstrap.SimplifiedChineseResourcePath
            ],
            requestedPaths
        );
        Assert.Equal(LocaleService.SimplifiedChinese, locale.CurrentLocale);
        Assert.Equal("简体中文", locale.Tr("sample"));
        Assert.Contains(LocaleService.English, locale.LoadedLocales);
        Assert.Contains(LocaleService.SimplifiedChinese, locale.LoadedLocales);
    }
}
