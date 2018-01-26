using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Refit
{
    public interface IRefitResponse<T>
    {
        HttpResponseHeaders Headers { get; }
        bool IsSuccessStatusCode { get; }
        string ReasonPhrase { get; }
        HttpRequestMessage RequestMessage { get; }
        HttpStatusCode StatusCode { get; }
        Version Version { get; }
        T Content { get; }
    }
}
