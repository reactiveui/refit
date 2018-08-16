using System;
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
            UrlParameterNameFormatter = new DefaultUrlParameterNameFormatter();
            UrlArgumentValueFormatter = new DefaultUrlArgumentValueFormatter();
            FormUrlEncodedParameterFormatter = new DefaultFormUrlEncodedParameterFormatter();
        }

        public Func<Task<string>> AuthorizationHeaderValueGetter { get; set; }
        public Func<HttpMessageHandler> HttpMessageHandlerFactory { get; set; }

        public JsonSerializerSettings JsonSerializerSettings { get; set; }
        public IUrlParameterNameFormatter UrlParameterNameFormatter { get; set; }
        public IUrlArgumentValueFormatter UrlArgumentValueFormatter { get; set; }
        public IFormUrlEncodedParameterFormatter FormUrlEncodedParameterFormatter { get; set; }
        public bool Buffered { get; set; } = true;

        [Obsolete("Use UrlArgumentValueFormatter instead")]
        public IUrlArgumentValueFormatter UrlParameterFormatter
        {
            get => UrlArgumentValueFormatter;
            set => UrlArgumentValueFormatter = value;
        }
    }

    public interface IUrlParameterNameFormatter
    {
        string Format(string argument, ParameterInfo parameterInfo);
    }

    public interface IUrlArgumentValueFormatter
    {
        string Format(object value, ParameterInfo parameterInfo);
    }

    public interface IFormUrlEncodedParameterFormatter
    {
        string Format(object value, string formatString);
    }

    public class DefaultUrlParameterNameFormatter : IUrlParameterNameFormatter
    {
        public virtual string Format(string argument, ParameterInfo parameterInfo) => argument;
    }

    public class DefaultUrlArgumentValueFormatter : IUrlArgumentValueFormatter
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
            if (parameterValue != null && parameterType.GetTypeInfo().IsEnum)
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

    [Obsolete("Use IUrlArgumentValueFormatter instead")]
    public interface IUrlParameterFormatter : IUrlArgumentValueFormatter { }
    [Obsolete("Use DefaultUrlArgumentValueFormatter instead")]
    public class DefaultParameterFormatter : DefaultUrlArgumentValueFormatter, IUrlParameterFormatter { }
}
