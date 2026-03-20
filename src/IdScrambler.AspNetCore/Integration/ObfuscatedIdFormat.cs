namespace IdScrambler.Integration;

/// <summary>
/// Controls the output representation of an obfuscated ID.
/// </summary>
public enum ObfuscatedIdFormat
{
    /// <summary>Output as a numeric value (default). JSON: 3819274103</summary>
    Numeric,

    /// <summary>Output as a Base64Url-encoded string (A-Z, a-z, 0-9, -, _). JSON: "Pno3xQ"</summary>
    Base64Url,

    /// <summary>Output as a Base62-encoded string (A-Z, a-z, 0-9 only). JSON: "Kf8Tj2"</summary>
    Base62
}
