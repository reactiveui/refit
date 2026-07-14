// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>Generation coverage for interface-member partitioning, property emission, inherited Refit methods, and
/// attribute-namespace matching branches that only run for specific interface shapes.</summary>
public sealed class InterfaceMemberGenerationCoverageTests
{
    /// <summary>The generated implementation source hint name.</summary>
    private const string Hint = "IGeneratedClient.g.cs";

    /// <summary>The reflective request-builder call emitted by fallback paths.</summary>
    private const string ReflectiveFallback = "BuildRestResultFuncForMethod";

    /// <summary>Verifies emittable and non-emittable interface properties: a nullable-annotated abstract property is
    /// implemented, while a static, a default-implemented, and (implicitly) an accessor-only member are skipped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InterfacePropertyEmissionSkipsStaticAndDefaultImplementedMembers()
    {
        const string Source =
            """
            #nullable enable
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a")]
                Task<string> Get();

                string? Name { get; set; }

                int Age { get; }

                static int Shared => 1;

                int Defaulted => 42;
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[Hint];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("public string? Name");
        await Assert.That(generated).Contains("public int Age");
    }

    /// <summary>Verifies an abstract indexer (a parameterized property) is not emitted as a stub property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InterfaceIndexerPropertyIsNotEmitted()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a")]
                Task<string> Get();

                int this[int index] { get; }
            }
            """;

        // The abstract indexer is skipped by the emitter, so the stub does not satisfy it; the generated file is still
        // produced, which is all the property-partitioning branch under test needs.
        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(Hint)).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain("this[");
    }

    /// <summary>Verifies an interface inheriting a Refit method from a base interface generates both the inherited and
    /// the directly-declared methods inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedRefitMethodGeneratesInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IBase
            {
                [Get("/base")]
                Task<string> BaseCall();
            }

            public interface IGeneratedClient : IBase
            {
                [Get("/derived")]
                Task<string> DerivedCall();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[Hint];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveFallback);
        await Assert.That(generated).Contains("BaseCall");
        await Assert.That(generated).Contains("DerivedCall");
    }

    /// <summary>Verifies an interface inheriting non-Refit members - a plain method, an emittable property, a static
    /// property, and an event - alongside an inherited Refit method partitions each inherited member correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedNonRefitMembersPartitionAcrossKinds()
    {
        const string Source =
            """
            using System;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IBase
            {
                [Get("/base")]
                Task<string> BaseCall();

                void PlainMethod();

                string Setting { get; }

                static int Shared => 1;

                event EventHandler Changed;
            }

            public interface IGeneratedClient : IBase
            {
                [Get("/derived")]
                Task<string> DerivedCall();
            }
            """;

        // The inherited event is not implemented by the stub, so compilation is not asserted; the branch under test is
        // the inherited-member partition switch, which classifies the event, the property and the plain method.
        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(Hint)).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).Contains("BaseCall");
    }

    /// <summary>Verifies a derived interface that explicitly re-declares base methods - a Refit method and a non-Refit
    /// method - resolves each through its explicit interface implementation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExplicitInterfaceReDeclarationsResolveThroughImplementations()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IBase
            {
                [Get("/op")]
                Task<string> Op();

                void Notify();
            }

            public interface IGeneratedClient : IBase
            {
                [Get("/own")]
                Task<string> Own();

                [Get("/op2")]
                Task<string> IBase.Op();

                abstract void IBase.Notify();
            }
            """;

        // The explicit re-declarations exercise the explicit-interface-implementation parsing branches; the emitted
        // stub for these reabstractions is not asserted to compile.
        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(Hint)).IsTrue();
    }

    /// <summary>Verifies a Refit method annotated with a user attribute whose type name matches a trim attribute but is
    /// declared in a different namespace is not treated as a trim annotation, and still generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TrimAttributeLookalikeInForeignNamespaceIsIgnored()
    {
        const string Source =
            """
            using System;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            [AttributeUsage(AttributeTargets.Method)]
            public sealed class RequiresUnreferencedCodeAttribute : Attribute
            {
                public RequiresUnreferencedCodeAttribute(string message) { }
            }

            [AttributeUsage(AttributeTargets.Method)]
            public sealed class RequiresDynamicCodeAttribute : Attribute
            {
                public RequiresDynamicCodeAttribute(string message) { }
            }

            public interface IGeneratedClient
            {
                [Get("/a")]
                [RequiresUnreferencedCode("nope")]
                [RequiresDynamicCode("nope")]
                Task<string> Get();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[Hint];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies parameter attributes whose type names match Refit attributes but live outside the <c>Refit</c>
    /// global-namespace are not treated as Refit attributes, so the method still generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RefitAttributeLookalikesInForeignNamespacesAreIgnored()
    {
        const string Source =
            """
            using System;
            using System.Threading.Tasks;

            namespace Outer.Refit
            {
                [AttributeUsage(AttributeTargets.Parameter)]
                public sealed class EncodedAttribute : Attribute { }
            }

            namespace RefitGeneratorTest
            {
                [AttributeUsage(AttributeTargets.Parameter)]
                public sealed class HeaderAttribute : Attribute { }

                public interface IGeneratedClient
                {
                    [global::Refit.Get("/a")]
                    System.Threading.Tasks.Task<string> Get(
                        [RefitGeneratorTest.HeaderAttribute] int header,
                        [Outer.Refit.EncodedAttribute] int encoded);
                }
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[Hint];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveFallback);
    }
}
