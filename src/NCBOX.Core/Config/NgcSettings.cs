using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NCBOX.Core.Paths;

namespace NCBOX.Core.Config;

public sealed class NgcSettings
{
    public string SitesAvailable { get; set; } = ServerPaths.DefaultSitesAvailable;
    public string SitesEnabled { get; set; } = ServerPaths.DefaultSitesEnabled;

    private static string ConfigFilePath =>
        Path.Combine(ServerPaths.GetUserConfigDir("ngc"), "config.json");

    public static NgcSettings Load()
    {
        string path = ConfigFilePath;
        if (!File.Exists(path))
        {
            var defaults = new NgcSettings();
            defaults.Save();
            return defaults;
        }

        try
        {
            string json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize(json, NgcSettingsJsonContext.Default.NgcSettings);
            return settings ?? new NgcSettings();
        }
        catch
        {
            return new NgcSettings();
        }
    }

    public void Save()
    {
        string path = ConfigFilePath;
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(this, NgcSettingsJsonContext.Default.NgcSettings);
        File.WriteAllText(path, json);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NgcSettings))]
internal partial class NgcSettingsJsonContext : JsonSerializerContext
{
}
