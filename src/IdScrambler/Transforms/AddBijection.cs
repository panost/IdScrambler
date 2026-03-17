using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>Modular addition with a constant offset.</summary>
internal sealed class AddBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly T _offset;

    public AddBijection(T offset) => _offset = offset;

    public T Offset => _offset;

    // Wrapping add/subtract via unchecked operator on T
    public T Forward(T value)
    {
        unchecked
        {
            return value + _offset;
        }
    }

    public T Inverse(T value)
    {
        unchecked
        {
            return value - _offset;
        }
    }

    public Expression BuildForwardExpression(Expression input)
        => Expression.Add(input, Expression.Constant(_offset));

    public Expression BuildInverseExpression(Expression input)
        => Expression.Subtract(input, Expression.Constant(_offset));
}
