using IdScrambler.Transforms;

namespace IdScrambler;

/// <summary>
/// Pre-built bijection chains for common scenarios.
/// </summary>
public static class Presets
{
    /// <summary>
    /// Strong avalanche mixing for 32-bit values.
    /// XorShiftRight → Multiply → XorShiftRight → Multiply → XorShiftRight
    /// </summary>
    public static IBijection<uint> StrongMix32 { get; } = BijectionChain<uint>.Create()
        .XorShiftRight(16)
        .Multiply(0x45D9F3B1)
        .XorShiftRight(16)
        .Multiply(0x45D9F3B1)
        .XorShiftRight(16);

    /// <summary>
    /// Strong avalanche mixing similar to splitmix64.
    /// Based on stafford_mix13 variant.
    /// </summary>
    public static IBijection<ulong> StrongMix64 { get; } = BijectionChain<ulong>.Create()
        .XorShiftRight(30)
        .Multiply(0xBF58476D1CE4E5B9UL)
        .XorShiftRight(27)
        .Multiply(0x94D049BB133111EBUL)
        .XorShiftRight(31);

    /// <summary>
    /// Lightweight but fast scramble for 32-bit ID obfuscation.
    /// </summary>
    public static IBijection<uint> LightScramble32 { get; } = BijectionChain<uint>.Create()
        .Multiply(0x9E3779B9)
        .XorShiftRight(16)
        .Xor(0xDEADBEEF);

    /// <summary>
    /// Lightweight but fast scramble for 64-bit ID obfuscation.
    /// </summary>
    public static IBijection<ulong> LightScramble64 { get; } = BijectionChain<ulong>.Create()
        .Multiply(0x9E3779B97F4A7C15UL)
        .XorShiftRight(32)
        .Xor(0xDEADBEEFCAFEBABEUL);

    /// <summary>
    /// Lightweight scramble for 16-bit ID obfuscation.
    /// </summary>
    public static IBijection<ushort> LightScramble16 { get; } = BijectionChain<ushort>.Create()
        .Multiply(unchecked((ushort)0x9E37))
        .XorShiftRight(8)
        .Xor((ushort)0xBEEF);
}
