using System.Text.Json;
using NightLadder.Core.Models;

namespace NightLadder.Core.Storage;

public class JsonRankStorage : IRankStorage
{
    private readonly string _filePath;

    public JsonRankStorage(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<IDictionary<string, PlayerRank>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
            return new Dictionary<string, PlayerRank>();

        await using var fs = File.OpenRead(_filePath);
        var data = await JsonSerializer.DeserializeAsync<Dictionary<string, PlayerRank>>(fs, cancellationToken: ct) ?? new();
        return data;
    }

    public async Task SaveAsync(IReadOnlyCollection<PlayerRank> ranks, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var dict = ranks.ToDictionary(r => r.PlayerId, r => r);
        await using var fs = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(fs, dict, new JsonSerializerOptions { WriteIndented = true }, ct);
    }
}
