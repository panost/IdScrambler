using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>Modular multiplication by an odd factor. Factor must be coprime to 2^N.</summary>
internal sealed class MultiplyBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly T _factor;
    private readonly T _inverseFactor;

    public MultiplyBijection(T factor)
    {
        if (T.IsEvenInteger(factor))
            throw new ArgumentException("Multiplication factor must be odd (coprime to 2^N).", nameof(factor));

        _factor = factor;
        _inverseFactor = ComputeModularInverse(factor);
    }

    public T Factor => _factor;
    public T InverseFactor => _inverseFactor;

    public T Forward(T value)
    {
        unchecked { return value * _factor; }
    }

    public T Inverse(T value)
    {
        unchecked { return value * _inverseFactor; }
    }

    public Expression BuildForwardExpression(Expression input)
        => Expression.Multiply(input, Expression.Constant(_factor));

    public Expression BuildInverseExpression(Expression input)
        => Expression.Multiply(input, Expression.Constant(_inverseFactor));

    /// <summary>
    /// Compute modular multiplicative inverse mod 2^N using the extended Euclidean algorithm
    /// adapted for power-of-two moduli. Uses Newton's method: x = x * (2 - factor * x).
    /// </summary>
    private static T ComputeModularInverse(T factor)
    {
        // Newton's method for modular inverse mod 2^N:
        // Start with x = factor (odd, so last bit is 1)
        // Iterate: x = x * (2 - factor * x)
        // Each iteration doubles the number of correct bits.
        int bits = typeof(T) == typeof(uint) ? 32 : 64;
        T x = factor; // initial approximation — works because factor is odd
        T two = T.One + T.One;
        for (int i = 0; i < bits; i++)
        {
            unchecked
            {
                x = x * (two - factor * x);
            }
        }
        return x;
    }
}
