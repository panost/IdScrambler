using IdScrambler;

namespace IdScrambler.Tests;

/// <summary>
/// Round-trip property tests: Inverse(Forward(x)) == x and Forward(Inverse(x)) == x
/// for every transform, both widths, on representative inputs.
/// </summary>
public class RoundTripTests
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
    }

    private static void AssertRoundTrip64(IBijection<ulong> b)
    {
        foreach (var x in TestValues64)
        {
            Assert.Equal(x, b.Inverse(b.Forward(x)));
            Assert.Equal(x, b.Forward(b.Inverse(x)));
        }
    }

    [Fact]
    public void Xor32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().Xor(0xDEADBEEF));

    [Fact]
    public void Xor64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().Xor(0xDEADBEEFCAFEBABE));

    [Fact]
    public void Add32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().Add(12345));

    [Fact]
    public void Add64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().Add(123456789));

    [Fact]
    public void Multiply32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().Multiply(0x9E3779B9));

    [Fact]
    public void Multiply64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().Multiply(0x9E3779B97F4A7C15));

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(31)]
    public void RotateBits32_RoundTrip(int amount) =>
        AssertRoundTrip32(BijectionChain<uint>.Create().RotateBits(amount));

    [Theory]
    [InlineData(1)]
    [InlineData(17)]
    [InlineData(32)]
    [InlineData(63)]
    public void RotateBits64_RoundTrip(int amount) =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().RotateBits(amount));

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(16)]
    [InlineData(31)]
    public void XorShiftRight32_RoundTrip(int shift) =>
        AssertRoundTrip32(BijectionChain<uint>.Create().XorShiftRight(shift));

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(16)]
    [InlineData(31)]
    public void XorShiftLeft32_RoundTrip(int shift) =>
        AssertRoundTrip32(BijectionChain<uint>.Create().XorShiftLeft(shift));

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(63)]
    public void XorShiftRight64_RoundTrip(int shift) =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().XorShiftRight(shift));

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(63)]
    public void XorShiftLeft64_RoundTrip(int shift) =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().XorShiftLeft(shift));

    [Fact]
    public void PermuteBytes32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().PermuteBytes([3, 2, 1, 0]));

    [Fact]
    public void PermuteBytes64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().PermuteBytes([7, 6, 5, 4, 3, 2, 1, 0]));

    [Fact]
    public void PermuteBytes32_RotateLeft_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().PermuteBytes([1, 2, 3, 0]));

    [Fact]
    public void SubstituteNibbles32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create()
            .SubstituteNibbles(SBoxPresets.Default));

    [Fact]
    public void SubstituteNibbles64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create()
            .SubstituteNibbles(SBoxPresets.Default));

    [Fact]
    public void ReverseBits32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().ReverseBits());

    [Fact]
    public void ReverseBits64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().ReverseBits());

    [Fact]
    public void GrayCode32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().GrayCode());

    [Fact]
    public void GrayCode64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().GrayCode());

    [Fact]
    public void Affine32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().Affine(0x9E3779B9, 0x12345678));

    [Fact]
    public void Affine64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().Affine(0x9E3779B97F4A7C15, 0x123456789ABCDEF0));

    [Fact]
    public void XorHighLow32_RoundTrip() =>
        AssertRoundTrip32(BijectionChain<uint>.Create().XorHighLow());

    [Fact]
    public void XorHighLow64_RoundTrip() =>
        AssertRoundTrip64(BijectionChain<ulong>.Create().XorHighLow());

    [Fact]
    public void Presets_StrongMix32_RoundTrip() => AssertRoundTrip32(Presets.StrongMix32);

    [Fact]
    public void Presets_StrongMix64_RoundTrip() => AssertRoundTrip64(Presets.StrongMix64);

    [Fact]
    public void Presets_LightScramble32_RoundTrip() => AssertRoundTrip32(Presets.LightScramble32);

    [Fact]
    public void Presets_LightScramble64_RoundTrip() => AssertRoundTrip64(Presets.LightScramble64);

    // Signed extension methods round-trip
    [Fact]
    public void SignedExtensions_Int32_RoundTrip()
    {
        var b = Presets.LightScramble32;
        int[] values = [0, 1, -1, int.MinValue, int.MaxValue, 42, -42];
        foreach (var x in values)
        {
            Assert.Equal(x, b.Inverse(b.Forward(x)));
            Assert.Equal(x, b.Forward(b.Inverse(x)));
        }
    }

    [Fact]
    public void SignedExtensions_Int64_RoundTrip()
    {
        var b = Presets.LightScramble64;
        long[] values = [0, 1, -1, long.MinValue, long.MaxValue, 42, -42];
        foreach (var x in values)
        {
            Assert.Equal(x, b.Inverse(b.Forward(x)));
            Assert.Equal(x, b.Forward(b.Inverse(x)));
        }
    }

    // Random samples
    [Fact]
    public void RandomSamples32_AllTransforms_RoundTrip()
    {
        var chain = BijectionChain<uint>.Create()
            .Xor(0xCAFEBABE)
            .Add(0x12345678)
            .Multiply(0x9E3779B9)
            .RotateBits(7)
            .XorShiftRight(13)
            .XorShiftLeft(5)
            .PermuteBytes([3, 2, 1, 0])
            .SubstituteNibbles(SBoxPresets.Default)
            .ReverseBits()
            .GrayCode()
            .Affine(0x45D9F3B1, 0xDEADBEEF)
            .XorHighLow();

        var rng = new Random(42);
        for (int i = 0; i < 10000; i++)
        {
            uint x = (uint)rng.Next() | ((uint)rng.Next() << 16);
            Assert.Equal(x, chain.Inverse(chain.Forward(x)));
        }
    }

    [Fact]
    public void RandomSamples64_AllTransforms_RoundTrip()
    {
        var chain = BijectionChain<ulong>.Create()
            .Xor(0xCAFEBABEDEADBEEF)
            .Add(0x123456789ABCDEF0)
            .Multiply(0x9E3779B97F4A7C15)
            .RotateBits(17)
            .XorShiftRight(27)
            .XorShiftLeft(11)
            .PermuteBytes([7, 6, 5, 4, 3, 2, 1, 0])
            .SubstituteNibbles(SBoxPresets.Default)
            .ReverseBits()
            .GrayCode()
            .Affine(0x6C62272E07BB0143, 0xDEADBEEFCAFEBABE)
            .XorHighLow();

        var rng = new Random(42);
        for (int i = 0; i < 10000; i++)
        {
            ulong x = ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next();
            Assert.Equal(x, chain.Inverse(chain.Forward(x)));
        }
    }
}
