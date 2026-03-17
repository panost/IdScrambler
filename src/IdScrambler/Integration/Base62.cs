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

    /// <summary>Decode a Base62 string to uint.</summary>
    public static uint DecodeUInt32(ReadOnlySpan<char> chars)
    {
        uint value = 0;
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (c >= 128 || DecodeMap[c] < 0)
                throw new FormatException($"Invalid Base62 character: '{c}'.");
            value = value * 62 + (uint)DecodeMap[c];
        }
        return value;
    }

    /// <summary>Decode a Base62 string to ulong.</summary>
    public static ulong DecodeUInt64(ReadOnlySpan<char> chars)
    {
        ulong value = 0;
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (c >= 128 || DecodeMap[c] < 0)
                throw new FormatException($"Invalid Base62 character: '{c}'.");
            value = value * 62 + (ulong)DecodeMap[c];
        }
        return value;
    }
}

/// <summary>
/// Base64Url encoding/decoding utilities for integer values.
/// </summary>
public static class Base64Url
{
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

    /// <summary>Decode a Base64Url string to uint.</summary>
    public static uint DecodeUInt32(ReadOnlySpan<char> chars)
    {
        var s = chars.ToString().Replace('-', '+').Replace('_', '/');
        // Add padding
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        var bytes = Convert.FromBase64String(s);
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    /// <summary>Decode a Base64Url string to ulong.</summary>
    public static ulong DecodeUInt64(ReadOnlySpan<char> chars)
    {
        var s = chars.ToString().Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        var bytes = Convert.FromBase64String(s);
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes);
    }
}
