using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Refit
{
    static class ApiResponse
    {
        internal static T Create<T>(HttpResponseMessage resp, object content)
        {
            return (T)Activator.CreateInstance(typeof(T), resp, content);
        }
    }

    public sealed class ApiResponse<T> : IDisposable
    {
        private readonly HttpResponseMessage response;

        bool disposed;

        public ApiResponse(HttpResponseMessage response, T content)
        {
            this.response = response ?? throw new ArgumentNullException(nameof(response));
            Content = content;
        }

        public T Content { get; }
        public HttpResponseHeaders Headers => response.Headers;
        public bool IsSuccessStatusCode => response.IsSuccessStatusCode;
        public string ReasonPhrase => response.ReasonPhrase;
        public HttpRequestMessage RequestMessage => response.RequestMessage;
        public HttpStatusCode StatusCode => response.StatusCode;
        public Version Version => response.Version;

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
