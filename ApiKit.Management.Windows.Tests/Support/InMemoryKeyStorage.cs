using System.Collections.Concurrent;
using CryptoKit.Storage;

namespace ApiKit.Management.Windows.Tests.Support;

/// <summary>
/// Stores cryptographic test keys in memory.
/// </summary>
internal sealed class InMemoryKeyStorage : IKeyStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _records = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, byte[]> Snapshot => _records.ToDictionary(
        static x => x.Key, static x => x.Value.ToArray(), StringComparer.Ordinal);

    public ValueTask<byte[]?> TryLoadAsync(string keyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_records.TryGetValue(keyId, out var data) ? data.ToArray() : null);
    }

    public ValueTask<bool> CreateAsync(string keyId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_records.TryAdd(keyId, data.ToArray()));
    }

    public ValueTask ReplaceAsync(string keyId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (true)
        {
            if (!_records.TryGetValue(keyId, out var previous)) throw new KeyNotFoundException(keyId);
            if (_records.TryUpdate(keyId, data.ToArray(), previous)) return ValueTask.CompletedTask;
        }
    }

    public ValueTask DeleteAsync(string keyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _records.TryRemove(keyId, out _);
        return ValueTask.CompletedTask;
    }
}
