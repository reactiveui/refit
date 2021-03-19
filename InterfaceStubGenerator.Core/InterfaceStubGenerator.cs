using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator
{
    // * Search for all Interfaces, find the method definitions
    //   and make sure there's at least one Refit attribute on one
    // * Generate the data we need for the template based on interface method
    //   defn's

    [Generator]
    public class InterfaceStubGenerator : ISourceGenerator
    {
#pragma warning disable RS2008 // Enable analyzer release tracking
        static readonly DiagnosticDescriptor InvalidRefitMember = new(
                "RF001",
                "Refit types must have Refit HTTP method attributes",
                "Method {0}.{1} either has no Refit HTTP method attribute or you've used something other than a string literal for the 'path' argument.",
                "Refit",
                DiagnosticSeverity.Warning,
                true);

        static readonly DiagnosticDescriptor RefitNotReferenced = new(
                "RF002",
                "Refit must be referenced",
                "Refit is not referenced. Add a reference to Refit.",
                "Refit",
                DiagnosticSeverity.Error,
                true);
#pragma warning restore RS2008 // Enable analyzer release tracking

        public void Execute(GeneratorExecutionContext context)
        {
            GenerateInterfaceStubs(context);            
        }      

        public void GenerateInterfaceStubs(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RefitInternalNamespace", out var refitInternalNamespace);

            refitInternalNamespace = $"{refitInternalNamespace ?? string.Empty}RefitInternalGenerated";

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            var options = (context.Compilation as CSharpCompilation)!.SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation;

            var disposableInterfaceSymbol = compilation.GetTypeByMetadataName("System.IDisposable")!;
            var httpMethodBaseAttributeSymbol = compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute");

            if(httpMethodBaseAttributeSymbol == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(RefitNotReferenced, null));
                return;
            }


            // Check the candidates and keep the ones we're actually interested in

            var interfaceToNullableEnabledMap = new Dictionary<INamedTypeSymbol, bool>();
            var methodSymbols = new List<IMethodSymbol>();
            foreach (var group in receiver.CandidateMethods.GroupBy(m => m.SyntaxTree))
            {
                var model = compilation.GetSemanticModel(group.Key);             
                foreach (var method in group)
                {
                    // Get the symbol being declared by the method
                    var methodSymbol = model.GetDeclaredSymbol(method);
                    if (IsRefitMethod(methodSymbol, httpMethodBaseAttributeSymbol))
                    {
                        var isAnnotated = context.Compilation.Options.NullableContextOptions == NullableContextOptions.Enable ||
                            model.GetNullableContext(method.SpanStart) == NullableContext.Enabled;
                        interfaceToNullableEnabledMap[methodSymbol!.ContainingType] = isAnnotated;

                        methodSymbols.Add(methodSymbol!);
                    }
                }
            }

            var interfaces = methodSymbols.GroupBy(m => m.ContainingType)
                                          .ToDictionary(g => g.Key, v => v.ToList());

            // Look through the candidate interfaces
            var interfaceSymbols = new List<INamedTypeSymbol>();
            foreach(var group in receiver.CandidateInterfaces.GroupBy(i => i.SyntaxTree))
            {
                var model = compilation.GetSemanticModel(group.Key);
                foreach (var iface in group)
                {
                    // get the symbol belonging to the interface
                    var ifaceSymbol = model.GetDeclaredSymbol(iface);

                    // See if we already know about it, might be a dup
                    if (ifaceSymbol is null || interfaces.ContainsKey(ifaceSymbol))
                        continue;

                    // The interface has no refit methods, but its base interfaces might
                    var hasDerivedRefit = ifaceSymbol.AllInterfaces
                                                     .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
                                                     .Where(m => IsRefitMethod(m, httpMethodBaseAttributeSymbol))
                                                     .Any();

                    if (hasDerivedRefit)
                    {
                        // Add the interface to the generation list with an empty set of methods
                        // The logic already looks for base refit methods
                        interfaces.Add(ifaceSymbol, new List<IMethodSymbol>() );
                        var isAnnotated = model.GetNullableContext(iface.SpanStart) == NullableContext.Enabled;

                        interfaceToNullableEnabledMap[ifaceSymbol] = isAnnotated;
                    }
                }
            }

            // Bail out if there aren't any interfaces to generate code for. This may be the case with transitives
            if(!interfaces.Any()) return;
           

            var supportsNullable = ((CSharpParseOptions)context.ParseOptions).LanguageVersion >= LanguageVersion.CSharp8;

            var keyCount = new Dictionary<string, int>();

            var attributeText = @$"
#pragma warning disable
namespace {refitInternalNamespace}
{{
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.AttributeUsage (global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct | global::System.AttributeTargets.Enum | global::System.AttributeTargets.Constructor | global::System.AttributeTargets.Method | global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Event | global::System.AttributeTargets.Interface | global::System.AttributeTargets.Delegate)]
    sealed class PreserveAttribute : global::System.Attribute
    {{
        //
        // Fields
        //
        public bool AllMembers;

        public bool Conditional;
    }}
}}
#pragma warning restore
";


            compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));

            // add the attribute text
            context.AddSource("PreserveAttribute.g.cs", SourceText.From(attributeText, Encoding.UTF8));

            // get the newly bound attribute
            var preserveAttributeSymbol = compilation.GetTypeByMetadataName($"{refitInternalNamespace}.PreserveAttribute")!;

            var generatedClassText = @$"
