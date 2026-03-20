using System.Reflection;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace IdScrambler.Integration;

/// <summary>
/// JsonTypeInfoResolver modifier that applies obfuscated ID conversion per-property.
/// </summary>
public static class ObfuscatedIdModifier
{
    /// <summary>Creates a modifier action that applies ObfuscatedId converters.</summary>
    public static Action<JsonTypeInfo> Apply(BijectionRegistry registry) => typeInfo =>
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        foreach (var prop in typeInfo.Properties)
        {
            var attr = prop.AttributeProvider?.GetCustomAttributes(typeof(ObfuscatedIdAttribute), false)
                .OfType<ObfuscatedIdAttribute>()
                .FirstOrDefault();

            if (attr == null) continue;

            if (prop.PropertyType == typeof(short))
            {
                prop.CustomConverter = new ObfuscatedInt16Converter(registry, attr.ChainName, attr.Format);
            }
            else if (prop.PropertyType == typeof(int))
            {
                prop.CustomConverter = new ObfuscatedInt32Converter(registry, attr.ChainName, attr.Format);
            }
            else if (prop.PropertyType == typeof(long))
            {
                prop.CustomConverter = new ObfuscatedInt64Converter(registry, attr.ChainName, attr.Format);
            }
        }
    };

    private sealed class ObfuscatedInt16Converter : JsonConverter<short>
    {
        private readonly IBijection<ushort> _chain;
        private readonly ObfuscatedIdFormat _format;

        public ObfuscatedInt16Converter(BijectionRegistry registry, string chainName, ObfuscatedIdFormat format)
        {
            _chain = registry.Resolve<ushort>(chainName);
            _format = format;
        }

        public override short Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ushort obfuscated;
            if (_format == ObfuscatedIdFormat.Numeric)
            {
                obfuscated = reader.TokenType == JsonTokenType.Number
                    ? (ushort)reader.GetUInt32()
                    : ushort.Parse(reader.GetString() ?? throw new JsonException("Expected non-null string."),
                        CultureInfo.InvariantCulture);
            }
            else
            {
                var token = reader.GetString() ?? throw new JsonException("Expected non-null string for encoded ID.");
                obfuscated = _format == ObfuscatedIdFormat.Base64Url
                    ? Base64Url.DecodeUInt16(token)
                    : Base62.DecodeUInt16(token);
            }
            return unchecked((short)_chain.Inverse(obfuscated));
        }

        public override void Write(Utf8JsonWriter writer, short value, JsonSerializerOptions options)
        {
            ushort obfuscated = _chain.Forward(unchecked((ushort)value));
            switch (_format)
            {
                case ObfuscatedIdFormat.Numeric:
                    writer.WriteNumberValue(obfuscated);
                    break;
                case ObfuscatedIdFormat.Base64Url:
                    writer.WriteStringValue(Base64Url.Encode(obfuscated));
                    break;
                case ObfuscatedIdFormat.Base62:
                    writer.WriteStringValue(Base62.Encode(obfuscated));
                    break;
            }
        }
    }

    private sealed class ObfuscatedInt32Converter : JsonConverter<int>
    {
        private readonly IBijection<uint> _chain;
        private readonly ObfuscatedIdFormat _format;

        public ObfuscatedInt32Converter(BijectionRegistry registry, string chainName, ObfuscatedIdFormat format)
        {
            _chain = registry.Resolve<uint>(chainName);
            _format = format;
        }

        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            uint obfuscated;
            if (_format == ObfuscatedIdFormat.Numeric)
            {
                obfuscated = reader.TokenType == JsonTokenType.Number
                    ? reader.GetUInt32()
                    : uint.Parse(reader.GetString() ?? throw new JsonException("Expected non-null string."),
                        CultureInfo.InvariantCulture);
            }
            else
            {
                var token = reader.GetString() ?? throw new JsonException("Expected non-null string for encoded ID.");
                obfuscated = _format == ObfuscatedIdFormat.Base64Url
                    ? Base64Url.DecodeUInt32(token)
                    : Base62.DecodeUInt32(token);
            }
            return unchecked((int)_chain.Inverse(obfuscated));
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            uint obfuscated = _chain.Forward(unchecked((uint)value));
            switch (_format)
            {
                case ObfuscatedIdFormat.Numeric:
                    writer.WriteNumberValue(obfuscated);
                    break;
                case ObfuscatedIdFormat.Base64Url:
                    writer.WriteStringValue(Base64Url.Encode(obfuscated));
                    break;
                case ObfuscatedIdFormat.Base62:
                    writer.WriteStringValue(Base62.Encode(obfuscated));
                    break;
            }
        }
    }

    private sealed class ObfuscatedInt64Converter : JsonConverter<long>
    {
        private readonly IBijection<ulong> _chain;
        private readonly ObfuscatedIdFormat _format;

        public ObfuscatedInt64Converter(BijectionRegistry registry, string chainName, ObfuscatedIdFormat format)
        {
            _chain = registry.Resolve<ulong>(chainName);
            _format = format;
        }

        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ulong obfuscated;
            if (_format == ObfuscatedIdFormat.Numeric)
            {
                obfuscated = reader.TokenType == JsonTokenType.Number
                    ? reader.GetUInt64()
                    : ulong.Parse(reader.GetString() ?? throw new JsonException("Expected non-null string."),
                        CultureInfo.InvariantCulture);
            }
            else
            {
                var token = reader.GetString() ?? throw new JsonException("Expected non-null string for encoded ID.");
                obfuscated = _format == ObfuscatedIdFormat.Base64Url
                    ? Base64Url.DecodeUInt64(token)
                    : Base62.DecodeUInt64(token);
            }
            return unchecked((long)_chain.Inverse(obfuscated));
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            ulong obfuscated = _chain.Forward(unchecked((ulong)value));
            switch (_format)
            {
                case ObfuscatedIdFormat.Numeric:
                    writer.WriteNumberValue(obfuscated);
                    break;
                case ObfuscatedIdFormat.Base64Url:
                    writer.WriteStringValue(Base64Url.Encode(obfuscated));
                    break;
                case ObfuscatedIdFormat.Base62:
                    writer.WriteStringValue(Base62.Encode(obfuscated));
                    break;
            }
        }
    }
}
