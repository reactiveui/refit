using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Refit
{
    public interface IErrorHandler
    {
        Task<Exception> HandleErrorAsync(HttpRequestMessage message, HttpMethod httpMethod, HttpResponseMessage response,
            RefitSettings refitSettings = null);
    }
}
