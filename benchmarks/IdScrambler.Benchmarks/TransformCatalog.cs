namespace IdScrambler.Benchmarks;

/// <summary>
/// Single-step chains for every transform, with representative parameters.
/// "Identity" is an empty chain and measures pure compiled-delegate call overhead —
/// subtract it mentally from every other row.
/// </summary>
internal static class TransformCatalog
{
    public static readonly string[] Names32 =
    [
        "Identity", "Xor", "Add", "Multiply", "RotateBits", "XorShiftRight", "XorShiftLeft",
        "PermuteBytes", "SubstituteNibbles", "ReverseBits", "GrayCode", "Affine", "XorHighLow",
        "XorRotate", "Quadratic", "Clmul", "Crc32", "Rxs"
    ];

    // Crc32 is 32-bit only.
    public static readonly string[] Names64 =
    [
        "Identity", "Xor", "Add", "Multiply", "RotateBits", "XorShiftRight", "XorShiftLeft",
        "PermuteBytes", "SubstituteNibbles", "ReverseBits", "GrayCode", "Affine", "XorHighLow",
        "XorRotate", "Quadratic", "Clmul", "Rxs"
    ];

    public static BijectionChain<uint> Get32(string name) => name switch
    {
        "Identity" => BijectionChain<uint>.Create(),
        "Xor" => BijectionChain<uint>.Create().Xor(0xDEADBEEF),
        "Add" => BijectionChain<uint>.Create().Add(0x12345678),
        "Multiply" => BijectionChain<uint>.Create().Multiply(0x9E3779B9),
        "RotateBits" => BijectionChain<uint>.Create().RotateBits(13),
        "XorShiftRight" => BijectionChain<uint>.Create().XorShiftRight(16),
        "XorShiftLeft" => BijectionChain<uint>.Create().XorShiftLeft(13),
        "PermuteBytes" => BijectionChain<uint>.Create().PermuteBytes([3, 2, 1, 0]),
        "SubstituteNibbles" => BijectionChain<uint>.Create().SubstituteNibbles(SBoxPresets.Default),
        "ReverseBits" => BijectionChain<uint>.Create().ReverseBits(),
        "GrayCode" => BijectionChain<uint>.Create().GrayCode(),
        "Affine" => BijectionChain<uint>.Create().Affine(0x9E3779B9, 0x12345678),
        "XorHighLow" => BijectionChain<uint>.Create().XorHighLow(),
        "XorRotate" => BijectionChain<uint>.Create().XorRotate(13, 27),
        "Quadratic" => BijectionChain<uint>.Create().Quadratic(),
        "Clmul" => BijectionChain<uint>.Create().Clmul(0x9E3779B9),
        "Crc32" => BijectionChain<uint>.Create().Crc32(),
        "Rxs" => BijectionChain<uint>.Create().Rxs(4, 4),
        _ => throw new ArgumentException($"Unknown transform: {name}")
    };

    public static BijectionChain<ulong> Get64(string name) => name switch
    {
        "Identity" => BijectionChain<ulong>.Create(),
        "Xor" => BijectionChain<ulong>.Create().Xor(0xDEADBEEFCAFEBABE),
        "Add" => BijectionChain<ulong>.Create().Add(0x123456789ABCDEF0),
        "Multiply" => BijectionChain<ulong>.Create().Multiply(0x9E3779B97F4A7C15),
        "RotateBits" => BijectionChain<ulong>.Create().RotateBits(29),
        "XorShiftRight" => BijectionChain<ulong>.Create().XorShiftRight(32),
        "XorShiftLeft" => BijectionChain<ulong>.Create().XorShiftLeft(29),
        "PermuteBytes" => BijectionChain<ulong>.Create().PermuteBytes([7, 6, 5, 4, 3, 2, 1, 0]),
        "SubstituteNibbles" => BijectionChain<ulong>.Create().SubstituteNibbles(SBoxPresets.Default),
        "ReverseBits" => BijectionChain<ulong>.Create().ReverseBits(),
        "GrayCode" => BijectionChain<ulong>.Create().GrayCode(),
        "Affine" => BijectionChain<ulong>.Create().Affine(0x9E3779B97F4A7C15, 0x123456789ABCDEF0),
        "XorHighLow" => BijectionChain<ulong>.Create().XorHighLow(),
        "XorRotate" => BijectionChain<ulong>.Create().XorRotate(25, 50),
        "Quadratic" => BijectionChain<ulong>.Create().Quadratic(),
        "Clmul" => BijectionChain<ulong>.Create().Clmul(0x9E3779B97F4A7C15),
        "Rxs" => BijectionChain<ulong>.Create().Rxs(5, 5),
        _ => throw new ArgumentException($"Unknown transform: {name}")
    };
}
