// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator;

/// <summary>Emits the generated source code for Refit interface implementations.</summary>
[SuppressMessage(
    "Style",
    "SST1202:Members should be ordered by accessibility",
    Justification = "Internal helper seams are kept next to their production call sites to preserve generator readability.")]
internal static class Emitter
{
    /// <summary>The generated literal for <see langword="false"/>.</summary>
    private const string FalseLiteral = "false";

    /// <summary>The generated literal for <see langword="true"/>.</summary>
    private const string TrueLiteral = "true";

    /// <summary>The C# global namespace alias prefix.</summary>
    private const string GlobalPrefix = "global::";

    /// <summary>The variable name used for the cached type parameter array field.</summary>
    private const string TypeParameterVariableName = "______typeParameters";

    /// <summary>Indentation levels spanned by the generated namespace and class nesting.</summary>
    private const int NamespaceAndClassIndentation = 2;

    /// <summary>Initial buffer size for shared generated infrastructure source.</summary>
    private const int SharedGeneratedSourceBaseCapacity = 2048;

    /// <summary>Estimated characters needed for one generated factory registration.</summary>
    private const int EstimatedFactoryRegistrationCapacity = 256;

    /// <summary>Initial buffer size for a typical generated interface implementation.</summary>
    private const int InterfaceSourceBaseCapacity = 4096;

    /// <summary>Estimated characters needed for one generated method body.</summary>
    private const int EstimatedMethodSourceCapacity = 768;

    /// <summary>Estimated characters needed for one generated property.</summary>
    private const int EstimatedPropertySourceCapacity = 128;

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

                              // This file is generated into consumer projects; suppress all analyzers so
                              // consumer analyzer policy does not report Refit implementation details.
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

        const string dynamicDependencyLine =
            "[System.Diagnostics.CodeAnalysis.DynamicDependency("
            + "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, "
            + "typeof(global::Refit.Implementation.Generated))]";

