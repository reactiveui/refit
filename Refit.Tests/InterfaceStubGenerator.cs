using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

using Refit.Generator;

using Xunit;

using Task = System.Threading.Tasks.Task;
using VerifyCS = Refit.Tests.CSharpSourceGeneratorVerifier<Refit.Generator.InterfaceStubGenerator>;
using VerifyCSV2 = Refit.Tests.CSharpIncrementalSourceGeneratorVerifier<Refit.Generator.InterfaceStubGeneratorV2>;

namespace Refit.Tests
{
    public class InterfaceStubGeneratorTests
    {
        static readonly MetadataReference RefitAssembly = MetadataReference.CreateFromFile(
            typeof(GetAttribute).Assembly.Location,
            documentation: XmlDocumentationProvider.CreateFromFile(Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")));

        static readonly ReferenceAssemblies ReferenceAssemblies;

        static InterfaceStubGeneratorTests()
        {
#if NET5_0
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
#elif NET6_0
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
#else
            ReferenceAssemblies = ReferenceAssemblies.Default
                .AddPackages(ImmutableArray.Create(new PackageIdentity("System.Text.Json", "6.0.1")));
#endif

#if NET461
            ReferenceAssemblies = ReferenceAssemblies
                .AddAssemblies(ImmutableArray.Create("System.Web"))
                .AddPackages(ImmutableArray.Create(new PackageIdentity("System.Net.Http", "4.3.4")));
#endif
        }

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

            var diags = inputCompilation.GetDiagnostics();

            // Make sure we don't have any errors
            Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));

            var rundriver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompiliation, out var diagnostics);

            var runResult = rundriver.GetRunResult();

            var generated = runResult.Results[0];

            var text = generated.GeneratedSources.First().SourceText.ToString();

