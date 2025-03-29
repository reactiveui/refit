using Microsoft.CodeAnalysis;

namespace Refit.Generator;

public class WellKnownTypes(Compilation compilation)
{
    readonly Dictionary<string, INamedTypeSymbol?> cachedTypes = new();

    public INamedTypeSymbol Get<T>() => Get(typeof(T));
    public INamedTypeSymbol Get(Type type)
    {
        return Get(type.FullName ?? throw new InvalidOperationException("Could not get name of type " + type));
    }

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
