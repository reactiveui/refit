// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator;

/// <summary>Emits the generated source code for Refit interface implementations.</summary>
internal static class Emitter
{
    /// <summary>The variable name used for the cached type parameter array field.</summary>
    private const string TypeParameterVariableName = "______typeParameters";

    /// <summary>Indentation levels spanned by the generated namespace and class nesting.</summary>
    private const int NamespaceAndClassIndentation = 2;

    /// <summary>Emits the shared preserve attribute and factory registration code.</summary>
    /// <param name="model">The context generation model describing the interfaces.</param>
    /// <param name="addSource">Callback used to add generated source files.</param>
    public static void EmitSharedCode(
        ContextGenerationModel model,
        Action<string, SourceText> addSource)
    {
        if (model.Interfaces.Count == 0)
        {
            return;
        }

        const string attributeUsageLine =
            "[global::System.AttributeUsage (global::System.AttributeTargets.Class | "
            + "global::System.AttributeTargets.Struct | global::System.AttributeTargets.Enum | "
            + "global::System.AttributeTargets.Constructor | global::System.AttributeTargets.Method | "
            + "global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | "
            + "global::System.AttributeTargets.Event | global::System.AttributeTargets.Interface | "
            + "global::System.AttributeTargets.Delegate)]";

        var attributeText = $$"""

                              #pragma warning disable
                              namespace {{model.RefitInternalNamespace}}
                              {
                                  [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                                  [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                                  {{attributeUsageLine}}
                                  sealed class PreserveAttribute : global::System.Attribute
                                  {
                                      //
                                      // Fields
                                      //
                                      public bool AllMembers;

                                      public bool Conditional;
                                  }
                              }
                              #pragma warning restore

                              """;

        // add the attribute text
        addSource("PreserveAttribute.g.cs", SourceText.From(attributeText, Encoding.UTF8));

        var generatedFactoryRegistrations = string.Join(
            "\n",
            model.Interfaces
                .Where(static interfaceModel => !interfaceModel.ClassDeclaration.Contains("<"))
                .Select(static interfaceModel =>
                    "                        global::Refit.RestService.RegisterGeneratedFactory(typeof("
                    + $"{interfaceModel.InterfaceDisplayName}), static (client, requestBuilder) => new "
                    + $"global::Refit.Implementation.Generated.{interfaceModel.Ns}{interfaceModel.ClassSuffix}"
                    + "(client, requestBuilder));"));

        const string dynamicDependencyLine =
            "[System.Diagnostics.CodeAnalysis.DynamicDependency("
            + "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, "
            + "typeof(global::Refit.Implementation.Generated))]";

        var generatedClassText = $$"""

                                   #pragma warning disable
                                   namespace Refit.Implementation
                                   {

                                       /// <inheritdoc />
                                       [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                                       [global::System.Diagnostics.DebuggerNonUserCode]
                                       [{{model.PreserveAttributeDisplayName}}]
                                       [global::System.Reflection.Obfuscation(Exclude=true)]
                                       [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                                       internal static partial class Generated
                                       {
                                   #if NET5_0_OR_GREATER
                                           [System.Runtime.CompilerServices.ModuleInitializer]
                                           {{dynamicDependencyLine}}
                                           public static void Initialize()
                                           {
                                   {{generatedFactoryRegistrations}}
                                           }
                                   #endif
                                       }
                                   }
                                   #pragma warning restore

                                   """;
        addSource("Generated.g.cs", SourceText.From(generatedClassText, Encoding.UTF8));
    }

