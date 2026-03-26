using BlazorWasmIssue2065;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddScoped(sp =>
    RestService.For<IIssue2065Api>(sp.GetRequiredService<HttpClient>())
);
builder.Services.AddScoped(sp =>
    RestService.For<IIssue2067Api>(
        sp.GetRequiredService<HttpClient>(),
        new RefitSettings(
            new SystemTextJsonContentSerializer(
                SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
            )
        )
    )
);

await builder.Build().RunAsync();
