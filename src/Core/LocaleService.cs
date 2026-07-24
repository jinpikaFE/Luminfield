using System.Text.Json;

namespace Luminfield.Core;

public sealed class LocaleService
{
    public const string English = "en";
    public const string SimplifiedChinese = "zh_CN";

    private readonly Dictionary<string, Dictionary<string, string>> _translations =
        new(StringComparer.Ordinal);

    public string CurrentLocale { get; private set; } = SimplifiedChinese;

    public event Action? LocaleChanged;

    public IReadOnlyCollection<string> LoadedLocales => _translations.Keys;

    public void LoadJson(string locale, string json)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new InvalidDataException($"Locale '{locale}' contains no translations.");
        _translations[locale] = new Dictionary<string, string>(values, StringComparer.Ordinal);
    }

    public void SetLocale(string locale)
    {
        if (!_translations.ContainsKey(locale))
        {
            throw new InvalidOperationException($"Locale '{locale}' is not loaded.");
        }

        if (CurrentLocale == locale)
        {
            return;
        }

        CurrentLocale = locale;
        LocaleChanged?.Invoke();
    }

    public string Toggle()
    {
        SetLocale(CurrentLocale == SimplifiedChinese ? English : SimplifiedChinese);
        return CurrentLocale;
    }

    public string Tr(string key, params object[] arguments)
    {
        var value = TryGet(CurrentLocale, key)
            ?? TryGet(English, key)
            ?? $"[{key}]";
        return arguments.Length == 0 ? value : string.Format(value, arguments);
    }

    public IReadOnlyCollection<string> Keys(string locale) =>
        _translations.TryGetValue(locale, out var values)
            ? values.Keys
            : Array.Empty<string>();

    private string? TryGet(string locale, string key) =>
        _translations.TryGetValue(locale, out var values) &&
        values.TryGetValue(key, out var value)
            ? value
            : null;
}
