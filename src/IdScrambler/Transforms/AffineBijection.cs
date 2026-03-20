using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>Affine transform: y = (x * factor + offset) mod 2^N. Factor must be odd.</summary>
internal sealed class AffineBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly T _factor;
    private readonly T _offset;
    private readonly T _inverseFactor;

    public AffineBijection(T factor, T offset)
    {
        if (T.IsEvenInteger(factor))
            throw new ArgumentException("Affine factor must be odd (coprime to 2^N).", nameof(factor));

        _factor = factor;
        _offset = offset;
        _inverseFactor = ModularInverse.Compute(factor);
    }

    public T Factor => _factor;
    public T Offset => _offset;

    public T Forward(T value)
    {
        unchecked { return value * _factor + _offset; }
    }

    public T Inverse(T value)
    {
        unchecked { return (value - _offset) * _inverseFactor; }
    }

    public Expression BuildForwardExpression(Expression input)
    {
        // (input * factor) + offset
        return Expression.Add(
            Expression.Multiply(input, Expression.Constant(_factor)),
            Expression.Constant(_offset));
    }

    public Expression BuildInverseExpression(Expression input)
    {
        // (input - offset) * inverseFactor
        return Expression.Multiply(
            Expression.Subtract(input, Expression.Constant(_offset)),
            Expression.Constant(_inverseFactor));
    }


}
