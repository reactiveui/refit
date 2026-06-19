// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

using Refit.Generator;

using Task = System.Threading.Tasks.Task;

namespace Refit.Tests;

/// <summary>Verifies the Refit interface stub source generator against known fixture interfaces.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class InterfaceStubGeneratorTests
{
    /// <summary>The Refit assembly metadata reference used when compiling generator inputs.</summary>
    [UnconditionalSuppressMessage(
        "SingleFile",
        "IL3000:Avoid accessing Assembly file path when publishing as a single file",
        Justification = "Test compiles generator inputs against the on-disk Refit assembly; never run as a single-file app.")]
    private static readonly MetadataReference RefitAssembly = MetadataReference.CreateFromFile(
        typeof(GetAttribute).Assembly.Location,
        documentation: XmlDocumentationProvider.CreateFromFile(
            Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")));

    /// <summary>The reference assemblies for the target framework under test.</summary>
    private static readonly ReferenceAssemblies ReferenceAssemblies;

    /// <summary>Initializes static members of the <see cref="InterfaceStubGeneratorTests"/> class.</summary>
    static InterfaceStubGeneratorTests()
    {
#if NET6_0
        ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
#elif NET8_0
        ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
#elif NET9_0
        ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
#else
        ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(
            [new PackageIdentity("System.Text.Json", "7.0.2")]);
#endif

#if NET48
        ReferenceAssemblies = ReferenceAssemblies
            .AddAssemblies(ImmutableArray.Create("System.Web"))
            .AddPackages(ImmutableArray.Create(new PackageIdentity("System.Net.Http", "4.3.4")));
#endif
    }

    /// <summary>Runs the interface stub generator over the supplied source file and verifies the output.</summary>
    /// <param name="input">The path to the source file to feed to the generator.</param>
    /// <returns>The snapshot verification result for the generated output.</returns>
    public static async Task<VerifyResult> VerifyGenerator(string input)
    {
        var assemblies = await ReferenceAssemblies.ResolveAsync(null, default);

        string[] inputs = [input];
        var compilation = CSharpCompilation.Create(
            "compilation",
            inputs.Select(source => CSharpSyntaxTree.ParseText(File.ReadAllText(source))),
            assemblies.Add(RefitAssembly),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

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
        var path = IntegrationTestHelper.GetPath("IInterfaceWithoutRefit.cs");
        await VerifyGenerator(path);
    }

    /// <summary>Verifies the generator discovers and processes Refit interfaces in a source file.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task FindInterfacesSmokeTest()
    {
        var path = IntegrationTestHelper.GetPath("GitHubApi.cs");
        await VerifyGenerator(path);
    }

    /// <summary>Verifies the generator handles a Refit interface declared without a namespace.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateInterfaceStubsWithoutNamespaceSmokeTest()
    {
        var path = IntegrationTestHelper.GetPath("IServiceWithoutNamespace.cs");
        await VerifyGenerator(path);
    }
}
