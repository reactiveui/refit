﻿using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator;

internal static class Emitter
{
    private const string TypeParameterVariableName = "______typeParameters";

    public static void EmitSharedCode(
        ContextGenerationModel model,
        Action<string, SourceText> addSource
    )
    {
        if (model.Interfaces.Count == 0)
            return;

        var attributeText = $$"""

            #pragma warning disable
            namespace {{model.RefitInternalNamespace}}
            {
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                [global::System.AttributeUsage (global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct | global::System.AttributeTargets.Enum | global::System.AttributeTargets.Constructor | global::System.AttributeTargets.Method | global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Event | global::System.AttributeTargets.Interface | global::System.AttributeTargets.Delegate)]
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
                    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(global::Refit.Implementation.Generated))]
                    public static void Initialize()
                    {
                    }
            #endif
                }
            }
            #pragma warning restore

            """;
        addSource("Generated.g.cs", SourceText.From(generatedClassText, Encoding.UTF8));
    }

    public static SourceText EmitInterface(InterfaceModel model)
    {
        var source = new SourceWriter();

        // if nullability is supported emit the nullable directive
        if (model.Nullability != Nullability.None)
        {
            source.WriteLine(
                "#nullable " + (model.Nullability == Nullability.Enabled ? "enable" : "disable")
            );
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
            """
        );

        source.Indentation += 2;
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

            """
        );

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

        source.Indentation -= 2;
        source.WriteLine(
            """
                }
                }
            }

            #pragma warning restore
            """
        );
        return source.ToSourceText();
    }

    /// <summary>
    /// Generates the body of the Refit method
    /// </summary>
    /// <param name="source"></param>
    /// <param name="methodModel"></param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    private static void WriteRefitMethod(
        SourceWriter source,
        MethodModel methodModel,
        bool isTopLevel,
        UniqueNameBuilder uniqueNames
    )
    {
        var parameterTypesExpression = GenerateTypeParameterExpression(
            source,
            methodModel,
            uniqueNames
        );

        var returnType = methodModel.ReturnType;
        var (isAsync, @return, configureAwait) = methodModel.ReturnTypeMetadata switch
        {
            ReturnTypeInfo.AsyncVoid => (true, "await (", ").ConfigureAwait(false)"),
            ReturnTypeInfo.AsyncResult => (true, "return await (", ").ConfigureAwait(false)"),
            ReturnTypeInfo.Return => (false, "return ", ""),
            _ => throw new ArgumentOutOfRangeException(
                nameof(methodModel.ReturnTypeMetadata),
                methodModel.ReturnTypeMetadata,
                "Unsupported value."
            ),
        };

        var isExplicit = methodModel.IsExplicitInterface || !isTopLevel;
        WriteMethodOpening(source, methodModel, isExplicit, isExplicit, isAsync);

        // Build the list of args for the array
        var argArray = methodModel
            .Parameters.AsArray()
            .Select(static param => $"@{param.MetadataName}")
            .ToArray();

        // List of generic arguments
        var genericArray = methodModel
            .Constraints.AsArray()
            .Select(static typeParam => $"typeof({typeParam.DeclaredName})")
            .ToArray();

        var argumentsArrayString =
            argArray.Length == 0
                ? "global::System.Array.Empty<object>()"
                : $"new object[] {{ {string.Join(", ", argArray)} }}";

        var genericString =
            genericArray.Length > 0
                ? $", new global::System.Type[] {{ {string.Join(", ", genericArray)} }}"
                : string.Empty;

        // Normalize method lookup key: strip explicit interface prefix if present (e.g. IFoo.Bar -> Bar)
        var lookupName = methodModel.Name;
        var lastDotIndex = lookupName.LastIndexOf('.');
        if (lastDotIndex >= 0 && lastDotIndex < lookupName.Length - 1)
        {
            lookupName = lookupName.Substring(lastDotIndex + 1);
        }

        source.WriteLine(
            $"""
            var ______arguments = {argumentsArrayString};
            var ______func = requestBuilder.BuildRestResultFuncForMethod("{lookupName}", {parameterTypesExpression}{genericString} );

            {@return}({returnType})______func(this.Client, ______arguments){configureAwait};
            """
        );

        WriteMethodClosing(source);
    }

    private static void WriteNonRefitMethod(SourceWriter source, MethodModel methodModel)
    {
        var isExplicit = methodModel.IsExplicitInterface;
        WriteMethodOpening(source, methodModel, isExplicit, isExplicit);

        source.WriteLine(
            @"throw new global::System.NotImplementedException(""Either this method has no Refit HTTP method attribute or you've used something other than a string literal for the 'path' argument."");"
        );

        WriteMethodClosing(source);
    }

    private static void WriteDisposableMethod(SourceWriter source)
    {
        source.WriteLine(
            """

            /// <inheritdoc />
            void global::System.IDisposable.Dispose()
            {
                    Client?.Dispose();
            }
            """
        );
    }

    private static string GenerateTypeParameterExpression(
        SourceWriter source,
        MethodModel methodModel,
        UniqueNameBuilder uniqueNames
    )
    {
        // use Array.Empty if method has no parameters.
        if (methodModel.Parameters.Count == 0)
            return "global::System.Array.Empty<global::System.Type>()";

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
            """
        );

        return typeParameterFieldName;
    }

    private static void WriteMethodOpening(
        SourceWriter source,
        MethodModel methodModel,
        bool isDerivedExplicitImpl,
        bool isExplicitInterface,
        bool isAsync = false
    )
    {
        var visibility = !isExplicitInterface ? "public " : string.Empty;
        var async = isAsync ? "async " : "";

        var builder = new StringBuilder();
        builder.Append(
            @$"/// <inheritdoc />
{visibility}{async}{methodModel.ReturnType} "
        );

        if (isExplicitInterface)
        {
            var ct = methodModel.ContainingType;
            if (!ct.StartsWith("global::"))
            {
                ct = "global::" + ct;
            }
            builder.Append(@$"{ct}.");
        }
        builder.Append(@$"{methodModel.DeclaredMethod}(");

        if (methodModel.Parameters.Count > 0)
        {
            var list = new List<string>();
            foreach (var param in methodModel.Parameters)
            {
                var annotation = param.Annotation;
                list.Add($@"{param.Type}{(annotation ? '?' : string.Empty)} @{param.MetadataName}");
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

    private static void WriteMethodClosing(SourceWriter source)
    {
        source.Indentation--;
        source.WriteLine("}");
    }

    private static void GenerateConstraints(
        SourceWriter writer,
        ImmutableEquatableArray<TypeConstraint> typeParameters,
        bool isOverrideOrExplicitImplementation
    )
    {
        // Need to loop over the constraints and create them
        foreach (var typeParameter in typeParameters)
        {
            WriteConstraintsForTypeParameter(
                writer,
                typeParameter,
                isOverrideOrExplicitImplementation
            );
        }
    }

    private static void WriteConstraintsForTypeParameter(
        SourceWriter source,
        TypeConstraint typeParameter,
        bool isOverrideOrExplicitImplementation
    )
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

        if (parameters.Count > 0)
        {
            source.WriteLine($"where {typeParameter.TypeName} : {string.Join(", ", parameters)}");
        }
    }
}
