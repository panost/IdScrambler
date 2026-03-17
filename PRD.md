# PRD: IdScrambler — Integer Bijection Mapping Library

## 1. Overview

**IdScrambler** is a .NET 10 class library providing a fluent, chainable API for applying reversible (bijective) transformations on **32-bit** (`int` / `uint`) and **64-bit** (`long` / `ulong`) integers. Every transformation is guaranteed to be a bijection over the full 2ᴺ domain (N = 32 or 64) — meaning it maps every possible integer to a unique output and can be perfectly reversed.

### Use Cases

| Use Case | Description |
|---|---|
| **ID Obfuscation** | Turn sequential database IDs (1, 2, 3…) into seemingly random public-facing tokens so that users cannot guess or enumerate resources. |
| **Hashcode Mixing** | Improve the avalanche properties of hash functions by scrambling bits before final modular reduction. |
| **Lightweight Scrambling** | Produce deterministic, non-cryptographic shuffles for game seeds, procedural generation, or test-data factories. |
| **Encoding Schemes** | Build custom reversible encodings where data must round-trip without loss (e.g., compact wire formats, save-file integers). |

> [!CAUTION]
> This library provides **obfuscation, not encryption**. The transformations are deterministic and not designed to resist cryptanalytic attacks. Do not use them to protect secrets.

---

## 2. Bijection Methods

Each method below defines a forward transform `F(x) → y` and its exact inverse `F⁻¹(y) → x`, operating on N-bit integers mod 2ᴺ (N = 32 or 64). All methods apply identically to both widths unless noted otherwise.

---

### 2.1 XOR with Constant

| Property | Value |
|---|---|
| **Parameter** | `key`: any `uint` / `ulong` value |
| **Forward** | `y = x ^ key` |
| **Inverse** | `x = y ^ key` (self-inverse) |
| **Strength** | Flips specific bits. Simple but adds no diffusion on its own. |

**Why it's a bijection:** XOR is its own inverse — `(x ^ k) ^ k == x` for all `x`.

---

### 2.2 Addition with Constant (Modular Addition)

| Property | Value |
|---|---|
| **Parameter** | `offset`: any `uint` / `ulong` value |
| **Forward** | `y = x + offset (mod 2ᴺ)` |
| **Inverse** | `x = y − offset (mod 2ᴺ)` |
| **Strength** | Shifts the entire number space. Cascading carry propagation provides some bit mixing. |

**Why it's a bijection:** Addition mod 2ᴺ is a group operation; every element has a unique additive inverse.

---

### 2.3 Modular Multiplication

| Property | Value |
|---|---|
| **Parameter** | `factor`: an **odd** `uint` / `ulong` (coprime to 2ᴺ) |
| **Forward** | `y = x × factor (mod 2ᴺ)` |
| **Inverse** | `x = y × factor⁻¹ (mod 2ᴺ)`, where `factor⁻¹` is computed via the extended Euclidean algorithm |
| **Strength** | Good diffusion — small input changes cascade across many bits. |

**Why it's a bijection:** Multiplication by a number coprime to the modulus is invertible in ℤ/nℤ. Since `2ᴺ` has only the prime factor 2, any odd number is coprime to it.

> [!IMPORTANT]
> The library must validate at construction time that the factor is odd and reject even values with a descriptive exception.

