using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Refit
{
    /// <summary>
    /// Defines various parameters on how Refit should work.
    /// </summary>
    public class RefitSettings
    {
        /// <summary>
        /// Creates a new <see cref="RefitSettings"/> instance with the default parameters
        /// </summary>
        public RefitSettings()
        {
            ContentSerializer = new SystemTextJsonContentSerializer();
            UrlParameterKeyFormatter = new DefaultUrlParameterKeyFormatter();
            UrlParameterFormatter = new DefaultUrlParameterFormatter();
            FormUrlEncodedParameterFormatter = new DefaultFormUrlEncodedParameterFormatter();
            ExceptionFactory = new DefaultApiExceptionFactory(this).CreateAsync;
        }

        /// <summary>
        /// Creates a new <see cref="RefitSettings"/> instance with the specified parameters
        /// </summary>
        /// <param name="contentSerializer">The <see cref="IHttpContentSerializer"/> instance to use</param>
        /// <param name="urlParameterFormatter">The <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>)</param>
        /// <param name="formUrlEncodedParameterFormatter">The <see cref="IFormUrlEncodedParameterFormatter"/> instance to use (defaults to <see cref="DefaultFormUrlEncodedParameterFormatter"/>)</param>
        public RefitSettings(
            IHttpContentSerializer contentSerializer,
            IUrlParameterFormatter? urlParameterFormatter,
            IFormUrlEncodedParameterFormatter? formUrlEncodedParameterFormatter
        )
            : this(contentSerializer, urlParameterFormatter, formUrlEncodedParameterFormatter, null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RefitSettings"/> instance with the specified parameters
        /// </summary>
        /// <param name="contentSerializer">The <see cref="IHttpContentSerializer"/> instance to use</param>
        /// <param name="urlParameterFormatter">The <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>)</param>
        /// <param name="formUrlEncodedParameterFormatter">The <see cref="IFormUrlEncodedParameterFormatter"/> instance to use (defaults to <see cref="DefaultFormUrlEncodedParameterFormatter"/>)</param>
        /// <param name="urlParameterKeyFormatter">The <see cref="IUrlParameterKeyFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterKeyFormatter"/>)</param>
        public RefitSettings(
            IHttpContentSerializer contentSerializer,
            IUrlParameterFormatter? urlParameterFormatter = null,
            IFormUrlEncodedParameterFormatter? formUrlEncodedParameterFormatter = null,
            IUrlParameterKeyFormatter? urlParameterKeyFormatter = null
        )
        {
            ContentSerializer =
                contentSerializer
                ?? throw new ArgumentNullException(
                    nameof(contentSerializer),
                    "The content serializer can't be null"
                );
            UrlParameterFormatter = urlParameterFormatter ?? new DefaultUrlParameterFormatter();
            FormUrlEncodedParameterFormatter =
                formUrlEncodedParameterFormatter ?? new DefaultFormUrlEncodedParameterFormatter();
            UrlParameterKeyFormatter =
                urlParameterKeyFormatter ?? new DefaultUrlParameterKeyFormatter();
            ExceptionFactory = new DefaultApiExceptionFactory(this).CreateAsync;
        }

        /// <summary>
        /// Supply a function to provide the Authorization header. Does not work if you supply an HttpClient instance.
        /// </summary>
        public Func<
            HttpRequestMessage,
            CancellationToken,
            Task<string>
        >? AuthorizationHeaderValueGetter { get; set; }

        /// <summary>
        /// Supply a custom inner HttpMessageHandler. Does not work if you supply an HttpClient instance.
        /// </summary>
        public Func<HttpMessageHandler>? HttpMessageHandlerFactory { get; set; }

        /// <summary>
        /// Supply a function to provide <see cref="Exception"/> based on <see cref="HttpResponseMessage"/>.
        /// If function returns null - no exception is thrown.
        /// </summary>
        public Func<HttpResponseMessage, Task<Exception?>> ExceptionFactory { get; set; }

        /// <summary>
        /// Defines how requests' content should be serialized. (defaults to <see cref="SystemTextJsonContentSerializer"/>)
        /// </summary>
        public IHttpContentSerializer ContentSerializer { get; set; }

        /// <summary>
        /// The <see cref="IUrlParameterKeyFormatter"/> instance to use for formatting URL parameter keys (defaults to <see cref="DefaultUrlParameterKeyFormatter" />.
        /// Allows customization of key naming conventions.
        /// </summary>
        public IUrlParameterKeyFormatter UrlParameterKeyFormatter { get; set; }

        /// <summary>
        /// The <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>)
        /// </summary>
        public IUrlParameterFormatter UrlParameterFormatter { get; set; }

        /// <summary>
        /// The <see cref="IFormUrlEncodedParameterFormatter"/> instance to use (defaults to <see cref="DefaultFormUrlEncodedParameterFormatter"/>)
        /// </summary>
        public IFormUrlEncodedParameterFormatter FormUrlEncodedParameterFormatter { get; set; }

        /// <summary>
        /// Sets the default collection format to use. (defaults to <see cref="CollectionFormat.RefitParameterFormatter"/>)
        /// </summary>
        public CollectionFormat CollectionFormat { get; set; } =
            CollectionFormat.RefitParameterFormatter;

        /// <summary>
        /// Sets the default behavior when sending a request's body content. (defaults to false, request body is not streamed to the server)
        /// </summary>
        public bool Buffered { get; set; }

        /// <summary>
        /// Optional Key-Value pairs, which are displayed in the property <see cref="HttpRequestMessage.Properties"/>.
        /// </summary>
        public Dictionary<string, object>? HttpRequestMessageOptions { get; set; }
    }

    /// <summary>
    /// Provides content serialization to <see cref="HttpContent"/>.
    /// </summary>
    public interface IHttpContentSerializer
    {
        /// <summary>
        /// Serializes an object of type <typeparamref name="T"/> to <see cref="HttpContent"/>
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize from.</typeparam>
        /// <param name="item">Object to serialize.</param>
        /// <returns><see cref="HttpContent"/> that contains the serialized <typeparamref name="T"/> object.</returns>
        HttpContent ToHttpContent<T>(T item);

        /// <summary>
        /// Deserializes an object of type <typeparamref name="T"/> from an <see cref="HttpContent"/> object.
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize to.</typeparam>
        /// <param name="content">HttpContent object to deserialize.</param>
        /// <param name="cancellationToken">CancellationToken to abort the deserialization.</param>
        /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
        Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Calculates what the field name should be for the given property. This may be affected by custom attributes the serializer understands
        /// </summary>
        /// <param name="propertyInfo">A PropertyInfo object.</param>
        /// <returns>The calculated field name.</returns>
        string? GetFieldNameForProperty(PropertyInfo propertyInfo);
    }

    /// <summary>
    /// Provides a mechanism for formatting URL parameter keys, allowing customization of key naming conventions.
    /// </summary>
    public interface IUrlParameterKeyFormatter
    {
        /// <summary>
        /// Formats the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        string Format(string key);
    }

    /// <summary>
    /// Provides Url parameter formatting.
    /// </summary>
    public interface IUrlParameterFormatter
    {
        /// <summary>
        /// Formats the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="attributeProvider">The attribute provider.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type);
    }

    /// <summary>
    /// Provides form Url-encoded parameter formatting.
    /// </summary>
    public interface IFormUrlEncodedParameterFormatter
    {
        /// <summary>
        /// Formats the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="formatString">The format string.</param>
        /// <returns></returns>
        string? Format(object? value, string? formatString);
    }

    /// <summary>
    /// Default Url parameter key formatter. Does not do any formatting.
    /// </summary>
    public class DefaultUrlParameterKeyFormatter : IUrlParameterKeyFormatter
    {
        /// <summary>
        /// Formats the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public virtual string Format(string key) => key;
    }

    /// <summary>
    /// Default Url parameter formater.
    /// </summary>
    public class DefaultUrlParameterFormatter : IUrlParameterFormatter
    {
        static readonly ConcurrentDictionary<
            Type,
            ConcurrentDictionary<string, EnumMemberAttribute?>
        > EnumMemberCache = new();

        /// <summary>
        /// Formats the specified parameter value.
        /// </summary>
        /// <param name="parameterValue">The parameter value.</param>
        /// <param name="attributeProvider">The attribute provider.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">attributeProvider</exception>
        public virtual string? Format(
            object? parameterValue,
            ICustomAttributeProvider attributeProvider,
            Type type
        )
        {
            if (attributeProvider is null)
            {
                throw new ArgumentNullException(nameof(attributeProvider));
            }

            if (parameterValue == null)
            {
                return null;
            }

            // See if we have a format
            var formatString = attributeProvider
                .GetCustomAttributes(typeof(QueryAttribute), true)
                .OfType<QueryAttribute>()
                .FirstOrDefault()
                ?.Format;

            EnumMemberAttribute? enumMember = null;
            var parameterType = parameterValue.GetType();
            if (parameterType.IsEnum)
            {
                var cached = EnumMemberCache.GetOrAdd(
                    parameterType,
                    t => new ConcurrentDictionary<string, EnumMemberAttribute?>()
                );
                enumMember = cached.GetOrAdd(
                    parameterValue.ToString()!,
                    val =>
                        parameterType
                            .GetMember(val)
                            .First()
                            .GetCustomAttribute<EnumMemberAttribute>()
                );
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                string.IsNullOrWhiteSpace(formatString) ? "{0}" : $"{{0:{formatString}}}",
                enumMember?.Value ?? parameterValue
            );
        }
    }

    /// <summary>
    /// Default form Url-encoded parameter formatter.
    /// </summary>
    public class DefaultFormUrlEncodedParameterFormatter : IFormUrlEncodedParameterFormatter
    {
        static readonly ConcurrentDictionary<
            Type,
            ConcurrentDictionary<string, EnumMemberAttribute?>
        > EnumMemberCache = new();

        /// <summary>
        /// Formats the specified parameter value.
        /// </summary>
        /// <param name="parameterValue">The parameter value.</param>
        /// <param name="formatString">The format string.</param>
        /// <returns></returns>
        public virtual string? Format(object? parameterValue, string? formatString)
        {
            if (parameterValue == null)
            {
                return null;
            }

            var parameterType = parameterValue.GetType();

            EnumMemberAttribute? enumMember = null;
            if (parameterType.GetTypeInfo().IsEnum)
            {
                var cached = EnumMemberCache.GetOrAdd(
                    parameterType,
                    t => new ConcurrentDictionary<string, EnumMemberAttribute?>()
                );
                enumMember = cached.GetOrAdd(
                    parameterValue.ToString()!,
                    val =>
                        parameterType
                            .GetMember(val)
                            .First()
                            .GetCustomAttribute<EnumMemberAttribute>()
                );
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                string.IsNullOrWhiteSpace(formatString) ? "{0}" : $"{{0:{formatString}}}",
                enumMember?.Value ?? parameterValue
            );
        }
    }

    /// <summary>
    /// Default Api exception factory.
    /// </summary>
    public class DefaultApiExceptionFactory(RefitSettings refitSettings)
    {
        static readonly Task<Exception?> NullTask = Task.FromResult<Exception?>(null);

        /// <summary>
        /// Creates the asynchronous.
        /// </summary>
        /// <param name="responseMessage">The response message.</param>
        /// <returns></returns>
        public Task<Exception?> CreateAsync(HttpResponseMessage responseMessage)
        {
            if (responseMessage?.IsSuccessStatusCode == false)
            {
                return CreateExceptionAsync(responseMessage, refitSettings)!;
            }
            else
            {
                return NullTask;
            }
        }

        static async Task<Exception> CreateExceptionAsync(
            HttpResponseMessage responseMessage,
            RefitSettings refitSettings
        )
        {
            var requestMessage = responseMessage.RequestMessage!;
            var method = requestMessage.Method;

            return await ApiException
                .Create(requestMessage, method, responseMessage, refitSettings)
                .ConfigureAwait(false);
        }
    }
}