    /// <summary>Emits the generated implementation source for a single interface.</summary>
    /// <param name="model">The interface model to emit.</param>
    /// <returns>The generated source text for the interface implementation.</returns>
    public static SourceText EmitInterface(InterfaceModel model)
    {
        var source = new SourceWriter();

        // if nullability is supported emit the nullable directive
        if (model.Nullability != Nullability.None)
        {
            source.WriteLine(
                "#nullable " + (model.Nullability == Nullability.Enabled ? "enable" : "disable"));
        }

        source.WriteLine(
            $$"""
              #pragma warning disable
              namespace Refit.Implementation
              {

                  partial class Generated
                  {

                  /// <inheritdoc />
                  [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                  [global::System.Diagnostics.DebuggerNonUserCode]
                  [{{model.PreserveAttributeDisplayName}}]
                  [global::System.Reflection.Obfuscation(Exclude=true)]
                  [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                  partial class {{model.Ns}}{{model.ClassDeclaration}}
                      : {{model.InterfaceDisplayName}}
              """);

        source.Indentation += NamespaceAndClassIndentation;
        GenerateConstraints(source, model.Constraints, false);
        source.Indentation--;

        source.WriteLine(
            $$"""
              {
                  /// <inheritdoc />
                  public global::System.Net.Http.HttpClient Client { get; }
                  readonly global::Refit.IRequestBuilder requestBuilder;

                  /// <inheritdoc />
                  public {{model.Ns}}{{model.ClassSuffix}}(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
                  {
                      Client = client;
                      this.requestBuilder = requestBuilder;
                  }

              """);

        source.Indentation++;
        var uniqueNames = new UniqueNameBuilder();
        uniqueNames.Reserve(model.MemberNames);

        // Handle Refit Methods
        foreach (var method in model.RefitMethods)
        {
            WriteRefitMethod(source, method, true, uniqueNames);
        }

        foreach (var method in model.DerivedRefitMethods)
        {
            WriteRefitMethod(source, method, false, uniqueNames);
        }

        // Handle non-refit Methods that aren't static or properties or have a method body
        foreach (var method in model.NonRefitMethods)
        {
            WriteNonRefitMethod(source, method);
        }

        // Handle Dispose
        if (model.DisposeMethod)
        {
            WriteDisposableMethod(source);
        }

        source.Indentation -= NamespaceAndClassIndentation;
        source.WriteLine(
            """
                }
                }
            }

            #pragma warning restore
            """);
        return source.ToSourceText();
    }

