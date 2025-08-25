using System.Collections.Concurrent;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;

namespace NightLadder.Plugin.Services
{
    // Serviço de nível: fornece nível Live (equipamento atual) e mantém o Máximo observado (em memória)
    internal static class LevelService
    {
        // Armazena nível máximo observado por PlatformId (string para reuso com Rank IDs)
        private static readonly ConcurrentDictionary<string, int> _maxLevels = new();

        public static int GetPlayerLevel(EntityManager em, User user)
        {
            int live = GetLiveGearLevel(em, user);
            var mode = App.RankManager?.Config.LevelTrackingMode ?? "Live";
            string id = user.PlatformId.ToString();

            if (mode.Equals("Max", System.StringComparison.OrdinalIgnoreCase))
            {
                // Atualiza e retorna o máximo observado
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
