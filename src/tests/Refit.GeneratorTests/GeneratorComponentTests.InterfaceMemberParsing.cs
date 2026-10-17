// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Direct unit tests for interface-member classification during parsing.</summary>
public static partial class GeneratorComponentTests
{
    /// <summary>Tests consumer options applied to parsed interface metadata.</summary>
    public class InterfaceSemanticParsingTests
    {
        /// <summary>Verifies an inherited-only tree is parsed once and retains its inherited request bindings.</summary>
        /// <param name="cancellationToken">The test cancellation token.</param>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task ParseInheritedInterface_FirstCandidate_CollectsAllDerivedInterfaces(CancellationToken cancellationToken)
        {
            const int DerivedInterfaceCount = 2;
            var parentTree = CSharpSyntaxTree.ParseText(
                """
                using System.Threading.Tasks;
                using Refit;
                public interface IParent
                {
                    [Get("/items")]
                    Task<string> Get();
                }
                """,
                cancellationToken: cancellationToken);
            var derivedTree = CSharpSyntaxTree.ParseText(
                """
                #nullable enable
                public interface IUnrelated : System.IDisposable { }
                public interface IFirst : IParent { }
                public interface ISecond : IParent { }
                """,
                cancellationToken: cancellationToken);
            var compilation = Fixture.CreateLibrary(parentTree, derivedTree);
            var model = compilation.GetSemanticModel(derivedTree);
            var declarations = (await derivedTree.GetRootAsync(cancellationToken)).DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToArray();
            var first = declarations.Single(static declaration => declaration.Identifier.ValueText == "IFirst");
            var second = declarations.Single(static declaration => declaration.Identifier.ValueText == "ISecond");

            var result = InterfaceSemanticParsing.ParseInheritedInterface(model, first, cancellationToken);

            await Assert.That(result.Length).IsEqualTo(DerivedInterfaceCount);
            await Assert.That(result.Select(static item => item.FileName)).IsCollectionEqualTo(["IFirst.g.cs", "ISecond.g.cs"]);
            await Assert.That(Array.TrueForAll(result, static item => item.Members.DerivedRefitMethods.Length == 1)).IsTrue();
            await Assert.That(InterfaceSemanticParsing.ParseInheritedInterface(model, second, cancellationToken)).IsEmpty();

            var reflection = InterfaceSemanticParsing.ApplyGeneratorOptions(result, new(null, false, false, false), "Consumer", cancellationToken);
            await Assert.That(reflection.Interfaces.SelectMany(static item => item.Members.DerivedRefitMethods)
                .All(static method => method.Request.Equals(RequestModel.Empty))).IsTrue();
            await Assert.That(result.SelectMany(static item => item.Members.DerivedRefitMethods)
                .All(static method => method.Request.Generation.CanGenerateInline)).IsTrue();
        }

        /// <summary>Verifies unrelated interface inheritance produces no generation candidates.</summary>
        /// <param name="cancellationToken">The test cancellation token.</param>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task ParseInheritedInterface_NoRefitMethods_ReturnsEmpty(CancellationToken cancellationToken)
        {
            var tree = CSharpSyntaxTree.ParseText("public interface IUnrelated : System.IDisposable { }", cancellationToken: cancellationToken);
            var compilation = Fixture.CreateLibrary(tree);
            var declaration = (await tree.GetRootAsync(cancellationToken)).DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single();

            var result = InterfaceSemanticParsing.ParseInheritedInterface(compilation.GetSemanticModel(tree), declaration, cancellationToken);

            await Assert.That(result).IsEmpty();
        }

        /// <summary>Verifies directly inspecting an ordinary interface method produces no generation candidates.</summary>
        /// <param name="cancellationToken">The test cancellation token.</param>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task ParseMethodInterface_NoHttpAttribute_ReturnsEmpty(CancellationToken cancellationToken)
        {
            var tree = CSharpSyntaxTree.ParseText("public interface IUnrelated { void Get(); }", cancellationToken: cancellationToken);
            var compilation = Fixture.CreateLibrary(tree);
            var declaration = (await tree.GetRootAsync(cancellationToken)).DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            var result = InterfaceSemanticParsing.ParseMethodInterface(compilation.GetSemanticModel(tree), declaration, cancellationToken);

            await Assert.That(result).IsEmpty();
        }

