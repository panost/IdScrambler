using IdScrambler.Transforms;

namespace IdScrambler;

/// <summary>
/// Provides built-in S-box presets for nibble substitution.
/// </summary>
public static class SBoxPresets
{
    private static readonly byte[] _default = NibbleSubstitutionBijection<uint>.DefaultSBox;

    /// <summary>
    /// A 4-bit S-box with good non-linearity properties (permutation of 0..15).
    /// Derived from DES S-box 5, first row. Returns a defensive copy.
    /// </summary>
    public static byte[] Default => (byte[])_default.Clone();
}
