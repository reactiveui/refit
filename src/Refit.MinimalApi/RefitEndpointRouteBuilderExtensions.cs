// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Refit;

/// <summary>Extension methods for mapping Refit interfaces to ASP.NET Core Minimal API endpoints.</summary>
public static class RefitEndpointRouteBuilderExtensions
{
    /// <summary>Maps Refit Minimal API endpoints on an <see cref="IEndpointRouteBuilder"/>.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Maps generated Refit Minimal API endpoint descriptors using a fixed implementation.</summary>
        /// <typeparam name="TApi">The Refit interface to map.</typeparam>
        /// <param name="implementation">The implementation invoked by each mapped endpoint.</param>
        /// <param name="apiEndpoints">The generated endpoint descriptors.</param>
        /// <returns>The endpoint route builder.</returns>
        public IEndpointRouteBuilder MapRefitApi<TApi>(
            TApi implementation,
            IEnumerable<RefitMinimalApiEndpoint<TApi>> apiEndpoints)
            where TApi : class
        {
            ArgumentExceptionHelper.ThrowIfNull(implementation);

            return MapRefitApiCore(endpoints, _ => implementation, apiEndpoints);
        }

        /// <summary>Maps generated Refit Minimal API endpoint descriptors using an implementation factory.</summary>
        /// <typeparam name="TApi">The Refit interface to map.</typeparam>
        /// <param name="implementationFactory">Creates the implementation invoked by each mapped endpoint.</param>
        /// <param name="apiEndpoints">The generated endpoint descriptors.</param>
        /// <returns>The endpoint route builder.</returns>
        public IEndpointRouteBuilder MapRefitApi<TApi>(
            Func<HttpContext, TApi> implementationFactory,
            IEnumerable<RefitMinimalApiEndpoint<TApi>> apiEndpoints)
            where TApi : class =>
            MapRefitApiCore(endpoints, implementationFactory, apiEndpoints);

