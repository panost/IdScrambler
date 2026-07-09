using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace IdScrambler.Transforms;

/// <summary>
/// Carry-less (GF(2) polynomial) multiplication by an odd constant, truncated mod X^N.
/// The GF(2) analogue of <see cref="MultiplyBijection{T}"/>: invertible iff the constant
/// polynomial is odd (nonzero constant term). The inverse is a carry-less multiply by the
/// inverse polynomial, precomputed at construction via Newton iteration in GF(2)[X]
/// (x ← c·x², doubling correct terms), so forward and inverse have identical cost.
/// Uses the PCLMULQDQ instruction when available, with a software fallback.
/// </summary>
internal sealed class ClmulBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly T _factor;
    private readonly T _inverseFactor;

    public ClmulBijection(T factor)
    {
        if (T.IsEvenInteger(factor))
            throw new ArgumentException(
                "Carry-less multiplication factor must be odd (nonzero constant term in GF(2)[X]).",
                nameof(factor));

        _factor = factor;

        // Newton in GF(2)[X] mod X^N: x ← c·x², starting from x = 1 (correct mod X since c is odd).
        int iterations = BitOperations.Log2((uint)BitWidth.Of<T>());
        T inv = T.One;
        for (int i = 0; i < iterations; i++)
            inv = CarrylessMath.MulLow(factor, CarrylessMath.MulLow(inv, inv));
        _inverseFactor = inv;

        if (CarrylessMath.MulLow(factor, inv) != T.One)
            throw new InvalidOperationException("Carry-less inverse computation failed."); // unreachable
    }

    public T Factor => _factor;
    public T InverseFactor => _inverseFactor;

    public T Forward(T value) => CarrylessMath.MulLow(value, _factor);
    public T Inverse(T value) => CarrylessMath.MulLow(value, _inverseFactor);

    public Expression BuildForwardExpression(Expression input)
        => BuildMulExpression(input, _factor);

    public Expression BuildInverseExpression(Expression input)
        => BuildMulExpression(input, _inverseFactor);

    private static Expression BuildMulExpression(Expression input, T constant)
        => Expression.Call(
            typeof(CarrylessMath).GetMethod(nameof(CarrylessMath.MulLow), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(T)),
            input,
            Expression.Constant(constant));
}

/// <summary>Carry-less multiplication helpers (low N bits of the GF(2) product).</summary>
internal static class CarrylessMath
{
    internal static T MulLow<T>(T a, T b)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        if (typeof(T) == typeof(ushort))
            return T.CreateTruncating((ushort)MulLow64(ushort.CreateTruncating(a), ushort.CreateTruncating(b)));
        if (typeof(T) == typeof(uint))
            return T.CreateTruncating((uint)MulLow64(uint.CreateTruncating(a), uint.CreateTruncating(b)));
        return T.CreateTruncating(MulLow64(ulong.CreateTruncating(a), ulong.CreateTruncating(b)));
    }

    private static ulong MulLow64(ulong a, ulong b)
    {
        if (Pclmulqdq.IsSupported)
        {
            return Pclmulqdq.CarrylessMultiply(
                Vector128.CreateScalar(a),
                Vector128.CreateScalar(b),
                0x00).GetElement(0);
        }

        // Software fallback: XOR a shifted copy of a for each set bit of b.
        ulong acc = 0;
        while (b != 0)
        {
            acc ^= a << BitOperations.TrailingZeroCount(b);
            b &= b - 1;
        }
        return acc;
    }
}
