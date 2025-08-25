using NightLadder.Core.Services;
using Unity.Entities;

namespace NightLadder.Plugin;

// Simple global registry for shared services
public static class App
{
    public static RankManager? RankManager { get; set; }
    public static EntityManager? ServerEntityManager { get; set; }

    // Indicates whether Harmony death hooks were applied successfully
    public static bool HarmonyPatched { get; set; }
}
