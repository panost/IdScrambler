using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>XOR with a constant key. Self-inverse.</summary>
internal sealed class XorBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly T _key;

    public XorBijection(T key) => _key = key;

    public T Key => _key;

    public T Forward(T value) => value ^ _key;
    public T Inverse(T value) => value ^ _key;

    public Expression BuildForwardExpression(Expression input)
        => Expression.ExclusiveOr(input, Expression.Constant(_key));

    public Expression BuildInverseExpression(Expression input)
        => Expression.ExclusiveOr(input, Expression.Constant(_key));
}
