using System.Linq.Expressions;
using System.Numerics;

namespace IdScrambler.Transforms;

/// <summary>
/// XOR of two rotations: y = x ^ RotL(x, a) ^ RotL(x, b).
/// Over GF(2) this is multiplication by the trinomial 1 + X^a + X^b in GF(2)[X]/(X^N − 1);
/// for power-of-two N that ring reduces so the map is invertible iff the term count is odd,
/// which holds for any distinct a, b. Unlike XorShift, no bits fall off the ends, so every
/// output bit depends on three input bits (used by the rrmxmx/NASAM family of mixers).
/// </summary>
internal sealed class XorRotateBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly int _rotateA;
    private readonly int _rotateB;
    private readonly int _bitWidth;

    // Inverse = F^(N−1) = Π F^(2^k) for k = 0..log2(N)−1, because squaring is linear
    // over GF(2): F² = I + R^2a + R^2b. Amounts are precomputed mod N (0 means identity rotation).
    private readonly (int A, int B)[] _inverseSteps;

    public XorRotateBijection(int rotateA, int rotateB)
    {
        _bitWidth = BitWidth.Of<T>();

        if (rotateA < 1 || rotateA > _bitWidth - 1)
            throw new ArgumentException(
                $"Rotation amount must be in [1, {_bitWidth - 1}]. Got: {rotateA}.",
                nameof(rotateA));
        if (rotateB < 1 || rotateB > _bitWidth - 1)
            throw new ArgumentException(
                $"Rotation amount must be in [1, {_bitWidth - 1}]. Got: {rotateB}.",
                nameof(rotateB));
        if (rotateA == rotateB)
            throw new ArgumentException(
                $"Rotation amounts must be distinct. Got: {rotateA} and {rotateB}.",
                nameof(rotateB));

        _rotateA = rotateA;
        _rotateB = rotateB;

        int iterations = BitOperations.Log2((uint)_bitWidth);
        _inverseSteps = new (int, int)[iterations];
        for (int k = 0; k < iterations; k++)
            _inverseSteps[k] = ((rotateA << k) & (_bitWidth - 1), (rotateB << k) & (_bitWidth - 1));
    }

    public int RotateA => _rotateA;
    public int RotateB => _rotateB;

    public T Forward(T value)
        => value ^ RotateLeft(value, _rotateA) ^ RotateLeft(value, _rotateB);

    public T Inverse(T value)
    {
        T x = value;
        foreach (var (a, b) in _inverseSteps)
            x = x ^ RotateLeft(x, a) ^ RotateLeft(x, b);
        return x;
    }

    private T RotateLeft(T value, int amount)
        => amount == 0 ? value : (value << amount) | (value >>> (_bitWidth - amount));

    public Expression BuildForwardExpression(Expression input)
    {
        // Bind input to a variable so it is evaluated once, not three times.
        var x = Expression.Variable(typeof(T), "x");
        return Expression.Block(
            [x],
            Expression.Assign(x, input),
            BuildStepExpression(x, _rotateA, _rotateB));
    }

    public Expression BuildInverseExpression(Expression input)
    {
        var x = Expression.Variable(typeof(T), "x");
        var body = new List<Expression> { Expression.Assign(x, input) };
        foreach (var (a, b) in _inverseSteps)
            body.Add(Expression.Assign(x, BuildStepExpression(x, a, b)));
        body.Add(x);
        return Expression.Block([x], body);
    }

    private Expression BuildStepExpression(Expression x, int a, int b)
        => Expression.ExclusiveOr(
            Expression.ExclusiveOr(x, BuildRotateLeftExpression(x, a)),
            BuildRotateLeftExpression(x, b));

    private Expression BuildRotateLeftExpression(Expression x, int amount)
    {
        if (amount == 0)
            return x;

        return Expression.Or(
            Expression.LeftShift(x, Expression.Constant(amount)),
            Expression.RightShift(x, Expression.Constant(_bitWidth - amount)));
    }
}
