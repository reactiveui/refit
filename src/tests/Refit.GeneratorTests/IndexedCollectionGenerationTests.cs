// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>
/// Verifies that a <c>[Query(CollectionFormat.Indexed)]</c> parameter whose element type is a flattened object
/// generates inline (no reflection fallback) and emits the expected indexed-prefix query: <c>key[0].Prop=val</c>.
/// </summary>
public sealed class IndexedCollectionGenerationTests
{
    /// <summary>The generated implementation source hint name.</summary>
    private const string Hint = "IGeneratedClient.g.cs";

    /// <summary>The reflective request-builder call emitted by fallback paths.</summary>
    private const string ReflectiveFallback = "BuildRestResultFuncForMethod";

    /// <summary>Source with a nullable Indexed collection parameter.</summary>
    private const string NullableIndexedSource =
        """
        #nullable enable
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public sealed class Item
        {
            public int Id { get; set; }
            public string? Value { get; set; }
        }

        public interface IGeneratedClient
        {
            [Get("/items")]
            Task<string> Search([Query(CollectionFormat.Indexed)] IReadOnlyList<Item>? items);
        }
        """;

    /// <summary>Source with a non-nullable Indexed collection parameter (value-type element).</summary>
    private const string NonNullableIndexedSource =
        """
        #nullable enable
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public struct Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        public interface IGeneratedClient
        {
            [Get("/points")]
            Task<string> Plot([Query(CollectionFormat.Indexed)] List<Point> points);
        }
        """;

    /// <summary>Source where the element type has a nested object property - should fall back to reflection
    /// because the nested type is complex and the reflection builder walks runtime types.</summary>
    private const string IndexedWithSimpleScalarCollectionSource =
        """
        #nullable enable
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public sealed class Tag
        {
            public string? Name { get; set; }
            [Query(CollectionFormat.Multi)] public int[]? Ids { get; set; }
        }

        public interface IGeneratedClient
        {
            [Get("/tags")]
            Task<string> Search([Query(CollectionFormat.Indexed)] List<Tag> tags);
        }
        """;

    /// <summary>Source where the element type has an [AliasAs] property name override.</summary>
    private const string IndexedWithAliasSource =
        """
        #nullable enable
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public sealed class Filter
        {
            [AliasAs("n")] public string? Name { get; set; }
            public int Count { get; set; }
        }

        public interface IGeneratedClient
        {
            [Get("/filters")]
            Task<string> Search([Query(CollectionFormat.Indexed)] List<Filter>? filters);
        }
        """;

    /// <summary>Verifies a nullable Indexed collection parameter flattens inline with no reflection fallback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullableIndexedCollectionGeneratesInline()
    {
        var result = Fixture.RunGenerator(NullableIndexedSource, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies a non-nullable Indexed collection of value-type elements generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonNullableIndexedCollectionGeneratesInline()
    {
        var result = Fixture.RunGenerator(NonNullableIndexedSource, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies a Indexed collection whose element type has a scalar collection property flattens inline
    /// (the collection property on the element uses the settings-default CollectionFormat).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IndexedWithElementCollectionPropertyGeneratesInline()
    {
        var result = Fixture.RunGenerator(IndexedWithSimpleScalarCollectionSource, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies a Indexed collection whose element type has an [AliasAs] property generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IndexedWithAliasedPropertyGeneratesInline()
    {
        var result = Fixture.RunGenerator(IndexedWithAliasSource, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies the generated source contains indexed key expressions like <c>"items[" + idx + "]"</c>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedSourceContainsIndexedKeyExpression()
    {
        var result = Fixture.RunGenerator(NullableIndexedSource, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[Hint];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();

        await Assert.That(generated).Contains("items[{");
    }

    /// <summary>Verifies a Indexed parameter whose element type is a scalar (not complex) generates inline
    /// as a regular <c>Collection</c> shape, not the indexed-object expansion.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IndexedWithScalarElementGeneratesAsRegularCollection()
    {
        const string source =
            """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/v")]
                Task<string> Values([Query(CollectionFormat.Indexed)] List<int> values);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();

        // A scalar element has no object properties to flatten — handled as regular Collection inline generation.
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies a Indexed parameter whose element type is a collection with a complex element type falls back to reflective generation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>    
    [Test]
    public async Task IndexedWithComplexIndexElementFallsBackToReflective()
    {
        const string source =
            """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Car
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }

            public sealed class ComplexElement
            {
                public List<Car>? Cars { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/v")]
                Task<string> Values([Query(CollectionFormat.Indexed)] List<ComplexElement> values);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();

        // A scalar element has no object properties to flatten — handled as regular Collection inline generation.
        await Assert.That(result.GeneratedSources[Hint]).Contains(ReflectiveFallback);
    }
}
