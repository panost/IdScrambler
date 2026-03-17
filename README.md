# IdScrambler

A .NET 10 class library providing a **fluent, chainable API** for applying reversible (bijective) transformations on **32-bit** (`int`/`uint`) and **64-bit** (`long`/`ulong`) integers. Every transformation is guaranteed to be a bijection over the full 2ᴺ domain — meaning it maps every possible integer to a unique output and can be perfectly reversed.

> [!CAUTION]
> This library provides **obfuscation, not encryption**. The transformations are deterministic and not designed to resist cryptanalytic attacks. Do not use them to protect secrets.

## Use Cases

| Use Case | Description |
|---|---|
| **ID Obfuscation** | Turn sequential database IDs (1, 2, 3…) into seemingly random public-facing tokens so that users cannot guess or enumerate resources. |
| **Hashcode Mixing** | Improve the avalanche properties of hash functions by scrambling bits before final modular reduction. |
| **Lightweight Scrambling** | Produce deterministic, non-cryptographic shuffles for game seeds, procedural generation, or test-data factories. |
| **Encoding Schemes** | Build custom reversible encodings where data must round-trip without loss. |

---

## Quick Start

```csharp
using IdScrambler;

// Build a chain of reversible transforms
var chain = BijectionChain<uint>.Create()
    .Multiply(0x9E3779B9)    // Knuth's golden-ratio constant
    .XorShiftRight(16)
    .Xor(0xDEADBEEF);

// Obfuscate
uint encoded = chain.Forward(42);   // → some scrambled value

// Reverse — always gets the original back
uint decoded = chain.Inverse(encoded);  // → 42
```

### Signed integers

```csharp
int original = 42;
int scrambled = chain.Forward(original);   // extension method for int → uint → Forward → int
int restored  = chain.Inverse(scrambled);  // → 42
```

### Compiled delegates (hot paths)

```csharp
// One-time compilation cost (~50–200μs)
Func<uint, uint> forward = chain.CompileForward();
Func<uint, uint> inverse = chain.CompileInverse();

// Hot-path usage — single direct call, no virtual dispatch
uint encoded = forward(42);
uint decoded = inverse(encoded);  // → 42
```

---

## Available Transforms

All transforms are bijective and apply identically to both 32-bit and 64-bit widths unless noted.

| Method | Description | Self-Inverse |
|---|---|---|
| `.Xor(key)` | XOR with a constant | ✅ |
| `.Add(offset)` | Modular addition | ❌ |
| `.Multiply(oddFactor)` | Modular multiplication (factor must be odd) | ❌ |
| `.RotateBits(amount)` | Circular bit rotation left | ❌ |
| `.XorShiftRight(shift)` | `x ^ (x >>> shift)` | ❌ |
| `.XorShiftLeft(shift)` | `x ^ (x << shift)` | ❌ |
| `.PermuteBytes(perm)` | Rearrange bytes by permutation | ❌ |
| `.SubstituteNibbles(sbox)` | 4-bit S-box substitution per nibble | ❌ |
| `.ReverseBits()` | Reverse all bit positions | ✅ |
| `.GrayCode()` | Binary-to-Gray-code encoding | ❌ |
| `.Affine(oddFactor, offset)` | Combined multiply-then-add | ❌ |
| `.XorHighLow()` | XOR low half into high half | ✅ |

### Parameter validation

All parameters are validated **at construction time** (fail-fast):

- `Multiply` / `Affine` — factor must be odd → `ArgumentException`
- `RotateBits` — amount must be in `[1, N-1]` → `ArgumentException`
- `XorShiftRight`/`Left` — shift must be in `[1, N-1]` → `ArgumentException`
- `PermuteBytes` — must be a valid permutation of `{0..N/8-1}` → `ArgumentException`
- `SubstituteNibbles` — must be a permutation of `{0..15}` in a `byte[16]` → `ArgumentException`

---

## Presets

Pre-built chains for common scenarios:

```csharp
// Strong avalanche mixing (splitmix-style)
var mixed = Presets.StrongMix32.Forward(42);

// Lightweight ID obfuscation
var token = Presets.LightScramble64.Forward(42UL);
```

| Preset | Width | Style |
|---|---|---|
| `Presets.StrongMix32` | 32-bit | Splitmix-style, strong avalanche |
| `Presets.StrongMix64` | 64-bit | Splitmix-style, strong avalanche |
| `Presets.LightScramble32` | 32-bit | 3-step, fast |
| `Presets.LightScramble64` | 64-bit | 3-step, fast |

---

## Serialization

Chains can be defined declaratively in JSON or XML and deserialized at runtime. Numeric parameters accept decimal (`12345`) or hexadecimal (`0xDEADBEEF`) notation.

### JSON

```json
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
```

### XML

```xml
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
```

### API

```csharp
using IdScrambler.Serialization;

// Deserialize
IBijection<uint> chain = BijectionSerializer.FromJson<uint>(json);
IBijection<uint> chain = BijectionSerializer.FromXml<uint>(xml);

// Serialize
string json = BijectionSerializer.ToJson<uint>(chain);
string xml  = BijectionSerializer.ToXml<uint>(chain);
```

