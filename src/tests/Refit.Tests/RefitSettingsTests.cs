// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for constructing <see cref="RefitSettings"/> through its various constructors.</summary>
public class RefitSettingsTests
{
    /// <summary>Verifies every <see cref="RefitSettings"/> constructor overload can be created without throwing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Can_CreateRefitSettings_WithoutException()
    {
        var contentSerializer = new SystemTextJsonContentSerializer();
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        var urlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter();
        var formUrlEncodedParameterFormatter = new DefaultFormUrlEncodedParameterFormatter();

        var exception = CaptureException(static () => new RefitSettings());
        await Assert.That(exception).IsNull();

        exception = CaptureException(() => new RefitSettings(contentSerializer));
        await Assert.That(exception).IsNull();

        exception = CaptureException(() => new RefitSettings(contentSerializer, urlParameterFormatter));
        await Assert.That(exception).IsNull();

        exception = CaptureException(() => new RefitSettings(
            contentSerializer,
            urlParameterFormatter,
            formUrlEncodedParameterFormatter));
        await Assert.That(exception).IsNull();

        exception = CaptureException(() => new RefitSettings(
            contentSerializer,
            urlParameterFormatter,
            formUrlEncodedParameterFormatter,
            urlParameterKeyFormatter));
        await Assert.That(exception).IsNull();
    }

    /// <summary>Verifies the full constructor rejects a null content serializer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Constructor_RejectsNullContentSerializer()
    {
        var exception = CaptureException(static () => new RefitSettings(null!, null, null, null));

        await Assert.That(exception).IsTypeOf<ArgumentNullException>();
    }

    /// <summary>Verifies grouped settings and flat properties share the same values and collections.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GroupedSettings_ShareFlatProperties()
    {
        Dictionary<string, object> requestOptions = new() { ["request"] = "value" };
        var settings = new RefitSettings { HttpRequestMessageOptions = requestOptions };
        var serializer = new SystemTextJsonContentSerializer();
        var formatter = new CamelCaseUrlParameterKeyFormatter();
        settings.Serialization.ContentSerializer = serializer;
        settings.Url.UrlParameterKeyFormatter = formatter;
        settings.Exceptions.CaptureRequestContent = true;
        settings.Requests.ValidateHeaders = true;

        await Assert.That(settings.ContentSerializer).IsSameReferenceAs(serializer);
        await Assert.That(settings.UrlParameterKeyFormatter).IsSameReferenceAs(formatter);
        await Assert.That(settings.CaptureRequestContent).IsTrue();
        await Assert.That(settings.ValidateHeaders).IsTrue();
        await Assert.That(settings.Requests.HttpRequestMessageOptions).IsSameReferenceAs(requestOptions);
        await Assert.That(settings.UrlParameterFormatterMap).IsSameReferenceAs(settings.Url.UrlParameterFormatterMap);
        await Assert.That(settings.ReturnTypeAdapters).IsSameReferenceAs(settings.Requests.ReturnTypeAdapters);

        settings.Buffered = true;
        settings.CollectionFormat = CollectionFormat.Multi;
        settings.CaptureRequestContent = false;
        settings.ValidateHeaders = false;

        await Assert.That(settings.Serialization.Buffered).IsTrue();
        await Assert.That(settings.Url.CollectionFormat).IsEqualTo(CollectionFormat.Multi);
        await Assert.That(settings.Exceptions.CaptureRequestContent).IsFalse();
        await Assert.That(settings.Requests.ValidateHeaders).IsFalse();
    }

    /// <summary>Verifies each client gets independent grouped settings and collection storage.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GroupedSettings_AreIndependentPerClient()
    {
        var first = new RefitSettings();
        var second = new RefitSettings();
        first.Url.UrlParameterFormatterMap.Add(typeof(string), new DefaultUrlParameterFormatter());
        first.Requests.ReturnTypeAdapters.Add(typeof(string));
        first.Serialization.Buffered = true;

        await Assert.That(second.Url.UrlParameterFormatterMap.Count).IsEqualTo(0);
        await Assert.That(second.Requests.ReturnTypeAdapters.Count).IsEqualTo(0);
        await Assert.That(second.Serialization.Buffered).IsFalse();
        await Assert.That(second.Url.HonorContentSerializerPropertyNamesInQuery).IsTrue();
        await Assert.That(second.Url.UrlResolution).IsEqualTo(UrlResolutionMode.RefitLegacy);
        await Assert.That(second.Serialization.RequestCompression).IsEqualTo(RequestCompression.None);
    }

    /// <summary>Invokes a factory and returns any exception it throws, otherwise null.</summary>
    /// <param name="factory">The factory whose construction is being verified.</param>
    /// <returns>The thrown exception, or null when the factory completed successfully.</returns>
    private static Exception? CaptureException(Func<object> factory)
    {
        try
        {
            _ = factory();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
