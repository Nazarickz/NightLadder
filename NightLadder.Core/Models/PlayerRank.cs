namespace NightLadder.Core.Models;

public class PlayerRank
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;

    // Index within RankConfig.Steps
    public int StepIndex { get; set; } = 0;

    // Points inside current step
    public int Points { get; set; } = 0;

    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    public string StepName => StepIndex >= 0 && StepIndex < Steps.Count ? Steps[StepIndex].Name : "Unknown";

    private static IReadOnlyList<RankStep> Steps => _steps ??= new RankConfig().Steps;
    private static IReadOnlyList<RankStep>? _steps;
}
