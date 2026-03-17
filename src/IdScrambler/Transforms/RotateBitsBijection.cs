using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>Circular bit rotation by a fixed amount.</summary>
internal sealed class RotateBitsBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly int _amount;
    private readonly int _bitWidth;

    public RotateBitsBijection(int amount)
    {
        _bitWidth = typeof(T) == typeof(uint) ? 32 : 64;

        if (amount < 1 || amount > _bitWidth - 1)
            throw new ArgumentException(
                $"Rotation amount must be in [1, {_bitWidth - 1}]. Got: {amount}.",
                nameof(amount));

        _amount = amount;
    }

    public int Amount => _amount;

    public T Forward(T value) => RotateLeft(value, _amount);
    public T Inverse(T value) => RotateRight(value, _amount);

    public Expression BuildForwardExpression(Expression input)
        => BuildRotateLeftExpression(input, _amount);

    public Expression BuildInverseExpression(Expression input)
        => BuildRotateRightExpression(input, _amount);

    private T RotateLeft(T value, int amount)
        => (value << amount) | (value >>> (_bitWidth - amount));

    private T RotateRight(T value, int amount)
        => (value >>> amount) | (value << (_bitWidth - amount));

    private Expression BuildRotateLeftExpression(Expression input, int amount)
    {
        // (input << amount) | (input >>> (bitWidth - amount))
        var left = Expression.LeftShift(input, Expression.Constant(amount));
        var right = Expression.RightShift(input, Expression.Constant(_bitWidth - amount));
        return Expression.Or(left, right);
    }

    private Expression BuildRotateRightExpression(Expression input, int amount)
    {
        // (input >>> amount) | (input << (bitWidth - amount))
        var right = Expression.RightShift(input, Expression.Constant(amount));
        var left = Expression.LeftShift(input, Expression.Constant(_bitWidth - amount));
        return Expression.Or(right, left);
    }
}
