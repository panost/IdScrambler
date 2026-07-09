using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

namespace IdScrambler.Transforms;

/// <summary>
/// Data-dependent xorshift-right (PCG's "RXS" step):
/// y = x ^ (x >>> (baseShift + (x >>> (N − selectorBits)))).
/// The top <c>selectorBits</c> bits choose the shift amount. Because every possible shift is
/// at least <c>selectorBits</c>, those selector bits pass through unchanged, so the inverse
/// can read the shift amount back out of the output and undo the xorshift with the usual
/// doubling method. Requires baseShift ≥ selectorBits and
/// baseShift + 2^selectorBits − 1 ≤ N − 1.
/// </summary>
internal sealed class RxsBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly int _selectorBits;
    private readonly int _baseShift;
    private readonly int _bitWidth;

    public RxsBijection(int selectorBits, int baseShift)
    {
        _bitWidth = BitWidth.Of<T>();

        if (selectorBits < 1 || selectorBits > _bitWidth / 2)
            throw new ArgumentException(
                $"Selector bit count must be in [1, {_bitWidth / 2}]. Got: {selectorBits}.",
                nameof(selectorBits));
        if (baseShift < selectorBits)
            throw new ArgumentException(
                $"Base shift must be at least the selector bit count ({selectorBits}) so the selector bits " +
                $"survive the transform. Got: {baseShift}.",
                nameof(baseShift));
        int maxShift = baseShift + (1 << selectorBits) - 1;
        if (maxShift > _bitWidth - 1)
            throw new ArgumentException(
                $"Maximum shift {maxShift} (baseShift + 2^selectorBits − 1) exceeds {_bitWidth - 1}.",
                nameof(baseShift));

        _selectorBits = selectorBits;
        _baseShift = baseShift;
    }

    public int SelectorBits => _selectorBits;
    public int BaseShift => _baseShift;

    public T Forward(T value)
    {
        int shift = _baseShift + int.CreateTruncating(value >>> (_bitWidth - _selectorBits));
        return value ^ (value >>> shift);
    }

    public T Inverse(T value)
    {
        // The selector bits are unchanged by Forward, so the shift can be recovered from the output.
        int shift = _baseShift + int.CreateTruncating(value >>> (_bitWidth - _selectorBits));
        T x = value;
        for (int s = shift; s < _bitWidth; s <<= 1)
            x ^= x >>> s;
        return x;
    }

    public Expression BuildForwardExpression(Expression input)
    {
        // x ^ (x >>> (baseShift + (int)(x >>> (N − selectorBits))))
        var x = Expression.Variable(typeof(T), "x");
        var shift = Expression.Add(
            Expression.Constant(_baseShift),
            Expression.Convert(
                Expression.RightShift(x, Expression.Constant(_bitWidth - _selectorBits)),
                typeof(int)));
        return Expression.Block(
            [x],
            Expression.Assign(x, input),
            Expression.ExclusiveOr(x, Expression.RightShift(x, shift)));
    }

    public Expression BuildInverseExpression(Expression input)
    {
        // The doubling loop's trip count depends on the recovered shift, so emit a call
        // instead of unrolling.
        return Expression.Call(
            Expression.Constant(this),
            typeof(RxsBijection<T>).GetMethod(nameof(Inverse), BindingFlags.Public | BindingFlags.Instance)!,
            input);
    }
}
