// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit.Tests;

/// <summary>Verifies the Newtonsoft serializer does not inherit an unsafe global <c>TypeNameHandling</c>.</summary>
[NotInParallel(nameof(NewtonsoftJsonSecurityTests))]
public sealed class NewtonsoftJsonSecurityTests
{
    /// <summary>Verifies a globally configured unsafe <c>TypeNameHandling</c> is forced off when no explicit settings are supplied.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage(
        "Security",
        "CA2326:Do not use TypeNameHandling values other than None",
        Justification = "The test intentionally configures an unsafe global value to prove the serializer neutralizes it.")]
    [SuppressMessage(
        "Security",
        "CA2327:Do not use insecure JsonSerializerSettings",
        Justification = "The test intentionally configures an unsafe global value to prove the serializer neutralizes it.")]
    public async Task GlobalTypeNameHandlingIsForcedOffWhenNoExplicitSettings()
    {
        var previous = JsonConvert.DefaultSettings;
        JsonConvert.DefaultSettings = static () => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        try
        {
            var serializer = new NewtonsoftJsonContentSerializer();

            var json = await serializer.ToHttpContent(new SampleModel { Name = "value" }).ReadAsStringAsync();

            await Assert.That(json).DoesNotContain("$type");
        }
        finally
        {
            JsonConvert.DefaultSettings = previous;
        }
    }

    /// <summary>Verifies a caller that explicitly opts into <c>TypeNameHandling</c> is honored.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage(
        "Security",
        "CA2326:Do not use TypeNameHandling values other than None",
        Justification = "The test intentionally opts into an unsafe value to prove explicit caller settings are honored.")]
    [SuppressMessage(
        "Security",
        "CA2327:Do not use insecure JsonSerializerSettings",
        Justification = "The test intentionally opts into an unsafe value to prove explicit caller settings are honored.")]
    public async Task ExplicitTypeNameHandlingIsHonored()
    {
        var serializer = new NewtonsoftJsonContentSerializer(
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

        var json = await serializer.ToHttpContent(new SampleModel { Name = "value" }).ReadAsStringAsync();

        await Assert.That(json).Contains("$type");
    }

    /// <summary>A simple model used to observe whether type metadata is emitted.</summary>
    private sealed class SampleModel
    {
        /// <summary>Gets or sets the name.</summary>
        public string? Name { get; set; }
    }
}
