using System.Net;
using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks;

[MemoryDiagnoser]
public class StartupBenchmark
{
    private IPerformanceService initialisedService;
    private const string Host = "https://github.com";
    private readonly RefitSettings settings = new RefitSettings()
    {
        HttpMessageHandlerFactory = () =>
            new StaticValueHttpResponseHandler(
                "Ok",
                HttpStatusCode.OK
            )
    };


    [IterationSetup(Targets = [nameof(FirstCallConstantRouteAsync), nameof(FirstCallComplexRequestAsync)])]
    public void Setup()
    {
        initialisedService = RestService.For<IPerformanceService>(Host, settings);
    }

    [Benchmark]
    public IPerformanceService CreateService() => RestService.For<IPerformanceService>(Host, settings);

    [Benchmark]
    public async Task<string> FirstCallConstantRouteAsync() => await initialisedService.ConstantRoute();

    [Benchmark]
    public async Task<string> ConstantRouteAsync()
    {
        var service = RestService.For<IPerformanceService>(Host, settings);
        return await service.ConstantRoute();
    }

    [Benchmark]
    public async Task<string> FirstCallComplexRequestAsync() => await initialisedService.ObjectRequest(new PathBoundObject(){SomeProperty = "myProperty", SomeQuery = "myQuery"});

    [Benchmark]
    public async Task<string> ComplexRequestAsync()
    {
        var service = RestService.For<IPerformanceService>(Host, settings);
        return await service.ObjectRequest(new PathBoundObject(){SomeProperty = "myProperty", SomeQuery = "myQuery"});
    }
}
