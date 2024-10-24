using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Refit
{
    static class ApiResponse
    {
        internal static T Create<T, TBody>(
            HttpResponseMessage resp,
            object? content,
            RefitSettings settings,
            ApiException? error = null
        )
        {
            return (T)
                Activator.CreateInstance(
                    typeof(ApiResponse<TBody>),
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
    /// <param name="response">Original HTTP Response message.</param>
    /// <param name="content">Response content.</param>
    /// <param name="settings">Refit settings used to send the request.</param>
    /// <param name="error">The ApiException, if the request failed.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public sealed class ApiResponse<T>(
        HttpResponseMessage response,
        T? content,
        RefitSettings settings,
        ApiException? error = null
        ) : IApiResponse<T>
    {
        readonly HttpResponseMessage response = response ?? throw new ArgumentNullException(nameof(response));
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
        public HttpResponseHeaders Headers => response.Headers;

        /// <summary>
        /// HTTP response content headers as defined in RFC 2616.
        /// </summary>
        public HttpContentHeaders? ContentHeaders => response.Content?.Headers;

        /// <summary>
        /// Indicates whether the request was successful.
        /// </summary>
#if NET6_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        public bool IsSuccessStatusCode => response.IsSuccessStatusCode;

        /// <summary>
        /// Indicates whether the request was successful and there wasn't any other error (for example, during deserialization).
        /// </summary>
#if NET6_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(Content))]
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        public bool IsSuccessful => IsSuccessStatusCode && Error is null;

        /// <summary>
        /// The reason phrase which typically is sent by the server together with the status code.
        /// </summary>
        public string? ReasonPhrase => response.ReasonPhrase;

        /// <summary>
        /// The HTTP Request message which led to this response.
        /// </summary>
        public HttpRequestMessage? RequestMessage => response.RequestMessage;

        /// <summary>
        /// HTTP response status code.
        /// </summary>
        public HttpStatusCode StatusCode => response.StatusCode;

        /// <summary>
        /// HTTP Message version.
        /// </summary>
        public Version Version => response.Version;

        /// <summary>
        /// The <see cref="ApiException" /> object in case of unsuccessful response.
        /// </summary>
        public ApiException? Error { get; private set; } = error;

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
        /// <exception cref="ApiException"></exception>
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
        /// <exception cref="ApiException"></exception>
        public async Task<ApiResponse<T>> EnsureSuccessfulAsync()
        {
            if (!IsSuccessful)
            {
                await ThrowsApiExceptionAsync().ConfigureAwait(false);
            }

            return this;
        }

        void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;

            disposed = true;

            response.Dispose();
        }

        private async Task<ApiException> ThrowsApiExceptionAsync()
        {
            var exception =
                    Error
                    ?? await ApiException
                        .Create(
                            response.RequestMessage!,
                            response.RequestMessage!.Method,
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
        /// The <see cref="ApiException"/> object in case of unsuccessful response.
        /// </summary>
        [SuppressMessage(
            "Naming",
            "CA1716:Identifiers should not match keywords",
            Justification = "By Design"
        )]
        new ApiException? Error { get; }

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
        /// Indicates whether the request was successful and there wasn't any other error (for example, during deserialization).
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
        HttpResponseHeaders Headers { get; }

        /// <summary>
        /// HTTP response content headers as defined in RFC 2616.
        /// </summary>
        HttpContentHeaders? ContentHeaders { get; }

        /// <summary>
        /// Indicates whether the request was successful.
        /// </summary>
#if NET6_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        bool IsSuccessStatusCode { get; }

        /// <summary>
        /// Indicates whether the request was successful and there wasn't any other error (for example, during deserialization).
        /// </summary>
#if NET6_0_OR_GREATER        
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        bool IsSuccessful { get; }

        /// <summary>
        /// HTTP response status code.
        /// </summary>
        HttpStatusCode StatusCode { get; }

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
        Version Version { get; }

        /// <summary>
        /// The <see cref="ApiException"/> object in case of unsuccessful response.
        /// </summary>
        [SuppressMessage(
           "Naming",
           "CA1716:Identifiers should not match keywords",
           Justification = "By Design"
       )]
        ApiException? Error { get; }
    }
}
