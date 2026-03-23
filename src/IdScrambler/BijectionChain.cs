using System.Linq.Expressions;
using System.Numerics;
using System.Threading;
using IdScrambler.Transforms;

namespace IdScrambler;

/// <summary>
/// Composes multiple bijection steps into a single forward/inverse pipeline.
/// The inverse walks the chain in reverse order.
/// </summary>
public sealed class BijectionChain<T> : IBijection<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    private readonly ChainNode? _tail;
    private readonly int _count;
    private IBijectionStep<T>[]? _steps;
    private Func<T, T>? _compiledForward;
    private Func<T, T>? _compiledInverse;

    private BijectionChain()
    {
        _steps = [];
    }

    private BijectionChain(ChainNode tail, int count)
    {
        _tail = tail;
        _count = count;
    }

    /// <summary>Create a new empty chain.</summary>
    public static BijectionChain<T> Create() => new();

    /// <summary>The number of steps in the chain.</summary>
    public int Count => _count;

    internal IReadOnlyList<IBijectionStep<T>> Steps => GetSteps();

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
        return new BijectionChain<T>(new ChainNode(step, _tail), _count + 1);
    }

    /// <summary>Apply all forward steps 0 → N.</summary>
    public T Forward(T value)
    {
        var steps = GetSteps();
        T result = value;
        for (int i = 0; i < steps.Length; i++)
            result = steps[i].Forward(result);
        return result;
    }

    /// <summary>Apply all inverse steps N → 0.</summary>
    public T Inverse(T value)
    {
        var steps = GetSteps();
        T result = value;
        for (int i = steps.Length - 1; i >= 0; i--)
            result = steps[i].Inverse(result);
        return result;
    }

    /// <summary>Compile the forward chain into a single delegate with all steps inlined.</summary>
    public Func<T, T> CompileForward()
    {
        var compiled = _compiledForward;
        if (compiled != null)
            return compiled;

        var steps = GetSteps();
        var param = Expression.Parameter(typeof(T), "value");
        Expression body = param;
        for (int i = 0; i < steps.Length; i++)
            body = steps[i].BuildForwardExpression(body);

        compiled = Expression.Lambda<Func<T, T>>(body, param).Compile();
        Interlocked.CompareExchange(ref _compiledForward, compiled, null);
        return _compiledForward!;
    }

    /// <summary>Compile the inverse chain into a single delegate with all steps inlined.</summary>
    public Func<T, T> CompileInverse()
    {
        var compiled = _compiledInverse;
        if (compiled != null)
            return compiled;

        var steps = GetSteps();
        var param = Expression.Parameter(typeof(T), "value");
        Expression body = param;
        for (int i = steps.Length - 1; i >= 0; i--)
            body = steps[i].BuildInverseExpression(body);

        compiled = Expression.Lambda<Func<T, T>>(body, param).Compile();
        Interlocked.CompareExchange(ref _compiledInverse, compiled, null);
        return _compiledInverse!;
    }

    private IBijectionStep<T>[] GetSteps()
    {
        var steps = _steps;
        if (steps != null)
            return steps;

        steps = new IBijectionStep<T>[_count];
        var node = _tail;
        for (int i = _count - 1; i >= 0; i--)
        {
            steps[i] = node!.Step;
            node = node.Previous;
        }

        Interlocked.CompareExchange(ref _steps, steps, null);
        return _steps!;
    }

    private sealed class ChainNode
    {
        public ChainNode(IBijectionStep<T> step, ChainNode? previous)
        {
            Step = step;
            Previous = previous;
        }

        public IBijectionStep<T> Step { get; }

        public ChainNode? Previous { get; }
    }
}

// Usage: BijectionChain<uint>.Create() for 32-bit, BijectionChain<ulong>.Create() for 64-bit
