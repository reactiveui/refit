using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Refit
{
    static class ApiResponse
    {
        internal static T Create<T, TBody>(
            HttpRequestMessage request,
            HttpResponseMessage? resp,
            object? content,
            RefitSettings settings,
            ApiExceptionBase? error = null
        )
        {
            return (T)
                Activator.CreateInstance(
                    typeof(ApiResponse<TBody>),
                    request,
                    resp,
                    content,
                    settings,
                    error
                )!;
        }
    }

    /// <summary>
    /// Implementation of <see cref="IApiResponse{T}"/> that provides additional functionalities.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    /// Create an instance of <see cref="ApiResponse{T}"/> with type <typeparamref name="T"/>.
    /// </remarks>
    /// <param name="request">Original HTTP Request.</param>
    /// <param name="response">Original HTTP Response message.</param>
    /// <param name="content">Response content.</param>
    /// <param name="settings">Refit settings used to send the request.</param>
    /// <param name="error">The exception, if the request failed.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public sealed class ApiResponse<T>(
        HttpRequestMessage request,
        HttpResponseMessage? response,
        T? content,
        RefitSettings settings,
        ApiExceptionBase? error = null
        ) : IApiResponse<T>
    {
        bool disposed;

        /// <summary>
        /// Deserialized request content as <typeparamref name="T"/>.
        /// </summary>
        public T? Content { get; } = content;

        /// <summary>
        /// Refit settings used to send the request.
        /// </summary>
        public RefitSettings Settings { get; } = settings;

        /// <summary>
        /// HTTP response headers.
        /// </summary>
        public HttpResponseHeaders? Headers => response?.Headers;

        /// <summary>
        /// HTTP response content headers as defined in RFC 2616.
        /// </summary>
        public HttpContentHeaders? ContentHeaders => response?.Content?.Headers;

        /// <summary>
        /// Indicates whether the request was successful.
        /// </summary>
#if NET6_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(Headers))]
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(true, nameof(StatusCode))]
        [MemberNotNullWhen(true, nameof(Version))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        public bool IsSuccessStatusCode => response?.IsSuccessStatusCode ?? false;

        /// <summary>
        /// Indicates whether the request was successful and there wasn't any other error (for example, during content deserialization).
        /// </summary>
#if NET6_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(Headers))]
        [MemberNotNullWhen(true, nameof(Content))]
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(true, nameof(StatusCode))]
        [MemberNotNullWhen(true, nameof(Version))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        public bool IsSuccessful => IsSuccessStatusCode && Error is null;

        /// <inheritdoc />
#if NET6_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(Headers))]
        [MemberNotNullWhen(true, nameof(Content))]
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(true, nameof(StatusCode))]
        [MemberNotNullWhen(true, nameof(Version))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        public bool IsReceived => response != null;

        /// <summary>
        /// The reason phrase which typically is sent by the server together with the status code.
        /// </summary>
        public string? ReasonPhrase => response?.ReasonPhrase;

        /// <summary>
        /// The HTTP Request message which led to this response.
        /// </summary>
        public HttpRequestMessage RequestMessage => request;

        /// <summary>
        /// HTTP response status code.
        /// </summary>
        public HttpStatusCode? StatusCode => response?.StatusCode;

        /// <summary>
        /// HTTP Message version.
        /// </summary>
        public Version? Version => response?.Version;

        /// <summary>
        /// The <see cref="ApiExceptionBase" /> object in case of unsuccessful request or response.
        /// </summary>
        /// <remarks>
        /// The <see cref="HasRequestError"/> and <see cref="HasResponseError"/> methods can be
        /// used to check the type of error.
        /// </remarks>
        public ApiExceptionBase? Error { get; private set; } = error;

        /// <summary>
        /// Implementation of <see cref="IApiResponse{T}"/> that provides additional functionalities.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <remarks>
        /// Create an instance of <see cref="ApiResponse{T}"/> with type <typeparamref name="T"/>.
        /// </remarks>
        /// <param name="response">Original HTTP Response message.</param>
        /// <param name="content">Response content.</param>
        /// <param name="settings">Refit settings used to send the request.</param>
        /// <param name="error">The exception, if the request failed.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ApiResponse(
            HttpResponseMessage response,
            T? content,
            RefitSettings settings,
            ApiExceptionBase? error = null)
            : this(
            (response ?? throw new ArgumentNullException(nameof(response))).RequestMessage,
            response,
            content,
            settings,
            error) { }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Ensures the request was successful by throwing an exception in case of failure
        /// </summary>
        /// <returns>The current <see cref="ApiResponse{T}"/></returns>
        /// <exception cref="ApiException">Thrown when an unsuccessful response was received from the server.</exception>
        /// <exception cref="ApiRequestException">Thrown when the request failed before receiving a response from the server.</exception>
        public async Task<ApiResponse<T>> EnsureSuccessStatusCodeAsync()
        {
            if (!IsSuccessStatusCode)
            {
                await ThrowsApiExceptionAsync().ConfigureAwait(false);
            }

            return this;
        }

        /// <summary>
        /// Ensures the request was successful and without any other error by throwing an exception in case of failure
        /// </summary>
        /// <returns>The current <see cref="ApiResponse{T}"/></returns>
        /// <exception cref="ApiException">Thrown when an unsuccessful response was received from the server.</exception>
        /// <exception cref="ApiRequestException">Thrown when the request failed before receiving a response from the server.</exception>
        public async Task<ApiResponse<T>> EnsureSuccessfulAsync()
        {
            if (!IsSuccessful)
            {
                await ThrowsApiExceptionAsync().ConfigureAwait(false);
            }

            return this;
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Naming",
            "CA1716:Identifiers should not match keywords",
            Justification = "By Design"
        )]
        
        public bool HasRequestError(
#if NET6_0_OR_GREATER
            [MaybeNullWhen(false)]
#endif
            out ApiRequestException error)
        {
            error = Error as ApiRequestException;
            return error != null;
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Naming",
            "CA1716:Identifiers should not match keywords",
            Justification = "By Design"
        )]
        public bool HasResponseError(
#if NET6_0_OR_GREATER
            [MaybeNullWhen(false)]
#endif
            out ApiException error)
        {
            error = Error as ApiException;
            return error != null;
        }

        void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;

            disposed = true;

            response?.Dispose();
        }

        private async Task ThrowsApiExceptionAsync()
        {
            var exception =
                    Error
                    ?? await ApiException
                        .Create(
                            request,
                            request.Method,
                            response,
                            Settings
                        )
                        .ConfigureAwait(false);

            Dispose();

            throw exception;
        }
    }

    /// <inheritdoc/>
    public interface IApiResponse<out T> : IApiResponse
    {
#if NET6_0_OR_GREATER
        /// <summary>
        /// The exception object in case of unsuccessful request or response.
        /// </summary>
        /// <remarks>
        /// The <see cref="IApiResponse.HasRequestError"/> and <see cref="IApiResponse.HasResponseError"/> methods can be
        /// used to check the type of error.
        /// </remarks>
        [SuppressMessage(
            "Naming",
            "CA1716:Identifiers should not match keywords",
            Justification = "By Design"
        )]
        new ApiExceptionBase? Error { get; }

        /// <summary>
        /// HTTP response content headers as defined in RFC 2616.
        /// </summary>
        new HttpContentHeaders? ContentHeaders { get; }

        /// <summary>
        /// Indicates whether the request was successful.
        /// </summary>
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(false, nameof(Error))]
        new bool IsSuccessStatusCode { get; }

        /// <summary>
        /// Indicates whether the request was successful and there wasn't any other error (for example, during content deserialization).
        /// </summary>
        [MemberNotNullWhen(true, nameof(Content))]
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(false, nameof(Error))]
        new bool IsSuccessful { get; }
