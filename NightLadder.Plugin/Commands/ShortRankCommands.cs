using VampireCommandFramework;

namespace NightLadder.Plugin.Commands;

// Comandos curtos (provisórios) para facilitar digitação: prefixo .rk
[CommandGroup("rk")] // .rk <sub>
public static class ShortRankCommands
{
    // Públicos
    [Command("el", description: "Seu elo e pontos (alias de rank elo)")]
    public static void E(VampireCommandFramework.ChatCommandContext ctx) => RankCommands.Elo(ctx);

    [Command("tp", description: "Top N (alias de rank top)")]
    public static void T(VampireCommandFramework.ChatCommandContext ctx, int n = 10) => RankCommands.Top(ctx, n);

    [Command("id", description: "Seu PlatformId e nome (alias de rank whoami)")]
    public static void Id(VampireCommandFramework.ChatCommandContext ctx) => RankCommands.WhoAmI(ctx);

    // Debug
    [Command("dbg", description: "[ADMIN] Reimprime histórico de logs (alias de rank debugelo)")]
    public static void Dbg(VampireCommandFramework.ChatCommandContext ctx) => RankCommands.DebugElo(ctx);

    [Command("dbgd", description: "[ADMIN] Liga/Desliga debug de mortes (alias de rank debugelo.deaths)")]
    public static void DbgDeaths(VampireCommandFramework.ChatCommandContext ctx, bool enable) => RankCommands.DebugEloDeaths(ctx, enable);

    // Admin (aliases diretos)
    [Command("add", description: "[ADMIN] Adiciona pontos (alias de rank admin.add)")]
    public static void Add(VampireCommandFramework.ChatCommandContext ctx, string platformId, int points) => RankCommands.AdminAdd(ctx, platformId, points);

    [Command("set", description: "[ADMIN] Define pontos (alias de rank admin.set)")]
    public static void Set(VampireCommandFramework.ChatCommandContext ctx, string platformId, int points) => RankCommands.AdminSet(ctx, platformId, points);

    [Command("stp", description: "[ADMIN] Define elo (índice) (alias de rank admin.step)")]
    public static void Stp(VampireCommandFramework.ChatCommandContext ctx, string platformId, int stepIndex) => RankCommands.AdminSetStep(ctx, platformId, stepIndex);

    [Command("rs", description: "[ADMIN] Reseta jogador (alias de rank admin.reset)")]
    public static void Rs(VampireCommandFramework.ChatCommandContext ctx, string platformId) => RankCommands.AdminReset(ctx, platformId);

    [Command("sim", description: "[ADMIN] Simula vitória A sobre B (alias de rank admin.sim)")]
    public static void Sim(VampireCommandFramework.ChatCommandContext ctx, string platformIdA, string platformIdB) => RankCommands.AdminSim(ctx, platformIdA, platformIdB);

    [Command("sv", description: "[ADMIN] Salva ranks (alias de rank admin.save)")]
    public static System.Threading.Tasks.Task Sv(VampireCommandFramework.ChatCommandContext ctx) => RankCommands.AdminSave(ctx);

    [Command("win", description: "[ADMIN] Registra kill simulada A> B (alias de rank admin.win)")]
    public static void Win(VampireCommandFramework.ChatCommandContext ctx, string killerId, string victimId, string? killerName = null, string? victimName = null) => RankCommands.AdminWin(ctx, killerId, victimId, killerName, victimName);

    [Command("my", description: "[ADMIN] Você vence B (alias de rank admin.mywin)")]
    public static void My(VampireCommandFramework.ChatCommandContext ctx, string victimId, string? victimName = null) => RankCommands.AdminMyWin(ctx, victimId, victimName);
}
