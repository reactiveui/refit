// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using ReactiveUI.Primitives.Advanced;

namespace Refit;

/// <summary>Reflection-based request builder that turns Refit interface calls into HTTP requests.</summary>
internal partial class RequestBuilderImplementation : IRequestBuilder
{
    /// <summary>Maximum stack-allocated buffer size, in characters, used when building paths and query strings.</summary>
    private const int StackallocThreshold = 512;

    /// <summary>The name of the <see cref="IReturnTypeAdapter{TReturn, TResult}.Adapt"/> method, resolved reflectively.</summary>
    private const string AdaptMethodName = "Adapt";

    /// <summary>The default query attribute applied when a parameter has none.</summary>
    private static readonly QueryAttribute DefaultQueryAttribute = new();

    /// <summary>A placeholder base URI used while building relative request URIs. Its scheme and host are
    /// discarded — only the combined path and query are kept (see <c>AssignRequestUri</c>), so it never
    /// reaches the network.</summary>
    private static readonly Uri BaseUri = new("https://api");

    /// <summary>Lookup of HTTP methods keyed by method name.</summary>
    private readonly Dictionary<string, List<RestMethodInfoInternal>> _interfaceHttpMethods;

    /// <summary>Cache of closed generic method infos keyed by method and type arguments.</summary>
    private readonly ConcurrentDictionary<CloseGenericMethodKey, RestMethodInfoInternal> _interfaceGenericHttpMethods;

    /// <summary>The content serializer from the active settings.</summary>
    private readonly IHttpContentSerializer _serializer;

    /// <summary>The settings controlling request building and serialization.</summary>
    private readonly RefitSettings _settings;

    /// <summary>Initializes a new instance of the <see cref="RequestBuilderImplementation"/> class for the given interface type.</summary>
    /// <param name="refitInterfaceType">The Refit interface type to build requests for.</param>
    /// <param name="refitSettings">The settings to use, or null for defaults.</param>
    [RequiresUnreferencedCode("Building requests from reflected interface methods requires interface and request object metadata to be available at runtime.")]
    public RequestBuilderImplementation(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        RefitSettings? refitSettings = null)
    {
        if (refitInterfaceType?.GetTypeInfo().IsInterface != true)
        {
            throw new ArgumentException("targetInterface must be an Interface");
        }

        var targetInterfaceInheritedInterfaces = refitInterfaceType.GetInterfaces();

        _settings = refitSettings ?? new RefitSettings();
        _serializer = _settings.ContentSerializer;
        _interfaceGenericHttpMethods =
            new();

        TargetType = refitInterfaceType;

        var dict = new Dictionary<string, List<RestMethodInfoInternal>>(StringComparer.Ordinal);

        AddInterfaceHttpMethods(refitInterfaceType, dict);
        foreach (var inheritedInterface in targetInterfaceInheritedInterfaces)
        {
            AddInterfaceHttpMethods(inheritedInterface, dict);
        }

        _interfaceHttpMethods = dict;
    }

    /// <summary>Gets the Refit interface type this builder targets.</summary>
    public Type TargetType { get; }

    /// <inheritdoc/>
    public RefitSettings Settings => _settings;

