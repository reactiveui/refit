using System.Collections.Concurrent;
using System.Net.Http;

namespace Refit
{
    class CachedRequestBuilderImplementation<T>
        : CachedRequestBuilderImplementation,
            IRequestBuilder<T>
    {
        public CachedRequestBuilderImplementation(IRequestBuilder<T> innerBuilder)
            : base(innerBuilder) { }
    }

    class CachedRequestBuilderImplementation : IRequestBuilder
    {
        public CachedRequestBuilderImplementation(IRequestBuilder innerBuilder)
        {
            this.innerBuilder =
                innerBuilder ?? throw new ArgumentNullException(nameof(innerBuilder));
            this.Settings = innerBuilder.Settings;
        }

        readonly IRequestBuilder innerBuilder;
        internal readonly ConcurrentDictionary<
            MethodTableKey,
            Func<HttpClient, object[], object?>
        > MethodDictionary = new();

        public RefitSettings Settings { get; }

        public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
            string methodName,
            Type[]? parameterTypes = null,
            Type[]? genericArgumentTypes = null
        )
        {
            var cacheKey = new MethodTableKey(
                methodName,
                parameterTypes ?? Array.Empty<Type>(),
                genericArgumentTypes ?? Array.Empty<Type>()
            );

            if (MethodDictionary.TryGetValue(cacheKey, out var methodFunc))
            {
                return methodFunc;
            }

            // use GetOrAdd with cloned array method table key. This prevents the array from being modified, breaking the dictionary.
            var func = MethodDictionary.GetOrAdd(
                new MethodTableKey(methodName,
                    parameterTypes?.ToArray() ?? Array.Empty<Type>(),
                    genericArgumentTypes?.ToArray() ?? Array.Empty<Type>()),
                _ =>
                    innerBuilder.BuildRestResultFuncForMethod(
                        methodName,
                        parameterTypes,
                        genericArgumentTypes
                    )
            );

            return func;
        }
    }

    /// <summary>
    /// Represents a method composed of its name, generic arguments and parameters.
    /// </summary>
    internal readonly struct MethodTableKey : IEquatable<MethodTableKey>
    {
        /// <summary>
        /// Constructs an instance of <see cref="MethodTableKey"/>.
        /// </summary>
        /// <param name="methodName">Represents the methods name.</param>
        /// <param name="parameters">Array containing the methods parameters.</param>
        /// <param name="genericArguments">Array containing the methods generic arguments.</param>
        public MethodTableKey (string methodName, Type[] parameters, Type[] genericArguments)
        {
            MethodName = methodName;
            Parameters = parameters;
            GenericArguments = genericArguments;
        }

        /// <summary>
        /// The methods name.
        /// </summary>
        string MethodName { get; }

        /// <summary>
        /// Array containing the methods parameters.
        /// </summary>
        Type[] Parameters { get; }

        /// <summary>
        /// Array containing the methods generic arguments.
        /// </summary>
        Type[] GenericArguments { get; }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MethodName.GetHashCode();

                foreach (var argument in Parameters)
                {
                    hashCode = (hashCode * 397) ^ argument.GetHashCode();
                }

                foreach (var genericArgument in GenericArguments)
                {
                    hashCode = (hashCode * 397) ^ genericArgument.GetHashCode();
                }
                return hashCode;
            }
        }

        public bool Equals(MethodTableKey other)
        {
            if (Parameters.Length != other.Parameters.Length
                || GenericArguments.Length != other.GenericArguments.Length
                || MethodName != other.MethodName)
            {
                return false;
            }

            for (var i = 0; i < Parameters.Length; i++)
            {
                if (Parameters[i] != other.Parameters[i])
                {
                    return false;
                }
            }

            for (var i = 0; i < GenericArguments.Length; i++)
            {
                if (GenericArguments[i] != other.GenericArguments[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is MethodTableKey other && Equals(other);
    }
}
