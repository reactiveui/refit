﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;

namespace Refit
{
    class CachedRequestBuilderImplementation<T> : IRequestBuilder<T>
    {
        public CachedRequestBuilderImplementation(IRequestBuilder<T> innerBuilder)
        {
            this.innerBuilder = innerBuilder;
        }

        readonly IRequestBuilder<T> innerBuilder;
        readonly ConcurrentDictionary<string, Func<HttpClient, object[], object>> methodDictionary = new ConcurrentDictionary<string, Func<HttpClient, object[], object>>();

        public Func<HttpClient, object[], object> BuildRestResultFuncForMethod(string methodName, Type[] parameterTypes = null, Type[] genericArgumentTypes = null)
        {
            var cacheKey = GetCacheKey(methodName, parameterTypes, genericArgumentTypes);
            var func = methodDictionary.GetOrAdd(cacheKey, _ => innerBuilder.BuildRestResultFuncForMethod(methodName, parameterTypes, genericArgumentTypes));

            return func;
        }

        string GetCacheKey(string methodName, Type[] parameterTypes, Type[] genericArgumentTypes)
        {
            var genericDefinition = GetGenericString(genericArgumentTypes);
            var argumentString = GetArgumentString(parameterTypes);

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
