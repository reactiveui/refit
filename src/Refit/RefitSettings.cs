// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit;

/// <summary>Defines various parameters on how Refit should work.</summary>
[System.Diagnostics.DebuggerDisplay("RefitSettings: AuthorizationHeaderValueGetter = {AuthorizationHeaderValueGetter}")]
[SuppressMessage(
    "Reliability",
    "SST2403:'this' escapes from a constructor before the object is fully built",
    Justification = "The default exception-factory delegate captures the settings instance but is only invoked after construction completes.")]
public class RefitSettings
{
    /// <summary>Initializes a new instance of the <see cref="RefitSettings"/> class.</summary>
    public RefitSettings() => Exceptions = new(this);

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
    /// <exception cref="ArgumentNullException"><paramref name="contentSerializer"/> is <see langword="null"/>.</exception>
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
        Exceptions = new(this);
    }

    /// <summary>Gets the request content serialization and compression settings.</summary>
    public RefitSerializationSettings Serialization { get; } = new();

    /// <summary>Gets the URL formatting and resolution settings.</summary>
    public RefitUrlSettings Url { get; } = new();

    /// <summary>Gets the exception creation, capture and redaction settings.</summary>
    public RefitExceptionsSettings Exceptions { get; }

    /// <summary>Gets the request metadata, headers and transport settings.</summary>
    public RefitRequestsSettings Requests { get; } = new();

    /// <summary>Gets or sets <see cref="RefitSerializationSettings.ContentSerializer"/>.</summary>
    public IHttpContentSerializer ContentSerializer
    {
        get => Serialization.ContentSerializer;
        set => Serialization.ContentSerializer = value;
    }

    /// <summary>Gets or sets <see cref="RefitSerializationSettings.FormUrlEncodedParameterFormatter"/>.</summary>
    public IFormUrlEncodedParameterFormatter FormUrlEncodedParameterFormatter
    {
        get => Serialization.FormUrlEncodedParameterFormatter;
        set => Serialization.FormUrlEncodedParameterFormatter = value;
    }

    /// <summary>Gets or sets <see cref="RefitSerializationSettings.Buffered"/>.</summary>
    public bool Buffered
    {
        get => Serialization.Buffered;
        set => Serialization.Buffered = value;
    }

    /// <summary>Gets or sets <see cref="RefitSerializationSettings.RequestBodySerialization"/>.</summary>
    public RequestBodySerializationMode RequestBodySerialization
    {
        get => Serialization.RequestBodySerialization;
        set => Serialization.RequestBodySerialization = value;
    }

    /// <summary>Gets or sets <see cref="RefitSerializationSettings.RequestCompression"/>.</summary>
    public RequestCompression RequestCompression
    {
        get => Serialization.RequestCompression;
        set => Serialization.RequestCompression = value;
    }

    /// <summary>Gets or sets <see cref="RefitSerializationSettings.RequestCompressionLevel"/>.</summary>
    public System.IO.Compression.CompressionLevel RequestCompressionLevel
    {
        get => Serialization.RequestCompressionLevel;
        set => Serialization.RequestCompressionLevel = value;
    }

#if NET9_0_OR_GREATER
    /// <summary>Gets or sets <see cref="RefitSerializationSettings.RequestCompressionOptions"/>.</summary>
    public RequestCompressionOptions? RequestCompressionOptions
    {
        get => Serialization.RequestCompressionOptions;
        set => Serialization.RequestCompressionOptions = value;
    }

