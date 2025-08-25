using System.Text.Json;
using NightLadder.Core.Models;

namespace NightLadder.Plugin.Config;

public static class RankConfigProvider
{
    public static RankConfig LoadOrCreate(string filePath)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            if (!File.Exists(filePath))
            {
                var cfg = new RankConfig();
                Save(filePath, cfg);
                return cfg;
            }

            var json = File.ReadAllText(filePath);
            var loaded = JsonSerializer.Deserialize<RankConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            return loaded ?? new RankConfig();
        }
        catch
        {
            return new RankConfig();
        }
    }

    public static void Save(string filePath, RankConfig config)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(config, opts);
        File.WriteAllText(filePath, json);
    }
}
