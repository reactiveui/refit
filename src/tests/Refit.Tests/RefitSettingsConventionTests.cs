// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Tests for the <see cref="RefitSettings"/> naming-convention presets.</summary>
public class RefitSettingsConventionTests
{
    /// <summary>Verifies the camelCase preset serializes JSON body property names in camelCase.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task CamelCase_SerializesJsonBodyInCamelCase()
    {
        var content = await RefitSettings.CamelCase().ContentSerializer
            .ToHttpContent(new ConventionModel { MyProperty = "x" }).ReadAsStringAsync();

        await Assert.That(content).Contains("\"myProperty\"");
    }

    /// <summary>Verifies the snake_case preset serializes JSON body property names in snake_case.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SnakeCase_SerializesJsonBodyInSnakeCase()
    {
        var content = await RefitSettings.SnakeCase().ContentSerializer
            .ToHttpContent(new ConventionModel { MyProperty = "x" }).ReadAsStringAsync();

        await Assert.That(content).Contains("\"my_property\"");
    }

    /// <summary>Verifies the kebab-case preset serializes JSON body property names in kebab-case.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task KebabCase_SerializesJsonBodyInKebabCase()
    {
        var content = await RefitSettings.KebabCase().ContentSerializer
            .ToHttpContent(new ConventionModel { MyProperty = "x" }).ReadAsStringAsync();

        await Assert.That(content).Contains("\"my-property\"");
    }

    /// <summary>Verifies each preset wires up the matching URL parameter key formatter.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Presets_UseMatchingUrlParameterKeyFormatter()
    {
        await Assert.That(RefitSettings.CamelCase().UrlParameterKeyFormatter)
            .IsTypeOf<CamelCaseUrlParameterKeyFormatter>();
        await Assert.That(RefitSettings.SnakeCase().UrlParameterKeyFormatter)
            .IsTypeOf<SnakeCaseUrlParameterKeyFormatter>();
        await Assert.That(RefitSettings.KebabCase().UrlParameterKeyFormatter)
            .IsTypeOf<KebabCaseUrlParameterKeyFormatter>();
    }

    /// <summary>Model with a PascalCase property used to verify JSON body naming.</summary>
    private sealed class ConventionModel
    {
        /// <summary>Gets or sets a value whose serialized name reflects the chosen naming convention.</summary>
        public string? MyProperty { get; set; }
    }
}
