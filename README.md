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
| `.XorRotate(a, b)` | `x ^ RotL(x,a) ^ RotL(x,b)` — rotation-based diffusion, no bits lost off the ends | ❌ |
| `.Quadratic()` | `x·(2x+1) mod 2ᴺ` (RC6's diffusion map) — inverse via Newton–Hensel lifting | ❌ |
| `.Clmul(oddFactor)` | Carry-less (GF(2)) multiplication — hardware-accelerated via PCLMULQDQ when available | ❌ |
| `.Crc32()` | One CRC-32C step — hardware-accelerated via SSE4.2/ARMv8; **32-bit chains only** | ❌ |
| `.Rxs(selectorBits, baseShift)` | Data-dependent xorshift (PCG's "RXS") — top bits select the shift amount | ❌ |

Measured cost of every transform is listed under [Benchmarks](#benchmarks). All transforms cost roughly 1–5 ns per call in both directions, with one deliberate exception — `Quadratic()` has a fast forward and a ~30× slower inverse; see [Quadratic: asymmetric cost](#quadratic-asymmetric-cost) before using it in bulk-decode paths.

### Parameter validation

All parameters are validated **at construction time** (fail-fast):

- `Multiply` / `Affine` — factor must be odd → `ArgumentException`
- `RotateBits` — amount must be in `[1, N-1]` → `ArgumentException`
- `XorShiftRight`/`Left` — shift must be in `[1, N-1]` → `ArgumentException`
- `PermuteBytes` — must be a valid permutation of `{0..N/8-1}` → `ArgumentException`
- `SubstituteNibbles` — must be a permutation of `{0..15}` in a `byte[16]` → `ArgumentException`
- `XorRotate` — both amounts in `[1, N-1]` and distinct → `ArgumentException`
- `Clmul` — factor must be odd → `ArgumentException`
- `Crc32` — chain type must be `uint` → `NotSupportedException`
- `Rxs` — `baseShift ≥ selectorBits` and `baseShift + 2^selectorBits − 1 ≤ N−1` → `ArgumentException`

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

The declared `width` is validated during deserialization and must match the target generic type (`uint` => `32`, `ulong` => `64`). Invalid configuration throws `BijectionConfigException` before any chain is created.

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
    { "type": "XorHighLow" },
    { "type": "XorRotate", "a": 13, "b": 27 },
    { "type": "Quadratic" },
    { "type": "Clmul", "factor": "0x9E3779B9" },
    { "type": "Crc32" },
    { "type": "Rxs", "selectorBits": 4, "baseShift": 4 }
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
  <XorRotate a="13" b="27" />
  <Quadratic />
  <Clmul factor="0x9E3779B9" />
  <Crc32 />
  <Rxs selectorBits="4" baseShift="4" />
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

String token decoding is strict:

- `Base62` tokens must be exactly 6 characters for 32-bit values and 11 for 64-bit values
- `Base64Url` tokens must be exactly 6 characters for 32-bit values and 11 for 64-bit values
- malformed or oversized tokens throw instead of being truncated or wrapped

---

## ASP.NET Core Integration

The library provides transparent ID obfuscation at the API serialization boundary. The database stores original IDs; obfuscation happens only when writing/reading JSON or binding route parameters.

`services.AddBijection(...)` configures both minimal APIs (`HttpJsonOptions`) and MVC controllers (`Microsoft.AspNetCore.Mvc.JsonOptions`), so the same `[ObfuscatedId]` attributes work in either pipeline.

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

`Numeric` format round-trips the full unsigned obfuscated domain for both widths, including 64-bit values above `long.MaxValue`.

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
| **Immutable** | `BijectionChain<T>` and all built-in transforms are immutable and thread-safe. Each fluent call returns a new chain instance with the extra step appended. |
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
│   ├── Transforms/                           16 bijection implementations
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
│   │   ├── XorHighLowBijection.cs
│   │   ├── XorRotateBijection.cs
│   │   ├── QuadraticBijection.cs
│   │   ├── ClmulBijection.cs
│   │   ├── Crc32Bijection.cs
│   │   └── RxsBijection.cs
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
├── tests/IdScrambler.Tests/
│   ├── RoundTripTests.cs                     Round-trip property tests
│   ├── KnownAnswerTests.cs                   Hand-verified values
│   ├── ChainTests.cs                         Chain, compilation, serialization
│   ├── ValidationTests.cs                    Invalid parameter rejection
│   └── NewTransformTests.cs                  XorRotate/Quadratic/Clmul/Crc32/Rxs (incl. exhaustive 16-bit)
└── benchmarks/IdScrambler.Benchmarks/        BenchmarkDotNet suite (all transforms, both widths)
```

---

## Benchmarks

Per-transform latency of the compiled delegates (forward and inverse, 32- and 64-bit), plus the presets:

```bash
dotnet run -c Release --project benchmarks/IdScrambler.Benchmarks -- --filter * --join

# Only one class, e.g. 32-bit transforms:
dotnet run -c Release --project benchmarks/IdScrambler.Benchmarks -- --filter *Transform32*
```

### Results

Measured with BenchmarkDotNet v0.15.8 on an AMD Ryzen AI 9 HX 370, .NET 10.0.9 (X64 RyuJIT, x86-64-v4), Windows 11. Numbers are nanoseconds per call through a compiled delegate, measured as serial-dependency latency (`v = f(v)`). Every benchmark allocated **0 bytes**.

The `Identity` row is an *empty* chain — it measures pure delegate-call overhead (~1.1–1.25 ns) and is the baseline to subtract from every other row. Most transforms sit at or barely above that baseline, i.e. their real cost is a fraction of a nanosecond.

| Transform | Forward 32 | Inverse 32 | Forward 64 | Inverse 64 |
|---|--:|--:|--:|--:|
| `Identity` (baseline) | 1.25 | 1.25 | 1.10 | 1.14 |
| `Xor` | 1.26 | 1.27 | 1.38 | 1.20 |
| `Add` | 1.28 | 1.25 | 1.26 | 1.25 |
| `Multiply` | 1.12 | 1.10 | 1.24 | 1.21 |
| `RotateBits` | 1.06 | 1.17 | 1.09 | 1.10 |
| `XorShiftRight` | 1.15 | 1.10 | 1.05 | 1.04 |
| `XorShiftLeft` | 1.10 | 1.16 | 1.04 | 1.14 |
| `PermuteBytes` | 1.28 | 1.38 | 2.14 | 2.14 |
| `SubstituteNibbles` | 3.05 | 3.05 | 5.11 | 5.25 |
| `ReverseBits` | 2.73 | 2.73 | 3.36 | 3.35 |
| `GrayCode` | 1.13 | 1.97 | 1.11 | 2.35 |
| `Affine` | 1.24 | 1.27 | 1.26 | 1.25 |
| `XorHighLow` | 1.30 | 1.27 | 1.99 | 2.01 |
| `XorRotate` | 1.20 | 2.96 | 2.06 | 4.56 |
| `Quadratic` | 1.13 | **39.05** | 1.25 | **55.32** |
| `Clmul` | 2.33 | 2.32 | 2.32 | 2.32 |
| `Crc32` (32-bit only) | 1.25 | 2.05 | — | — |
| `Rxs` | 1.03 | 1.36 | 1.26 | 1.46 |

Preset chains for context (full multi-step pipelines, round-trip-capable):

| Preset | Forward | Inverse |
|---|--:|--:|
| `LightScramble32` (3 steps) | 1.21 | 1.21 |
| `StrongMix32` (5 steps) | 2.34 | 2.34 |
| `LightScramble64` (3 steps) | 1.32 | 1.34 |
| `StrongMix64` (5 steps) | 2.38 | 3.58 |

Notes on the measurements:

- **Cost is per step, per call.** A chain pays the sum of its steps' costs once per `Forward`/`Inverse` call, regardless of chain length bookkeeping — the compiled delegate inlines all steps into one body.
- **`Clmul` and `Crc32` are hardware-accelerated** via the PCLMULQDQ and SSE4.2 CRC32 instructions (ARMv8 CRC32 on ARM), with automatic software fallbacks. On CPUs without these instructions, expect `Clmul` to slow to roughly N/2 shift-XOR pairs and `Crc32` forward to four table lookups; `Crc32` inverse is always table-based and unaffected.
- **`XorRotate`'s inverse** runs the log₂ N doubling steps (5 for 32-bit, 6 for 64-bit), which is why it costs ~2–4× its forward — still cheaper than the existing `SubstituteNibbles`.
- **`Rxs` is effectively free** in both directions despite being data-dependent.

### Quadratic: asymmetric cost

`Quadratic()` is the one transform with a deliberately asymmetric cost profile, and it deserves its own explanation.

**Why the asymmetry exists.** The forward direction `y = x·(2x+1) mod 2ᴺ` is two adds and a multiply — it benchmarks at delegate overhead, i.e. effectively free. But unlike `Multiply`, whose inverse is a single multiply by a precomputed constant, the quadratic has **no closed-form inverse**. Inverting means *solving* `2x² + x = y`, which the library does by Newton–Hensel lifting: starting from `x₀ = y` (correct mod 2), each iteration doubles the number of correct low bits, so 5 iterations are needed for 32-bit and 6 for 64-bit — and each iteration embeds a modular-reciprocal computation of the derivative `4x+1`. That's the 39 ns / 55 ns you see in the table: ~30× the other transforms **relatively**, yet still well under a tenth of a microsecond **absolutely**.

**Why it's worth having anyway.** `Quadratic` is the only transform in the library with high-degree arithmetic non-linearity over the full word. `Multiply` is linear (bit 0 passes through unchanged); the nibble S-box is non-linear but only within isolated 4-bit lanes. A `Quadratic` step composed with a diffusion step (`XorRotate`, `XorShiftRight`) is substantially harder to peel off with the linear-algebra techniques that recover purely GF(2)-linear chains from a handful of known input/output pairs — relevant here precisely because sequential IDs make known plaintext the realistic scenario.

**When the cost matters — and when it doesn't.** Match the slow direction to the rare direction of your workload:

- ✅ **ASP.NET ID obfuscation (the primary use case): use freely.** The two directions have very different call frequencies. `Forward` (encode) runs for *every* ID written into a JSON response — a list endpoint returning 100 DTOs with two obfuscated IDs each performs 200 Forward calls, all effectively free. `Inverse` (decode) runs once per obfuscated route/query parameter — typically 0–2 per request via the model binder. At ~40–55 ns each, that's ~0.1 µs per request against a processing budget measured in milliseconds: four to five orders of magnitude below observability.
- ⚠️ **Bulk-decode endpoints: measure first.** A request body carrying an array of obfuscated IDs (batch delete, import, sync) pays one Inverse per element. The absolute numbers remain small — 100,000 decodes ≈ 5 ms total — but it is no longer free.
- ❌ **Inverse-hot loops: avoid.** If decoding is your hot path (e.g. streaming a large token file back to IDs), the 30–50× gap against the symmetric transforms is a real throughput difference. Build such chains from `XorRotate`, `Clmul`, `Rxs`, or `Crc32` instead — all invert in 1.4–4.6 ns.

**Rule of thumb:** use `Quadratic` in chains whose decode path is per-request; avoid it in chains that bulk-decode. The cost is paid once per `Inverse` call on the whole chain, not multiplied by the other steps.

---

## License

See [LICENSE](LICENSE).
