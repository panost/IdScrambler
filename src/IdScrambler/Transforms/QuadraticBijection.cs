using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

namespace IdScrambler.Transforms;

/// <summary>
/// Quadratic permutation: y = x · (2x + 1) mod 2^N (RC6's diffusion map).
/// Bijective because f(x) − f(y) = (x − y)(2(x + y) + 1) and the second factor is always odd.
/// The forward direction is two adds and a multiply; the inverse has no closed form and is
/// solved by Newton–Hensel lifting (the derivative 4x + 1 is always odd, so the lift never
/// fails and each iteration doubles the number of correct low bits).
/// </summary>
internal sealed class QuadraticBijection<T> : IBijectionStep<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly int _iterations;

    public QuadraticBijection()
    {
        // x₀ = y is correct mod 2, so log2(N) doublings reach all N bits.
        _iterations = BitOperations.Log2((uint)BitWidth.Of<T>());
    }

    public T Forward(T value)
    {
        unchecked { return value * (value + value + T.One); }
    }

    public T Inverse(T value)
    {
        unchecked
        {
            T x = value;
            for (int i = 0; i < _iterations; i++)
            {
                T fx = x * (x + x + T.One) - value;          // 2x² + x − y
                T derivative = (x << 2) + T.One;             // 4x + 1, always odd
                x -= fx * ModularInverse.Compute(derivative);
            }
            return x;
        }
    }

    public Expression BuildForwardExpression(Expression input)
    {
        // input * (input + input + 1), with input bound once
        var x = Expression.Variable(typeof(T), "x");
        return Expression.Block(
            [x],
            Expression.Assign(x, input),
            Expression.Multiply(x,
                Expression.Add(Expression.Add(x, x), Expression.Constant(T.One))));
    }

    public Expression BuildInverseExpression(Expression input)
    {
        // The Newton–Hensel loop (with its nested reciprocal) is emitted as a single
        // static call rather than unrolled inline.
        return Expression.Call(
            typeof(QuadraticBijection<T>).GetMethod(nameof(InverseCore), BindingFlags.NonPublic | BindingFlags.Static)!,
            input,
            Expression.Constant(_iterations));
    }

    private static T InverseCore(T value, int iterations)
    {
        unchecked
        {
            T x = value;
            for (int i = 0; i < iterations; i++)
            {
                T fx = x * (x + x + T.One) - value;
                T derivative = (x << 2) + T.One;
                x -= fx * ModularInverse.Compute(derivative);
            }
            return x;
        }
    }
}
