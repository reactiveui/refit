// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Verifies which path-parameter types generated request building supports inline.</summary>
public sealed class PathParameterTypeTests
{
    /// <summary>The generated client hint name.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The reflection request-builder call emitted when a method falls back.</summary>
    private const string ReflectiveRequestBuilderCall = "BuildRestResultFuncForMethod";

    /// <summary>Verifies scalar path-parameter types (string/bool plus every IFormattable) generate inline.</summary>
    /// <param name="parameterType">The path parameter type expression.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("string")]
    [Arguments("bool")]
    [Arguments("char")]
    [Arguments("sbyte")]
    [Arguments("byte")]
    [Arguments("int")]
    [Arguments("long")]
    [Arguments("double")]
    [Arguments("decimal")]
    [Arguments("System.Guid")]
    [Arguments("System.DateTime")]
    [Arguments("System.DateTimeOffset")]
    [Arguments("System.DateOnly")]
    [Arguments("System.DateOnly?")]
    [Arguments("System.TimeOnly")]
    [Arguments("System.TimeSpan")]
    [Arguments("System.Int128")]
    [Arguments("System.Half")]
    [Arguments("System.DayOfWeek")]
    public async Task ScalarPathParameterGeneratesInline(string parameterType)
    {
        var generated = Generate($"[Get(\"/items/{{value}}\")] Task<string> Get({parameterType} value);");

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies non-scalar path-parameter types fall back to the reflection request builder.</summary>
    /// <param name="parameterType">The path parameter type expression.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("byte[]")]
    [Arguments("int[]")]
    [Arguments("System.Collections.Generic.List<int>")]
    public async Task NonScalarPathParameterFallsBack(string parameterType)
    {
        var generated = Generate($"[Get(\"/items/{{value}}\")] Task<string> Get({parameterType} value);");

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an enum with duplicate constants as a path parameter renders through the URL parameter
    /// formatter (no reflection-free fast path) while still generating inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DuplicateEnumPathParameterUsesFormatter()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum Duplicated { First = 1, Alias = 1 }

            public interface IGeneratedClient
            {
                [Get("/items/{mode}")]
                Task<string> Get(Duplicated mode);
            }
            """;

        var generated = Fixture.RunGenerator(source, generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Runs the generator over an interface body and returns the generated client source.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>The generated client source text.</returns>
    private static string Generate(string body) =>
        Fixture.RunGenerator(BuildSource(body), generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

    /// <summary>Wraps an interface body in a compilable Refit client source.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>The full source string.</returns>
    private static string BuildSource(string body) =>
        $$"""
          using System;
          using System.Threading.Tasks;
          using Refit;

          namespace RefitGeneratorTest;

          public interface IGeneratedClient
          {
          {{body}}
          }
          """;
}
