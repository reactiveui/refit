// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests request option initialization through grouped settings.</summary>
public sealed class RefitRequestsSettingsTests
{
    /// <summary>Verifies grouped options retain the supplied dictionary, including null.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HttpRequestMessageOptions_Initializer_RetainsDictionary()
    {
        const string TraceOption = "trace";
        Dictionary<string, object> options = new() { [TraceOption] = "request" };
        var settings = new RefitRequestsSettings { HttpRequestMessageOptions = options };
        var withoutOptions = new RefitRequestsSettings { HttpRequestMessageOptions = null };

        await Assert.That(settings.HttpRequestMessageOptions).IsSameReferenceAs(options);
        options[TraceOption] = "updated";
        await Assert.That(settings.HttpRequestMessageOptions).IsSameReferenceAs(options);
        await Assert.That(settings.HttpRequestMessageOptions).IsEquivalentTo(new Dictionary<string, object> { [TraceOption] = "updated" });
        await Assert.That(withoutOptions.HttpRequestMessageOptions).IsNull();
    }
}
