using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using NightLadder.Core.Models;
using NightLadder.Core.Services;
using NightLadder.Core.Storage;
using NightLadder.Plugin.Util;
using Unity.Entities;

namespace NightLadder.Plugin.Hooks;

// Bootstrap simples para aplicar patches Harmony quando o servidor carrega
[BepInPlugin(Guid, Name, Version)]
public class HarmonyBootstrap : BasePlugin
{
    public const string Guid = "vrising.nightladder.harmony";
    public const string Name = "NightLadder.Harmony";
    public const string Version = "0.1.0";

    private Harmony? _harmony;

    public override void Load()
    {
        if (App.HarmonyPatched)
        {
            Log.LogInfo("Harmony já aplicado anteriormente. Ignorando bootstrap duplicado.");
            return;
        }

        _harmony = new Harmony(Guid);
        var baseDir = Path.Combine(Paths.PluginPath, "NightLadder");
        var jsonPath = Path.Combine(baseDir, "ranks.json");
        var dbPath = Path.Combine(baseDir, "ranks.ldb");
        var config = new RankConfig();
        var storage = new LiteDbRankStorage(dbPath, jsonPath);
        App.RankManager = new RankManager(config, storage);
        _ = App.RankManager.InitializeAsync();

        // Aplica patches e aguarda EntityManager do servidor ficar disponível
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            for (int i = 0; i < 60; i++)
            {
                try
                {
                    var em = ServerWorldUtility.GetServerEntityManager();
                    App.ServerEntityManager = em;
                    if (App.RankManager != null && _harmony != null)
                    {
                        // Patch primário: hook estável no DeathEventListenerSystem
                        _harmony.PatchAll(typeof(DeathEventListenerPatch));

                        App.HarmonyPatched = true;
                        Log.LogInfo($"{Name} carregado e Harmony patches aplicados.");
                        return;
                    }
                }
                catch (System.Exception ex)
                {
                    Log.LogWarning($"Aguardando EntityManager... {ex.Message}");
                }
                await System.Threading.Tasks.Task.Delay(1000);
            }
            Log.LogWarning("[NightLadder] Não foi possível obter o Server EntityManager após várias tentativas. Desativando patches.");
        });
    }

    ~HarmonyBootstrap()
    {
        _ = App.RankManager?.SaveAsync();
    }
}
