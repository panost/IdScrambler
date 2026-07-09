using IdScrambler;
using IdScrambler.Serialization;

namespace IdScrambler.Tests;

/// <summary>
/// Tests for the XorRotate, Quadratic, Clmul, Crc32 and Rxs transforms:
/// round-trips, exhaustive 16-bit bijectivity, compiled/interpreted equivalence,
/// parameter validation, and serialization.
/// </summary>
public class NewTransformTests
{
    private static readonly uint[] TestValues32 =
    [
        0, 1, 2, 42, 255, 256, 0x7FFFFFFF, 0x80000000, 0xDEADBEEF, uint.MaxValue,
        0x12345678, 0x00010000, 0xFFFF0000, 0x0000FFFF, 0xAAAAAAAA, 0x55555555
    ];

    private static readonly ulong[] TestValues64 =
    [
        0, 1, 2, 42, 255, 256, 0x7FFFFFFFFFFFFFFF, 0x8000000000000000,
        0xDEADBEEFCAFEBABE, ulong.MaxValue, 0x123456789ABCDEF0,
        0xAAAAAAAAAAAAAAAA, 0x5555555555555555, 0x00000000FFFFFFFF
    ];

    private static void AssertRoundTrip32(IBijection<uint> b)
    {
        foreach (var x in TestValues32)
        {
            Assert.Equal(x, b.Inverse(b.Forward(x)));
            Assert.Equal(x, b.Forward(b.Inverse(x)));
        }

        var rng = new Random(2026);
        for (int i = 0; i < 100_000; i++)
        {
            uint x = (uint)rng.Next() ^ ((uint)rng.Next() << 16);
            Assert.Equal(x, b.Inverse(b.Forward(x)));
        }
    }

    private static void AssertRoundTrip64(IBijection<ulong> b)
    {
        foreach (var x in TestValues64)
        {
            Assert.Equal(x, b.Inverse(b.Forward(x)));
            Assert.Equal(x, b.Forward(b.Inverse(x)));
        }

        var rng = new Random(2026);
        for (int i = 0; i < 100_000; i++)
        {
            ulong x = ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next();
            Assert.Equal(x, b.Inverse(b.Forward(x)));
        }
    }

    /// <summary>Exhaustive bijectivity over the full 2^16 domain.</summary>
    private static void AssertBijective16(BijectionChain<ushort> b)
    {
        var seen = new bool[65536];
        for (int x = 0; x <= ushort.MaxValue; x++)
        {
            ushort y = b.Forward((ushort)x);
            Assert.False(seen[y], $"Collision at input {x}: output {y} already produced.");
            seen[y] = true;
            Assert.Equal((ushort)x, b.Inverse(y));
        }
    }

    private static void AssertCompiledMatches32(BijectionChain<uint> chain)
    {
        var forward = chain.CompileForward();
        var inverse = chain.CompileInverse();
        var rng = new Random(99);
        foreach (var x in TestValues32)
        {
            Assert.Equal(chain.Forward(x), forward(x));
            Assert.Equal(chain.Inverse(x), inverse(x));
        }
        for (int i = 0; i < 10_000; i++)
        {
            uint x = (uint)rng.Next() ^ ((uint)rng.Next() << 16);
            Assert.Equal(chain.Forward(x), forward(x));
            Assert.Equal(chain.Inverse(x), inverse(x));
            Assert.Equal(x, inverse(forward(x)));
        }
    }

    private static void AssertCompiledMatches64(BijectionChain<ulong> chain)
    {
        var forward = chain.CompileForward();
        var inverse = chain.CompileInverse();
        var rng = new Random(99);
        foreach (var x in TestValues64)
        {
            Assert.Equal(chain.Forward(x), forward(x));
            Assert.Equal(chain.Inverse(x), inverse(x));
        }
        for (int i = 0; i < 10_000; i++)
        {
            ulong x = ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next();
            Assert.Equal(chain.Forward(x), forward(x));
            Assert.Equal(chain.Inverse(x), inverse(x));
            Assert.Equal(x, inverse(forward(x)));
        }
    }