        /// <summary>Verifies an empty input retains consumer options without referring to a preserve attribute.</summary>
        /// <param name="generatedRequests">Whether inline requests are enabled.</param>
        /// <param name="emitMarkers">Whether generated-code markers are enabled.</param>
        /// <param name="cancellationToken">The test cancellation token.</param>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        [Arguments(false, false)]
        [Arguments(true, true)]
        public async Task ApplyGeneratorOptions_EmptyInterfaces_PreservesOptions(bool generatedRequests, bool emitMarkers, CancellationToken cancellationToken)
        {
            const string AssemblyName = "EmptyConsumer";
            var result = InterfaceSemanticParsing.ApplyGeneratorOptions(
                [],
                new(null, generatedRequests, emitMarkers, false),
                AssemblyName,
                cancellationToken);

            await Assert.That(result.Interfaces).IsEmpty();
            await Assert.That(result.PreserveAttributeDisplayName).IsEqualTo(string.Empty);
            await Assert.That(result.RefitInternalNamespace).IsEqualTo(GeneratedTypeNaming.BuildRefitInternalNamespace(null, AssemblyName));
            await Assert.That(result.GeneratedClassName).IsEqualTo(GeneratedTypeNaming.BuildGeneratedContainerName(AssemblyName));
            await Assert.That(result.GeneratedRequestBuilding).IsEqualTo(generatedRequests);
            await Assert.That(result.EmitGeneratedCodeMarkers).IsEqualTo(emitMarkers);
        }
    }

    /// <summary>Tests for the method return-type shape classifier.</summary>
    public class ReturnTypeClassificationTests
    {
        /// <summary>Verifies every recognized return-type shape classifies, including sync void and non-named returns.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task GetReturnTypeInfo_ClassifiesEveryShape()
        {
            var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("""
                using System;
                using System.Collections.Generic;
                using System.Net.Http;
                using System.Threading.Tasks;

                namespace Lookalikes
                {
                    // Same simple names as the async wrappers but a different arity; the classifier keys on
                    // (Name, Arity) only, so these exercise the arity-mismatch arms.
                    public interface IObservable { }
                    public interface IAsyncEnumerable { }
                    public sealed class Task<TFirst, TSecond> { }
                    public sealed class Void<TItem> { }
                }

                namespace ReturnShapes
                {
                    public interface IShapes<TItem>
                    {
                        void SyncVoid();
                        TItem GenericReturn();
                        int[] ArrayReturn();
                        Task AsyncVoid();
                        Task<string> AsyncResult();
                        ValueTask<int> ValueResult();
                        ValueTask NonGenericValueTask();
                        Task<HttpRequestMessage> RequestMessage();
                        IAsyncEnumerable<int> AsyncEnumerableReturn();
                        IObservable<int> Observable();
                        Lookalikes.IObservable ObservableLookalike();
                        Lookalikes.IAsyncEnumerable AsyncEnumerableLookalike();
                        Lookalikes.Task<int, string> TaskArityTwo();
                        Lookalikes.Void<int> VoidArityOne();
                        string PlainNamed();
                    }
                }
                """));
            var shapes = compilation.GetTypeByMetadataName("ReturnShapes.IShapes`1")!;
            IMethodSymbol Method(string name) => shapes.GetMembers(name).OfType<IMethodSymbol>().First();
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("SyncVoid"))).IsEqualTo(ReturnTypeInfo.SyncVoid);

            // A bare type parameter and an array are not INamedTypeSymbol, so they fall to the plain return shape.
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("GenericReturn"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("ArrayReturn"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("AsyncVoid"))).IsEqualTo(ReturnTypeInfo.AsyncVoid);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("AsyncResult"))).IsEqualTo(ReturnTypeInfo.AsyncResult);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("ValueResult"))).IsEqualTo(ReturnTypeInfo.AsyncResult);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("RequestMessage"))).IsEqualTo(ReturnTypeInfo.RequestMessage);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("AsyncEnumerableReturn"))).IsEqualTo(ReturnTypeInfo.AsyncEnumerable);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("Observable"))).IsEqualTo(ReturnTypeInfo.Observable);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("PlainNamed"))).IsEqualTo(ReturnTypeInfo.Return);

            // A wrapper simple-name at the wrong arity does not match its arm and falls to the plain return shape.
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("NonGenericValueTask"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("ObservableLookalike"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("AsyncEnumerableLookalike"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("TaskArityTwo"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(Method("VoidArityOne"))).IsEqualTo(ReturnTypeInfo.Return);
        }
    }

    /// <summary>Tests for distinct interface member-name collection.</summary>
    public class MemberNameCollectionTests
    {
        /// <summary>Verifies member-name collection deduplicates overloaded members while preserving first-seen order.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task CollectMemberNames_DeduplicatesOverloadedMembers()
        {
            const int DistinctNameCount = 2;
            var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("""
                using System.Threading.Tasks;

                namespace Members;

                public interface IOverloads
                {
                    Task Do();
                    Task Do(int value);
                    Task Other();
                }
                """));
            var overloads = compilation.GetTypeByMetadataName("Members.IOverloads")!;
            var names = InterfaceMemberParsing.CollectMemberNames(overloads.GetMembers());

            // The two "Do" overloads collapse to a single distinct name alongside "Other", in first-seen order.
            await Assert.That(names.Count).IsEqualTo(DistinctNameCount);
            await Assert.That(names).IsCollectionEqualTo(["Do", "Other"]);
        }
    }
}
