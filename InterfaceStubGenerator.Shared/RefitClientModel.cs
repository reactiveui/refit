using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Refit.Generator;

internal class RefitClientModel
{
    readonly RefitMetadata refitMetadata;

    public RefitClientModel(INamedTypeSymbol refitInterface, List<IMethodSymbol> refitMethods, RefitMetadata refitMetadata)
    {
        RefitInterface = refitInterface;
        RefitMethods = refitMethods;
        this.refitMetadata = refitMetadata;

        // Get any other methods on the refit interfaces. We'll need to generate something for them and warn
        var nonRefitMethods = refitInterface
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Except(refitMethods, SymbolEqualityComparer.Default)
            .Cast<IMethodSymbol>()
            .ToList();

        // get methods for all inherited
        var derivedMethods = refitInterface
            .AllInterfaces.SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
            .ToList();

        // Look for disposable
        DisposeMethod = derivedMethods.Find(
                m =>
                    m.ContainingType?.Equals(
                        refitMetadata.DisposableInterfaceSymbol,
                        SymbolEqualityComparer.Default
                    ) == true
            );
        if (DisposeMethod != null)
        {
            //remove it from the derived methods list so we don't process it with the rest
            derivedMethods.Remove(DisposeMethod);
        }

        // Pull out the refit methods from the derived types
        var derivedRefitMethods = derivedMethods.Where(refitMetadata.IsRefitMethod).ToList();
        var derivedNonRefitMethods = derivedMethods.Except(derivedMethods, SymbolEqualityComparer.Default).Cast<IMethodSymbol>().ToList();

        AllRefitMethods = refitMethods.Concat(derivedRefitMethods);
        NonRefitMethods = nonRefitMethods.Concat(derivedNonRefitMethods)
            .Where(static method =>
            {
                return !(method.IsStatic ||
                    method.MethodKind == MethodKind.PropertyGet ||
                    method.MethodKind == MethodKind.PropertySet ||
                    !method.IsAbstract);
            });
    }

    public INamedTypeSymbol RefitInterface { get; }
    public List<IMethodSymbol> RefitMethods { get; }
    public IEnumerable<IMethodSymbol> AllRefitMethods { get; }
    public IEnumerable<IMethodSymbol> NonRefitMethods { get; }

    public string FileName => RefitInterface.Name;

    public string ClassDeclaration
    {
        get
        {
            // Get the class name with the type parameters, then remove the namespace
            var className = RefitInterface.ToDisplayString();
            var lastDot = className.LastIndexOf('.');
            if (lastDot > 0)
            {
                className = className.Substring(lastDot + 1);
            }
            var classDeclaration = $"{RefitInterface.ContainingType?.Name}{className}";
            return classDeclaration;
        }
    }

    public string ClassSuffix
    {
        get
        {
            // Get the class name itself
            var classSuffix = $"{RefitInterface.ContainingType?.Name}{RefitInterface.Name}";
            return classSuffix;
        }
    }

    public string NamespacePrefix
    {
        get
        {
            var ns = RefitInterface.ContainingNamespace?.ToDisplayString();

            // if it's the global namespace, our lookup rules say it should be the same as the class name
            if (RefitInterface.ContainingNamespace != null && RefitInterface.ContainingNamespace.IsGlobalNamespace)
            {
                return string.Empty;
            }

            // Remove dots
            ns = ns!.Replace(".", "");
            return ns;
        }
    }
    public string BaseInterfaceDeclaration => $"{RefitInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";

    public IMethodSymbol DisposeMethod { get; }
}
