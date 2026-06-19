// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Tests for constructing <see cref="RefitSettings"/> through its various constructors.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class RefitSettingsTests
{
    /// <summary>Verifies every <see cref="RefitSettings"/> constructor overload can be created without throwing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Can_CreateRefitSettings_WithoutException()
    {
        var contentSerializer = new NewtonsoftJsonContentSerializer();
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        var urlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter();
        var formUrlEncodedParameterFormatter = new DefaultFormUrlEncodedParameterFormatter();

        var exception = CaptureException(() => new RefitSettings());
        await Assert.That(exception).IsNull();

        exception = CaptureException(() => new RefitSettings(contentSerializer));
        await Assert.That(exception).IsNull();

        exception = CaptureException(
            () => new RefitSettings(contentSerializer, urlParameterFormatter));
        await Assert.That(exception).IsNull();

        exception = CaptureException(
            () => new RefitSettings(
                contentSerializer,
                urlParameterFormatter,
                formUrlEncodedParameterFormatter));
        await Assert.That(exception).IsNull();

        exception = CaptureException(
            () => new RefitSettings(
                contentSerializer,
                urlParameterFormatter,
                formUrlEncodedParameterFormatter,
                urlParameterKeyFormatter));
        await Assert.That(exception).IsNull();
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
