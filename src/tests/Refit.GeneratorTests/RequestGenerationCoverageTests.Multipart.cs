// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>Request-generation coverage for inline multipart content emission branches.</summary>
public sealed partial class RequestGenerationCoverageTests
{
    /// <summary>Verifies a multipart method with statically-dispatchable parts builds its content inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartMethodGeneratesInline()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload([AliasAs("file")] IEnumerable<StreamPart> streams);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("new global::System.Net.Http.MultipartFormDataContent(\"----MyGreatBoundary\")");
    }

    /// <summary>Verifies a multipart method with an <c>object</c>-typed part still falls back to the reflective builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartMethodWithObjectPartFallsBack()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload(object payload);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a multipart method with an <see cref="System.Net.Http.HttpContent"/> part adds the content directly and generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartHttpContentPartGeneratesInline()
    {
        const string Source =
            """
            using System.Net.Http;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload([AliasAs("content")] HttpContent content);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains(".Add(@content)");
    }

    /// <summary>Verifies a <c>[Query]</c> parameter inside a multipart method feeds the query string rather than a part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartQueryParameterFeedsQueryStringInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload([Query] string filter, [AliasAs("file")] StreamPart part);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("MultipartFormDataContent");
    }

    /// <summary>Verifies multipart parts classified through the single-value and reference-enumerable element paths:
    /// a nullable formattable, an array of strings, an enumerable of byte arrays, and a list of strings.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartEnumerableAndNullableElementPartsGenerateInline()
    {
        const string Source =
            """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/a")]
                Task<string> UploadNullableId([AliasAs("id")] Guid? id);

                [Multipart]
                [Post("/b")]
                Task<string> UploadTags([AliasAs("tag")] string[] tags);

                [Multipart]
                [Post("/c")]
                Task<string> UploadBlobs([AliasAs("blob")] IEnumerable<byte[]> blobs);

                [Multipart]
                [Post("/d")]
                Task<string> UploadNames([AliasAs("name")] List<string> names);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an explicit <c>[Multipart(null)]</c> boundary argument falls to the attribute default boundary.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartNullBoundaryArgumentUsesDefaultBoundary()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart(null)]
                [Post("/upload")]
                Task<string> Upload([AliasAs("file")] StreamPart part);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("new global::System.Net.Http.MultipartFormDataContent(\"----MyGreatBoundary\")");
    }

    /// <summary>Verifies an obsolete <c>[AttachmentName]</c> override supplies a multipart part's file name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartAttachmentNameOverridesFileName()
    {
        // AttachmentNameAttribute is obsolete but still parsed by the generator; it lives inside the generator-input
        // source string, so no obsolete warning reaches the test project itself.
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload([AliasAs("blob")][AttachmentName("custom.bin")] byte[] data);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("\"custom.bin\"");
    }

    /// <summary>Verifies multipart parts whose declared type is not statically dispatchable fall back to the reflection
    /// builder: a <c>[Query]</c> object whose shape cannot flatten, a reference enumerable of an undispatchable element,
    /// a value-type array, and a value-type enumerable.</summary>
    /// <param name="body">The interface member body declaring the undispatchable part.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("[Multipart][Post(\"/a\")] Task<string> A([Query] object filter, [AliasAs(\"file\")] StreamPart part);")]
    [Arguments("[Multipart][Post(\"/b\")] Task<string> B([AliasAs(\"o\")] System.Collections.Generic.IEnumerable<object> objs);")]
    [Arguments("[Multipart][Post(\"/c\")] Task<string> C([AliasAs(\"n\")] int[] nums);")]
    [Arguments("[Multipart][Post(\"/d\")] Task<string> D([AliasAs(\"n\")] System.Collections.Generic.IEnumerable<int> nums);")]
    public async Task MultipartUndispatchablePartsFallBack(string body)
    {
        var source =
            $$"""
              using System.Threading.Tasks;
              using Refit;

              namespace RefitGeneratorTest;

              public interface IGeneratedClient
              {
                  {{body}}
              }
              """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an opt-in <c>[FormObject]</c> multipart parameter routes the whole method to the reflection
    /// request builder for every model shape of the parameter type (the attribute, not the type, triggers the
    /// fallback), which flattens the object's properties into individual form-data parts.</summary>
    /// <param name="typeDeclaration">The model type declaration inserted into the generated source, if any.</param>
    /// <param name="parameterType">The <c>[FormObject]</c> parameter's declared type.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("public sealed class FormModel { public string? Name { get; set; } }", "FormModel")]
    [Arguments("public class FormModel { public string? Name { get; set; } }", "FormModel")]
    [Arguments("public struct FormModel { public string? Name { get; set; } }", "FormModel")]
    [Arguments("public interface IFormModel { string? Name { get; } }", "IFormModel")]
    [Arguments("public record FormModel(string? Name);", "FormModel")]
    [Arguments("", "object")]
    [Arguments("", "System.Collections.Generic.Dictionary<string, string>")]
    public async Task MultipartFormObjectPartFallsBackForAnyModelShape(string typeDeclaration, string parameterType)
    {
        var source =
            $$"""
              using System.Threading.Tasks;
              using Refit;

              namespace RefitGeneratorTest;

              {{typeDeclaration}}

              public interface IGeneratedClient
              {
                  [Multipart]
                  [Post("/upload")]
                  Task<string> Upload([FormObject] {{parameterType}} model);
              }
              """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an opt-in <c>[FormObject]</c> parameter on an open generic method also falls back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartFormObjectGenericParameterFallsBack()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload<TModel>([FormObject] TModel model);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies the same concrete model part without <c>[FormObject]</c> is serialized inline, confirming the
    /// attribute is the sole trigger for the reflection fallback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartConcreteModelPartWithoutFormObjectGeneratesInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class FormModel
            {
                public string? Name { get; set; }
            }

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload(FormModel model);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("MultipartFormDataContent");
    }
}
