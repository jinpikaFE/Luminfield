using Luminfield.Core;

namespace Luminfield.Game;

public static class GameLocaleBootstrap
{
    public const string EnglishResourcePath = "res://localization/en.json";
    public const string SimplifiedChineseResourcePath =
        "res://localization/zh_CN.json";

    public static void LoadDefault(
        LocaleService locale,
        Func<string, string> readResource
    )
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(readResource);

        locale.LoadJson(
            LocaleService.English,
            readResource(EnglishResourcePath)
        );
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            readResource(SimplifiedChineseResourcePath)
        );
        locale.SetLocale(LocaleService.SimplifiedChinese);
    }
}
