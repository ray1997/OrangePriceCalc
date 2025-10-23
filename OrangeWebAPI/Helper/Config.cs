using System.ComponentModel;

namespace OrangeWebAPI.Helper;

public static class Config
{
    private static readonly string ConfigPath =
        Path.Combine(AppContext.BaseDirectory, "config.conf");

    private static readonly Dictionary<string, string> Values = Load();

    private static Dictionary<string, string> Load()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(ConfigPath))
        {
            using var sw = File.CreateText(ConfigPath);
            sw.Write(defaultEmptyFile);
            sw.Close();
        }

        foreach (var line in File.ReadAllLines(ConfigPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
                dict[parts[0].Trim()] = parts[1].Trim();
        }

        return dict;
    }

    public static T Get<T>(string key, T defaultValue = default!)
    {
        if (!Values.TryGetValue(key, out var raw))
            return defaultValue;

        try
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null && converter.CanConvertFrom(typeof(string)))
                return (T)converter.ConvertFromString(raw)!;
        }
        catch { /* ignore parse errors */ }

        return defaultValue;
    }

    private const string defaultEmptyFile = $"""
                                            {nameof(OrangeController.LatestJsonPath)}=/storage/media/configs/n8n/database/latest.json
                                            {nameof(OrangeController.DatabasePath)}=/storage/media/configs/n8n/database/
                                            """;

}