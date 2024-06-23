using System.Net;
using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks;

[MemoryDiagnoser]
public class StartupBenchmark
{
    private const string Host = "https://github.com";
    private readonly RefitSettings settings = new RefitSettings()
    {
        HttpMessageHandlerFactory = () =>
            new StaticValueHttpResponseHandler(
                "Ok",
                HttpStatusCode.OK
            )
    };

    [Benchmark]
    public IPerformanceApi CreateService() => RestService.For<IPerformanceApi>(Host, settings);

    [Benchmark]
    public async Task<string> ConstantRouteAsync()
    {
        var service = RestService.For<IPerformanceApi>(Host, settings);
        return await service.ConstantRoute();
    }


    [Benchmark]
    public async Task<string> ComplexRequestAsync()
    {
        var service = RestService.For<IPerformanceApi>(Host, settings);
        return await service.ObjectRequest(new PathBoundObject(){SomeProperty = "myProperty", SomeQuery = "myQuery"});
    }
}
