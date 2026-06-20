// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Text;

namespace Refit.Generator;

/// <summary>Emits the generated source code for Refit interface implementations.</summary>
internal static partial class Emitter
{
    /// <summary>Common scalar type display names used by generated Minimal API binding.</summary>
    private static readonly Dictionary<string, string> KnownMinimalApiScalarTypes =
        new(StringComparer.Ordinal)
        {
            ["string"] = "String",
            ["global::System.String"] = "String",
            ["int"] = "Int32",
            ["global::System.Int32"] = "Int32",
            ["long"] = "Int64",
            ["global::System.Int64"] = "Int64",
            ["bool"] = "Boolean",
            ["global::System.Boolean"] = "Boolean",
            ["global::System.Guid"] = "Guid"
        };

    /// <summary>Gets the number of Refit methods emitted as Minimal API endpoints.</summary>
    /// <param name="model">The interface model.</param>
    /// <returns>The endpoint method count.</returns>
    private static int MinimalApiMethodCount(InterfaceModel model) =>
        model.MinimalApi is null
            ? 0
            : model.RefitMethods.Count + model.DerivedRefitMethods.Count;

    /// <summary>Determines whether the Refit client stub should be generated for an interface.</summary>
    /// <param name="interfaceModel">The interface model.</param>
    /// <returns><see langword="true"/> when client code should be emitted.</returns>
    private static bool ShouldEmitGeneratedClient(InterfaceModel interfaceModel) =>
        interfaceModel.MinimalApi?.GenerateClient != false;

    /// <summary>Writes the generated Minimal API endpoint registrations for non-generic interfaces.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="interfaces">The parsed interface models.</param>
    private static void WriteGeneratedMinimalApiEndpointRegistrations(
        SourceWriter source,
        ImmutableEquatableArray<InterfaceModel> interfaces)
    {
        for (var i = 0; i < interfaces.Count; i++)
        {
            var interfaceModel = interfaces[i];
            if (interfaceModel.MinimalApi is null || interfaceModel.ClassDeclaration.Contains("<"))
            {
                continue;
            }

            source.Append("                        global::Refit.RefitEndpointRouteBuilderExtensions.RegisterGeneratedMinimalApiEndpoints<");
            source.Append(interfaceModel.InterfaceDisplayName);
            source.WriteLine(">(");
            source.Append("                            global::Refit.Implementation.Generated.");
            source.Append(GetMinimalApiEndpointClassName(interfaceModel));
            source.WriteLine(".All);");
        }
    }

    /// <summary>Writes source-generated Minimal API endpoint descriptors for a marked interface.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="interfaceModel">The interface model.</param>
    private static void WriteMinimalApiEndpointDescriptors(SourceWriter source, InterfaceModel interfaceModel)
    {
        if (interfaceModel.MinimalApi is null || interfaceModel.ClassDeclaration.Contains("<"))
        {
            return;
        }

        var endpointClassName = GetMinimalApiEndpointClassName(interfaceModel);

        source.WriteLine();
        source.WriteLine("/// <summary>Source-generated Minimal API endpoint descriptors.</summary>");
        source.WriteLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        source.WriteLine("[global::System.Diagnostics.DebuggerNonUserCode]");
        source.WriteLine($"[{interfaceModel.PreserveAttributeDisplayName}]");
        source.WriteLine("[global::System.Reflection.Obfuscation(Exclude=true)]");
        source.WriteLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        source.WriteLine($"internal static class {endpointClassName}");
        source.WriteLine("{");
        source.Indentation++;
        source.WriteLine("/// <summary>Gets all endpoint descriptors.</summary>");
        source.WriteLine($"internal static global::System.Collections.Generic.IReadOnlyList<global::Refit.RefitMinimalApiEndpoint<{interfaceModel.InterfaceDisplayName}>> All {{ get; }} =");
        source.WriteLine("[");
        source.Indentation++;
        WriteMinimalApiEndpointList(source, interfaceModel.RefitMethods);
        WriteMinimalApiEndpointList(source, interfaceModel.DerivedRefitMethods);
        source.Indentation--;
        source.WriteLine("];");

        WriteMinimalApiEndpointHandlers(source, interfaceModel, interfaceModel.RefitMethods);
        WriteMinimalApiEndpointHandlers(source, interfaceModel, interfaceModel.DerivedRefitMethods);

        source.Indentation--;
        source.WriteLine("}");
    }

