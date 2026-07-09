using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

namespace IdScrambler.Transforms;

/// <summary>
/// One CRC-32C (Castagnoli) step over the four bytes of a 32-bit value with seed 0.
/// This is a GF(2)-linear map y = M·x with M invertible (the CRC polynomial has a nonzero
/// constant term), hence a bijection over the full 32-bit domain — 32-bit chains only.
/// Forward uses the SSE4.2 / ARMv8 CRC32 instruction when available (software table otherwise);
/// the inverse applies the precomputed M⁻¹ via four 256-entry lookup tables.
/// </summary>
internal sealed class Crc32Bijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    public Crc32Bijection()
    {
        if (typeof(T) != typeof(uint))
            throw new NotSupportedException(
                $"Crc32 transform is only defined for 32-bit (uint) chains. Got: {typeof(T).Name}.");
    }

    public T Forward(T value)
        => T.CreateTruncating(Crc32Math.ForwardCore(uint.CreateTruncating(value)));

    public T Inverse(T value)
        => T.CreateTruncating(Crc32Math.InverseCore(uint.CreateTruncating(value)));

    public Expression BuildForwardExpression(Expression input)
        => Expression.Call(
            typeof(Crc32Math).GetMethod(nameof(Crc32Math.ForwardCore), BindingFlags.NonPublic | BindingFlags.Static)!,
            input);

    public Expression BuildInverseExpression(Expression input)
        => Expression.Call(
            typeof(Crc32Math).GetMethod(nameof(Crc32Math.InverseCore), BindingFlags.NonPublic | BindingFlags.Static)!,
            input);
}

/// <summary>CRC-32C single-step forward/inverse over 32-bit values (seed 0, no init/final XOR).</summary>
internal static class Crc32Math
{
    private const uint ReflectedPolynomial = 0x82F63B78; // CRC-32C (Castagnoli)

    private static readonly uint[] ForwardTable = BuildForwardTable();
    private static readonly uint[][] InverseTables = BuildInverseTables();

    internal static uint ForwardCore(uint value)
    {
        if (System.Runtime.Intrinsics.X86.Sse42.IsSupported)
            return System.Runtime.Intrinsics.X86.Sse42.Crc32(0u, value);
        if (System.Runtime.Intrinsics.Arm.Crc32.IsSupported)
            return System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32C(0u, value);
        return ForwardSoftware(value);
    }

    internal static uint InverseCore(uint value)
        => InverseTables[0][value & 0xFF]
         ^ InverseTables[1][(value >> 8) & 0xFF]
         ^ InverseTables[2][(value >> 16) & 0xFF]
         ^ InverseTables[3][value >> 24];

    internal static uint ForwardSoftware(uint value)
    {
        // Folding the seed-0 register through the four little-endian bytes of the value
        // is equivalent to four table rounds starting from the value itself.
        uint crc = value;
        for (int i = 0; i < 4; i++)
            crc = ForwardTable[crc & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] BuildForwardTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ ReflectedPolynomial : crc >> 1;
            table[i] = crc;
        }
        return table;
    }

    private static uint[][] BuildInverseTables()
    {
        // Columns of M: images of the basis vectors under the (linear, seed-0) forward map.
        var m = new uint[32];
        for (int i = 0; i < 32; i++)
            m[i] = ForwardSoftware(1u << i);

        // Gauss–Jordan over GF(2) with column operations; tracker starts as identity
        // and ends holding the columns of M⁻¹.
        var a = (uint[])m.Clone();
        var inverse = new uint[32];
        for (int i = 0; i < 32; i++)
            inverse[i] = 1u << i;

        for (int p = 0; p < 32; p++)
        {
            int pivot = p;
            while (pivot < 32 && (a[pivot] & (1u << p)) == 0)
                pivot++;
            if (pivot == 32)
                throw new InvalidOperationException("CRC-32C step matrix is singular."); // unreachable

            (a[p], a[pivot]) = (a[pivot], a[p]);
            (inverse[p], inverse[pivot]) = (inverse[pivot], inverse[p]);

            for (int q = 0; q < 32; q++)
            {
                if (q != p && (a[q] & (1u << p)) != 0)
                {
                    a[q] ^= a[p];
                    inverse[q] ^= inverse[p];
                }
            }
        }

        // Byte-sliced application tables: InverseTables[j][b] = M⁻¹ · (b << 8j).
        var tables = new uint[4][];
        for (int j = 0; j < 4; j++)
        {
            tables[j] = new uint[256];
            for (uint b = 1; b < 256; b++)
            {
                uint result = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((b & (1u << bit)) != 0)
                        result ^= inverse[j * 8 + bit];
                }
                tables[j][b] = result;
            }
        }
        return tables;
    }
}
