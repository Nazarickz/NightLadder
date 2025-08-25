using System;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;   // StatChangeSystem, StatChangeReason
using ProjectM.Network;            // EntityOwner, User
using Unity.Entities;              // EntityManager
using NightLadder.Plugin.Util;     // PluginLog
using NightLadder.Plugin.Combat;   // AssistStore

namespace NightLadder.Plugin.Hooks
{
    // Monitora StatChangeEvent para montar a janela de assistências
    public static class StatChangeHook
    {
        [HarmonyPatch(typeof(StatChangeSystem), nameof(StatChangeSystem.OnUpdate))]
        public static class Patch
        {
            public static void Prefix(StatChangeSystem __instance)
            {
                try
                {
                    var changes = __instance._MergedChanges;
                    var em = __instance.EntityManager;

                    foreach (var ev in changes)
                    {
                        if (ev.Reason != StatChangeReason.DealDamageSystem_0) continue;
                        if (ev.Change >= 0) continue;
                        if (!em.HasComponent<EntityOwner>(ev.Source)) continue;

                        var attackerEnt = em.GetComponentData<EntityOwner>(ev.Source).Owner;
                        var victimEnt = ev.Entity;

                        if (!em.HasComponent<PlayerCharacter>(attackerEnt)) continue;
                        if (!em.HasComponent<PlayerCharacter>(victimEnt)) continue;

                        var attackerUserEntity = em.GetComponentData<PlayerCharacter>(attackerEnt).UserEntity;
                        var victimUserEntity = em.GetComponentData<PlayerCharacter>(victimEnt).UserEntity;
                        if (attackerUserEntity == Entity.Null || victimUserEntity == Entity.Null) continue;
                        if (!em.HasComponent<User>(attackerUserEntity) || !em.HasComponent<User>(victimUserEntity)) continue;

                        var attackerUser = em.GetComponentData<User>(attackerUserEntity);
                        var victimUser = em.GetComponentData<User>(victimUserEntity);

                        string aId = attackerUser.PlatformId.ToString();
                        string vId = victimUser.PlatformId.ToString();
                        string aNm = attackerUser.CharacterName.ToString();
                        string vNm = victimUser.CharacterName.ToString();

                        AssistStore.RegisterHit(aId, aNm, vId, vNm);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Warn($"[StatChangeHook] Falha ao inspecionar mudanças de estatística: {ex.Message}");
                }
            }
        }
    }
}