    /// <inheritdoc/>
    [RequiresUnreferencedCode("Building request delegates from reflected method metadata requires generic method metadata to be available at runtime.")]
    [RequiresDynamicCode("Building request delegates from reflected method metadata requires runtime generic method instantiation.")]
    public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
        string methodName,
        Type[]? parameterTypes = null,
        Type[]? genericArgumentTypes = null)
    {
        var restMethod = FindMatchingRestMethodInfo(
            methodName,
            parameterTypes,
            genericArgumentTypes);

        // Task (void)
        if (restMethod.ReturnType == typeof(Task))
        {
            return BuildVoidTaskFuncForMethod(restMethod);
        }

        // Task<HttpRequestMessage>: build the request and hand it back to the caller without sending it. Runs before the
        // Task<T> shape so the request is not dispatched and its response deserialized.
        if (IsRequestMessageReturnType(restMethod))
        {
            return BuildRequestMessageFuncForMethod(restMethod);
        }

        // Task<T>
        if (IsGenericReturnType(restMethod, typeof(Task<>)))
        {
            return BuildResultFuncForMethod(restMethod, nameof(BuildTaskFuncForMethod));
        }

        // ValueTask<T>
        if (IsGenericReturnType(restMethod, typeof(ValueTask<>)))
        {
            return BuildResultFuncForMethod(restMethod, nameof(BuildValueTaskFuncForMethod));
        }

        // IAsyncEnumerable<T>
        if (IsGenericReturnType(restMethod, typeof(IAsyncEnumerable<>)))
        {
            return BuildResultFuncForMethod(restMethod, nameof(BuildAsyncEnumerableFuncForMethod));
        }

        // IObservable<T>
        if (IsGenericReturnType(restMethod, typeof(IObservable<>)))
        {
            return BuildResultFuncForMethod(restMethod, nameof(BuildRxFuncForMethod));
        }

        // A registered IReturnTypeAdapter surfaces this return type; this check runs after the built-in shapes so
        // registering an adapter never overrides them.
        return restMethod.HasReturnTypeAdapter
            ? BuildAdapterFuncForMethod(restMethod)
            : BuildGeneratedSyncFuncForMethod(restMethod);
    }

    /// <summary>Finds a method declared on this implementation type by name.</summary>
    /// <param name="name">The method name.</param>
    /// <returns>The declared method.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' may break when trimming",
        Justification = "This resolves Refit's own private generic delegate factory methods by known method name.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2111:Reflection access to methods with DynamicallyAccessedMembersAttribute",
        Justification = "This helper filters by known method names and does not invoke methods with dynamic-access requirements.")]
    internal static MethodInfo FindDeclaredMethod(string name)
    {
        foreach (var method in typeof(RequestBuilderImplementation).GetTypeInfo().DeclaredMethods)
        {
            if (method.Name == name)
            {
                return method;
            }
        }

        throw new MissingMethodException(typeof(RequestBuilderImplementation).FullName, name);
    }

    /// <summary>Gets the lookup key for a method, stripping any explicit-interface prefix from the name.</summary>
    /// <param name="methodInfo">The method to derive a key for.</param>
    /// <returns>The simple method name used as a lookup key.</returns>
    private static string GetLookupKeyForMethod(MethodInfo methodInfo)
    {
        var name = methodInfo.Name;
        var lastDot = name.LastIndexOf('.');
        return lastDot >= 0 ? name[(lastDot + 1)..] : name;
    }

    /// <summary>Determines whether the method's return type is a closed generic of the supplied open generic type.</summary>
    /// <param name="restMethod">The rest method to inspect.</param>
    /// <param name="openGenericType">The open generic type definition to match.</param>
    /// <returns><see langword="true"/> if the return type closes <paramref name="openGenericType"/>; otherwise <see langword="false"/>.</returns>
    private static bool IsGenericReturnType(RestMethodInfoInternal restMethod, Type openGenericType) =>
        restMethod.ReturnType.GetTypeInfo().IsGenericType
        && restMethod.ReturnType.GetGenericTypeDefinition() == openGenericType;

    /// <summary>Determines whether the method returns <see cref="Task{TResult}"/> of <see cref="HttpRequestMessage"/>.</summary>
    /// <param name="restMethod">The rest method to inspect.</param>
    /// <returns><see langword="true"/> when the method builds and returns its request without sending it.</returns>
    private static bool IsRequestMessageReturnType(RestMethodInfoInternal restMethod) =>
        IsGenericReturnType(restMethod, typeof(Task<>))
        && restMethod.ReturnResultType == typeof(HttpRequestMessage);

    /// <summary>Filters the candidate methods by parameter count and generic arity.</summary>
    /// <param name="httpMethods">The candidate methods.</param>
    /// <param name="parameterTypes">The parameter types to match.</param>
    /// <param name="genericArgumentTypes">The generic argument types, or null.</param>
    /// <returns>The matching candidate methods.</returns>
    private static RestMethodInfoInternal[] FilterPossibleMethods(
        List<RestMethodInfoInternal> httpMethods,
        Type[] parameterTypes,
        Type[]? genericArgumentTypes)
    {
        var isGeneric = genericArgumentTypes?.Length > 0;
        List<RestMethodInfoInternal>? possibleMethods = null;

        for (var i = 0; i < httpMethods.Count; i++)
        {
            var method = httpMethods[i];
            if (method.MethodInfo.GetParameters().Length != parameterTypes.Length)
            {
                continue;
            }

            if (isGeneric)
            {
                if (!method.MethodInfo.IsGenericMethod
                    || method.MethodInfo.GetGenericArguments().Length != genericArgumentTypes!.Length)
                {
                    continue;
                }
            }
            else if (method.MethodInfo.IsGenericMethod)
            {
                continue;
            }

            possibleMethods ??= [];
            possibleMethods.Add(method);
        }

        return possibleMethods is null ? [] : [.. possibleMethods];
    }

    /// <summary>Runs an asynchronous task factory synchronously and waits for completion.</summary>
    /// <param name="taskFactory">The task factory to run.</param>
    [SuppressMessage(
        "Usage",
        "VSTHRD002:Avoid problematic synchronous waits",
        Justification = "Deliberate sync-over-async bridge for synchronous (void/non-Task) interface methods that have no async caller; the work is offloaded via Task.Run to avoid deadlocks.")]
    private static void RunSynchronous(Func<Task> taskFactory) =>
        Task.Run(taskFactory).GetAwaiter().GetResult();

    /// <summary>Runs an asynchronous task factory synchronously and returns its result.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="taskFactory">The task factory to run.</param>
    /// <returns>The result produced by the task.</returns>
    [SuppressMessage(
        "Usage",
        "VSTHRD002:Avoid problematic synchronous waits",
        Justification = "Deliberate sync-over-async bridge for synchronous (non-Task) interface methods that have no async caller; the work is offloaded via Task.Run to avoid deadlocks.")]
    private static T? RunSynchronous<T>(Func<Task<T?>> taskFactory) =>
        Task.Run(taskFactory).GetAwaiter().GetResult();

    /// <summary>Awaits the request task and disposes the linked cancellation source once it completes.</summary>
    /// <typeparam name="T">The result type produced by the request.</typeparam>
    /// <param name="task">The in-flight request task.</param>
    /// <param name="cts">The linked cancellation source to dispose when the task finishes.</param>
    /// <returns>The result produced by <paramref name="task"/>.</returns>
    [SuppressMessage(
        "Usage",
        "VSTHRD003:Avoid awaiting foreign Tasks",
        Justification = "The task is the request just launched by the caller; awaiting it here only scopes disposal of the linked cancellation source.")]
    private static async Task<T?> DisposeWhenDoneAsync<T>(Task<T?> task, CancellationTokenSource cts)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        finally
        {
            cts.Dispose();
        }
    }

    /// <summary>Determines whether reflected parameters exactly match the requested parameter types.</summary>
    /// <param name="parameters">The reflected method parameters.</param>
    /// <param name="parameterTypes">The requested parameter types.</param>
    /// <returns><see langword="true"/> when the parameter types match.</returns>
    private static bool ParametersMatch(ParameterInfo[] parameters, Type[] parameterTypes)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType != parameterTypes[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Finds the first cancellation token in an argument array.</summary>
    /// <param name="paramList">The argument values.</param>
    /// <returns>The first cancellation token, or <see cref="CancellationToken.None"/>.</returns>
    private static CancellationToken GetCancellationToken(object[] paramList)
    {
        for (var i = 0; i < paramList.Length; i++)
        {
            if (paramList[i] is CancellationToken cancellationToken)
            {
                return cancellationToken;
            }
        }

        return CancellationToken.None;
    }

    /// <summary>Discovers the Refit HTTP methods on an interface and adds them to the lookup dictionary.</summary>
    /// <param name="interfaceType">The interface to scan for HTTP methods.</param>
    /// <param name="methods">The dictionary to populate with discovered methods.</param>
    [RequiresUnreferencedCode("Reading reflected interface methods requires interface and request object metadata to be available at runtime.")]
    private void AddInterfaceHttpMethods(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type interfaceType,
        Dictionary<string, List<RestMethodInfoInternal>> methods)
    {
        foreach (var methodInfo in interfaceType.GetTypeInfo().DeclaredMethods)
        {
            if (!methodInfo.IsAbstract
                || methodInfo.GetCustomAttribute<HttpMethodAttribute>(true) is null)
            {
                continue;
            }

            var key = GetLookupKeyForMethod(methodInfo);
#if NET8_0_OR_GREATER
            // Hashes the key once: the lookup and the insert share a single bucket probe.
            ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(methods, key, out _);
            var value = slot ??= [];
#else
            if (!methods.TryGetValue(key, out var value))
            {
                value = [];
                methods.Add(key, value);
            }
#endif

            var restinfo = new RestMethodInfoInternal(interfaceType, methodInfo, _settings);
            value.Add(restinfo);
        }
    }

    /// <summary>Finds the rest method matching the given name, parameter types and generic arguments.</summary>
    /// <param name="key">The method lookup key.</param>
    /// <param name="parameterTypes">The parameter types to match, or null to match a single overload.</param>
    /// <param name="genericArgumentTypes">The generic argument types to close over, or null.</param>
    /// <returns>The matching rest method info.</returns>
    [RequiresUnreferencedCode("Resolving generic Refit methods from reflected metadata requires generic method metadata to be available at runtime.")]
    [RequiresDynamicCode("Resolving generic Refit methods from reflected metadata requires runtime generic method instantiation.")]
    private RestMethodInfoInternal FindMatchingRestMethodInfo(
        string key,
        Type[]? parameterTypes,
        Type[]? genericArgumentTypes)
    {
        if (!_interfaceHttpMethods.TryGetValue(key, out var httpMethods))
        {
            throw new ArgumentException(
                "Method must be defined and have an HTTP Method attribute");
        }

        if (parameterTypes is null)
        {
            if (httpMethods.Count > 1)
            {
                throw new ArgumentException(
                    $"MethodName exists more than once, '{nameof(parameterTypes)}' mut be defined");
            }

            return CloseGenericMethodIfNeeded(httpMethods[0], genericArgumentTypes);
        }

        var possibleMethods = FilterPossibleMethods(httpMethods, parameterTypes, genericArgumentTypes);

        if (possibleMethods.Length == 1)
        {
            return CloseGenericMethodIfNeeded(possibleMethods[0], genericArgumentTypes);
        }

        foreach (var method in possibleMethods)
        {
            try
            {
                var closedMethod = CloseGenericMethodIfNeeded(method, genericArgumentTypes);
                if (ParametersMatch(closedMethod.MethodInfo.GetParameters(), parameterTypes))
                {
                    return closedMethod;
                }
            }
            catch (Exception exception) when (exception.Message.Contains("violates the constraint", StringComparison.CurrentCultureIgnoreCase))
            {
            }
        }

        throw new InvalidOperationException("No suitable Method found...");
    }

    /// <summary>Closes an open generic rest method over the supplied type arguments, caching the result.</summary>
    /// <param name="restMethodInfo">The (possibly generic) rest method.</param>
    /// <param name="genericArgumentTypes">The generic argument types, or null if not generic.</param>
    /// <returns>The closed rest method info, or the original when no generic arguments are supplied.</returns>
    [RequiresUnreferencedCode("Closing generic Refit methods requires generic method metadata to be available at runtime.")]
    [RequiresDynamicCode("Closing generic Refit methods requires runtime generic method instantiation.")]
    private RestMethodInfoInternal CloseGenericMethodIfNeeded(
        RestMethodInfoInternal restMethodInfo,
        Type[]? genericArgumentTypes) =>
        genericArgumentTypes is { } genericArguments
            ? _interfaceGenericHttpMethods.GetOrAdd(
                new(restMethodInfo.MethodInfo, genericArguments),
                static (_, state) =>
                    new RestMethodInfoInternal(
                        state.RestMethod.Type,
                        state.RestMethod.MethodInfo.MakeGenericMethod(state.GenericArguments),
                        state.RestMethod.RefitSettings),
                (RestMethod: restMethodInfo, GenericArguments: genericArguments))
            : restMethodInfo;

    /// <summary>Builds a result delegate for a method by invoking the named generic builder over the result types.</summary>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <param name="builderMethodName">The name of the private generic builder method.</param>
    /// <returns>A delegate that invokes the method.</returns>
    [RequiresUnreferencedCode("Building generic result delegates requires generic method metadata to be available at runtime.")]
    [RequiresDynamicCode("Building generic result delegates requires runtime generic method instantiation.")]
    private Func<HttpClient, object[], object?> BuildResultFuncForMethod(
        RestMethodInfoInternal restMethod,
        string builderMethodName)
    {
        var builderMethodInfo = FindDeclaredMethod(builderMethodName);
        var resultFunc = (MulticastDelegate?)
            builderMethodInfo!.MakeGenericMethod(
                    restMethod.ReturnResultType,
                    restMethod.DeserializedResultType)
                .Invoke(this, [restMethod]);

        // The array is explicit: DynamicInvoke takes 'params object?[]?', and its expanded form
        // packs (client, args) into a single argument array rather than forwarding them one-to-one.
        return (client, args) => resultFunc!.DynamicInvoke([client, args]);
    }

    /// <summary>Builds a delegate for a method whose return type a registered <see cref="IReturnTypeAdapter{TReturn, TResult}"/> surfaces.</summary>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <returns>A delegate that adapts the deferred HTTP call into the surfaced return type.</returns>
    [RequiresUnreferencedCode("Building return-type adapter delegates requires generic method metadata to be available at runtime.")]
    [RequiresDynamicCode("Building return-type adapter delegates requires runtime generic method instantiation.")]
    private Func<HttpClient, object[], object?> BuildAdapterFuncForMethod(RestMethodInfoInternal restMethod)
    {
        var builderMethodInfo = FindDeclaredMethod(nameof(BuildAdapterFuncForMethodGeneric));
        return (Func<HttpClient, object[], object?>)
            builderMethodInfo!.MakeGenericMethod(
                    restMethod.ReturnResultType,
                    restMethod.DeserializedResultType)
                .Invoke(this, [restMethod])!;
    }

    /// <summary>Builds an adapter invocation delegate for a method with known result and body types.</summary>
    /// <typeparam name="T">The result type the HTTP call materializes (the adapter's <c>TResult</c>).</typeparam>
    /// <typeparam name="TBody">The body type used for API responses.</typeparam>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <returns>A delegate that returns the adapter's surfaced value.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode("Instantiating the adapter and resolving its interface method requires adapter metadata to be available at runtime.")]
    [RequiresDynamicCode("Instantiating the adapter and closing its interface method requires runtime generic type instantiation.")]
    private Func<HttpClient, object[], object?> BuildAdapterFuncForMethodGeneric<T, TBody>(
        RestMethodInfoInternal restMethod)
    {
        var taskFunc = BuildCancellableTaskFuncForMethod<T, TBody>(restMethod);

        // This runs only when HasReturnTypeAdapter already matched an adapter for this exact return type and adapter
        // set, so ResolveClosedAdapterType resolves the same match and never returns null here.
        var adapterType = ReturnTypeAdapterResolver.ResolveClosedAdapterType(
            restMethod.ReturnType,
            restMethod.RefitSettings.ReturnTypeAdapters)!;
        var adapter = Activator.CreateInstance(adapterType)!;

        // The adapter implements IReturnTypeAdapter<ReturnType, T>; T is the inner result classified for it, so the
        // closed interface method matches the strongly typed invoke delegate built below.
        var adapterInterface = typeof(IReturnTypeAdapter<,>).MakeGenericType(restMethod.ReturnType, typeof(T));
        var adaptMethod = adapterInterface.GetMethod(AdaptMethodName)!;

        return (client, paramList) =>
        {
            var methodCt = restMethod.CancellationToken is not null
                ? GetCancellationToken(paramList)
                : CancellationToken.None;

            Func<CancellationToken, Task<T?>> invoke = ct =>
            {
                // Link the adapter's per-invocation token with the method's token, and keep the linked source alive
                // until the request finishes. The reflection builder rebuilds the request on each invoke, so a cold
                // adapter can re-subscribe.
                var cts = CancellationTokenSource.CreateLinkedTokenSource(methodCt, ct);
                var task = taskFunc(client, cts.Token, paramList);
                return DisposeWhenDoneAsync(task, cts);
            };

            return adaptMethod.Invoke(adapter, [invoke]);
        };
    }

    /// <summary>Builds a synchronous invocation delegate for a generated (sync) interface method.</summary>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <returns>A delegate that invokes the method synchronously.</returns>
    [RequiresUnreferencedCode("Building synchronous result delegates requires generic method metadata to be available at runtime.")]
    [RequiresDynamicCode("Building synchronous result delegates requires runtime generic method instantiation.")]
    private Func<HttpClient, object[], object?> BuildGeneratedSyncFuncForMethod(
        RestMethodInfoInternal restMethod)
    {
        if (restMethod.ReturnResultType == typeof(void))
        {
            return (client, paramList) =>
            {
                RunSynchronous(() =>
                    ExecuteVoidRequestAsync(
                        client,
                        restMethod,
                        paramList,
                        paramsContainsCancellationToken: false,
                        CancellationToken.None));
                return null;
            };
        }

        var syncFuncMi = FindDeclaredMethod(nameof(BuildGeneratedSyncFuncForMethodGeneric));
        var func =
            syncFuncMi!
                .MakeGenericMethod(
                    restMethod.ReturnResultType,
                    restMethod.DeserializedResultType)
                .Invoke(this, [restMethod]);
        return (Func<HttpClient, object[], object?>)func!;
    }

    /// <summary>Builds a synchronous invocation delegate for a generated method with known result and body types.</summary>
    /// <typeparam name="T">The deserialized result type.</typeparam>
    /// <typeparam name="TBody">The body type used for API responses.</typeparam>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <returns>A delegate that invokes the method synchronously.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private Func<HttpClient, object[], object?> BuildGeneratedSyncFuncForMethodGeneric<T, TBody>(
        RestMethodInfoInternal restMethod) =>
        (client, paramList) =>
            RunSynchronous(() =>
                ExecuteRequestAsync<T, TBody>(
                    client,
                    restMethod,
                    paramList,
                    paramsContainsCancellationToken: false,
                    CancellationToken.None));

    /// <summary>Builds an observable invocation delegate for a method.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The body type used for API responses.</typeparam>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <returns>A delegate that returns an observable of the result.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private Func<HttpClient, object[], IObservable<T?>> BuildRxFuncForMethod<T, TBody>(
        RestMethodInfoInternal restMethod)
    {
        var taskFunc = BuildCancellableTaskFuncForMethod<T, TBody>(restMethod);

        return (client, paramList) =>
            new FromAsyncSignal<T?>(ct =>
            {
                var methodCt = CancellationToken.None;
                if (restMethod.CancellationToken is not null)
                {
                    methodCt = GetCancellationToken(paramList);
                }

                // link the two
                var cts = CancellationTokenSource.CreateLinkedTokenSource(methodCt, ct);

                var task = taskFunc(client, cts.Token, paramList);

                // Keep the linked source alive until the request completes, then dispose it.
                return DisposeWhenDoneAsync(task, cts);
            });
    }

    /// <summary>Builds a delegate that streams the response of a method returning <see cref="IAsyncEnumerable{T}"/>.</summary>
    /// <typeparam name="T">The element type yielded to the caller.</typeparam>
    /// <typeparam name="TBody">Unused; present so the delegate factory shares the two-type-parameter shape.</typeparam>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <returns>A delegate that returns an asynchronous sequence of the result.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [SuppressMessage(
        "StyleSharp",
        "SST1452:Unused type parameters should be removed",
        Justification = "The second type parameter is required so this factory matches the two-type-argument shape invoked reflectively by BuildResultFuncForMethod.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private Func<HttpClient, object[], IAsyncEnumerable<T?>> BuildAsyncEnumerableFuncForMethod<T, TBody>(
        RestMethodInfoInternal restMethod) =>
        (client, paramList) => ExecuteAsyncEnumerableRequestAsync<T>(client, restMethod, paramList);

    /// <summary>Builds a task invocation delegate for a method.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The body type used for API responses.</typeparam>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <returns>A delegate that returns a task of the result.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private Func<HttpClient, object[], Task<T?>> BuildTaskFuncForMethod<T, TBody>(
        RestMethodInfoInternal restMethod)
    {
        var ret = BuildCancellableTaskFuncForMethod<T, TBody>(restMethod);

        return (client, paramList) =>
            restMethod.CancellationToken is not null
                ? ret(client, GetCancellationToken(paramList), paramList)
                : ret(client, CancellationToken.None, paramList);
    }

    /// <summary>Builds a value-task invocation delegate for a method.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The body type used for API responses.</typeparam>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <returns>A delegate that returns a value task of the result.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private Func<HttpClient, object[], ValueTask<T?>> BuildValueTaskFuncForMethod<T, TBody>(
        RestMethodInfoInternal restMethod)
    {
        var ret = BuildTaskFuncForMethod<T, TBody>(restMethod);

        return (client, paramList) => new(ret(client, paramList));
    }

    /// <summary>Builds a task invocation delegate for a method with no response body.</summary>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <returns>A delegate that returns a task with no result.</returns>
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private Func<HttpClient, object[], Task> BuildVoidTaskFuncForMethod(
        RestMethodInfoInternal restMethod) =>
        (client, paramList) =>
        {
            var ct = CancellationToken.None;

            if (restMethod.CancellationToken is not null)
            {
                ct = GetCancellationToken(paramList);
            }

            return ExecuteVoidRequestAsync(
                client,
                restMethod,
                paramList,
                restMethod.CancellationToken is not null,
                ct);
        };
}