    // ---- XorRotate ----

    [Fact]
    public void XorRotate32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().XorRotate(13, 27));

    [Fact]
    public void XorRotate64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().XorRotate(25, 50));

    [Fact]
    public void XorRotate16_AllPairs_ExhaustivelyBijective()
    {
        for (int a = 1; a < 16; a++)
            for (int b = a + 1; b < 16; b++)
                AssertBijective16(BijectionChain<ushort>.Create().XorRotate(a, b));
    }

    [Fact]
    public void XorRotate32_Compiled_MatchesInterpreted() =>
        AssertCompiledMatches32(BijectionChain<uint>.Create().XorRotate(7, 19));

    [Fact]
    public void XorRotate64_Compiled_MatchesInterpreted() =>
        AssertCompiledMatches64(BijectionChain<ulong>.Create().XorRotate(24, 49));

    [Fact]
    public void XorRotate_EqualAmounts_Throws() =>
        Assert.Throws<ArgumentException>(() => BijectionChain<uint>.Create().XorRotate(7, 7));

    [Fact]
    public void XorRotate_ZeroAmount_Throws() =>
        Assert.Throws<ArgumentException>(() => BijectionChain<uint>.Create().XorRotate(0, 7));

    [Fact]
    public void XorRotate_AmountEqualWidth_Throws() =>
        Assert.Throws<ArgumentException>(() => BijectionChain<uint>.Create().XorRotate(7, 32));

    // ---- Quadratic ----

    [Fact]
    public void Quadratic32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().Quadratic());

    [Fact]
    public void Quadratic64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().Quadratic());

    [Fact]
    public void Quadratic16_ExhaustivelyBijective() =>
        AssertBijective16(BijectionChain<ushort>.Create().Quadratic());

    [Fact]
    public void Quadratic32_Compiled_MatchesInterpreted() =>
        AssertCompiledMatches32(BijectionChain<uint>.Create().Quadratic());

    [Fact]
    public void Quadratic64_Compiled_MatchesInterpreted() =>
        AssertCompiledMatches64(BijectionChain<ulong>.Create().Quadratic());

    [Fact]
    public void Quadratic_KnownValues()
    {
        var b = BijectionChain<uint>.Create().Quadratic();
        Assert.Equal(0u, b.Forward(0));      // 0 * 1
        Assert.Equal(3u, b.Forward(1));      // 1 * 3
        Assert.Equal(10u, b.Forward(2));     // 2 * 5
        Assert.Equal(3570u, b.Forward(42));  // 42 * 85
    }

    // ---- Clmul ----

    [Fact]
    public void Clmul32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().Clmul(0x9E3779B9));

    [Fact]
    public void Clmul64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().Clmul(0x9E3779B97F4A7C15));

    [Fact]
    public void Clmul16_ExhaustivelyBijective() =>
        AssertBijective16(BijectionChain<ushort>.Create().Clmul(0x9E37));

    [Fact]
    public void Clmul32_Compiled_MatchesInterpreted() =>
        AssertCompiledMatches32(BijectionChain<uint>.Create().Clmul(0x45D9F3B1));

    [Fact]
    public void Clmul64_Compiled_MatchesInterpreted() =>
        AssertCompiledMatches64(BijectionChain<ulong>.Create().Clmul(0xBF58476D1CE4E5B9));

    [Fact]
    public void Clmul_EvenFactor_Throws() =>
        Assert.Throws<ArgumentException>(() => BijectionChain<uint>.Create().Clmul(2));

    [Fact]
    public void Clmul_ByOne_IsIdentity()
    {
        var b = BijectionChain<uint>.Create().Clmul(1);
        Assert.Equal(12345u, b.Forward(12345));
        Assert.Equal(12345u, b.Inverse(12345));
    }

    [Fact]
    public void Clmul_MatchesBitwiseReference()
    {
        // Independent bit-by-bit carry-less product to cross-check hardware/software paths.
        static uint Reference(uint a, uint b)
        {
            uint acc = 0;
            for (int i = 0; i < 32; i++)
                if ((b & (1u << i)) != 0)
                    acc ^= a << i;
            return acc;
        }

        var chain = BijectionChain<uint>.Create().Clmul(0x9E3779B9);
        var rng = new Random(7);
        for (int i = 0; i < 10_000; i++)
        {
            uint x = (uint)rng.Next() ^ ((uint)rng.Next() << 16);
            Assert.Equal(Reference(x, 0x9E3779B9), chain.Forward(x));
        }
    }

    // ---- Crc32 ----

    [Fact]
    public void Crc32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().Crc32());

    [Fact]
    public void Crc32_Compiled_MatchesInterpreted() =>
        AssertCompiledMatches32(BijectionChain<uint>.Create().Crc32());

    [Fact]
    public void Crc32_On64BitChain_Throws() =>
        Assert.Throws<NotSupportedException>(() => BijectionChain<ulong>.Create().Crc32());

    [Fact]
    public void Crc32_On16BitChain_Throws() =>
        Assert.Throws<NotSupportedException>(() => BijectionChain<ushort>.Create().Crc32());

    [Fact]
    public void Crc32_IsLinearOverGf2()
    {
        // Seed-0 CRC step must satisfy f(x ^ y) == f(x) ^ f(y) and f(0) == 0.
        var b = BijectionChain<uint>.Create().Crc32();
        Assert.Equal(0u, b.Forward(0));
        var rng = new Random(11);
        for (int i = 0; i < 1000; i++)
        {
            uint x = (uint)rng.Next() ^ ((uint)rng.Next() << 16);
            uint y = (uint)rng.Next() ^ ((uint)rng.Next() << 16);
            Assert.Equal(b.Forward(x) ^ b.Forward(y), b.Forward(x ^ y));
        }
    }

    // ---- Rxs ----

    [Fact]
    public void Rxs32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().Rxs(4, 4));

    [Fact]
    public void Rxs64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().Rxs(5, 5));

    [Fact]
    public void Rxs16_ExhaustivelyBijective()
    {
        AssertBijective16(BijectionChain<ushort>.Create().Rxs(3, 3));
        AssertBijective16(BijectionChain<ushort>.Create().Rxs(2, 8));
    }

    [Fact]
    public void Rxs32_Compiled_MatchesInterpreted() =>
        AssertCompiledMatches32(BijectionChain<uint>.Create().Rxs(4, 4));

    [Fact]
    public void Rxs64_Compiled_MatchesInterpreted() =>
        AssertCompiledMatches64(BijectionChain<ulong>.Create().Rxs(4, 6));

    [Fact]
    public void Rxs_BaseShiftBelowSelectorBits_Throws() =>
        Assert.Throws<ArgumentException>(() => BijectionChain<uint>.Create().Rxs(4, 3));

    [Fact]
    public void Rxs_MaxShiftTooLarge_Throws() =>
        Assert.Throws<ArgumentException>(() => BijectionChain<uint>.Create().Rxs(4, 17)); // 17 + 15 > 31

    [Fact]
    public void Rxs_ZeroSelectorBits_Throws() =>
        Assert.Throws<ArgumentException>(() => BijectionChain<uint>.Create().Rxs(0, 4));

    // ---- Chains mixing new and old transforms ----

    [Fact]
    public void MixedChain32_RoundTrip_InterpretedAndCompiled()
    {
        var chain = BijectionChain<uint>.Create()
            .XorRotate(13, 27)
            .Multiply(0x9E3779B9)
            .Quadratic()
            .Rxs(4, 4)
            .Clmul(0x45D9F3B1)
            .Crc32()
            .Xor(0xDEADBEEF);

        AssertRoundTrip32(chain);
        AssertCompiledMatches32(chain);
    }

    [Fact]
    public void MixedChain64_RoundTrip_InterpretedAndCompiled()
    {
        var chain = BijectionChain<ulong>.Create()
            .XorRotate(25, 50)
            .Multiply(0xBF58476D1CE4E5B9)
            .Quadratic()
            .Rxs(5, 5)
            .Clmul(0x9E3779B97F4A7C15);

        AssertRoundTrip64(chain);
        AssertCompiledMatches64(chain);
    }

    // ---- Serialization ----

    [Fact]
    public void JsonSerialization_RoundTrip_NewTransforms32()
    {
        var chain = BijectionChain<uint>.Create()
            .XorRotate(13, 27)
            .Quadratic()
            .Clmul(0x9E3779B9)
            .Crc32()
            .Rxs(4, 4);

        string json = BijectionSerializer.ToJson<uint>(chain);
        var restored = BijectionSerializer.FromJson<uint>(json);

        for (uint x = 0; x < 500; x++)
        {
            Assert.Equal(chain.Forward(x), restored.Forward(x));
            Assert.Equal(chain.Inverse(x), restored.Inverse(x));
        }
    }

    [Fact]
    public void XmlSerialization_RoundTrip_NewTransforms32()
    {
        var chain = BijectionChain<uint>.Create()
            .XorRotate(13, 27)
            .Quadratic()
            .Clmul(0x9E3779B9)
            .Crc32()
            .Rxs(4, 4);

        string xml = BijectionSerializer.ToXml<uint>(chain);
        var restored = BijectionSerializer.FromXml<uint>(xml);

        for (uint x = 0; x < 500; x++)
        {
            Assert.Equal(chain.Forward(x), restored.Forward(x));
            Assert.Equal(chain.Inverse(x), restored.Inverse(x));
        }
    }

    [Fact]
    public void JsonSerialization_RoundTrip_NewTransforms64()
    {
        var chain = BijectionChain<ulong>.Create()
            .XorRotate(25, 50)
            .Quadratic()
            .Clmul(0x9E3779B97F4A7C15)
            .Rxs(5, 5);

        string json = BijectionSerializer.ToJson<ulong>(chain);
        var restored = BijectionSerializer.FromJson<ulong>(json);

        var rng = new Random(42);
        for (int i = 0; i < 500; i++)
        {
            ulong x = ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next();
            Assert.Equal(chain.Forward(x), restored.Forward(x));
            Assert.Equal(chain.Inverse(x), restored.Inverse(x));
        }
    }

    [Fact]
    public void JsonDeserialization_NewTransforms_FromSpecExample()
    {
        var json = """
        {
          "width": 32,
          "steps": [
            { "type": "XorRotate", "a": 13, "b": 27 },
            { "type": "Quadratic" },
            { "type": "Clmul", "factor": "0x9E3779B9" },
            { "type": "Crc32" },
            { "type": "Rxs", "selectorBits": 4, "baseShift": 4 }
          ]
        }
        """;

        var chain = BijectionSerializer.FromJson<uint>(json);
        uint encoded = chain.Forward(42);
        Assert.Equal(42u, chain.Inverse(encoded));
    }

    [Fact]
    public void JsonDeserialization_ClmulEvenFactor_Throws()
    {
        var json = """{"width":32,"steps":[{"type":"Clmul","factor":"2"}]}""";
        Assert.Throws<BijectionConfigException>(() => BijectionSerializer.FromJson<uint>(json));
    }

    [Fact]
    public void JsonDeserialization_Crc32OnWidth64_Throws()
    {
        var json = """{"width":64,"steps":[{"type":"Crc32"}]}""";
        Assert.Throws<BijectionConfigException>(() => BijectionSerializer.FromJson<ulong>(json));
    }
}
