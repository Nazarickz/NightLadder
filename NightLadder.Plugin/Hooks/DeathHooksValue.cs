using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NightLadder.Core.Services;
using NightLadder.Plugin.Util;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace NightLadder.Plugin.Hooks;

// Patch alternativo para métodos que recebem DeathEvent por valor (sem ref)
[HarmonyPatch]
public static class DeathHooksValue
{
    private static RankManager? _manager;
    private static EntityManager? _em;
    private static volatile bool _debugDeaths;

    public static void Initialize(RankManager manager, EntityManager entityManager)
    {
        _manager = manager;
        _em = entityManager;
    }

    public static void SetDebugDeaths(bool enable) => _debugDeaths = enable;

    [HarmonyTargetMethods]
    static IEnumerable<MethodBase> TargetMethods()
    {
        var results = new List<MethodBase>();
        try
        {
            var asm = typeof(DeathEvent).Assembly;
            foreach (var t in asm.GetTypes())
            {
                if (t.FullName == null) continue;
                if (!t.FullName.Contains("Death", StringComparison.OrdinalIgnoreCase) &&
                    !t.FullName.Contains("OnDeath", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    var ps = m.GetParameters();
                    if (ps.Length == 1 && !ps[0].ParameterType.IsByRef && ps[0].ParameterType == typeof(DeathEvent))
                    {
                        results.Add(m);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warn($"[Value] TargetMethods scan falhou: {ex.Message}");
        }

        if (results.Count > 0)
        {
            var names = string.Join(", ", results.Select(r => r.DeclaringType?.FullName + "." + r.Name).Distinct());
            PluginLog.Info($"[Value] DeathHooks alvo(s): {names}");
        }
        return results;
    }

    [HarmonyPostfix]
    static void Postfix(DeathEvent __0)
    {
        if (_debugDeaths)
        {
            PluginLog.Info($"[DEBUG-DEATH:VAL] Evento recebido: Died={__0.Died.Index} Killer={__0.Killer.Index}");
        }

        if (_manager == null || _em == null) return;
        try
        {
            var died = __0.Died;
            var killer = __0.Killer;

            if (died == Entity.Null) return;

            if (!TryGetUserFromAny(_em.Value, died, out var victimUser)) return;
            if (!TryGetUserFromAny(_em.Value, killer, out var killerUser)) return;
            if (killerUser.PlatformId.Equals(victimUser.PlatformId)) return;

            string victimId = victimUser.PlatformId.ToString();
            string victimName = victimUser.CharacterName.ToString();
            string killerId = killerUser.PlatformId.ToString();
            string killerName = killerUser.CharacterName.ToString();

            var (kd, vd, kr, vr) = _manager.RegisterKill(killerId, killerName, victimId, victimName);
            var msg = new FixedString512Bytes($"Você matou {victimName} (+{kd}). Elo: {kr.StepName} ({kr.Points})");
            try { ServerChatUtils.SendSystemMessageToClient(_em.Value, killerUser, ref msg); } catch { }
            PluginLog.Info($"[VAL] Kill contabilizada: {killerName} (+{kd}) vs {victimName} ({vd}).");
        }
        catch (Exception ex)
        {
            PluginLog.Error($"[VAL] Exceção em DeathHooksValue.Postfix: {ex.Message}");
        }
    }

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
}
