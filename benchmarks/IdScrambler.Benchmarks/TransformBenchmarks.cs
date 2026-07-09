using BenchmarkDotNet.Attributes;

namespace IdScrambler.Benchmarks;

/// <summary>
/// Per-transform latency of the compiled delegates (the hot path), measured as a
/// serially-dependent chain of 1024 applications so each call must wait for the previous
/// result. The "Identity" transform is an empty chain: pure delegate-call overhead.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class Transform32Benchmarks
{
    private Func<uint, uint> _forward = null!;
    private Func<uint, uint> _inverse = null!;

    [ParamsSource(nameof(Names))]
    public string Transform { get; set; } = "";

    public static IEnumerable<string> Names => TransformCatalog.Names32;

    [GlobalSetup]
    public void Setup()
    {
        var chain = TransformCatalog.Get32(Transform);
        _forward = chain.CompileForward();
        _inverse = chain.CompileInverse();
    }

    [Benchmark(OperationsPerInvoke = 1024)]
    public uint Forward()
    {
        uint v = 0xDEADBEEF;
        for (int i = 0; i < 1024; i++)
            v = _forward(v);
        return v;
    }

    [Benchmark(OperationsPerInvoke = 1024)]
    public uint Inverse()
    {
        uint v = 0xDEADBEEF;
        for (int i = 0; i < 1024; i++)
            v = _inverse(v);
        return v;
    }
}

[ShortRunJob]
[MemoryDiagnoser]
public class Transform64Benchmarks
{
    private Func<ulong, ulong> _forward = null!;
    private Func<ulong, ulong> _inverse = null!;

    [ParamsSource(nameof(Names))]
    public string Transform { get; set; } = "";

    public static IEnumerable<string> Names => TransformCatalog.Names64;

    [GlobalSetup]
    public void Setup()
    {
        var chain = TransformCatalog.Get64(Transform);
        _forward = chain.CompileForward();
        _inverse = chain.CompileInverse();
    }

    [Benchmark(OperationsPerInvoke = 1024)]
    public ulong Forward()
    {
        ulong v = 0xDEADBEEFCAFEBABE;
        for (int i = 0; i < 1024; i++)
            v = _forward(v);
        return v;
    }

    [Benchmark(OperationsPerInvoke = 1024)]
    public ulong Inverse()
    {
        ulong v = 0xDEADBEEFCAFEBABE;
        for (int i = 0; i < 1024; i++)
            v = _inverse(v);
        return v;
    }
}

/// <summary>Whole-chain latency for the shipped presets, compiled.</summary>
[ShortRunJob]
[MemoryDiagnoser]
public class PresetBenchmarks
{
    private readonly Func<uint, uint> _lightScramble32Fwd =
        ((BijectionChain<uint>)Presets.LightScramble32).CompileForward();
    private readonly Func<uint, uint> _lightScramble32Inv =
        ((BijectionChain<uint>)Presets.LightScramble32).CompileInverse();
    private readonly Func<uint, uint> _strongMix32Fwd =
        ((BijectionChain<uint>)Presets.StrongMix32).CompileForward();
    private readonly Func<uint, uint> _strongMix32Inv =
        ((BijectionChain<uint>)Presets.StrongMix32).CompileInverse();
    private readonly Func<ulong, ulong> _lightScramble64Fwd =
        ((BijectionChain<ulong>)Presets.LightScramble64).CompileForward();
    private readonly Func<ulong, ulong> _lightScramble64Inv =
        ((BijectionChain<ulong>)Presets.LightScramble64).CompileInverse();
    private readonly Func<ulong, ulong> _strongMix64Fwd =
        ((BijectionChain<ulong>)Presets.StrongMix64).CompileForward();
    private readonly Func<ulong, ulong> _strongMix64Inv =
        ((BijectionChain<ulong>)Presets.StrongMix64).CompileInverse();

    [Benchmark(OperationsPerInvoke = 1024)]
    public uint LightScramble32_Forward() => Run(_lightScramble32Fwd);

    [Benchmark(OperationsPerInvoke = 1024)]
    public uint LightScramble32_Inverse() => Run(_lightScramble32Inv);

    [Benchmark(OperationsPerInvoke = 1024)]
    public uint StrongMix32_Forward() => Run(_strongMix32Fwd);

    [Benchmark(OperationsPerInvoke = 1024)]
    public uint StrongMix32_Inverse() => Run(_strongMix32Inv);

    [Benchmark(OperationsPerInvoke = 1024)]
    public ulong LightScramble64_Forward() => Run(_lightScramble64Fwd);

    [Benchmark(OperationsPerInvoke = 1024)]
    public ulong LightScramble64_Inverse() => Run(_lightScramble64Inv);

    [Benchmark(OperationsPerInvoke = 1024)]
    public ulong StrongMix64_Forward() => Run(_strongMix64Fwd);

    [Benchmark(OperationsPerInvoke = 1024)]
    public ulong StrongMix64_Inverse() => Run(_strongMix64Inv);

    private static uint Run(Func<uint, uint> f)
    {
        uint v = 0xDEADBEEF;
        for (int i = 0; i < 1024; i++)
            v = f(v);
        return v;
    }

    private static ulong Run(Func<ulong, ulong> f)
    {
        ulong v = 0xDEADBEEFCAFEBABE;
        for (int i = 0; i < 1024; i++)
            v = f(v);
        return v;
    }
}
