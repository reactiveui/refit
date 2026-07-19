// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Direct unit tests for interface-member classification during parsing.</summary>
public static partial class GeneratorComponentTests
{
    /// <summary>Tests for the method return-type shape classifier.</summary>
    public class ReturnTypeClassificationTests
    {
        /// <summary>Verifies every recognized return-type shape classifies, including sync void and non-named returns.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task GetReturnTypeInfo_ClassifiesEveryShape()
        {
            var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(
                """
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

            await Assert.That(Parser.GetReturnTypeInfo(Method("SyncVoid"))).IsEqualTo(ReturnTypeInfo.SyncVoid);

            // A bare type parameter and an array are not INamedTypeSymbol, so they fall to the plain return shape.
            await Assert.That(Parser.GetReturnTypeInfo(Method("GenericReturn"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(Parser.GetReturnTypeInfo(Method("ArrayReturn"))).IsEqualTo(ReturnTypeInfo.Return);

            await Assert.That(Parser.GetReturnTypeInfo(Method("AsyncVoid"))).IsEqualTo(ReturnTypeInfo.AsyncVoid);
            await Assert.That(Parser.GetReturnTypeInfo(Method("AsyncResult"))).IsEqualTo(ReturnTypeInfo.AsyncResult);
            await Assert.That(Parser.GetReturnTypeInfo(Method("ValueResult"))).IsEqualTo(ReturnTypeInfo.AsyncResult);
            await Assert.That(Parser.GetReturnTypeInfo(Method("RequestMessage"))).IsEqualTo(ReturnTypeInfo.RequestMessage);
            await Assert.That(Parser.GetReturnTypeInfo(Method("AsyncEnumerableReturn"))).IsEqualTo(ReturnTypeInfo.AsyncEnumerable);
            await Assert.That(Parser.GetReturnTypeInfo(Method("Observable"))).IsEqualTo(ReturnTypeInfo.Observable);
            await Assert.That(Parser.GetReturnTypeInfo(Method("PlainNamed"))).IsEqualTo(ReturnTypeInfo.Return);

            // A wrapper simple-name at the wrong arity does not match its arm and falls to the plain return shape.
            await Assert.That(Parser.GetReturnTypeInfo(Method("NonGenericValueTask"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(Parser.GetReturnTypeInfo(Method("ObservableLookalike"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(Parser.GetReturnTypeInfo(Method("AsyncEnumerableLookalike"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(Parser.GetReturnTypeInfo(Method("TaskArityTwo"))).IsEqualTo(ReturnTypeInfo.Return);
            await Assert.That(Parser.GetReturnTypeInfo(Method("VoidArityOne"))).IsEqualTo(ReturnTypeInfo.Return);
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
            var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(
                """
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

            var names = Parser.CollectMemberNames(overloads.GetMembers());

            // The two "Do" overloads collapse to a single distinct name alongside "Other", in first-seen order.
            await Assert.That(names.Count).IsEqualTo(DistinctNameCount);
            await Assert.That(names.AsArray()).IsCollectionEqualTo(["Do", "Other"]);
        }
    }
}
