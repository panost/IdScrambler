using System.Numerics;

namespace IdScrambler;

/// <summary>
/// Extension methods providing signed integer convenience overloads.
/// </summary>
public static class BijectionExtensions
{
    /// <summary>Apply the forward transformation on a signed 32-bit integer.</summary>
    public static int Forward(this IBijection<uint> bijection, int value)
        => unchecked((int)bijection.Forward(unchecked((uint)value)));

    /// <summary>Apply the inverse transformation on a signed 32-bit integer.</summary>
    public static int Inverse(this IBijection<uint> bijection, int value)
        => unchecked((int)bijection.Inverse(unchecked((uint)value)));

    /// <summary>Apply the forward transformation on a signed 64-bit integer.</summary>
    public static long Forward(this IBijection<ulong> bijection, long value)
        => unchecked((long)bijection.Forward(unchecked((ulong)value)));

    /// <summary>Apply the inverse transformation on a signed 64-bit integer.</summary>
    public static long Inverse(this IBijection<ulong> bijection, long value)
        => unchecked((long)bijection.Inverse(unchecked((ulong)value)));
}
