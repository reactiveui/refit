using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit
{
    public abstract class ApiResponse
    {
        public HttpResponseHeaders Headers { get; }
        public bool IsSuccessStatusCode { get; }
        public string ReasonPhrase { get; }
        public HttpRequestMessage RequestMessage { get; }
        public HttpStatusCode StatusCode { get; }
        public Version Version { get; }

        public ApiResponse(HttpResponseMessage response)
        {
            Headers = response.Headers;
            IsSuccessStatusCode = response.IsSuccessStatusCode;
            ReasonPhrase = response.ReasonPhrase;
            RequestMessage = response.RequestMessage;
            StatusCode = response.StatusCode;
            Version = response.Version;
        }

        //#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        //        internal static async Task<T> Create<T>(Type serializedReturnType, HttpResponseMessage resp, JsonSerializer serializer, JsonTextReader jsonReader)
        //#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        //        {
        //            object deserializedObject = null;

        //            if (serializedReturnType.IsConstructedGenericType &&
        //                serializedReturnType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
        //            {
        //                deserializedObject = serializer.Deserialize(jsonReader, serializedReturnType.GenericTypeArguments[0]);
        //            }

        //            return (T)(object)Activator.CreateInstance(typeof(T), resp, deserializedObject);
        //        }

        //#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        internal static async Task<T> Create<T>(HttpResponseMessage resp, object content)
        //#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            return (T)Activator.CreateInstance(typeof(T), resp, content);
        }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T Content { get; }

        public ApiResponse(HttpResponseMessage response,
                           T content) : base(response)
        {
            this.Content = (T)content;
        }
    }
}
