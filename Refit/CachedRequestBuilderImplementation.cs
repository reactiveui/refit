using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Refit
{
    public class CachedRequestBuilderImplementation : IRequestBuilder
    {
        readonly IRequestBuilder innerBuilder;
        readonly ConcurrentDictionary<string, Func<HttpClient, object[], object>> methodDictionary = new ConcurrentDictionary<string, Func<HttpClient, object[], object>>();

        public CachedRequestBuilderImplementation(IRequestBuilder innerBuilder)
        {
            this.innerBuilder = innerBuilder;
        }

        public Func<HttpClient, object[], object> BuildRestResultFuncForMethod(string methodName, Type[] parameterTypes = null, Type[] genericArgumentTypes = null)
        {
            string cacheKey = GetCacheKey(methodName, parameterTypes, genericArgumentTypes);
            var func = methodDictionary.GetOrAdd(cacheKey, _ => innerBuilder.BuildRestResultFuncForMethod(methodName, parameterTypes, genericArgumentTypes));

            return func;
        }

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

            return parameterTypes.Select(t => t.Name).Aggregate((s1, s2) => s1 + ", " + s2);
        }
    
        string GetGenericString(Type[] genericArgumentTypes)
        {
            if (genericArgumentTypes == null || genericArgumentTypes.Length == 0)
            {
                return "";
            }

            return "<" + genericArgumentTypes.Select(t => t.Name).Aggregate((s1, s2) => s1 + ", " + s2) + ">";
        }

        public Type targetType => innerBuilder.targetType;
    }
}
