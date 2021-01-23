using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Refit; // InterfaceStubGenerator looks for this
using Refit.Generator;
using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Refit.Tests
{
    public class InterfaceStubGeneratorTests
    {
        [Fact(Skip = "Generator in test issue")]
        public void GenerateInterfaceStubsSmokeTest()
        {
            var fixture = new InterfaceStubGenerator();

            var driver = CSharpGeneratorDriver.Create(fixture);


            var inputCompilation = CreateCompilation(
                IntegrationTestHelper.GetPath("RestService.cs"),
                IntegrationTestHelper.GetPath("GitHubApi.cs"),
                IntegrationTestHelper.GetPath("InheritedInterfacesApi.cs"),
                IntegrationTestHelper.GetPath("InheritedGenericInterfacesApi.cs"));

            var rundriver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompiliation, out var diagnostics);
            
            var runResult = rundriver.GetRunResult();

            var generated = runResult.Results[0];

            var text = generated.GeneratedSources.First().SourceText.ToString();

            Assert.Contains("IGitHubApi", text);
            Assert.Contains("IAmInterfaceC", text);
        }

        static Compilation CreateCompilation(params string[] sourceFiles)
        {
            return CSharpCompilation.Create("compilation",
                sourceFiles.Select(source => CSharpSyntaxTree.ParseText(File.ReadAllText(source))),
                new[] { MetadataReference.CreateFromFile(typeof(GetAttribute).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        }


        [Fact(Skip = "Generator in test issue")]
        public void FindInterfacesSmokeTest()
        {
            var input = IntegrationTestHelper.GetPath("GitHubApi.cs");
            var fixture = new InterfaceStubGenerator();

            //var result = fixture.FindInterfacesToGenerate(CSharpSyntaxTree.ParseText(File.ReadAllText(input)));
            //Assert.Equal(3, result.Count);
            //Assert.Contains(result, x => x.Identifier.ValueText == "IGitHubApi");

            //input = IntegrationTestHelper.GetPath("InterfaceStubGenerator.cs");

            //result = fixture.FindInterfacesToGenerate(CSharpSyntaxTree.ParseText(File.ReadAllText(input)));
            //Assert.Equal(3, result.Count);
            //Assert.Contains(result, x => x.Identifier.ValueText == "IAmARefitInterfaceButNobodyUsesMe");
            //Assert.Contains(result, x => x.Identifier.ValueText == "IBoringCrudApi");
            //Assert.Contains(result, x => x.Identifier.ValueText == "INonGenericInterfaceWithGenericMethod");
            //Assert.True(result.All(x => x.Identifier.ValueText != "IAmNotARefitInterface"));

            Assert.False(true);
        }    
     

        [Fact(Skip ="Generator in test issue")]
        public void GenerateInterfaceStubsWithoutNamespaceSmokeTest()
        {
            var fixture = new InterfaceStubGenerator();

            var driver = CSharpGeneratorDriver.Create(fixture);

            var inputCompilation = CreateCompilation(IntegrationTestHelper.GetPath("IServiceWithoutNamespace.cs"));

            var runDriver = driver.RunGenerators(inputCompilation);

            var runResult = runDriver.GetRunResult();

            var generated = runResult.Results[0];

            var result = generated.GeneratedSources.Last().SourceText.ToString();

            Assert.Contains("IServiceWithoutNamespace", result);
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

    public interface IBoringCrudApi<T, in TKey> where T : class
    {
        [Post("")]
        Task<T> Create([Body] T paylod);

        [Get("")]
        Task<List<T>> ReadAll();

        [Get("/{key}")]
        Task<T> ReadOne(TKey key);

        [Put("/{key}")]
        Task Update(TKey key, [Body]T payload);

        [Delete("/{key}")]
        Task Delete(TKey key);
    }

    public interface INonGenericInterfaceWithGenericMethod
    {
        [Post("")]
        Task PostMessage<T>([Body] T message) where T : IMessage;

        [Post("")]
        Task PostMessage<T, U, V>([Body] T message, U param1, V param2) where T : IMessage where U : T;
    }

    public interface IMessage { }

}
