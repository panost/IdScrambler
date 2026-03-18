using IdScrambler;
using IdScrambler.Integration;
using IdScrambler.Serialization;
using IdScrambler.Transforms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace IdScrambler.Tests;

/// <summary>
/// Tests for chain composition, compilation, and serialization round-trips.
/// </summary>
public class ChainTests
{
    [Fact]
    public void Chain32_MultiStep_RoundTrip()
    {
        var chain = BijectionChain<uint>.Create()
            .Xor(0xDEADBEEF)
            .Multiply(0x9E3779B9)
            .XorShiftRight(16);

        uint encoded = chain.Forward(42);
        Assert.Equal(42u, chain.Inverse(encoded));
        Assert.NotEqual(42u, encoded); // Obfuscated value should differ
    }

    [Fact]
    public void Chain64_MultiStep_RoundTrip()
    {
        var chain = BijectionChain<ulong>.Create()
            .Xor(0xDEADBEEFCAFEBABE)
            .Multiply(0x9E3779B97F4A7C15)
            .XorShiftRight(32);

        ulong encoded = chain.Forward(42);
        Assert.Equal(42UL, chain.Inverse(encoded));
    }

    // Compiled delegates
    [Fact]
    public void CompiledForward32_MatchesInterpreted()
    {
        var chain = BijectionChain<uint>.Create()
            .Xor(0xDEADBEEF)
            .Multiply(0x9E3779B9)
            .XorShiftRight(16);

        var forward = chain.CompileForward();
        var inverse = chain.CompileInverse();

        for (uint x = 0; x < 1000; x++)
        {
            Assert.Equal(chain.Forward(x), forward(x));
            Assert.Equal(chain.Inverse(x), inverse(x));
        }
    }

    [Fact]
    public void CompiledDelegate_RoundTrip32()
    {
        var chain = BijectionChain<uint>.Create()
            .Multiply(0x9E3779B9)
            .XorShiftRight(16)
            .Add(0x12345678)
            .GrayCode()
            .XorHighLow();

        var forward = chain.CompileForward();
        var inverse = chain.CompileInverse();

        var rng = new Random(123);
        for (int i = 0; i < 10000; i++)
        {
            uint x = (uint)rng.Next();
            Assert.Equal(x, inverse(forward(x)));
        }
    }

    [Fact]
    public void CompiledDelegate_RoundTrip64()
    {
        var chain = BijectionChain<ulong>.Create()
            .Multiply(0x9E3779B97F4A7C15)
            .XorShiftRight(32)
            .Xor(0xDEADBEEFCAFEBABE);

        var forward = chain.CompileForward();
        var inverse = chain.CompileInverse();

        var rng = new Random(456);
        for (int i = 0; i < 10000; i++)
        {
            ulong x = ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next();
            Assert.Equal(x, inverse(forward(x)));
        }
    }

    // Randomized chain round-trip
    [Fact]
    public void RandomizedChain32_RoundTrip()
    {
        var rng = new Random(789);
        for (int trial = 0; trial < 20; trial++)
        {
            var chain = BijectionChain<uint>.Create();
            int steps = rng.Next(5, 11);
            for (int s = 0; s < steps; s++)
            {
                switch (rng.Next(7))
                {
                    case 0: chain.Xor((uint)rng.Next()); break;
                    case 1: chain.Add((uint)rng.Next()); break;
                    case 2: chain.Multiply((uint)rng.Next() | 1); break; // ensure odd
                    case 3: chain.RotateBits(rng.Next(1, 32)); break;
                    case 4: chain.XorShiftRight(rng.Next(1, 32)); break;
                    case 5: chain.XorShiftLeft(rng.Next(1, 32)); break;
                    case 6: chain.GrayCode(); break;
                }
            }

            for (int i = 0; i < 1000; i++)
            {
                uint x = (uint)rng.Next();
                Assert.Equal(x, chain.Inverse(chain.Forward(x)));
            }
        }
    }

    // Empty chain is identity
    [Fact]
    public void EmptyChain_IsIdentity()
    {
        var chain = BijectionChain<uint>.Create();
        Assert.Equal(42u, chain.Forward(42));
        Assert.Equal(42u, chain.Inverse(42));
    }

