namespace RestApiforTest;

/// <summary>
/// Provides the entry point and web host configuration methods for the application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Serves as the entry point for the application.
    /// </summary>
    /// <remarks>This method configures and starts the web host. It is typically called automatically
    /// by the runtime and should not be invoked directly.</remarks>
    /// <param name="args">An array of command-line arguments supplied to the application.</param>
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    /// <summary>
    /// Initializes a new instance of the web host builder with pre-configured defaults and the specified startup
    /// class.
    /// </summary>
    /// <remarks>This method sets up the web host with default configuration, logging, and Kestrel
    /// server settings, and specifies the application's startup class. It is typically called from the
    /// application's entry point to configure and launch the ASP.NET Core application.</remarks>
    /// <param name="args">An array of command-line arguments to configure the web host. May be empty but cannot be null.</param>
    /// <returns>A configured web host builder instance that can be used to build and run the web application.</returns>
    public static IHostBuilder CreateWebHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}
