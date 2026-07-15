// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>
/// Verifies that a per-type formatter registered in <see cref="RefitSettings.UrlParameterFormatterMap"/> renders a
/// value into path and query strings, that unregistered types fall back to the default formatter, and that the
/// reflection and source-generated request builders produce identical URLs.
/// </summary>
public class UrlParameterFormatterMapTests
{
    /// <summary>The base address used by every client under test.</summary>
    private const string BaseAddress = "http://api/";

    /// <summary>An unregistered string path value that must fall back to the default formatter.</summary>
    private const string SampleCity = "paris";

    /// <summary>The temperature value whose custom rendering is asserted.</summary>
    private const int SampleCelsius = 21;

    /// <summary>The unregistered integer query value that must fall back to the default formatter.</summary>
    private const int SampleCount = 5;

    /// <summary>Verifies a registered formatter renders a path value while an unregistered path value uses the default.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RegisteredFormatterRendersPathValueInBothPaths()
    {
        var settings = CreateSettingsWithTemperatureFormatter();
        var temperature = new Temperature(SampleCelsius);

        var generated = await SendGeneratedAsync(settings, api => api.GetByPath(temperature, SampleCity));
        var reflected = await new RequestBuilderImplementation<IFormatterMapApi>(settings)
            .BuildRequestFactoryForMethod(nameof(IFormatterMapApi.GetByPath))([temperature, SampleCity]);

        // 21deg comes from the registered formatter; paris is a string, which has no registry entry and uses the default.
        await Assert.That(generated).IsEqualTo("/weather/21deg/paris");
        await Assert.That(reflected.RequestUri!.PathAndQuery).IsEqualTo("/weather/21deg/paris");
    }

    /// <summary>Verifies a registered formatter renders a query value while an unregistered query value uses the default.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RegisteredFormatterRendersQueryValueInBothPaths()
    {
        var settings = CreateSettingsWithTemperatureFormatter();
        var temperature = new Temperature(SampleCelsius);

        var generated = await SendGeneratedAsync(settings, api => api.GetByQuery(temperature, SampleCount));
        var reflected = await new RequestBuilderImplementation<IFormatterMapApi>(settings)
            .BuildRequestFactoryForMethod(nameof(IFormatterMapApi.GetByQuery))([temperature, SampleCount]);

        // temp uses the registered formatter; count is an int with no registry entry and falls back to the default.
        await Assert.That(generated).IsEqualTo("/weather?temp=21deg&count=5");
        await Assert.That(reflected.RequestUri!.PathAndQuery).IsEqualTo("/weather?temp=21deg&count=5");
    }

    /// <summary>Verifies that with no registry entry the value renders through the default formatter in both paths.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UnregisteredTypeFallsBackToDefaultFormatter()
    {
        var settings = new RefitSettings();
        var temperature = new Temperature(SampleCelsius);

        var generated = await SendGeneratedAsync(settings, api => api.GetByPath(temperature, SampleCity));
        var reflected = await new RequestBuilderImplementation<IFormatterMapApi>(settings)
            .BuildRequestFactoryForMethod(nameof(IFormatterMapApi.GetByPath))([temperature, SampleCity]);

        // The default formatter renders Temperature through IFormattable, so no "deg" suffix is applied.
        await Assert.That(generated).IsEqualTo("/weather/21/paris");
        await Assert.That(reflected.RequestUri!.PathAndQuery).IsEqualTo("/weather/21/paris");
    }

    /// <summary>Verifies the generated and reflection builders produce identical URLs when a per-type formatter is registered.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GeneratedAndReflectionUrlsMatchWithRegisteredFormatter()
    {
        var settings = CreateSettingsWithTemperatureFormatter();
        var temperature = new Temperature(SampleCelsius);

        var generatedPath = await SendGeneratedAsync(settings, api => api.GetByPath(temperature, SampleCity));
        var reflectedPath = await new RequestBuilderImplementation<IFormatterMapApi>(settings)
            .BuildRequestFactoryForMethod(nameof(IFormatterMapApi.GetByPath))([temperature, SampleCity]);

        var generatedQuery = await SendGeneratedAsync(settings, api => api.GetByQuery(temperature, SampleCount));
        var reflectedQuery = await new RequestBuilderImplementation<IFormatterMapApi>(settings)
            .BuildRequestFactoryForMethod(nameof(IFormatterMapApi.GetByQuery))([temperature, SampleCount]);

        await Assert.That(generatedPath).IsEqualTo(reflectedPath.RequestUri!.PathAndQuery);
        await Assert.That(generatedQuery).IsEqualTo(reflectedQuery.RequestUri!.PathAndQuery);
    }

    /// <summary>Builds settings that render <see cref="Temperature"/> through a per-type formatter.</summary>
    /// <returns>The configured settings.</returns>
    private static RefitSettings CreateSettingsWithTemperatureFormatter()
    {
        var settings = new RefitSettings();
        settings.UrlParameterFormatterMap[typeof(Temperature)] = new TemperatureUrlParameterFormatter();
        return settings;
    }

    /// <summary>Sends one request through the source-generated client and returns the relative URI it produced.</summary>
    /// <param name="settings">The settings to build the client with.</param>
    /// <param name="call">The interface method to invoke.</param>
    /// <returns>The generated request's path and query.</returns>
    private static async Task<string> SendGeneratedAsync(RefitSettings settings, Func<IFormatterMapApi, Task<string>> call)
    {
        var handler = new TestHttpMessageHandler();
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
        var api = RestService.ForGenerated<IFormatterMapApi>(client, settings);

        _ = await call(api);

        return handler.RequestMessage!.RequestUri!.PathAndQuery;
    }
}
