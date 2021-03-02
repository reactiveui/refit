using Serilog;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HttpClientDiagnostics
{
    [DebuggerStepThrough]
    public class HttpClientDiagnosticsHandler : DelegatingHandler
    {
        public HttpClientDiagnosticsHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        public HttpClientDiagnosticsHandler()
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var totalElapsedTime = Stopwatch.StartNew();

            Log.Debug(string.Format("Request: {0}", request));
            if (request.Content != null)
            {
                var content = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                Log.Debug(string.Format("Request Content: {0}", content));
            }

            var responseElapsedTime = Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);

            Log.Debug(string.Format("Response: {0}", response));
            if (response.Content != null)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Log.Debug(string.Format("Response Content: {0}", content));
            }

            responseElapsedTime.Stop();
            Log.Debug(string.Format("Response elapsed time: {0} ms", responseElapsedTime.ElapsedMilliseconds));

            totalElapsedTime.Stop();
            Log.Debug(string.Format("Total elapsed time: {0} ms", totalElapsedTime.ElapsedMilliseconds));

            return response;
        }
    }
}
