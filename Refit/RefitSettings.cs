using System;
using System.Collections.Generic;
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
        public virtual string Format(object parameterValue, ParameterInfo parameterInfo)
        {
            // See if we have a format
            var formatString = parameterInfo.GetCustomAttribute<QueryAttribute>(true)?.Format;

            EnumMemberAttribute enummember = null;
            if (parameterValue != null && parameterInfo.ParameterType.GetTypeInfo().IsEnum)
            {
                enummember = parameterInfo.ParameterType
                    .GetMember(parameterValue.ToString())
                    .FirstOrDefault()?
                    .GetCustomAttribute<EnumMemberAttribute>();
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
        public virtual string Format(object parameterValue, string formatString)
        {
            if (parameterValue == null)
                return null;

            var parameterType = parameterValue.GetType();

            EnumMemberAttribute enummember = null;
            if (parameterValue != null && parameterType.GetTypeInfo().IsEnum)
            {
                enummember = parameterType
                    .GetMember(parameterValue.ToString())
                    .FirstOrDefault()?
                    .GetCustomAttribute<EnumMemberAttribute>();
            }

            return string.Format(CultureInfo.InvariantCulture, 
                                 string.IsNullOrWhiteSpace(formatString)
                                     ? "{0}"
                                     : $"{{0:{formatString}}}",
                                 enummember?.Value ?? parameterValue);
        }
    }
}
