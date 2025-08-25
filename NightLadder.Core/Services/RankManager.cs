using NightLadder.Core.Models;
using NightLadder.Core.Storage;
using System.Collections.Concurrent;

namespace NightLadder.Core.Services;

public class RankManager
{
    private readonly RankService _service;
    private readonly IRankStorage _storage;
    private readonly IPartialUpsertStorage? _partialStorage;
    private readonly ConcurrentDictionary<string, PlayerRank> _cache = new();

    public RankConfig Config { get; }

    public RankManager(RankConfig config, IRankStorage storage)
    {
        Config = config;
        _service = new RankService(config);
        _storage = storage;
        _partialStorage = storage as IPartialUpsertStorage;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var loaded = await _storage.LoadAsync(ct);
        foreach (var kv in loaded)
        {
            _cache[kv.Key] = kv.Value;
        }
    }

    public PlayerRank GetOrCreate(string playerId, string playerName)
    {
        return _cache.GetOrAdd(playerId, id => new PlayerRank { PlayerId = id, PlayerName = playerName });
    }

    // Resultado detalhado de uma operação de delta
    public record RankDeltaResult(bool Promoted, PlayerRank Rank, int AppliedDelta);

    // Resultado detalhado de uma kill
    public record KillResult(int KillerDelta, int VictimDelta, PlayerRank KillerRank, PlayerRank VictimRank, bool KillerPromoted, bool VictimPromoted);

    public KillResult RegisterKillDetailed(string killerId, string killerName, string victimId, string victimName, int? overrideKillerDelta = null)
    {
        var killer = GetOrCreate(killerId, killerName);
        var victim = GetOrCreate(victimId, victimName);

        var (kd, vd) = _service.ComputeKillDeltas(killer, victim);
        if (overrideKillerDelta.HasValue) kd = overrideKillerDelta.Value;

        _service.ApplyDelta(killer, kd, out var killerPromoted);
        _service.ApplyDelta(victim, vd, out var victimPromoted);

        _service.RebalanceDraculaSeats(_cache.Values);

        if (_partialStorage != null)
        {
            _ = _partialStorage.UpsertAsync(new[] { killer, victim });
        }

        return new KillResult(kd, vd, killer, victim, killerPromoted, victimPromoted);
    }

    public (int killerDelta, int victimDelta, PlayerRank killerRank, PlayerRank victimRank) RegisterKill(string killerId, string killerName, string victimId, string victimName, int? overrideKillerDelta = null)
    {
        var res = RegisterKillDetailed(killerId, killerName, victimId, victimName, overrideKillerDelta);
        return (res.KillerDelta, res.VictimDelta, res.KillerRank, res.VictimRank);
    }

    // Aplica um delta (p. ex., assistência) e persiste parcialmente
    public RankDeltaResult AwardAssistDetailed(string playerId, string playerName, int delta)
    {
        var pr = GetOrCreate(playerId, playerName);
        _service.ApplyDelta(pr, delta, out var promoted);
        if (_partialStorage != null)
        {
            _ = _partialStorage.UpsertAsync(new[] { pr });
        }
        return new RankDeltaResult(promoted, pr, delta);
    }

    public void AwardAssist(string playerId, string playerName, int delta)
    {
        _ = AwardAssistDetailed(playerId, playerName, delta);
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        await _storage.SaveAsync(_cache.Values.ToList(), ct);
    }

    public IEnumerable<PlayerRank> GetTop(int count = 50)
    {
        return _cache.Values
            .OrderByDescending(r => r.StepIndex)
            .ThenByDescending(r => r.Points)
            .Take(count);
    }

    // Novo: expõe todos os jogadores para resolução por nome
    public IEnumerable<PlayerRank> GetAll() => _cache.Values;

    // Admin helpers
    public void ApplyDelta(PlayerRank pr, int delta) => _service.ApplyDelta(pr, delta, out _);
    public (int killerDelta, int victimDelta) ComputeKillDeltas(PlayerRank killer, PlayerRank victim) => _service.ComputeKillDeltas(killer, victim);
    public void SetPoints(PlayerRank pr, int points)
    {
        pr.Points = points;
        pr.LastUpdatedUtc = DateTime.UtcNow;
    }
    public void SetStep(PlayerRank pr, int stepIndex)
    {
        pr.StepIndex = stepIndex < 0 ? 0 : stepIndex;
        pr.Points = 0;
        pr.LastUpdatedUtc = DateTime.UtcNow;
    }
    public void Reset(PlayerRank pr)
    {
        pr.StepIndex = 0;
        pr.Points = 0;
        pr.LastUpdatedUtc = DateTime.UtcNow;
    }
}
