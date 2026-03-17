using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>XOR the low half of the integer into the high half. Self-inverse.</summary>
internal sealed class XorHighLowBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly int _halfBits;
    private readonly T _lowMask;

    public XorHighLowBijection()
    {
        if (typeof(T) == typeof(uint))
        {
            _halfBits = 16;
            _lowMask = T.CreateTruncating(0xFFFF);
        }
        else
        {
            _halfBits = 32;
            _lowMask = T.CreateTruncating(0xFFFFFFFF);
        }
    }

    public T Forward(T value) => value ^ ((value & _lowMask) << _halfBits);
    public T Inverse(T value) => value ^ ((value & _lowMask) << _halfBits); // self-inverse

    public Expression BuildForwardExpression(Expression input)
        => BuildExpression(input);

    public Expression BuildInverseExpression(Expression input)
        => BuildExpression(input);

    private Expression BuildExpression(Expression input)
    {
        // x ^ ((x & lowMask) << halfBits)
        return Expression.ExclusiveOr(
            input,
            Expression.LeftShift(
                Expression.And(input, Expression.Constant(_lowMask)),
                Expression.Constant(_halfBits)));
    }
}
