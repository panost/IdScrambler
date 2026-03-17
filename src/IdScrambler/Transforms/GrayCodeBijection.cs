using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>Gray code encoding/decoding bijection.</summary>
internal sealed class GrayCodeBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly int _bitWidth;

    public GrayCodeBijection()
    {
        _bitWidth = typeof(T) == typeof(uint) ? 32 : 64;
    }

    /// <summary>Forward: binary to Gray code.</summary>
    public T Forward(T value) => value ^ (value >>> 1);

    /// <summary>Inverse: Gray code to binary (iterative decode).</summary>
    public T Inverse(T value)
    {
        T x = value;
        for (int shift = 1; shift < _bitWidth; shift <<= 1)
            x ^= (x >>> shift);
        return x;
    }

    public Expression BuildForwardExpression(Expression input)
    {
        // x ^ (x >>> 1)
        return Expression.ExclusiveOr(
            input,
            Expression.RightShift(input, Expression.Constant(1)));
    }

    public Expression BuildInverseExpression(Expression input)
    {
        // Unroll: x ^= x>>>1; x ^= x>>>2; x ^= x>>>4; x ^= x>>>8; x ^= x>>>16; (x ^= x>>>32 for 64-bit)
        var param = Expression.Variable(typeof(T), "x");
        var assignments = new List<Expression>
        {
            Expression.Assign(param, input)
        };

        for (int shift = 1; shift < _bitWidth; shift <<= 1)
        {
            assignments.Add(Expression.Assign(param,
                Expression.ExclusiveOr(param,
                    Expression.RightShift(param, Expression.Constant(shift)))));
        }

        assignments.Add(param);
        return Expression.Block(new[] { param }, assignments);
    }
}
