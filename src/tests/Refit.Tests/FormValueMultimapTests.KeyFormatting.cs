// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Linq;

namespace Refit.Tests;

/// <summary>Tests pinning that field-name resolution stays sensitive to the per-call key formatter even though
/// attribute metadata is cached by source type.</summary>
public partial class FormValueMultimapTests
{
    /// <summary>Verifies field-name resolution honors the per-call key formatter for the same source type across
    /// repeated construction, so caching attribute metadata by type never freezes settings-dependent key formatting.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task KeyFormattingHonorsSettingsAcrossRepeatedConstructionForSameType()
    {
        var source = new MultiWordNameForm { FirstName = "Ada" };
        var snakeSettings = new RefitSettings
        {
            UrlParameterKeyFormatter = new SnakeCaseUrlParameterKeyFormatter()
        };

        var defaultFirst = new FormValueMultimap(source, _settings).Keys.ToArray();
        var snake = new FormValueMultimap(source, snakeSettings).Keys.ToArray();
        var defaultSecond = new FormValueMultimap(source, _settings).Keys.ToArray();

        await Assert.That(defaultFirst).Contains("FirstName");
        await Assert.That(snake).Contains("first_name");
        await Assert.That(defaultSecond).Contains("FirstName");
    }

    /// <summary>Test fixture with a multi-word property name that a key formatter rewrites, used to verify
    /// settings-dependent key formatting is applied per call rather than cached by type.</summary>
    public class MultiWordNameForm
    {
        /// <summary>Gets or sets the multi-word property whose key differs under a snake_case formatter.</summary>
        public string? FirstName { get; set; }
    }
}
