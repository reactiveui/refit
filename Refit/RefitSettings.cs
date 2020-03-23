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
        JsonSerializerSettings jsonSerializerSettings;

        /// <summary>
        /// Creates a new <see cref="RefitSettings"/> instance with the default parameters
        /// </summary>
        public RefitSettings()
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer();
            UrlParameterFormatter = new DefaultUrlParameterFormatter();
            FormUrlEncodedParameterFormatter = new DefaultFormUrlEncodedParameterFormatter();
        }

        /// <summary>
        /// Creates a new <see cref="RefitSettings"/> instance with the specified parameters
        /// </summary>
        /// <param name="contentSerializer">The <see cref="IContentSerializer"/> instance to use</param>
        /// <param name="urlParameterFormatter">The <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>)</param>
        /// <param name="formUrlEncodedParameterFormatter">The <see cref="IFormUrlEncodedParameterFormatter"/> instance to use (defaults to <see cref="DefaultFormUrlEncodedParameterFormatter"/>)</param>
        public RefitSettings(
            IContentSerializer contentSerializer,
            IUrlParameterFormatter urlParameterFormatter = null,
            IFormUrlEncodedParameterFormatter formUrlEncodedParameterFormatter = null)
        {
            ContentSerializer = contentSerializer ?? throw new ArgumentNullException(nameof(contentSerializer), "The content serializer can't be null");
            UrlParameterFormatter = urlParameterFormatter ?? new DefaultUrlParameterFormatter();
            FormUrlEncodedParameterFormatter = formUrlEncodedParameterFormatter ?? new DefaultFormUrlEncodedParameterFormatter();
        }

        /// <summary>
        /// Supply a function to provide the Authorization header. Does not work if you supply an HttpClient instance.
        /// </summary>
        public Func<Task<string>> AuthorizationHeaderValueGetter { get; set; }

        /// <summary>
        /// Supply a function to provide the Authorization header. Does not work if you supply an HttpClient instance.
        /// </summary>
        public Func<HttpRequestMessage, Task<string>> AuthorizationHeaderValueWithParamGetter { get; set; }

        /// <summary>
        /// Supply a custom inner HttpMessageHandler. Does not work if you supply an HttpClient instance.
        /// </summary>
        public Func<HttpMessageHandler> HttpMessageHandlerFactory { get; set; }

        [Obsolete("Set RefitSettings.ContentSerializer = new NewtonsoftJsonContentSerializer(JsonSerializerSettings) instead.", false)]
        public JsonSerializerSettings JsonSerializerSettings
        {
            get => jsonSerializerSettings;
            set
            {
                jsonSerializerSettings = value;
                ContentSerializer = new JsonContentSerializer(value);
            }
        }

        public IContentSerializer ContentSerializer { get; set; }
        public IUrlParameterFormatter UrlParameterFormatter { get; set; }
        public IFormUrlEncodedParameterFormatter FormUrlEncodedParameterFormatter { get; set; }
        public CollectionFormat CollectionFormat { get; set; } = CollectionFormat.RefitParameterFormatter;
        public bool Buffered { get; set; } = true;
    }

    public interface IContentSerializer
    {
        Task<HttpContent> SerializeAsync<T>(T item);

        Task<T> DeserializeAsync<T>(HttpContent content);
    }

    public interface IUrlParameterFormatter
    {
        string Format(object value, ICustomAttributeProvider attributeProvider, Type type);
    }

    public interface IFormUrlEncodedParameterFormatter
    {
        string Format(object value, string formatString);
    }

    public class DefaultUrlParameterFormatter : IUrlParameterFormatter
    {
        static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumMemberAttribute>> EnumMemberCache
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumMemberAttribute>>();

        public virtual string Format(object parameterValue, ICustomAttributeProvider attributeProvider, Type type)
        {

            // See if we have a format
            var formatString = attributeProvider.GetCustomAttributes(typeof(QueryAttribute), true)
                .OfType<QueryAttribute>()
                .FirstOrDefault()?.Format;

            EnumMemberAttribute enummember = null;
            if (parameterValue != null && type.GetTypeInfo().IsEnum)
            {
                var cached = EnumMemberCache.GetOrAdd(type, t => new ConcurrentDictionary<string, EnumMemberAttribute>());
                enummember = cached.GetOrAdd(parameterValue.ToString(), val => type.GetMember(val).First().GetCustomAttribute<EnumMemberAttribute>());
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
        static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumMemberAttribute>> EnumMemberCache
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumMemberAttribute>>();

        public virtual string Format(object parameterValue, string formatString)
        {
            if (parameterValue == null)
                return null;

            var parameterType = parameterValue.GetType();

            EnumMemberAttribute enummember = null;
            if (parameterType.GetTypeInfo().IsEnum)
            {
                var cached = EnumMemberCache.GetOrAdd(parameterType, t => new ConcurrentDictionary<string, EnumMemberAttribute>());
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