        /// <summary>Maps a Refit interface to Minimal API endpoints by reflecting over the interface.</summary>
        /// <typeparam name="TApi">The Refit interface to map.</typeparam>
        /// <param name="implementation">The implementation invoked by each mapped endpoint.</param>
        /// <returns>The endpoint route builder.</returns>
        [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(ReflectionMappingRequiresDynamicCodeMessage)]
        public IEndpointRouteBuilder MapRefitApi<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces
                    | DynamicallyAccessedMemberTypes.PublicMethods)]
            TApi>(
            TApi implementation)
            where TApi : class
        {
            ArgumentExceptionHelper.ThrowIfNull(implementation);

            return MapRefitApiCore(endpoints, _ => implementation, CreateReflectiveEndpoints<TApi>());
        }

        /// <summary>Maps a Refit interface to Minimal API endpoints by reflecting over the interface and resolving implementations per request.</summary>
        /// <typeparam name="TApi">The Refit interface to map.</typeparam>
        /// <param name="implementationFactory">Creates the implementation invoked by each mapped endpoint.</param>
        /// <returns>The endpoint route builder.</returns>
        [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(ReflectionMappingRequiresDynamicCodeMessage)]
        public IEndpointRouteBuilder MapRefitApi<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces
                    | DynamicallyAccessedMemberTypes.PublicMethods)]
            TApi>(
            Func<HttpContext, TApi> implementationFactory)
            where TApi : class =>
            MapRefitApiCore(endpoints, implementationFactory, CreateReflectiveEndpoints<TApi>());
    }

    /// <summary>Message for reflection mapping that depends on runtime metadata.</summary>
    private const string ReflectionMappingRequiresUnreferencedCodeMessage =
        "Reflection-based Refit Minimal API mapping requires interface, method, parameter, and property metadata at runtime.";

    /// <summary>Message for reflection mapping that depends on runtime serialization code.</summary>
    private const string ReflectionMappingRequiresDynamicCodeMessage =
        "Reflection-based Refit Minimal API mapping uses runtime JSON serialization and reflected result handling.";

    /// <summary>Default boxed values for scalar value types used by reflected binding.</summary>
    private static readonly FrozenDictionary<Type, object> ScalarDefaultValues =
        new Dictionary<Type, object>
        {
            [typeof(bool)] = default(bool),
            [typeof(char)] = default(char),
            [typeof(sbyte)] = default(sbyte),
            [typeof(byte)] = default(byte),
            [typeof(short)] = default(short),
            [typeof(ushort)] = default(ushort),
            [typeof(int)] = default(int),
            [typeof(uint)] = default(uint),
            [typeof(long)] = default(long),
            [typeof(ulong)] = default(ulong),
            [typeof(float)] = default(float),
            [typeof(double)] = default(double),
            [typeof(decimal)] = default(decimal),
            [typeof(DateTime)] = default(DateTime),
            [typeof(Guid)] = default(Guid),
            [typeof(DateTimeOffset)] = default(DateTimeOffset),
            [typeof(TimeSpan)] = default(TimeSpan),
        }.ToFrozenDictionary();

    /// <summary>Maps already-built Refit Minimal API descriptors to ASP.NET Core endpoints.</summary>
    /// <typeparam name="TApi">The Refit interface to map.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="implementationFactory">Creates the implementation invoked by each mapped endpoint.</param>
    /// <param name="apiEndpoints">The generated endpoint descriptors.</param>
    /// <returns>The same endpoint route builder.</returns>
    private static IEndpointRouteBuilder MapRefitApiCore<TApi>(
        IEndpointRouteBuilder endpoints,
        Func<HttpContext, TApi> implementationFactory,
        IEnumerable<RefitMinimalApiEndpoint<TApi>> apiEndpoints)
        where TApi : class
    {
        ArgumentExceptionHelper.ThrowIfNull(endpoints);
        ArgumentExceptionHelper.ThrowIfNull(implementationFactory);
        ArgumentExceptionHelper.ThrowIfNull(apiEndpoints);

        foreach (var apiEndpoint in apiEndpoints)
        {
            endpoints.MapMethods(
                apiEndpoint.Pattern,
                apiEndpoint.HttpMethods,
                async context =>
                {
                    var implementation = implementationFactory(context);
                    await apiEndpoint.Handler(context, implementation).ConfigureAwait(false);
                });
        }

        return endpoints;
    }

    /// <summary>Creates endpoint descriptors by reflecting over the Refit interface.</summary>
    /// <typeparam name="TApi">The Refit interface to inspect.</typeparam>
    /// <returns>The reflected endpoint descriptors.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified by reflection-based extension methods.")]
    [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(ReflectionMappingRequiresDynamicCodeMessage)]
    private static IEnumerable<RefitMinimalApiEndpoint<TApi>> CreateReflectiveEndpoints<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
                | DynamicallyAccessedMemberTypes.PublicMethods)]
        TApi>()
        where TApi : class
    {
        var apiType = typeof(TApi);
        if (!apiType.IsInterface)
        {
            throw new ArgumentException("Refit Minimal API mapping requires an interface type.", nameof(TApi));
        }

        return CreateReflectiveEndpointsCore<TApi>(apiType);
    }

    /// <summary>Creates endpoint descriptors from a validated Refit interface type.</summary>
    /// <typeparam name="TApi">The Refit interface to inspect.</typeparam>
    /// <param name="apiType">The Refit interface type.</param>
    /// <returns>The reflected endpoint descriptors.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally flows through the reflected endpoint descriptor result.")]
    [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(ReflectionMappingRequiresDynamicCodeMessage)]
    private static IEnumerable<RefitMinimalApiEndpoint<TApi>> CreateReflectiveEndpointsCore<TApi>(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
                | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type apiType)
        where TApi : class
    {
        foreach (var method in GetRefitMethods(apiType))
        {
            var httpMethod = method.GetCustomAttribute<HttpMethodAttribute>(true)!;
            var pattern = SplitRoute(httpMethod.Path).Path;

            yield return new(
                pattern,
                httpMethod.Method.Method,
                async (context, implementation) =>
                {
                    var arguments = await BindArgumentsAsync(context, method).ConfigureAwait(false);
                    var result = method.Invoke(implementation, arguments);
                    var value = await AwaitResultAsync(result).ConfigureAwait(false);
                    await WriteReflectionResultAsync(context, value).ConfigureAwait(false);
                });
        }
    }

    /// <summary>Gets Refit methods declared directly or on inherited interfaces.</summary>
    /// <param name="apiType">The Refit interface type.</param>
    /// <returns>The reflected Refit methods.</returns>
    [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
    private static IEnumerable<MethodInfo> GetRefitMethods(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
                | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type apiType)
    {
        foreach (var method in apiType.GetMethods())
        {
            if (method.GetCustomAttribute<HttpMethodAttribute>(true) is not null)
            {
                yield return method;
            }
        }

        foreach (var inheritedInterface in apiType.GetInterfaces())
        {
            foreach (var method in inheritedInterface.GetMethods())
            {
                if (method.GetCustomAttribute<HttpMethodAttribute>(true) is not null)
                {
                    yield return method;
                }
            }
        }
    }

    /// <summary>Binds endpoint request data to reflected method arguments.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="method">The reflected Refit method.</param>
    /// <returns>The bound argument values.</returns>
    [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(ReflectionMappingRequiresDynamicCodeMessage)]
    private static async ValueTask<object?[]> BindArgumentsAsync(HttpContext context, MethodInfo method)
    {
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = await BindParameterAsync(context, parameters[i]).ConfigureAwait(false);
        }

        return arguments;
    }

    /// <summary>Binds one endpoint request value to a reflected method parameter.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="parameter">The reflected method parameter.</param>
    /// <returns>The bound parameter value.</returns>
    [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(ReflectionMappingRequiresDynamicCodeMessage)]
    private static async ValueTask<object?> BindParameterAsync(
        HttpContext context,
        ParameterInfo parameter)
    {
        if (parameter.ParameterType == typeof(CancellationToken))
        {
            return context.RequestAborted;
        }

        if (parameter.ParameterType == typeof(HttpContext))
        {
            return context;
        }

        if (parameter.GetCustomAttribute<HeaderAttribute>(true) is { } header)
        {
            return ConvertValue(GetHeaderValue(context, header.Header), parameter.ParameterType);
        }

        if (parameter.GetCustomAttribute<BodyAttribute>(true) is not null)
        {
            return await context.Request
                .ReadFromJsonAsync(parameter.ParameterType, cancellationToken: context.RequestAborted)
                .ConfigureAwait(false);
        }

        var name = GetBoundName(parameter);
        if (TryGetScalarValue(context, name, out var rawValue))
        {
            return ConvertValue(rawValue, parameter.ParameterType);
        }

        if (!IsSimpleType(parameter.ParameterType))
        {
            return BindComplexParameter(context, parameter);
        }

        return parameter.HasDefaultValue
            ? parameter.DefaultValue
            : GetDefaultValue(parameter.ParameterType);
    }

    /// <summary>Binds a reflected complex parameter from route and query values.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="parameter">The complex reflected method parameter.</param>
    /// <returns>The bound complex value.</returns>
    [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
    private static object? BindComplexParameter(HttpContext context, ParameterInfo parameter)
    {
        var target = Activator.CreateInstance(parameter.ParameterType);
        if (target is null)
        {
            return null;
        }

        var parameterName = parameter.Name ?? string.Empty;
        foreach (var property in parameter.ParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanWrite)
            {
                continue;
            }

            var propertyName = GetBoundName(property);
            var prefixedName = parameterName.Length == 0 ? propertyName : parameterName + "." + propertyName;

            if (TryGetScalarValue(context, prefixedName, out var rawValue)
                || TryGetScalarValue(context, propertyName, out rawValue))
            {
                property.SetValue(target, ConvertValue(rawValue, property.PropertyType));
            }
        }

        return target;
    }

    /// <summary>Gets a scalar route or query value.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="name">The route or query key.</param>
    /// <param name="value">Receives the scalar value.</param>
    /// <returns><see langword="true"/> when a value was found.</returns>
    private static bool TryGetScalarValue(HttpContext context, string name, out string? value)
    {
        if (context.Request.RouteValues.TryGetValue(name, out var routeValue) && routeValue is not null)
        {
            value = string.Create(CultureInfo.InvariantCulture, $"{routeValue}");
            return true;
        }

        if (context.Request.Query.TryGetValue(name, out var queryValues))
        {
            value = queryValues.Count > 0 ? queryValues[0] : null;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Gets one header value from the request.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="headerName">The header name.</param>
    /// <returns>The header value, or null.</returns>
    private static string? GetHeaderValue(HttpContext context, string headerName) =>
        context.Request.Headers.TryGetValue(headerName, out var values)
            ? values.ToString()
            : null;

    /// <summary>Converts a scalar string value to a reflected target type.</summary>
    /// <param name="value">The scalar value.</param>
    /// <param name="targetType">The reflected target type.</param>
    /// <returns>The converted value.</returns>
    private static object? ConvertValue(string? value, Type targetType)
    {
        var nullableTarget = Nullable.GetUnderlyingType(targetType);
        var conversionType = nullableTarget ?? targetType;

        if (string.IsNullOrEmpty(value))
        {
            return nullableTarget is not null || !targetType.IsValueType
                ? null
                : GetDefaultValue(targetType);
        }

        return ConvertNonEmptyValue(value, conversionType);
    }

    /// <summary>Converts a non-empty scalar string value to a reflected target type.</summary>
    /// <param name="value">The non-empty scalar value.</param>
    /// <param name="conversionType">The non-nullable reflected target type.</param>
    /// <returns>The converted value.</returns>
    private static object? ConvertNonEmptyValue(string value, Type conversionType)
    {
        if (conversionType == typeof(string))
        {
            return value;
        }

        if (conversionType == typeof(Guid))
        {
            return Guid.Parse(value);
        }

        if (conversionType == typeof(DateTimeOffset))
        {
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
        }

        if (conversionType == typeof(DateTime))
        {
            return DateTime.Parse(value, CultureInfo.InvariantCulture);
        }

        if (conversionType.IsEnum)
        {
            return Enum.Parse(conversionType, value, ignoreCase: true);
        }

        if (conversionType == typeof(TimeSpan))
        {
            return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        }

        if (conversionType == typeof(Uri))
        {
            return new Uri(value, UriKind.RelativeOrAbsolute);
        }

        return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
    }

    /// <summary>Determines whether a type binds as a scalar value.</summary>
    /// <param name="type">The reflected type.</param>
    /// <returns><see langword="true"/> for scalar value types.</returns>
    private static bool IsSimpleType(Type type)
    {
        var targetType = Nullable.GetUnderlyingType(type) ?? type;
        return targetType.IsPrimitive
            || targetType.IsEnum
            || targetType == typeof(string)
            || targetType == typeof(decimal)
            || targetType == typeof(Guid)
            || targetType == typeof(DateTime)
            || targetType == typeof(DateTimeOffset)
            || targetType == typeof(TimeSpan)
            || targetType == typeof(Uri);
    }

    /// <summary>Creates the default value for a reflected type.</summary>
    /// <param name="type">The reflected type.</param>
    /// <returns>The default value.</returns>
    private static object? GetDefaultValue(Type type)
    {
        if (!type.IsValueType)
        {
            return null;
        }

        if (type.IsEnum)
        {
            return Enum.ToObject(type, 0);
        }

        return ScalarDefaultValues.TryGetValue(type, out var value) ? value : null;
    }

    /// <summary>Gets the Refit-bound name for a reflected parameter.</summary>
    /// <param name="parameter">The reflected parameter.</param>
    /// <returns>The bound name.</returns>
    private static string GetBoundName(ParameterInfo parameter) =>
        parameter.GetCustomAttribute<AliasAsAttribute>(true)?.Name
        ?? parameter.Name
        ?? string.Empty;

    /// <summary>Gets the Refit-bound name for a reflected property.</summary>
    /// <param name="property">The reflected property.</param>
    /// <returns>The bound name.</returns>
    private static string GetBoundName(PropertyInfo property) =>
        property.GetCustomAttribute<AliasAsAttribute>(true)?.Name
        ?? property.Name;

    /// <summary>Splits a Refit route into path and query sections.</summary>
    /// <param name="route">The Refit route.</param>
    /// <returns>The path and query sections.</returns>
    private static (string Path, string? Query) SplitRoute(string route)
    {
        var queryIndex = route.IndexOf('?');
        return queryIndex < 0 ? (route, null) : (route[..queryIndex], route[(queryIndex + 1)..]);
    }

    /// <summary>Awaits a reflected handler result.</summary>
    /// <param name="result">The reflected result.</param>
    /// <returns>The unwrapped result value.</returns>
    [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
    private static async ValueTask<object?> AwaitResultAsync(object? result)
    {
        if (result is null)
        {
            return null;
        }

        if (result is Task resultTask)
        {
            await resultTask.ConfigureAwait(false);
            return GetTaskResult(resultTask);
        }

        if (result is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            return null;
        }

        var resultType = result.GetType();
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var valueTaskAsTask = (Task)resultType.GetMethod(nameof(ValueTask<object>.AsTask))!.Invoke(result, [])!;
            await valueTaskAsTask.ConfigureAwait(false);
            return GetTaskResult(valueTaskAsTask);
        }

        return result;
    }

    /// <summary>Gets the result value from a completed task.</summary>
    /// <param name="task">The completed task.</param>
    /// <returns>The task result, or null for non-generic tasks.</returns>
    [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
    private static object? GetTaskResult(Task task)
    {
        var taskType = task.GetType();
        return taskType.IsGenericType
            ? taskType.GetProperty(nameof(Task<object>.Result))!.GetValue(task)
            : null;
    }

    /// <summary>Writes the result produced by reflection-based endpoint mapping.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="value">The endpoint value.</param>
    /// <returns>A task that completes when the response is written.</returns>
    [RequiresUnreferencedCode(ReflectionMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(ReflectionMappingRequiresDynamicCodeMessage)]
    private static async ValueTask WriteReflectionResultAsync(HttpContext context, object? value)
    {
        if (value is null)
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        if (value is IResult result)
        {
            await result.ExecuteAsync(context).ConfigureAwait(false);
            return;
        }

        await context.Response.WriteAsJsonAsync(value, context.RequestAborted).ConfigureAwait(false);
    }
}
