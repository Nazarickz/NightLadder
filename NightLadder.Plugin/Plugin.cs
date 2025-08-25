using NightLadder.Core.Models;
using NightLadder.Core.Services;
using NightLadder.Core.Storage;
using Unity.Entities; // Para EntityManager
using NightLadder.Plugin.Util; // ServerWorldUtility

namespace NightLadder.Plugin;

// Esqueleto de plugin independente de framework específico. Integre com a API do seu servidor VRising (BepInEx, VampireCommandFramework, etc.)
public class Plugin
{
    private readonly RankManager _manager;

    public Plugin()
    {
        var config = new RankConfig();
        var storage = new JsonRankStorage(Path.Combine(AppContext.BaseDirectory, "Data", "ranks.json"));
        _manager = new RankManager(config, storage);
    }

    // Chame no início do servidor
    public async Task InitializeAsync()
    {
        await _manager.InitializeAsync();

        // Registra no App para uso em outros pontos se necessário
        App.RankManager = _manager;
        try { App.ServerEntityManager = ServerWorldUtility.GetServerEntityManager(); } catch { }
    }

    // Hook de evento: quando um player mata outro
    public void OnPlayerKill(string killerId, string killerName, string victimId, string victimName)
    {
        var (kd, vd, kr, vr) = _manager.RegisterKill(killerId, killerName, victimId, victimName);
        Console.WriteLine($"Kill: {killerName}(+{kd}) {kr.StepName} {kr.Points} vs {victimName}({vd}) {vr.StepName} {vr.Points}");
    }

    // Salvar periodicamente e ao desligar
    public Task SaveAsync() => _manager.SaveAsync();

    // Comandos utilitários
    public IEnumerable<PlayerRank> Top(int n = 10) => _manager.GetTop(n);
}
