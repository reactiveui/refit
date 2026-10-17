// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Tests the ordering and empty-input behavior of method candidate merging.</summary>
public sealed class InterfaceStubGeneratorV2Tests
{
    /// <summary>Verifies every HTTP verb contributes its candidates in provider order.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CombineStandardHttpMethodCandidates_PreservesVerbAndDeclarationOrder(CancellationToken cancellationToken)
    {
        var tree = CSharpSyntaxTree.ParseText(
            """
            interface IApi
            {
                void DeleteFirst();
                void DeleteSecond();
                void Get();
                void Head();
                void Options();
                void Patch();
                void Post();
                void Put();
            }
            """,
            cancellationToken: cancellationToken);
        var root = await tree.GetRootAsync(cancellationToken);
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        var candidates = new InterfaceStubGeneratorV2.StandardHttpMethodCandidates(
            [methods[0], methods[1]],
            [methods[2]],
            [methods[3]],
            [methods[4]],
            [methods[5]],
            [methods[6]],
            [methods[7]]);

        var result = InterfaceStubGeneratorV2.CombineStandardHttpMethodCandidates(candidates);

        await Assert.That(result.SequenceEqual(methods)).IsTrue();
    }

    /// <summary>Verifies merging empty verb providers produces an initialized empty array.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CombineStandardHttpMethodCandidates_EmptyProviders_ReturnsEmptyArray()
    {
        var candidates = new InterfaceStubGeneratorV2.StandardHttpMethodCandidates([], [], [], [], [], [], []);

        var result = InterfaceStubGeneratorV2.CombineStandardHttpMethodCandidates(candidates);

        await Assert.That(result.IsDefault).IsFalse();
        await Assert.That(result.IsEmpty).IsTrue();
    }

    /// <summary>Verifies an empty side returns the other immutable array unchanged.</summary>
    /// <param name="standardIsEmpty">Whether the built-in candidates are empty.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task CombineCandidateMethods_OneEmptySide_ReturnsOtherArray(bool standardIsEmpty)
    {
        var method = (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration("void Fetch();")!;
        ImmutableArray<MethodDeclarationSyntax> methods = [method];

        var result = InterfaceStubGeneratorV2.CombineCandidateMethods(
            standardIsEmpty ? ([], methods) : (methods, []));

        await Assert.That(result.Equals(methods)).IsTrue();
        await Assert.That(result[0]).IsSameReferenceAs(method);
    }

    /// <summary>Verifies two empty inputs produce an initialized empty array.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CombineCandidateMethods_BothEmpty_ReturnsEmptyArray()
    {
        var result = InterfaceStubGeneratorV2.CombineCandidateMethods(([], []));

        await Assert.That(result.IsDefault).IsFalse();
        await Assert.That(result.IsEmpty).IsTrue();
    }

    /// <summary>Verifies merging retains standard-first order and repeated declarations.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CombineCandidateMethods_NonEmptyInputs_PreservesOrderAndDuplicates()
    {
        var standard = (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration("void Fetch();")!;
        var custom = (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration("void CustomFetch();")!;

        var result = InterfaceStubGeneratorV2.CombineCandidateMethods(([standard], [custom, standard]));

        const int ExpectedCandidateCount = 3;
        await Assert.That(result.Length).IsEqualTo(ExpectedCandidateCount);
        await Assert.That(result[0]).IsSameReferenceAs(standard);
        await Assert.That(result[1]).IsSameReferenceAs(custom);
        await Assert.That(result[2]).IsSameReferenceAs(standard);
    }
}
