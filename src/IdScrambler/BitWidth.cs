using System.Numerics;
using System.Runtime.CompilerServices;

namespace IdScrambler;

/// <summary>
/// Provides bit-width and type-validation utilities for supported integer types.
/// Supported types: <see cref="ushort"/> (16), <see cref="uint"/> (32), <see cref="ulong"/> (64).
/// </summary>
internal static class BitWidth
{
    /// <summary>Returns the bit width for type T, or throws if T is unsupported.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Of<T>() where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        if (typeof(T) == typeof(ushort)) return 16;
        if (typeof(T) == typeof(uint)) return 32;
        if (typeof(T) == typeof(ulong)) return 64;
        throw new NotSupportedException(
            $"Unsupported type: {typeof(T).Name}. Supported types are ushort, uint, and ulong.");
    }
}
