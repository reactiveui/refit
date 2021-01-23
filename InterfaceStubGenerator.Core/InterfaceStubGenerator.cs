using System;
using System.Collections.Generic;
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
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RefitInternalNamespace", out var refitInternalNamespace);

            refitInternalNamespace = $"{refitInternalNamespace ?? string.Empty}RefitInternalGenerated";

            var attributeText = @$"
using System;
#pragma warning disable
namespace {refitInternalNamespace}
{{
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate)]
    sealed class PreserveAttribute : Attribute
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

            // add the attribute text
            context.AddSource("PreserveAttribute", SourceText.From(attributeText, Encoding.UTF8));

            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            var options = (context.Compilation as CSharpCompilation)!.SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));

            // get the newly bound attribute
            var preserveAttributeSymbol = compilation.GetTypeByMetadataName($"{refitInternalNamespace}.PreserveAttribute")!;
            var disposableInterfaceSymbol = compilation.GetTypeByMetadataName("System.IDisposable")!;
            var httpMethodBaseAttributeSymbol = compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute");

            if(httpMethodBaseAttributeSymbol == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(RefitNotReferenced, null));
                return;
            }

            // Check the candidates and keep the ones we're actually interested in
            var methodSymbols = new List<IMethodSymbol>();
            foreach (var method in receiver.CandidateMethods)
            {
                var model = compilation.GetSemanticModel(method.SyntaxTree);
                
                // Get the symbol being declared by the method
                var methodSymbol = model.GetDeclaredSymbol(method);
                if (IsRefitMethod(methodSymbol, httpMethodBaseAttributeSymbol))
                {
                    methodSymbols.Add(methodSymbol!);
                }
            }

            var keyCount = new Dictionary<string, int>();

            // group the fields by interface and generate the source
            foreach (var group in methodSymbols.GroupBy(m => m.ContainingType))
            {
                // each group is keyed by the Interface INamedTypeSymbol and contains the members
                // with a refit attribute on them. Types may contain other members, without the attribute, which we'll
                // need to check for and error out on

                var classSource = ProcessInterface(group.Key, group.ToList(), preserveAttributeSymbol, disposableInterfaceSymbol, httpMethodBaseAttributeSymbol, context);
             
                var keyName = group.Key.Name;
                if(keyCount.TryGetValue(keyName, out var value))
                {
                    keyName = $"{keyName}{++value}";
                }
                keyCount[keyName] = value;

                context.AddSource($"{keyName}_refit.cs", SourceText.From(classSource, Encoding.UTF8));
            }

        }

        string ProcessInterface(INamedTypeSymbol interfaceSymbol,
                                List<IMethodSymbol> refitMethods,
                                ISymbol preserveAttributeSymbol,
                                ISymbol disposableInterfaceSymbol,
                                INamedTypeSymbol httpMethodBaseAttributeSymbol,
                                GeneratorExecutionContext context)
        {

            // Get the glass name with the type parameters, then remove the namespace
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
                ns = $"AutoGenerated{classSuffix}";
            }

            var source = new StringBuilder($@"
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8669 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.

namespace {ns}
{{
    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [{preserveAttributeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}]
    [global::System.Reflection.Obfuscation(Exclude=true)]
    partial class AutoGenerated{classDeclaration}
        : {interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{GenerateConstraints(interfaceSymbol)}

    {{
        /// <inheritdoc />
        public global::System.Net.Http.HttpClient Client {{ get; }}
        readonly global::Refit.IRequestBuilder requestBuilder;

        /// <inheritdoc />
        public AutoGenerated{classSuffix}(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
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
            foreach(var method in refitMethods.Concat(derivedRefitMethods))
            {
                ProcessRefitMethod(source, method);
            }

            // Handle non-refit Methods that aren't static or properties
            foreach(var method in nonRefitMethods.Concat(derivedNonRefitMethods))
            {
                if (method.IsStatic || method.MethodKind == MethodKind.PropertyGet || method.MethodKind == MethodKind.PropertySet)
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

#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8669 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
");
            return source.ToString();
        }

        void ProcessRefitMethod(StringBuilder source, IMethodSymbol methodSymbol)
        {
            WriteMethodOpening(source, methodSymbol);

            // Build the list of args for the array
            var argList = new List<string>();
            foreach(var param in methodSymbol.Parameters)
            {
                argList.Add($"@{param.MetadataName}");
            }

            // List of types. For nullable one, wrap in ToNullable()
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
            var arguments = new object[] {{ {string.Join(", ", argList)} }};
            var func = requestBuilder.BuildRestResultFuncForMethod(""{methodSymbol.Name}"", new global::System.Type[] {{ {string.Join(", ", typeList)} }}{genericString} );
            return ({methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})func(Client, arguments);
");

            WriteMethodClosing(source);
        }

        void ProcessDisposableMethod(StringBuilder source, IMethodSymbol methodSymbol)
        {
            WriteMethodOpening(source, methodSymbol);

            source.Append(@"
                Client?.Dispose();
");

            WriteMethodClosing(source);
        }

        string GenerateConstraints(INamedTypeSymbol iface)
        {
            var source = new StringBuilder();
            // Need to loop over the constraints and create them
            foreach(var typeParameter in iface.TypeParameters)
            {
                WriteConstraitsForTypeParameter(source, typeParameter);
            }

            return source.ToString();
        }

        void WriteConstraitsForTypeParameter(StringBuilder source, ITypeParameterSymbol typeParameter)
        {
            var parameters = new List<string>();
            if(typeParameter.HasReferenceTypeConstraint)
            {
                parameters.Add("class");
            }
            if (typeParameter.HasUnmanagedTypeConstraint)
            {
                parameters.Add("unmanaged");
            }
            if (typeParameter.HasValueTypeConstraint)
            {
                parameters.Add("struct");
            }
            if (typeParameter.HasNotNullConstraint)
            {
                parameters.Add("notnull");
            }
            foreach(var typeConstraint in typeParameter.ConstraintTypes)
            {
                parameters.Add(typeConstraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
            // new constraint has to be last
            if (typeParameter.HasConstructorConstraint)
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
            WriteMethodOpening(source, methodSymbol);

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

        void WriteMethodOpening(StringBuilder source, IMethodSymbol methodSymbol)
        {
            source.Append(@$"

        /// <inheritdoc />
        {methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(");

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
            
           source.Append(@")
        {");
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

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // We're looking for methods with an attribute that are in an interfaces 
                if(syntaxNode is MethodDeclarationSyntax methodDeclarationSyntax &&
                   methodDeclarationSyntax.Parent is InterfaceDeclarationSyntax &&
                   methodDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CandidateMethods.Add(methodDeclarationSyntax);
                }
            }
        }
    }
}
