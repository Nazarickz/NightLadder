using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using NightLadder.Core.Services;
using NightLadder.Core.Storage;
using NightLadder.Plugin.Commands;
using NightLadder.Plugin.Config;
using NightLadder.Plugin.Security;
using NightLadder.Plugin.Util;
using Unity.Entities;
using VampireCommandFramework; // VCF

namespace NightLadder.Plugin;

[BepInPlugin(Guid, Name, Version)]
[BepInDependency("gg.deca.VampireCommandFramework")] // Ensure VCF loads first
public class NightLadderBepInExPlugin : BasePlugin
{
    public const string Guid = "vrising.nightladder";
    public const string Name = "NightLadder";
    public const string Version = "0.1.0";

    private RankManager? _manager;
    private Harmony? _harmony;

    public override void Load()
    {
        PluginLog.Init(Name);
        PluginLog.Info("Inicializando NightLadder...");
        var baseDir = Path.Combine(Paths.PluginPath, "NightLadder");
        var configPath = Path.Combine(baseDir, "rankconfig.json");
        var cfg = RankConfigProvider.LoadOrCreate(configPath);

        // Storage: LiteDB com import/migração de JSON (ranks.json)
        var jsonPath = Path.Combine(baseDir, "ranks.json");
        var litedbPath = Path.Combine(baseDir, "ranks.ldb");
        IRankStorage storage = new LiteDbRankStorage(litedbPath, jsonPath);

        _manager = new RankManager(cfg, storage);
        _ = _manager.InitializeAsync();
        App.RankManager = _manager;

        // Admin whitelist
        AdminService.Initialize(Path.Combine(baseDir, "admins.json"));

        // VCF: registra comandos deste assembly UMA vez (inclui .rank e .rk)
        var asm = typeof(RankCommands).Assembly;
        CommandRegistry.UnregisterAssembly(asm);
        RankCommands.Initialize(_manager);
        CommandRegistry.RegisterAll(asm);
        PluginLog.Info("Comandos registrados (.rank e .rk) sem duplicatas.");

        // Harmony patches (único bootstrap)
        _harmony = new Harmony(Guid);
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
                        // Patch estável baseado no sistema nativo de DeathEvent
                        _harmony.PatchAll(typeof(Hooks.DeathEventListenerPatch));
                        // Patch para registrar quem derrubou o alvo (downer) e evitar kill-steal inconsistências
                        _harmony.PatchAll(typeof(Hooks.VampireDownedPatch));
                        // Patch de dano para rastrear assistências
                        _harmony.PatchAll(typeof(Hooks.StatChangeHook.Patch));

                        App.HarmonyPatched = true;
                        PluginLog.Info("Patches aplicados com sucesso.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Warn($"Aguardando EntityManager... {ex.Message}");
                }
                await System.Threading.Tasks.Task.Delay(1000);
            }
            PluginLog.Warn("EntityManager indisponível. Hooks não aplicados.");
        });
    }

    ~NightLadderBepInExPlugin()
    {
        _ = _manager?.SaveAsync();
    }
}
