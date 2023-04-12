using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.Benchmarks
{
    public class StaticFileHttpResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode responseCode;
        private readonly string responsePayload;

        public StaticFileHttpResponseHandler(string fileName, HttpStatusCode responseCode)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            responsePayload = File.ReadAllText(fileName);
;           this.responseCode = responseCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(responseCode)
            {
                RequestMessage = request,
                Content = new StringContent(responsePayload)
            });
        }
    }
}
