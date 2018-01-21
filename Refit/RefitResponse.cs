using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Refit
{
    public class RefitResponse<T> : IRefitResponse<T>
    {
        public HttpResponseHeaders Headers { get; }
        public bool IsSuccessStatusCode { get; }
        public string ReasonPhrase { get; }
        public HttpRequestMessage RequestMessage { get; }
        public HttpStatusCode StatusCode { get; }
        public Version Version { get; }
        public T Content { get; }

        public RefitResponse(HttpResponseMessage response,
                             T content)
        {
            Headers = response.Headers;
            IsSuccessStatusCode = response.IsSuccessStatusCode;
            ReasonPhrase = response.ReasonPhrase;
            RequestMessage = response.RequestMessage;
            StatusCode = response.StatusCode;
            Version = response.Version;
            Content = content;
        }

        internal RefitResponse(HttpResponseMessage response,
                               object content) : this(response, (T)content)
        {
        }
    }
}
