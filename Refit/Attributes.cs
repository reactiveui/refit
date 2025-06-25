using System.Net.Http;

namespace Refit
{
    /// <summary>
    /// HttpMethodAttribute.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    /// <remarks>
    /// Initializes a new instance of the <see cref="HttpMethodAttribute"/> class.
    /// </remarks>
    /// <param name="path">The path.</param>
    public abstract class HttpMethodAttribute(string path) : Attribute
    {
        /// <summary>
        /// Gets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public abstract HttpMethod Method { get; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public virtual string Path { get; protected set; } = path;
    }

    /// <summary>
    /// Send the request with HTTP method 'GET'.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="GetAttribute"/> class.
    /// </remarks>
    /// <param name="path">The path.</param>
    [AttributeUsage(AttributeTargets.Method)]
#pragma warning disable CA1813 // Avoid unsealed attributes
    public class GetAttribute(string path) : HttpMethodAttribute(path)
    {
        /// <summary>
        /// Gets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public override HttpMethod Method => HttpMethod.Get;
    }

    /// <summary>
    /// Send the request with HTTP method 'POST'.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PostAttribute"/> class.
    /// </remarks>
    /// <param name="path">The path.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class PostAttribute(string path) : HttpMethodAttribute(path)
    {
        /// <summary>
        /// Gets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public override HttpMethod Method => HttpMethod.Post;
    }

    /// <summary>
    /// Send the request with HTTP method 'PUT'.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PutAttribute"/> class.
    /// </remarks>
    /// <param name="path">The path.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class PutAttribute(string path) : HttpMethodAttribute(path)
    {
        /// <summary>
        /// Gets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public override HttpMethod Method => HttpMethod.Put;
    }

    /// <summary>
    /// Send the request with HTTP method 'DELETE'.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DeleteAttribute"/> class.
    /// </remarks>
    /// <param name="path">The path.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class DeleteAttribute(string path) : HttpMethodAttribute(path)
    {
        /// <summary>
        /// Gets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public override HttpMethod Method => HttpMethod.Delete;
    }

    /// <summary>
    /// Send the request with HTTP method 'PATCH'.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PatchAttribute"/> class.
    /// </remarks>
    /// <param name="path">The path.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchAttribute(string path) : HttpMethodAttribute(path)
    {
        /// <summary>
        /// Gets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public override HttpMethod Method => new("PATCH");
    }

    /// <summary>
    /// Send the request with HTTP method 'OPTION'.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OptionsAttribute"/> class.
    /// </remarks>
    /// <param name="path">The path.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class OptionsAttribute(string path) : HttpMethodAttribute(path)
    {
        /// <summary>
        /// Gets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public override HttpMethod Method => new("OPTIONS");
    }

    /// <summary>
    /// Send the request with HTTP method 'HEAD'.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="HeadAttribute"/> class.
    /// </remarks>
    /// <param name="path">The path.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class HeadAttribute(string path) : HttpMethodAttribute(path)
    {
        /// <summary>
        /// Gets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public override HttpMethod Method => HttpMethod.Head;
    }

    /// <summary>
    /// Send the request as multipart.
    /// </summary>
    /// <remarks>
    /// Currently, multipart methods only support the following parameter types: <see cref="string"/>, <see cref="byte"/> array, <see cref="System.IO.Stream"/>, <see cref="System.IO.FileInfo"/>.
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MultipartAttribute"/> class.
    /// </remarks>
    /// <param name="boundaryText">The boundary text.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class MultipartAttribute(string boundaryText = "----MyGreatBoundary") : Attribute
    {
        /// <summary>
        /// Gets the boundary text.
        /// </summary>
        /// <value>
        /// The boundary text.
        /// </value>
        public string BoundaryText { get; private set; } = boundaryText;
    }

    /// <summary>
    /// Defines methods to serialize HTTP requests' bodies.
    /// </summary>
    public enum BodySerializationMethod
    {
        /// <summary>
        /// Encodes everything using the ContentSerializer in RefitSettings except for strings. Strings are set as-is
        /// </summary>
        Default,

        /// <summary>
        /// Json encodes everything, including strings
        /// </summary>
        [Obsolete("Use BodySerializationMethod.Serialized instead", false)]
        Json,

