// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Refit.Generator;

using Task = System.Threading.Tasks.Task;

namespace Refit.GeneratorTests;

/// <summary>Verifies the Refit interface stub source generator against known fixture interfaces.</summary>
public class InterfaceStubGeneratorTests
{
    /// <summary>Runs the interface stub generator over the supplied source file and verifies the output.</summary>
    /// <param name="input">The path to the source file to feed to the generator.</param>
    /// <returns>The snapshot verification result for the generated output.</returns>
    public static async Task<VerifyResult> VerifyGenerator(string input)
    {
        var source = await File.ReadAllTextAsync(input);
        var compilation = Fixture.CreateLibrary(
            CSharpSyntaxTree.ParseText(source));

        var generator = new InterfaceStubGeneratorV2();
        var driver = CSharpGeneratorDriver.Create(generator);
        var ranDriver = driver.RunGenerators(compilation);

        return await Verify(ranDriver).ToTask();
    }

    /// <summary>Verifies the generator produces no output for a source file with no Refit interfaces.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NoRefitInterfacesSmokeTest()
    {
        var path = GetFixturePath("IInterfaceWithoutRefit.cs");
        await VerifyGenerator(path);
    }

    /// <summary>Verifies the generator discovers and processes Refit interfaces in a source file.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task FindInterfacesSmokeTest()
    {
        var path = GetFixturePath("GitHubApi.cs");
        await VerifyGenerator(path);
    }

    /// <summary>Verifies the generator handles a Refit interface declared without a namespace.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateInterfaceStubsWithoutNamespaceSmokeTest()
    {
        var path = GetFixturePath("IServiceWithoutNamespace.cs");
        await VerifyGenerator(path);
    }

    /// <summary>Gets the path to a source fixture owned by the runtime test project.</summary>
    /// <param name="paths">The fixture path parts.</param>
    /// <returns>The absolute fixture path.</returns>
    private static string GetFixturePath(params string[] paths)
    {
        var generatorTestProjectDirectory = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var runtimeTestProjectDirectory = Path.GetFullPath(
            Path.Combine(generatorTestProjectDirectory, "..", "Refit.Tests"));
        return Path.Combine([runtimeTestProjectDirectory, .. paths]);
    }
}
