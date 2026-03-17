using IdScrambler;
using IdScrambler.Serialization;

namespace IdScrambler.Tests;

/// <summary>
/// Validation rejection tests: invalid parameters must throw at construction time.
/// </summary>
public class ValidationTests
{
    // Multiply: even factor
    [Fact]
    public void Multiply32_EvenFactor_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().Multiply(2));
    }

    [Fact]
    public void Multiply32_Zero_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().Multiply(0));
    }

    [Fact]
    public void Multiply64_EvenFactor_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<ulong>.Create().Multiply(4));
    }

    // RotateBits: out of range
    [Fact]
    public void RotateBits32_Zero_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().RotateBits(0));
    }

    [Fact]
    public void RotateBits32_N_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().RotateBits(32));
    }

    [Fact]
    public void RotateBits32_Negative_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().RotateBits(-1));
    }

    [Fact]
    public void RotateBits64_Zero_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<ulong>.Create().RotateBits(0));
    }

    [Fact]
    public void RotateBits64_N_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<ulong>.Create().RotateBits(64));
    }

    // XorShift: out of range
    [Fact]
    public void XorShift32_Zero_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().XorShiftRight(0));
    }

    [Fact]
    public void XorShift32_N_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().XorShiftRight(32));
    }

    [Fact]
    public void XorShift32_Negative_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().XorShiftLeft(-1));
    }

    [Fact]
    public void XorShift64_Zero_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<ulong>.Create().XorShiftRight(0));
    }

    [Fact]
    public void XorShift64_N_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<ulong>.Create().XorShiftLeft(64));
    }

    // NibbleSubstitution: non-permutation
    [Fact]
    public void NibbleSubstitution_DuplicateValue_Throws()
    {
        byte[] badSbox = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 14]; // duplicate 14
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().SubstituteNibbles(badSbox));
    }

    [Fact]
    public void NibbleSubstitution_OutOfRange_Throws()
    {
        byte[] badSbox = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16]; // 16 out of range
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().SubstituteNibbles(badSbox));
    }

    [Fact]
    public void NibbleSubstitution_WrongLength_Throws()
    {
        byte[] badSbox = [0, 1, 2, 3]; // too short
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().SubstituteNibbles(badSbox));
    }

    [Fact]
    public void NibbleSubstitution_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BijectionChain<uint>.Create().SubstituteNibbles(null!));
    }

    // BytePermutation: invalid arrays
    [Fact]
    public void BytePermutation32_DuplicateIndex_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().PermuteBytes([0, 1, 2, 2]));
    }

    [Fact]
    public void BytePermutation32_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().PermuteBytes([0, 1, 2, 4]));
    }

    [Fact]
    public void BytePermutation32_WrongLength_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().PermuteBytes([0, 1, 2]));
    }

    [Fact]
    public void BytePermutation64_WrongLength_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<ulong>.Create().PermuteBytes([0, 1, 2, 3])); // need 8
    }

    // Affine: even factor
    [Fact]
    public void Affine32_EvenFactor_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BijectionChain<uint>.Create().Affine(2, 100));
    }

    // Identity transforms
    [Fact]
    public void Multiply32_ByOne_IsIdentity()
    {
        var b = BijectionChain<uint>.Create().Multiply(1);
        Assert.Equal(12345u, b.Forward(12345));
    }

    [Fact]
    public void Add32_Zero_IsIdentity()
    {
        var b = BijectionChain<uint>.Create().Add(0);
        Assert.Equal(12345u, b.Forward(12345));
    }

    [Fact]
    public void Xor32_Zero_IsIdentity()
    {
        var b = BijectionChain<uint>.Create().Xor(0);
        Assert.Equal(12345u, b.Forward(12345));
    }

    [Fact]
    public void FullChainOfIdentities_IsIdentity()
    {
        var chain = BijectionChain<uint>.Create()
            .Xor(0)
            .Add(0)
            .Multiply(1);

        Assert.Equal(42u, chain.Forward(42));
        Assert.Equal(42u, chain.Inverse(42));
    }

    // Serialization validation
    [Fact]
    public void JsonDeserialization_EvenFactor_Throws()
    {
        var json = """
        {
          "width": 32,
          "steps": [
            { "type": "Multiply", "factor": "2" }
          ]
        }
        """;

        Assert.Throws<BijectionConfigException>(() =>
            BijectionSerializer.FromJson<uint>(json));
    }

    [Fact]
    public void JsonDeserialization_UnknownType_Throws()
    {
        var json = """
        {
          "width": 32,
          "steps": [
            { "type": "UnknownOp" }
          ]
        }
        """;

        Assert.Throws<BijectionConfigException>(() =>
            BijectionSerializer.FromJson<uint>(json));
    }

    [Fact]
    public void XmlDeserialization_EvenFactor_Throws()
    {
        var xml = """
        <BijectionChain width="32">
          <Multiply factor="2" />
        </BijectionChain>
        """;

        Assert.Throws<BijectionConfigException>(() =>
            BijectionSerializer.FromXml<uint>(xml));
    }

    // Default S-box is a valid permutation
    [Fact]
    public void DefaultSBox_IsValidPermutation()
    {
        var sbox = SBoxPresets.Default;
        Assert.Equal(16, sbox.Length);
        var sorted = sbox.OrderBy(x => x).ToArray();
        for (int i = 0; i < 16; i++)
            Assert.Equal(i, sorted[i]);
    }
}