#endif

    /// <summary>Gets or sets <see cref="RefitUrlSettings.UrlParameterKeyFormatter"/>.</summary>
    public IUrlParameterKeyFormatter UrlParameterKeyFormatter
    {
        get => Url.UrlParameterKeyFormatter;
        set => Url.UrlParameterKeyFormatter = value;
    }

    /// <summary>Gets or sets <see cref="RefitUrlSettings.HonorContentSerializerPropertyNamesInQuery"/>.</summary>
    public bool HonorContentSerializerPropertyNamesInQuery
    {
        get => Url.HonorContentSerializerPropertyNamesInQuery;
        set => Url.HonorContentSerializerPropertyNamesInQuery = value;
    }

    /// <summary>Gets or sets <see cref="RefitUrlSettings.UrlParameterFormatter"/>.</summary>
    public IUrlParameterFormatter UrlParameterFormatter
    {
        get => Url.UrlParameterFormatter;
        set => Url.UrlParameterFormatter = value;
    }

    /// <summary>Gets <see cref="RefitUrlSettings.UrlParameterFormatterMap"/>.</summary>
    public IDictionary<Type, IUrlParameterFormatter> UrlParameterFormatterMap => Url.UrlParameterFormatterMap;

    /// <summary>Gets or sets <see cref="RefitUrlSettings.CollectionFormat"/>.</summary>
    public CollectionFormat CollectionFormat
    {
        get => Url.CollectionFormat;
        set => Url.CollectionFormat = value;
    }

    /// <summary>Gets or sets <see cref="RefitUrlSettings.AllowUnmatchedRouteParameters"/>.</summary>
    public bool AllowUnmatchedRouteParameters
    {
        get => Url.AllowUnmatchedRouteParameters;
        set => Url.AllowUnmatchedRouteParameters = value;
    }

    /// <summary>Gets or sets <see cref="RefitUrlSettings.UrlResolution"/>.</summary>
    public UrlResolutionMode UrlResolution
    {
        get => Url.UrlResolution;
        set => Url.UrlResolution = value;
    }

    /// <summary>Gets or sets <see cref="RefitExceptionsSettings.ExceptionFactory"/>.</summary>
    public Func<HttpResponseMessage, ValueTask<Exception?>> ExceptionFactory
    {
        get => Exceptions.ExceptionFactory;
        set => Exceptions.ExceptionFactory = value;
    }

    /// <summary>Gets or sets <see cref="RefitExceptionsSettings.DeserializationExceptionFactory"/>.</summary>
    public Func<HttpResponseMessage, Exception, ValueTask<Exception?>>? DeserializationExceptionFactory
    {
        get => Exceptions.DeserializationExceptionFactory;
        set => Exceptions.DeserializationExceptionFactory = value;
    }

    /// <summary>Gets or sets <see cref="RefitExceptionsSettings.CaptureRequestContent"/>.</summary>
    public bool CaptureRequestContent
    {
        get => Exceptions.CaptureRequestContent;
        set => Exceptions.CaptureRequestContent = value;
    }

    /// <summary>Gets or sets <see cref="RefitExceptionsSettings.MaxExceptionContentLength"/>.</summary>
    public int? MaxExceptionContentLength
    {
        get => Exceptions.MaxExceptionContentLength;
        set => Exceptions.MaxExceptionContentLength = value;
    }

    /// <summary>Gets or sets <see cref="RefitExceptionsSettings.ExceptionRedactor"/>.</summary>
    public Action<ApiExceptionBase>? ExceptionRedactor
    {
        get => Exceptions.ExceptionRedactor;
        set => Exceptions.ExceptionRedactor = value;
    }

    /// <summary>Gets or sets <see cref="RefitExceptionsSettings.TransportExceptionFactory"/>.</summary>
    public Func<HttpRequestMessage, Exception, CancellationToken, Exception> TransportExceptionFactory
    {
        get => Exceptions.TransportExceptionFactory;
        set => Exceptions.TransportExceptionFactory = value;
    }

    /// <summary>Gets or sets <see cref="RefitRequestsSettings.AuthorizationHeaderValueGetter"/>.</summary>
    public Func<
        HttpRequestMessage,
        CancellationToken,
        ValueTask<string>
    >? AuthorizationHeaderValueGetter
    {
        get => Requests.AuthorizationHeaderValueGetter;
        set => Requests.AuthorizationHeaderValueGetter = value;
    }

    /// <summary>Gets or sets <see cref="RefitRequestsSettings.HttpMessageHandlerFactory"/>.</summary>
    public Func<HttpMessageHandler>? HttpMessageHandlerFactory
    {
        get => Requests.HttpMessageHandlerFactory;
        set => Requests.HttpMessageHandlerFactory = value;
    }

    /// <summary>Gets <see cref="RefitRequestsSettings.ReturnTypeAdapters"/>.</summary>
    public IList<Type> ReturnTypeAdapters => Requests.ReturnTypeAdapters;

    /// <summary>Gets or sets <see cref="RefitRequestsSettings.CaptureMethodArguments"/>.</summary>
    public bool CaptureMethodArguments
    {
        get => Requests.CaptureMethodArguments;
        set => Requests.CaptureMethodArguments = value;
    }

    /// <summary>Gets or sets <see cref="RefitRequestsSettings.ValidateHeaders"/>.</summary>
    public bool ValidateHeaders
    {
        get => Requests.ValidateHeaders;
        set => Requests.ValidateHeaders = value;
    }

    /// <summary>Gets or initializes <see cref="RefitRequestsSettings.HttpRequestMessageOptions"/>.</summary>
    public Dictionary<string, object>? HttpRequestMessageOptions
    {
        get => Requests.HttpRequestMessageOptions;
        init => Requests.InitializeRequestOptions(value);
    }

#if NET6_0_OR_GREATER
    /// <summary>Gets or sets <see cref="RefitRequestsSettings.Version"/>.</summary>
    public Version Version
    {
        get => Requests.Version;
        set => Requests.Version = value;
    }

