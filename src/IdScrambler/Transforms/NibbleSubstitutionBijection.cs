using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>Nibble substitution (4-bit S-box) bijection.</summary>
internal sealed class NibbleSubstitutionBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly byte[] _sbox;
    private readonly byte[] _inverseSbox;
    private readonly int _nibbleCount;

    /// <summary>A built-in 4-bit S-box with good non-linearity (DES S-box 5, first row).</summary>
    public static readonly byte[] DefaultSBox = [2, 12, 4, 1, 7, 10, 11, 6, 8, 5, 3, 15, 13, 0, 14, 9];

    public NibbleSubstitutionBijection(byte[] sbox)
    {
        ArgumentNullException.ThrowIfNull(sbox);
        if (sbox.Length != 16)
            throw new ArgumentException("S-box must have exactly 16 elements.", nameof(sbox));

        // Validate it's a valid permutation of 0..15
        var seen = new bool[16];
        for (int i = 0; i < 16; i++)
        {
            if (sbox[i] > 15)
                throw new ArgumentException(
                    $"S-box element at index {i} is {sbox[i]}, must be in [0, 15].",
                    nameof(sbox));
            if (seen[sbox[i]])
                throw new ArgumentException(
                    $"Duplicate value {sbox[i]} in S-box.",
                    nameof(sbox));
            seen[sbox[i]] = true;
        }

        _sbox = (byte[])sbox.Clone();
        _inverseSbox = new byte[16];
        for (int i = 0; i < 16; i++)
            _inverseSbox[_sbox[i]] = (byte)i;

        _nibbleCount = typeof(T) == typeof(uint) ? 8 : 16;
    }

    public byte[] SBox => (byte[])_sbox.Clone();

    public T Forward(T value) => ApplySbox(value, _sbox);
    public T Inverse(T value) => ApplySbox(value, _inverseSbox);

    public Expression BuildForwardExpression(Expression input)
        => BuildSboxExpression(input, _sbox);

    public Expression BuildInverseExpression(Expression input)
        => BuildSboxExpression(input, _inverseSbox);

    private T ApplySbox(T value, byte[] box)
    {
        T result = T.Zero;
        T mask = T.CreateTruncating(0xF);
        for (int i = 0; i < _nibbleCount; i++)
        {
            int nibble = int.CreateTruncating((value >>> (i * 4)) & mask);
            result |= T.CreateTruncating(box[nibble]) << (i * 4);
        }
        return result;
    }

    private Expression BuildSboxExpression(Expression input, byte[] box)
    {
        // For expression compilation, we build a lookup using a series of
        // conditional expressions (switch-like) for each nibble.
        // This is complex, so we use a captured array lookup approach.
        var boxCopy = (byte[])box.Clone();
        var param = Expression.Variable(typeof(T), "result");
        var assignments = new List<Expression>
        {
            Expression.Assign(param, Expression.Constant(T.Zero))
        };

        var maskConst = Expression.Constant(T.CreateTruncating(0xF));
        var boxArray = Expression.Constant(boxCopy);

        for (int i = 0; i < _nibbleCount; i++)
        {
            int shift = i * 4;
            // nibble = (int)((input >>> shift) & 0xF)
            Expression nibbleExpr = Expression.And(
                shift == 0 ? input : Expression.RightShift(input, Expression.Constant(shift)),
                maskConst);
            var nibbleInt = Expression.Convert(nibbleExpr, typeof(int));

            // box[nibble]
            var lookup = Expression.ArrayIndex(boxArray, nibbleInt);
            // T.CreateTruncating(box[nibble])
            var lookupT = Expression.Convert(lookup, typeof(T));

            // lookupT << shift
            Expression shifted = shift == 0 ? lookupT : Expression.LeftShift(lookupT, Expression.Constant(shift));
            assignments.Add(Expression.Assign(param, Expression.Or(param, shifted)));
        }

        assignments.Add(param);
        return Expression.Block(new[] { param }, assignments);
    }
}
