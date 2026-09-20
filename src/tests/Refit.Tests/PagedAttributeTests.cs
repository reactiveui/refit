// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit.Tests;

/// <summary>Verifies the settings a <see cref="PagedAttribute"/> and <see cref="PageTokenAttribute"/> carry.</summary>
public class PagedAttributeTests
{
    /// <summary>The member path the attributes name.</summary>
    private const string NextPath = "Content.Next";

    /// <summary>Verifies every setting written on the attribute is read back.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PagedAttribute_ExposesItsSettings()
    {
        var attribute = new PagedAttribute
        {
            Items = "Content.Items",
            Next = NextPath,
            NextHeader = "Link",
            Total = "Content.Total",
            Origins = ["https://api.example.com"],
            SameOrigin = true,
            AnyOrigin = true,
        };

        using (Assert.Multiple())
        {
            await Assert.That(attribute.Items).IsEqualTo("Content.Items");
            await Assert.That(attribute.Next).IsEqualTo(NextPath);
            await Assert.That(attribute.NextHeader).IsEqualTo("Link");
            await Assert.That(attribute.Total).IsEqualTo("Content.Total");
            await Assert.That(attribute.Origins).IsEquivalentTo(["https://api.example.com"]);
            await Assert.That(attribute.SameOrigin).IsTrue();
            await Assert.That(attribute.AnyOrigin).IsTrue();
        }
    }

    /// <summary>Verifies the attributes on a generated client's interface are read with the values they were declared with.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DeclaredAttributes_CarryTheirNamedArguments()
    {
        var method = typeof(IGeneratedPagingApi).GetMethod(nameof(IGeneratedPagingApi.ListedOriginLinkItems))!;
        var paged = method.GetCustomAttribute<PagedAttribute>()!;
        var token = typeof(IGeneratedPagingApi).GetMethods()
            .First(static candidate => candidate.Name == nameof(IGeneratedPagingApi.HeaderItems))
            .GetParameters()[0]
            .GetCustomAttribute<PageTokenAttribute>();

        using (Assert.Multiple())
        {
            await Assert.That(paged.Next).IsEqualTo(NextPath);
            await Assert.That(paged.Origins).IsEquivalentTo([PagedApiHandler.Origin]);
            await Assert.That(paged.SameOrigin).IsFalse();
            await Assert.That(token).IsNotNull();
        }
    }
}
