// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies how formattable multipart values behave when the form formatter returns null.</summary>
public sealed class MultipartFormattableValueFormattingTests
{
    /// <summary>A formatter returning null yields an empty multipart part rather than throwing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullFormatterYieldsEmptyPart()
    {
        var settings = new RefitSettings
        {
            FormUrlEncodedParameterFormatter = new NullFormattingFormUrlEncodedParameterFormatter()
        };

        var body = await CaptureIdPartBody(settings);

        await Assert.That(body).IsEqualTo(string.Empty);
    }

    /// <summary>The default formatter yields a non-empty multipart part for a formattable value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DefaultFormatterYieldsNonEmptyPart()
    {
        var body = await CaptureIdPartBody(new RefitSettings());

        await Assert.That(body).IsNotEmpty();
    }

    /// <summary>Uploads a formattable Guid value and returns the captured body of the "id" multipart part.</summary>
    /// <param name="settings">The Refit settings that configure the form formatter.</param>
    /// <returns>The body of the "id" multipart part.</returns>
    private static async Task<string> CaptureIdPartBody(RefitSettings settings)
    {
        var fixture = new RequestBuilderImplementation<IRunscopeApi>(settings);
        var func = fixture.BuildRestResultFuncForMethod(nameof(IRunscopeApi.UploadFormattableValues));
        var handler = new MultipartCapturingHttpMessageHandler();
        using var client = HttpClientTestFactory.Create(handler, new("https://api/"));
        await (Task)func(client, [Guid.NewGuid(), DateTimeOffset.UnixEpoch])!;

        return handler.Parts.First(static part => part.Name == "id").Body;
    }
}