#pragma warning disable
namespace Refit.Implementation
{{

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [{preserveAttributeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}]
    [global::System.Reflection.Obfuscation(Exclude=true)]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static partial class Generated
    {{
    }}
}}
#pragma warning restore
";
            context.AddSource("Generated.g.cs", SourceText.From(generatedClassText, Encoding.UTF8));

            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(generatedClassText, Encoding.UTF8), options));



            // group the fields by interface and generate the source
            foreach (var group in interfaces)
            {
                // each group is keyed by the Interface INamedTypeSymbol and contains the members
                // with a refit attribute on them. Types may contain other members, without the attribute, which we'll
                // need to check for and error out on

                var classSource = ProcessInterface(group.Key,
                                                   group.Value,
                                                   preserveAttributeSymbol,
                                                   disposableInterfaceSymbol,
                                                   httpMethodBaseAttributeSymbol,
                                                   supportsNullable,
                                                   interfaceToNullableEnabledMap[group.Key],
                                                   context);
             
                var keyName = group.Key.Name;
                if(keyCount.TryGetValue(keyName, out var value))
                {
                    keyName = $"{keyName}{++value}";
                }
                keyCount[keyName] = value;

                context.AddSource($"{keyName}.g.cs", SourceText.From(classSource, Encoding.UTF8));
            }           

        }

        string ProcessInterface(INamedTypeSymbol interfaceSymbol,
                                List<IMethodSymbol> refitMethods,
                                ISymbol preserveAttributeSymbol,
                                ISymbol disposableInterfaceSymbol,
                                INamedTypeSymbol httpMethodBaseAttributeSymbol,
                                bool supportsNullable,
                                bool nullableEnabled,
                                GeneratorExecutionContext context)
        {

            // Get the class name with the type parameters, then remove the namespace
            var className = interfaceSymbol.ToDisplayString();
            var lastDot = className.LastIndexOf('.');
            if(lastDot > 0)
            {
                className = className.Substring(lastDot+1);
            }
            var classDeclaration = $"{interfaceSymbol.ContainingType?.Name}{className}";


            // Get the class name itself
            var classSuffix = $"{interfaceSymbol.ContainingType?.Name}{interfaceSymbol.Name}";
            var ns = interfaceSymbol.ContainingNamespace?.ToDisplayString();

            // if it's the global namespace, our lookup rules say it should be the same as the class name
            if(interfaceSymbol.ContainingNamespace != null && interfaceSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                ns = string.Empty;
            }

            // Remove dots
            ns = ns!.Replace(".", "");

            // See what the nullable context is


            var source = new StringBuilder();
            if(supportsNullable)
            {
                source.Append("#nullable ");

                if(nullableEnabled)
                {
                    source.Append("enable");
                }
                else
                {
                    source.Append("disable");
                }
            }

            source.Append($@"
#pragma warning disable
namespace Refit.Implementation
{{

    partial class Generated
    {{

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [{preserveAttributeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}]
    [global::System.Reflection.Obfuscation(Exclude=true)]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    partial class {ns}{classDeclaration}
        : {interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{GenerateConstraints(interfaceSymbol.TypeParameters, false)}

    {{
        /// <inheritdoc />
        public global::System.Net.Http.HttpClient Client {{ get; }}
        readonly global::Refit.IRequestBuilder requestBuilder;

        /// <inheritdoc />
        public {ns}{classSuffix}(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
        {{
            Client = client;
            this.requestBuilder = requestBuilder;
        }}
    
");
            // Get any other methods on the refit interfaces. We'll need to generate something for them and warn
            var nonRefitMethods = interfaceSymbol.GetMembers().OfType<IMethodSymbol>().Except(refitMethods, SymbolEqualityComparer.Default).Cast<IMethodSymbol>().ToList();

            // get methods for all inherited
            var derivedMethods = interfaceSymbol.AllInterfaces.SelectMany(i => i.GetMembers().OfType<IMethodSymbol>()).ToList();

            // Look for disposable
            var disposeMethod = derivedMethods.Find(m => m.ContainingType?.Equals(disposableInterfaceSymbol, SymbolEqualityComparer.Default) == true);
            if(disposeMethod != null)
            {
                //remove it from the derived methods list so we don't process it with the rest
                derivedMethods.Remove(disposeMethod);
            }

            // Pull out the refit methods from the derived types
            var derivedRefitMethods = derivedMethods.Where(m => IsRefitMethod(m, httpMethodBaseAttributeSymbol)).ToList();
            var derivedNonRefitMethods = derivedMethods.Except(derivedMethods, SymbolEqualityComparer.Default).Cast<IMethodSymbol>().ToList();

            // Handle Refit Methods            
            foreach(var method in refitMethods)
            {
                ProcessRefitMethod(source, method, true);
            }

            foreach (var method in refitMethods.Concat(derivedRefitMethods))
            {
                ProcessRefitMethod(source, method, false);
            }


            // Handle non-refit Methods that aren't static or properties or have a method body
            foreach (var method in nonRefitMethods.Concat(derivedNonRefitMethods))
            {
                if (method.IsStatic ||
                    method.MethodKind == MethodKind.PropertyGet ||
                    method.MethodKind == MethodKind.PropertySet ||
                    !method.IsAbstract) // If an interface method has a body, it won't be abstract
                    continue;

                ProcessNonRefitMethod(source, method, context);
            }

            // Handle Dispose
            if(disposeMethod != null)
            {
                ProcessDisposableMethod(source, disposeMethod);
            }          

            source.Append(@"
    }
    }
}

#pragma warning restore
");
            return source.ToString();
        }

        /// <summary>
        /// Generates the body of the Refit method
        /// </summary>
        /// <param name="source"></param>
        /// <param name="methodSymbol"></param>
        /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces</param>
        void ProcessRefitMethod(StringBuilder source, IMethodSymbol methodSymbol, bool isTopLevel)
        {
            WriteMethodOpening(source, methodSymbol, !isTopLevel);

            // Build the list of args for the array
            var argList = new List<string>();
            foreach(var param in methodSymbol.Parameters)
            {
                argList.Add($"@{param.MetadataName}");
            }

            // List of types. 
            var typeList = new List<string>();
            foreach(var param in methodSymbol.Parameters)
            {
                typeList.Add($"typeof({param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");                
            }

            // List of generic arguments
            var genericList = new List<string>();            
            foreach(var typeParam in methodSymbol.TypeParameters)
            {
                genericList.Add($"typeof({typeParam.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
            }

            var genericString = genericList.Count > 0 ? $", new global::System.Type[] {{ {string.Join(", ", genericList)} }}" : string.Empty;            

            source.Append(@$"
            var ______arguments = new object[] {{ {string.Join(", ", argList)} }};
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""{methodSymbol.Name}"", new global::System.Type[] {{ {string.Join(", ", typeList)} }}{genericString} );
            return ({methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})______func(this.Client, ______arguments);
");

            WriteMethodClosing(source);
        }

        void ProcessDisposableMethod(StringBuilder source, IMethodSymbol methodSymbol)
        {
            WriteMethodOpening(source, methodSymbol, true);

            source.Append(@"
                Client?.Dispose();
");

            WriteMethodClosing(source);
        }

        string GenerateConstraints(ImmutableArray<ITypeParameterSymbol> typeParameters, bool isOverrideOrExplicitImplementation)
        {
            var source = new StringBuilder();
            // Need to loop over the constraints and create them
            foreach(var typeParameter in typeParameters)
            {
                WriteConstraitsForTypeParameter(source, typeParameter, isOverrideOrExplicitImplementation);
            }

            return source.ToString();
        }

        void WriteConstraitsForTypeParameter(StringBuilder source, ITypeParameterSymbol typeParameter, bool isOverrideOrExplicitImplementation)
        {
            // Explicit interface implementations and ovverrides can only have class or struct constraints

            var parameters = new List<string>();
            if(typeParameter.HasReferenceTypeConstraint)
            {
                parameters.Add("class");
            }
            if (typeParameter.HasUnmanagedTypeConstraint && !isOverrideOrExplicitImplementation)
            {
                parameters.Add("unmanaged");
            }
            if (typeParameter.HasValueTypeConstraint)
            {
                parameters.Add("struct");
            }
            if (typeParameter.HasNotNullConstraint && !isOverrideOrExplicitImplementation)
            {
                parameters.Add("notnull");
            }
            if (!isOverrideOrExplicitImplementation)
            {
                foreach (var typeConstraint in typeParameter.ConstraintTypes)
                {
                    parameters.Add(typeConstraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
            }
            
            // new constraint has to be last
            if (typeParameter.HasConstructorConstraint && !isOverrideOrExplicitImplementation)
            {
                parameters.Add("new()");
            }

            if(parameters.Count > 0)
            {
                source.Append(@$"
         where {typeParameter.Name} : {string.Join(", ", parameters)}");
            }

        }

        void ProcessNonRefitMethod(StringBuilder source, IMethodSymbol methodSymbol, GeneratorExecutionContext context)
        {
            WriteMethodOpening(source, methodSymbol, true);

            source.Append(@"
                throw new global::System.NotImplementedException(""Either this method has no Refit HTTP method attribute or you've used something other than a string literal for the 'path' argument."");
");

            WriteMethodClosing(source);

            foreach(var location in methodSymbol.Locations)
            {
                var diagnostic = Diagnostic.Create(InvalidRefitMember, location, methodSymbol.ContainingType.Name, methodSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }            
        }

        void WriteMethodOpening(StringBuilder source, IMethodSymbol methodSymbol, bool isExplicitInterface)
        {
            var visibility = !isExplicitInterface ? "public " : string.Empty;

            source.Append(@$"

        /// <inheritdoc />
        {visibility}{methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} ");

            if(isExplicitInterface)
            {
                source.Append(@$"{methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.");
            }
            source.Append(@$"{methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(");            

            if(methodSymbol.Parameters.Length > 0)
            {
                var list = new List<string>();
                foreach(var param in methodSymbol.Parameters)
                {
                    var annotation = !param.Type.IsValueType && param.NullableAnnotation == NullableAnnotation.Annotated;

                    list.Add($@"{param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{(annotation ? '?' : string.Empty)} @{param.MetadataName}");
                }

                source.Append(string.Join(", ", list));
            }
            
           source.Append(@$") {GenerateConstraints(methodSymbol.TypeParameters, isExplicitInterface)}
        {{");
        }

        void WriteMethodClosing(StringBuilder source) => source.Append(@"        }");


        bool IsRefitMethod(IMethodSymbol? methodSymbol, INamedTypeSymbol httpMethodAttibute)
        {
            return methodSymbol?.GetAttributes().Any(ad => ad.AttributeClass?.InheritsFromOrEquals(httpMethodAttibute) == true) == true;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<MethodDeclarationSyntax> CandidateMethods { get; } = new();

            public List<InterfaceDeclarationSyntax> CandidateInterfaces { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // We're looking for methods with an attribute that are in an interfaces 
                if(syntaxNode is MethodDeclarationSyntax methodDeclarationSyntax &&
                   methodDeclarationSyntax.Parent is InterfaceDeclarationSyntax &&
                   methodDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CandidateMethods.Add(methodDeclarationSyntax);
                }

                // We also look for interfaces that derive from others, so we can see if any base methods contain
                // Refit methods
                if(syntaxNode is InterfaceDeclarationSyntax iface && iface.BaseList is not null)
                {
                    CandidateInterfaces.Add(iface);
                }
            }
        }
    }
}
