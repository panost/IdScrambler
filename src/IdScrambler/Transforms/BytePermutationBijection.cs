using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.InteropServices;

namespace IdScrambler.Transforms;

/// <summary>Permute the bytes of an integer.</summary>
internal sealed class BytePermutationBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly byte[] _permutation;
    private readonly byte[] _inversePermutation;
    private readonly int _byteCount;

    public BytePermutationBijection(byte[] permutation)
    {
        _byteCount = BitWidth.Of<T>() / 8;

        ArgumentNullException.ThrowIfNull(permutation);
        if (permutation.Length != _byteCount)
            throw new ArgumentException(
                $"Permutation must have exactly {_byteCount} elements. Got: {permutation.Length}.",
                nameof(permutation));

        // Validate it's a valid permutation of 0..byteCount-1
        var seen = new bool[_byteCount];
        for (int i = 0; i < _byteCount; i++)
        {
            if (permutation[i] >= _byteCount)
                throw new ArgumentException(
                    $"Permutation element at index {i} is {permutation[i]}, must be in [0, {_byteCount - 1}].",
                    nameof(permutation));
            if (seen[permutation[i]])
                throw new ArgumentException(
                    $"Duplicate value {permutation[i]} in permutation.",
                    nameof(permutation));
            seen[permutation[i]] = true;
        }

        _permutation = (byte[])permutation.Clone();
        _inversePermutation = new byte[_byteCount];
        for (int i = 0; i < _byteCount; i++)
            _inversePermutation[_permutation[i]] = (byte)i;
    }

    public byte[] Permutation => (byte[])_permutation.Clone();

    public T Forward(T value) => ApplyPermutation(value, _permutation);
    public T Inverse(T value) => ApplyPermutation(value, _inversePermutation);

    public Expression BuildForwardExpression(Expression input)
        => BuildPermutationExpression(input, _permutation);

    public Expression BuildInverseExpression(Expression input)
        => BuildPermutationExpression(input, _inversePermutation);

    private T ApplyPermutation(T value, byte[] perm)
    {
        if (typeof(T) == typeof(ushort))
        {
            ushort v = ushort.CreateTruncating(value);
            Span<byte> src = stackalloc byte[2];
            Span<byte> dst = stackalloc byte[2];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(src, v);
            for (int i = 0; i < 2; i++)
                dst[i] = src[perm[i]];
            return T.CreateTruncating(System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(dst));
        }
        else if (typeof(T) == typeof(uint))
        {
            uint v = uint.CreateTruncating(value);
            Span<byte> src = stackalloc byte[4];
            Span<byte> dst = stackalloc byte[4];
            MemoryMarshal.Write(src, in v);
            for (int i = 0; i < 4; i++)
                dst[i] = src[perm[i]];
            return T.CreateTruncating(MemoryMarshal.Read<uint>(dst));
        }
        else
        {
            ulong v = ulong.CreateTruncating(value);
            Span<byte> src = stackalloc byte[8];
            Span<byte> dst = stackalloc byte[8];
            MemoryMarshal.Write(src, in v);
            for (int i = 0; i < 8; i++)
                dst[i] = src[perm[i]];
            return T.CreateTruncating(MemoryMarshal.Read<ulong>(dst));
        }
    }

    private Expression BuildPermutationExpression(Expression input, byte[] perm)
    {
        // Build the permutation using shift/mask/or operations
        // Extract each byte, place it at the target position
        int bits = _byteCount * 8;
        Expression? result = null;

        for (int dst = 0; dst < _byteCount; dst++)
        {
            int src = perm[dst];
            int srcShift = src * 8;
            int dstShift = dst * 8;

            // Extract byte: (input >>> srcShift) & 0xFF, then shift to dstShift
            Expression byteExpr = Expression.And(
                srcShift == 0 ? input : Expression.RightShift(input, Expression.Constant(srcShift)),
                Expression.Constant(T.CreateTruncating(0xFF)));

            if (dstShift != 0)
                byteExpr = Expression.LeftShift(byteExpr, Expression.Constant(dstShift));

            result = result == null ? byteExpr : Expression.Or(result, byteExpr);
        }

        return result!;
    }
}
