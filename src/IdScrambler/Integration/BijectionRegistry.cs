using System.Numerics;

namespace IdScrambler.Integration;

/// <summary>
/// Registry of named bijection chains. Names are case-insensitive.
/// </summary>
public sealed class BijectionRegistry
{
    private readonly Dictionary<(string Name, Type Type), object> _chains = new(
        new NameTypeComparer());

    /// <summary>Register a named chain.</summary>
    public void Register<T>(string name, IBijection<T> chain)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(chain);
        _chains[(name, typeof(T))] = chain;
    }

    /// <summary>Resolve a chain by name. Throws KeyNotFoundException if not found.</summary>
    public IBijection<T> Resolve<T>(string name)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        if (!TryResolve<T>(name, out var chain))
            throw new KeyNotFoundException(
                $"No bijection chain registered with name '{name}' for type {typeof(T).Name}.");
        return chain!;
    }

    /// <summary>Try to resolve a chain by name.</summary>
    public bool TryResolve<T>(string name, out IBijection<T>? chain)
        where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        if (_chains.TryGetValue((name, typeof(T)), out var obj))
        {
            chain = (IBijection<T>)obj;
            return true;
        }
        chain = null;
        return false;
    }

    private sealed class NameTypeComparer : IEqualityComparer<(string Name, Type Type)>
    {
        public bool Equals((string Name, Type Type) x, (string Name, Type Type) y)
            => StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name) && x.Type == y.Type;

        public int GetHashCode((string Name, Type Type) obj)
            => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name), obj.Type);
    }
}
