using System.Globalization;

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
            sw.Write(DefaultEmptyFile);
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
            object? result = typeof(T) switch
            {
                var t when t == typeof(string) => raw,
                var t when t == typeof(int) => int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : defaultValue,
                var t when t == typeof(bool) => bool.TryParse(raw, out var b) ? b : defaultValue,
                var t when t == typeof(decimal) => decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : defaultValue,
                var t when t == typeof(double) => double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : defaultValue,
                var t when t == typeof(DateTime) => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : defaultValue,
                _ => defaultValue
            };

            return (T)result!;
        }
        catch
        {
            return defaultValue;
        }
    }

    private const string DefaultEmptyFile = $"""
                                            {nameof(OrangeController.LatestJsonPath)}=/storage/media/configs/n8n/database/latest.json
                                            {nameof(OrangeController.DatabasePath)}=/storage/media/configs/n8n/database/
                                            """;

}