            Assert.Contains("IGitHubApi", text);
            Assert.Contains("IAmInterfaceC", text);
        }

        static Compilation CreateCompilation(params string[] sourceFiles)
        {
            var keyReferences = new[]
            {
                typeof(Binder),
                typeof(GetAttribute),
                typeof(RichardSzalay.MockHttp.MockHttpMessageHandler),
                typeof(System.Reactive.Unit),
                typeof(System.Linq.Enumerable),
                typeof(Newtonsoft.Json.JsonConvert),
                typeof(Xunit.FactAttribute),
                typeof(System.Net.Http.HttpContent),
                typeof(ModelObject),
                typeof(Attribute)
            };


            return CSharpCompilation.Create("compilation",
                sourceFiles.Select(source => CSharpSyntaxTree.ParseText(File.ReadAllText(source))),
                                   keyReferences.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location)),
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        }

        [Fact]
        public async Task NoRefitInterfacesSmokeTest()
        {
            var input = File.ReadAllText(IntegrationTestHelper.GetPath("IInterfaceWithoutRefit.cs"));

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies,
                TestState =
                {
                    AdditionalReferences = { RefitAssembly },
                    Sources = { input },
                },
            }.RunAsync();

            await new VerifyCSV2.Test
            {
                ReferenceAssemblies = ReferenceAssemblies,
                TestState =
                {
                    AdditionalReferences = { RefitAssembly },
                    Sources = { input },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task FindInterfacesSmokeTest()
        {
            var input = File.ReadAllText(IntegrationTestHelper.GetPath("GitHubApi.cs"));

            var output1 = @"
#pragma warning disable
namespace RefitInternalGenerated
{
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.AttributeUsage (global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct | global::System.AttributeTargets.Enum | global::System.AttributeTargets.Constructor | global::System.AttributeTargets.Method | global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Event | global::System.AttributeTargets.Interface | global::System.AttributeTargets.Delegate)]
    sealed class PreserveAttribute : global::System.Attribute
    {
        //
        // Fields
        //
        public bool AllMembers;

        public bool Conditional;
    }
}
#pragma warning restore
";

            var output1_5 = @"
#pragma warning disable
namespace Refit.Implementation
{

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [global::RefitInternalGenerated.PreserveAttribute]
    [global::System.Reflection.Obfuscation(Exclude=true)]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static partial class Generated
    {
    }
}
#pragma warning restore
";

            var output2 = @"#nullable disable
#pragma warning disable
namespace Refit.Implementation
{

    partial class Generated
    {

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [global::RefitInternalGenerated.PreserveAttribute]
    [global::System.Reflection.Obfuscation(Exclude=true)]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    partial class RefitTestsIGitHubApi
        : global::Refit.Tests.IGitHubApi

    {
        /// <inheritdoc />
        public global::System.Net.Http.HttpClient Client { get; }
        readonly global::Refit.IRequestBuilder requestBuilder;

        /// <inheritdoc />
        public RefitTestsIGitHubApi(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
        {
            Client = client;
            this.requestBuilder = requestBuilder;
        }
    


        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.Tests.User> GetUser(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUser"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserObservable(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserObservable"", new global::System.Type[] { typeof(string) } );
            return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserCamelCase(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserCamelCase"", new global::System.Type[] { typeof(string) } );
            return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> GetOrgMembers(string @orgName, global::System.Threading.CancellationToken @cancellationToken) 
        {
            var ______arguments = new object[] { @orgName, @cancellationToken };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetOrgMembers"", new global::System.Type[] { typeof(string), typeof(global::System.Threading.CancellationToken) } );
            return (global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> FindUsers(string @q) 
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""FindUsers"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> GetIndex() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetIndex"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.IObservable<string> GetIndexObservable() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetIndexObservable"", new global::System.Type[] {  } );
            return (global::System.IObservable<string>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.Tests.User> NothingToSeeHere() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""NothingToSeeHere"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> NothingToSeeHereWithMetadata() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""NothingToSeeHereWithMetadata"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> GetUserWithMetadata(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserWithMetadata"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>> GetUserObservableWithMetadata(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserObservableWithMetadata"", new global::System.Type[] { typeof(string) } );
            return (global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.Tests.User> CreateUser(global::Refit.Tests.User @user) 
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""CreateUser"", new global::System.Type[] { typeof(global::Refit.Tests.User) } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> CreateUserWithMetadata(global::Refit.Tests.User @user) 
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""CreateUserWithMetadata"", new global::System.Type[] { typeof(global::Refit.Tests.User) } );
            return (global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.GetUser(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUser"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.GetUserObservable(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserObservable"", new global::System.Type[] { typeof(string) } );
            return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.GetUserCamelCase(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserCamelCase"", new global::System.Type[] { typeof(string) } );
            return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.GetOrgMembers(string @orgName, global::System.Threading.CancellationToken @cancellationToken) 
        {
            var ______arguments = new object[] { @orgName, @cancellationToken };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetOrgMembers"", new global::System.Type[] { typeof(string), typeof(global::System.Threading.CancellationToken) } );
            return (global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> global::Refit.Tests.IGitHubApi.FindUsers(string @q) 
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""FindUsers"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> global::Refit.Tests.IGitHubApi.GetIndex() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetIndex"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.IObservable<string> global::Refit.Tests.IGitHubApi.GetIndexObservable() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetIndexObservable"", new global::System.Type[] {  } );
            return (global::System.IObservable<string>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.NothingToSeeHere() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""NothingToSeeHere"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.NothingToSeeHereWithMetadata() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""NothingToSeeHereWithMetadata"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.GetUserWithMetadata(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserWithMetadata"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.GetUserObservableWithMetadata(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserObservableWithMetadata"", new global::System.Type[] { typeof(string) } );
            return (global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.CreateUser(global::Refit.Tests.User @user) 
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""CreateUser"", new global::System.Type[] { typeof(global::Refit.Tests.User) } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.CreateUserWithMetadata(global::Refit.Tests.User @user) 
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""CreateUserWithMetadata"", new global::System.Type[] { typeof(global::Refit.Tests.User) } );
            return (global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }
    }
    }
}

#pragma warning restore
";
            var output3 = @"#nullable disable
#pragma warning disable
namespace Refit.Implementation
{

    partial class Generated
    {

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [global::RefitInternalGenerated.PreserveAttribute]
    [global::System.Reflection.Obfuscation(Exclude=true)]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    partial class RefitTestsIGitHubApiDisposable
        : global::Refit.Tests.IGitHubApiDisposable

    {
        /// <inheritdoc />
        public global::System.Net.Http.HttpClient Client { get; }
        readonly global::Refit.IRequestBuilder requestBuilder;

        /// <inheritdoc />
        public RefitTestsIGitHubApiDisposable(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
        {
            Client = client;
            this.requestBuilder = requestBuilder;
        }
    


        /// <inheritdoc />
        public global::System.Threading.Tasks.Task RefitMethod() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""RefitMethod"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task global::Refit.Tests.IGitHubApiDisposable.RefitMethod() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""RefitMethod"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        void global::System.IDisposable.Dispose() 
        {
                Client?.Dispose();
        }
    }
    }
}

#pragma warning restore
";
            var output4 = @"#nullable disable
#pragma warning disable
namespace Refit.Implementation
{

    partial class Generated
    {

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [global::RefitInternalGenerated.PreserveAttribute]
    [global::System.Reflection.Obfuscation(Exclude=true)]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    partial class RefitTestsTestNestedINestedGitHubApi
        : global::Refit.Tests.TestNested.INestedGitHubApi

    {
        /// <inheritdoc />
        public global::System.Net.Http.HttpClient Client { get; }
        readonly global::Refit.IRequestBuilder requestBuilder;

        /// <inheritdoc />
        public RefitTestsTestNestedINestedGitHubApi(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
        {
            Client = client;
            this.requestBuilder = requestBuilder;
        }
    


        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.Tests.User> GetUser(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUser"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserObservable(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserObservable"", new global::System.Type[] { typeof(string) } );
            return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserCamelCase(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserCamelCase"", new global::System.Type[] { typeof(string) } );
            return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> GetOrgMembers(string @orgName) 
        {
            var ______arguments = new object[] { @orgName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetOrgMembers"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> FindUsers(string @q) 
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""FindUsers"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> GetIndex() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetIndex"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.IObservable<string> GetIndexObservable() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetIndexObservable"", new global::System.Type[] {  } );
            return (global::System.IObservable<string>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task NothingToSeeHere() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""NothingToSeeHere"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.TestNested.INestedGitHubApi.GetUser(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUser"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.TestNested.INestedGitHubApi.GetUserObservable(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserObservable"", new global::System.Type[] { typeof(string) } );
            return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.TestNested.INestedGitHubApi.GetUserCamelCase(string @userName) 
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetUserCamelCase"", new global::System.Type[] { typeof(string) } );
            return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> global::Refit.Tests.TestNested.INestedGitHubApi.GetOrgMembers(string @orgName) 
        {
            var ______arguments = new object[] { @orgName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetOrgMembers"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> global::Refit.Tests.TestNested.INestedGitHubApi.FindUsers(string @q) 
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""FindUsers"", new global::System.Type[] { typeof(string) } );
            return (global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> global::Refit.Tests.TestNested.INestedGitHubApi.GetIndex() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetIndex"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.IObservable<string> global::Refit.Tests.TestNested.INestedGitHubApi.GetIndexObservable() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetIndexObservable"", new global::System.Type[] {  } );
            return (global::System.IObservable<string>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task global::Refit.Tests.TestNested.INestedGitHubApi.NothingToSeeHere() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""NothingToSeeHere"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task)______func(this.Client, ______arguments);
        }
    }
    }
}

#pragma warning restore
";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies,
                TestState =
                {
                    AdditionalReferences = { RefitAssembly },
                    Sources = { input },
                    GeneratedSources =
                    {
                        (typeof(InterfaceStubGenerator), "PreserveAttribute.g.cs", output1),
                        (typeof(InterfaceStubGenerator), "Generated.g.cs", output1_5),
                        (typeof(InterfaceStubGenerator), "IGitHubApi.g.cs", output2),
                        (typeof(InterfaceStubGenerator), "IGitHubApiDisposable.g.cs", output3),
                        (typeof(InterfaceStubGenerator), "INestedGitHubApi.g.cs", output4),
                    },
                },
            }.RunAsync();

            await new VerifyCSV2.Test
            {
                ReferenceAssemblies = ReferenceAssemblies,
                TestState =
                {
                    AdditionalReferences = { RefitAssembly },
                    Sources = { input },
                    GeneratedSources =
                    {
                        (typeof(InterfaceStubGeneratorV2), "PreserveAttribute.g.cs", output1),
                        (typeof(InterfaceStubGeneratorV2), "Generated.g.cs", output1_5),
                        (typeof(InterfaceStubGeneratorV2), "IGitHubApi.g.cs", output2),
                        (typeof(InterfaceStubGeneratorV2), "IGitHubApiDisposable.g.cs", output3),
                        (typeof(InterfaceStubGeneratorV2), "INestedGitHubApi.g.cs", output4),
                    },
                },
            }.RunAsync();
        }


        [Fact]
        public async Task GenerateInterfaceStubsWithoutNamespaceSmokeTest()
        {
            var input = File.ReadAllText(IntegrationTestHelper.GetPath("IServiceWithoutNamespace.cs"));
            var output1 = @"
#pragma warning disable
namespace RefitInternalGenerated
{
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.AttributeUsage (global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct | global::System.AttributeTargets.Enum | global::System.AttributeTargets.Constructor | global::System.AttributeTargets.Method | global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Event | global::System.AttributeTargets.Interface | global::System.AttributeTargets.Delegate)]
    sealed class PreserveAttribute : global::System.Attribute
    {
        //
        // Fields
        //
        public bool AllMembers;

        public bool Conditional;
    }
}
#pragma warning restore
";
            var output1_5 = @"
#pragma warning disable
namespace Refit.Implementation
{

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [global::RefitInternalGenerated.PreserveAttribute]
    [global::System.Reflection.Obfuscation(Exclude=true)]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static partial class Generated
    {
    }
}
#pragma warning restore
";

            var output2 = @"#nullable disable
#pragma warning disable
namespace Refit.Implementation
{

    partial class Generated
    {

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [global::RefitInternalGenerated.PreserveAttribute]
    [global::System.Reflection.Obfuscation(Exclude=true)]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    partial class IServiceWithoutNamespace
        : global::IServiceWithoutNamespace

    {
        /// <inheritdoc />
        public global::System.Net.Http.HttpClient Client { get; }
        readonly global::Refit.IRequestBuilder requestBuilder;

        /// <inheritdoc />
        public IServiceWithoutNamespace(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
        {
            Client = client;
            this.requestBuilder = requestBuilder;
        }
    


        /// <inheritdoc />
        public global::System.Threading.Tasks.Task GetRoot() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetRoot"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task PostRoot() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""PostRoot"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task global::IServiceWithoutNamespace.GetRoot() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetRoot"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        global::System.Threading.Tasks.Task global::IServiceWithoutNamespace.PostRoot() 
        {
            var ______arguments = new object[] {  };
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""PostRoot"", new global::System.Type[] {  } );
            return (global::System.Threading.Tasks.Task)______func(this.Client, ______arguments);
        }
    }
    }
}

#pragma warning restore
";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies,
                TestState =
                {
                    AdditionalReferences = { RefitAssembly },
                    Sources = { input },
                    GeneratedSources =
                    {
                        (typeof(InterfaceStubGenerator), "PreserveAttribute.g.cs", output1),
                        (typeof(InterfaceStubGenerator), "Generated.g.cs", output1_5),
                        (typeof(InterfaceStubGenerator), "IServiceWithoutNamespace.g.cs", output2),
                    },
                },
            }.RunAsync();

            await new VerifyCSV2.Test
            {
                ReferenceAssemblies = ReferenceAssemblies,
                TestState =
                {
                    AdditionalReferences = { RefitAssembly },
                    Sources = { input },
                    GeneratedSources =
                    {
                        (typeof(InterfaceStubGeneratorV2), "PreserveAttribute.g.cs", output1),
                        (typeof(InterfaceStubGeneratorV2), "Generated.g.cs", output1_5),
                        (typeof(InterfaceStubGeneratorV2), "IServiceWithoutNamespace.g.cs", output2),
                    },
                },
            }.RunAsync();
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
        Task Update(TKey key, [Body] T payload);

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
