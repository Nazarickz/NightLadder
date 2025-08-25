using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM;             // ClanTeam
using ProjectM.Network;     // User
using Unity.Entities;       // EntityManager

namespace NightLadder.Plugin.Combat
{
    internal struct AssistHit
    {
        public string AttackerId;   // PlatformId.ToString()
        public string AttackerName;
        public string VictimId;     // PlatformId.ToString()
        public string VictimName;
        public long TimestampUtc;
    }

    // Mantém histórico curto de hits para determinar assistências por vítima.
    internal static class AssistStore
    {
        private const double AssistWindowSeconds = 30.0;
        private const int MaxRecordPerVictim = 512;
        private const int MaxAssistersPerKill = 2; // limite por kill (somente do mesmo clã)

        private static readonly Dictionary<string, List<AssistHit>> _hitsByVictim = new();

        public static void RegisterHit(string attackerId, string attackerName, string victimId, string victimName)
        {
            if (!_hitsByVictim.TryGetValue(victimId, out var list))
            {
                list = new List<AssistHit>(8);
                _hitsByVictim[victimId] = list;
            }

            list.Add(new AssistHit
            {
                AttackerId = attackerId,
                AttackerName = attackerName,
                VictimId = victimId,
                VictimName = victimName,
                TimestampUtc = DateTime.UtcNow.Ticks
            });

            if (list.Count > MaxRecordPerVictim)
            {
                Cleanup(victimId);
            }
        }

        public static List<(string Id, string Name)> GetRecentAssisters(string victimId, string killerId)
        {
            Cleanup(victimId);
            if (!_hitsByVictim.TryGetValue(victimId, out var list)) return new List<(string, string)>();

            long now = DateTime.UtcNow.Ticks;
            long window = TimeSpan.FromSeconds(AssistWindowSeconds).Ticks;
            long earliest = now - window;

            var assisters = list
                .Where(h => h.TimestampUtc >= earliest)
                .GroupBy(h => h.AttackerId)
                .Select(g => g.Last())
                .Where(h => h.AttackerId != killerId && h.AttackerId != victimId)
                .Select(h => (h.AttackerId, h.AttackerName))
                .ToList();

            return assisters;
        }

        // Agora: retorna somente assistentes do mesmo clã do killer, limitando a N por kill
        public static List<(string Id, string Name)> FilterClanAllies(EntityManager em, User killerUser, List<(string Id, string Name)> assisters)
        {
            try
            {
                var killerClanEntity = killerUser.ClanEntity._Entity;
                if (killerClanEntity == Entity.Null || !em.HasComponent<ClanTeam>(killerClanEntity))
                {
                    // killer sem clã -> nenhuma assistência válida
                    return new List<(string, string)>();
                }
                var killerClan = em.GetComponentData<ClanTeam>(killerClanEntity).Name.ToString();
                if (string.IsNullOrEmpty(killerClan)) return new List<(string, string)>();

                var q = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var users = q.ToEntityArray(Unity.Collections.Allocator.Temp);
                try
                {
                    var byId = new Dictionary<string, User>(assisters.Count);
                    foreach (var e in users)
                    {
                        var u = em.GetComponentData<User>(e);
                        var id = u.PlatformId.ToString();
                        if (assisters.Any(a => a.Id == id))
                            byId[id] = u;
                    }

                    var sameClan = assisters
                        .Where(a =>
                        {
                            if (!byId.TryGetValue(a.Id, out var u)) return false;
                            var ce = u.ClanEntity._Entity;
                            if (ce == Entity.Null || !em.HasComponent<ClanTeam>(ce)) return false;
                            var clan = em.GetComponentData<ClanTeam>(ce).Name.ToString();
                            return string.Equals(clan, killerClan, StringComparison.OrdinalIgnoreCase);
                        })
                        .Take(MaxAssistersPerKill)
                        .ToList();

                    return sameClan;
                }
                finally
                {
                    users.Dispose();
                }
            }
            catch { }
            return new List<(string, string)>();
        }

        public static void ClearVictim(string victimId)
        {
            _hitsByVictim.Remove(victimId);
        }

        private static void Cleanup(string victimId)
        {
            if (!_hitsByVictim.TryGetValue(victimId, out var list)) return;
            long now = DateTime.UtcNow.Ticks;
            long window = TimeSpan.FromSeconds(AssistWindowSeconds).Ticks;
            list.RemoveAll(h => now - h.TimestampUtc > window);
        }
    }
}
