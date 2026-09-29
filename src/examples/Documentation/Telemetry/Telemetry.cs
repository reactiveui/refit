// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Refit.Documentation;

/// <summary>Traces and measures generated-client calls with OpenTelemetry, labelled by Refit's method and route.</summary>
internal static class Telemetry
{
    /// <summary>The reply every call receives.</summary>
    private const string AdaJson = "{\"id\":1,\"name\":\"Ada\"}";

    /// <summary>Runs the tracing and metrics scenarios.</summary>
    /// <returns>A task that faults when an observed result differs from the documented behavior.</returns>
    internal static async Task RunAsync()
    {
        await TraceCallAsync();
        await MeasureCallAsync();
    }

    /// <summary>Names HttpClient's span after the Refit route and tags it with the method name.</summary>
    /// <param name="options">The HttpClient instrumentation options.</param>
    internal static void EnrichSpans(OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions options) =>
        options.EnrichWithHttpRequestMessage = static (activity, request) =>
        {
            if (RefitRequestLabels.RouteTemplate(request) is { } route)
            {
                activity.DisplayName = $"{request.Method} {route}"; // "GET /people/{id}", not one name per person
                _ = activity.SetTag("url.template", route);
            }

            if (RefitRequestLabels.MethodName(request) is { } method)
            {
                _ = activity.SetTag("refit.method", method);
            }
        };

    /// <summary>Problem: How do you see which Refit method a request span belongs to?</summary>
    /// <returns>A task that completes after the exported span is checked.</returns>
    private static async Task TraceCallAsync()
    {
        List<Activity> spans = [];
        using TracerProvider tracing = Sdk.CreateTracerProviderBuilder()
            .AddHttpClientInstrumentation(EnrichSpans)
            .AddInMemoryExporter(spans)
            .Build();

        await using ServiceProvider provider = CreateProvider();
        Person person = await provider.GetRequiredService<ITelemetryPeopleApi>().GetPersonAsync(1, CancellationToken.None);

        Activity span = spans[0]; // HttpClient's own span: Refit adds no second one
        Console.WriteLine(span.DisplayName); // GET /people/{id}
        Console.WriteLine(span.GetTagItem("refit.method")); // GetPersonAsync

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(1, spans.Count);
        SampleCheck.Equal("Ada", person.Name);
        SampleCheck.Equal("GET /people/{id}", span.DisplayName);
        SampleCheck.Equal("/people/{id}", span.GetTagItem("url.template") as string);
        SampleCheck.Equal("GetPersonAsync", span.GetTagItem("refit.method") as string);
    }

    /// <summary>Problem: How do you group request durations by API route instead of by full URL?</summary>
    /// <returns>A task that completes after the exported measurement is checked.</returns>
    private static async Task MeasureCallAsync()
    {
        List<Metric> metrics = [];
        using MeterProvider meters = Sdk.CreateMeterProviderBuilder()
            .AddHttpClientInstrumentation()
            .AddInMemoryExporter(metrics)
            .Build();

        await using ServiceProvider provider = CreateProvider();
        ITelemetryPeopleApi api = provider.GetRequiredService<ITelemetryPeopleApi>();
        _ = await api.GetPersonAsync(1, CancellationToken.None);
        _ = await api.GetPersonAsync(2, CancellationToken.None);
        _ = meters.ForceFlush();

        List<MetricPoint> points = [];
        foreach (Metric metric in metrics)
        {
            if (metric.Name != "http.client.request.duration")
            {
                continue;
            }

            foreach (ref readonly MetricPoint point in metric.GetMetricPoints())
            {
                points.Add(point);
            }
        }

        MetricPoint byRoute = points[0]; // both people land in one "/people/{id}" series
        Dictionary<string, object?> tags = [];
        foreach (KeyValuePair<string, object?> tag in byRoute.Tags)
        {
            tags[tag.Key] = tag.Value;
        }

        Console.WriteLine(tags["url.template"]); // /people/{id}
        Console.WriteLine(byRoute.GetHistogramCount()); // 2

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(1, points.Count);
        SampleCheck.Equal("/people/{id}", tags["url.template"] as string);
        SampleCheck.Equal("GetPersonAsync", tags["refit.method"] as string);
        SampleCheck.Equal(2L, byRoute.GetHistogramCount());
    }

    /// <summary>Registers the client with the metric tags handler and an in-memory connection.</summary>
    /// <returns>The service provider that owns the client.</returns>
    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new ServiceCollection();
        _ = services.AddRefitGeneratedClient<ITelemetryPeopleApi>(SampleJsonContext.Default)
            .ConfigureHttpClient(static client => client.BaseAddress = new Uri("http://localhost"))
            .AddHttpMessageHandler(static () => new RefitMetricsTagsHandler())
            .ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler
            {
                ConnectCallback = static (_, _) => ValueTask.FromResult<Stream>(new CannedResponseStream(AdaJson)),
            });
        return services.BuildServiceProvider();
    }
}
