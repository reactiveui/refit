// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Request-generation coverage for return-type classification and custom HTTP method verb resolution.</summary>
public sealed partial class RequestGenerationCoverageTests
{
    /// <summary>The interface source exercising each statically-readable custom verb getter shape.</summary>
    private const string CustomVerbGetterShapesSource =
        """
        using System.Net.Http;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public abstract class ReportMethodBaseAttribute : HttpMethodAttribute
        {
            protected ReportMethodBaseAttribute(string path) : base(path) { }

            public override HttpMethod Method => new HttpMethod("REPORT");
        }

        public sealed class ReportAttribute : ReportMethodBaseAttribute
        {
            public ReportAttribute(string path) : base(path) { }
        }

        // The leaf shadows the inherited Method property with a same-named method, so property lookup skips it and
        // walks past to the base's readable Method override.
        public sealed class WalkAttribute : ReportMethodBaseAttribute
        {
            public WalkAttribute(string path) : base(path) { }

            public new void Method() { }
        }

        public sealed class PurgeAttribute : HttpMethodAttribute
        {
            public PurgeAttribute(string path) : base(path) { }

            public override HttpMethod Method { get => new HttpMethod("PURGE"); }
        }

        public sealed class TraceMethodAttribute : HttpMethodAttribute
        {
            public TraceMethodAttribute(string path) : base(path) { }

            public override HttpMethod Method { get { return new HttpMethod("TRACE"); } }
        }

        public interface IGeneratedClient
        {
            [Report("/report")] Task<string> Report();

            [Walk("/walk")] Task<string> Walk();

            [Purge("/purge")] Task<string> Purge();

            [TraceMethod("/trace")] Task<string> Trace();
        }
        """;

    /// <summary>Verifies the generic API response interface return type is classified as an API response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericApiResponseInterfaceReturnIsClassified()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/typed")]
                Task<IApiResponse<string>> Get();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a method returning a non-named type is parsed without inline generation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonNamedReturnTypeIsParsed()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/array")]
                string[] GetArray();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies the scalar classifier evaluates its full special-type pattern across representative types.</summary>
    /// <param name="parameterType">The path parameter type expression.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("int")]
    [Arguments("string")]
    [Arguments("System.DateTime")]
    [Arguments("System.Guid")]
    [Arguments("object")]
    public async Task ScalarClassifierEvaluatesSpecialTypePattern(string parameterType)
    {
        var source =
            $$"""
              using System;
              using System.Threading.Tasks;
              using Refit;

              namespace RefitGeneratorTest;

              public interface IGeneratedClient
              {
                  [Get("/items/{value}")]
                  Task<string> Get({{parameterType}} value);
              }
              """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies a Refit-namespaced result type that is not an API response is classified accordingly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RefitNonResponseReturnTypeIsNotClassifiedAsApiResponse()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/exception")]
                Task<ApiException> Get();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies the HTTP method attribute lookup returns null when no matching attribute is present.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FindHttpMethodAttributeReturnsNullWithoutHttpAttribute()
    {
        const string Source =
            """
            using System;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IApi
            {
                [Obsolete]
                Task<string> Tagged();

                Task<string> Bare();
            }
            """;

        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(Source));
        var httpMethodBase = compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute")!;
        var api = compilation.GetTypeByMetadataName("RefitGeneratorTest.IApi")!;
        var tagged = api.GetMembers("Tagged").OfType<IMethodSymbol>().First();
        var bare = api.GetMembers("Bare").OfType<IMethodSymbol>().First();

        await Assert.That(Parser.FindHttpMethodAttribute(tagged, httpMethodBase)).IsNull();
        await Assert.That(Parser.FindHttpMethodAttribute(bare, httpMethodBase)).IsNull();
    }

    /// <summary>Verifies an unbalanced brace segment makes a path template unsupported for inline generation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsPathSupportedRejectsUnbalancedBraceSegment()
    {
        await Assert.That(Parser.IsPathSupported("/root/{open/leaf}")).IsFalse();
        await Assert.That(Parser.IsPathSupported("/root/{closed}/leaf")).IsTrue();
    }

    /// <summary>Verifies custom HTTP method attributes whose <c>Method</c> getter is statically readable resolve their
    /// verbs inline: an inherited expression-bodied property, an expression-bodied accessor, and a block accessor.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CustomHttpMethodAttributeGetterShapesResolveVerbsInline()
    {
        var result = Fixture.RunGenerator(CustomVerbGetterShapesSource, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("new global::System.Net.Http.HttpMethod(\"REPORT\")");
        await Assert.That(generated).Contains("new global::System.Net.Http.HttpMethod(\"PURGE\")");
        await Assert.That(generated).Contains("new global::System.Net.Http.HttpMethod(\"TRACE\")");
    }

    /// <summary>Verifies custom HTTP method attributes whose verb is not statically readable fall back: a shadowing
    /// non-<c>HttpMethod</c> <c>Method</c> property, and an auto-property getter with no readable body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CustomHttpMethodAttributesWithUnreadableVerbsFallBack()
    {
        const string Source =
            """
            using System.Net.Http;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public abstract class ShadowBaseAttribute : HttpMethodAttribute
            {
                protected ShadowBaseAttribute(string path) : base(path) { }

                public override HttpMethod Method => new HttpMethod("SHADOW");
            }

            public sealed class ShadowAttribute : ShadowBaseAttribute
            {
                public ShadowAttribute(string path) : base(path) { }

                public new string Method => "SHADOW";
            }

            public sealed class OpaqueAttribute : HttpMethodAttribute
            {
                public OpaqueAttribute(string path) : base(path) { }

                public override HttpMethod Method { get; } = new HttpMethod("OPAQUE");
            }

            public interface IGeneratedClient
            {
                [Shadow("/shadow")] Task<string> Shadow();

                [Opaque("/opaque")] Task<string> Opaque();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }
}
