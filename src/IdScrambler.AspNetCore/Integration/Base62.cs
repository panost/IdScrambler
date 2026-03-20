using System.Buffers;
using System.Numerics;

namespace IdScrambler.Integration;

/// <summary>
/// Base62 encoding/decoding utility (A-Z, a-z, 0-9).
/// </summary>
public static class Base62
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly int[] DecodeMap = BuildDecodeMap();

    private static int[] BuildDecodeMap()
    {
        var map = new int[128];
        Array.Fill(map, -1);
        for (int i = 0; i < Alphabet.Length; i++)
            map[Alphabet[i]] = i;
        return map;
    }

    /// <summary>Encode a ushort to Base62 (3 chars).</summary>
    public static string Encode(ushort value)
    {
        Span<char> chars = stackalloc char[3];
        for (int i = 2; i >= 0; i--)
        {
            chars[i] = Alphabet[value % 62];
            value /= 62;
        }
        return new string(chars);
    }

    /// <summary>Encode a uint to Base62 (6 chars).</summary>
    public static string Encode(uint value)
    {
        Span<char> chars = stackalloc char[6];
        for (int i = 5; i >= 0; i--)
        {
            chars[i] = Alphabet[(int)(value % 62)];
            value /= 62;
        }
        return new string(chars);
    }

    /// <summary>Encode a ulong to Base62 (11 chars).</summary>
    public static string Encode(ulong value)
    {
        Span<char> chars = stackalloc char[11];
        for (int i = 10; i >= 0; i--)
        {
            chars[i] = Alphabet[(int)(value % 62)];
            value /= 62;
        }
        return new string(chars);
    }

    /// <summary>Decode a Base62 string to ushort.</summary>
    public static ushort DecodeUInt16(ReadOnlySpan<char> chars)
    {
        if (chars.Length != 3)
            throw new FormatException("Base62-encoded 16-bit values must be exactly 3 characters.");

        try
        {
            uint value = 0;
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c >= 128 || DecodeMap[c] < 0)
                    throw new FormatException($"Invalid Base62 character: '{c}'.");
                checked
                {
                    value = value * 62 + (uint)DecodeMap[c];
                }
            }
            if (value > ushort.MaxValue)
                throw new FormatException("Base62 value overflows a 16-bit unsigned integer.");
            return (ushort)value;
        }
        catch (OverflowException ex)
        {
            throw new FormatException("Base62 value overflows the target integer type.", ex);
        }
    }

    /// <summary>Decode a Base62 string to uint.</summary>
    public static uint DecodeUInt32(ReadOnlySpan<char> chars)
    {
        if (chars.Length != 6)
            throw new FormatException("Base62-encoded 32-bit values must be exactly 6 characters.");

        try
        {
            uint value = 0;
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c >= 128 || DecodeMap[c] < 0)
                    throw new FormatException($"Invalid Base62 character: '{c}'.");
                checked
                {
                    value = value * 62 + (uint)DecodeMap[c];
                }
            }
            return value;
        }
        catch (OverflowException ex)
        {
            throw new FormatException("Base62 value overflows the target integer type.", ex);
        }
    }

    /// <summary>Decode a Base62 string to ulong.</summary>
    public static ulong DecodeUInt64(ReadOnlySpan<char> chars)
    {
        if (chars.Length != 11)
            throw new FormatException("Base62-encoded 64-bit values must be exactly 11 characters.");

        try
        {
            ulong value = 0;
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c >= 128 || DecodeMap[c] < 0)
                    throw new FormatException($"Invalid Base62 character: '{c}'.");
                checked
                {
                    value = value * 62 + (ulong)DecodeMap[c];
                }
            }
            return value;
        }
        catch (OverflowException ex)
        {
            throw new FormatException("Base62 value overflows the target integer type.", ex);
        }
    }
}

/// <summary>
/// Base64Url encoding/decoding utilities for integer values.
/// </summary>
public static class Base64Url
{
    /// <summary>Encode a ushort to Base64Url (3 chars, no padding).</summary>
    public static string Encode(ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Encode a uint to Base64Url (6 chars, no padding).</summary>
    public static string Encode(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Encode a ulong to Base64Url (11 chars, no padding).</summary>
    public static string Encode(ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Decode a Base64Url string to ushort.</summary>
    public static ushort DecodeUInt16(ReadOnlySpan<char> chars)
    {
        if (chars.Length != 3)
            throw new FormatException("Base64Url-encoded 16-bit values must be exactly 3 characters.");

        var s = chars.ToString().Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
            case 1: throw new FormatException("Invalid Base64Url length.");
        }
        var bytes = Convert.FromBase64String(s);
        if (bytes.Length != 2)
            throw new FormatException("Base64Url-encoded 16-bit values must decode to exactly 2 bytes.");
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    /// <summary>Decode a Base64Url string to uint.</summary>
    public static uint DecodeUInt32(ReadOnlySpan<char> chars)
    {
        if (chars.Length != 6)
            throw new FormatException("Base64Url-encoded 32-bit values must be exactly 6 characters.");

        var s = chars.ToString().Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
            case 1: throw new FormatException("Invalid Base64Url length.");
        }
        var bytes = Convert.FromBase64String(s);
        if (bytes.Length != 4)
            throw new FormatException("Base64Url-encoded 32-bit values must decode to exactly 4 bytes.");
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    /// <summary>Decode a Base64Url string to ulong.</summary>
    public static ulong DecodeUInt64(ReadOnlySpan<char> chars)
    {
        if (chars.Length != 11)
            throw new FormatException("Base64Url-encoded 64-bit values must be exactly 11 characters.");

        var s = chars.ToString().Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
            case 1: throw new FormatException("Invalid Base64Url length.");
        }
        var bytes = Convert.FromBase64String(s);
        if (bytes.Length != 8)
            throw new FormatException("Base64Url-encoded 64-bit values must decode to exactly 8 bytes.");
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes);
    }
}
