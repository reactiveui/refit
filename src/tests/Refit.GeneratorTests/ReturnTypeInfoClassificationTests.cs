// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Verifies every return type shape the generator classifies.</summary>
public sealed class ReturnTypeInfoClassificationTests
{
    /// <summary>The source declaring one method per return type shape.</summary>
    private const string Source =
        """
        using System;
        using System.Collections.Generic;
        using System.Net.Http;
        using System.Threading.Tasks;

        namespace Shapes
        {
            public class PagedEnumerable<TPage, TItem>
            {
            }

            public interface IShapes
            {
                Task AsyncVoid();
                Task<int> AsyncResult();
                Task<HttpRequestMessage> RequestMessage();
                ValueTask<int> ValueResult();
                IAsyncEnumerable<int> AsyncEnumerable();
                IObservable<int> Observable();
                PagedEnumerable<int, int> LookAlikePaged();
                Refit.PagedEnumerable<int, int> Paged();
                void SyncVoid();
                int Plain();
                int[] Array();
                Func<int, int, int> ThreeArguments();
            }
        }
        """;

    /// <summary>Verifies each declared shape maps to the expected classification.</summary>
    /// <param name="methodName">The method whose return type is classified.</param>
    /// <param name="expected">The expected classification.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments("AsyncVoid", "AsyncVoid")]
    [Arguments("AsyncResult", "AsyncResult")]
    [Arguments("RequestMessage", "RequestMessage")]
    [Arguments("ValueResult", "AsyncResult")]
    [Arguments("AsyncEnumerable", "AsyncEnumerable")]
    [Arguments("Observable", "Observable")]
    [Arguments("LookAlikePaged", "Return")]
    [Arguments("Paged", "Paged")]
    [Arguments("SyncVoid", "SyncVoid")]
    [Arguments("Plain", "Return")]
    [Arguments("Array", "Return")]
    [Arguments("ThreeArguments", "Return")]
    public async Task ReturnTypeShapesAreClassified(string methodName, string expected)
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(Source));
        var method = (IMethodSymbol)compilation.GetTypeByMetadataName("Shapes.IShapes")!.GetMembers(methodName)[0];

        await Assert.That(InterfaceMemberParsing.GetReturnTypeInfo(method)).IsEqualTo(Enum.Parse<ReturnTypeInfo>(expected));
    }
}
