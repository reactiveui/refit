using System.Collections.Immutable;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

using Refit.Generator;

using VerifyTests.DiffPlex;

using Task = System.Threading.Tasks.Task;

namespace Refit.Tests;

public class InterfaceStubGeneratorTests
{
    static readonly MetadataReference RefitAssembly = MetadataReference.CreateFromFile(
        typeof(GetAttribute).Assembly.Location,
        documentation: XmlDocumentationProvider.CreateFromFile(
            Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")
        )
    );

    static readonly ReferenceAssemblies ReferenceAssemblies;

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
            ImmutableArray.Create(new PackageIdentity("System.Text.Json", "7.0.2"))
        );
#endif

#if NET48
        ReferenceAssemblies = ReferenceAssemblies
            .AddAssemblies(ImmutableArray.Create("System.Web"))
            .AddPackages(ImmutableArray.Create(new PackageIdentity("System.Net.Http", "4.3.4")));
#endif
    }

    public static async Task<VerifyResult> VerifyGenerator(string input)
    {
        var assemblies = await ReferenceAssemblies.ResolveAsync(null, default);

        string[] inputs = [input];
        var compilation = CSharpCompilation.Create(
            "compilation",
            inputs.Select(source => CSharpSyntaxTree.ParseText(File.ReadAllText(source))),
            assemblies.Add(RefitAssembly),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );

        var generator = new InterfaceStubGeneratorV2();
        var driver = CSharpGeneratorDriver.Create(generator);
        var ranDriver = driver.RunGenerators(compilation);

        return await Verify(ranDriver).ToTask();
    }

    [Test]
    public async Task NoRefitInterfacesSmokeTest()
    {
        var path = IntegrationTestHelper.GetPath("IInterfaceWithoutRefit.cs");
        await VerifyGenerator(path);
    }

    [Test]
    public async Task FindInterfacesSmokeTest()
    {
        var path = IntegrationTestHelper.GetPath("GitHubApi.cs");
        await VerifyGenerator(path);
    }

    [Test]
    public async Task GenerateInterfaceStubsWithoutNamespaceSmokeTest()
    {
        var path = IntegrationTestHelper.GetPath("IServiceWithoutNamespace.cs");
        await VerifyGenerator(path);
    }
}

public static class ThisIsDumbButMightHappen
{
    public const string PeopleDoWeirdStuff = "But we don't let them";
}

public interface IAmARefitInterfaceButNobodyUsesMe
{
    [Get("whatever")]
    Task RefitMethod();

    [Refit.GetAttribute("something-else")]
    Task AnotherRefitMethod();

    [Get(ThisIsDumbButMightHappen.PeopleDoWeirdStuff)]
    Task NoConstantsAllowed();

    [Get("spaces-shouldnt-break-me")]
    Task SpacesShouldntBreakMe();

    // We don't need an explicit test for this because if it isn't supported we can't compile
    [Get("anything")]
    Task ReservedWordsForParameterNames(int @int, string @string, float @long);
}

public interface IAmNotARefitInterface
{
    Task NotARefitMethod();
}

public interface IBoringCrudApi<T, in TKey>
    where T : class
{
    [Post("")]
    Task<T> Create([Body] T paylod);

    [Get("")]
    Task<List<T>> ReadAll();

    [Get("/{key}")]
    Task<T> ReadOne(TKey key);

    [Put("/{key}")]
    Task Update(TKey key, [Body] T payload);

    [Delete("/{key}")]
    Task Delete(TKey key);
}

public interface INonGenericInterfaceWithGenericMethod
{
    [Post("")]
    Task PostMessage<T>([Body] T message)
        where T : IMessage;

    [Post("")]
    Task PostMessage<T, U, V>([Body] T message, U param1, V param2)
        where T : IMessage
        where U : T;
}

public interface IMessage;
