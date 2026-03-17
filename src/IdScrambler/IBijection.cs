using System.Linq.Expressions;
using System.Numerics;

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

/// <summary>32-bit bijection.</summary>
public interface IBijection32 : IBijection<uint> { }

/// <summary>64-bit bijection.</summary>
public interface IBijection64 : IBijection<ulong> { }
