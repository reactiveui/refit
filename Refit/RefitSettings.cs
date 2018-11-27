using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit
{
    public class RefitSettings
    {
        public RefitSettings()
        {
            UrlParameterFormatter = new DefaultUrlParameterFormatter();
            FormUrlEncodedParameterFormatter = new DefaultFormUrlEncodedParameterFormatter();
        }

        public Func<Task<string>> AuthorizationHeaderValueGetter { get; set; }
        public Func<HttpMessageHandler> HttpMessageHandlerFactory { get; set; }

        public JsonSerializerSettings JsonSerializerSettings { get; set; }
        public IUrlParameterFormatter UrlParameterFormatter { get; set; }
        public IFormUrlEncodedParameterFormatter FormUrlEncodedParameterFormatter { get; set; }
        public bool Buffered { get; set; } = true;
    }

    public interface IUrlParameterFormatter
    {
        string Format(object value, ParameterInfo parameterInfo);
    }

    public interface IFormUrlEncodedParameterFormatter
    {
        string Format(object value, string formatString);
    }

    public class DefaultUrlParameterFormatter : IUrlParameterFormatter
    {
        static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumMemberAttribute>> enumMemberCache
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumMemberAttribute>>();

        public virtual string Format(object parameterValue, ParameterInfo parameterInfo)
        {
            // See if we have a format
            var formatString = parameterInfo.GetCustomAttribute<QueryAttribute>(true)?.Format;

            EnumMemberAttribute enummember = null;
            if (parameterValue != null && parameterInfo.ParameterType.GetTypeInfo().IsEnum)
            {
                var cached = enumMemberCache.GetOrAdd(parameterInfo.ParameterType, t => new ConcurrentDictionary<string, EnumMemberAttribute>());
                enummember = cached.GetOrAdd(parameterValue.ToString(), val => parameterInfo.ParameterType.GetMember(val).First().GetCustomAttribute<EnumMemberAttribute>());
            }

            return parameterValue == null
                       ? null
                       : string.Format(CultureInfo.InvariantCulture,
                                       string.IsNullOrWhiteSpace(formatString)
                                           ? "{0}"
                                           : $"{{0:{formatString}}}",
                                       enummember?.Value ?? parameterValue);
        }
    }

    public class DefaultFormUrlEncodedParameterFormatter : IFormUrlEncodedParameterFormatter
    {
        static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumMemberAttribute>> enumMemberCache
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumMemberAttribute>>();

        public virtual string Format(object parameterValue, string formatString)
        {
            if (parameterValue == null)
                return null;

            var parameterType = parameterValue.GetType();

            EnumMemberAttribute enummember = null;
            if (parameterType.GetTypeInfo().IsEnum)
            {
                var cached = enumMemberCache.GetOrAdd(parameterType, t => new ConcurrentDictionary<string, EnumMemberAttribute>());
                enummember = cached.GetOrAdd(parameterValue.ToString(), val => parameterType.GetMember(val).First().GetCustomAttribute<EnumMemberAttribute>());
            }

            return string.Format(CultureInfo.InvariantCulture,
                                 string.IsNullOrWhiteSpace(formatString)
                                     ? "{0}"
                                     : $"{{0:{formatString}}}",
                                 enummember?.Value ?? parameterValue);
        }
    }
}
