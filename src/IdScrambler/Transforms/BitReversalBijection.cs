using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>Reverse all bit positions. Self-inverse.</summary>
internal sealed class BitReversalBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    public BitReversalBijection()
    {
        // Validate the type is supported
        _ = BitWidth.Of<T>();
    }

    public T Forward(T value) => ReverseBits(value);
    public T Inverse(T value) => ReverseBits(value); // self-inverse

    public Expression BuildForwardExpression(Expression input) => BuildReverseBitsExpression(input);
    public Expression BuildInverseExpression(Expression input) => BuildReverseBitsExpression(input);

    private static T ReverseBits(T value)
    {
        if (typeof(T) == typeof(ushort))
        {
            uint v = ushort.CreateTruncating(value);
            v = ReverseBits32(v) >> 16;
            return T.CreateTruncating((ushort)v);
        }
        else if (typeof(T) == typeof(uint))
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

    private static Expression BuildReverseBitsExpression(Expression input)
    {
        int bits = BitWidth.Of<T>();

        if (bits == 16)
        {
            // For 16-bit: swap pairs, nibbles, then bytes
            var v = input;
            v = SwapBitsExpr(v, 1, T.CreateTruncating(0x5555));
            v = SwapBitsExpr(v, 2, T.CreateTruncating(0x3333));
            v = SwapBitsExpr(v, 4, T.CreateTruncating(0x0F0F));
            // swap bytes: (v >> 8) | (v << 8), masked to 16-bit
            v = Expression.And(
                Expression.Or(
                    Expression.RightShift(v, Expression.Constant(8)),
                    Expression.LeftShift(v, Expression.Constant(8))),
                Expression.Constant(T.CreateTruncating(0xFFFF)));
            return v;
        }
        else if (bits == 32)
        {
            var v = input;
            v = SwapBitsExpr(v, 1, T.CreateTruncating(0x55555555u));
            v = SwapBitsExpr(v, 2, T.CreateTruncating(0x33333333u));
            v = SwapBitsExpr(v, 4, T.CreateTruncating(0x0F0F0F0Fu));
            v = SwapBitsExpr(v, 8, T.CreateTruncating(0x00FF00FFu));
            v = Expression.Or(
                Expression.RightShift(v, Expression.Constant(16)),
                Expression.LeftShift(v, Expression.Constant(16)));
            return v;
        }
        else // 64-bit
        {
            var v = input;
            v = SwapBitsExpr(v, 1, T.CreateTruncating(0x5555555555555555UL));
            v = SwapBitsExpr(v, 2, T.CreateTruncating(0x3333333333333333UL));
            v = SwapBitsExpr(v, 4, T.CreateTruncating(0x0F0F0F0F0F0F0F0FUL));
            v = SwapBitsExpr(v, 8, T.CreateTruncating(0x00FF00FF00FF00FFUL));
            v = SwapBitsExpr(v, 16, T.CreateTruncating(0x0000FFFF0000FFFFUL));
            v = Expression.Or(
                Expression.RightShift(v, Expression.Constant(32)),
                Expression.LeftShift(v, Expression.Constant(32)));
            return v;
        }
    }

    /// <summary>Builds: ((v >> shift) &amp; mask) | ((v &amp; mask) &lt;&lt; shift)</summary>
    private static Expression SwapBitsExpr(Expression v, int shift, T mask)
    {
        var maskExpr = Expression.Constant(mask);
        var shiftExpr = Expression.Constant(shift);
        return Expression.Or(
            Expression.And(Expression.RightShift(v, shiftExpr), maskExpr),
            Expression.LeftShift(Expression.And(v, maskExpr), shiftExpr));
    }
}
