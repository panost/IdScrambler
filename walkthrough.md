# Walkthrough: IdScrambler Implementation

## Summary

Implemented the complete IdScrambler .NET 10 library per [PRD.md](file:///f:/Dev/GitHub/IdScrambler/PRD.md). The library provides a fluent, chainable API for reversible (bijective) transformations on 32-bit and 64-bit integers.

## Project Structure

```
IdScrambler/
├── src/IdScrambler/
│   ├── IBijection.cs             — Core interfaces
│   ├── BijectionChain.cs         — Fluent chain + expression compilation
│   ├── BijectionExtensions.cs    — Signed int/long convenience methods
│   ├── Presets.cs                — StrongMix32/64, LightScramble32/64
│   ├── SBoxPresets.cs            — Public default S-box
│   ├── Transforms/               — 11 bijection implementations
│   ├── Serialization/            — JSON/XML read/write + BijectionConfigException
│   └── Integration/              — ASP.NET Core (registry, attribute, model binder, DI, Base62/Base64Url)
└── tests/IdScrambler.Tests/
    ├── RoundTripTests.cs          — 46 tests
    ├── KnownAnswerTests.cs        — 16 tests
    ├── ChainTests.cs              — 23 tests
    └── ValidationTests.cs         — 31 tests
```

## Key Design Decisions

- **`BijectionChain<T>` is sealed** — PRD listed `BijectionChain32`/`BijectionChain64` subclasses, but these can't inherit a sealed class. Users use `BijectionChain<uint>` / `BijectionChain<ulong>` directly.

- **XorShift inverse uses the doubling method** — The PRD described a repeated same-shift approach, but the mathematically correct algorithm doubles the shift each iteration: `x ^= x>>>s; x ^= x>>>2s; x ^= x>>>4s; ...`. This converges in O(log(N/s)) iterations and works correctly for all shift values.

- **[SBoxPresets](file:///f:/Dev/GitHub/IdScrambler/src/IdScrambler/SBoxPresets.cs#8-16) class** — The default S-box is defined on the internal `NibbleSubstitutionBijection<T>` class. A public [SBoxPresets](file:///f:/Dev/GitHub/IdScrambler/src/IdScrambler/SBoxPresets.cs#8-16) class exposes it for consumers.

## Test Results

```
Total tests: 116
     Passed: 116
     Failed: 0
```

All transforms tested with:
- Round-trip verification on representative inputs (0, 1, MaxValue, powers-of-two, random)
- 10,000 random sample round-trips for full chains
- Hand-verified known-answer values
- Randomized multi-step chain generation (20 trials × 1000 samples)
- Compiled delegate round-trip verification
- JSON and XML serialization round-trips
- Invalid parameter rejection (even factors, out-of-range shifts, non-permutation arrays)
