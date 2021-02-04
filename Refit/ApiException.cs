using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Refit
{

    [Serializable]
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? ReasonPhrase { get; }
        public HttpResponseHeaders Headers { get; }
        public HttpMethod HttpMethod { get; }
        public Uri? Uri => RequestMessage.RequestUri;
        public HttpRequestMessage RequestMessage { get; }
        public HttpContentHeaders? ContentHeaders { get; private set; }
        public string? Content { get; private set; }
        public bool HasContent => !string.IsNullOrWhiteSpace(Content);
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

        public async Task<T?> GetContentAsAsync<T>() => HasContent ?
                await RefitSettings.ContentSerializer.FromHttpContentAsync<T>(new StringContent(Content!)).ConfigureAwait(false) :
                default;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task<ApiException> Create(HttpRequestMessage message, HttpMethod httpMethod, HttpResponseMessage response, RefitSettings refitSettings, Exception? innerException = null)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            var exceptionMessage = CreateMessage(response.StatusCode, response.ReasonPhrase);
            return Create(exceptionMessage, message, httpMethod, response, refitSettings, innerException);
        }

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
