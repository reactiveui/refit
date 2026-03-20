using System.Net;
using System.Text;

using Newtonsoft.Json;

using Refit;

namespace Meow;

public static class Issue2056And2058Demo
{
    public static async Task RunAsync()
    {
        using var httpClient = new HttpClient(
            new CustomerIdHeaderHandler(new DemoBackendHandler())
        )
        {
            BaseAddress = new Uri("https://demo.local")
        };

        var api = RestService.For<IIssueDemoApi>(
            httpClient,
            new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer()
            }
        );

        await ValidateIssue2056Async(api);
        await ValidateIssue2058Async(api);
    }

    static async Task ValidateIssue2056Async(IIssueDemoApi api)
    {
        var customerIds = Enumerable.Range(1000, 50).ToArray();

        var responses = await Task.WhenAll(
            customerIds.Select(async customerId =>
            {
                var echo = await api.EchoCustomerAsync(customerId);
                return (Expected: customerId, Actual: echo.CustomerIdHeader);
            })
        );

        var mismatches = responses.Where(x => x.Expected.ToString() != x.Actual).ToArray();
        if (mismatches.Length > 0)
        {
            throw new InvalidOperationException(
                $"Issue #2056 check failed. Found {mismatches.Length} mismatched CustomerId headers."
            );
        }
    }

    static async Task ValidateIssue2058Async(IIssueDemoApi api)
    {
        var payload = await api.GetLargePayloadAsync(2000);
        if (payload.Items.Count != 2000)
        {
            throw new InvalidOperationException(
                $"Issue #2058 check failed. Expected 2000 items but got {payload.Items.Count}."
            );
        }
    }
}

public interface IIssueDemoApi
{
    [Get("/echo-customer")]
    Task<CustomerEchoResponse> EchoCustomerAsync([Property("CustomerId")] int customerId);

    [Get("/large-payload")]
    Task<LargePayloadResponse> GetLargePayloadAsync([AliasAs("size")] int size);
}

public sealed class CustomerEchoResponse
{
    [JsonProperty("customerIdHeader")]
    public string? CustomerIdHeader { get; set; }
}

public sealed class LargePayloadResponse
{
    [JsonProperty("items")]
    public List<int> Items { get; set; } = [];
}

public sealed class CustomerIdHeaderHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    static readonly HttpRequestOptionsKey<object?> CustomerIdKey = new("CustomerId");

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (request.Options.TryGetValue(CustomerIdKey, out var customerId) && customerId is not null)
        {
            request.Headers.Remove("CustomerId");
            request.Headers.TryAddWithoutValidation("CustomerId", customerId.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}

public sealed class DemoBackendHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (request.RequestUri?.AbsolutePath == "/echo-customer")
        {
            var customerIdHeader = request.Headers.TryGetValues("CustomerId", out var values)
                ? values.FirstOrDefault()
                : null;

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonConvert.SerializeObject(new CustomerEchoResponse
                        {
                            CustomerIdHeader = customerIdHeader
                        }),
                        Encoding.UTF8,
                        "application/json"
                    )
                }
            );
        }

        if (request.RequestUri?.AbsolutePath == "/large-payload")
        {
            var query = request.RequestUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            var size = 100;
            foreach (var part in query)
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 && kv[0] == "size" && int.TryParse(Uri.UnescapeDataString(kv[1]), out var parsed))
                {
                    size = parsed;
                    break;
                }
            }
            var payload = JsonConvert.SerializeObject(new LargePayloadResponse
            {
                Items = Enumerable.Range(1, size).ToList()
            });

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new AsyncOnlyJsonHttpContent(payload)
                }
            );
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

public sealed class AsyncOnlyJsonHttpContent(string json) : HttpContent
{
    readonly byte[] _buffer = Encoding.UTF8.GetBytes(json);

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        stream.WriteAsync(_buffer, 0, _buffer.Length);

    protected override bool TryComputeLength(out long length)
    {
        length = _buffer.Length;
        return true;
    }

    protected override Task<Stream> CreateContentReadStreamAsync() =>
        Task.FromResult<Stream>(new AsyncOnlyReadStream(_buffer));
}

public sealed class AsyncOnlyReadStream(byte[] data) : Stream
{
    readonly MemoryStream _inner = new(data, writable: false);

    public override bool CanRead => true;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _inner.Length;
    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Synchronous reads are not supported in this stream.");

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default
    ) => await _inner.ReadAsync(buffer, cancellationToken);

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    ) => _inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