#endif

        /// <summary>
        /// Deserialized request content as <typeparamref name="T"/>.
        /// </summary>
        T? Content { get; }
    }

    /// <summary>
    /// Base interface used to represent an API response.
    /// </summary>
    public interface IApiResponse : IDisposable
    {
        /// <summary>
        /// HTTP response headers.
        /// </summary>
        HttpResponseHeaders? Headers { get; }

        /// <summary>
        /// HTTP response content headers as defined in RFC 2616.
        /// </summary>
        HttpContentHeaders? ContentHeaders { get; }

        /// <summary>
        /// Indicates whether the request was successful.
        /// </summary>
#if NET6_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(Headers))]
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(true, nameof(StatusCode))]
        [MemberNotNullWhen(true, nameof(Version))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        bool IsSuccessStatusCode { get; }

        /// <summary>
        /// Indicates whether the request was successful and there wasn't any other error (for example, during content deserialization).
        /// </summary>
#if NET6_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(Headers))]
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(true, nameof(StatusCode))]
        [MemberNotNullWhen(true, nameof(Version))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        bool IsSuccessful { get; }

        /// <summary>
        /// Indicates whether a response was received from the server.
        /// </summary>
#if NET6_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(Headers))]
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(true, nameof(StatusCode))]
        [MemberNotNullWhen(true, nameof(Version))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        bool IsReceived { get; }

        /// <summary>
        /// HTTP response status code.
        /// </summary>
        HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// The reason phrase which typically is sent by the server together with the status code.
        /// </summary>
        string? ReasonPhrase { get; }

        /// <summary>
        /// The HTTP Request message which led to this response.
        /// </summary>
        HttpRequestMessage? RequestMessage { get; }

        /// <summary>
        /// HTTP Message version.
        /// </summary>
        Version? Version { get; }

        /// <summary>
        /// The exception object in case of unsuccessful request or response.
        /// </summary>
        /// <remarks>
        /// <see cref="HasRequestError"/> and <see cref="HasResponseError"/> methods can be used to check the type of error.
        /// </remarks>
        [SuppressMessage(
           "Naming",
           "CA1716:Identifiers should not match keywords",
           Justification = "By Design"
       )]
        ApiExceptionBase? Error { get; }

        /// <summary>
        /// Checks if the call failed before a response was received from the server.
        /// </summary>
        /// <param name="error">The <see cref="ApiRequestException"/> object in case of an unsuccessful request.</param>
        /// <returns><c>true</c> if the call failed before a response was received from the server, otherwise <c>false</c>.</returns>
        [SuppressMessage(
            "Naming",
            "CA1716:Identifiers should not match keywords",
            Justification = "By Design"
        )]
        bool HasRequestError(
#if NET6_0_OR_GREATER
            [MaybeNullWhen(false)]
#endif
            out ApiRequestException error);

        /// <summary>
        /// Checks if the call failed due to an unsuccessful response from the server.
        /// </summary>
        /// <param name="error">The <see cref="ApiException"/> object in case of unsuccessful response.</param>
        /// <returns><c>true</c> if the call failed due to an unsuccessful response from the server, otherwise <c>false</c>.</returns>
        [SuppressMessage(
            "Naming",
            "CA1716:Identifiers should not match keywords",
            Justification = "By Design"
        )]
        bool HasResponseError(
#if NET6_0_OR_GREATER
            [MaybeNullWhen(false)]
#endif
            out ApiException error);
    }
}
