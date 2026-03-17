using System.Numerics;
using System.Xml.Linq;

namespace IdScrambler.Serialization;

/// <summary>
/// Reads a bijection chain from XML format.
/// </summary>
internal static class XmlChainReader
{
    public static BijectionChain<T> Read<T>(string xml)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        var doc = XDocument.Parse(xml);
        return ReadFromElement<T>(doc.Root
            ?? throw new BijectionConfigException("Empty XML document."));
    }

    public static BijectionChain<T> Read<T>(Stream stream)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        var doc = XDocument.Load(stream);
        return ReadFromElement<T>(doc.Root
            ?? throw new BijectionConfigException("Empty XML document."));
    }

    private static BijectionChain<T> ReadFromElement<T>(XElement root)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        var chain = BijectionChain<T>.Create();

        int stepIndex = 0;
        foreach (var element in root.Elements())
        {
            try
            {
                switch (element.Name.LocalName)
                {
                    case "Xor":
                        chain.Xor(ParseValue<T>(element, "key"));
                        break;
                    case "Add":
                        chain.Add(ParseValue<T>(element, "offset"));
                        break;
                    case "Multiply":
                        chain.Multiply(ParseValue<T>(element, "factor"));
                        break;
                    case "RotateBits":
                        chain.RotateBits(ParseInt(element, "amount"));
                        break;
                    case "XorShiftRight":
                        chain.XorShiftRight(ParseInt(element, "shift"));
                        break;
                    case "XorShiftLeft":
                        chain.XorShiftLeft(ParseInt(element, "shift"));
                        break;
                    case "PermuteBytes":
                        chain.PermuteBytes(ParseCommaSeparatedBytes(element, "permutation"));
                        break;
                    case "SubstituteNibbles":
                        chain.SubstituteNibbles(ParseCommaSeparatedBytes(element, "sbox"));
                        break;
                    case "ReverseBits":
                        chain.ReverseBits();
                        break;
                    case "GrayCode":
                        chain.GrayCode();
                        break;
                    case "Affine":
                        chain.Affine(ParseValue<T>(element, "factor"), ParseValue<T>(element, "offset"));
                        break;
                    case "XorHighLow":
                        chain.XorHighLow();
                        break;
                    default:
                        throw new BijectionConfigException(
                            $"Unknown step element: '{element.Name.LocalName}'.", stepIndex);
                }
            }
            catch (BijectionConfigException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BijectionConfigException(
                    $"Invalid configuration for '{element.Name.LocalName}': {ex.Message}",
                    stepIndex, ex);
            }

            stepIndex++;
        }

        return chain;
    }

    private static T ParseValue<T>(XElement element, string attributeName)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        var attr = element.Attribute(attributeName)
            ?? throw new BijectionConfigException($"Missing required attribute '{attributeName}'.");

        return JsonChainReader.ParseNumericValue<T>(attr.Value);
    }

    private static int ParseInt(XElement element, string attributeName)
    {
        var attr = element.Attribute(attributeName)
            ?? throw new BijectionConfigException($"Missing required attribute '{attributeName}'.");

        return int.Parse(attr.Value);
    }

    private static byte[] ParseCommaSeparatedBytes(XElement element, string attributeName)
    {
        var attr = element.Attribute(attributeName)
            ?? throw new BijectionConfigException($"Missing required attribute '{attributeName}'.");

        return attr.Value.Split(',').Select(s => byte.Parse(s.Trim())).ToArray();
    }
}
