# Evaluation: S-Box & Lattice Matrix for IdScrambler

## S-Box (Nibble Substitution)

**Already included in the PRD** (§2.7). Fully specified as a first-class bijection method with:
- `byte[16]` S-box parameter (permutation of `{0..15}`)
- Forward via nibble-wise lookup, inverse via inverse S-box
- Chain method: `SubstituteNibbles(byte[] sbox)`
- Implementation file: `NibbleSubstitutionBijection.cs`

✅ **No action needed.**

---

## Lattice Matrix (Linear Mixing Matrix)

A lattice/mixing matrix transform multiplies the bytes (or nibbles) of the integer by an **invertible matrix over GF(2⁸)**, similar to AES's **MixColumns** step.

### What It Would Offer

| Aspect | Assessment |
|---|---|
| **Diffusion** | Excellent — every output byte depends on *all* input bytes. Strongest single-step diffusion primitive available. |
| **Complements S-Box** | S-Box provides *confusion* (non-linearity); MixColumns provides *diffusion* (spreading). Together they form the SPN (Substitution-Permutation Network) backbone. |
| **Uniqueness** | No current method achieves full cross-byte diffusion in a single step. |

### Why It's Not a Good Fit

| Concern | Detail |
|---|---|
| **Complexity explosion** | GF(2⁸) arithmetic (polynomial multiplication mod an irreducible polynomial, lookup tables or carry-less multiply intrinsics) is a significant jump versus the library's simple bitwise/arithmetic ops. |
| **Parameter burden** | Requires an *invertible matrix* over GF(2⁸) — 4×4 for 32-bit, 8×8 for 64-bit. Validating invertibility needs a GF(2⁸) determinant computation. Far heavier than "pick an odd number." |
| **Performance** | Per-byte GF multiply (even with lookup tables) is significantly slower than other transforms. Breaks the "completes in nanoseconds" promise. |
| **Redundancy in chains** | Chaining existing methods (`Multiply → XorShiftRight → Multiply`, the splitmix pattern) already achieves near-perfect avalanche. Lattice adds theoretical elegance but little practical quality on top. |
| **Scope creep toward crypto** | A Galois-field mixing matrix is a cryptographic primitive. Adding it contradicts the library's explicit non-goal of cryptographic security. |
| **Expression compilation** | GF(2⁸) multiply is hard to inline into expression trees — requires 256-byte lookup tables or `PCLMULQDQ` intrinsics, neither fits `System.Linq.Expressions` cleanly. |

### Verdict

**Lattice Matrix does not warrant inclusion.** The diffusion benefit is already achievable by chaining `Multiply + XorShift`. The implementation complexity, performance cost, and crypto-adjacent nature push it outside the library's design philosophy.

> [!TIP]
> If single-step "every output byte depends on every input byte" diffusion is ever needed, the **Feistel Network** (already in PRD §8 — Future Considerations) is a better fit: simpler to implement, easier to parameterize, and stays in regular integer arithmetic.
