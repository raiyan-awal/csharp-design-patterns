namespace PrototypePattern;

/// <summary>
/// PROTOTYPE — the cloning contract.
///
/// Why not use .NET's built-in ICloneable?
///   • ICloneable.Clone() returns <c>object</c> — every caller must cast
///   • ICloneable doesn't distinguish shallow from deep copy — the contract
///     is ambiguous and the docs say "implementors can choose either"
///
/// This generic interface fixes both problems:
///   • ShallowClone() and DeepClone() are explicit and separate methods
///   • Return type is <typeparamref name="T"/>, so no cast is needed at the call site
/// </summary>
public interface IPrototype<T>
{
    /// <summary>
    /// Returns a new instance whose value-type fields are copied,
    /// but whose reference-type fields still point to the SAME objects
    /// as the original. Fast but dangerous if the clone mutates shared state.
    /// </summary>
    T ShallowClone();

    /// <summary>
    /// Returns a fully independent copy. Every nested object is also cloned,
    /// so mutations on the copy never affect the original and vice-versa.
    /// </summary>
    T DeepClone();
}
