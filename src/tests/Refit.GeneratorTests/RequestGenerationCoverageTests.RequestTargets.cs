// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>Request-generation coverage for HTTP verb resolution and method-targeting branches.</summary>
public sealed partial class RequestGenerationCoverageTests
{
    /// <summary>Verifies every built-in HTTP verb attribute resolves its verb and generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AllStandardHttpVerbsResolveInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Delete("/a")] Task<string> Delete();
                [Get("/b")] Task<string> Get();
                [Head("/c")] Task<string> Head();
                [Options("/d")] Task<string> Options();
                [Patch("/e")] Task<string> Patch();
                [Post("/f")] Task<string> Post();
                [Put("/g")] Task<string> Put();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("HttpMethod.Delete");
        await Assert.That(generated).Contains("HttpMethod.Head");
    }

    /// <summary>Verifies a built-in HTTP verb attribute on a non-interface method is ignored by the syntax filter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HttpAttributeOnClassMethodIsIgnored()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public class NotAnInterface
            {
                [Get("/x")]
                public Task<string> Get() => Task.FromResult(string.Empty);
            }

            public interface IGeneratedClient
            {
                [Get("/y")]
                Task<string> Get();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
        await Assert.That(result.GeneratedSources.ContainsKey("NotAnInterface.g.cs")).IsFalse();
    }

    /// <summary>Verifies a built-in HTTP verb attribute applied only to non-interface methods produces no generated
    /// client at all, exercising the syntax filter's rejection with no interface candidate present.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HttpAttributeOnlyOnClassMethodsProducesNoClient()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public class OnlyClass
            {
                [Get("/x")]
                public Task<string> Get() => Task.FromResult(string.Empty);

                [Post("/y")]
                public Task<string> Post() => Task.FromResult(string.Empty);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey("OnlyClass.g.cs")).IsFalse();
    }

    /// <summary>Verifies an interface declared in the global namespace generates a stable stub inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GlobalNamespaceInterfaceGeneratesInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            public interface IGlobalClient
            {
                [Get("/global")]
                Task<string> Get();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources.ContainsKey("IGlobalClient.g.cs")).IsTrue();
    }

    /// <summary>Verifies a <c>[Property]</c> parameter that also carries <c>[Query]</c> but whose type cannot flatten
    /// inline falls the method back to the reflection builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PropertyAndQueryParameterOfUnflattenableTypeFallsBack()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/x")]
                Task<string> Get([Property("Trace")][Query] object value);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an <c>[Authorize(null)]</c> parameter falls back to the default authorization scheme.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AuthorizeWithNullSchemeUsesDefaultScheme()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a")]
                Task<string> Get([Authorize(null)] string token);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("\"Bearer \"");
    }
}