#endif
#if NET6_0_OR_GREATER
    /// <summary>Gets or sets <see cref="RefitRequestsSettings.VersionPolicy"/>.</summary>
    public HttpVersionPolicy VersionPolicy
    {
        get => Requests.VersionPolicy;
        set => Requests.VersionPolicy = value;
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>Creates settings that run on the options of a source-generated context, with reflection-based JSON disabled.</summary>
    /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
    /// <returns>New settings whose content serializer is <see cref="SystemTextJsonContentSerializer.ForContext(JsonSerializerContext)"/>.</returns>
    public static RefitSettings ForJsonContext(JsonSerializerContext context) =>
        new(SystemTextJsonContentSerializer.ForContext(context));

    /// <summary>Creates settings that run on the options of a source-generated context, optionally with a reflection fallback.</summary>
    /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
    /// <param name="allowReflectionFallback"><see langword="true"/> to let a type the context does not describe use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
    /// <returns>New settings whose content serializer is <see cref="SystemTextJsonContentSerializer.ForContext(JsonSerializerContext, bool)"/>.</returns>
    public static RefitSettings ForJsonContext(JsonSerializerContext context, bool allowReflectionFallback) =>
        new(SystemTextJsonContentSerializer.ForContext(context, allowReflectionFallback));

#endif

    /// <summary>Creates settings whose query keys, form-url-encoded keys, and JSON body property names are all formatted in camelCase.</summary>
    /// <returns>A new <see cref="RefitSettings"/> instance configured for camelCase naming.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefitSettings CamelCase() =>
        ForNamingConvention(JsonNamingPolicy.CamelCase, new CamelCaseUrlParameterKeyFormatter());

    /// <summary>Creates settings whose query keys, form-url-encoded keys, and JSON body property names are all formatted in snake_case.</summary>
    /// <returns>A new <see cref="RefitSettings"/> instance configured for snake_case naming.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefitSettings SnakeCase() =>
        ForNamingConvention(SeparatedCaseJsonNamingPolicy.Snake, new SnakeCaseUrlParameterKeyFormatter());

    /// <summary>Creates settings whose query keys, form-url-encoded keys, and JSON body property names are all formatted in kebab-case.</summary>
    /// <returns>A new <see cref="RefitSettings"/> instance configured for kebab-case naming.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefitSettings KebabCase() =>
        ForNamingConvention(SeparatedCaseJsonNamingPolicy.Kebab, new KebabCaseUrlParameterKeyFormatter());

#if NET8_0_OR_GREATER
    /// <summary>Adds a source-generated context to these settings' System.Text.Json serializer, with reflection-based JSON disabled.</summary>
    /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
    /// <returns>These settings.</returns>
    /// <exception cref="InvalidOperationException"><see cref="ContentSerializer"/> is not a <see cref="SystemTextJsonContentSerializer"/>.</exception>
    /// <remarks>
    /// The serializer's naming policy, converters and resolvers stay in place. Only <see cref="ContentSerializer"/> changes,
    /// to a serializer built on a copy of the current options.
    /// </remarks>
    public RefitSettings UseJsonContext(JsonSerializerContext context) =>
        UseJsonContext(context, false);

    /// <summary>Adds a source-generated context to these settings' System.Text.Json serializer, optionally with a reflection fallback.</summary>
    /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
    /// <param name="allowReflectionFallback"><see langword="true"/> to let a type no resolver describes use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
    /// <returns>These settings.</returns>
    /// <exception cref="InvalidOperationException"><see cref="ContentSerializer"/> is not a <see cref="SystemTextJsonContentSerializer"/>.</exception>
    public RefitSettings UseJsonContext(JsonSerializerContext context, bool allowReflectionFallback)
    {
        ArgumentExceptionHelper.ThrowIfNull(context);

        ContentSerializer = ContentSerializer is SystemTextJsonContentSerializer serializer
            ? serializer.WithContext(context, allowReflectionFallback)
            : throw new InvalidOperationException(
                "A JSON serializer context can only be added to settings that use SystemTextJsonContentSerializer.");

        return this;
    }

#endif

    /// <summary>Builds settings that apply the given JSON naming policy and URL parameter key formatter consistently.</summary>
    /// <param name="jsonNamingPolicy">The naming policy applied to JSON body property names.</param>
    /// <param name="urlParameterKeyFormatter">The formatter applied to query and form-url-encoded keys.</param>
    /// <returns>The configured settings.</returns>
    internal static RefitSettings ForNamingConvention(
        JsonNamingPolicy jsonNamingPolicy,
        IUrlParameterKeyFormatter urlParameterKeyFormatter)
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        options.PropertyNamingPolicy = jsonNamingPolicy;
        return new(new SystemTextJsonContentSerializer(options)) { UrlParameterKeyFormatter = urlParameterKeyFormatter, };
    }

    /// <summary>Returns the default <see cref="TransportExceptionFactory"/> delegate for this instance.</summary>
    /// <returns>
    /// A factory that passes an <see cref="OperationCanceledException"/> through unchanged when
    /// <see cref="CancellationToken.IsCancellationRequested"/> is <see langword="true"/>, and wraps every
    /// other exception in an <see cref="ApiRequestException"/> that captures the request and these settings.
    /// </returns>
    internal Func<HttpRequestMessage, Exception, CancellationToken, Exception> DefaultTransportExceptionFactory() =>
        (req, ex, ct) =>
            ex is OperationCanceledException && ct.IsCancellationRequested ? ex : new ApiRequestException(req, req.Method, this, ex);
}
