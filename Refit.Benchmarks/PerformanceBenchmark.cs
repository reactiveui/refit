using System.Net;
using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks;

[MemoryDiagnoser]
public class PerformanceBenchmark
{
    private IPerformanceApi? service;

    private const string Host = "https://github.com";
    private SystemTextJsonContentSerializer systemTextJsonContentSerializer;

    [GlobalSetup]
    public Task SetupAsync()
    {
        systemTextJsonContentSerializer = new SystemTextJsonContentSerializer();
        service =
            RestService.For<IPerformanceApi>(
                Host,
                new RefitSettings(systemTextJsonContentSerializer)
                {
                    HttpMessageHandlerFactory = () =>
                        new StaticValueHttpResponseHandler(
                            "Ok",
                            HttpStatusCode.OK
                        )
                }
            );

        return Task.CompletedTask;
    }

    [Benchmark]
    public async Task<string> ConstantRouteAsync() => await service.ConstantRoute();

    [Benchmark]
    public async Task<string> DynamicRouteAsync() => await service.DynamicRoute(101);

    [Benchmark]
    public async Task<string> ComplexDynamicRouteAsync() => await service.ComplexDynamicRoute(101, "tom", "yCxv");

    [Benchmark]
    public async Task<string> ObjectRequestAsync() => await service.ObjectRequest(new PathBoundObject(){SomeProperty = "myProperty", SomeQuery = "myQuery"});

    [Benchmark]
    public async Task<string> ComplexRequestAsync() => await service.ComplexRequest(101, new PathBoundObject(){SomeProperty = "myProperty", SomeQuery = "myQuery"}, [1,2,3,4,5,6]);
}
