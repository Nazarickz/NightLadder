using BepInEx.Logging;
using System.Collections.Concurrent;

namespace NightLadder.Plugin.Util;

public static class PluginLog
{
    public static ManualLogSource? Source { get; private set; }

    // History buffer for replay (.debugelo)
    private static readonly ConcurrentQueue<string> _history = new();
    private const int MaxHistory = 1000;

    public static void Init(string name)
    {
        if (Source == null)
        {
            Source = Logger.CreateLogSource(name);
        }
    }

    public static void Info(string msg)
    {
        Source?.LogInfo(msg);
        AddHistory("INFO", msg);
    }

    public static void Warn(string msg)
    {
        Source?.LogWarning(msg);
        AddHistory("WARN", msg);
    }

    public static void Error(string msg)
    {
        Source?.LogError(msg);
        AddHistory("ERROR", msg);
    }

    private static void AddHistory(string level, string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {msg}";
        _history.Enqueue(line);
        // Trim
        while (_history.Count > MaxHistory && _history.TryDequeue(out _)) { }
    }

    public static IReadOnlyList<string> GetHistorySnapshot()
    {
        return _history.ToArray();
    }

    public static int ReplayHistoryToConsole()
    {
        var snapshot = GetHistorySnapshot();
        foreach (var line in snapshot)
        {
            System.Console.WriteLine(line);
        }
        return snapshot.Count;
    }
}
