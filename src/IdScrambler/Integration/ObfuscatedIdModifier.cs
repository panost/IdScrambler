using System.Reflection;
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

            if (prop.PropertyType == typeof(int))
            {
                prop.CustomConverter = new ObfuscatedInt32Converter(registry, attr.ChainName, attr.Format);
            }
            else if (prop.PropertyType == typeof(long))
            {
                prop.CustomConverter = new ObfuscatedInt64Converter(registry, attr.ChainName, attr.Format);
            }
        }
    };

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
                    ? (uint)reader.GetInt64()
                    : uint.Parse(reader.GetString()!);
            }
            else
            {
                var token = reader.GetString()!;
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
                    ? (ulong)reader.GetInt64()
                    : ulong.Parse(reader.GetString()!);
            }
            else
            {
                var token = reader.GetString()!;
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
