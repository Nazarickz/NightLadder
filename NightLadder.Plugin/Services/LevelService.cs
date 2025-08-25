using System.Collections.Concurrent;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;

namespace NightLadder.Plugin.Services
{
    // Servi�o de n�vel: fornece n�vel Live (equipamento atual) e mant�m o M�ximo observado (em mem�ria)
    internal static class LevelService
    {
        // Armazena n�vel m�ximo observado por PlatformId (string para reuso com Rank IDs)
        private static readonly ConcurrentDictionary<string, int> _maxLevels = new();

        public static int GetPlayerLevel(EntityManager em, User user)
        {
            int live = GetLiveGearLevel(em, user);
            var mode = App.RankManager?.Config.LevelTrackingMode ?? "Live";
            string id = user.PlatformId.ToString();

            if (mode.Equals("Max", System.StringComparison.OrdinalIgnoreCase))
            {
                // Atualiza e retorna o m�ximo observado
                return _maxLevels.AddOrUpdate(id, live, (_, prev) => live > prev ? live : prev);
            }
            return live;
        }

        public static void UpdateFromUser(EntityManager em, User user)
        {
            string id = user.PlatformId.ToString();
            int live = GetLiveGearLevel(em, user);
            _maxLevels.AddOrUpdate(id, live, (_, prev) => live > prev ? live : prev);
        }

        internal static int GetLiveGearLevel(EntityManager em, User user)
        {
            var charEntity = user.LocalCharacter._Entity;
            if (charEntity == Entity.Null) return 0;
            if (!em.HasComponent<Equipment>(charEntity)) return 0;
            var eq = em.GetComponentData<Equipment>(charEntity);
            return (int)System.Math.Round(eq.ArmorLevel + eq.SpellLevel + eq.WeaponLevel);
        }
    }
}