> [!TIP]
> **Recommended constants (Knuth's Multiplicative Hash,** *The Art of Computer Programming*, Vol. 3**):** derived from the golden ratio `⌊2ᴺ / φ⌋`. The golden ratio ensures that sequential inputs are scattered as uniformly as possible across the output space, maximizing the minimum gap between consecutive hashed values.
>
> | Width | Factor | Inverse |
> |---|---|---|
> | **32-bit** | `0x9E3779B9` (`2654435769`) | `0x144CBC89` (`340573321`) |
> | **64-bit** | `0x9E3779B97F4A7C15` (`11400714819323198485`) | `0x6A09E667F3BCC909` (`7640891576956012809`) |

---

### 2.4 Bit Rotation (Circular Shift)

| Property | Value |
|---|---|
| **Parameter** | `amount`: `int` in range `[1, N−1]` (i.e. `[1,31]` for 32-bit, `[1,63]` for 64-bit) |
| **Forward** | `y = (x << amount) | (x >>> (N − amount))` |
| **Inverse** | `x = (y >>> amount) | (y << (N − amount))` |
| **Validation** | `amount` must satisfy `1 <= amount <= N-1`. Values of `0`, `N`, or negative values throw `ArgumentException`. |
| **Strength** | Moves bits without destroying them. Excellent when composed with arithmetic operations. |

**Why it's a bijection:** No information is lost — all N bits are preserved, just repositioned circularly.

---

### 2.5 XOR-Shift

Only two forms are supported — both are provably bijective:

| Form | Forward | Inverse |
|---|---|---|
| **Right** | `y = x ^ (x >>> shift)` | Apply `x ^= (x >>> shift)` for `ceil(N/shift)` iterations |
| **Left** | `y = x ^ (x << shift)` | Apply `x ^= (x << shift)` for `ceil(N/shift)` iterations |

| Property | Value |
|---|---|
| **Parameter** | `shift`: `int` in range `[1, N−1]` (i.e. `[1,31]` for 32-bit, `[1,63]` for 64-bit) |
| **Validation** | `shift` must satisfy `1 <= shift <= N-1`. Values of `0`, `N`, or negative values throw `ArgumentException`. Mixed-direction forms (e.g., `x ^ (x >>> a) ^ (x << b)`) are **not** supported — they are not guaranteed to be bijective without further constraints. |
| **Strength** | Creates bit diffusion — a single changed bit affects multiple output bits. Core building block of many PRNG mixing functions (e.g., `xorshift`, `splitmix`). |

**Why it's a bijection:** Each form is a triangular (unipotent) matrix over GF(2)ᴺ, which is always invertible. Right-shift produces an upper-triangular matrix; left-shift produces a lower-triangular matrix.

---

### 2.6 Byte Permutation

| Property | Value |
|---|---|
| **Parameter** | `permutation`: a permutation of byte positions (`{0..3}` for 32-bit, `{0..7}` for 64-bit) |
| **Forward** | Rearrange the 4 or 8 bytes of the integer according to the permutation |
| **Inverse** | Apply the inverse permutation |
| **Strength** | Moves data at the byte level. Cheap and useful for breaking positional patterns. |

**Why it's a bijection:** A permutation of a finite set is by definition invertible.

> [!NOTE]
> The library should provide named presets for common permutations: `Reverse` (3,2,1,0), `Swap` inner/outer pairs, `RotateLeft`, `RotateRight`.

---

### 2.7 Nibble Substitution (S-Box)

| Property | Value |
|---|---|
| **Parameter** | `sbox`: a `byte[16]` array that is a permutation of `{0..15}` |
| **Forward** | Replace each 4-bit nibble of the integer using the S-box lookup |
| **Inverse** | Replace each nibble using the inverse S-box |
| **Strength** | Non-linear transformation — the only method in this set that is not affine over GF(2). Critical for avalanche quality. |

**Why it's a bijection:** Each nibble is independently mapped through a permutation; the combined transform is a bijection on 2ᴺ (8 nibbles for 32-bit, 16 nibbles for 64-bit).

> [!TIP]
> The library should ship with at least one built-in 4-bit S-box preset that is a valid permutation of `{0..15}` with good non-linearity properties. The specific S-box values must be defined as a constant array in the source code and verified by tests to be a valid permutation.

---

### 2.8 Bit Reversal

| Property | Value |
|---|---|
| **Parameter** | None (parameterless) |
| **Forward** | Reverse the bit order of the integer (bit 0 ↔ bit N−1, bit 1 ↔ bit N−2, …) |
| **Inverse** | Same operation (self-inverse) |
| **Strength** | Radically redistributes bit significance. Useful as a position-breaking step. |

**Why it's a bijection:** Bit reversal is a self-inverse permutation of the N bit positions.

---

### 2.9 Gray Code

| Property | Value |
|---|---|
| **Parameter** | None (parameterless) |
| **Forward** | `y = x ^ (x >>> 1)` |
| **Inverse** | Standard iterative Gray-to-binary decode |
| **Strength** | Adjacent integers differ by exactly 1 bit in the output — useful when that Hamming-distance-1 property is desirable. |

**Why it's a bijection:** The encoding is a well-known bijection; every binary value corresponds to exactly one Gray code.

---

### 2.10 Affine Transform (Combined Multiply-Add)

| Property | Value |
|---|---|
| **Parameter** | `factor` (odd `uint` / `ulong`), `offset` (`uint` / `ulong`) |
| **Forward** | `y = (x × factor + offset) mod 2ᴺ` |
| **Inverse** | `x = (y − offset) × factor⁻¹ mod 2ᴺ` |
| **Strength** | Combines multiplication and addition for stronger mixing in a single step. Classic affine cipher over ℤ/2ᴺℤ. |

**Why it's a bijection:** Composition of two bijections (multiply-by-odd, then add-constant) is itself a bijection.

---

### 2.11 XOR High-Low

| Property | Value |
|---|---|
| **Parameter** | None (parameterless) |
| **Forward (32-bit)** | `y = x ^ ((x & 0xFFFF) << 16)` — XOR the low 16 bits into the high 16 bits |
| **Forward (64-bit)** | `y = x ^ ((x & 0xFFFFFFFF) << 32)` — XOR the low 32 bits into the high 32 bits |
| **Inverse** | Same operation (self-inverse) |
| **Strength** | Mixes low-order bits into high-order bits. Cheap, branchless, and useful for breaking patterns where only the low half varies (e.g., sequential IDs). |

**Why it's a bijection:** The low N/2 bits are preserved unchanged (`y_low = x_low`), so the original high bits can be recovered: `x_high = y_high ⊕ y_low`. Applying the operation twice yields the identity.

---

## 3. API Design

### 3.1 Core Interface

The library uses a generic interface parameterized on the unsigned integer type:

```csharp
namespace IdScrambler;

/// <summary>
/// Represents a single reversible transformation on an unsigned integer.
/// </summary>
/// <typeparam name="T">The unsigned integer type: uint or ulong.</typeparam>
public interface IBijection<T> where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    /// <summary>Apply the forward transformation.</summary>
    T Forward(T value);

    /// <summary>Apply the inverse transformation, reversing Forward.</summary>
    T Inverse(T value);
}

/// <summary>
/// Internal interface implemented by each transform to support expression-tree compilation.
/// </summary>
internal interface IBijectionStep<T> : IBijection<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    /// <summary>Build an expression that computes the forward transform on the input expression.</summary>
    Expression BuildForwardExpression(Expression input);

    /// <summary>Build an expression that computes the inverse transform on the input expression.</summary>
    Expression BuildInverseExpression(Expression input);
}
```

Concrete type aliases for discoverability:

```csharp
/// <summary>32-bit bijection.</summary>
public interface IBijection32 : IBijection<uint> { }

/// <summary>64-bit bijection.</summary>
public interface IBijection64 : IBijection<ulong> { }
```

### 3.2 Chain (Pipeline)

A `BijectionChain<T>` composes multiple `IBijection<T>` steps into a single forward/inverse pipeline. The inverse walks the chain in reverse order.

```csharp
public sealed class BijectionChain<T> : IBijection<T>
    where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
    public static BijectionChain<T> Create() => new();

    public BijectionChain<T> Xor(T key);
    public BijectionChain<T> Add(T offset);
    public BijectionChain<T> Multiply(T oddFactor);
    public BijectionChain<T> RotateBits(int amount);
    public BijectionChain<T> XorShiftRight(int shift);
    public BijectionChain<T> XorShiftLeft(int shift);
    public BijectionChain<T> PermuteBytes(byte[] permutation);
    public BijectionChain<T> SubstituteNibbles(byte[] sbox);
    public BijectionChain<T> ReverseBits();
    public BijectionChain<T> GrayCode();
    public BijectionChain<T> Affine(T oddFactor, T offset);
    public BijectionChain<T> XorHighLow();

    public T Forward(T value);   // applies steps 0 → N
    public T Inverse(T value);   // applies inverse steps N → 0

    /// <summary>
    /// Compile the forward chain into a single delegate with all steps inlined.
    /// </summary>
    public Func<T, T> CompileForward();

    /// <summary>
    /// Compile the inverse chain into a single delegate with all steps inlined.
    /// </summary>
    public Func<T, T> CompileInverse();
}
```

Convenience aliases:

```csharp
public sealed class BijectionChain32 : BijectionChain<uint> { }
public sealed class BijectionChain64 : BijectionChain<ulong> { }
```

### 3.3 Expression Compilation

`CompileForward()` and `CompileInverse()` use `System.Linq.Expressions` to build an expression tree that **inlines all arithmetic** from every step in the chain, then compiles it to a `Func<T, T>` delegate via `Expression.Lambda<T>.Compile()`.

```csharp
var chain = BijectionChain<uint>.Create()
    .Xor(0xDEADBEEF)
    .Multiply(0x9E3779B9)
    .XorShiftRight(16);

// One-time compilation cost (~50–200μs)
Func<uint, uint> forward = chain.CompileForward();
Func<uint, uint> inverse = chain.CompileInverse();

// Hot-path usage — single direct call, no virtual dispatch
uint encoded = forward(42);
uint decoded = inverse(encoded);  // == 42
```

**How it works:** Each transform implements `IBijectionStep<T>.BuildForwardExpression` / `BuildInverseExpression`, which returns an `Expression` subtree for its operation. The chain concatenates these into a single `BlockExpression`, wraps it in a lambda, and calls `Compile()`. Loops (e.g., XorShift inverse) are **unrolled** at compile time into a fixed number of XOR+shift expressions.

| Aspect | `Forward(x)` via chain | Compiled `Func<T, T>` |
|---|---|---|
| **First call** | Instant | ~50–200μs compile cost |
| **Per-call** | Virtual dispatch × N steps | Single direct call, fully inlined |
| **Best for** | One-off or few calls | Hot paths with millions of calls |

> [!TIP]
> The compiled delegate is a regular `Func<T, T>` that can be cached in a `static readonly` field for maximum performance.

### 3.4 Signed/Unsigned Convenience

All internal math operates on unsigned types. The library provides extension methods for `int` and `long` that reinterpret-cast via `unchecked` and back.

```csharp
public static class BijectionExtensions
{
    public static int Forward(this IBijection<uint> bijection, int value);
    public static int Inverse(this IBijection<uint> bijection, int value);

    public static long Forward(this IBijection<ulong> bijection, long value);
    public static long Inverse(this IBijection<ulong> bijection, long value);
}
```

### 3.5 Preset Chains

The library ships pre-built chains for common scenarios, for both widths:

```csharp
public static class Presets
{
    /// <summary>Strong avalanche mixing similar to splitmix32.</summary>
    public static IBijection<uint> StrongMix32 { get; }

    /// <summary>Strong avalanche mixing similar to splitmix64.</summary>
    public static IBijection<ulong> StrongMix64 { get; }

    /// <summary>Lightweight but fast scramble for 32-bit ID obfuscation.</summary>
    public static IBijection<uint> LightScramble32 { get; }

    /// <summary>Lightweight but fast scramble for 64-bit ID obfuscation.</summary>
    public static IBijection<ulong> LightScramble64 { get; }
}
```

### 3.6 Chain Serialization (JSON / XML)

Chains can be defined declaratively in configuration files and deserialized at runtime. This allows transformations to be changed without recompilation.

#### JSON Format

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

#### XML Format

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

> [!NOTE]
> Numeric parameters accept decimal (`12345`) or hexadecimal (`0xDEADBEEF`) notation. For 64-bit chains, set `width` to `64` and use `ulong`-range values.

#### Deserialization API

```csharp
public static class BijectionSerializer
{
    /// <summary>Deserialize a chain from a JSON string.</summary>
    public static IBijection<T> FromJson<T>(string json)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>;

    /// <summary>Deserialize a chain from a JSON stream (e.g., config file).</summary>
    public static IBijection<T> FromJson<T>(Stream stream)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>;

    /// <summary>Deserialize a chain from an XML string.</summary>
    public static IBijection<T> FromXml<T>(string xml)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>;

    /// <summary>Deserialize a chain from an XML stream.</summary>
    public static IBijection<T> FromXml<T>(Stream stream)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>;

    /// <summary>Serialize a chain to JSON.</summary>
    public static string ToJson<T>(IBijection<T> chain)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>;

    /// <summary>Serialize a chain to XML.</summary>
    public static string ToXml<T>(IBijection<T> chain)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>;
}
```

> [!IMPORTANT]
> Deserialization performs the same fail-fast validation as the fluent API — invalid parameters (even factors, out-of-range shifts, non-permutation arrays) throw `BijectionConfigException` with the step index and a descriptive message.

### 3.7 ASP.NET Core Integration

The library provides integration with **System.Text.Json** and **Microsoft.Extensions.DependencyInjection** so that bijection chains can be applied transparently at the API serialization boundary. The database stores original IDs; obfuscation happens only when writing/reading JSON.

#### BijectionRegistry

Chains are registered by **string name** in a `BijectionRegistry`. Names are case-insensitive.

```csharp
public sealed class BijectionRegistry
{
    /// <summary>Register a named chain.</summary>
    public void Register<T>(string name, IBijection<T> chain)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>;

    /// <summary>Resolve a chain by name. Throws KeyNotFoundException if not found.</summary>
    public IBijection<T> Resolve<T>(string name)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>;

    /// <summary>Try to resolve a chain by name.</summary>
    public bool TryResolve<T>(string name, out IBijection<T>? chain)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>;
}
```

#### DI Registration

Chains can be registered programmatically or loaded from configuration:

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

// Or load all chains from configuration section
services.AddBijection(configuration.GetSection("Bijection"));
```

#### Configuration via `appsettings.json`

When loading from configuration, each key in the section becomes the chain name:

```json
{
  "Bijection": {
    "Order": {
      "width": 32,
      "steps": [
        { "type": "Multiply", "factor": "0x9E3779B9" },
        { "type": "XorShiftRight", "shift": 16 },
        { "type": "Xor", "key": "0xDEADBEEF" }
      ]
    },
    "Product": {
      "width": 32,
      "steps": [
        { "type": "Multiply", "factor": "0x45D9F3B1" },
        { "type": "XorShiftRight", "shift": 13 },
        { "type": "Xor", "key": "0xCAFEBABE" }
      ]
    }
  }
}
```

#### `[ObfuscatedId]` Attribute

Apply per-property to opt into automatic obfuscation during JSON serialization/deserialization. The attribute references the chain by its registry name.

```csharp
public class OrderDto
{
    [ObfuscatedId("Order")]    // uses the "Order" chain
    public int Id { get; set; }

    [ObfuscatedId("Product")]  // uses the "Product" chain
    public int ProductId { get; set; }

    public string Name { get; set; }  // not affected
}
```

The `Format` property controls the output representation:

```csharp
public enum ObfuscatedIdFormat
{
    /// <summary>Output as a numeric value (default). JSON: 3819274103</summary>
    Numeric,

    /// <summary>Output as a Base64Url-encoded string (A-Z, a-z, 0-9, -, _). JSON: "Pno3xQ"</summary>
    Base64Url,

    /// <summary>Output as a Base62-encoded string (A-Z, a-z, 0-9 only). JSON: "Kf8Tj2"</summary>
    Base62
}

[ObfuscatedId("Order", Format = ObfuscatedIdFormat.Base62)]
public int Id { get; set; }
```

**String encoding** converts the obfuscated integer's bytes into a URL-safe string:

| Format | Alphabet | 32-bit Length | 64-bit Length |
|---|---|---|---|
| **Base64Url** | `A-Z a-z 0-9 - _` | 6 chars | 11 chars |
| **Base62** | `A-Z a-z 0-9` | 6 chars | 11 chars |

Base62 is recommended for URL-facing tokens — purely alphanumeric, no special characters, always selects as one word on double-click.

**Behavior:**
- **Serialization (write):** `Forward(originalId)` → obfuscated value written to JSON
- **Deserialization (read):** `Inverse(obfuscatedValue)` → original ID restored in the DTO
- **Missing chain name:** throws `InvalidOperationException` at serialization time with a descriptive message

#### Implementation Mechanism

A `JsonConverterFactory` alone cannot implement this — it operates at the **type** level (`int`) and has no visibility into which property the value belongs to, so it cannot read per-property attributes to select the correct chain.

The correct mechanism is a **type-info modifier** on `DefaultJsonTypeInfoResolver`. The `services.AddBijection()` call registers a `IJsonTypeInfoResolver` modifier that:

1. At contract-creation time, scans each `JsonPropertyInfo` for the `[ObfuscatedId]` attribute
2. For each annotated property, creates a per-property `JsonConverter<int>` (or `JsonConverter<long>`) bound to the named chain from the `BijectionRegistry`
3. Assigns this converter to `JsonPropertyInfo.CustomConverter`

```csharp
// Registered automatically by services.AddBijection()
services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
    {
        Modifiers = { ObfuscatedIdModifier.Apply(registry) }
    };
});
```

> [!IMPORTANT]
> This requires .NET 8+ (`DefaultJsonTypeInfoResolver` with modifiers). The modifier runs once per type at contract-creation time, not per serialization call, so there is no per-request overhead.

#### Model Binding (Route / Query Parameters)

For URLs like `/api/orders/Pno3xQ`, a custom `IModelBinder` decodes the obfuscated ID from route and query parameters.

```csharp
// Client requests: GET /api/orders/Pno3xQ
[HttpGet("/api/orders/{id}")]
public Order Get([ObfuscatedId("Order", Format = ObfuscatedIdFormat.Base64Url)] int id)
{
    // id == 42 (decoded by model binder: Base64Url → uint → Inverse → int)
    return _db.Orders.Find(id);
}
```

The model binder:
1. Reads the `[ObfuscatedId]` attribute on the parameter
2. Resolves the named chain from the `BijectionRegistry`
3. Decodes the value (Base64Url → bytes → `uint`/`ulong`, or parse as numeric)
4. Applies `Inverse` to recover the original ID

#### URL Generation Helper

An extension method makes it easy to generate obfuscated IDs for URLs and links:

```csharp
// Inject IBijectionRegistry
public string GetOrderUrl(int orderId)
{
    string token = registry.Encode("Order", orderId, ObfuscatedIdFormat.Base64Url);
    return $"/api/orders/{token}";  // → /api/orders/Pno3xQ
}
```

#### Full Round-Trip

```
DB:              Id = 42
App code:        works with int 42
JSON response:   { "id": "Pno3xQ" }        ← Forward(42) → Base64Url
URL in response: /api/orders/Pno3xQ
Client sends:    GET /api/orders/Pno3xQ
Model binder:    Base64Url → Inverse → 42
EF query:        WHERE Id = 42
```

---

## 4. Design Principles

| Principle | Detail |
|---|---|
| **Correctness invariant** | For every `IBijection<T> b` and every `T x`: `b.Inverse(b.Forward(x)) == x` and `b.Forward(b.Inverse(x)) == x`. |
| **Deterministic** | Same input + same parameters = same output, always. No randomness involved. |
| **Allocation-free** | `Forward` and `Inverse` perform zero heap allocations. All state is captured at construction time. |
| **Immutable** | All `IBijection` instances are immutable and thread-safe after construction. |
| **Fail-fast validation** | Invalid parameters (e.g., an even multiplication factor, an out-of-range shift) throw `ArgumentException` at construction time, never at `Forward`/`Inverse` time. |

---

## 5. Testing Strategy

### 5.1 Round-Trip Property Tests

For every method, parameter configuration, and both widths: test on a representative set of inputs (0, 1, `T.MaxValue`, powers-of-two, random samples) that `Inverse(Forward(x)) == x` and `Forward(Inverse(x)) == x`.

### 5.2 Bijectivity Assurance

Bijectivity is primarily assured by **mathematical proof** (each transform is provably a bijection by construction) and **property-based tests** (round-trip on large random samples). Exhaustive 2³² collision testing is **not** a CI requirement — it is an optional one-off verification that developers may run locally for added confidence on parameterless transforms.

### 5.3 Chain Round-Trip

Build randomized chains of 5–10 steps with random parameters and verify the round-trip property on a large sample.

### 5.4 Known-Answer Tests

For each method, include a small table of hand-verified `(input, parameter, expected_output)` triples.

### 5.5 Edge Cases

- Multiplication by 1 (identity)
- Addition of 0 (identity)
- XOR with 0 (identity)
- Rotation by 0. or N — must throw `ArgumentException`
- XorShift by 0 or N — must throw `ArgumentException`
- Full chain of identities

### 5.6 Validation Rejection Tests

Verify that construction with invalid parameters throws the expected exceptions:
- Even multiplication factor → `ArgumentException`
- Rotation amount `0`, `N`, or negative → `ArgumentException`
- XorShift amount `0`, `N`, or negative → `ArgumentException`
- Non-permutation S-box array → `ArgumentException`
- Byte permutation with duplicate or out-of-range indices → `ArgumentException`

---

## 6. Project Structure

```
IdScrambler/
├── src/
│   └── IdScrambler/
│       ├── IdScrambler.csproj           # targets net10.0
│       ├── IBijection.cs
│       ├── BijectionChain.cs
│       ├── Transforms/
│       │   ├── XorBijection.cs
│       │   ├── AddBijection.cs
│       │   ├── MultiplyBijection.cs
│       │   ├── RotateBitsBijection.cs
│       │   ├── XorShiftBijection.cs
│       │   ├── BytePermutationBijection.cs
│       │   ├── NibbleSubstitutionBijection.cs
│       │   ├── BitReversalBijection.cs
│       │   ├── GrayCodeBijection.cs
│       │   ├── AffineBijection.cs
│       │   └── XorHighLowBijection.cs
│       ├── Presets.cs
│       ├── BijectionExtensions.cs
│       ├── Serialization/
│       │   ├── BijectionSerializer.cs
│       │   ├── JsonChainReader.cs
│       │   └── XmlChainReader.cs
│       └── Integration/
│           ├── BijectionRegistry.cs
│           ├── BijectionServiceExtensions.cs
│           ├── ObfuscatedIdAttribute.cs
│           ├── ObfuscatedIdFormat.cs
│           ├── ObfuscatedIdModifier.cs
│           ├── ObfuscatedIdModelBinder.cs
│           └── BijectionRegistryExtensions.cs
└── tests/
    └── IdScrambler.Tests/
        ├── IdScrambler.Tests.csproj
        ├── RoundTripTests.cs
        ├── KnownAnswerTests.cs
        ├── ChainTests.cs
        └── ValidationTests.cs
```

---

## 7. Non-Goals

- **Cryptographic security** — This is explicitly not a crypto library.
- **Arbitrary bit-widths** — Scope is 32-bit and 64-bit integers only. Other widths (16-bit, 128-bit) are out of scope.
- **Async** — All operations are synchronous, CPU-bound, and complete in nanoseconds.

---

## 8. Future Considerations

| Item | Notes |
|---|---|
| **128-bit variant** | Extend the generic approach to `UInt128` when use cases arise. |
| **Feistel Network** | A general-purpose Feistel round method (split word into halves, apply a round function, swap). More complex but very flexible. |
| **Source generators** | Compile a chain at build-time into a single inlined method for zero-overhead. |
| **Bit Permutation** | Arbitrary permutation of the N bit positions — generalizes Bit Reversal and Byte Permutation. |
| **Base32 format** | Add `ObfuscatedIdFormat.Base32` (`A-Z 2-7`, case-insensitive). Longer output (7 chars for 32-bit, 13 for 64-bit) but useful when case-insensitivity is required (e.g., DNS, case-folding systems). |
