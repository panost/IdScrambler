# Review Findings

## Findings

### Medium

- The randomized chain test is ineffective because the immutable chain result is discarded on each fluent call. The test currently exercises the empty identity chain instead of random composed chains. See `tests/IdScrambler.Tests/ChainTests.cs`.

- Numeric token encoding is culture-sensitive on write but invariant on read. `Encode(..., Numeric)` uses `ToString()` with current culture, while decode uses `Parse(..., InvariantCulture)`, so numeric tokens can fail to round-trip consistently across environments. See `src/IdScrambler.AspNetCore/Integration/BijectionRegistryExtensions.cs`.

### Low

- `Base64Url` encode/decode allocates more than necessary. Each encode performs base64 string creation plus trim/replace passes, and each decode performs `ToString()`, multiple `Replace()` calls, padding concatenation, and `Convert.FromBase64String`. For ASP.NET serialization and model binding, this can dominate total cost once the bijection itself is cheap. See `src/IdScrambler.AspNetCore/Integration/Base62.cs`.

- Chain construction and delegate compilation both repeat linear work. `Append` copies the full step array on every fluent call, and `CompileForward` / `CompileInverse` rebuild and compile delegates every time they are called. This is acceptable for startup-time configuration but avoidable if chains are created dynamically or compiled delegates are requested repeatedly. See `src/IdScrambler/BijectionChain.cs`.

## Speed Optimizations

- Rewrite `Base64Url` encode/decode using pure span-based code with fixed-size stack buffers to remove most transient allocations on the web boundary.

- Cache compiled delegates inside `BijectionChain<T>` with lazy fields. The chain is immutable, so compiled forward and inverse functions are natural cache candidates.

- Add a builder/freeze path for chain creation if config-driven or generated chains are expected. Preserve immutable runtime chains, but avoid O(n^2) copying during construction.

- Specialize `BytePermutationBijection<T>.ApplyPermutation` to use shift/mask logic or precomputed masks/shifts instead of byte-buffer shuffling. For common endian-reversal permutations, `BinaryPrimitives.ReverseEndianness` is the obvious fast path.

## Bijection Methods To Add

- Add `FeistelBijection<T>`. A small fixed-round Feistel network over half-words gives a much richer reversible family, strong diffusion, and easy inversion without requiring odd multipliers or lookup tables.

- Add a generic `PermuteBits(...)` transform. The library already has `ReverseBits` and `PermuteBytes`; a validated bit-permutation step would fill the gap for users who want structural rearrangement without nonlinear substitution.

- Consider a fused arithmetic step such as `MultiplyAddXor`. It is not a new mathematical family, but it reduces chain length and can help both interpreted and compiled execution paths.

## Verification Note

- `dotnet test` did not complete inside the current sandbox because the CLI first-run and restore paths are restricted in this environment. The review above is based on static inspection rather than a successful full test run.
