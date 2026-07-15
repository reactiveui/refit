// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>Request-generation coverage for url-encoded and unrolled form-body emission branches.</summary>
public sealed partial class RequestGenerationCoverageTests
{
    /// <summary>Verifies every constructor and named argument shape of a form-body query attribute is parsed.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlEncodedFormBodyCoversAllQueryShapes()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public class SignupForm
            {
                [Query("d", "p", "fmt")]
                public string WithFormat { get; set; }

                [Query(CollectionFormat.Multi)]
                public List<string> Roles { get; set; }

                [Query(Format = "F2")]
                public double Amount { get; set; }

                [Query(CollectionFormat = CollectionFormat.Csv)]
                public List<int> Ids { get; set; }

                [Query(SerializeNull = true)]
                public string Note { get; set; }

                [Query(TreatAsString = true)]
                public string Treated { get; set; }

                [AliasAs(null)]
                public string Aliased { get; set; }

                public static string Ignored { get; set; }

                public string this[int index] => index.ToString();

                private string Secret { get; set; }

                public string HiddenGetter { private get; set; }

                public string WriteOnly { set { } }
            }

            public interface IGeneratedClient
            {
                [Post("/signup")]
                Task Signup([Body(BodySerializationMethod.UrlEncoded)] SignupForm form);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains(
            "global::Refit.FormField<global::RefitGeneratorTest.SignupForm>[]");
        await Assert.That(generated).Contains("body.@WithFormat");
        await Assert.That(generated).DoesNotContain("body.@Secret");
        await Assert.That(generated).DoesNotContain("body.@Ignored");
    }

    /// <summary>Verifies an unrolled scalar form body emits the empty-value branch for a nullable
    /// <c>[Query(SerializeNull = true)]</c> field, alongside an unconditionally added value-type field.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnrolledFormBodyEmitsSerializeNullEmptyBranch()
    {
        const string Source =
            """
            #nullable enable
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class NoteForm
            {
                public int Count { get; set; }

                [Query(SerializeNull = true)]
                public string? Note { get; set; }
            }

            public interface IGeneratedClient
            {
                [Post("/notes")]
                Task Submit([Body(BodySerializationMethod.UrlEncoded)] NoteForm form);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("string.Empty");
    }

    /// <summary>Verifies an unrolled form body whose only field renders through the formatter (an enum with duplicate
    /// constants) declares no default-form-formatting branch and still generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnrolledFormBodyWithFormatterOnlyFieldGeneratesInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum Duplicated { First = 1, Alias = 1 }

            public sealed class ModeForm
            {
                public Duplicated Mode { get; set; }
            }

            public interface IGeneratedClient
            {
                [Post("/modes")]
                Task Submit([Body(BodySerializationMethod.UrlEncoded)] ModeForm form);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies form bodies of otherwise-ineligible type kinds keep the reflection content path.</summary>
    /// <param name="signature">The method signature exercising a specific ineligible body type kind.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("Task Post<TBody>([Body(BodySerializationMethod.UrlEncoded)] TBody body);")]
    [Arguments("Task Post<T>([Body(BodySerializationMethod.UrlEncoded)] System.Collections.Generic.List<T> body);")]
    [Arguments("Task Post([Body(BodySerializationMethod.UrlEncoded)] dynamic body);")]
    [Arguments("Task Post([Body(BodySerializationMethod.UrlEncoded)] MissingBodyType body);")]
    [Arguments("Task Post([Body(BodySerializationMethod.UrlEncoded)] int* body);")]
    public async Task IneligibleFormBodyTypeKindsUseReflectionContent(string signature)
    {
        var source =
            $$"""
              using System.Threading.Tasks;
              using Refit;

              namespace RefitGeneratorTest;

              public interface IGeneratedClient
              {
                  [Post("/x")]
                  {{signature}}
              }
              """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).DoesNotContain("global::Refit.FormField<");
    }

    /// <summary>Verifies a form body carrying a nested object, dictionary, or complex-element collection property routes
    /// the whole body through the reflection content path (which flattens recursively) instead of emitting descriptors.</summary>
    /// <param name="property">The complex property declaration forcing the reflection fallback.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("public Nested Detail { get; set; }")]
    [Arguments("public System.Collections.Generic.Dictionary<string, string> Extra { get; set; }")]
    [Arguments("public System.Collections.Generic.List<Nested> Items { get; set; }")]
    public async Task ComplexFormBodyPropertyUsesReflectionContent(string property)
    {
        var source =
            $$"""
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public class Nested
            {
                public string Email { get; set; }
            }

            public class ComplexForm
            {
                public string Name { get; set; }
                {{property}}
            }

            public interface IGeneratedClient
            {
                [Post("/f")]
                Task Submit([Body(BodySerializationMethod.UrlEncoded)] ComplexForm form);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains(
            "global::Refit.GeneratedRequestRunner.CreateUrlEncodedBodyContent<global::RefitGeneratorTest.ComplexForm>(");
        await Assert.That(generated).DoesNotContain("global::Refit.FormField<");
    }

    /// <summary>Verifies url-encoded form bodies covering nullable collection entries and escaped field names.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlEncodedFormBodyCoversNullableCollectionAndEscapedNames()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Form
            {
                public List<string?>? Tags { get; set; }
                [AliasAs("we\"ird\\name")] public string? Odd { get; set; }
                public string? Plain { get; set; }
            }

            public interface IGeneratedClient
            {
                [Post("/f")]
                Task Submit([Body(BodySerializationMethod.UrlEncoded)] Form form);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
    }
}