        /// <summary>
        /// Form-UrlEncode's the values
        /// </summary>
        UrlEncoded,

        /// <summary>
        /// Encodes everything using the ContentSerializer in RefitSettings
        /// </summary>
        Serialized
    }

    /// <summary>
    /// Set a parameter to be sent as the HTTP request's body.
    /// </summary>
    /// <remarks>
    /// There are four behaviors when sending a parameter as the request body:<br/>
    /// - If the type is/implements <see cref="System.IO.Stream"/>, the content will be streamed via <see cref="StreamContent"/>.<br/>
    /// - If the type is <see cref="string"/>, it will be used directly as the content unless <c>[Body(BodySerializationMethod.Json)]</c> is set
    /// which will send it as a <see cref="StringContent"/>.<br/>
    /// - If the parameter has the attribute <c>[Body(BodySerializationMethod.UrlEncoded)]</c>, the content will be URL-encoded.<br/>
    /// - For all other types, the object will be serialized using the content serializer specified in the request's <see cref="RefitSettings"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class BodyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BodyAttribute"/> class.
        /// </summary>
        public BodyAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyAttribute"/> class.
        /// </summary>
        /// <param name="buffered">if set to <c>true</c> [buffered].</param>
        public BodyAttribute(bool buffered) => Buffered = buffered;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyAttribute"/> class.
        /// </summary>
        /// <param name="serializationMethod">The serialization method.</param>
        /// <param name="buffered">if set to <c>true</c> [buffered].</param>
        public BodyAttribute(BodySerializationMethod serializationMethod, bool buffered)
        {
            SerializationMethod = serializationMethod;
            Buffered = buffered;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyAttribute"/> class.
        /// </summary>
        /// <param name="serializationMethod">The serialization method.</param>
        public BodyAttribute(
            BodySerializationMethod serializationMethod = BodySerializationMethod.Default
        )
        {
            SerializationMethod = serializationMethod;
        }

        /// <summary>
        /// Gets or sets the buffered.
        /// </summary>
        /// <value>
        /// The buffered.
        /// </value>
        public bool? Buffered { get; }

        /// <summary>
        /// Gets or sets the serialization method.
        /// </summary>
        /// <value>
        /// The serialization method.
        /// </value>
        public BodySerializationMethod SerializationMethod { get; } =
            BodySerializationMethod.Default;
    }

