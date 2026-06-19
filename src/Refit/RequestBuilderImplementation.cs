// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Refit
{
    /// <summary>Reflection-based request builder that turns Refit interface calls into HTTP requests.</summary>
    internal partial class RequestBuilderImplementation : IRequestBuilder
    {
        /// <summary>Maximum stack-allocated buffer size, in characters, used when building paths and query strings.</summary>
        private const int StackallocThreshold = 512;

        /// <summary>The message used when content cannot be deserialized into the requested type.</summary>
        private const string DeserializationErrorMessage = "An error occured deserializing the response.";

        /// <summary>The error message used when the HTTP client has no base address configured.</summary>
        private const string BaseAddressRequiredMessage = "BaseAddress must be set on the HttpClient instance";

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
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Minor Code Smell",
            "SST1114:Remove the blank line between the declaration and the first parameter",
            Justification = "False positive: the #if-guarded parameter attribute is required for trim annotations but is unavailable on non-net5 targets.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Minor Code Smell",
            "SST1115:Remove the blank line before this parameter",
            Justification = "False positive: the #if-guarded parameter attribute is required for trim annotations but is unavailable on non-net5 targets.")]
        public RequestBuilderImplementation(
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
#endif
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
                new ConcurrentDictionary<CloseGenericMethodKey, RestMethodInfoInternal>();

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
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
            string methodName,
            Type[]? parameterTypes = null,
            Type[]? genericArgumentTypes = null)
        {
            if (!_interfaceHttpMethods.ContainsKey(methodName))
            {
                throw new ArgumentException(
                    "Method must be defined and have an HTTP Method attribute");
            }

            var restMethod = FindMatchingRestMethodInfo(
                methodName,
                parameterTypes,
                genericArgumentTypes);

            // Task (void)
            if (restMethod.ReturnType == typeof(Task))
            {
                return BuildVoidTaskFuncForMethod(restMethod);
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

            // IObservable<T>
            if (IsGenericReturnType(restMethod, typeof(IObservable<>)))
            {
                return BuildResultFuncForMethod(restMethod, nameof(BuildRxFuncForMethod));
            }

            var isExplicitInterfaceMember = restMethod.MethodInfo.Name.Contains('.');
            var isNonPublic = !restMethod.MethodInfo.IsPublic;
            if (isExplicitInterfaceMember || isNonPublic)
            {
                return BuildGeneratedSyncFuncForMethod(restMethod);
            }

            throw new ArgumentException(
                $"Method \"{restMethod.MethodInfo.Name}\" is invalid. All REST Methods must return either Task<T> or ValueTask<T> or IObservable<T>");
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

            var possibleMethodsCollection = httpMethods.Where(
                method => method.MethodInfo.GetParameters().Length == parameterTypes.Length);

            possibleMethodsCollection = isGeneric
                ? possibleMethodsCollection.Where(
                    method =>
                        method.MethodInfo.IsGenericMethod
                        && method.MethodInfo.GetGenericArguments().Length
                            == genericArgumentTypes!.Length)
                : possibleMethodsCollection.Where(
                    method => !method.MethodInfo.IsGenericMethod);

            return [.. possibleMethodsCollection];
        }

        /// <summary>Runs an asynchronous task factory synchronously and waits for completion.</summary>
        /// <param name="taskFactory">The task factory to run.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Usage",
            "VSTHRD002:Avoid problematic synchronous waits",
            Justification = "Deliberate sync-over-async bridge for synchronous (void/non-Task) interface methods that have no async caller; the work is offloaded via Task.Run to avoid deadlocks.")]
        private static void RunSynchronous(Func<Task> taskFactory) =>
            Task.Run(taskFactory).GetAwaiter().GetResult();

        /// <summary>Runs an asynchronous task factory synchronously and returns its result.</summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="taskFactory">The task factory to run.</param>
        /// <returns>The result produced by the task.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
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

        /// <summary>Discovers the Refit HTTP methods on an interface and adds them to the lookup dictionary.</summary>
        /// <param name="interfaceType">The interface to scan for HTTP methods.</param>
        /// <param name="methods">The dictionary to populate with discovered methods.</param>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
            Justification = "Refit must bind to non-public interface members to resolve explicit interface implementations.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Minor Code Smell",
            "SST1114:Remove the blank line between the declaration and the first parameter",
            Justification = "False positive: the #if-guarded parameter attribute is required for trim annotations but is unavailable on non-net5 targets.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Minor Code Smell",
            "SST1115:Remove the blank line before this parameter",
            Justification = "False positive: the #if-guarded parameter attribute is required for trim annotations but is unavailable on non-net5 targets.")]
        private void AddInterfaceHttpMethods(
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
#endif
            Type interfaceType,
            Dictionary<string, List<RestMethodInfoInternal>> methods)
        {
            var methodInfos = interfaceType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(i => i.IsAbstract);

            foreach (var methodInfo in methodInfos)
            {
                var attrs = methodInfo.GetCustomAttributes(true);
                var hasHttpMethod = attrs.OfType<HttpMethodAttribute>().Any();
                if (hasHttpMethod)
                {
                    var key = GetLookupKeyForMethod(methodInfo);
                    if (!methods.TryGetValue(key, out var value))
                    {
                        value = [];
                        methods.Add(key, value);
                    }

                    var restinfo = new RestMethodInfoInternal(interfaceType, methodInfo, _settings);
                    value.Add(restinfo);
                }
            }
        }

        /// <summary>Finds the rest method matching the given name, parameter types and generic arguments.</summary>
        /// <param name="key">The method lookup key.</param>
        /// <param name="parameterTypes">The parameter types to match, or null to match a single overload.</param>
        /// <param name="genericArgumentTypes">The generic argument types to close over, or null.</param>
        /// <returns>The matching rest method info.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
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
                var match = method
                    .MethodInfo.GetParameters()
                    .Select(p => p.ParameterType)
                    .SequenceEqual(parameterTypes);
                if (match)
                {
                    return CloseGenericMethodIfNeeded(method, genericArgumentTypes);
                }
            }

            throw new InvalidOperationException("No suitable Method found...");
        }

        /// <summary>Closes an open generic rest method over the supplied type arguments, caching the result.</summary>
        /// <param name="restMethodInfo">The (possibly generic) rest method.</param>
        /// <param name="genericArgumentTypes">The generic argument types, or null if not generic.</param>
        /// <returns>The closed rest method info, or the original when no generic arguments are supplied.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        private RestMethodInfoInternal CloseGenericMethodIfNeeded(
            RestMethodInfoInternal restMethodInfo,
            Type[]? genericArgumentTypes)
        {
            if (genericArgumentTypes is not null)
            {
                return _interfaceGenericHttpMethods.GetOrAdd(
                    new CloseGenericMethodKey(restMethodInfo.MethodInfo, genericArgumentTypes),
                    _ =>
                        new RestMethodInfoInternal(
                            restMethodInfo.Type,
                            restMethodInfo.MethodInfo.MakeGenericMethod(genericArgumentTypes),
                            restMethodInfo.RefitSettings));
            }

            return restMethodInfo;
        }

        /// <summary>Builds a result delegate for a method by invoking the named generic builder over the result types.</summary>
        /// <param name="restMethod">The rest method to build a delegate for.</param>
        /// <param name="builderMethodName">The name of the private generic builder method.</param>
        /// <returns>A delegate that invokes the method.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
            Justification = "Refit must invoke its own non-public generic builder methods by reflection.")]
        private Func<HttpClient, object[], object?> BuildResultFuncForMethod(
            RestMethodInfoInternal restMethod,
            string builderMethodName)
        {
            var builderMethodInfo = typeof(RequestBuilderImplementation).GetMethod(
                builderMethodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            var resultFunc = (MulticastDelegate?)
                builderMethodInfo!.MakeGenericMethod(
                        restMethod.ReturnResultType,
                        restMethod.DeserializedResultType)
                    .Invoke(this, [restMethod]);

            return (client, args) => resultFunc!.DynamicInvoke(client, args);
        }

        /// <summary>Builds a synchronous invocation delegate for a generated (sync) interface method.</summary>
        /// <param name="restMethod">The rest method to build a delegate for.</param>
        /// <returns>A delegate that invokes the method synchronously.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
            Justification = "Refit must invoke its own non-public generic builder methods by reflection.")]
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

            var syncFuncMi = typeof(RequestBuilderImplementation).GetMethod(
                nameof(BuildGeneratedSyncFuncForMethodGeneric),
                BindingFlags.NonPublic | BindingFlags.Instance);
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
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private Func<HttpClient, object[], object?> BuildGeneratedSyncFuncForMethodGeneric<T, TBody>(
            RestMethodInfoInternal restMethod)
        {
            return (client, paramList) =>
                RunSynchronous(() =>
                    ExecuteRequestAsync<T, TBody>(
                        client,
                        restMethod,
                        paramList,
                        paramsContainsCancellationToken: false,
                        CancellationToken.None));
        }

        /// <summary>Builds an observable invocation delegate for a method.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <typeparam name="TBody">The body type used for API responses.</typeparam>
        /// <param name="restMethod">The rest method to build a delegate for.</param>
        /// <returns>A delegate that returns an observable of the result.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private Func<HttpClient, object[], IObservable<T?>> BuildRxFuncForMethod<T, TBody>(
            RestMethodInfoInternal restMethod)
        {
            var taskFunc = BuildCancellableTaskFuncForMethod<T, TBody>(restMethod);

            return (client, paramList) =>
                new TaskToObservable<T>(ct =>
                {
                    var methodCt = CancellationToken.None;
                    if (restMethod.CancellationToken is not null)
                    {
                        methodCt = paramList.OfType<CancellationToken>().FirstOrDefault();
                    }

                    // link the two
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(methodCt, ct);

                    var task = taskFunc(client, cts.Token, paramList);

                    // Keep the linked source alive until the request completes, then dispose it.
                    return DisposeWhenDoneAsync(task, cts);
                });
        }

        /// <summary>Builds a task invocation delegate for a method.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <typeparam name="TBody">The body type used for API responses.</typeparam>
        /// <param name="restMethod">The rest method to build a delegate for.</param>
        /// <returns>A delegate that returns a task of the result.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private Func<HttpClient, object[], Task<T?>> BuildTaskFuncForMethod<T, TBody>(
            RestMethodInfoInternal restMethod)
        {
            var ret = BuildCancellableTaskFuncForMethod<T, TBody>(restMethod);

            return (client, paramList) =>
            {
                if (restMethod.CancellationToken is not null)
                {
                    return ret(
                        client,
                        paramList.OfType<CancellationToken>().FirstOrDefault(),
                        paramList);
                }

                return ret(client, CancellationToken.None, paramList);
            };
        }

        /// <summary>Builds a value-task invocation delegate for a method.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <typeparam name="TBody">The body type used for API responses.</typeparam>
        /// <param name="restMethod">The rest method to build a delegate for.</param>
        /// <returns>A delegate that returns a value task of the result.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private Func<HttpClient, object[], ValueTask<T?>> BuildValueTaskFuncForMethod<T, TBody>(
            RestMethodInfoInternal restMethod)
        {
            var ret = BuildTaskFuncForMethod<T, TBody>(restMethod);

            return (client, paramList) => new ValueTask<T?>(ret(client, paramList));
        }

        /// <summary>Builds a task invocation delegate for a method with no response body.</summary>
        /// <param name="restMethod">The rest method to build a delegate for.</param>
        /// <returns>A delegate that returns a task with no result.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        private Func<HttpClient, object[], Task> BuildVoidTaskFuncForMethod(
            RestMethodInfoInternal restMethod)
        {
            return (client, paramList) =>
            {
                var ct = CancellationToken.None;

                if (restMethod.CancellationToken is not null)
                {
                    ct = paramList.OfType<CancellationToken>().FirstOrDefault();
                }

                return ExecuteVoidRequestAsync(
                    client,
                    restMethod,
                    paramList,
                    restMethod.CancellationToken is not null,
                    ct);
            };
        }
    }
}