        var generatedSource = new SourceWriter(EstimateSharedSourceCapacity(model));
        generatedSource.WriteLine(
            $$"""

              // This file is generated into consumer projects; suppress all analyzers so
              // consumer analyzer policy does not report Refit implementation details.
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
              """);
        WriteGeneratedFactoryRegistrations(generatedSource, model.Interfaces);
        generatedSource.WriteLine(
            """
                    }
            #endif
                }
            }
            #pragma warning restore
            """);
        addSource("Generated.g.cs", generatedSource.ToSourceText());
    }

    /// <summary>Emits the generated implementation source for a single interface.</summary>
    /// <param name="model">The interface model to emit.</param>
    /// <returns>The generated source text for the interface implementation.</returns>
    public static SourceText EmitInterface(InterfaceModel model)
    {
        var source = new SourceWriter(EstimateInterfaceSourceCapacity(model));

        // if nullability is supported emit the nullable directive
        if (model.Nullability != Nullability.None)
        {
            source.WriteLine(
                "#nullable " + (model.Nullability == Nullability.Enabled ? "enable" : "disable"));
        }

        source.WriteLine(
            $$"""
              // This file is generated into consumer projects; suppress all analyzers so
              // consumer analyzer policy does not report Refit implementation details.
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

        foreach (var property in model.Properties)
        {
            WriteInterfaceProperty(source, property);
        }

        // Handle Refit Methods
        foreach (var method in model.RefitMethods)
        {
            WriteRefitMethod(source, method, true, model, uniqueNames);
        }

        foreach (var method in model.DerivedRefitMethods)
        {
            WriteRefitMethod(source, method, false, model, uniqueNames);
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

    /// <summary>Estimates the initial buffer size for shared generated infrastructure source.</summary>
    /// <param name="model">The context generation model describing the interfaces.</param>
    /// <returns>The estimated source buffer capacity.</returns>
    private static int EstimateSharedSourceCapacity(ContextGenerationModel model) =>
        SharedGeneratedSourceBaseCapacity + (model.Interfaces.Count * EstimatedFactoryRegistrationCapacity);

    /// <summary>Estimates the initial buffer size for one generated interface implementation.</summary>
    /// <param name="model">The interface model being emitted.</param>
    /// <returns>The estimated source buffer capacity.</returns>
    private static int EstimateInterfaceSourceCapacity(InterfaceModel model)
    {
        var methodCount =
            model.RefitMethods.Count + model.DerivedRefitMethods.Count + model.NonRefitMethods.Count;
        return InterfaceSourceBaseCapacity
            + (methodCount * EstimatedMethodSourceCapacity)
            + (model.Properties.Count * EstimatedPropertySourceCapacity);
    }

    /// <summary>Writes the generated factory registrations for non-generic interfaces.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="interfaces">The parsed interface models.</param>
    private static void WriteGeneratedFactoryRegistrations(
        SourceWriter source,
        ImmutableEquatableArray<InterfaceModel> interfaces)
    {
        for (var i = 0; i < interfaces.Count; i++)
        {
            var interfaceModel = interfaces[i];
            if (interfaceModel.ClassDeclaration.Contains("<"))
            {
                continue;
            }

            source.WriteLine("                        global::Refit.RestService.RegisterGeneratedFactory(");
            source.Append("                            typeof(");
            source.Append(interfaceModel.InterfaceDisplayName);
            source.WriteLine("),");
            source.Append(
                "                            static (client, requestBuilder) => new global::Refit.Implementation.Generated.");
            source.Append(interfaceModel.Ns);
            source.Append(interfaceModel.ClassSuffix);
            source.WriteLine("(client, requestBuilder));");
        }
    }

    /// <summary>Generates the body of the Refit method.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
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
        InterfaceModel interfaceModel,
        UniqueNameBuilder uniqueNames)
    {
        if (interfaceModel.GeneratedRequestBuilding && methodModel.Request.CanGenerateInline)
        {
            WriteInlineRefitMethod(source, methodModel, interfaceModel, isTopLevel);
            return;
        }

        var cachedTypeParameterFieldName = GenerateTypeParameterField(
            source,
            methodModel,
            uniqueNames);

        var returnType = methodModel.ReturnType;
        var (isAsync, @return, configureAwait) = GetReturnInvocationParts(methodModel.ReturnTypeMetadata);

        var isExplicit = methodModel.IsExplicitInterface || !isTopLevel;
        WriteMethodOpening(source, methodModel, isExplicit, isExplicit, isAsync);

        var lookupName = StripExplicitInterfacePrefix(methodModel.Name);

        source.WriteIndentation();
        source.Append("var ______arguments = ");
        AppendArgumentsArrayLiteral(source, methodModel);
        source.Append(';');
        source.WriteLine();

        source.WriteIndentation();
        source.Append("var ______func = requestBuilder.BuildRestResultFuncForMethod(\"");
        source.Append(lookupName);
        source.Append("\", ");
        AppendTypeParameterExpression(source, methodModel.Parameters, cachedTypeParameterFieldName);
        AppendGenericTypesArgument(source, methodModel);
        source.Append(" );");
        source.WriteLine();

        source.WriteLine();
        if (methodModel.ReturnTypeMetadata == ReturnTypeInfo.SyncVoid)
        {
            source.WriteLine("______func(this.Client, ______arguments);");
        }
        else
        {
            source.WriteIndentation();
            source.Append(@return);
            source.Append('(');
            source.Append(returnType);
            source.Append(")______func(this.Client, ______arguments)");
            source.Append(configureAwait);
            source.Append(';');
            source.WriteLine();
        }

        WriteMethodClosing(source);
    }

    /// <summary>Generates a Refit method that constructs the request directly in generated code.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces.</param>
    private static void WriteInlineRefitMethod(
        SourceWriter source,
        MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isTopLevel)
    {
        var isExplicit = methodModel.IsExplicitInterface || !isTopLevel;
        WriteMethodOpening(source, methodModel, isExplicit, isExplicit);

        var request = methodModel.Request;
        var bodyParameter = FindRequestParameter(request, RequestParameterKind.Body);
        var cancellationTokenExpression = BuildCancellationTokenExpression(request);
        var bufferBodyExpression = BuildBufferBodyExpression(bodyParameter);

        source.WriteLine("var ______settings = requestBuilder.Settings;");
        source.WriteLine(
            """var ______basePath = this.Client.BaseAddress?.AbsolutePath ?? throw new global::System.InvalidOperationException("BaseAddress must be set on the HttpClient instance");""");
        source.WriteLine(
            "______basePath = ______basePath == \"/\" ? string.Empty : ______basePath.TrimEnd('/');");
        var requestUriExpression =
            $"new global::System.Uri(______basePath + {ToCSharpStringLiteral(request.Path)}, global::System.UriKind.Relative)";
        source.WriteLine(
            $"var ______rq = new global::System.Net.Http.HttpRequestMessage({ToHttpMethodExpression(request.HttpMethod)}, {requestUriExpression});");
        source.WriteLine(
            """
            #if NET6_0_OR_GREATER
            ______rq.Version = ______settings.Version;
            ______rq.VersionPolicy = ______settings.VersionPolicy;
            #endif
            """);

        if (bodyParameter is not null)
        {
            var streamBodyExpression = BuildStreamBodyExpression(bodyParameter);
            var serializationMethodExpression = BuildBodySerializationMethodExpression(bodyParameter);
            var contentExpression =
                $"""
                 global::Refit.GeneratedRequestRunner.CreateBodyContent<{bodyParameter.Type}>(
                     ______settings,
                     @{bodyParameter.Name},
                     {serializationMethodExpression},
                     {streamBodyExpression})
                 """;
            source.WriteLine(
                $"______rq.Content = {contentExpression};");
        }

        WriteInlineHeaders(source, request);
        WriteInlineRequestProperties(source, request, interfaceModel);
        WriteInlineReturn(source, methodModel, request, bufferBodyExpression, cancellationTokenExpression);
        WriteMethodClosing(source);
    }

    /// <summary>Emits the return statement for an inline generated Refit method.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="request">The parsed request model.</param>
    /// <param name="bufferBodyExpression">The expression indicating whether request content should be buffered.</param>
    /// <param name="cancellationTokenExpression">The cancellation token expression.</param>
    private static void WriteInlineReturn(
        SourceWriter source,
        MethodModel methodModel,
        RequestModel request,
        string bufferBodyExpression,
        string cancellationTokenExpression)
    {
        if (methodModel.ReturnTypeMetadata == ReturnTypeInfo.AsyncVoid)
        {
            var sendVoidExpression =
                $"""
                 global::Refit.GeneratedRequestRunner.SendVoidAsync(
                     this.Client,
                     ______rq,
                     ______settings,
                     {bufferBodyExpression},
                     {cancellationTokenExpression})
                 """;
            source.WriteLine(
                $"return {sendVoidExpression};");
            return;
        }

        var sendExpression =
            $"""
             global::Refit.GeneratedRequestRunner.SendAsync<{request.ResultType}, {request.DeserializedResultType}>(
                 this.Client,
                 ______rq,
                 ______settings,
                 {ToLowerInvariantString(request.IsApiResponse)},
                 {ToLowerInvariantString(request.ShouldDisposeResponse)},
                 {bufferBodyExpression},
                 {cancellationTokenExpression})
             """;

        if (methodModel.ReturnType.StartsWith("global::System.Threading.Tasks.ValueTask<", StringComparison.Ordinal))
        {
            source.WriteLine($"return new {methodModel.ReturnType}({sendExpression});");
            return;
        }

        source.WriteLine($"return {sendExpression};");
    }

    /// <summary>Emits static and dynamic header application for an inline generated method.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="request">The parsed request model.</param>
    private static void WriteInlineHeaders(SourceWriter source, RequestModel request)
    {
        foreach (var header in request.StaticHeaders)
        {
            source.WriteLine(
                $"global::Refit.GeneratedRequestRunner.SetHeader(______rq, {ToCSharpStringLiteral(header.Name)}, "
                + $"{ToNullableCSharpStringLiteral(header.Value)});");
        }

        foreach (var parameter in request.Parameters)
        {
            switch (parameter.Kind)
            {
                case RequestParameterKind.Header:
                    {
                        source.WriteLine(
                                            $"global::Refit.GeneratedRequestRunner.SetHeader(______rq, {ToCSharpStringLiteral(parameter.HeaderName)}, "
                                            + $"{BuildHeaderValueExpression(parameter)});");
                        continue;
                    }

                case RequestParameterKind.HeaderCollection:
                    {
                        source.WriteLine(
                                            $"global::Refit.GeneratedRequestRunner.AddHeaderCollection(______rq, @{parameter.Name});");
                        break;
                    }
            }
        }
    }

    /// <summary>Builds a header value expression without null-conditionals on non-nullable value types.</summary>
    /// <param name="parameter">The header parameter to format.</param>
    /// <returns>The generated header value expression.</returns>
    private static string BuildHeaderValueExpression(RequestParameterModel parameter)
    {
        var parameterExpression = $"@{parameter.Name}";
        return parameter.CanBeNull
            ? $"{parameterExpression}?.ToString()"
            : $"{parameterExpression}.ToString()";
    }

    /// <summary>Emits request-option/property application for an inline generated method.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="request">The parsed request model.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    private static void WriteInlineRequestProperties(
        SourceWriter source,
        RequestModel request,
        InterfaceModel interfaceModel)
    {
        source.WriteLine(
            "global::Refit.GeneratedRequestRunner.AddConfiguredRequestOptions(______rq, ______settings, "
            + $"typeof({interfaceModel.InterfaceDisplayName}));");

        foreach (var property in interfaceModel.Properties)
        {
            if (property.RequestPropertyKey.Length == 0 || !property.HasGetter)
            {
                continue;
            }

            source.WriteLine(
                $"global::Refit.GeneratedRequestRunner.AddRequestProperty<{property.Type}>"
                + $"(______rq, {ToCSharpStringLiteral(property.RequestPropertyKey)}, "
                + $"{BuildPropertyAccessExpression(property)});");
        }

        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind == RequestParameterKind.Property)
            {
                source.WriteLine(
                    $"global::Refit.GeneratedRequestRunner.AddRequestProperty<{parameter.Type}>"
                    + $"(______rq, {ToCSharpStringLiteral(parameter.PropertyKey)}, @{parameter.Name});");
            }
        }
    }

    /// <summary>Finds the first request parameter of the given kind.</summary>
    /// <param name="request">The request model to inspect.</param>
    /// <param name="kind">The parameter kind to find.</param>
    /// <returns>The parameter model, if present.</returns>
    private static RequestParameterModel? FindRequestParameter(RequestModel request, RequestParameterKind kind)
    {
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind == kind)
            {
                return parameter;
            }
        }

        return null;
    }

    /// <summary>Builds the cancellation token expression for an inline generated method.</summary>
    /// <param name="request">The request model to inspect.</param>
    /// <returns>The cancellation token expression.</returns>
    private static string BuildCancellationTokenExpression(RequestModel request)
    {
        var cancellationToken = FindRequestParameter(request, RequestParameterKind.CancellationToken);
        if (cancellationToken is null)
        {
            return "global::System.Threading.CancellationToken.None";
        }

        return cancellationToken.CanBeNull
            ? $"@{cancellationToken.Name}.GetValueOrDefault()"
            : $"@{cancellationToken.Name}";
    }

    /// <summary>Builds the request-body buffering expression for an inline generated method.</summary>
    /// <param name="bodyParameter">The parsed body parameter, if any.</param>
    /// <returns>The buffering expression.</returns>
    internal static string BuildBufferBodyExpression(RequestParameterModel? bodyParameter) =>
        bodyParameter is null
            ? FalseLiteral
            : bodyParameter.BodyBufferMode switch
            {
                BodyBufferMode.Settings => "______settings.Buffered",
                BodyBufferMode.Buffered => TrueLiteral,
                _ => FalseLiteral
            };

    /// <summary>Builds the serialized-body streaming expression for an inline generated method.</summary>
    /// <param name="bodyParameter">The parsed body parameter.</param>
    /// <returns>The streaming expression.</returns>
    internal static string BuildStreamBodyExpression(RequestParameterModel bodyParameter) =>
        bodyParameter.BodySerializationMethod == "UrlEncoded"
            ? FalseLiteral
            : bodyParameter.BodyBufferMode switch
            {
                BodyBufferMode.Settings => "!______settings.Buffered",
                BodyBufferMode.Buffered => FalseLiteral,
                BodyBufferMode.Streaming => TrueLiteral,
                _ => FalseLiteral
            };

    /// <summary>Builds the body serialization enum expression for an inline generated method.</summary>
    /// <param name="bodyParameter">The parsed body parameter.</param>
    /// <returns>The serialization method expression.</returns>
    private static string BuildBodySerializationMethodExpression(RequestParameterModel bodyParameter)
    {
        var serializationMethod = bodyParameter.BodySerializationMethod == "Json"
            ? "Serialized"
            : bodyParameter.BodySerializationMethod;
        return $"global::Refit.BodySerializationMethod.{serializationMethod}";
    }

    /// <summary>Builds the expression used to read an implemented interface property.</summary>
    /// <param name="property">The property model.</param>
    /// <returns>The generated property access expression.</returns>
    internal static string BuildPropertyAccessExpression(InterfacePropertyModel property)
    {
        if (property.IsSatisfiedByGeneratedMember)
        {
            return "this.Client";
        }

        return property.IsExplicitInterface
            ? $"(({EnsureGlobalPrefix(property.ContainingType)})this).{property.Name}"
            : $"this.{property.Name}";
    }

    /// <summary>Ensures a type display name is prefixed with <c>global::</c>.</summary>
    /// <param name="typeName">The type display name.</param>
    /// <returns>The globally qualified type display name.</returns>
    internal static string EnsureGlobalPrefix(string typeName) =>
        typeName.StartsWith(GlobalPrefix, StringComparison.Ordinal)
            ? typeName
            : GlobalPrefix + typeName;

    /// <summary>Maps a parsed HTTP method name to an expression that creates or returns an <see cref="HttpMethod"/>.</summary>
    /// <param name="httpMethod">The HTTP method text.</param>
    /// <returns>The HTTP method expression.</returns>
    [ExcludeFromCodeCoverage]
    internal static string ToHttpMethodExpression(string httpMethod) =>
        httpMethod switch
        {
            "DELETE" => "global::System.Net.Http.HttpMethod.Delete",
            "GET" => "global::System.Net.Http.HttpMethod.Get",
            "HEAD" => "global::System.Net.Http.HttpMethod.Head",
            "OPTIONS" => "global::System.Net.Http.HttpMethod.Options",
            "POST" => "global::System.Net.Http.HttpMethod.Post",
            "PUT" => "global::System.Net.Http.HttpMethod.Put",
            "PATCH" => "new global::System.Net.Http.HttpMethod(\"PATCH\")",
            _ => throw new ArgumentOutOfRangeException(nameof(httpMethod), httpMethod, "Unsupported HTTP method.")
        };

    /// <summary>Gets the invocation text used for a generated method return type.</summary>
    /// <param name="returnTypeInfo">The method return type shape.</param>
    /// <returns>The async flag, return prefix, and configure-await suffix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="returnTypeInfo"/> is unsupported.</exception>
    internal static (bool IsAsync, string ReturnPrefix, string ConfigureAwaitSuffix) GetReturnInvocationParts(
        ReturnTypeInfo returnTypeInfo) =>
        returnTypeInfo switch
        {
            ReturnTypeInfo.AsyncVoid => (true, "await (", ").ConfigureAwait(false)"),
            ReturnTypeInfo.AsyncResult => (true, "return await (", ").ConfigureAwait(false)"),
            ReturnTypeInfo.Return => (false, "return ", string.Empty),
            ReturnTypeInfo.SyncVoid => (false, string.Empty, string.Empty),
            _ => throw new ArgumentOutOfRangeException(
                nameof(returnTypeInfo),
                returnTypeInfo,
                "Unsupported value.")
        };

    /// <summary>Converts a bool to a lowercase C# literal.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The lowercase bool literal.</returns>
    private static string ToLowerInvariantString(bool value) => value ? TrueLiteral : FalseLiteral;

    /// <summary>Converts a string into a C# string literal.</summary>
    /// <param name="value">The value to quote.</param>
    /// <returns>The escaped C# string literal.</returns>
    private static string ToCSharpStringLiteral(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var c in value)
        {
            AppendEscapedCharacter(builder, c);
        }

        builder.Append('"');
        return builder.ToString();
    }

    /// <summary>Converts a nullable string into a C# string literal or null literal.</summary>
    /// <param name="value">The value to quote.</param>
    /// <returns>The generated expression.</returns>
    internal static string ToNullableCSharpStringLiteral(string? value) =>
        value is null ? "null" : ToCSharpStringLiteral(value);

    /// <summary>Appends one escaped C# string-literal character.</summary>
    /// <param name="builder">The target builder.</param>
    /// <param name="character">The character to append.</param>
    [SuppressMessage(
        "CodeQuality",
        "S1541:Methods and properties should not be too complex",
        Justification = "A compact switch avoids a dictionary or repeated helper calls on the generator hot path.")]
    internal static void AppendEscapedCharacter(StringBuilder builder, char character) =>
        _ = character switch
        {
            '\\' => builder.Append(@"\\"),
            '"' => builder.Append("\\\""),
            '\0' => builder.Append(@"\0"),
            '\a' => builder.Append(@"\a"),
            '\b' => builder.Append(@"\b"),
            '\f' => builder.Append(@"\f"),
            '\n' => builder.Append(@"\n"),
            '\r' => builder.Append(@"\r"),
            '\t' => builder.Append(@"\t"),
            '\v' => builder.Append(@"\v"),
            _ => builder.Append(character)
        };

    /// <summary>Appends the <c>object[]</c> literal that holds the method's argument values.</summary>
    /// <param name="source">The source writer to append to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    private static void AppendArgumentsArrayLiteral(SourceWriter source, MethodModel methodModel)
    {
        // Build the arguments array literal directly. This runs for every Refit method, so we
        // avoid LINQ Select/ToArray + string.Join and their intermediate array/iterator allocations.
        var parameters = methodModel.Parameters.AsArray();
        if (parameters.Length == 0)
        {
            source.Append("global::System.Array.Empty<object>()");
            return;
        }

        source.Append("new object[] { ");
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
            {
                source.Append(", ");
            }

            source.Append('@');
            source.Append(parameters[i].MetadataName);
        }

        source.Append(" }");
    }

    /// <summary>Appends the optional generic <c>Type[]</c> argument for the request builder call.</summary>
    /// <param name="source">The source writer to append to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    private static void AppendGenericTypesArgument(SourceWriter source, MethodModel methodModel)
    {
        var constraints = methodModel.Constraints.AsArray();
        if (constraints.Length == 0)
        {
            return;
        }

        source.Append(", new global::System.Type[] { ");
        for (var i = 0; i < constraints.Length; i++)
        {
            if (i > 0)
            {
                source.Append(", ");
            }

            source.Append("typeof(");
            source.Append(constraints[i].DeclaredName);
            source.Append(')');
        }

        source.Append(" }");
    }

    /// <summary>Strips an explicit interface prefix from a method name (e.g. <c>IFoo.Bar</c> becomes <c>Bar</c>).</summary>
    /// <param name="name">The method name to normalize.</param>
    /// <returns>The method name without any explicit interface prefix.</returns>
    internal static string StripExplicitInterfacePrefix(string name)
    {
        var lastDotIndex = name.LastIndexOf('.');
        return lastDotIndex >= 0 && lastDotIndex < name.Length - 1
            ? name[(lastDotIndex + 1)..]
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

    /// <summary>Emits an interface property implementation.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="property">The property model being emitted.</param>
    private static void WriteInterfaceProperty(SourceWriter source, InterfacePropertyModel property)
    {
        if (property.IsSatisfiedByGeneratedMember)
        {
            return;
        }

        var visibility = property.IsExplicitInterface ? string.Empty : "public ";
        var annotation = property.Annotation ? "?" : string.Empty;
        var explicitInterface = property.IsExplicitInterface
            ? EnsureGlobalPrefix(property.ContainingType) + "."
            : string.Empty;
        var getter = property.HasGetter ? " get;" : string.Empty;
        var setter = property.HasSetter ? " set;" : string.Empty;

        source.WriteLine(
            $$"""

              /// <inheritdoc />
              {{visibility}}{{property.Type}}{{annotation}} {{explicitInterface}}{{property.Name}} { {{getter}}{{setter}} }
              """);
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

    /// <summary>Generates a cached field for non-generic method parameter types, when possible.</summary>
    /// <param name="source">The source writer to emit any backing field to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <returns>The generated field name, or <see langword="null"/> when the type array must be emitted inline.</returns>
    private static string? GenerateTypeParameterField(
        SourceWriter source,
        MethodModel methodModel,
        UniqueNameBuilder uniqueNames)
    {
        // Use Array.Empty when there are no parameters and inline arrays when method type parameters are involved.
        if (methodModel.Parameters.Count == 0 || ContainsGenericParameter(methodModel.Parameters))
        {
            return null;
        }

        // find a name and generate field declaration.
        var typeParameterFieldName = uniqueNames.New(TypeParameterVariableName);

        source.WriteLine();
        source.WriteIndentation();
        source.Append("private static readonly global::System.Type[] ");
        source.Append(typeParameterFieldName);
        source.Append(" = new global::System.Type[] {");
        AppendParameterTypeList(source, methodModel.Parameters);
        source.Append(" };");
        source.WriteLine();

        return typeParameterFieldName;
    }

    /// <summary>Determines whether any parameter type depends on a method type parameter.</summary>
    /// <param name="parameters">The parameter models to inspect.</param>
    /// <returns>True when at least one parameter is generic.</returns>
    private static bool ContainsGenericParameter(ImmutableEquatableArray<ParameterModel> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].IsGeneric)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Appends the expression used to pass the method's parameter types to the request builder.</summary>
    /// <param name="source">The source writer to append to.</param>
    /// <param name="parameters">The parameter models to emit.</param>
    /// <param name="cachedTypeParameterFieldName">The cached field name, if one was generated.</param>
    private static void AppendTypeParameterExpression(
        SourceWriter source,
        ImmutableEquatableArray<ParameterModel> parameters,
        string? cachedTypeParameterFieldName)
    {
        if (parameters.Count == 0)
        {
            source.Append("global::System.Array.Empty<global::System.Type>()");
            return;
        }

        if (cachedTypeParameterFieldName is not null)
        {
            source.Append(cachedTypeParameterFieldName);
            return;
        }

        source.Append("new global::System.Type[] { ");
        AppendParameterTypeList(source, parameters);
        source.Append(" }");
    }

    /// <summary>Appends the generated <c>typeof(...)</c> argument list for method parameters.</summary>
    /// <param name="source">The source writer to append to.</param>
    /// <param name="parameters">The parameter models to emit.</param>
    private static void AppendParameterTypeList(
        SourceWriter source,
        ImmutableEquatableArray<ParameterModel> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                source.Append(", ");
            }

            source.Append("typeof(");
            source.Append(parameters[i].Type);
            source.Append(')');
        }
    }

    /// <summary>Emits the method signature, constraints, and opening brace.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="isDerivedExplicitImpl">True if the method is a derived explicit implementation.</param>
    /// <param name="isExplicitInterface">True if the method is an explicit interface implementation.</param>
    /// <param name="isAsync">True if the method should be emitted as async.</param>
    internal static void WriteMethodOpening(
        SourceWriter source,
        MethodModel methodModel,
        bool isDerivedExplicitImpl,
        bool isExplicitInterface,
        bool isAsync = false)
    {
        var visibility = !isExplicitInterface ? "public " : string.Empty;
        var asyncKeyword = isAsync ? "async " : string.Empty;

        source.WriteLine();
        source.WriteLine("/// <inheritdoc />");
        source.WriteIndentation();
        source.Append(visibility);
        source.Append(asyncKeyword);
        source.Append(methodModel.ReturnType);
        source.Append(' ');

        if (isExplicitInterface)
        {
            var ct = methodModel.ContainingType;
            if (!ct.StartsWith(GlobalPrefix, StringComparison.Ordinal))
            {
                source.Append(GlobalPrefix);
            }

            source.Append(ct);
            source.Append('.');
        }

        source.Append(methodModel.DeclaredMethod);
        source.Append('(');

        var parameters = methodModel.Parameters.AsArray();
        if (parameters.Length > 0)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    source.Append(", ");
                }

                var (metadataName, type, annotation, _) = parameters[i];
                source.Append(type);
                if (annotation)
                {
                    source.Append('?');
                }

                source.Append(" @");
                source.Append(metadataName);
            }
        }

        source.Append(')');
        source.WriteLine();
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
        in TypeConstraint typeParameter,
        bool isOverrideOrExplicitImplementation)
    {
        if (!HasConstraintKeywords(typeParameter, isOverrideOrExplicitImplementation))
        {
            return;
        }

        var wroteConstraint = false;
        source.WriteIndentation();
        source.Append("where ");
        source.Append(typeParameter.TypeName);
        source.Append(" : ");

        var knownConstraints = typeParameter.KnownTypeConstraint;
        AppendConstraintKeywordIf(source, "class", knownConstraints.HasFlag(KnownTypeConstraint.Class), ref wroteConstraint);
        AppendConstraintKeywordIf(
            source,
            "unmanaged",
            knownConstraints.HasFlag(KnownTypeConstraint.Unmanaged) && !isOverrideOrExplicitImplementation,
            ref wroteConstraint);
        AppendConstraintKeywordIf(source, "struct", knownConstraints.HasFlag(KnownTypeConstraint.Struct), ref wroteConstraint);
        AppendConstraintKeywordIf(
            source,
            "notnull",
            knownConstraints.HasFlag(KnownTypeConstraint.NotNull) && !isOverrideOrExplicitImplementation,
            ref wroteConstraint);

        if (!isOverrideOrExplicitImplementation)
        {
            foreach (var constraint in typeParameter.Constraints)
            {
                AppendConstraintKeyword(source, constraint, ref wroteConstraint);
            }
        }

        AppendConstraintKeywordIf(
            source,
            "new()",
            knownConstraints.HasFlag(KnownTypeConstraint.New) && !isOverrideOrExplicitImplementation,
            ref wroteConstraint);
        source.WriteLine();
    }

    /// <summary>Determines whether a type parameter has constraints that should be emitted.</summary>
    /// <param name="typeParameter">The type parameter constraint to inspect.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    /// <returns><see langword="true"/> when at least one constraint should be emitted.</returns>
    private static bool HasConstraintKeywords(
        in TypeConstraint typeParameter,
        bool isOverrideOrExplicitImplementation)
    {
        var knownConstraints = typeParameter.KnownTypeConstraint;
        if (knownConstraints.HasFlag(KnownTypeConstraint.Class))
        {
            return true;
        }

        if (knownConstraints.HasFlag(KnownTypeConstraint.Unmanaged) && !isOverrideOrExplicitImplementation)
        {
            return true;
        }

        if (knownConstraints.HasFlag(KnownTypeConstraint.Struct))
        {
            return true;
        }

        return (knownConstraints.HasFlag(KnownTypeConstraint.NotNull) && !isOverrideOrExplicitImplementation)
               || (!isOverrideOrExplicitImplementation && (typeParameter.Constraints.Count > 0 || knownConstraints.HasFlag(KnownTypeConstraint.New)));
    }

    /// <summary>Appends a constraint keyword when the condition is true.</summary>
    /// <param name="source">The source writer to append to.</param>
    /// <param name="keyword">The constraint keyword.</param>
    /// <param name="condition">Whether the keyword should be emitted.</param>
    /// <param name="wroteConstraint">Tracks whether a previous keyword has been emitted.</param>
    private static void AppendConstraintKeywordIf(
        SourceWriter source,
        string keyword,
        bool condition,
        ref bool wroteConstraint)
    {
        if (!condition)
        {
            return;
        }

        AppendConstraintKeyword(source, keyword, ref wroteConstraint);
    }

    /// <summary>Appends one constraint keyword, including any required separator.</summary>
    /// <param name="source">The source writer to append to.</param>
    /// <param name="keyword">The constraint keyword.</param>
    /// <param name="wroteConstraint">Tracks whether a previous keyword has been emitted.</param>
    private static void AppendConstraintKeyword(
        SourceWriter source,
        string keyword,
        ref bool wroteConstraint)
    {
        if (wroteConstraint)
        {
            source.Append(", ");
        }

        source.Append(keyword);
        wroteConstraint = true;
    }
}
