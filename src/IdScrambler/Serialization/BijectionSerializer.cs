using System.Numerics;
using System.Text.Json;
using System.Xml.Linq;
using IdScrambler.Transforms;

namespace IdScrambler.Serialization;

/// <summary>
/// Serialize and deserialize bijection chains to/from JSON and XML.
/// </summary>
public static class BijectionSerializer
{
    /// <summary>Deserialize a chain from a JSON string.</summary>
    public static IBijection<T> FromJson<T>(string json)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
        => JsonChainReader.Read<T>(json);

    /// <summary>Deserialize a chain from a JSON stream.</summary>
    public static IBijection<T> FromJson<T>(Stream stream)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
        => JsonChainReader.Read<T>(stream);

    /// <summary>Deserialize a chain from an XML string.</summary>
    public static IBijection<T> FromXml<T>(string xml)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
        => XmlChainReader.Read<T>(xml);

    /// <summary>Deserialize a chain from an XML stream.</summary>
    public static IBijection<T> FromXml<T>(Stream stream)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
        => XmlChainReader.Read<T>(stream);

    /// <summary>Serialize a chain to JSON.</summary>
    public static string ToJson<T>(IBijection<T> chain)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        if (chain is not BijectionChain<T> bijectionChain)
            throw new ArgumentException("Can only serialize BijectionChain<T> instances.", nameof(chain));

        int width = typeof(T) == typeof(uint) ? 32 : 64;

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteNumber("width", width);
        writer.WriteStartArray("steps");

        foreach (var step in bijectionChain.Steps)
        {
            writer.WriteStartObject();
            WriteStep(writer, step);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Serialize a chain to XML.</summary>
    public static string ToXml<T>(IBijection<T> chain)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        if (chain is not BijectionChain<T> bijectionChain)
            throw new ArgumentException("Can only serialize BijectionChain<T> instances.", nameof(chain));

        int width = typeof(T) == typeof(uint) ? 32 : 64;

        var root = new XElement("BijectionChain", new XAttribute("width", width));

        foreach (var step in bijectionChain.Steps)
        {
            root.Add(BuildXmlElement(step));
        }

        return root.ToString();
    }

    private static void WriteStep<T>(Utf8JsonWriter writer, IBijectionStep<T> step)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        switch (step)
        {
            case XorBijection<T> xor:
                writer.WriteString("type", "Xor");
                writer.WriteString("key", FormatHex(xor.Key));
                break;
            case AddBijection<T> add:
                writer.WriteString("type", "Add");
                writer.WriteNumber("offset", ulong.CreateTruncating(add.Offset));
                break;
            case MultiplyBijection<T> mul:
                writer.WriteString("type", "Multiply");
                writer.WriteString("factor", FormatHex(mul.Factor));
                break;
            case RotateBitsBijection<T> rot:
                writer.WriteString("type", "RotateBits");
                writer.WriteNumber("amount", rot.Amount);
                break;
            case XorShiftBijection<T> xs:
                writer.WriteString("type", xs.Direction == XorShiftDirection.Right ? "XorShiftRight" : "XorShiftLeft");
                writer.WriteNumber("shift", xs.Shift);
                break;
            case BytePermutationBijection<T> bp:
                writer.WriteString("type", "PermuteBytes");
                writer.WriteStartArray("permutation");
                foreach (var b in bp.Permutation) writer.WriteNumberValue(b);
                writer.WriteEndArray();
                break;
            case NibbleSubstitutionBijection<T> ns:
                writer.WriteString("type", "SubstituteNibbles");
                writer.WriteStartArray("sbox");
                foreach (var b in ns.SBox) writer.WriteNumberValue(b);
                writer.WriteEndArray();
                break;
            case BitReversalBijection<T>:
                writer.WriteString("type", "ReverseBits");
                break;
            case GrayCodeBijection<T>:
                writer.WriteString("type", "GrayCode");
                break;
            case AffineBijection<T> aff:
                writer.WriteString("type", "Affine");
                writer.WriteString("factor", FormatHex(aff.Factor));
                writer.WriteNumber("offset", ulong.CreateTruncating(aff.Offset));
                break;
            case XorHighLowBijection<T>:
                writer.WriteString("type", "XorHighLow");
                break;
            default:
                throw new InvalidOperationException($"Unknown step type: {step.GetType().Name}");
        }
    }

    private static XElement BuildXmlElement<T>(IBijectionStep<T> step)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        return step switch
        {
            XorBijection<T> xor => new XElement("Xor", new XAttribute("key", FormatHex(xor.Key))),
            AddBijection<T> add => new XElement("Add", new XAttribute("offset", ulong.CreateTruncating(add.Offset))),
            MultiplyBijection<T> mul => new XElement("Multiply", new XAttribute("factor", FormatHex(mul.Factor))),
            RotateBitsBijection<T> rot => new XElement("RotateBits", new XAttribute("amount", rot.Amount)),
            XorShiftBijection<T> xs => new XElement(
                xs.Direction == XorShiftDirection.Right ? "XorShiftRight" : "XorShiftLeft",
                new XAttribute("shift", xs.Shift)),
            BytePermutationBijection<T> bp => new XElement("PermuteBytes",
                new XAttribute("permutation", string.Join(",", bp.Permutation))),
            NibbleSubstitutionBijection<T> ns => new XElement("SubstituteNibbles",
                new XAttribute("sbox", string.Join(",", ns.SBox))),
            BitReversalBijection<T> => new XElement("ReverseBits"),
            GrayCodeBijection<T> => new XElement("GrayCode"),
            AffineBijection<T> aff => new XElement("Affine",
                new XAttribute("factor", FormatHex(aff.Factor)),
                new XAttribute("offset", ulong.CreateTruncating(aff.Offset))),
            XorHighLowBijection<T> => new XElement("XorHighLow"),
            _ => throw new InvalidOperationException($"Unknown step type: {step.GetType().Name}")
        };
    }

    private static string FormatHex<T>(T value) where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        if (typeof(T) == typeof(uint))
            return $"0x{uint.CreateTruncating(value):X8}";
        else
            return $"0x{ulong.CreateTruncating(value):X16}";
    }
}
