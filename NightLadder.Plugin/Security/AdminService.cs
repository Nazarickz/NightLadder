using System.Text.Json;

namespace NightLadder.Plugin.Security;

public static class AdminService
{
    private static readonly HashSet<string> _ids = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);
    private static string _filePath = string.Empty;

    public static void Initialize(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public static void Load()
    {
        try
        {
            _ids.Clear();
            _names.Clear();
            if (!File.Exists(_filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                File.WriteAllText(_filePath, "[]");
                return;
            }
            var json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new();
            foreach (var item in list)
            {
                var token = item?.Trim();
                if (string.IsNullOrEmpty(token)) continue;
                if (token.All(char.IsDigit)) _ids.Add(token);
                else _names.Add(token);
            }
        }
        catch { }
    }

    public static void Save()
    {
        try
        {
            var list = new List<string>();
            list.AddRange(_ids);
            list.AddRange(_names);
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }

    public static bool IsAdmin(ulong platformId, string? name)
    {
        if (_ids.Contains(platformId.ToString())) return true;
        if (!string.IsNullOrWhiteSpace(name) && _names.Contains(name)) return true;
        return false;
    }

    public static bool Add(string token)
    {
        token = token.Trim();
        bool added = false;
        if (token.All(char.IsDigit)) added = _ids.Add(token);
        else added = _names.Add(token);
        if (added) Save();
        return added;
    }

    public static bool Remove(string token)
    {
        token = token.Trim();
        bool removed = false;
        if (token.All(char.IsDigit)) removed = _ids.Remove(token);
        else removed = _names.Remove(token);
        if (removed) Save();
        return removed;
    }

    public static IEnumerable<string> List()
    {
        foreach (var id in _ids) yield return id;
        foreach (var nm in _names) yield return nm;
    }
}
