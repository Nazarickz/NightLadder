namespace NightLadder.Core.Models;

public record RankStep(string Name, int ThresholdPoints, bool ResetsOnPromotion);

public class RankConfig
{
    public int SameTierKillPoints { get; set; } = 15;
    public int PerTierDifferenceBonus { get; set; } = 15;
    public int PerTierDifferencePenalty { get; set; } = 2;
    public int DraculaSlots { get; set; } = 4;

    // Anti-smurf/anti-farm de low level: penaliza ganhos quando killer tem nível muito acima da vítima
    public bool LevelPenaltyEnabled { get; set; } = true;
    // Se killerLevel - victimLevel > LevelGapThreshold, aplica penalidade
    public int LevelGapThreshold { get; set; } = 10;
    // Percentual de redução adicional por nível acima do threshold (0.05 = 5% por nível)
    public double LevelPenaltyPerLevelPercent { get; set; } = 0.05;
    // Redução máxima acumulada (0.8 = no máximo 80% de redução)
    public double LevelPenaltyMaxReductionPercent { get; set; } = 0.8;
    // Modo de rastreamento de nível: "Live" (nível do equipamento atual) ou "Max" (maior já observado)
    public string LevelTrackingMode { get; set; } = "Live";

    // Assist: só aceitar do mesmo clã (default = true)
    public bool AssistClanOnlyEnabled { get; set; } = true;

    // Define the progression ladder
    public List<RankStep> Steps { get; set; } = new()
    {
        new("Osso", 30, false),
        new("Osso-Reforçado", 60, true),
        new("Cobre", 30, false),
        new("Cobre-Impiedoso", 60, true),
        new("Ferro", 30, false),
        new("Ferro-Impiedoso", 60, true),
        new("Ouro-sol", 80, true),
        new("Prata-Escura", 125, true),
        new("Sanguíneo", int.MaxValue, false),
        new("Drácula", int.MaxValue, false),
    };

    // Mapeamento de grupos para ignorar subcategorias na diferença de elo
    // 0: Osso/Osso-Reforçado, 1: Cobre/Cobre-Impiedoso, 2: Ferro/Ferro-Impiedoso, 3: Ouro-sol, 4: Prata-Escura, 5: Sanguíneo, 6: Drácula
    public List<int> GroupMap { get; set; } = new() { 0, 0, 1, 1, 2, 2, 3, 4, 5, 6 };
}
