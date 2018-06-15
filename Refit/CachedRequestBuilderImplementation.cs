using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;

namespace Refit
{
    class CachedRequestBuilderImplementation : IRequestBuilderInternal
    {
        public CachedRequestBuilderImplementation(IRequestBuilderInternal innerBuilder)
        {
            this.innerBuilder = innerBuilder;
        }

        readonly IRequestBuilderInternal innerBuilder;
        readonly ConcurrentDictionary<string, Func<HttpClient, object[], object>> methodDictionary = new ConcurrentDictionary<string, Func<HttpClient, object[], object>>();

        public Func<HttpClient, object[], object> BuildRestResultFuncForMethod(string methodName, Type[] parameterTypes = null, Type[] genericArgumentTypes = null)
        {
            string cacheKey = GetCacheKey(methodName, parameterTypes, genericArgumentTypes);
            var func = methodDictionary.GetOrAdd(cacheKey, _ => innerBuilder.BuildRestResultFuncForMethod(methodName, parameterTypes, genericArgumentTypes));

            return func;
        }

        public Type TargetType => innerBuilder.TargetType;

        string GetCacheKey(string methodName, Type[] parameterTypes, Type[] genericArgumentTypes)
        {
            string genericDefinition = GetGenericString(genericArgumentTypes);
            string argumentString = GetArgumentString(parameterTypes);

            return $"{methodName}{genericDefinition}({argumentString})";
        }

        string GetArgumentString(Type[] parameterTypes)
        {
            if (parameterTypes == null || parameterTypes.Length == 0)
            {
                return "";
            }

            return string.Join(", ", parameterTypes.Select(t => t.Name));
        }

        string GetGenericString(Type[] genericArgumentTypes)
        {
            if (genericArgumentTypes == null || genericArgumentTypes.Length == 0)
            {
                return "";
            }

            return "<" + string.Join(", ", genericArgumentTypes.Select(t => t.Name)) + ">";
        }
    }
}
