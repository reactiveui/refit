using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Refit
{
    static class ApiResponse
    {
        internal static T Create<T, TBody>(HttpResponseMessage resp, object? content, RefitSettings settings, ApiException? error = null)
        {
            return (T)Activator.CreateInstance(typeof(ApiResponse<TBody>), resp, content, settings, error)!;
        }
    }

    /// <summary>
    /// Implementation of <see cref="IApiResponse{T}"/> that provides additional functionalities.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ApiResponse<T> : IApiResponse<T>
    {
        readonly HttpResponseMessage response;
        bool disposed;

        /// <summary>
        /// Create an instance of <see cref="ApiResponse{T}"/> with type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="response">Original HTTP Response message.</param>
        /// <param name="content">Response content.</param>
        /// <param name="settings">Refit settings used to send the request.</param>
        /// <param name="error">The ApiException, if the request failed.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ApiResponse(HttpResponseMessage response, T? content, RefitSettings settings, ApiException? error = null)
        {
            this.response = response ?? throw new ArgumentNullException(nameof(response));
            Error = error;
            Content = content;
            Settings = settings;
        }

        /// <summary>
        /// Deserialized request content as <typeparamref name="T"/>.
        /// </summary>
        public T? Content { get; }

        /// <summary>
        /// Refit settings used to send the request.
        /// </summary>
        public RefitSettings Settings { get; }
        
        public HttpResponseHeaders Headers => response.Headers;
        
        public HttpContentHeaders? ContentHeaders => response.Content?.Headers;
        
#if NET5_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(Content))]
        [MemberNotNullWhen(true, nameof(ContentHeaders))]
        [MemberNotNullWhen(false, nameof(Error))]
#endif
        public bool IsSuccessStatusCode => response.IsSuccessStatusCode;

        public string? ReasonPhrase => response.ReasonPhrase;
        
        public HttpRequestMessage? RequestMessage => response.RequestMessage;
        
        public HttpStatusCode StatusCode => response.StatusCode;
        
        public Version Version => response.Version;
        
        public ApiException? Error { get; private set; }


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
                var exception = await ApiException.Create(response.RequestMessage!, response.RequestMessage!.Method, response, Settings).ConfigureAwait(false);

                Dispose();

                throw exception;
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
    }

    /// <inheritdoc/>
    public interface IApiResponse<out T> : IApiResponse
    {
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
        bool IsSuccessStatusCode { get; }

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
        ApiException? Error { get; }
    }
}
