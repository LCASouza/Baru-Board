using System.Security.Cryptography;

namespace BaruBoard.Core.Boards;

/// <summary>
/// Immutable binary payload referenced by elements. The identity is the SHA-256
/// of the content in lowercase hex, so equal content is always the same asset and
/// <c>SHA256(Data) == Id</c> holds for the whole lifetime of the instance.
/// </summary>
public sealed class BoardAsset
{
    private readonly byte[] _data;

    private BoardAsset(string id, string mediaType, byte[] data)
    {
        Id = id;
        MediaType = mediaType;
        _data = data;
    }

    public string Id { get; }

    public string MediaType { get; }

    public ReadOnlyMemory<byte> Data => _data;

    public Stream OpenRead() => new MemoryStream(_data, writable: false);

    public static BoardAsset Create(ReadOnlySpan<byte> data, string mediaType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);

        // The copy is what makes the identity stable: nobody outside can mutate it.
        var owned = data.ToArray();
        return new BoardAsset(ComputeId(owned), mediaType, owned);
    }

    public static string ComputeId(ReadOnlySpan<byte> data) =>
        Convert.ToHexStringLower(SHA256.HashData(data));

    public static bool IsValidId(string? id) =>
        id is { Length: 64 } && id.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}
