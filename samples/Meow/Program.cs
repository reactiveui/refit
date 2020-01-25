using Serilog;
using System;
using System.Threading.Tasks;

namespace Meow
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Verbose()
            .CreateLogger();

            Task.Run(() => AsyncMain()).Wait();
        }

        static async Task AsyncMain()
        {
            var service = new CatsService(new Uri("https://api.thecatapi.com"));
            var results = await service.Search("bengal");

            Log.Debug("{results}", results);

        }
    }
}
