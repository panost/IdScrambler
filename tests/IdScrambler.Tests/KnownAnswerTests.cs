using IdScrambler;

namespace IdScrambler.Tests;

/// <summary>
/// Known-answer tests: hand-verified (input, expected_output) pairs for each transform.
/// </summary>
public class KnownAnswerTests
{
    // XOR: x ^ key
    [Fact]
    public void Xor32_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().Xor(0xFF00FF00);
        // 0x12345678 ^ 0xFF00FF00 = 0xED34A978
        Assert.Equal(0xED34A978u, b.Forward(0x12345678));
    }

    [Fact]
    public void Xor64_KnownAnswer()
    {
        var b = BijectionChain<ulong>.Create().Xor(0xFF00FF00FF00FF00);
        Assert.Equal(0xED3456789ABCDEF0UL ^ 0xFF00FF00FF00FF00UL, b.Forward(0xED3456789ABCDEF0UL));
    }

    // Add: (x + offset) mod 2^N
    [Fact]
    public void Add32_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().Add(10);
        Assert.Equal(52u, b.Forward(42));
        Assert.Equal(42u, b.Inverse(52u));
    }

    [Fact]
    public void Add32_Overflow_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().Add(1);
        Assert.Equal(0u, b.Forward(uint.MaxValue)); // wraps
        Assert.Equal(uint.MaxValue, b.Inverse(0));
    }

    // Multiply: (x * factor) mod 2^N, using Knuth's golden ratio constant
    [Fact]
    public void Multiply32_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().Multiply(0x9E3779B9);
        uint result = b.Forward(1);
        Assert.Equal(0x9E3779B9u, result);
        Assert.Equal(1u, b.Inverse(result));
    }

    [Fact]
    public void Multiply32_MultiplyByOne_IsIdentity()
    {
        var b = BijectionChain<uint>.Create().Multiply(1);
        Assert.Equal(42u, b.Forward(42));
    }

    // Rotate bits
    [Fact]
    public void RotateBits32_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().RotateBits(8);
        // Rotate left by 8: 0x12345678 → 0x34567812
        Assert.Equal(0x34567812u, b.Forward(0x12345678));
        Assert.Equal(0x12345678u, b.Inverse(0x34567812u));
    }

    [Fact]
    public void RotateBits64_KnownAnswer()
    {
        var b = BijectionChain<ulong>.Create().RotateBits(16);
        // Rotate left by 16: 0x0123456789ABCDEF → 0x456789ABCDEF0123
        Assert.Equal(0x456789ABCDEF0123UL, b.Forward(0x0123456789ABCDEF));
    }

    // XorShift right
    [Fact]
    public void XorShiftRight32_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().XorShiftRight(16);
        // x ^ (x >>> 16) where x = 0xAAAA0000:
        // 0xAAAA0000 ^ 0x0000AAAA = 0xAAAAAAAA
        Assert.Equal(0xAAAAAAAAu, b.Forward(0xAAAA0000));
        Assert.Equal(0xAAAA0000u, b.Inverse(0xAAAAAAAAu));
    }

    // XorShift left
    [Fact]
    public void XorShiftLeft32_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().XorShiftLeft(16);
        // x ^ (x << 16) where x = 0x0000AAAA:
        // 0x0000AAAA ^ 0xAAAA0000 = 0xAAAAAAAA
        Assert.Equal(0xAAAAAAAAu, b.Forward(0x0000AAAA));
        Assert.Equal(0x0000AAAAu, b.Inverse(0xAAAAAAAAu));
    }

    // Gray code
    [Fact]
    public void GrayCode32_KnownAnswers()
    {
        var b = BijectionChain<uint>.Create().GrayCode();
        // Standard Gray code values
        Assert.Equal(0u, b.Forward(0));
        Assert.Equal(1u, b.Forward(1));
        Assert.Equal(3u, b.Forward(2));   // 10 → 11
        Assert.Equal(2u, b.Forward(3));   // 11 → 10
        Assert.Equal(6u, b.Forward(4));   // 100 → 110
        Assert.Equal(7u, b.Forward(5));   // 101 → 111
        Assert.Equal(5u, b.Forward(6));   // 110 → 101
        Assert.Equal(4u, b.Forward(7));   // 111 → 100
    }

    // Bit reversal
    [Fact]
    public void BitReversal32_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().ReverseBits();
        // 0x00000001 reversed = 0x80000000
        Assert.Equal(0x80000000u, b.Forward(1));
        Assert.Equal(1u, b.Forward(0x80000000u));
    }

    [Fact]
    public void BitReversal32_Palindrome()
    {
        var b = BijectionChain<uint>.Create().ReverseBits();
        // 0xFF0000FF is a bit-palindrome: reversed = 0xFF0000FF
        Assert.Equal(0xFF0000FFu, b.Forward(0xFF0000FFu));
    }

    // XorHighLow
    [Fact]
    public void XorHighLow32_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().XorHighLow();
        // x = 0x0000FFFF → x ^ (0xFFFF << 16) = 0x0000FFFF ^ 0xFFFF0000 = 0xFFFFFFFF
        Assert.Equal(0xFFFFFFFFu, b.Forward(0x0000FFFFu));
        // Self-inverse
        Assert.Equal(0x0000FFFFu, b.Forward(0xFFFFFFFFu));
    }

    // Affine
    [Fact]
    public void Affine32_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().Affine(1, 100);
        Assert.Equal(142u, b.Forward(42)); // 42*1+100 = 142
        Assert.Equal(42u, b.Inverse(142));
    }

    // Byte permutation (reverse)
    [Fact]
    public void BytePermutation32_Reverse_KnownAnswer()
    {
        var b = BijectionChain<uint>.Create().PermuteBytes([3, 2, 1, 0]);
        // 0x12345678 with bytes [0x78, 0x56, 0x34, 0x12] (little-endian)
        // Reversed: [0x12, 0x34, 0x56, 0x78] = 0x78563412
        Assert.Equal(0x78563412u, b.Forward(0x12345678));
    }

    // Nibble substitution with identity S-box
    [Fact]
    public void NibbleSubstitution32_IdentitySBox()
    {
        byte[] identitySbox = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
        var b = BijectionChain<uint>.Create().SubstituteNibbles(identitySbox);
        Assert.Equal(0x12345678u, b.Forward(0x12345678u));
    }
}
