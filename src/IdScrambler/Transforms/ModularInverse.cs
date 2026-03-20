using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>
/// Computes the modular multiplicative inverse mod 2^N using Newton's method.
/// x_{n+1} = x_n * (2 - factor * x_n), doubling correct bits each iteration.
/// </summary>
internal static class ModularInverse
{
    /// <summary>
    /// Compute the modular multiplicative inverse of <paramref name="factor"/> mod 2^N.
    /// The factor must be odd (coprime to 2^N).
    /// </summary>
    public static T Compute<T>(T factor) where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        int bits = BitWidth.Of<T>();
        // Newton's method: each iteration doubles the number of correct low bits.
        // We need ⌈log₂(bits)⌉ iterations to converge for N-bit modulus.
        int iterations = bits switch
        {
            16 => 4,  // ceil(log2(16))
            32 => 5,  // ceil(log2(32))
            64 => 6,  // ceil(log2(64))
            _ => bits  // conservative fallback
        };

        T x = factor; // initial approximation — works because factor is odd
        T two = T.One + T.One;
        for (int i = 0; i < iterations; i++)
        {
            unchecked
            {
                x = x * (two - factor * x);
            }
        }
        return x;
    }
}
