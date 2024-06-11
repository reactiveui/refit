using System;
using System.Threading.Tasks;
using Serilog;

namespace Meow
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Verbose()
                .CreateLogger();

            var service = new CatsService(new Uri("https://api.thecatapi.com"));
            var results = await service.Search("bengal");

            Log.Debug("{results}", results);
        }
    }
}
