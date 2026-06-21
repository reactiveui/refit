// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Defines various parameters on how Refit should work.</summary>
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
    }

    /// <summary>Gets or sets a function to provide the Authorization header. Does not work if you supply an HttpClient instance.</summary>
    public Func<
        HttpRequestMessage,
        CancellationToken,
        Task<string>
    >? AuthorizationHeaderValueGetter { get; set; }

    /// <summary>Gets or sets a custom inner HttpMessageHandler. Does not work if you supply an HttpClient instance.</summary>
    public Func<HttpMessageHandler>? HttpMessageHandlerFactory { get; set; }

    /// <summary>Gets or sets a function to provide <see cref="Exception"/> based on <see cref="HttpResponseMessage"/>. If function returns null - no exception is thrown.</summary>
    public Func<HttpResponseMessage, Task<Exception?>> ExceptionFactory { get; set; }

    /// <summary>
    /// Gets or sets a function to provide <see cref="Exception"/> when deserialization exception is encountered.
    /// If function returns null - no exception is thrown.
    /// </summary>
    public Func<HttpResponseMessage, Exception, Task<Exception?>>? DeserializationExceptionFactory { get; set; }

    /// <summary>Gets or sets how requests' content should be serialized. (defaults to <see cref="SystemTextJsonContentSerializer"/>).</summary>
    public IHttpContentSerializer ContentSerializer { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IUrlParameterKeyFormatter"/> instance to use for formatting URL parameter keys
    /// (defaults to <see cref="DefaultUrlParameterKeyFormatter" />). Allows customization of key naming conventions.
    /// </summary>
    public IUrlParameterKeyFormatter UrlParameterKeyFormatter { get; set; }

    /// <summary>Gets or sets the <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>).</summary>
    public IUrlParameterFormatter UrlParameterFormatter { get; set; }

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

    /// <summary>Gets optional Key-Value pairs, which are displayed in the property <see cref="HttpRequestMessage.Properties"/>.</summary>
    public Dictionary<string, object>? HttpRequestMessageOptions { get; init; }

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
}
