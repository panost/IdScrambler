using System.Linq.Expressions;
using System.Numerics;
using IdScrambler.Transforms;

namespace IdScrambler;

/// <summary>
/// Composes multiple bijection steps into a single forward/inverse pipeline.
/// The inverse walks the chain in reverse order.
/// </summary>
public sealed class BijectionChain<T> : IBijection<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly IReadOnlyList<IBijectionStep<T>> _steps;

    private BijectionChain()
    {
        _steps = Array.Empty<IBijectionStep<T>>();
    }

    private BijectionChain(IReadOnlyList<IBijectionStep<T>> steps)
    {
        _steps = steps;
    }

    /// <summary>Create a new empty chain.</summary>
    public static BijectionChain<T> Create() => new();

    /// <summary>The number of steps in the chain.</summary>
    public int Count => _steps.Count;

    internal IReadOnlyList<IBijectionStep<T>> Steps => _steps;

    /// <summary>XOR with a constant key.</summary>
    public BijectionChain<T> Xor(T key)
    {
        return Append(new XorBijection<T>(key));
    }

    /// <summary>Modular addition with a constant offset.</summary>
    public BijectionChain<T> Add(T offset)
    {
        return Append(new AddBijection<T>(offset));
    }

    /// <summary>Modular multiplication by an odd factor.</summary>
    public BijectionChain<T> Multiply(T oddFactor)
    {
        return Append(new MultiplyBijection<T>(oddFactor));
    }

    /// <summary>Circular bit rotation left by the given amount.</summary>
    public BijectionChain<T> RotateBits(int amount)
    {
        return Append(new RotateBitsBijection<T>(amount));
    }

    /// <summary>XOR-shift right.</summary>
    public BijectionChain<T> XorShiftRight(int shift)
    {
        return Append(new XorShiftBijection<T>(shift, XorShiftDirection.Right));
    }

    /// <summary>XOR-shift left.</summary>
    public BijectionChain<T> XorShiftLeft(int shift)
    {
        return Append(new XorShiftBijection<T>(shift, XorShiftDirection.Left));
    }

    /// <summary>Permute the bytes of the integer.</summary>
    public BijectionChain<T> PermuteBytes(byte[] permutation)
    {
        return Append(new BytePermutationBijection<T>(permutation));
    }

    /// <summary>Substitute each 4-bit nibble using an S-box.</summary>
    public BijectionChain<T> SubstituteNibbles(byte[] sbox)
    {
        return Append(new NibbleSubstitutionBijection<T>(sbox));
    }

    /// <summary>Reverse all bit positions.</summary>
    public BijectionChain<T> ReverseBits()
    {
        return Append(new BitReversalBijection<T>());
    }

    /// <summary>Apply Gray code encoding.</summary>
    public BijectionChain<T> GrayCode()
    {
        return Append(new GrayCodeBijection<T>());
    }

    /// <summary>Affine transform: y = (x * factor + offset) mod 2^N.</summary>
    public BijectionChain<T> Affine(T oddFactor, T offset)
    {
        return Append(new AffineBijection<T>(oddFactor, offset));
    }

    /// <summary>XOR low half into high half.</summary>
    public BijectionChain<T> XorHighLow()
    {
        return Append(new XorHighLowBijection<T>());
    }

    private BijectionChain<T> Append(IBijectionStep<T> step)
    {
        var steps = new IBijectionStep<T>[_steps.Count + 1];
        for (int i = 0; i < _steps.Count; i++)
            steps[i] = _steps[i];
        steps[^1] = step;
        return new BijectionChain<T>(steps);
    }

    /// <summary>Apply all forward steps 0 → N.</summary>
    public T Forward(T value)
    {
        T result = value;
        for (int i = 0; i < _steps.Count; i++)
            result = _steps[i].Forward(result);
        return result;
    }

    /// <summary>Apply all inverse steps N → 0.</summary>
    public T Inverse(T value)
    {
        T result = value;
        for (int i = _steps.Count - 1; i >= 0; i--)
            result = _steps[i].Inverse(result);
        return result;
    }

    /// <summary>Compile the forward chain into a single delegate with all steps inlined.</summary>
    public Func<T, T> CompileForward()
    {
        var param = Expression.Parameter(typeof(T), "value");
        Expression body = param;
        for (int i = 0; i < _steps.Count; i++)
            body = _steps[i].BuildForwardExpression(body);
        return Expression.Lambda<Func<T, T>>(body, param).Compile();
    }

    /// <summary>Compile the inverse chain into a single delegate with all steps inlined.</summary>
    public Func<T, T> CompileInverse()
    {
        var param = Expression.Parameter(typeof(T), "value");
        Expression body = param;
        for (int i = _steps.Count - 1; i >= 0; i--)
            body = _steps[i].BuildInverseExpression(body);
        return Expression.Lambda<Func<T, T>>(body, param).Compile();
    }
}

// Usage: BijectionChain<uint>.Create() for 32-bit, BijectionChain<ulong>.Create() for 64-bit
