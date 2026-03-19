using Serilog;

namespace Meow;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Verbose()
            .CreateLogger();

        await Issue2056And2058Demo.RunAsync();

        Log.Information("Issue #2056 and #2058 demo checks passed.");

        var service = new CatsService(new Uri("https://api.thecatapi.com"));
        var results = await service.Search("bengal");

        Log.Debug("{results}", results);
    }
}
