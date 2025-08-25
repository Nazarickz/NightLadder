using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace NightLadder.Plugin.Hooks
{
    [HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
    public static class VampireDownedPatch
    {
        public static void Prefix(VampireDownedServerEventSystem __instance)
        {
            var em = __instance.EntityManager;
            // Access the query field if available; otherwise, create a safe query for VampireDownedBuff components
            NativeArray<Entity> downedEvents = default;
            try
            {
                // Some versions expose a generated query field; use broader query if private field is not accessible
                var q = em.CreateEntityQuery(ComponentType.ReadOnly<VampireDownedBuff>());
                downedEvents = q.ToEntityArray(Allocator.Temp);

                foreach (var entity in downedEvents)
                {
                    if (!VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, em, out var victimEntity))
                        continue;

                    var downBuff = em.GetComponentData<VampireDownedBuff>(entity);

                    if (!VampireDownedServerEventSystem.TryFindRootOwner(downBuff.Source, 1, em, out var killerEntity))
                        continue;

                    if (!em.HasComponent<PlayerCharacter>(victimEntity) || !em.HasComponent<PlayerCharacter>(killerEntity))
                        continue;

                    var victimUserEntity = em.GetComponentData<PlayerCharacter>(victimEntity).UserEntity;
                    var killerUserEntity = em.GetComponentData<PlayerCharacter>(killerEntity).UserEntity;

                    if (!em.HasComponent<User>(victimUserEntity) || !em.HasComponent<User>(killerUserEntity))
                        continue;

                    KillCache.SetDowner(victimEntity, killerEntity);
                }
            }
            finally
            {
                if (downedEvents.IsCreated) downedEvents.Dispose();
            }
        }
    }
}
