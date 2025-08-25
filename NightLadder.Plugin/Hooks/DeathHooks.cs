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

// Hooka o processamento de DeathEvent para converter em OnPlayerKill
[HarmonyPatch]
public static class DeathHooks
{
    private static RankManager? _manager;
    private static EntityManager? _em;

    // debug: imprime no console cada DeathEvent e tentativa de resolução
    private static volatile bool _debugDeaths;

    public static void Initialize(RankManager manager, EntityManager entityManager, Harmony harmony)
    {
        _manager = manager;
        _em = entityManager;
        try
        {
            var patched = harmony.CreateClassProcessor(typeof(DeathHooks)).Patch();
            PluginLog.Info($"DeathHooks Patch iniciado. Métodos alvo encontrados? {patched != null}");
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Erro ao aplicar DeathHooks: {ex.Message}");
        }
    }

    // Permite atualizar o manager após reload
    public static void SetManager(RankManager manager)
    {
        _manager = manager;
    }

    // Habilita/Desabilita debug detalhado de mortes
    public static void SetDebugDeaths(bool enable)
    {
        _debugDeaths = enable;
        PluginLog.Info($"Debug de DeathEvents: {(enable ? "ON" : "OFF")}");
    }

    // Alvo dinâmico: procura todos os jobs que tenham DeathEvent em qualquer parâmetro
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
                bool looksLikeDeath = t.FullName.Contains("OnDeathSystem", StringComparison.OrdinalIgnoreCase) ||
                                      t.FullName.Contains("OnDeath", StringComparison.OrdinalIgnoreCase) ||
                                      t.FullName.Contains("DeathSystem", StringComparison.OrdinalIgnoreCase);

                if (!looksLikeDeath) continue;

                foreach (var nt in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    foreach (var m in nt.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        var ps = m.GetParameters();
                        if (ps.Any(p => (p.ParameterType.IsByRef && p.ParameterType.GetElementType() == typeof(DeathEvent)) || p.ParameterType == typeof(DeathEvent)))
                        {
                            results.Add(m);
                        }
                    }
                }

                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    var ps = m.GetParameters();
                    if (ps.Any(p => (p.ParameterType.IsByRef && p.ParameterType.GetElementType() == typeof(DeathEvent)) || p.ParameterType == typeof(DeathEvent)))
                    {
                        results.Add(m);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warn($"TargetMethods scan falhou: {ex.Message}");
        }

