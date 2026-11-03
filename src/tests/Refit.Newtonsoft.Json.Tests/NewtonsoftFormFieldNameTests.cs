// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit.Tests;

/// <summary>Verifies generated form-field descriptors resolve names the way the Newtonsoft serializer does.</summary>
public sealed class NewtonsoftFormFieldNameTests
{
    /// <summary>Verifies the Newtonsoft serializer reports the Newtonsoft name style.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SerializerReportsNewtonsoftNameStyle() =>
        await Assert.That(new NewtonsoftJsonContentSerializer().NameStyle).IsEqualTo(ContentSerializerNameStyle.Newtonsoft);

    /// <summary>Verifies the descriptor path picks the Newtonsoft name and agrees with the reflection path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DescriptorPathUsesNewtonsoftNameAndMatchesReflection()
    {
        var settings = new RefitSettings(new NewtonsoftJsonContentSerializer());
        var body = new NamedBody { Renamed = "r", Unnamed = "u", Aliased = "a", };
        var fields = new[]
        {
            new FormField<NamedBody>(static b => b.Renamed, nameof(NamedBody.Renamed), new(null, "json", "newtonsoft_name", null), null, null, null, false),
            new FormField<NamedBody>(static b => b.Unnamed, nameof(NamedBody.Unnamed), new(null, "json_unnamed", null, null), null, null, null, false),
            new FormField<NamedBody>(static b => b.Aliased, nameof(NamedBody.Aliased), new("alias", "json_aliased", "newtonsoft_aliased", null), null, null, null, false),
        };

        var descriptor = await GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, body, fields).ReadAsStringAsync();
        var reflection = await GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, body).ReadAsStringAsync();

        await Assert.That(descriptor).IsEqualTo("newtonsoft_name=r&Unnamed=u&alias=a");
        await Assert.That(descriptor).IsEqualTo(reflection);
    }

    /// <summary>Form body whose properties carry Newtonsoft naming attributes.</summary>
    private sealed class NamedBody
    {
        /// <summary>Gets or sets the property renamed through Newtonsoft.</summary>
        [JsonProperty("newtonsoft_name")]
        public string? Renamed { get; set; }

        /// <summary>Gets or sets the property with no Newtonsoft name.</summary>
        public string? Unnamed { get; set; }

        /// <summary>Gets or sets the property whose alias wins over the Newtonsoft name.</summary>
        [AliasAs("alias")]
        [JsonProperty("newtonsoft_aliased")]
        public string? Aliased { get; set; }
    }
}
