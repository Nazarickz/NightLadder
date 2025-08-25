using LiteDB;
using NightLadder.Core.Models;

namespace NightLadder.Core.Storage;

public class LiteDbRankStorage : IRankStorage, IPartialUpsertStorage
{
    private readonly string _dbPath;
    private readonly string? _importJsonPath;

    static LiteDbRankStorage()
    {
        // Configure LiteDB to use PlayerId as the document Id
        var mapper = BsonMapper.Global;
        mapper.Entity<PlayerRank>().Id(x => x.PlayerId);
    }

    public LiteDbRankStorage(string dbPath, string? importJsonPath = null)
    {
        _dbPath = dbPath;
        _importJsonPath = importJsonPath;
    }

    public async Task<IDictionary<string, PlayerRank>> LoadAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            EnsureDirectory();
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<PlayerRank>("ranks");
            col.EnsureIndex(x => x.PlayerId, true);
            col.EnsureIndex(x => x.StepIndex);
            col.EnsureIndex(x => x.Points);

            var all = col.FindAll().ToList();
            if (all.Count == 0 && !string.IsNullOrEmpty(_importJsonPath) && File.Exists(_importJsonPath))
            {
                try
                {
                    // One-time import from JSON for migration
                    var jsonStorage = new JsonRankStorage(_importJsonPath);
                    var data = jsonStorage.LoadAsync(ct).GetAwaiter().GetResult();
                    if (data.Count > 0)
                    {
                        col.Upsert(data.Values);
                        all = data.Values.ToList();
                        // Backup old JSON
                        try
                        {
                            var bak = _importJsonPath + ".bak";
                            if (File.Exists(bak)) File.Delete(bak);
                            File.Move(_importJsonPath, bak);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return all.ToDictionary(r => r.PlayerId, r => r);
        }, ct);
    }

    public async Task SaveAsync(IReadOnlyCollection<PlayerRank> ranks, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            EnsureDirectory();
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<PlayerRank>("ranks");
            col.EnsureIndex(x => x.PlayerId, true);
            col.EnsureIndex(x => x.StepIndex);
            col.EnsureIndex(x => x.Points);
            col.Upsert(ranks);
        }, ct);
    }

    public async Task UpsertAsync(IReadOnlyCollection<PlayerRank> ranks, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            EnsureDirectory();
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<PlayerRank>("ranks");
            col.Upsert(ranks);
        }, ct);
    }

    private void EnsureDirectory()
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }
}