    /// <summary>Writes Minimal API endpoint descriptor initializers.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="methods">The Refit methods represented by endpoints.</param>
    private static void WriteMinimalApiEndpointList(
        SourceWriter source,
        ImmutableEquatableArray<MethodModel> methods)
    {
        for (var i = 0; i < methods.Count; i++)
        {
            var method = methods[i];
            if (!CanEmitMinimalApiEndpoint(method))
            {
                continue;
            }

            source.WriteLine(
                $"new({ToCSharpStringLiteral(GetMinimalApiPattern(method.Request.Path))}, {ToCSharpStringLiteral(method.Request.HttpMethod)}, {BuildMinimalApiHandlerName(method)}),");
        }
    }

    /// <summary>Writes Minimal API endpoint handler methods.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="interfaceModel">The interface model.</param>
    /// <param name="methods">The Refit methods represented by endpoints.</param>
    private static void WriteMinimalApiEndpointHandlers(
        SourceWriter source,
        InterfaceModel interfaceModel,
        ImmutableEquatableArray<MethodModel> methods)
    {
        for (var i = 0; i < methods.Count; i++)
        {
            var method = methods[i];
            if (CanEmitMinimalApiEndpoint(method))
            {
                WriteMinimalApiEndpointHandler(source, interfaceModel, method);
            }
        }
    }

    /// <summary>Writes one Minimal API endpoint handler method.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="interfaceModel">The interface model.</param>
    /// <param name="method">The Refit method represented by the endpoint.</param>
    private static void WriteMinimalApiEndpointHandler(
        SourceWriter source,
        InterfaceModel interfaceModel,
        MethodModel method)
    {
        source.WriteLine();
        source.WriteLine("/// <summary>Handles a source-generated Minimal API endpoint.</summary>");
        source.WriteLine($"private static async global::System.Threading.Tasks.ValueTask {BuildMinimalApiHandlerName(method)}(");
        source.WriteLine("    global::Microsoft.AspNetCore.Http.HttpContext context,");
        source.WriteLine($"    {interfaceModel.InterfaceDisplayName} implementation)");
        source.WriteLine("{");
        source.Indentation++;

        WriteMinimalApiArgumentBindings(source, interfaceModel, method);
        WriteMinimalApiInvocation(source, interfaceModel, method);

        source.Indentation--;
        source.WriteLine("}");
    }

