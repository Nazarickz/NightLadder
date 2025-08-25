using System.Collections.Generic;
using Unity.Entities;

namespace NightLadder.Plugin.Hooks
{
    // Simple cache to map a downed victim to the player entity that downed them
    internal static class KillCache
    {
        private static readonly Dictionary<Entity, Entity> _downedBy = new();

        public static void SetDowner(Entity victim, Entity killer)
        {
            _downedBy[victim] = killer;
        }

        public static Entity? GetDowner(Entity victim)
        {
            return _downedBy.TryGetValue(victim, out var killer) ? killer : (Entity?)null;
        }

        public static void Clear(Entity victim)
        {
            _downedBy.Remove(victim);
        }

        public static void ClearAll()
        {
            _downedBy.Clear();
        }
    }
}
