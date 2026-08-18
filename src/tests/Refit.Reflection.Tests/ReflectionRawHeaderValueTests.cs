// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Pins that a declared header value reaches the request exactly as written, however many headers the method
/// declares. Values are added without validation, so the framework parser must never get to reformat one.</summary>
public sealed class ReflectionRawHeaderValueTests
{
    /// <summary>The header whose value the parser would reformat if it materialized it.</summary>
    private const string RawHeaderName = "Accept";

    /// <summary>The declared media type, with no space after the parameter separator.</summary>
    private const string RawHeaderValue = "application/vnd.api.json;version=3.4.1";

    /// <summary>Verifies a lone declared header reaches the request verbatim.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task SingleDeclaredHeaderKeepsItsValueVerbatim() =>
        AssertRawAcceptAsync(nameof(IMultipleStaticHeaderApi.OneHeader));

    /// <summary>Verifies a declared header reaches the request verbatim when a second header follows it.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task DeclaredHeaderKeepsItsValueVerbatimBesideASecondHeader() =>
        AssertRawAcceptAsync(nameof(IMultipleStaticHeaderApi.TwoHeaders));

    /// <summary>Builds a request for the named method and asserts its stored <c>Accept</c> value was never reformatted.</summary>
    /// <param name="methodName">The interface method to build.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertRawAcceptAsync(string methodName)
    {
        var builder = new RequestBuilderImplementation<IMultipleStaticHeaderApi>();

        using var request = await builder.BuildRequestFactoryForMethod(methodName)([]);

        // NonValidated reports what is actually stored, so it distinguishes the declared value from the parsed one.
        var stored = request.Headers.NonValidated.TryGetValues(RawHeaderName, out var values);

        await Assert.That(stored).IsTrue();
        await Assert.That(values.ToString()).IsEqualTo(RawHeaderValue);
    }
}