        // Fallback herdado com nomes conhecidos
        var candidates = new[]
        {
            "ProjectM.Gameplay.Systems.OnDeathSystem+OnDeathSystem_762B491D_LambdaJob_0_Job",
            "ProjectM.Gameplay.Systems.OnDeathSystem_762B491D_LambdaJob_0_Job"
        };
        foreach (var typeName in candidates)
        {
            var t = AccessTools.TypeByName(typeName);
            if (t == null) continue;
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var ps = m.GetParameters();
                if (ps.Any(p => (p.ParameterType.IsByRef && p.ParameterType.GetElementType() == typeof(DeathEvent)) || p.ParameterType == typeof(DeathEvent)))
                {
                    results.Add(m);
                }
            }
        }

        // Dedup e blacklist de métodos problemáticos (ex.: DeathUtilities.Kill)
        results = results
            .Where(m => m != null)
            .Distinct()
            .Where(m => !(m.DeclaringType?.FullName?.Contains("ProjectM.DeathUtilities") == true || m.Name == "Kill"))
            .ToList();

        if (results.Count == 0)
        {
            PluginLog.Warn("Nenhum alvo de DeathEvent encontrado para patch. Kill tracking ficará inativo.");
        }
        else
        {
            var names = string.Join(", ", results.Select(r => r.DeclaringType?.FullName + "." + r.Name).Distinct());
            PluginLog.Info($"DeathHooks alvo(s): {names}");
        }
        return results;
    }

    // Postfix único e genérico: obtém DeathEvent de qualquer posição
    [HarmonyPostfix]
    static void PostfixArgs(MethodBase __originalMethod, object[] __args)
    {
        try
        {
            if (__args == null) return;
            foreach (var a in __args)
            {
                if (a is DeathEvent de)
                {
                    HandleEvent(de);
                    return;
                }
            }
            if (_debugDeaths)
            {
                PluginLog.Warn($"[DEBUG-DEATH] Postfix em {__originalMethod?.DeclaringType?.FullName}.{__originalMethod?.Name} mas nenhum DeathEvent encontrado nos argumentos.");
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warn($"PostfixArgs falhou: {ex.Message}");
        }
    }

    private static void HandleEvent(DeathEvent de)
    {
        try
        {
            var died = de.Died;
            var killer = de.Killer;

            if (_debugDeaths)
            {
                PluginLog.Info($"[DEBUG-DEATH] Evento recebido: Died={died.Index} Killer={killer.Index}");
                if (_em != null)
                {
                    var diedDesc = DescribeEntitySafe(_em.Value, died);
                    var killerDesc = DescribeEntitySafe(_em.Value, killer);
                    PluginLog.Info($"[DEBUG-DEATH] Died components: {diedDesc}");
                    PluginLog.Info($"[DEBUG-DEATH] Killer components: {killerDesc}");
                }
            }
        }
        catch { }

        if (_manager == null || _em == null) return;
        try
        {
            var died = de.Died;
            var killer = de.Killer;

            if (died == Entity.Null) { PluginLog.Warn("DeathEvent sem Died válido."); return; }

            if (!TryGetUserFromAny(_em.Value, died, out var victimUser))
            {
                if (TryGetUserFromAny(_em.Value, killer, out var kUser))
                {
                    var dbg = new FixedString512Bytes("NightLadder: morte ignorada (vítima não é jogador). Sem pontos.");
                    try { ServerChatUtils.SendSystemMessageToClient(_em.Value, kUser, ref dbg); } catch { }
                }
                return;
            }

            if (!TryGetUserFromAny(_em.Value, killer, out var killerUser))
            {
                var dbg = new FixedString512Bytes("NightLadder: killer não identificado (habilidade/projétil). Kill não pontuada.");
                try { ServerChatUtils.SendSystemMessageToClient(_em.Value, victimUser, ref dbg); } catch { }
                PluginLog.Warn($"Killer não resolvido. Died={died.Index} Killer={killer.Index}");
                return;
            }

            if (killerUser.PlatformId.Equals(victimUser.PlatformId))
            {
                var dbg = new FixedString512Bytes("NightLadder: morte própria/suicídio ignorada.");
                try { ServerChatUtils.SendSystemMessageToClient(_em.Value, killerUser, ref dbg); } catch { }
                return;
            }

            string victimId = victimUser.PlatformId.ToString();
            string victimName = victimUser.CharacterName.ToString();
            string killerId = killerUser.PlatformId.ToString();
            string killerName = killerUser.CharacterName.ToString();

            var (kd, vd, kr, vr) = _manager.RegisterKill(killerId, killerName, victimId, victimName);

            var msg = new FixedString512Bytes($"Você matou {victimName} (+{kd}). Elo: {kr.StepName} ({kr.Points})");
            try { ServerChatUtils.SendSystemMessageToClient(_em.Value, killerUser, ref msg); } catch { }
            PluginLog.Info($"Kill contabilizada: {killerName} (+{kd}) vs {victimName} ({vd}).");
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Exceção em DeathHooks.HandleEvent: {ex.Message}");
        }
    }

    private static string DescribeEntitySafe(EntityManager em, Entity e)
    {
        if (e == Entity.Null) return "<NULL>";
        try
        {
            var flags = new List<string>(6);
            if (em.Exists(e))
            {
                if (em.HasComponent<PlayerCharacter>(e)) flags.Add("PlayerCharacter");
                if (em.HasComponent<User>(e)) flags.Add("User");
                if (em.HasComponent<EntityOwner>(e)) flags.Add("EntityOwner");
            }
            return flags.Count == 0 ? "<no-known-components>" : string.Join("|", flags);
        }
        catch { return "<inspect-error>"; }
    }

    // Resolve User a partir de um Entity que pode ser um PlayerCharacter ou proprietário (EntityOwner) de uma entidade de habilidade/projétil
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