    /// <summary>
    /// Override the key that will be sent in the query string.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="AliasAsAttribute"/> class.
    /// </remarks>
    /// <param name="name">The name.</param>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class AliasAsAttribute(string name) : Attribute
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; protected set; } = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AttachmentNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    [Obsolete(
        "Use Refit.StreamPart, Refit.ByteArrayPart, Refit.FileInfoPart or if necessary, inherit from Refit.MultipartItem",
        false
    )]
    public class AttachmentNameAttribute(string name) : Attribute
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; protected set; } = name;
    }

    /// <summary>
    /// Allows you to provide a Dictionary of headers to be added to the request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class HeaderCollectionAttribute : Attribute { }

    /// <summary>
    /// Add multiple headers to the request.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="HeadersAttribute"/> class.
    /// </remarks>
    /// <param name="headers">The headers.</param>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class HeadersAttribute(params string[] headers) : Attribute
    {
        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public string[] Headers { get; } = headers ?? [];
    }

    /// <summary>
    /// Add a header to the request.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="HeaderAttribute"/> class.
    /// </remarks>
    /// <param name="header">The header.</param>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class HeaderAttribute(string header) : Attribute
    {
        /// <summary>
        /// Gets the header.
        /// </summary>
        /// <value>
        /// The header.
        /// </value>
        public string Header { get; } = header;
    }

    /// <summary>
    /// Used to store the value in HttpRequestMessage.Properties for further processing in a custom DelegatingHandler.
    /// If a string is supplied to the constructor then it will be used as the key in the HttpRequestMessage.Properties dictionary.
    /// If no key is specified then the key will be defaulted to the name of the parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PropertyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyAttribute"/> class.
        /// </summary>
        public PropertyAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyAttribute"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public PropertyAttribute(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Specifies the key under which to store the value on the HttpRequestMessage.Properties dictionary.
        /// </summary>
        public string? Key { get; }
    }

    /// <summary>
    /// Add the Authorize header to the request with the value of the associated parameter.
    /// </summary>
    /// <remarks>
    /// Default authorization scheme: Bearer
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class.
    /// </remarks>
    /// <param name="scheme">The scheme.</param>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class AuthorizeAttribute(string scheme = "Bearer") : Attribute
    {
        /// <summary>
        /// Gets the scheme.
        /// </summary>
        /// <value>
        /// The scheme.
        /// </value>
        public string Scheme { get; } = scheme;
    }

    /// <summary>
    /// Associated value will be added to the request Uri as query-string, using a delimiter to split the values. (default: '.')
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)] // Property is to allow for form url encoded data
    public class QueryAttribute : Attribute
    {
        CollectionFormat? collectionFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
        /// </summary>
        public QueryAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
        /// </summary>
        /// <param name="delimiter">The delimiter.</param>
        public QueryAttribute(string delimiter)
        {
            Delimiter = delimiter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
        /// </summary>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="prefix">The prefix.</param>
        public QueryAttribute(string delimiter, string prefix)
        {
            Delimiter = delimiter;
            Prefix = prefix;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
        /// </summary>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="format">The format.</param>
        public QueryAttribute(string delimiter, string prefix, string format)
        {
            Delimiter = delimiter;
            Prefix = prefix;
            Format = format;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
        /// </summary>
        /// <param name="collectionFormat">The collection format.</param>
        public QueryAttribute(CollectionFormat collectionFormat)
        {
            CollectionFormat = collectionFormat;
        }

        /// <summary>
        /// Used to specify that the value should be treated as a string.
        /// Set to true if you want to call ToString() on the object before adding it to the query string.
        /// </summary>
        public bool TreatAsString { get; set; }

        /// <summary>
        /// Used to customize the name of either the query parameter pair or of the form field when form encoding.
        /// </summary>
        /// <seealso cref="Prefix"/>
        public string Delimiter { get; protected set; } = ".";

        /// <summary>
        /// Used to customize the name of the encoded value.
        /// </summary>
        /// <remarks>
        /// Gets combined with <see cref="Delimiter"/> in the format <code>var name = $"{Prefix}{Delimiter}{originalFieldName}"</code>
        /// where <c>originalFieldName</c> is the name of the object property or method parameter.
        /// </remarks>
        /// <example>
        /// <code>
        /// class Form
        /// {
        ///   [Query("-", "dontlog")]
        ///   public string password { get; }
        /// }
        /// </code>
        /// will result in the encoded form having a field named <c>dontlog-password</c>.
        /// </example>
        public string? Prefix { get; protected set; }

#pragma warning disable CA1019 // Define accessors for attribute arguments

        /// <summary>
        /// Used to customize the formatting of the encoded value.
        /// </summary>
        /// <example>
        /// <code>
        /// interface IServerApi
        /// {
        ///   [Get("/expenses")]
        ///   Task addExpense([Query(Format="0.00")] double expense);
        /// }
        /// </code>
        /// Calling <c>serverApi.addExpense(5)</c> will result in a URI of <c>{baseUri}/expenses?expense=5.00</c>.
        /// </example>
        public string? Format { get; set; }

        /// <summary>
        /// Specifies how the collection should be encoded.
        /// </summary>
        public CollectionFormat CollectionFormat
        {
            // Cannot make property nullable due to Attribute restrictions
            get => collectionFormat.GetValueOrDefault();
            set => collectionFormat = value;
        }

#pragma warning restore CA1019 // Define accessors for attribute arguments

        /// <summary>
        /// Gets a value indicating whether this instance is collection format specified.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is collection format specified; otherwise, <c>false</c>.
        /// </value>
        public bool IsCollectionFormatSpecified => collectionFormat.HasValue;
    }

    /// <summary>
    /// QueryUriFormatAttribute.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    /// <remarks>
    /// Initializes a new instance of the <see cref="QueryUriFormatAttribute"/> class.
    /// </remarks>
    /// <param name="uriFormat">The URI format.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class QueryUriFormatAttribute(UriFormat uriFormat) : Attribute
    {
        /// <summary>
        /// Specifies how the Query Params should be encoded.
        /// </summary>
        public UriFormat UriFormat { get; } = uriFormat;
    }
#pragma warning restore CA1813 // Avoid unsealed attributes
}
