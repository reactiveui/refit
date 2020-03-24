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
        public string ReasonPhrase { get; }
        public HttpResponseHeaders Headers { get; }
        public HttpMethod HttpMethod { get; }
        public Uri Uri => RequestMessage.RequestUri;
        public HttpRequestMessage RequestMessage { get; }
        public HttpContentHeaders ContentHeaders { get; private set; }
        public string Content { get; private set; }
        public bool HasContent => !string.IsNullOrWhiteSpace(Content);
        public RefitSettings RefitSettings { get; set; }

        protected ApiException(HttpRequestMessage message, HttpMethod httpMethod, HttpStatusCode statusCode, string reasonPhrase, HttpResponseHeaders headers, RefitSettings refitSettings = null) :
            base(CreateMessage(statusCode, reasonPhrase))
        {
            RequestMessage = message;
            HttpMethod = httpMethod;
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Headers = headers;
            RefitSettings = refitSettings;
        }

        [Obsolete("Use GetContentAsAsync<T>() instead", false)]
        public T GetContentAs<T>() => GetContentAsAsync<T>().ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task<T> GetContentAsAsync<T>() => HasContent ?
                await RefitSettings.ContentSerializer.DeserializeAsync<T>(new StringContent(Content)).ConfigureAwait(false) :
                default;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static async Task<ApiException> Create(HttpRequestMessage message, HttpMethod httpMethod, HttpResponseMessage response, RefitSettings refitSettings = null)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            var exception = new ApiException(message, httpMethod, response.StatusCode, response.ReasonPhrase, response.Headers, refitSettings);

            if (response.Content == null)
            {
                return exception;
            }

            try
            {
                exception.ContentHeaders = response.Content.Headers;
                exception.Content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.Content.Headers?.ContentType?.MediaType?.Equals("application/problem+json") ?? false)
                {
                    exception = await ValidationApiException.Create(exception).ConfigureAwait(false);
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

        static string CreateMessage(HttpStatusCode statusCode, string reasonPhrase) =>
            $"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}).";
    }
}
