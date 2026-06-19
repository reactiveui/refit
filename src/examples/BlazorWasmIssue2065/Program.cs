// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BlazorWasmIssue2065;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped(sp =>
    RestService.For<IIssue2065Api>(sp.GetRequiredService<HttpClient>()));

builder.Services.AddScoped(sp =>
    RestService.For<IIssue2067Api>(
        sp.GetRequiredService<HttpClient>(),
        new RefitSettings(
            new SystemTextJsonContentSerializer(
                SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))));

await builder.Build().RunAsync();
