using System.Globalization;
using System.Numerics;

namespace IdScrambler.Integration;

/// <summary>
/// Extension methods for BijectionRegistry to encode/decode IDs with format support.
/// </summary>
public static class BijectionRegistryExtensions
{
    /// <summary>Encode a short ID using the named 16-bit chain and specified format.</summary>
    public static string Encode(this BijectionRegistry registry, string name, short id,
        ObfuscatedIdFormat format = ObfuscatedIdFormat.Numeric)
    {
        var chain = registry.Resolve<ushort>(name);
        ushort obfuscated = chain.Forward(unchecked((ushort)id));
        return format switch
        {
            ObfuscatedIdFormat.Numeric => obfuscated.ToString(),
            ObfuscatedIdFormat.Base64Url => Base64Url.Encode(obfuscated),
            ObfuscatedIdFormat.Base62 => Base62.Encode(obfuscated),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    /// <summary>Encode an int ID using the named 32-bit chain and specified format.</summary>
    public static string Encode(this BijectionRegistry registry, string name, int id,
        ObfuscatedIdFormat format = ObfuscatedIdFormat.Numeric)
    {
        var chain = registry.Resolve<uint>(name);
        uint obfuscated = chain.Forward(unchecked((uint)id));
        return format switch
        {
            ObfuscatedIdFormat.Numeric => obfuscated.ToString(),
            ObfuscatedIdFormat.Base64Url => Base64Url.Encode(obfuscated),
            ObfuscatedIdFormat.Base62 => Base62.Encode(obfuscated),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    /// <summary>Encode a long ID using the named 64-bit chain and specified format.</summary>
    public static string Encode(this BijectionRegistry registry, string name, long id,
        ObfuscatedIdFormat format = ObfuscatedIdFormat.Numeric)
    {
        var chain = registry.Resolve<ulong>(name);
        ulong obfuscated = chain.Forward(unchecked((ulong)id));
        return format switch
        {
            ObfuscatedIdFormat.Numeric => obfuscated.ToString(),
            ObfuscatedIdFormat.Base64Url => Base64Url.Encode(obfuscated),
            ObfuscatedIdFormat.Base62 => Base62.Encode(obfuscated),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    /// <summary>Decode a string token back to short using the named 16-bit chain and format.</summary>
    public static short DecodeInt16(this BijectionRegistry registry, string name, string token,
        ObfuscatedIdFormat format = ObfuscatedIdFormat.Numeric)
    {
        var chain = registry.Resolve<ushort>(name);
        ushort obfuscated = format switch
        {
            ObfuscatedIdFormat.Numeric => ushort.Parse(token, CultureInfo.InvariantCulture),
            ObfuscatedIdFormat.Base64Url => Base64Url.DecodeUInt16(token),
            ObfuscatedIdFormat.Base62 => Base62.DecodeUInt16(token),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        return unchecked((short)chain.Inverse(obfuscated));
    }

    /// <summary>Decode a string token back to int using the named 32-bit chain and format.</summary>
    public static int DecodeInt32(this BijectionRegistry registry, string name, string token,
        ObfuscatedIdFormat format = ObfuscatedIdFormat.Numeric)
    {
        var chain = registry.Resolve<uint>(name);
        uint obfuscated = format switch
        {
            ObfuscatedIdFormat.Numeric => uint.Parse(token, CultureInfo.InvariantCulture),
            ObfuscatedIdFormat.Base64Url => Base64Url.DecodeUInt32(token),
            ObfuscatedIdFormat.Base62 => Base62.DecodeUInt32(token),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        return unchecked((int)chain.Inverse(obfuscated));
    }

    /// <summary>Decode a string token back to long using the named 64-bit chain and format.</summary>
    public static long DecodeInt64(this BijectionRegistry registry, string name, string token,
        ObfuscatedIdFormat format = ObfuscatedIdFormat.Numeric)
    {
        var chain = registry.Resolve<ulong>(name);
        ulong obfuscated = format switch
        {
            ObfuscatedIdFormat.Numeric => ulong.Parse(token, CultureInfo.InvariantCulture),
            ObfuscatedIdFormat.Base64Url => Base64Url.DecodeUInt64(token),
            ObfuscatedIdFormat.Base62 => Base62.DecodeUInt64(token),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        return unchecked((long)chain.Inverse(obfuscated));
    }
}
