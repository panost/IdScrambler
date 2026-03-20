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

    // --- 16-bit known-answer tests ---

    [Fact]
    public void Xor16_KnownAnswer()
    {
        var b = BijectionChain<ushort>.Create().Xor(0xFF00);
        Assert.Equal((ushort)(0x1234 ^ 0xFF00), b.Forward(0x1234));
    }

    [Fact]
    public void Add16_KnownAnswer()
    {
        var b = BijectionChain<ushort>.Create().Add(10);
        Assert.Equal((ushort)52, b.Forward(42));
        Assert.Equal((ushort)42, b.Inverse(52));
    }

    [Fact]
    public void Add16_Overflow_KnownAnswer()
    {
        var b = BijectionChain<ushort>.Create().Add(1);
        Assert.Equal((ushort)0, b.Forward(ushort.MaxValue));
        Assert.Equal(ushort.MaxValue, b.Inverse(0));
    }

    [Fact]
    public void Multiply16_KnownAnswer()
    {
        var b = BijectionChain<ushort>.Create().Multiply(0x9E37);
        ushort result = b.Forward(1);
        Assert.Equal((ushort)0x9E37, result);
        Assert.Equal((ushort)1, b.Inverse(result));
    }

    [Fact]
    public void RotateBits16_KnownAnswer()
    {
        var b = BijectionChain<ushort>.Create().RotateBits(8);
        // Rotate left by 8: 0x1234 → 0x3412
        Assert.Equal((ushort)0x3412, b.Forward(0x1234));
        Assert.Equal((ushort)0x1234, b.Inverse(0x3412));
    }

    [Fact]
    public void BitReversal16_KnownAnswer()
    {
        var b = BijectionChain<ushort>.Create().ReverseBits();
        // 0x0001 reversed in 16-bit = 0x8000
        Assert.Equal((ushort)0x8000, b.Forward(1));
        Assert.Equal((ushort)1, b.Forward(0x8000));
    }

    [Fact]
    public void GrayCode16_KnownAnswers()
    {
        var b = BijectionChain<ushort>.Create().GrayCode();
        Assert.Equal((ushort)0, b.Forward(0));
        Assert.Equal((ushort)1, b.Forward(1));
        Assert.Equal((ushort)3, b.Forward(2));
        Assert.Equal((ushort)2, b.Forward(3));
    }

    [Fact]
    public void XorHighLow16_KnownAnswer()
    {
        var b = BijectionChain<ushort>.Create().XorHighLow();
        // x = 0x00FF → x ^ (0xFF << 8) = 0x00FF ^ 0xFF00 = 0xFFFF
        Assert.Equal((ushort)0xFFFF, b.Forward(0x00FF));
        Assert.Equal((ushort)0x00FF, b.Forward(0xFFFF)); // self-inverse
    }

    [Fact]
    public void BytePermutation16_Swap_KnownAnswer()
    {
        var b = BijectionChain<ushort>.Create().PermuteBytes([1, 0]);
        // 0x1234 with bytes [0x34, 0x12] (little-endian), swapped: [0x12, 0x34] = 0x3412
        Assert.Equal((ushort)0x3412, b.Forward(0x1234));
    }

    // --- 64-bit additional known-answer tests (fills gap) ---

    [Fact]
    public void Add64_KnownAnswer()
    {
        var b = BijectionChain<ulong>.Create().Add(10);
        Assert.Equal(52UL, b.Forward(42));
        Assert.Equal(42UL, b.Inverse(52));
    }

    [Fact]
    public void Multiply64_KnownAnswer()
    {
        var b = BijectionChain<ulong>.Create().Multiply(0x9E3779B97F4A7C15);
        ulong result = b.Forward(1);
        Assert.Equal(0x9E3779B97F4A7C15UL, result);
        Assert.Equal(1UL, b.Inverse(result));
    }

    [Fact]
    public void BitReversal64_KnownAnswer()
    {
        var b = BijectionChain<ulong>.Create().ReverseBits();
        Assert.Equal(0x8000000000000000UL, b.Forward(1));
        Assert.Equal(1UL, b.Forward(0x8000000000000000UL));
    }

    [Fact]
    public void XorHighLow64_KnownAnswer()
    {
        var b = BijectionChain<ulong>.Create().XorHighLow();
        Assert.Equal(0xFFFFFFFFFFFFFFFFUL, b.Forward(0x00000000FFFFFFFF));
        Assert.Equal(0x00000000FFFFFFFFUL, b.Forward(0xFFFFFFFFFFFFFFFF)); // self-inverse
    }

    [Fact]
    public void Affine64_KnownAnswer()
    {
        var b = BijectionChain<ulong>.Create().Affine(1, 100);
        Assert.Equal(142UL, b.Forward(42));
        Assert.Equal(42UL, b.Inverse(142));
    }
}
