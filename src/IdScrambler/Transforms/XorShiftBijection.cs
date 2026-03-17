using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

internal enum XorShiftDirection { Right, Left }

/// <summary>XOR-shift bijection (right or left).</summary>
internal sealed class XorShiftBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly int _shift;
    private readonly XorShiftDirection _direction;
    private readonly int _bitWidth;

    public XorShiftBijection(int shift, XorShiftDirection direction)
    {
        _bitWidth = typeof(T) == typeof(uint) ? 32 : 64;

        if (shift < 1 || shift > _bitWidth - 1)
            throw new ArgumentException(
                $"XorShift amount must be in [1, {_bitWidth - 1}]. Got: {shift}.",
                nameof(shift));

        _shift = shift;
        _direction = direction;
    }

    public int Shift => _shift;
    public XorShiftDirection Direction => _direction;

    public T Forward(T value)
    {
        return _direction == XorShiftDirection.Right
            ? value ^ (value >>> _shift)
            : value ^ (value << _shift);
    }

    /// <summary>
    /// Inverse using the doubling method:
    /// For right: x ^= x>>>s; x ^= x>>>2s; x ^= x>>>4s; ...
    /// For left:  x ^= x<<s;  x ^= x<<2s;  x ^= x<<4s;  ...
    /// Each iteration doubles the number of correct bits.
    /// </summary>
    public T Inverse(T value)
    {
        T x = value;
        if (_direction == XorShiftDirection.Right)
        {
            for (int s = _shift; s < _bitWidth; s <<= 1)
                x ^= (x >>> s);
        }
        else
        {
            for (int s = _shift; s < _bitWidth; s <<= 1)
                x ^= (x << s);
        }
        return x;
    }

    public Expression BuildForwardExpression(Expression input)
    {
        var shiftExpr = _direction == XorShiftDirection.Right
            ? Expression.RightShift(input, Expression.Constant(_shift))
            : Expression.LeftShift(input, Expression.Constant(_shift));
        return Expression.ExclusiveOr(input, shiftExpr);
    }

    public Expression BuildInverseExpression(Expression input)
    {
        // Unroll the inverse loop at compile time using the doubling method
        var param = Expression.Variable(typeof(T), "x");
        var assignments = new List<Expression>
        {
            Expression.Assign(param, input)
        };

        for (int s = _shift; s < _bitWidth; s <<= 1)
        {
            var shiftExpr = _direction == XorShiftDirection.Right
                ? Expression.RightShift(param, Expression.Constant(s))
                : Expression.LeftShift(param, Expression.Constant(s));
            assignments.Add(Expression.Assign(param, Expression.ExclusiveOr(param, shiftExpr)));
        }

        assignments.Add(param); // final value
        return Expression.Block(new[] { param }, assignments);
    }
}