Invalid parameters in configuration throw `BijectionConfigException` with the step index and a descriptive message.

---

## ASP.NET Core Integration

The library provides transparent ID obfuscation at the API serialization boundary. The database stores original IDs; obfuscation happens only when writing/reading JSON or binding route parameters.

### 1. Register chains via DI

```csharp
// Programmatic registration
services.AddBijection(registry =>
{
    registry.Register("Order", BijectionChain<uint>.Create()
        .Multiply(0x9E3779B9)
        .XorShiftRight(16)
        .Xor(0xDEADBEEF));

    registry.Register("Product", BijectionChain<uint>.Create()
        .Multiply(0x45D9F3B1)
        .XorShiftRight(13)
        .Xor(0xCAFEBABE));
});

// Or load all chains from appsettings.json
services.AddBijection(configuration.GetSection("Bijection"));
```

### 2. Annotate DTO properties

```csharp
public class OrderDto
{
    [ObfuscatedId("Order")]                                    // numeric output
    public int Id { get; set; }

    [ObfuscatedId("Product", Format = ObfuscatedIdFormat.Base62)]  // alphanumeric
    public int ProductId { get; set; }

    public string Name { get; set; }  // not affected
}
```

### 3. Output formats

| Format | Alphabet | 32-bit Length | 64-bit Length |
|---|---|---|---|
| `Numeric` | digits | varies | varies |
| `Base64Url` | `A-Z a-z 0-9 - _` | 6 chars | 11 chars |
| `Base62` | `A-Z a-z 0-9` | 6 chars | 11 chars |

### 4. Model binding (route/query parameters)

```csharp
// Client requests: GET /api/orders/Kf8Tj2
[HttpGet("/api/orders/{id}")]
public Order Get([ObfuscatedId("Order", Format = ObfuscatedIdFormat.Base62)] int id)
{
    // id == 42 (decoded by model binder)
    return _db.Orders.Find(id);
}
```

### 5. URL generation helper

```csharp
// Inject BijectionRegistry
string token = registry.Encode("Order", 42, ObfuscatedIdFormat.Base62);
string url = $"/api/orders/{token}";  // → /api/orders/Kf8Tj2
```

### Full round-trip

```
DB:              Id = 42
App code:        works with int 42
JSON response:   { "id": "Kf8Tj2" }         ← Forward(42) → Base62
URL in response: /api/orders/Kf8Tj2
Client sends:    GET /api/orders/Kf8Tj2
Model binder:    Base62 → Inverse → 42
EF query:        WHERE Id = 42
```

---

## Design Principles

| Principle | Detail |
|---|---|
| **Correctness invariant** | For every `IBijection<T> b` and every `T x`: `b.Inverse(b.Forward(x)) == x` and `b.Forward(b.Inverse(x)) == x`. |
| **Deterministic** | Same input + same parameters = same output, always. No randomness. |
| **Allocation-free** | `Forward` and `Inverse` perform zero heap allocations. |
| **Immutable** | All `IBijection` instances are immutable and thread-safe after construction. |
| **Fail-fast** | Invalid parameters throw `ArgumentException` at construction time, never at `Forward`/`Inverse` time. |

---

## Project Structure

```
IdScrambler/
├── src/IdScrambler/
│   ├── IBijection.cs                         Core interfaces
│   ├── BijectionChain.cs                     Fluent chain + expression compilation
│   ├── BijectionExtensions.cs                Signed convenience (int, long)
│   ├── Presets.cs                            Pre-built chains
│   ├── SBoxPresets.cs                        Default S-box constant
│   ├── Transforms/                           11 bijection implementations
│   │   ├── XorBijection.cs
│   │   ├── AddBijection.cs
│   │   ├── MultiplyBijection.cs
│   │   ├── RotateBitsBijection.cs
│   │   ├── XorShiftBijection.cs
│   │   ├── BytePermutationBijection.cs
│   │   ├── NibbleSubstitutionBijection.cs
│   │   ├── BitReversalBijection.cs
│   │   ├── GrayCodeBijection.cs
│   │   ├── AffineBijection.cs
│   │   └── XorHighLowBijection.cs
│   ├── Serialization/                        JSON/XML serialization
│   │   ├── BijectionSerializer.cs
│   │   ├── JsonChainReader.cs
│   │   ├── XmlChainReader.cs
│   │   └── BijectionConfigException.cs
│   └── Integration/                          ASP.NET Core integration
│       ├── BijectionRegistry.cs
│       ├── BijectionServiceExtensions.cs
│       ├── ObfuscatedIdAttribute.cs
│       ├── ObfuscatedIdFormat.cs
│       ├── ObfuscatedIdModifier.cs
│       ├── ObfuscatedIdModelBinder.cs
│       ├── BijectionRegistryExtensions.cs
│       └── Base62.cs
└── tests/IdScrambler.Tests/
    ├── RoundTripTests.cs                     Round-trip property tests
    ├── KnownAnswerTests.cs                   Hand-verified values
    ├── ChainTests.cs                         Chain, compilation, serialization
    └── ValidationTests.cs                    Invalid parameter rejection
```

---

## License

See [LICENSE](LICENSE).
