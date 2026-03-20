namespace IdScrambler.Integration;

/// <summary>
/// Marks a property or parameter for automatic ID obfuscation using a named bijection chain.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ObfuscatedIdAttribute : Attribute
{
    /// <summary>The registry name of the bijection chain to use.</summary>
    public string ChainName { get; }

    /// <summary>The output format (default: Numeric).</summary>
    public ObfuscatedIdFormat Format { get; set; } = ObfuscatedIdFormat.Numeric;

    public ObfuscatedIdAttribute(string chainName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chainName);
        ChainName = chainName;
    }
}
