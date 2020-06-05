using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Utf8Json;
using Utf8Json.Resolvers;

namespace Refit
{
    public sealed class Utf8JsonContentSerializer : IContentSerializer
    {
        private readonly MediaTypeHeaderValue jsonMediaType = new MediaTypeHeaderValue("application/json") { CharSet = Encoding.UTF8.WebName };

        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);

            var item = await JsonSerializer.DeserializeAsync<T>(stream, StandardResolver.CamelCase).ConfigureAwait(false);

            stream.Dispose();

            return item;
        }

        public async Task<HttpContent> SerializeAsync<T>(T item)
        {
            var output = new MemoryStream();

            await JsonSerializer.SerializeAsync(output, item, StandardResolver.AllowPrivateCamelCase).ConfigureAwait(false);
            output.Position = 0;

            var content = new StreamContent(output)
            {
                Headers =
                {
                    ContentLength = output.Length,
                    ContentType = jsonMediaType
                }
            };

            return content;
        }
    }
}
