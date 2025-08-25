using NightLadder.Core.Models;

namespace NightLadder.Core.Storage;

public interface IRankStorage
{
    Task<IDictionary<string, PlayerRank>> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IReadOnlyCollection<PlayerRank> ranks, CancellationToken ct = default);
}
