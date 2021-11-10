using System;
using System.Net.Http;

namespace Refit
{
    public abstract class HttpMethodAttribute : Attribute
    {
        public HttpMethodAttribute(string path)
        {
            Path = path;
        }

        public abstract HttpMethod Method { get; }

        public virtual string Path
        {
            get;
            protected set;
        }
    }

    /// <summary>
    /// Send the request with HTTP method 'GET'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class GetAttribute : HttpMethodAttribute
    {
        public GetAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return HttpMethod.Get; }
        }
    }

    /// <summary>
    /// Send the request with HTTP method 'POST'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PostAttribute : HttpMethodAttribute
    {
        public PostAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return HttpMethod.Post; }
        }
    }

    /// <summary>
    /// Send the request with HTTP method 'PUT'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PutAttribute : HttpMethodAttribute
    {
        public PutAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return HttpMethod.Put; }
        }
    }

    /// <summary>
    /// Send the request with HTTP method 'DELETE'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DeleteAttribute : HttpMethodAttribute
    {
        public DeleteAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return HttpMethod.Delete; }
        }
    }

    /// <summary>
    /// Send the request with HTTP method 'PATCH'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchAttribute : HttpMethodAttribute
    {
        public PatchAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return new HttpMethod("PATCH"); }
        }
    }

    /// <summary>
    /// Send the request with HTTP method 'OPTION'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OptionsAttribute : HttpMethodAttribute
    {
        public OptionsAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return new HttpMethod("OPTIONS"); }
        }
    }

    /// <summary>
    /// Send the request with HTTP method 'HEAD'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HeadAttribute : HttpMethodAttribute
    {
        public HeadAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return HttpMethod.Head; }
        }
    }

    /// <summary>
    /// Send the request as multipart.
    /// </summary>
    /// <remarks>
    /// Currently, multipart methods only support the following parameter types: <see cref="string"/>, <see cref="byte"/> array, <see cref="System.IO.Stream"/>, <see cref="System.IO.FileInfo"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class MultipartAttribute : Attribute
    {
        public string BoundaryText { get; private set; }

        public MultipartAttribute(string boundaryText = "----MyGreatBoundary")
        {
            BoundaryText = boundaryText;
        }

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
        public BodyAttribute()
        {

        }
        public BodyAttribute(bool buffered)
        {
            Buffered = buffered;
        }

        public BodyAttribute(BodySerializationMethod serializationMethod, bool buffered)
        {
            SerializationMethod = serializationMethod;
            Buffered = buffered;
        }

        public BodyAttribute(BodySerializationMethod serializationMethod = BodySerializationMethod.Default)
        {
            SerializationMethod = serializationMethod;
        }



        public bool? Buffered { get; set; }
        public BodySerializationMethod SerializationMethod { get; protected set; } = BodySerializationMethod.Default;
    }

    /// <summary>
    /// Override the key that will be sent in the query string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class AliasAsAttribute : Attribute
    {
        public AliasAsAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; protected set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    [Obsolete("Use Refit.StreamPart, Refit.ByteArrayPart, Refit.FileInfoPart or if necessary, inherit from Refit.MultipartItem", false)]
    public class AttachmentNameAttribute : Attribute
    {
        public AttachmentNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; protected set; }
    }

    /// <summary>
    /// Allows you to provide a Dictionary of headers to be added to the request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class HeaderCollectionAttribute : Attribute
    {

    }

    /// <summary>
    /// Add multiple headers to the request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class HeadersAttribute : Attribute
    {
        public HeadersAttribute(params string[] headers)
        {
            Headers = headers ?? Array.Empty<string>();
        }

        public string[] Headers { get; }
    }

    /// <summary>
    /// Add a header to the request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header)
        {
            Header = header;
        }

        public string Header { get; }
    }

    /// <summary>
    /// Used to store the value in HttpRequestMessage.Properties for further processing in a custom DelegatingHandler.
    /// If a string is supplied to the constructor then it will be used as the key in the HttpRequestMessage.Properties dictionary.
    /// If no key is specified then the key will be defaulted to the name of the parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PropertyAttribute : Attribute
    {
        public PropertyAttribute() { }

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
    [AttributeUsage(AttributeTargets.Parameter)]
    public class AuthorizeAttribute : Attribute
    {
        public AuthorizeAttribute(string scheme = "Bearer")
        {
            Scheme = scheme;
        }

        public string Scheme { get; }
    }

    /// <summary>
    /// Associated value will be added to the request Uri as query-string, using a delimiter to split the values. (default: '.')
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)] // Property is to allow for form url encoded data
    public class QueryAttribute : Attribute
    {
        CollectionFormat? collectionFormat;

        public QueryAttribute() { }

        public QueryAttribute(string delimiter)
        {
            Delimiter = delimiter;
        }

        public QueryAttribute(string delimiter, string prefix)
        {
            Delimiter = delimiter;
            Prefix = prefix;
        }

        public QueryAttribute(string delimiter, string prefix, string format)
        {
            Delimiter = delimiter;
            Prefix = prefix;
            Format = format;
        }

        public QueryAttribute(CollectionFormat collectionFormat)
        {
            CollectionFormat = collectionFormat;
        }

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

        public bool IsCollectionFormatSpecified => collectionFormat.HasValue;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class QueryUriFormatAttribute : Attribute
    {
        public QueryUriFormatAttribute(UriFormat uriFormat)
        {
            UriFormat = uriFormat;
        }

        /// <summary>
        /// Specifies how the Query Params should be encoded.
        /// </summary>
        public UriFormat UriFormat { get; }
    }
}
