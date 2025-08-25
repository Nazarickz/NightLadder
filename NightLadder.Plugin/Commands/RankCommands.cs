using VampireCommandFramework;
using NightLadder.Core.Services;
using NightLadder.Plugin.Util;
using NightLadder.Plugin.Hooks;
using System.Linq;

namespace NightLadder.Plugin.Commands;

[CommandGroup("rank")] // .rank <sub>
public static class RankCommands
{
    private static RankManager? _manager;

    public static void Initialize(RankManager manager) => _manager = manager;

    // Public commands
    [Command("elo", description: "Mostra seu elo e pontos.")]
    public static void Elo(ChatCommandContext ctx)
    {
        if (_manager == null) { ctx.Reply("Sistema de ranks não inicializado."); return; }
        var pr = _manager.GetOrCreate(ctx.User.PlatformId.ToString(), ctx.Name ?? "Jogador");
        ctx.Reply($"Elo: {pr.StepName} | Pontos: {pr.Points}");
    }

    [Command("top", description: "Mostra o top jogadores por elo/pontos.")]
    public static void Top(ChatCommandContext ctx, int n = 10)
    {
        if (_manager == null) { ctx.Reply("Sistema de ranks não inicializado."); return; }
        var list = _manager.GetTop(n).ToList();
        if (list.Count == 0) { ctx.Reply("Sem jogadores ranqueados ainda."); return; }
        int i = 1;
        foreach (var p in list)
        {
            ctx.Reply($"{i++}. {p.PlayerName} – {p.StepName} ({p.Points})");
        }
    }

    [Command("whoami", description: "Mostra seu ID (PlatformId) e nome.")]
    public static void WhoAmI(ChatCommandContext ctx)
    {
        ctx.Reply($"ID: {ctx.User.PlatformId} | Nome: {ctx.Name ?? "Jogador"}");
    }

    // Debug: reimprime histórico de logs
    [Command("debugelo", description: "[ADMIN] Reimprime no console o histórico de logs do NightLadder (últimos eventos).")]
    public static void DebugElo(ChatCommandContext ctx)
    {
        if (!ctx.IsAdmin) { ctx.Reply("Apenas administradores."); return; }
        var count = PluginLog.ReplayHistoryToConsole();
        ctx.Reply($"Reimpresso histórico de {count} linhas no console.");
    }

    // Debug: liga/desliga inspeção detalhada de DeathEvents
    [Command("debugelo.deaths", description: "[ADMIN] Liga/Desliga debug detalhado de DeathEvents (true/false).")]
    public static void DebugEloDeaths(ChatCommandContext ctx, bool enable)
    {
        if (!ctx.IsAdmin) { ctx.Reply("Apenas administradores."); return; }
        DeathHooks.SetDebugDeaths(enable);
        DeathHooksValue.SetDebugDeaths(enable);
        ctx.Reply($"Debug de mortes: {(enable ? "ON" : "OFF")}");
    }

    // Admin commands
    [Command("admin.add", description: "[ADMIN] Adiciona pontos ao jogador pelo PlatformId.")]
    public static void AdminAdd(ChatCommandContext ctx, string platformId, int points)
    {
        if (!ctx.IsAdmin) { ctx.Reply("Apenas administradores."); return; }
        if (_manager == null) { ctx.Reply("Sistema de ranks não inicializado."); return; }
        var pr = _manager.GetOrCreate(platformId, platformId);
        var before = pr.Points;
        _manager.ApplyDelta(pr, points);
        ctx.Reply($"OK. {platformId}: {before} -> {pr.Points} ({pr.StepName})");
    }

    [Command("admin.set", description: "[ADMIN] Define os pontos absolutos do jogador.")]
    public static void AdminSet(ChatCommandContext ctx, string platformId, int points)
    {
        if (!ctx.IsAdmin) { ctx.Reply("Apenas administradores."); return; }
        if (_manager == null) { ctx.Reply("Sistema de ranks não inicializado."); return; }
        var pr = _manager.GetOrCreate(platformId, platformId);
        _manager.SetPoints(pr, points);
        ctx.Reply($"OK. {platformId}: pontos = {pr.Points} ({pr.StepName})");
    }

