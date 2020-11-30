using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;

namespace Refit
{
    class CachedRequestBuilderImplementation<T> : CachedRequestBuilderImplementation, IRequestBuilder<T>
    {
        public CachedRequestBuilderImplementation(IRequestBuilder<T> innerBuilder) : base(innerBuilder)
        {
        }
    }

    class CachedRequestBuilderImplementation : IRequestBuilder
    {
        public CachedRequestBuilderImplementation(IRequestBuilder innerBuilder)
        {
            this.innerBuilder = innerBuilder ?? throw new ArgumentNullException(nameof(innerBuilder));
        }

        readonly IRequestBuilder innerBuilder;
        readonly ConcurrentDictionary<string, Func<HttpClient, object[], object?>> methodDictionary = new();

        public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(string methodName, Type[]? parameterTypes = null, Type[]? genericArgumentTypes = null)
        {
            var cacheKey = GetCacheKey(methodName, parameterTypes ?? Array.Empty<Type>(), genericArgumentTypes ?? Array.Empty<Type>());
            var func = methodDictionary.GetOrAdd(cacheKey, _ => innerBuilder.BuildRestResultFuncForMethod(methodName, parameterTypes, genericArgumentTypes));

            return func;
        }

        static string GetCacheKey(string methodName, Type[] parameterTypes, Type[] genericArgumentTypes)
        {
            var genericDefinition = GetGenericString(genericArgumentTypes);
            var argumentString = GetArgumentString(parameterTypes);

            return $"{methodName}{genericDefinition}({argumentString})";
        }

        static string GetArgumentString(Type[] parameterTypes)
        {
            if (parameterTypes == null || parameterTypes.Length == 0)
            {
                return "";
            }

            return string.Join(", ", parameterTypes.Select(t => t.FullName));
        }

        static string GetGenericString(Type[] genericArgumentTypes)
        {
            if (genericArgumentTypes == null || genericArgumentTypes.Length == 0)
            {
                return "";
            }

            return "<" + string.Join(", ", genericArgumentTypes.Select(t => t.FullName)) + ">";
        }
    }
}
