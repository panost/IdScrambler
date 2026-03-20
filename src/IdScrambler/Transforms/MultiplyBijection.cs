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
        _inverseFactor = ModularInverse.Compute(factor);
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


}
