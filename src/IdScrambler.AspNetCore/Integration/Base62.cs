using System.Buffers;
using System.Buffers.Text;

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
        return EncodeBase64Url(bytes);
    }

    /// <summary>Encode a uint to Base64Url (6 chars, no padding).</summary>
    public static string Encode(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return EncodeBase64Url(bytes);
    }

    /// <summary>Encode a ulong to Base64Url (11 chars, no padding).</summary>
    public static string Encode(ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        return EncodeBase64Url(bytes);
    }

    /// <summary>Decode a Base64Url string to ushort.</summary>
    public static ushort DecodeUInt16(ReadOnlySpan<char> chars)
    {
        if (chars.Length != 3)
            throw new FormatException("Base64Url-encoded 16-bit values must be exactly 3 characters.");

        Span<byte> bytes = stackalloc byte[2];
        DecodeBase64Url(chars, bytes);
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    /// <summary>Decode a Base64Url string to uint.</summary>
    public static uint DecodeUInt32(ReadOnlySpan<char> chars)
    {
        if (chars.Length != 6)
            throw new FormatException("Base64Url-encoded 32-bit values must be exactly 6 characters.");

        Span<byte> bytes = stackalloc byte[4];
        DecodeBase64Url(chars, bytes);
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    /// <summary>Decode a Base64Url string to ulong.</summary>
    public static ulong DecodeUInt64(ReadOnlySpan<char> chars)
    {
        if (chars.Length != 11)
            throw new FormatException("Base64Url-encoded 64-bit values must be exactly 11 characters.");

        Span<byte> bytes = stackalloc byte[8];
        DecodeBase64Url(chars, bytes);
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes);
    }

    private static string EncodeBase64Url(ReadOnlySpan<byte> bytes)
    {
        Span<byte> encoded = stackalloc byte[12];
        var status = Base64.EncodeToUtf8(bytes, encoded, out _, out int written);
        if (status != OperationStatus.Done)
            throw new InvalidOperationException("Base64Url encoding failed.");

        Span<char> chars = stackalloc char[11];
        int count = 0;
        for (int i = 0; i < written; i++)
        {
            byte value = encoded[i];
            if (value == (byte)'=')
                break;

            chars[count++] = value switch
            {
                (byte)'+' => '-',
                (byte)'/' => '_',
                _ => (char)value
            };
        }

        return new string(chars[..count]);
    }

    private static void DecodeBase64Url(ReadOnlySpan<char> chars, Span<byte> destination)
    {
        int paddedLength = ((chars.Length + 3) / 4) * 4;
        Span<byte> encoded = stackalloc byte[12];

        int i = 0;
        for (; i < chars.Length; i++)
        {
            char c = chars[i];
            encoded[i] = c switch
            {
                '-' => (byte)'+',
                '_' => (byte)'/',
                <= (char)127 => (byte)c,
                _ => throw new FormatException($"Invalid Base64Url character: '{c}'.")
            };
        }

        for (; i < paddedLength; i++)
            encoded[i] = (byte)'=';

        var status = Base64.DecodeFromUtf8(encoded[..paddedLength], destination, out _, out int written);
        if (status != OperationStatus.Done || written != destination.Length)
        {
            throw new FormatException(
                $"Base64Url-encoded {destination.Length * 8}-bit values must decode to exactly {destination.Length} bytes.");
        }
    }
}