    /// <summary>Writes typed argument binding code for a generated Minimal API handler.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="interfaceModel">The interface model.</param>
    /// <param name="method">The Refit method represented by the endpoint.</param>
    private static void WriteMinimalApiArgumentBindings(
        SourceWriter source,
        InterfaceModel interfaceModel,
        MethodModel method)
    {
        var parameters = method.Parameters.AsArray();
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var requestParameter = method.Request.Parameters[i];
            WriteMinimalApiArgumentBinding(source, interfaceModel, method, parameter, requestParameter);
        }
    }

    /// <summary>Writes typed binding code for one generated Minimal API handler argument.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="interfaceModel">The interface model.</param>
    /// <param name="method">The Refit method represented by the endpoint.</param>
    /// <param name="parameter">The method parameter model.</param>
    /// <param name="requestParameter">The request parameter model.</param>
    private static void WriteMinimalApiArgumentBinding(
        SourceWriter source,
        InterfaceModel interfaceModel,
        MethodModel method,
        ParameterModel parameter,
        RequestParameterModel requestParameter)
    {
        switch (requestParameter.Kind)
        {
            case RequestParameterKind.CancellationToken:
            {
                source.WriteLine($"var @{parameter.MetadataName} = context.RequestAborted;");
                return;
            }

            case RequestParameterKind.Header:
            {
                WriteMinimalApiScalarBinding(
                    source,
                    parameter,
                    $"context.Request.Headers[{ToCSharpStringLiteral(requestParameter.HeaderName)}].ToString()");
                return;
            }

            case RequestParameterKind.Body:
            {
                WriteMinimalApiBodyBinding(source, interfaceModel, parameter);
                return;
            }

            default:
            {
                WriteMinimalApiRouteOrQueryBinding(source, method, parameter, requestParameter);
                return;
            }
        }
    }

    /// <summary>Writes body deserialization code for one generated Minimal API handler argument.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="interfaceModel">The interface model.</param>
    /// <param name="parameter">The method parameter model.</param>
    private static void WriteMinimalApiBodyBinding(
        SourceWriter source,
        InterfaceModel interfaceModel,
        ParameterModel parameter)
    {
        source.WriteLine(
            $"var @{parameter.MetadataName} = await global::System.Text.Json.JsonSerializer.DeserializeAsync(");
        source.WriteLine("    context.Request.Body,");
        source.WriteLine($"    {BuildJsonTypeInfoExpression(interfaceModel.MinimalApi!, parameter.Type)},");
        source.WriteLine("    context.RequestAborted).ConfigureAwait(false);");
        source.WriteLine($"if (@{parameter.MetadataName} is null)");
        source.WriteLine("{");
        source.Indentation++;
        source.WriteLine("context.Response.StatusCode = global::Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest;");
        source.WriteLine("return;");
        source.Indentation--;
        source.WriteLine("}");
    }

    /// <summary>Writes route or query binding code for one generated Minimal API handler argument.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="method">The Refit method represented by the endpoint.</param>
    /// <param name="parameter">The method parameter model.</param>
    /// <param name="requestParameter">The request parameter model.</param>
    private static void WriteMinimalApiRouteOrQueryBinding(
        SourceWriter source,
        MethodModel method,
        ParameterModel parameter,
        RequestParameterModel requestParameter)
    {
        var bindingName = requestParameter.Name;
        var routeTemplate = "{" + bindingName + "}";
        var rawExpression = method.Request.Path.IndexOf(routeTemplate, StringComparison.Ordinal) >= 0
            ? $"global::System.Convert.ToString(context.Request.RouteValues[{ToCSharpStringLiteral(bindingName)}], global::System.Globalization.CultureInfo.InvariantCulture)"
            : $"context.Request.Query[{ToCSharpStringLiteral(bindingName)}].ToString()";

        WriteMinimalApiScalarBinding(source, parameter, rawExpression);
    }

    /// <summary>Writes typed scalar conversion code for one generated Minimal API handler argument.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="parameter">The method parameter model.</param>
    /// <param name="rawExpression">The raw string expression.</param>
    private static void WriteMinimalApiScalarBinding(
        SourceWriter source,
        ParameterModel parameter,
        string rawExpression)
    {
        var conversionExpression = BuildMinimalApiScalarConversionExpression(parameter.Type, rawExpression);
        source.WriteLine($"var @{parameter.MetadataName} = {conversionExpression};");
    }

    /// <summary>Writes the implementation invocation and response handling for a generated Minimal API handler.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="interfaceModel">The interface model.</param>
    /// <param name="method">The Refit method represented by the endpoint.</param>
    private static void WriteMinimalApiInvocation(
        SourceWriter source,
        InterfaceModel interfaceModel,
        MethodModel method)
    {
        var invocation = BuildMinimalApiInvocation(method);
        if (method.ReturnTypeMetadata == ReturnTypeInfo.AsyncVoid)
        {
            source.WriteLine($"await {invocation}.ConfigureAwait(false);");
            source.WriteLine("context.Response.StatusCode = global::Microsoft.AspNetCore.Http.StatusCodes.Status204NoContent;");
            return;
        }

        source.WriteLine($"var result = await {invocation}.ConfigureAwait(false);");
        if (method.Request.ResultType == "global::Microsoft.AspNetCore.Http.IResult")
        {
            source.WriteLine("await result.ExecuteAsync(context).ConfigureAwait(false);");
            return;
        }

        source.WriteLine("context.Response.ContentType = \"application/json\";");
        source.WriteLine("await global::System.Text.Json.JsonSerializer.SerializeAsync(");
        source.WriteLine("    context.Response.Body,");
        source.WriteLine("    result,");
        source.WriteLine($"    {BuildJsonTypeInfoExpression(interfaceModel.MinimalApi!, method.Request.ResultType)},");
        source.WriteLine("    context.RequestAborted).ConfigureAwait(false);");
    }

    /// <summary>Determines whether a method can be emitted as a generated Minimal API endpoint.</summary>
    /// <param name="method">The Refit method model.</param>
    /// <returns><see langword="true"/> when a descriptor can be emitted.</returns>
    private static bool CanEmitMinimalApiEndpoint(MethodModel method) =>
        method.Request.HttpMethod.Length > 0
        && method.Request.Path.Length > 0
        && method.ReturnTypeMetadata is ReturnTypeInfo.AsyncVoid or ReturnTypeInfo.AsyncResult
        && method.Parameters.Count == method.Request.Parameters.Count;

    /// <summary>Builds the generated Minimal API endpoint class name.</summary>
    /// <param name="interfaceModel">The interface model.</param>
    /// <returns>The endpoint class name.</returns>
    private static string GetMinimalApiEndpointClassName(InterfaceModel interfaceModel) =>
        interfaceModel.Ns + interfaceModel.ClassSuffix + "MinimalApiEndpoints";

    /// <summary>Builds a generated Minimal API handler method name.</summary>
    /// <param name="method">The Refit method model.</param>
    /// <returns>The handler method name.</returns>
    private static string BuildMinimalApiHandlerName(MethodModel method) =>
        "Handle" + StripExplicitInterfacePrefix(method.Name);

    /// <summary>Builds the route pattern for a generated Minimal API endpoint.</summary>
    /// <param name="path">The Refit method path.</param>
    /// <returns>The Minimal API route pattern.</returns>
    private static string GetMinimalApiPattern(string path)
    {
        var queryIndex = path.IndexOf('?');
        return queryIndex < 0 ? path : path[..queryIndex];
    }

    /// <summary>Builds the implementation invocation expression for a generated Minimal API handler.</summary>
    /// <param name="method">The Refit method model.</param>
    /// <returns>The invocation expression.</returns>
    private static string BuildMinimalApiInvocation(MethodModel method)
    {
        var builder = new StringBuilder("implementation.")
            .Append(StripExplicitInterfacePrefix(method.Name))
            .Append('(');
        var parameters = method.Parameters.AsArray();
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append('@').Append(parameters[i].MetadataName);
        }

        builder.Append(')');
        return builder.ToString();
    }

    /// <summary>Builds a source-generated JSON metadata expression.</summary>
    /// <param name="minimalApi">The Minimal API generation model.</param>
    /// <param name="type">The serialized type.</param>
    /// <returns>The JSON type info expression.</returns>
    private static string BuildJsonTypeInfoExpression(MinimalApiModel minimalApi, string type) =>
        minimalApi.JsonSerializerContextType + ".Default." + BuildJsonTypeInfoPropertyName(type);

    /// <summary>Builds the default JSON source-generation property name for a type display string.</summary>
    /// <param name="type">The serialized type.</param>
    /// <returns>The JSON type info property name.</returns>
    private static string BuildJsonTypeInfoPropertyName(string type)
    {
        var normalized = type.StartsWith(GlobalPrefix, StringComparison.Ordinal)
            ? type[GlobalPrefix.Length..]
            : type;
        var nullableIndex = normalized.IndexOf('?');
        if (nullableIndex >= 0)
        {
            normalized = normalized[..nullableIndex];
        }

        var lastDot = normalized.LastIndexOf('.');
        return lastDot >= 0 ? normalized[(lastDot + 1)..] : normalized;
    }

    /// <summary>Builds the typed conversion expression for a scalar Minimal API binding.</summary>
    /// <param name="type">The target type display string.</param>
    /// <param name="rawExpression">The raw string expression.</param>
    /// <returns>The scalar conversion expression.</returns>
    private static string BuildMinimalApiScalarConversionExpression(string type, string rawExpression)
    {
        var normalizedType = NormalizeKnownMinimalApiScalarType(type);
        return normalizedType switch
        {
            "String" => rawExpression,
            "Int32" => $"global::System.Convert.ToInt32({rawExpression}, global::System.Globalization.CultureInfo.InvariantCulture)",
            "Int64" => $"global::System.Convert.ToInt64({rawExpression}, global::System.Globalization.CultureInfo.InvariantCulture)",
            "Boolean" => $"global::System.Convert.ToBoolean({rawExpression}, global::System.Globalization.CultureInfo.InvariantCulture)",
            "Guid" => $"global::System.Guid.Parse({rawExpression})",
            _ => $"({type})global::System.Convert.ChangeType({rawExpression}, typeof({type}), global::System.Globalization.CultureInfo.InvariantCulture)"
        };
    }

    /// <summary>Normalizes common scalar type display names for generated Minimal API binding.</summary>
    /// <param name="type">The target type display string.</param>
    /// <returns>The normalized type name.</returns>
    private static string NormalizeKnownMinimalApiScalarType(string type) =>
        KnownMinimalApiScalarTypes.TryGetValue(type, out var normalizedType)
            ? normalizedType
            : type;
}
