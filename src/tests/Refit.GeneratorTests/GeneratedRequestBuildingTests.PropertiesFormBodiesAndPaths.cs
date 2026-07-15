// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests for interface property implementation, URL-encoded form-body descriptors, and path and query request construction.</summary>
public partial class GeneratedRequestBuildingTests
{
    /// <summary>Verifies property attributes on interface properties are implemented and passed into generated requests.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsInterfacePropertyRequestProperty()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                [Property("tenant")]
                int TenantId { get; set; }

                [Get("/users")]
                Task<string> Get();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("public int TenantId {  get; set; }");
        await Assert.That(generated).Contains("GeneratedRequestRunner.AddRequestProperty<int>(refitRequest, \"tenant\", this.TenantId)");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a get-only HttpClient Client interface property is satisfied by the generated infrastructure property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DoesNotReemitGeneratedClientProperty()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                HttpClient Client { get; }

                [Get("/users")]
                Task<string> Get();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        var clientPropertyCount = generated.Split("public global::System.Net.Http.HttpClient Client").Length - 1;
        await Assert.That(clientPropertyCount).IsEqualTo(1);
    }

    /// <summary>Verifies inherited non-Refit interface properties are implemented explicitly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ImplementsInheritedRegularInterfaceProperty()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IBaseApi
            {
                string BaseUri { get; set; }
            }

            public interface IGeneratedClient : IBaseApi
            {
                [Get("/users")]
                Task<string> Get();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("global::IBaseApi.BaseUri");
        await Assert.That(generated).DoesNotContain("Either this method has no Refit HTTP method attribute");
    }

    /// <summary>Verifies inherited generated-client properties and methods are emitted through explicit interfaces.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsInheritedPropertiesMethodsAndDispose()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IBaseApi : IDisposable
            {
                [Property("base-tenant")]
                int BaseTenant { get; }

                string Name { set; }

                [Get("/base")]
                Task<string> GetBase();

                string Helper<T>(T value)
                    where T : class, new();
            }

            public interface IGeneratedClient : IBaseApi
            {
                [Get("/users")]
                Task<string> Get();

                string IBaseApi.Helper<T>(T value) => string.Empty;
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("global::IBaseApi.BaseTenant");
        await Assert.That(generated).Contains("global::IBaseApi.Name");
        await Assert.That(generated).Contains("global::IBaseApi.GetBase()");
        await Assert.That(generated).Contains("void global::System.IDisposable.Dispose()");
        await Assert.That(generated).DoesNotContain("global::IBaseApi.Helper");
    }

    /// <summary>Verifies a URL-encoded form body emits reflection-free generated field descriptors.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlEncodedFormBodyEmitsGeneratedFieldDescriptors()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using System.Text.Json.Serialization;
            using Refit;

            namespace RefitGeneratorTest;

            public class LoginForm
            {
                [AliasAs("user_name")]
                public string UserName { get; set; }

                [JsonPropertyName("pwd")]
                public string Password { get; set; }

                [Query(SerializeNull = true)]
                public string Note { get; set; }

                [Query(CollectionFormat.Multi)]
                public List<string> Roles { get; set; }
            }

            public interface IGeneratedClient
            {
                [Post("/login")]
                Task Login([Body(BodySerializationMethod.UrlEncoded)] LoginForm form);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains(
            "global::Refit.FormField<global::RefitGeneratorTest.LoginForm>[]");
        await Assert.That(generated).Contains(
            "static body => (object?)body.@UserName");
        await Assert.That(generated).Contains("\"user_name\"");
        await Assert.That(generated).Contains("\"pwd\"");
        await Assert.That(generated).Contains("(global::Refit.CollectionFormat)");
        await Assert.That(generated).Contains(
            "global::Refit.GeneratedRequestRunner.CreateUrlEncodedBodyContent<global::RefitGeneratorTest.LoginForm>(");
    }

    /// <summary>Verifies special characters in a form field alias are escaped in the generated descriptor literal.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlEncodedFormBodyEscapesSpecialCharactersInFieldNames()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public class QuotedForm
            {
                [AliasAs("a\"b\\c")]
                public string Value { get; set; }
            }

            public interface IGeneratedClient
            {
                [Post("/login")]
                Task Login([Body(BodySerializationMethod.UrlEncoded)] QuotedForm form);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("\"a\\\"b\\\\c\"");
    }

    /// <summary>Verifies a string-typed form body keeps the reflection-based content path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlEncodedStringBodyKeepsReflectionContentPath()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Post("/login")]
            Task Login([Body(BodySerializationMethod.UrlEncoded)] string form);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(
            "global::Refit.GeneratedRequestRunner.CreateUrlEncodedBodyContent<string>(");
        await Assert.That(generated).DoesNotContain("global::Refit.FormField<");
    }

    /// <summary>Verifies non-flattenable form body types keep the reflection-based content path.</summary>
    /// <param name="bodyType">The ineligible body type expression.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("object")]
    [Arguments("System.Collections.Generic.Dictionary<string, string>")]
    [Arguments("System.Collections.Generic.List<string>")]
    [Arguments("int[]")]
    [Arguments("System.IDisposable")]
    public async Task UrlEncodedIneligibleBodyKeepsReflectionContentPath(string bodyType)
    {
        var generated = Fixture.GenerateForBody(
            $$"""
            [Post("/x")]
            Task Post([Body(BodySerializationMethod.UrlEncoded)] {{bodyType}} body);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(
            "global::Refit.GeneratedRequestRunner.CreateUrlEncodedBodyContent<");
        await Assert.That(generated).DoesNotContain("global::Refit.FormField<");
    }

    /// <summary>Verifies form field descriptors include inherited properties and apply the query prefix.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlEncodedFormBodyEmitsInheritedAndPrefixedFieldDescriptors()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public class BaseForm
            {
                public string BaseValue { get; set; }
            }

            public class DerivedForm : BaseForm
            {
                [Query("-", "secret")]
                public string Token { get; set; }
            }

            public interface IGeneratedClient
            {
                [Post("/login")]
                Task Login([Body(BodySerializationMethod.UrlEncoded)] DerivedForm form);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("@form.@Token");
        await Assert.That(generated).Contains("@form.@BaseValue");
        await Assert.That(generated).Contains("\"secret-\"");
    }

    /// <summary>Verifies that path parameters are supported by the source generator.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmitsInlineConstructionForPathParameters()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a/{aVal}")]
                Task Sample(int aVal);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("""GeneratedRequestRunner.BuildRequestPath("/a/{aVal}", refitSettings.AllowUnmatchedRouteParameters, [((3, 9), """);
    }

    /// <summary>Verifies that path parameters are supported by the source generator.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UsesGeneratedRequestBuilderForTemplatedQueryParameters()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a?b={bVal}")]
                Task Sample(string bVal);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("""GeneratedRequestRunner.BuildRequestPath("/a?b={bVal}", refitSettings.AllowUnmatchedRouteParameters, [((5, 11), """);
    }

    /// <summary>Verifies that auto-appended query parameters generate inline query construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmitsInlineConstructionForNonTemplatedQueryParameters()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a")]
                Task Sample(string bVal);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("new global::Refit.GeneratedQueryStringBuilder(\"/a\", false)");
        await Assert.That(generated).Contains("refitQueryBuilder.AddPreEscapedKey(\"bVal\"");
    }

    /// <summary>Verifies that round trip path parameters are not supported by the source generator.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratesInlineForRoundTripPathPlaceholders()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a/{**path}")]
                Task Sample(string path);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("RoundTripEscapePath");
    }

    /// <summary>Verifies an interface-level <c>[PathPrefix]</c> is baked into each method's emitted route template.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsInterfacePathPrefixInRoute()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            [PathPrefix("/api/v2")]
            public interface IGeneratedClient
            {
                [Get("/users/{id}")]
                Task<string> GetUser(int id);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("/api/v2/users/");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies the client interface's <c>[PathPrefix]</c> is applied to a method inherited from a base interface.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsPathPrefixOnInheritedMethod()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IBaseApi
            {
                [Get("/ping")]
                Task<string> Ping();
            }

            [PathPrefix("/api/v2")]
            public interface IGeneratedClient : IBaseApi
            {
                [Get("/own")]
                Task<string> Own();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        // The derived prefix applies to both the derived method and the inherited base method, and the two prefixes
        // are never concatenated.
        await Assert.That(generated).Contains("/api/v2/ping");
        await Assert.That(generated).Contains("/api/v2/own");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }
}
