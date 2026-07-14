// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;

namespace Refit.Tests;

/// <summary>Flattens a query object whose properties span the classifier's simple and formattable types.</summary>
public sealed class QueryObjectPropertyTypeClassificationTests
{
    /// <summary>The nullable-number value flattened by the classification fixture.</summary>
    private const int OptionalNumberValue = 42;

    /// <summary>The plain-number value flattened by the classification fixture.</summary>
    private const int NumberValue = 7;

    /// <summary>Each classified property type is flattened into the query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ClassifiedPropertyTypesAreFlattenedIntoQuery()
    {
        var filters = new QueryObjectWithClassifiedTypes
        {
            Text = "hello",
            Flag = true,
            Symbol = 'x',
            Link = new("http://example/resource"),
            Culture = CultureInfo.InvariantCulture,
            OptionalNumber = OptionalNumberValue,
            Number = NumberValue
        };

        var fixture = new RequestBuilderImplementation<IQueryObjectTypesApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IQueryObjectTypesApi.Search));
        var output = await factory([filters]);

        var query = output.RequestUri!.Query;

        await Assert.That(query).Contains("Text=hello");
        await Assert.That(query).Contains("Flag=True");
        await Assert.That(query).Contains("Symbol=x");
        await Assert.That(query).Contains("Link=");
        await Assert.That(query).Contains("Culture=");
        await Assert.That(query).Contains("OptionalNumber=42");
        await Assert.That(query).Contains("Number=7");
    }

    /// <summary>A null nullable value-type property is omitted from the query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullNullablePropertyIsOmitted()
    {
        var filters = new QueryObjectWithClassifiedTypes
        {
            Text = "hello",
            Flag = false,
            Symbol = 'y',
            OptionalNumber = null,
            Number = 1
        };

        var fixture = new RequestBuilderImplementation<IQueryObjectTypesApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IQueryObjectTypesApi.Search));
        var output = await factory([filters]);

        var query = output.RequestUri!.Query;

        await Assert.That(query).Contains("Number=1");
        await Assert.That(query).DoesNotContain("OptionalNumber");
    }
}
