using NightLadder.Core.Models;

namespace NightLadder.Core.Storage;

// Optional capability for storages that can upsert a subset efficiently (e.g., LiteDB)
public interface IPartialUpsertStorage
{
    Task UpsertAsync(IReadOnlyCollection<PlayerRank> ranks, CancellationToken ct = default);
}
