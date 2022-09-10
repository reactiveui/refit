using System.Linq;

using Microsoft.CodeAnalysis;

namespace Refit.Generator;

internal class RefitMetadata
{
    public RefitMetadata(INamedTypeSymbol? disposableInterfaceSymbol, INamedTypeSymbol httpMethodBaseAttributeSymbol)
    {
        DisposableInterfaceSymbol = disposableInterfaceSymbol;
        HttpMethodBaseAttributeSymbol = httpMethodBaseAttributeSymbol;
    }

    public INamedTypeSymbol? DisposableInterfaceSymbol { get; }
    public INamedTypeSymbol HttpMethodBaseAttributeSymbol { get; }

    public bool IsRefitMethod(IMethodSymbol? methodSymbol)
    {
        return methodSymbol?.GetAttributes().Any(ad => ad.AttributeClass?.InheritsFromOrEquals(HttpMethodBaseAttributeSymbol) == true) == true;
    }
}
