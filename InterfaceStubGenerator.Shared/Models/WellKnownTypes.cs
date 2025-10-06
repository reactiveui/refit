using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>
/// WellKnownTypes.
/// </summary>
public class WellKnownTypes(Compilation compilation)
{
    readonly Dictionary<string, INamedTypeSymbol?> cachedTypes = [];

    /// <summary>
    /// Gets this instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public INamedTypeSymbol Get<T>() => Get(typeof(T));

    /// <summary>
    /// Gets the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Could not get name of type " + type</exception>
    public INamedTypeSymbol Get(Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return Get(type.FullName ?? throw new InvalidOperationException("Could not get name of type " + type));
    }

    /// <summary>
    /// Tries the get.
    /// </summary>
    /// <param name="typeFullName">Full name of the type.</param>
    /// <returns></returns>
    public INamedTypeSymbol? TryGet(string typeFullName)
    {
        if (cachedTypes.TryGetValue(typeFullName, out var typeSymbol))
        {
            return typeSymbol;
        }

        typeSymbol = compilation.GetTypeByMetadataName(typeFullName);
        cachedTypes.Add(typeFullName, typeSymbol);

        return typeSymbol;
    }

    INamedTypeSymbol Get(string typeFullName) =>
        TryGet(typeFullName) ?? throw new InvalidOperationException("Could not get type " + typeFullName);
}
