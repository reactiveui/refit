using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Refit
{
    /// <summary>
    /// Represents an error that occured while sending an API request.
    /// </summary>
    [Serializable]
    public class ApiException : Exception
    {
        /// <summary>
        /// HTTP response status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The reason phrase which typically is sent by the server together with the status code.
        /// </summary>
        public string? ReasonPhrase { get; }

        /// <summary>
        /// HTTP response headers.
        /// </summary>
        public HttpResponseHeaders Headers { get; }

        /// <summary>
        /// The HTTP method used to send the request.
        /// </summary>
        public HttpMethod HttpMethod { get; }

        /// <summary>
        /// The <see cref="System.Uri"/> used to send the HTTP request.
        /// </summary>
        public Uri? Uri => RequestMessage.RequestUri;

        /// <summary>
        /// The HTTP Request message used to send the request.
        /// </summary>
        public HttpRequestMessage RequestMessage { get; }

        /// <summary>
        /// HTTP response content headers as defined in RFC 2616.
        /// </summary>
        public HttpContentHeaders? ContentHeaders { get; private set; }

        /// <summary>
        /// HTTP Response content as string.
        /// </summary>
        public string? Content { get; private set; }

        /// <summary>
        /// Does the response have content?
        /// </summary>
        public bool HasContent => !string.IsNullOrWhiteSpace(Content);

        /// <summary>
        /// Refit settings used to send the request.
        /// </summary>
        public RefitSettings RefitSettings { get; }

        protected ApiException(HttpRequestMessage message, HttpMethod httpMethod, string? content, HttpStatusCode statusCode, string? reasonPhrase, HttpResponseHeaders headers, RefitSettings refitSettings, Exception? innerException = null) :
            this(CreateMessage(statusCode, reasonPhrase), message, httpMethod, content, statusCode, reasonPhrase, headers, refitSettings, innerException)
        {
        }

        protected ApiException(string exceptionMessage, HttpRequestMessage message, HttpMethod httpMethod, string? content, HttpStatusCode statusCode, string? reasonPhrase, HttpResponseHeaders headers, RefitSettings refitSettings, Exception? innerException = null) :
            base(exceptionMessage, innerException)
        {
            RequestMessage = message;
            HttpMethod = httpMethod;
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Headers = headers;
            RefitSettings = refitSettings;
            Content = content;
        }

        /// <summary>
        /// Get the deserialized response content as nullable <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to deserialize the content to</typeparam>
        /// <returns>The response content deserialized as <typeparamref name="T"/></returns>
        public async Task<T?> GetContentAsAsync<T>() => HasContent ?
                await RefitSettings.ContentSerializer.FromHttpContentAsync<T>(new StringContent(Content!)).ConfigureAwait(false) :
                default;

        /// <summary>
        /// Create an instance of <see cref="ApiException"/>.
        /// </summary>
        /// <param name="message">The HTTP Request message used to send the request.</param>
        /// <param name="httpMethod">The HTTP method used to send the request.</param>
        /// <param name="response">The HTTP Response message.</param>
        /// <param name="refitSettings">Refit settings used to sent the request.</param>
        /// <param name="innerException">Add an inner exception to the <see cref="ApiException"/>.</param>
        /// <returns>A newly created <see cref="ApiException"/>.</returns>
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task<ApiException> Create(HttpRequestMessage message, HttpMethod httpMethod, HttpResponseMessage response, RefitSettings refitSettings, Exception? innerException = null)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            var exceptionMessage = CreateMessage(response.StatusCode, response.ReasonPhrase);
            return Create(exceptionMessage, message, httpMethod, response, refitSettings, innerException);
        }

        /// <summary>
        /// Create an instance of <see cref="ApiException"/> with a custom exception message.
        /// </summary>
        /// <param name="exceptionMessage">A custom exception message.</param>
        /// <param name="message">The HTTP Request message used to send the request.</param>
        /// <param name="httpMethod">The HTTP method used to send the request.</param>
        /// <param name="response">The HTTP Response message.</param>
        /// <param name="refitSettings">Refit settings used to sent the request.</param>
        /// <param name="innerException">Add an inner exception to the <see cref="ApiException"/>.</param>
        /// <returns>A newly created <see cref="ApiException"/>.</returns>
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static async Task<ApiException> Create(string exceptionMessage, HttpRequestMessage message, HttpMethod httpMethod, HttpResponseMessage response, RefitSettings refitSettings, Exception? innerException = null)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            var exception = new ApiException(exceptionMessage, message, httpMethod, null, response.StatusCode, response.ReasonPhrase, response.Headers, refitSettings, innerException);

            if (response.Content == null)
            {
                return exception;
            }

            try
            {
                exception.ContentHeaders = response.Content.Headers;
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                exception.Content = content;

                if (response.Content.Headers?.ContentType?.MediaType?.Equals("application/problem+json") ?? false)
                {
                    exception = ValidationApiException.Create(exception);                    
                }

                response.Content.Dispose();
            }
            catch
            {
                // NB: We're already handling an exception at this point, 
                // so we want to make sure we don't throw another one 
                // that hides the real error.
            }

            return exception;
        }

        static string CreateMessage(HttpStatusCode statusCode, string? reasonPhrase) =>
            $"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}).";
    }
}