    [Fact]
    public void Chain_IsImmutable()
    {
        var empty = BijectionChain<uint>.Create();
        var withXor = empty.Xor(0xDEADBEEF);
        var withTwoSteps = withXor.Add(12345);

        Assert.Equal(0, empty.Count);
        Assert.Equal(1, withXor.Count);
        Assert.Equal(2, withTwoSteps.Count);
        Assert.Equal(42u, empty.Forward(42));
        Assert.NotEqual(withXor.Forward(42), withTwoSteps.Forward(42));
    }

    // Serialization round-trip: JSON
    [Fact]
    public void JsonSerialization_RoundTrip32()
    {
        var chain = BijectionChain<uint>.Create()
            .Xor(0xDEADBEEF)
            .Multiply(0x9E3779B9)
            .XorShiftRight(16)
            .Add(12345)
            .RotateBits(7)
            .ReverseBits()
            .GrayCode()
            .XorHighLow();

        string json = BijectionSerializer.ToJson<uint>(chain);
        var restored = BijectionSerializer.FromJson<uint>(json);

        for (uint x = 0; x < 500; x++)
        {
            Assert.Equal(chain.Forward(x), restored.Forward(x));
            Assert.Equal(chain.Inverse(x), restored.Inverse(x));
        }
    }

    // Serialization round-trip: XML
    [Fact]
    public void XmlSerialization_RoundTrip32()
    {
        var chain = BijectionChain<uint>.Create()
            .Xor(0xDEADBEEF)
            .Multiply(0x9E3779B9)
            .XorShiftRight(16)
            .Affine(0x45D9F3B1, 0x12345678);

        string xml = BijectionSerializer.ToXml<uint>(chain);
        var restored = BijectionSerializer.FromXml<uint>(xml);

        for (uint x = 0; x < 500; x++)
        {
            Assert.Equal(chain.Forward(x), restored.Forward(x));
        }
    }

    // JSON deserialization from spec example
    [Fact]
    public void JsonDeserialization_FromSpecExample()
    {
        var json = """
        {
          "width": 32,
          "steps": [
            { "type": "Xor", "key": "0xDEADBEEF" },
            { "type": "Multiply", "factor": "0x9E3779B9" },
            { "type": "XorShiftRight", "shift": 16 },
            { "type": "Add", "offset": 12345 },
            { "type": "RotateBits", "amount": 7 },
            { "type": "XorShiftLeft", "shift": 5 },
            { "type": "PermuteBytes", "permutation": [3, 2, 1, 0] },
            { "type": "SubstituteNibbles", "sbox": [14,4,13,1,2,15,11,8,3,10,6,12,5,9,0,7] },
            { "type": "ReverseBits" },
            { "type": "GrayCode" },
            { "type": "Affine", "factor": "0x12345679", "offset": "0xABCDEF01" },
            { "type": "XorHighLow" }
          ]
        }
        """;

        var chain = BijectionSerializer.FromJson<uint>(json);
        // Verify it round-trips
        uint encoded = chain.Forward(42);
        Assert.Equal(42u, chain.Inverse(encoded));
    }

    // XML deserialization from spec example
    [Fact]
    public void XmlDeserialization_FromSpecExample()
    {
        var xml = """
        <BijectionChain width="32">
          <Xor key="0xDEADBEEF" />
          <Multiply factor="0x9E3779B9" />
          <XorShiftRight shift="16" />
          <Add offset="12345" />
          <RotateBits amount="7" />
          <XorShiftLeft shift="5" />
          <PermuteBytes permutation="3,2,1,0" />
          <SubstituteNibbles sbox="14,4,13,1,2,15,11,8,3,10,6,12,5,9,0,7" />
          <ReverseBits />
          <GrayCode />
          <Affine factor="0x12345679" offset="0xABCDEF01" />
          <XorHighLow />
        </BijectionChain>
        """;

        var chain = BijectionSerializer.FromXml<uint>(xml);
        uint encoded = chain.Forward(42);
        Assert.Equal(42u, chain.Inverse(encoded));
    }

    // Base62 round-trip
    [Fact]
    public void Base62_RoundTrip32()
    {
        for (uint x = 0; x < 1000; x++)
        {
            var encoded = Base62.Encode(x);
            Assert.Equal(6, encoded.Length);
            Assert.Equal(x, Base62.DecodeUInt32(encoded));
        }
    }

