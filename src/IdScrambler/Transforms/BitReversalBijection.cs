using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>Reverse all bit positions. Self-inverse.</summary>
internal sealed class BitReversalBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly int _bitWidth;

    public BitReversalBijection()
    {
        _bitWidth = typeof(T) == typeof(uint) ? 32 : 64;
    }

    public T Forward(T value) => ReverseBits(value);
    public T Inverse(T value) => ReverseBits(value); // self-inverse

    public Expression BuildForwardExpression(Expression input) => BuildReverseBitsExpression(input);
    public Expression BuildInverseExpression(Expression input) => BuildReverseBitsExpression(input);

    private T ReverseBits(T value)
    {
        if (typeof(T) == typeof(uint))
        {
            uint v = uint.CreateTruncating(value);
            v = ReverseBits32(v);
            return T.CreateTruncating(v);
        }
        else
        {
            ulong v = ulong.CreateTruncating(value);
            v = ReverseBits64(v);
            return T.CreateTruncating(v);
        }
    }

    private static uint ReverseBits32(uint v)
    {
        v = ((v >> 1) & 0x55555555u) | ((v & 0x55555555u) << 1);
        v = ((v >> 2) & 0x33333333u) | ((v & 0x33333333u) << 2);
        v = ((v >> 4) & 0x0F0F0F0Fu) | ((v & 0x0F0F0F0Fu) << 4);
        v = ((v >> 8) & 0x00FF00FFu) | ((v & 0x00FF00FFu) << 8);
        v = (v >> 16) | (v << 16);
        return v;
    }

    private static ulong ReverseBits64(ulong v)
    {
        v = ((v >> 1) & 0x5555555555555555UL) | ((v & 0x5555555555555555UL) << 1);
        v = ((v >> 2) & 0x3333333333333333UL) | ((v & 0x3333333333333333UL) << 2);
        v = ((v >> 4) & 0x0F0F0F0F0F0F0F0FUL) | ((v & 0x0F0F0F0F0F0F0F0FUL) << 4);
        v = ((v >> 8) & 0x00FF00FF00FF00FFUL) | ((v & 0x00FF00FF00FF00FFUL) << 8);
        v = ((v >> 16) & 0x0000FFFF0000FFFFUL) | ((v & 0x0000FFFF0000FFFFUL) << 16);
        v = (v >> 32) | (v << 32);
        return v;
    }

    private Expression BuildReverseBitsExpression(Expression input)
    {
        // We capture `this` and call the method via a delegate approach.
        // For bit reversal, it's cleaner to use a method call expression.
        var method = typeof(BitReversalBijection<T>).GetMethod(
            nameof(ReverseBits),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return Expression.Call(Expression.Constant(this), method, input);
    }
}
