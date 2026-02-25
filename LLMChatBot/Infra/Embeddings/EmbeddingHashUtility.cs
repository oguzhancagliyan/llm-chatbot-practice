using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Infra.Embeddings;

public static class EmbeddingHashUtility
{
    public static string ComputeStableHash(double[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(long)];
        var span = bytes.AsSpan();

        for (var i = 0; i < embedding.Length; i++)
        {
            var bits = BitConverter.DoubleToInt64Bits(embedding[i]);
            BinaryPrimitives.WriteInt64BigEndian(span.Slice(i * sizeof(long), sizeof(long)), bits);
        }

        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
