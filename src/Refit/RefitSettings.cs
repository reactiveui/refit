// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Refit;

/// <summary>Defines various parameters on how Refit should work.</summary>
[SuppressMessage(
    "Reliability",
    "SST2403:'this' escapes from a constructor before the object is fully built",
    Justification = "The default exception-factory delegate captures the settings instance but is only invoked after construction completes.")]
public class RefitSettings
{
    /// <summary>Initializes a new instance of the <see cref="RefitSettings"/> class.</summary>
#if NET8_0_OR_GREATER
#endif
    public RefitSettings()
    {
        ContentSerializer = new SystemTextJsonContentSerializer();
        UrlParameterKeyFormatter = new DefaultUrlParameterKeyFormatter();
        UrlParameterFormatter = new DefaultUrlParameterFormatter();
        FormUrlEncodedParameterFormatter = new DefaultFormUrlEncodedParameterFormatter();
        ExceptionFactory = new DefaultApiExceptionFactory(this).CreateAsync;
        TransportExceptionFactory = DefaultTransportExceptionFactory();
    }

    /// <summary>Initializes a new instance of the <see cref="RefitSettings"/> class.</summary>
    /// <param name="contentSerializer">The <see cref="IHttpContentSerializer"/> instance to use.</param>
    public RefitSettings(IHttpContentSerializer contentSerializer)
        : this(contentSerializer, null, null, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RefitSettings"/> class.</summary>
    /// <param name="contentSerializer">The <see cref="IHttpContentSerializer"/> instance to use.</param>
    /// <param name="urlParameterFormatter">The <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>).</param>
    public RefitSettings(
        IHttpContentSerializer contentSerializer,
        IUrlParameterFormatter? urlParameterFormatter)
        : this(contentSerializer, urlParameterFormatter, null, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RefitSettings"/> class.</summary>
    /// <param name="contentSerializer">The <see cref="IHttpContentSerializer"/> instance to use.</param>
    /// <param name="urlParameterFormatter">The <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>).</param>
    /// <param name="formUrlEncodedParameterFormatter">The <see cref="IFormUrlEncodedParameterFormatter"/> instance to use (defaults to <see cref="DefaultFormUrlEncodedParameterFormatter"/>).</param>
    public RefitSettings(
        IHttpContentSerializer contentSerializer,
        IUrlParameterFormatter? urlParameterFormatter,
        IFormUrlEncodedParameterFormatter? formUrlEncodedParameterFormatter)
        : this(contentSerializer, urlParameterFormatter, formUrlEncodedParameterFormatter, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RefitSettings"/> class.</summary>
    /// <param name="contentSerializer">The <see cref="IHttpContentSerializer"/> instance to use.</param>
    /// <param name="urlParameterFormatter">The <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>).</param>
    /// <param name="formUrlEncodedParameterFormatter">The <see cref="IFormUrlEncodedParameterFormatter"/> instance to use (defaults to <see cref="DefaultFormUrlEncodedParameterFormatter"/>).</param>
    /// <param name="urlParameterKeyFormatter">The <see cref="IUrlParameterKeyFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterKeyFormatter"/>).</param>
    public RefitSettings(
        IHttpContentSerializer contentSerializer,
        IUrlParameterFormatter? urlParameterFormatter,
        IFormUrlEncodedParameterFormatter? formUrlEncodedParameterFormatter,
        IUrlParameterKeyFormatter? urlParameterKeyFormatter)
    {
        ContentSerializer =
            contentSerializer
            ?? throw new ArgumentNullException(
                nameof(contentSerializer),
                "The content serializer can't be null");
        UrlParameterFormatter = urlParameterFormatter ?? new DefaultUrlParameterFormatter();
        FormUrlEncodedParameterFormatter =
            formUrlEncodedParameterFormatter ?? new DefaultFormUrlEncodedParameterFormatter();
        UrlParameterKeyFormatter =
            urlParameterKeyFormatter ?? new DefaultUrlParameterKeyFormatter();
        ExceptionFactory = new DefaultApiExceptionFactory(this).CreateAsync;
        TransportExceptionFactory = DefaultTransportExceptionFactory();
    }

    /// <summary>Gets or sets a function to provide the Authorization header. Does not work if you supply an HttpClient instance.</summary>
    public Func<
        HttpRequestMessage,
        CancellationToken,
        ValueTask<string>
    >? AuthorizationHeaderValueGetter
    { get; set; }

    /// <summary>Gets or sets a custom inner HttpMessageHandler. Does not work if you supply an HttpClient instance.</summary>
    public Func<HttpMessageHandler>? HttpMessageHandlerFactory { get; set; }

    /// <summary>Gets or sets a function to provide <see cref="Exception"/> based on <see cref="HttpResponseMessage"/>. If function returns null - no exception is thrown.</summary>
    public Func<HttpResponseMessage, ValueTask<Exception?>> ExceptionFactory { get; set; }

    /// <summary>
    /// Gets or sets a function to provide <see cref="Exception"/> when deserialization exception is encountered.
    /// If function returns null - no exception is thrown.
    /// </summary>
    public Func<HttpResponseMessage, Exception, ValueTask<Exception?>>? DeserializationExceptionFactory { get; set; }

    /// <summary>Gets or sets how requests' content should be serialized. (defaults to <see cref="SystemTextJsonContentSerializer"/>).</summary>
    public IHttpContentSerializer ContentSerializer { get; set; }

    /// <summary>Gets the return-type adapters the opt-in reflection request builder uses to surface custom return
    /// shapes (for example <c>IObservable&lt;T&gt;</c> or <c>Result&lt;T&gt;</c>) from interface methods.</summary>
    /// <remarks>
    /// Each entry is a type implementing <see cref="IReturnTypeAdapter{TReturn, TResult}"/>: either a closed type,
    /// or an open generic definition whose single type parameter is the wrapped result type. The source generator
    /// does not consult this collection — it discovers adapters at compile time — so this only affects
    /// <see cref="RestService.For{T}(HttpClient, RefitSettings)"/> and other reflection-based builds.
    /// </remarks>
    public IList<Type> ReturnTypeAdapters { get; } = [];

    /// <summary>
    /// Gets or sets the <see cref="IUrlParameterKeyFormatter"/> instance to use for formatting URL parameter keys
    /// (defaults to <see cref="DefaultUrlParameterKeyFormatter" />). Allows customization of key naming conventions.
    /// </summary>
    public IUrlParameterKeyFormatter UrlParameterKeyFormatter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a query object's flattened property keys honor the content serializer's
    /// property name (for example <c>[JsonPropertyName]</c> with the default System.Text.Json serializer, or
    /// <c>[JsonProperty]</c> with Refit.Newtonsoft.Json), matching how form-encoded field names are resolved
    /// (defaults to <see langword="true"/>).
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, flattened query keys use only <c>[AliasAs]</c> and the
    /// <see cref="UrlParameterKeyFormatter"/> over the CLR property name, as in Refit 13 and earlier. <c>[AliasAs]</c>
    /// always takes precedence regardless of this setting.
    /// </remarks>
    public bool HonorContentSerializerPropertyNamesInQuery { get; set; } = true;

    /// <summary>Gets or sets the <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>).</summary>
    public IUrlParameterFormatter UrlParameterFormatter { get; set; }

    /// <summary>Gets a registry of <see cref="IUrlParameterFormatter"/> instances keyed by the CLR type they format,
    /// consulted before <see cref="UrlParameterFormatter"/> when rendering a value into a path or query string.</summary>
    /// <remarks>
    /// When a value is rendered into a URL (a path parameter, a round-trip path segment, or a query value), its runtime
    /// type (<see cref="object.GetType"/>) is looked up here first; a registered formatter is used for that value, and
    /// any other type falls back to <see cref="UrlParameterFormatter"/>. Matching is by exact runtime type only - no base
    /// class or interface walking - so register the concrete type the value will have at runtime. Both the reflection and
    /// source-generated request builders consult this registry identically. It does not affect header or body
    /// serialization. Registering any entry opts a request out of the generator's inline formatting fast path.
    /// </remarks>
    public IDictionary<Type, IUrlParameterFormatter> UrlParameterFormatterMap { get; } =
        new Dictionary<Type, IUrlParameterFormatter>();

    /// <summary>Gets or sets the <see cref="IFormUrlEncodedParameterFormatter"/> instance to use (defaults to <see cref="DefaultFormUrlEncodedParameterFormatter"/>).</summary>
    public IFormUrlEncodedParameterFormatter FormUrlEncodedParameterFormatter { get; set; }

    /// <summary>Gets or sets the default collection format to use. (defaults to <see cref="CollectionFormat.RefitParameterFormatter"/>).</summary>
    public CollectionFormat CollectionFormat { get; set; } =
        CollectionFormat.RefitParameterFormatter;

    /// <summary>
    /// Gets or sets a value indicating whether the request's body content is buffered before sending.
    /// (defaults to false, request body is not streamed to the server).
    /// </summary>
    public bool Buffered { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the request body is captured as a string before sending so it can be
    /// read from <see cref="ApiExceptionBase.RequestContent"/> when a request fails (defaults to false).
    /// </summary>
    /// <remarks>
    /// By default <see cref="HttpClient"/> disposes the request content once the request is sent, so the body cannot
    /// be read back from the exception (see issue #1189). Enabling this buffers the body into memory before sending;
    /// avoid it for large or streamed uploads.
    /// <para>
    /// Security note: the captured body frequently contains credentials or PII (for example login, token-refresh, or
    /// payment payloads) and is exposed as a public property on the thrown exception. Use
    /// <see cref="ExceptionRedactor"/> to scrub it before the exception reaches a logging or telemetry pipeline that
    /// serializes exception objects.
    /// </para>
    /// </remarks>
    public bool CaptureRequestContent { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of characters of an error response body that are read into
    /// <see cref="ApiException.Content"/> (defaults to <see langword="null"/>, meaning unbounded).
    /// </summary>
    /// <remarks>
    /// Error responses are read with <see cref="HttpCompletionOption.ResponseHeadersRead"/>, so
    /// <see cref="HttpClient.MaxResponseContentBufferSize"/> does not apply. A hostile or oversized error body can
    /// therefore drive unbounded memory allocation while constructing the <see cref="ApiException"/>. Set a limit to
    /// bound that read; the body is truncated to the first <c>value</c> characters.
    /// </remarks>
    public int? MaxExceptionContentLength { get; set; }

    /// <summary>
    /// Gets or sets a hook invoked just before an <see cref="ApiExceptionBase"/> is returned, allowing sensitive data
    /// to be scrubbed before the exception propagates (defaults to <see langword="null"/>).
    /// </summary>
    /// <remarks>
    /// An <see cref="ApiException"/> retains the live request (including the <c>Authorization</c> header and any secret
    /// <c>[Header]</c> values), the optionally captured request body, and the raw response headers (including
    /// <c>Set-Cookie</c>) and body. Logging and telemetry libraries that serialize exceptions by walking properties
    /// will capture all of it. Use this hook to remove or mask those values, for example:
    /// <code>
    /// settings.ExceptionRedactor = ex =>
    /// {
    ///     ex.RequestMessage.Headers.Authorization = null;
    ///     if (ex is ApiException api) api.Content = null;
    /// };
    /// </code>
    /// </remarks>
    public Action<ApiExceptionBase>? ExceptionRedactor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a route placeholder with no matching method argument is allowed.
    /// When false (the default) Refit throws while building the method; when true the unmatched <c>{token}</c> is
    /// left in the URL verbatim so it can be rewritten later, for example inside a <see cref="DelegatingHandler"/>.
    /// </summary>
    public bool AllowUnmatchedRouteParameters { get; set; }

    /// <summary>
    /// Gets or sets how the client's base address is combined with a method's relative URL
    /// (defaults to <see cref="UrlResolutionMode.RefitLegacy"/>). Set to <see cref="UrlResolutionMode.Rfc3986"/>
    /// to opt in to RFC 3986 / <see cref="System.Net.Http.HttpClient"/> style resolution.
    /// </summary>
    public UrlResolutionMode UrlResolution { get; set; } = UrlResolutionMode.RefitLegacy;

    /// <summary>
    /// Gets or sets how JSON request bodies are serialized (defaults to <see cref="RequestBodySerializationMode.Default"/>).
    /// <see cref="RequestBodySerializationMode.Buffered"/> and <see cref="RequestBodySerializationMode.Streamed"/> use
    /// the synchronous serialization path so the System.Text.Json source-generated fast-path can engage; both require
    /// the configured <see cref="ContentSerializer"/> to implement <see cref="ISynchronousContentSerializer"/>.
    /// </summary>
    public RequestBodySerializationMode RequestBodySerialization { get; set; } = RequestBodySerializationMode.Default;

    /// <summary>Gets optional Key-Value pairs, which are displayed in the property <see cref="HttpRequestMessage.Properties"/>.</summary>
    public Dictionary<string, object>? HttpRequestMessageOptions { get; init; }

    /// <summary>
    /// Gets or sets a factory invoked when <see cref="HttpClient.SendAsync(HttpRequestMessage)"/> throws a
    /// transport-level exception, giving callers full control over the exception that is ultimately surfaced.
    /// </summary>
    /// <remarks>
    /// The factory receives the <see cref="HttpRequestMessage"/>, the raw transport exception, and the
    /// <see cref="CancellationToken"/> that was passed to the call, and returns the exception to surface.
    /// <para>
    /// By default, an <see cref="OperationCanceledException"/> is returned unchanged when
    /// <see cref="CancellationToken.IsCancellationRequested"/> is <see langword="true"/> (so a cancelled
    /// <see cref="ApiResponse{T}"/> call throws instead of populating <c>Error</c>); every other exception
    /// is wrapped in an <see cref="ApiRequestException"/> that carries the full request context.
    /// </para>
    /// </remarks>
    public Func<HttpRequestMessage, Exception, CancellationToken, Exception> TransportExceptionFactory { get; set; }

#if NET6_0_OR_GREATER

    /// <summary>Gets or sets the version.</summary>
    /// <value>
    /// The version.
    /// </value>
    public Version Version { get; set; } = System.Net.HttpVersion.Version11;

    /// <summary>Gets or sets the version policy.</summary>
    /// <value>
    /// The version policy.
    /// </value>
    public HttpVersionPolicy VersionPolicy { get; set; } =
        HttpVersionPolicy.RequestVersionOrLower;
#endif

    /// <summary>Creates settings whose query keys, form-url-encoded keys, and JSON body property names are all formatted in camelCase.</summary>
    /// <returns>A new <see cref="RefitSettings"/> instance configured for camelCase naming.</returns>
    public static RefitSettings CamelCase() =>
        ForNamingConvention(JsonNamingPolicy.CamelCase, new CamelCaseUrlParameterKeyFormatter());

    /// <summary>Creates settings whose query keys, form-url-encoded keys, and JSON body property names are all formatted in snake_case.</summary>
    /// <returns>A new <see cref="RefitSettings"/> instance configured for snake_case naming.</returns>
    public static RefitSettings SnakeCase() =>
        ForNamingConvention(SeparatedCaseJsonNamingPolicy.Snake, new SnakeCaseUrlParameterKeyFormatter());

    /// <summary>Creates settings whose query keys, form-url-encoded keys, and JSON body property names are all formatted in kebab-case.</summary>
    /// <returns>A new <see cref="RefitSettings"/> instance configured for kebab-case naming.</returns>
    public static RefitSettings KebabCase() =>
        ForNamingConvention(SeparatedCaseJsonNamingPolicy.Kebab, new KebabCaseUrlParameterKeyFormatter());

    /// <summary>Builds settings that apply the given JSON naming policy and URL parameter key formatter consistently.</summary>
    /// <param name="jsonNamingPolicy">The naming policy applied to JSON body property names.</param>
    /// <param name="urlParameterKeyFormatter">The formatter applied to query and form-url-encoded keys.</param>
    /// <returns>The configured settings.</returns>
    private static RefitSettings ForNamingConvention(
        JsonNamingPolicy jsonNamingPolicy,
        IUrlParameterKeyFormatter urlParameterKeyFormatter)
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        options.PropertyNamingPolicy = jsonNamingPolicy;
        return new(new SystemTextJsonContentSerializer(options))
        {
            UrlParameterKeyFormatter = urlParameterKeyFormatter,
        };
    }

    /// <summary>Returns the default <see cref="TransportExceptionFactory"/> delegate for this instance.</summary>
    /// <returns>
    /// A factory that passes an <see cref="OperationCanceledException"/> through unchanged when
    /// <see cref="CancellationToken.IsCancellationRequested"/> is <see langword="true"/>, and wraps every
    /// other exception in an <see cref="ApiRequestException"/> that captures the request and these settings.
    /// </returns>
    private Func<HttpRequestMessage, Exception, CancellationToken, Exception> DefaultTransportExceptionFactory()
        => (req, ex, ct) =>
             ex is OperationCanceledException && ct.IsCancellationRequested ? ex : new ApiRequestException(req, req.Method, this, ex);
}
