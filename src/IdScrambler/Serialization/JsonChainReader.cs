using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace IdScrambler.Serialization;

/// <summary>
/// Reads a bijection chain from JSON format.
/// </summary>
internal static class JsonChainReader
{
    public static BijectionChain<T> Read<T>(string json)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        using var doc = JsonDocument.Parse(json);
        return ReadFromElement<T>(doc.RootElement);
    }

    public static BijectionChain<T> Read<T>(Stream stream)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        using var doc = JsonDocument.Parse(stream);
        return ReadFromElement<T>(doc.RootElement);
    }

    internal static BijectionChain<T> ReadFromElement<T>(JsonElement root)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        ValidateWidth<T>(root);

        var chain = BijectionChain<T>.Create();

        if (!root.TryGetProperty("steps", out var stepsElement))
            throw new BijectionConfigException("Missing 'steps' array in JSON.");

        int stepIndex = 0;
        foreach (var step in stepsElement.EnumerateArray())
        {
            if (!step.TryGetProperty("type", out var typeElement))
                throw new BijectionConfigException("Missing 'type' property.", stepIndex);

            var type = typeElement.GetString()
                ?? throw new BijectionConfigException("'type' cannot be null.", stepIndex);

            try
            {
                switch (type)
                {
                    case "Xor":
                        chain = chain.Xor(ParseValue<T>(step, "key"));
                        break;
                    case "Add":
                        chain = chain.Add(ParseValue<T>(step, "offset"));
                        break;
                    case "Multiply":
                        chain = chain.Multiply(ParseValue<T>(step, "factor"));
                        break;
                    case "RotateBits":
                        chain = chain.RotateBits(ParseInt(step, "amount"));
                        break;
                    case "XorShiftRight":
                        chain = chain.XorShiftRight(ParseInt(step, "shift"));
                        break;
                    case "XorShiftLeft":
                        chain = chain.XorShiftLeft(ParseInt(step, "shift"));
                        break;
                    case "PermuteBytes":
                        chain = chain.PermuteBytes(ParseByteArray(step, "permutation"));
                        break;
                    case "SubstituteNibbles":
                        chain = chain.SubstituteNibbles(ParseByteArray(step, "sbox"));
                        break;
                    case "ReverseBits":
                        chain = chain.ReverseBits();
                        break;
                    case "GrayCode":
                        chain = chain.GrayCode();
                        break;
                    case "Affine":
                        chain = chain.Affine(ParseValue<T>(step, "factor"), ParseValue<T>(step, "offset"));
                        break;
                    case "XorHighLow":
                        chain = chain.XorHighLow();
                        break;
                    default:
                        throw new BijectionConfigException($"Unknown step type: '{type}'.", stepIndex);
                }
            }
            catch (BijectionConfigException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BijectionConfigException($"Invalid configuration for '{type}': {ex.Message}", stepIndex, ex);
            }

            stepIndex++;
        }

        return chain;
    }

    private static void ValidateWidth<T>(JsonElement root)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        if (!root.TryGetProperty("width", out var widthElement))
            throw new BijectionConfigException("Missing 'width' property in JSON.");

        int width = widthElement.GetInt32();
        int expectedWidth = typeof(T) == typeof(uint) ? 32 : 64;
        if (width != expectedWidth)
        {
            throw new BijectionConfigException(
                $"Width {width} does not match target type {typeof(T).Name}. Expected {expectedWidth}.");
        }
    }

    private static T ParseValue<T>(JsonElement step, string propertyName)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        if (!step.TryGetProperty(propertyName, out var prop))
            throw new BijectionConfigException($"Missing required property '{propertyName}'.");

        string text = prop.ValueKind == JsonValueKind.String
            ? prop.GetString()!
            : prop.GetRawText();

        return ParseNumericValue<T>(text);
    }

    internal static T ParseNumericValue<T>(string text)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        text = text.Trim();

        if (typeof(T) == typeof(uint))
        {
            uint value = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? uint.Parse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : uint.Parse(text, CultureInfo.InvariantCulture);
            return T.CreateTruncating(value);
        }
        else
        {
            ulong value = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? ulong.Parse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : ulong.Parse(text, CultureInfo.InvariantCulture);
            return T.CreateTruncating(value);
        }
    }

    private static int ParseInt(JsonElement step, string propertyName)
    {
        if (!step.TryGetProperty(propertyName, out var prop))
            throw new BijectionConfigException($"Missing required property '{propertyName}'.");

        return prop.GetInt32();
    }

    private static byte[] ParseByteArray(JsonElement step, string propertyName)
    {
        if (!step.TryGetProperty(propertyName, out var prop))
            throw new BijectionConfigException($"Missing required property '{propertyName}'.");

        var list = new List<byte>();
        foreach (var item in prop.EnumerateArray())
            list.Add((byte)item.GetInt32());
        return list.ToArray();
    }
}
