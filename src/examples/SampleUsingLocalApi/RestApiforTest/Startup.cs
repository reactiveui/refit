// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace RestApiforTest;

/// <summary>Provides configuration and service setup for the application's startup process.</summary>
/// <remarks>The Startup class is used by the ASP.NET Core runtime to configure services and the HTTP
/// request pipeline for the application. It defines methods for registering services with the dependency injection
/// container and for specifying how HTTP requests are handled. This class is typically specified as the entry point
/// for application startup in the program's host configuration.</remarks>
public sealed class Startup
{
    /// <summary>Initializes a new instance of the Startup class with the specified application configuration settings.</summary>
    /// <param name="configuration">The application configuration settings to be used for configuring services and the app's request pipeline.
    /// Cannot be null.</param>
    public Startup(IConfiguration configuration) => Configuration = configuration;

    /// <summary>Gets the application's configuration settings.</summary>
    /// <remarks>Use this property to access key-value pairs and configuration sections defined for
    /// the application, such as settings from appsettings.json, environment variables, or other configuration
    /// providers.</remarks>
    public IConfiguration Configuration { get; }

    /// <summary>Configures the application's services by adding required service registrations to the dependency injection container.</summary>
    /// <remarks>Call this method to register services needed by the application, such as MVC
    /// controllers and related infrastructure. This method is typically called by the runtime during application
    /// startup.</remarks>
    /// <param name="services">The collection of service descriptors to which application services are added. This parameter must not be
    /// null.</param>
    public void ConfigureServices(IServiceCollection services) => services.AddControllers();

    /// <summary>Configures the application's request pipeline and environment-specific middleware.</summary>
    /// <remarks>In a development environment, this method adds middleware to display detailed
    /// exception information. It also configures MVC routing for handling HTTP requests.</remarks>
    /// <param name="app">The application builder used to configure the HTTP request pipeline.</param>
    /// <param name="env">The hosting environment information for the current application.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            _ = app.UseDeveloperExceptionPage();
        }

        _ = app.UseRouting();

        _ = app.UseEndpoints(static endpoints => endpoints.MapControllers());
    }
}