    [Fact]
    public void Base62_RoundTrip64()
    {
        ulong[] values = [0, 1, 42, ulong.MaxValue, 0xDEADBEEFCAFEBABE];
        foreach (var x in values)
        {
            var encoded = Base62.Encode(x);
            Assert.Equal(11, encoded.Length);
            Assert.Equal(x, Base62.DecodeUInt64(encoded));
        }
    }

    // Base64Url round-trip
    [Fact]
    public void Base64Url_RoundTrip32()
    {
        uint[] values = [0, 1, 42, uint.MaxValue, 0xDEADBEEF];
        foreach (var x in values)
        {
            var encoded = Base64Url.Encode(x);
            Assert.Equal(x, Base64Url.DecodeUInt32(encoded));
        }
    }

    [Fact]
    public void Base64Url_RoundTrip64()
    {
        ulong[] values = [0, 1, 42, ulong.MaxValue, 0xDEADBEEFCAFEBABE];
        foreach (var x in values)
        {
            var encoded = Base64Url.Encode(x);
            Assert.Equal(x, Base64Url.DecodeUInt64(encoded));
        }
    }

    // BijectionRegistry round-trip
    [Fact]
    public void Registry_Encode_Decode_RoundTrip()
    {
        var registry = new BijectionRegistry();
        registry.Register("Test", BijectionChain<uint>.Create()
            .Multiply(0x9E3779B9)
            .XorShiftRight(16)
            .Xor(0xDEADBEEF));

        // Numeric
        var numericToken = registry.Encode("Test", 42);
        Assert.Equal(42, registry.DecodeInt32("Test", numericToken, ObfuscatedIdFormat.Numeric));

        // Base62
        var base62Token = registry.Encode("Test", 42, ObfuscatedIdFormat.Base62);
        Assert.Equal(42, registry.DecodeInt32("Test", base62Token, ObfuscatedIdFormat.Base62));

        // Base64Url
        var base64Token = registry.Encode("Test", 42, ObfuscatedIdFormat.Base64Url);
        Assert.Equal(42, registry.DecodeInt32("Test", base64Token, ObfuscatedIdFormat.Base64Url));
    }

    // Registry case-insensitive
    [Fact]
    public void Registry_CaseInsensitive()
    {
        var registry = new BijectionRegistry();
        registry.Register("MyChain", Presets.LightScramble32);

        var chain = registry.Resolve<uint>("mychain");
        Assert.Equal(Presets.LightScramble32.Forward(42), chain.Forward(42));
    }

    [Fact]
    public void AddBijection_ConfiguresMvcAndHttpJsonSerialization()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddBijection(registry =>
        {
            registry.Register("Order", BijectionChain<uint>.Create()
                .Multiply(0x9E3779B9)
                .XorShiftRight(16)
                .Xor(0xDEADBEEF));
        });

        using var provider = services.BuildServiceProvider();
        var httpJson = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
            .Value.SerializerOptions;
        var mvcJson = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
            .Value.JsonSerializerOptions;

        var dto = new OrderDto { Id = 42, Name = "sample" };
        string httpJsonText = JsonSerializer.Serialize(dto, httpJson);
        string mvcJsonText = JsonSerializer.Serialize(dto, mvcJson);

        Assert.Equal(httpJsonText, mvcJsonText);
        Assert.DoesNotContain("\"Id\":42", httpJsonText, StringComparison.Ordinal);
    }

    [Fact]
    public void ObfuscatedId_Numeric64_SupportsFullUnsignedRange()
    {
        var registry = new BijectionRegistry();
        registry.Register("Value64", BijectionChain<ulong>.Create().Xor(0x8000000000000000));

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { ObfuscatedIdModifier.Apply(registry) }
            }
        };

        var dto = JsonSerializer.Deserialize<LongDto>("{\"Id\":9223372036854775808}", options);

        Assert.NotNull(dto);
        Assert.Equal(0L, dto!.Id);
    }

    private sealed class OrderDto
    {
        [ObfuscatedId("Order")]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class LongDto
    {
        [ObfuscatedId("Value64")]
        public long Id { get; set; }
    }
}
