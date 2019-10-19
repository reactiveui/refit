using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Refit
{
    static class ApiResponse
    {
        internal static T Create<T>(HttpResponseMessage resp, object content, ApiException error = null)
        {
            return (T)Activator.CreateInstance(typeof(T), resp, content, error);
        }
    }

    public sealed class ApiResponse<T> : IDisposable
    {
        readonly HttpResponseMessage response;
        bool disposed;

        public ApiResponse(HttpResponseMessage response, T content, ApiException error = null)
        {
            this.response = response ?? throw new ArgumentNullException(nameof(response));
            Error = error;
            Content = content;
        }

        public T Content { get; }
        public HttpResponseHeaders Headers => response.Headers;
        public HttpContentHeaders ContentHeaders => response.Content?.Headers;
        public bool IsSuccessStatusCode => response.IsSuccessStatusCode;
        public string ReasonPhrase => response.ReasonPhrase;
        public HttpRequestMessage RequestMessage => response.RequestMessage;
        public HttpStatusCode StatusCode => response.StatusCode;
        public Version Version => response.Version;
        public ApiException Error { get; private set; }


        public void Dispose()
        {
            Dispose(true);
        }

        public async Task<ApiResponse<T>> EnsureSuccessStatusCodeAsync()
        {
            if (!IsSuccessStatusCode)
            {
                var exception = await ApiException.Create(response.RequestMessage, response.RequestMessage.Method, response).ConfigureAwait(false);

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
}
