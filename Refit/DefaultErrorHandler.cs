using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit
{
    public class DefaultErrorHandler: IErrorHandler
    {
        public virtual async Task<Exception> HandleErrorAsync(HttpRequestMessage message, HttpMethod httpMethod, HttpResponseMessage response,
            RefitSettings refitSettings = null)
        {
            var exception = await ApiException.Create(response.RequestMessage, response.RequestMessage.Method, response, refitSettings)
                .ConfigureAwait(false);
            return exception;
        }
    }
}
