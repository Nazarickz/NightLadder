using System;
using System.Linq;
using HarmonyLib;
using NightLadder.Core.Services;
using NightLadder.Plugin.Util;
using NightLadder.Plugin.Combat; // AssistStore
using NightLadder.Plugin.Services; // LevelService
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace NightLadder.Plugin.Hooks;

// Patch direto do sistema nativo que processa DeathEvent. Estável entre versões.
[HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
public static class DeathEventListenerPatch
{
    private static EntityQuery _deathQuery;

    private static RankManager? Manager => App.RankManager;
    private static EntityManager? EM => App.ServerEntityManager;

    private static void EnsureQuery()
    {
        if (EM == null) return;
        // Some Unity.Entities versions don't expose IsCreated; rely on default comparison
        if (_deathQuery.Equals(default(EntityQuery)))
        {
            _deathQuery = EM.Value.CreateEntityQuery(ComponentType.ReadOnly<DeathEvent>());
        }
    }

    // Postfix do OnUpdate para varrer todos os DeathEvent criados neste frame
    public static void Postfix(DeathEventListenerSystem __instance)
    {
        if (Manager == null || EM == null) return;
        EnsureQuery();

        NativeArray<Entity> entities = default;
        try
        {
            entities = _deathQuery.ToEntityArray(Allocator.Temp);
            foreach (var e in entities)
            {
                if (!EM.Value.HasComponent<DeathEvent>(e)) continue;
                var de = EM.Value.GetComponentData<DeathEvent>(e);
                HandleDeathEvent(EM.Value, de);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warn($"[NightLadder] Falha ao processar DeathEventListenerPatch: {ex.Message}");
        }
        finally
        {
            if (entities.IsCreated) entities.Dispose();
        }
    }

    private static void HandleDeathEvent(EntityManager em, DeathEvent de)
    {
        try
        {
            var victim = de.Died;
            // Preferir o "downer" (quem derrubou) quando disponível
            var killerResolved = KillCache.GetDowner(victim) ?? de.Killer;
            KillCache.Clear(victim);

            if (victim == Entity.Null) return;

            if (!TryGetUserFromAny(em, victim, out var victimUser))
            {
                // Se só killer for jogador, informa que morte não pontua
                if (TryGetUserFromAny(em, killerResolved, out var kUser))
                {
                    var dbg = new FixedString512Bytes("NightLadder: morte ignorada (vítima não é jogador). Sem pontos.");
                    try { ServerChatUtils.SendSystemMessageToClient(em, kUser, ref dbg); } catch { }
                }
                return;
            }

            if (!TryGetUserFromAny(em, killerResolved, out var killerUser))
            {
                var dbg = new FixedString512Bytes("NightLadder: killer não identificado (habilidade/projétil). Kill não pontuada.");
                try { ServerChatUtils.SendSystemMessageToClient(em, victimUser, ref dbg); } catch { }
                PluginLog.Warn($"Killer não resolvido. Died={victim.Index} Killer={killerResolved.Index}");
                return;
            }

            if (killerUser.PlatformId.Equals(victimUser.PlatformId))
            {
                var dbg = new FixedString512Bytes("NightLadder: morte própria/suicídio ignorada.");
                try { ServerChatUtils.SendSystemMessageToClient(em, killerUser, ref dbg); } catch { }
                return;
            }

            string victimId = victimUser.PlatformId.ToString();
            string victimName = victimUser.CharacterName.ToString();
            string killerId = killerUser.PlatformId.ToString();
            string killerName = killerUser.CharacterName.ToString();

            // Registrar kill principal
            var killRes = Manager!.RegisterKillDetailed(killerId, killerName, victimId, victimName);
            int baseKd = killRes.KillerDelta;
            int kd = baseKd;
            int vd = killRes.VictimDelta;

            // Anti-farm por nível
            ApplyLevelPenaltyIfConfigured(em, killerUser, victimUser, ref kd);
            if (kd != baseKd)
            {
                var killerRank = killRes.KillerRank;
                int adjust = kd - baseKd;
                if (adjust != 0) Manager.ApplyDelta(killerRank, adjust);
            }

            // Assistências (clã)
            var assistersAll = AssistStore.GetRecentAssisters(victimId, killerId);
            var cfg = App.RankManager?.Config;
            List<(string Id, string Name)> assisters = cfg != null && cfg.AssistClanOnlyEnabled
                ? AssistStore.FilterClanAllies(em, killerUser, assistersAll)
                : new List<(string, string)>();

            var assistResults = new System.Collections.Generic.List<(string Name, int Delta, bool Promoted, NightLadder.Core.Models.PlayerRank Rank)>();
            foreach (var (assistId, assistName) in assisters)
            {
                if (assistId == killerId || assistId == victimId) continue;

                var assistRank = Manager.GetOrCreate(assistId, assistName);
                var victimRank = Manager.GetOrCreate(victimId, victimName);
                var (assistDeltaFull, _) = Manager.ComputeKillDeltas(assistRank, victimRank);
                var assistDelta = Math.Max(1, (int)Math.Floor(assistDeltaFull / 3.0));

                var assistUser = GetUserByPlatformId(em, assistId);
                if (assistUser.HasValue)
                {
                    ApplyLevelPenaltyIfConfigured(em, assistUser.Value, victimUser, ref assistDelta);
                }
                var assistRes = Manager.AwardAssistDetailed(assistId, assistName, assistDelta);
                assistResults.Add((assistName, assistDelta, assistRes.Promoted, assistRes.Rank));

                // Mensagem local ao assistente
                if (assistUser.HasValue)
                {
                    var msgAssistLocal = new FixedString512Bytes($"Assistência vs {victimName} +{assistDelta}pts. Elo: {assistRes.Rank.StepName} ({assistRes.Rank.Points}pts)");
                    try { ServerChatUtils.SendSystemMessageToClient(em, assistUser.Value, ref msgAssistLocal); } catch { }
                }
            }

            // Feedback ao killer/vítima
            var killerRankAfter = Manager.GetOrCreate(killerId, killerName);
            var victimRankAfter = Manager.GetOrCreate(victimId, victimName);
            var msgKiller = new FixedString512Bytes($"Você matou {victimName} ({victimRankAfter.StepName}) +{kd}pts");
            try { ServerChatUtils.SendSystemMessageToClient(em, killerUser, ref msgKiller); } catch { }

            var msgVictim = new FixedString512Bytes($"Você morreu para {killerName} ({killerRankAfter.StepName}) {vd}pts");
            try { ServerChatUtils.SendSystemMessageToClient(em, victimUser, ref msgVictim); } catch { }

            if (assistResults.Count > 0)
            {
                var names = string.Join(", ", assistResults.Select(a => $"{a.Name}(+{a.Delta})"));
                var msgAssist = new FixedString512Bytes($"Assistência: {names}");
                try { ServerChatUtils.SendSystemMessageToClient(em, killerUser, ref msgAssist); } catch { }
            }

            // Mensagens de promoção (global/local) exatas com promoted==true
            TryAnnouncePromotions(em, killerUser, killerName, killRes.KillerRank, killRes.KillerPromoted);
            TryAnnouncePromotions(em, victimUser, victimName, killRes.VictimRank, killRes.VictimPromoted);
            foreach (var ar in assistResults)
            {
                var u = GetUserByPlatformId(em, App.RankManager?.GetAll().FirstOrDefault(r => r.PlayerName == ar.Name)?.PlayerId ?? string.Empty);
                if (u.HasValue)
                {
                    TryAnnouncePromotions(em, u.Value, ar.Name, ar.Rank, ar.Promoted);
                }
            }

            // Limpa janela do vítima para não contar em dobro
            AssistStore.ClearVictim(victimId);

            PluginLog.Info($"Kill contabilizada: {killerName} (+{kd}) vs {victimName} ({vd}). Assists: {assistResults.Count}");
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Exceção em DeathEventListenerPatch.HandleDeathEvent: {ex.Message}");
        }
    }

    private static void ApplyLevelPenaltyIfConfigured(EntityManager em, User killerUser, User victimUser, ref int delta)
    {
        try
        {
            var cfg = App.RankManager?.Config;
            if (cfg == null || !cfg.LevelPenaltyEnabled) return;

            int killerLvl = LevelService.GetPlayerLevel(em, killerUser);
            int victimLvl = LevelService.GetPlayerLevel(em, victimUser);
            int diff = killerLvl - victimLvl;
            if (diff <= cfg.LevelGapThreshold) return;

            int over = diff - cfg.LevelGapThreshold;
            double reduction = over * cfg.LevelPenaltyPerLevelPercent;
            if (reduction > cfg.LevelPenaltyMaxReductionPercent) reduction = cfg.LevelPenaltyMaxReductionPercent;

            int reduceBy = (int)System.Math.Floor(delta * reduction);
            delta = System.Math.Max(1, delta - reduceBy);
        }
        catch { }
    }

    private static User? GetUserByPlatformId(EntityManager em, string platformId)
    {
        try
        {
            // Percorre todos Users procurando pelo PlatformId. Custo aceitável pela raridade do evento.
            var q = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var entities = q.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var e in entities)
                {
                    var u = em.GetComponentData<User>(e);
                    if (u.PlatformId.ToString() == platformId) return u;
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
        catch { }
        return null;
    }

    // Helpers (copiados do DeathHooks para resolver o User a partir de qualquer entidade)
    private static bool TryGetUserFromAny(EntityManager em, Entity entity, out User user)
    {
        user = default;
        if (entity == Entity.Null) return false;

        if (TryGetUserFromCharacter(em, entity, out user)) return true;

        try
        {
            var current = entity;
            for (int i = 0; i < 5; i++)
            {
                if (current == Entity.Null) break;
                if (TryGetUserFromCharacter(em, current, out user)) return true;

                if (em.HasComponent<EntityOwner>(current))
                {
                    var owner = em.GetComponentData<EntityOwner>(current).Owner;
                    if (owner == current) break;
                    current = owner;
                    continue;
                }

                if (em.HasComponent<PlayerCharacter>(current))
                {
                    var pc = em.GetComponentData<PlayerCharacter>(current);
                    var ue = pc.UserEntity;
                    if (ue != Entity.Null && em.HasComponent<User>(ue))
                    {
                        user = em.GetComponentData<User>(ue);
                        return true;
                    }
                }

                break;
            }
        }
        catch { }

        return false;
    }

    private static bool TryGetUserFromCharacter(EntityManager em, Entity character, out User user)
    {
        user = default;
        try
        {
            if (character != Entity.Null && em.HasComponent<PlayerCharacter>(character))
            {
                var pc = em.GetComponentData<PlayerCharacter>(character);
                var userEntity = pc.UserEntity;
                if (userEntity != Entity.Null && em.HasComponent<User>(userEntity))
                {
                    user = em.GetComponentData<User>(userEntity);
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    private static void TryAnnouncePromotions(EntityManager em, User user, string name, NightLadder.Core.Models.PlayerRank rank, bool promoted)
    {
        try
        {
            // Mensagem local ao jogador
            var local = new FixedString512Bytes($"Elo atualizado: {rank.StepName} ({rank.Points}pts)");
            ServerChatUtils.SendSystemMessageToClient(em, user, ref local);

            if (promoted)
            {
                // Mensagem global exata
                var global = new FixedString512Bytes($"Parabéns! {name} subiu para {rank.StepName}!");
                ServerChatUtils.SendSystemMessageToAllClients(em, ref global);
            }
        }
        catch { }
    }
}