    [Command("admin.step", description: "[ADMIN] Define o passo/elo do jogador (índice).")]
    public static void AdminSetStep(ChatCommandContext ctx, string platformId, int stepIndex)
    {
        if (!ctx.IsAdmin) { ctx.Reply("Apenas administradores."); return; }
        if (_manager == null) { ctx.Reply("Sistema de ranks não inicializado."); return; }
        var pr = _manager.GetOrCreate(platformId, platformId);
        _manager.SetStep(pr, stepIndex);
        ctx.Reply($"OK. {platformId}: step = {pr.StepIndex} ({pr.StepName})");
    }

    [Command("admin.reset", description: "[ADMIN] Zera pontos e elo do jogador.")]
    public static void AdminReset(ChatCommandContext ctx, string platformId)
    {
        if (!ctx.IsAdmin) { ctx.Reply("Apenas administradores."); return; }
        if (_manager == null) { ctx.Reply("Sistema de ranks não inicializado."); return; }
        var pr = _manager.GetOrCreate(platformId, platformId);
        _manager.Reset(pr);
        ctx.Reply($"OK. {platformId}: resetado.");
    }

    [Command("admin.sim", description: "[ADMIN] Simula vitória do jogador A (platformIdA) sobre B (platformIdB).")]
    public static void AdminSim(ChatCommandContext ctx, string platformIdA, string platformIdB)
    {
        if (!ctx.IsAdmin) { ctx.Reply("Apenas administradores."); return; }
        if (_manager == null) { ctx.Reply("Sistema de ranks não inicializado."); return; }
        var a = _manager.GetOrCreate(platformIdA, platformIdA);
        var b = _manager.GetOrCreate(platformIdB, platformIdB);
        var (da, db) = _manager.ComputeKillDeltas(a, b);
        ctx.Reply($"Vitória A vs B: +{da} / {db}");
    }

    // Novo: aplica uma kill simulada (chama RegisterKill) sem precisar matar in-game
    [Command("admin.win", description: "[ADMIN] Registra uma kill simulada entre A (killer) e B (victim). Não requer que estejam online.")]
    public static void AdminWin(ChatCommandContext ctx, string killerId, string victimId, string? killerName = null, string? victimName = null)
    {
        if (!ctx.IsAdmin) { ctx.Reply("Apenas administradores."); return; }
        if (_manager == null) { ctx.Reply("Sistema de ranks não inicializado."); return; }
        killerName ??= killerId;
        victimName ??= victimId;
        var (kd, vd, kr, vr) = _manager.RegisterKill(killerId, killerName, victimId, victimName);
        ctx.Reply($"OK. {killerName} matou {victimName}: +{kd} / {vd}. Elo atual: {kr.StepName} ({kr.Points}).");
    }

    // Conveniência: o próprio caller vence contra um alvo (por PlatformId)
    [Command("admin.mywin", description: "[ADMIN] Registra uma kill simulada sua contra o PlatformId alvo.")]
    public static void AdminMyWin(ChatCommandContext ctx, string victimId, string? victimName = null)
    {
        if (!ctx.IsAdmin) { ctx.Reply("Apenas administradores."); return; }
        if (_manager == null) { ctx.Reply("Sistema de ranks não inicializado."); return; }
        var killerId = ctx.User.PlatformId.ToString();
        var killerName = ctx.Name ?? "Jogador";
        victimName ??= victimId;
        var (kd, vd, kr, vr) = _manager.RegisterKill(killerId, killerName, victimId, victimName);
        ctx.Reply($"OK. {killerName} matou {victimName}: +{kd} / {vd}. Elo atual: {kr.StepName} ({kr.Points}).");
    }

    [Command("admin.save", description: "[ADMIN] Salva o estado dos ranks.")]
    public static async System.Threading.Tasks.Task AdminSave(ChatCommandContext ctx)
    {
        if (!ctx.IsAdmin) { ctx.Reply("Apenas administradores."); return; }
        if (_manager == null) { ctx.Reply("Sistema de ranks não inicializado."); return; }
        await _manager.SaveAsync();
        ctx.Reply("Ranks salvos.");
    }
}