    /// <summary>Generates the body of the Refit method.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    [SuppressMessage(
        "Usage",
        "CA2208:Instantiate argument exceptions correctly",
        Justification =
            "The ArgumentOutOfRangeException intentionally reports the offending model property (ReturnTypeMetadata) rather than a method parameter.")]
    private static void WriteRefitMethod(
        SourceWriter source,
        MethodModel methodModel,
        bool isTopLevel,
        UniqueNameBuilder uniqueNames)
    {
        var parameterTypesExpression = GenerateTypeParameterExpression(
            source,
            methodModel,
            uniqueNames);

        var returnType = methodModel.ReturnType;
        var (isAsync, @return, configureAwait) = methodModel.ReturnTypeMetadata switch
        {
            ReturnTypeInfo.AsyncVoid => (true, "await (", ").ConfigureAwait(false)"),
            ReturnTypeInfo.AsyncResult => (true, "return await (", ").ConfigureAwait(false)"),
            ReturnTypeInfo.Return => (false, "return ", string.Empty),
            ReturnTypeInfo.SyncVoid => (false, string.Empty, string.Empty),
            _ => throw new ArgumentOutOfRangeException(
                nameof(methodModel.ReturnTypeMetadata),
                methodModel.ReturnTypeMetadata,
                "Unsupported value.")
        };

        var isExplicit = methodModel.IsExplicitInterface || !isTopLevel;
        WriteMethodOpening(source, methodModel, isExplicit, isExplicit, isAsync);

        var argumentsArrayString = BuildArgumentsArrayLiteral(methodModel);
        var genericString = BuildGenericTypesArgument(methodModel);
        var lookupName = StripExplicitInterfacePrefix(methodModel.Name);

        var callExpression = methodModel.ReturnTypeMetadata == ReturnTypeInfo.SyncVoid
            ? "______func(this.Client, ______arguments);"
            : $"{@return}({returnType})______func(this.Client, ______arguments){configureAwait};";

        source.WriteLine(
            $"""
             var ______arguments = {argumentsArrayString};
             var ______func = requestBuilder.BuildRestResultFuncForMethod("{lookupName}", {parameterTypesExpression}{genericString} );

             {callExpression}
             """);

        WriteMethodClosing(source);
    }

    /// <summary>Builds the <c>object[]</c> literal that holds the method's argument values.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <returns>The arguments array expression.</returns>
    private static string BuildArgumentsArrayLiteral(MethodModel methodModel)
    {
        // Build the arguments array literal directly. This runs for every Refit method, so we
        // avoid LINQ Select/ToArray + string.Join and their intermediate array/iterator allocations.
        var parameters = methodModel.Parameters.AsArray();
        if (parameters.Length == 0)
        {
            return "global::System.Array.Empty<object>()";
        }

        var argsBuilder = new StringBuilder("new object[] { ");
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
            {
                argsBuilder.Append(", ");
            }

            argsBuilder.Append('@').Append(parameters[i].MetadataName);
        }

        argsBuilder.Append(" }");
        return argsBuilder.ToString();
    }

    /// <summary>Builds the optional generic <c>Type[]</c> argument for the request builder call.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <returns>The generic type argument, or an empty string when the method has no constraints.</returns>
    private static string BuildGenericTypesArgument(MethodModel methodModel)
    {
        var constraints = methodModel.Constraints.AsArray();
        if (constraints.Length == 0)
        {
            return string.Empty;
        }

        var genericBuilder = new StringBuilder(", new global::System.Type[] { ");
        for (var i = 0; i < constraints.Length; i++)
        {
            if (i > 0)
            {
                genericBuilder.Append(", ");
            }

            genericBuilder.Append("typeof(").Append(constraints[i].DeclaredName).Append(')');
        }

        genericBuilder.Append(" }");
        return genericBuilder.ToString();
    }

    /// <summary>Strips an explicit interface prefix from a method name (e.g. <c>IFoo.Bar</c> becomes <c>Bar</c>).</summary>
    /// <param name="name">The method name to normalize.</param>
    /// <returns>The method name without any explicit interface prefix.</returns>
    private static string StripExplicitInterfacePrefix(string name)
    {
        var lastDotIndex = name.LastIndexOf('.');
        return lastDotIndex >= 0 && lastDotIndex < name.Length - 1
            ? name.Substring(lastDotIndex + 1)
            : name;
    }

    /// <summary>Emits a stub body for a non-Refit method that throws at runtime.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    private static void WriteNonRefitMethod(SourceWriter source, MethodModel methodModel)
    {
        var isExplicit = methodModel.IsExplicitInterface;
        WriteMethodOpening(source, methodModel, isExplicit, isExplicit);

        source.WriteLine(
            "throw new global::System.NotImplementedException(\"Either this method has no Refit "
            + "HTTP method attribute or you've used something other than a string literal for the "
            + "'path' argument.\");");

        WriteMethodClosing(source);
    }

    /// <summary>Emits the explicit IDisposable.Dispose implementation.</summary>
    /// <param name="source">The source writer to emit to.</param>
    private static void WriteDisposableMethod(SourceWriter source) =>
        source.WriteLine(
            """

            /// <inheritdoc />
            void global::System.IDisposable.Dispose()
            {
                    Client?.Dispose();
            }
            """);

    /// <summary>Generates the expression used to pass the method's parameter types to the request builder.</summary>
    /// <param name="source">The source writer to emit any backing field to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <returns>The expression that resolves the parameter type array.</returns>
    private static string GenerateTypeParameterExpression(
        SourceWriter source,
        MethodModel methodModel,
        UniqueNameBuilder uniqueNames)
    {
        // use Array.Empty if method has no parameters.
        if (methodModel.Parameters.Count == 0)
        {
            return "global::System.Array.Empty<global::System.Type>()";
        }

        // if one of the parameters is/contains a type parameter then it cannot be cached as it will change type between calls.
        if (methodModel.Parameters.Any(x => x.IsGeneric))
        {
            var typeEnumerable = methodModel.Parameters.Select(param => $"typeof({param.Type})");
            return $"new global::System.Type[] {{ {string.Join(", ", typeEnumerable)} }}";
        }

        // find a name and generate field declaration.
        var typeParameterFieldName = uniqueNames.New(TypeParameterVariableName);
        var types = string.Join(", ", methodModel.Parameters.Select(x => $"typeof({x.Type})"));

        source.WriteLine(
            $$"""

              private static readonly global::System.Type[] {{typeParameterFieldName}} = new global::System.Type[] {{{types}} };
              """);

        return typeParameterFieldName;
    }

    /// <summary>Emits the method signature, constraints, and opening brace.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="isDerivedExplicitImpl">True if the method is a derived explicit implementation.</param>
    /// <param name="isExplicitInterface">True if the method is an explicit interface implementation.</param>
    /// <param name="isAsync">True if the method should be emitted as async.</param>
    [SuppressMessage(
        "Performance",
        "CA1834:Consider using 'StringBuilder.Append(char)' when applicable",
        Justification = "Generator emit path; keeping the string overload preserves the existing output.")]
    private static void WriteMethodOpening(
        SourceWriter source,
        MethodModel methodModel,
        bool isDerivedExplicitImpl,
        bool isExplicitInterface,
        bool isAsync = false)
    {
        var visibility = !isExplicitInterface ? "public " : string.Empty;
        var asyncKeyword = isAsync ? "async " : string.Empty;

        var builder = new StringBuilder();
        builder.Append(
            @$"/// <inheritdoc />
{visibility}{asyncKeyword}{methodModel.ReturnType} ");

        if (isExplicitInterface)
        {
            var ct = methodModel.ContainingType;
            if (!ct.StartsWith("global::", StringComparison.Ordinal))
            {
                ct = "global::" + ct;
            }

            builder.Append(@$"{ct}.");
        }

        builder.Append(@$"{methodModel.DeclaredMethod}(");

        var parameters = methodModel.Parameters.AsArray();
        if (parameters.Length > 0)
        {
            // Size known up front: use an array rather than a growing List.
            var list = new string[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var annotation = param.Annotation;
                list[i] = $@"{param.Type}{(annotation ? '?' : string.Empty)} @{param.MetadataName}";
            }

            builder.Append(string.Join(", ", list));
        }

        builder.Append(")");

        source.WriteLine();
        source.WriteLine(builder.ToString());
        source.Indentation++;
        GenerateConstraints(source, methodModel.Constraints, isDerivedExplicitImpl || isExplicitInterface);
        source.Indentation--;
        source.WriteLine("{");
        source.Indentation++;
    }

    /// <summary>Emits the closing brace for a method body.</summary>
    /// <param name="source">The source writer to emit to.</param>
    private static void WriteMethodClosing(SourceWriter source)
    {
        source.Indentation--;
        source.WriteLine("}");
    }

    /// <summary>Emits the generic type constraint clauses for the given type parameters.</summary>
    /// <param name="writer">The source writer to emit to.</param>
    /// <param name="typeParameters">The type parameter constraints to emit.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    private static void GenerateConstraints(
        SourceWriter writer,
        ImmutableEquatableArray<TypeConstraint> typeParameters,
        bool isOverrideOrExplicitImplementation)
    {
        // Need to loop over the constraints and create them
        foreach (var typeParameter in typeParameters)
        {
            WriteConstraintsForTypeParameter(
                writer,
                typeParameter,
                isOverrideOrExplicitImplementation);
        }
    }

    /// <summary>Emits the constraint clause for a single type parameter.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="typeParameter">The type parameter constraint to emit.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    private static void WriteConstraintsForTypeParameter(
        SourceWriter source,
        TypeConstraint typeParameter,
        bool isOverrideOrExplicitImplementation)
    {
        var parameters = CollectConstraintKeywords(typeParameter, isOverrideOrExplicitImplementation);
        if (parameters.Count == 0)
        {
            return;
        }

        source.WriteLine($"where {typeParameter.TypeName} : {string.Join(", ", parameters)}");
    }

    /// <summary>Collects the ordered constraint keywords that apply to a type parameter.</summary>
    /// <param name="typeParameter">The type parameter constraint to inspect.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    /// <returns>The constraint keywords in the order they must be emitted.</returns>
    private static List<string> CollectConstraintKeywords(
        TypeConstraint typeParameter,
        bool isOverrideOrExplicitImplementation)
    {
        // Explicit interface implementations and overrides can only have class or struct constraints
        var parameters = new List<string>();
        var knownConstraints = typeParameter.KnownTypeConstraint;
        if (knownConstraints.HasFlag(KnownTypeConstraint.Class))
        {
            parameters.Add("class");
        }

        if (
            knownConstraints.HasFlag(KnownTypeConstraint.Unmanaged)
            && !isOverrideOrExplicitImplementation
        )
        {
            parameters.Add("unmanaged");
        }

        if (knownConstraints.HasFlag(KnownTypeConstraint.Struct))
        {
            parameters.Add("struct");
        }

        if (
            knownConstraints.HasFlag(KnownTypeConstraint.NotNull)
            && !isOverrideOrExplicitImplementation
        )
        {
            parameters.Add("notnull");
        }

        if (!isOverrideOrExplicitImplementation)
        {
            parameters.AddRange(typeParameter.Constraints);
        }

        // new constraint has to be last
        if (
            knownConstraints.HasFlag(KnownTypeConstraint.New) && !isOverrideOrExplicitImplementation
        )
        {
            parameters.Add("new()");
        }

        return parameters;
    }
}
