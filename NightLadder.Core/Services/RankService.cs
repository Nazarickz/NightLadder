using NightLadder.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace NightLadder.Core.Services;

public class RankService
{
    internal readonly RankConfig _config;

    public RankService(RankConfig? config = null)
    {
        _config = config ?? new RankConfig();
    }

    // Calcula os deltas de pontos para um abate entre os dois jogadores atuais
    public (int deltaForKiller, int deltaForVictim) ComputeKillDeltas(PlayerRank killer, PlayerRank victim)
    {
        int killerGroup = GetGroupIndex(killer);
        int victimGroup = GetGroupIndex(victim);
        int groupDiff = killerGroup - victimGroup;

        int basePoints = _config.SameTierKillPoints;

        // Ganho do killer depende do diff de grupos
        int killerDelta = basePoints;
        if (groupDiff < 0)
        {
            killerDelta += Math.Abs(groupDiff) * _config.PerTierDifferenceBonus;
        }
        else if (groupDiff > 0)
        {
            killerDelta -= groupDiff * _config.PerTierDifferencePenalty;
            if (killerDelta < 1) killerDelta = 1;
        }

        // Perda da vítima agora espelha o ganho do killer (não valor fixo)
        int victimDelta = -killerDelta;
        return (killerDelta, victimDelta);
    }

    public void ApplyDelta(PlayerRank pr, int delta, out bool promoted)
    {
        promoted = false;
        if (delta == 0) return;
        pr.Points += delta;

        // Avança e carrega excedentes; retrocede respeitando pontos negativos
        while (true)
        {
            var step = _config.Steps[pr.StepIndex];

            // Topo: não passa do último, e não negativa
            if (pr.StepIndex == _config.Steps.Count - 1)
            {
                if (pr.Points < 0) pr.Points = 0;
                break;
            }

            // Promoção: carrega excedente (ResetsOnPromotion ignora reset; carrega excedente sempre)
            if (pr.Points >= step.ThresholdPoints)
            {
                pr.Points = pr.Points - step.ThresholdPoints; // carrega excedente integral
                pr.StepIndex++;
                promoted = true;
                continue;
            }

            // Rebaixamento: empresta do step anterior mantendo excedente negativo
            if (pr.Points < 0)
            {
                if (pr.StepIndex > 0)
                {
                    pr.StepIndex--;
                    var prev = _config.Steps[pr.StepIndex];
                    pr.Points = prev.ThresholdPoints + pr.Points; // pode continuar negativo e encadear múltiplos
                    continue;
                }
                // Step inicial: não abaixo de 0
                pr.Points = 0;
            }
            break;
        }

        pr.LastUpdatedUtc = DateTime.UtcNow;
    }

    public void RebalanceDraculaSeats(IEnumerable<PlayerRank> allPlayers)
    {
        int draculaIndex = _config.Steps.FindIndex(s => s.Name.StartsWith("Dr", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("r?cula"));
        int sanguineoIndex = _config.Steps.FindIndex(s => s.Name.StartsWith("Sang", StringComparison.OrdinalIgnoreCase));
        if (draculaIndex < 0 || sanguineoIndex < 0) return;

        var ordered = allPlayers
            .OrderByDescending(r => r.StepIndex)
            .ThenByDescending(r => r.Points)
            .ToList();

        var currentDraculas = ordered.Where(r => r.StepIndex >= draculaIndex).ToList();
        var sanguineos = ordered.Where(r => r.StepIndex == sanguineoIndex).ToList();

        // Promote top sanguíneos até preencher vagas
        while (currentDraculas.Count < _config.DraculaSlots)
        {
            var candidate = sanguineos.OrderByDescending(r => r.Points).FirstOrDefault();
            if (candidate == null) break;
            candidate.StepIndex = draculaIndex;
            currentDraculas.Add(candidate);
            sanguineos.Remove(candidate);
        }

        // Se exceder vagas, rebaixa os com menos pontos
        currentDraculas = allPlayers.Where(r => r.StepIndex >= draculaIndex).OrderByDescending(r => r.Points).ToList();
        if (currentDraculas.Count > _config.DraculaSlots)
        {
            foreach (var r in currentDraculas.Skip(_config.DraculaSlots))
            {
                r.StepIndex = sanguineoIndex;
            }
        }
    }

    private int GetGroupIndex(PlayerRank pr)
    {
        var idx = pr.StepIndex;
        if (idx < 0 || idx >= _config.GroupMap.Count) return idx;
        return _config.GroupMap[idx];
    }
}
