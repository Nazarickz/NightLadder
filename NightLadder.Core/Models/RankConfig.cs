namespace NightLadder.Core.Models;

public record RankStep(string Name, int ThresholdPoints, bool ResetsOnPromotion);

public class RankConfig
{
    public int SameTierKillPoints { get; set; } = 15;
    public int PerTierDifferenceBonus { get; set; } = 15;
    public int PerTierDifferencePenalty { get; set; } = 2;
    public int DraculaSlots { get; set; } = 4;

    // Anti-smurf/anti-farm de low level: penaliza ganhos quando killer tem n�vel muito acima da v�tima
    public bool LevelPenaltyEnabled { get; set; } = true;
    // Se killerLevel - victimLevel > LevelGapThreshold, aplica penalidade
    public int LevelGapThreshold { get; set; } = 10;
    // Percentual de redu��o adicional por n�vel acima do threshold (0.05 = 5% por n�vel)
    public double LevelPenaltyPerLevelPercent { get; set; } = 0.05;
    // Redu��o m�xima acumulada (0.8 = no m�ximo 80% de redu��o)
    public double LevelPenaltyMaxReductionPercent { get; set; } = 0.8;
    // Modo de rastreamento de n�vel: "Live" (n�vel do equipamento atual) ou "Max" (maior j� observado)
    public string LevelTrackingMode { get; set; } = "Live";

    // Assist: s� aceitar do mesmo cl� (default = true)
    public bool AssistClanOnlyEnabled { get; set; } = true;

    // Define the progression ladder
    public List<RankStep> Steps { get; set; } = new()
    {
        new("Osso", 30, false),
        new("Osso-Refor�ado", 60, true),
        new("Cobre", 30, false),
        new("Cobre-Impiedoso", 60, true),
        new("Ferro", 30, false),
        new("Ferro-Impiedoso", 60, true),
        new("Ouro-sol", 80, true),
        new("Prata-Escura", 125, true),
        new("Sangu�neo", int.MaxValue, false),
        new("Dr�cula", int.MaxValue, false),
    };

    // Mapeamento de grupos para ignorar subcategorias na diferen�a de elo
    // 0: Osso/Osso-Refor�ado, 1: Cobre/Cobre-Impiedoso, 2: Ferro/Ferro-Impiedoso, 3: Ouro-sol, 4: Prata-Escura, 5: Sangu�neo, 6: Dr�cula
    public List<int> GroupMap { get; set; } = new() { 0, 0, 1, 1, 2, 2, 3, 4, 5, 6 };
}
