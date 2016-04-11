using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit
{
    public class RefitSettings
    {
        JsonSerializerSettings jsonSerializerSettings;

        public RefitSettings()
        {
            UrlParameterFormatter = new DefaultUrlParameterFormatter();
            ResponseContentDeserializer = new NewtonsoftJsonDeserializer();
        }

        /// <summary>
        /// Default serializer settings
        /// </summary>
        public JsonSerializerSettings JsonSerializerSettings
        {
            get { return jsonSerializerSettings; }
            set
            {
                jsonSerializerSettings = value;

                NewtonsoftJsonDeserializer newtonsoftJsonDeserializer = ResponseContentDeserializer as NewtonsoftJsonDeserializer;
                if (newtonsoftJsonDeserializer != null) newtonsoftJsonDeserializer.JsonSerializerSettings = value;
            }
        }
        public IUrlParameterFormatter UrlParameterFormatter { get; set; }
        public Func<Task<string>> AuthorizationHeaderValueGetter { get; set; }
        public Func<HttpMessageHandler> HttpMessageHandlerFactory { get; set; }

        public IDeserializer ResponseContentDeserializer { get; set; }
    }

    public interface IUrlParameterFormatter
    {
        string Format(object value, ParameterInfo parameterInfo);
    }

    public class DefaultUrlParameterFormatter : IUrlParameterFormatter
    {
        public virtual string Format(object parameterValue, ParameterInfo parameterInfo)
        {
            return parameterValue != null ? parameterValue.ToString() : null;
        }
    }
}
