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
            documentation: XmlDocumentationProvider.CreateFromFile(
                Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")
            )
        );

        static readonly ReferenceAssemblies ReferenceAssemblies;

        static InterfaceStubGeneratorTests()
        {
#if NET5_0
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
#elif NET6_0
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
#elif NET7_0
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
#else
            ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(
                ImmutableArray.Create(new PackageIdentity("System.Text.Json", "7.0.2"))
            );
#endif

#if NET461
            ReferenceAssemblies = ReferenceAssemblies
                .AddAssemblies(ImmutableArray.Create("System.Web"))
                .AddPackages(
                    ImmutableArray.Create(new PackageIdentity("System.Net.Http", "4.3.4"))
                );
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
                IntegrationTestHelper.GetPath("InheritedGenericInterfacesApi.cs")
            );

            var diags = inputCompilation.GetDiagnostics();

            // Make sure we don't have any errors
            Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));

            var rundriver = driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out var outputCompiliation,
                out var diagnostics
            );

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

            return CSharpCompilation.Create(
                "compilation",
                sourceFiles.Select(source => CSharpSyntaxTree.ParseText(File.ReadAllText(source))),
                keyReferences.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location)),
                new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            );
        }

        [Fact]
        public async Task NoRefitInterfacesSmokeTest()
        {
            var input = File.ReadAllText(
                IntegrationTestHelper.GetPath("IInterfaceWithoutRefit.cs")
            );

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies,
                TestState = { AdditionalReferences = { RefitAssembly }, Sources = { input }, },
            }.RunAsync();

            await new VerifyCSV2.Test
            {
                ReferenceAssemblies = ReferenceAssemblies,
                TestState = { AdditionalReferences = { RefitAssembly }, Sources = { input }, },
            }.RunAsync();
        }

        [Fact]
        public async Task FindInterfacesSmokeTest()
        {
            var input = File.ReadAllText(IntegrationTestHelper.GetPath("GitHubApi.cs"));

            var output1 =
                @"
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

            var output1_5 =
                @"
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

            var output2 = """
#nullable disable
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



        private static readonly global::System.Type[] _typeParameters = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.User> GetUser(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUser", _typeParameters );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters0 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserObservable(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservable", _typeParameters0 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters1 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserCamelCase(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserCamelCase", _typeParameters1 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters2 = new global::System.Type[] {typeof(string), typeof(global::System.Threading.CancellationToken) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> GetOrgMembers(string @orgName, global::System.Threading.CancellationToken @cancellationToken)
        {
            var ______arguments = new object[] { @orgName, @cancellationToken };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetOrgMembers", _typeParameters2 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters3 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> FindUsers(string @q)
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("FindUsers", _typeParameters3 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> GetIndex()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndex", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        public global::System.IObservable<string> GetIndexObservable()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndexObservable", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return (global::System.IObservable<string>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.User> NothingToSeeHere()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("NothingToSeeHere", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> NothingToSeeHereWithMetadata()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("NothingToSeeHereWithMetadata", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters4 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> GetUserWithMetadata(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserWithMetadata", _typeParameters4 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters5 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>> GetUserObservableWithMetadata(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservableWithMetadata", _typeParameters5 );
            try
            {
                return (global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters6 = new global::System.Type[] {typeof(global::Refit.Tests.User) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.User> CreateUser(global::Refit.Tests.User @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("CreateUser", _typeParameters6 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters7 = new global::System.Type[] {typeof(global::Refit.Tests.User) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> CreateUserWithMetadata(global::Refit.Tests.User @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("CreateUserWithMetadata", _typeParameters7 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters8 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.GetUser(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUser", _typeParameters8 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters9 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.GetUserObservable(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservable", _typeParameters9 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters10 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.GetUserCamelCase(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserCamelCase", _typeParameters10 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters11 = new global::System.Type[] {typeof(string), typeof(global::System.Threading.CancellationToken) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.GetOrgMembers(string @orgName, global::System.Threading.CancellationToken @cancellationToken)
        {
            var ______arguments = new object[] { @orgName, @cancellationToken };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetOrgMembers", _typeParameters11 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters12 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> global::Refit.Tests.IGitHubApi.FindUsers(string @q)
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("FindUsers", _typeParameters12 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> global::Refit.Tests.IGitHubApi.GetIndex()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndex", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        global::System.IObservable<string> global::Refit.Tests.IGitHubApi.GetIndexObservable()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndexObservable", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return (global::System.IObservable<string>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.NothingToSeeHere()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("NothingToSeeHere", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.NothingToSeeHereWithMetadata()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("NothingToSeeHereWithMetadata", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters13 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.GetUserWithMetadata(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserWithMetadata", _typeParameters13 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters14 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.GetUserObservableWithMetadata(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservableWithMetadata", _typeParameters14 );
            try
            {
                return (global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters15 = new global::System.Type[] {typeof(global::Refit.Tests.User) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.CreateUser(global::Refit.Tests.User @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("CreateUser", _typeParameters15 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters16 = new global::System.Type[] {typeof(global::Refit.Tests.User) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.CreateUserWithMetadata(global::Refit.Tests.User @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("CreateUserWithMetadata", _typeParameters16 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }
    }
    }
}

#pragma warning restore

""";
            var output3 =
                @"#nullable disable
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
        public async global::System.Threading.Tasks.Task RefitMethod()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""RefitMethod"", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task global::Refit.Tests.IGitHubApiDisposable.RefitMethod()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""RefitMethod"", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
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
            var output4 = """
#nullable disable
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



        private static readonly global::System.Type[] _typeParameters = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.User> GetUser(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUser", _typeParameters );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters0 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserObservable(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservable", _typeParameters0 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters1 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserCamelCase(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserCamelCase", _typeParameters1 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters2 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> GetOrgMembers(string @orgName)
        {
            var ______arguments = new object[] { @orgName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetOrgMembers", _typeParameters2 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters3 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> FindUsers(string @q)
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("FindUsers", _typeParameters3 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> GetIndex()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndex", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        public global::System.IObservable<string> GetIndexObservable()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndexObservable", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return (global::System.IObservable<string>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task NothingToSeeHere()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("NothingToSeeHere", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters4 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.TestNested.INestedGitHubApi.GetUser(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUser", _typeParameters4 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters5 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.TestNested.INestedGitHubApi.GetUserObservable(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservable", _typeParameters5 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters6 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.TestNested.INestedGitHubApi.GetUserCamelCase(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserCamelCase", _typeParameters6 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters7 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> global::Refit.Tests.TestNested.INestedGitHubApi.GetOrgMembers(string @orgName)
        {
            var ______arguments = new object[] { @orgName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetOrgMembers", _typeParameters7 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        private static readonly global::System.Type[] _typeParameters8 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> global::Refit.Tests.TestNested.INestedGitHubApi.FindUsers(string @q)
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("FindUsers", _typeParameters8 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> global::Refit.Tests.TestNested.INestedGitHubApi.GetIndex()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndex", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        global::System.IObservable<string> global::Refit.Tests.TestNested.INestedGitHubApi.GetIndexObservable()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndexObservable", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return (global::System.IObservable<string>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task global::Refit.Tests.TestNested.INestedGitHubApi.NothingToSeeHere()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("NothingToSeeHere", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }
    }
    }
}

#pragma warning restore

""";

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
            var input = File.ReadAllText(
                IntegrationTestHelper.GetPath("IServiceWithoutNamespace.cs")
            );
            var output1 =
                @"
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
            var output1_5 =
                @"
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

            var output2 =
                @"#nullable disable
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
        public async global::System.Threading.Tasks.Task GetRoot()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetRoot"", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task PostRoot()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""PostRoot"", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task global::IServiceWithoutNamespace.GetRoot()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""GetRoot"", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task global::IServiceWithoutNamespace.PostRoot()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod(""PostRoot"", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ex)
            {
                throw ex;
            }
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
                        (
                            typeof(InterfaceStubGeneratorV2),
                            "IServiceWithoutNamespace.g.cs",
                            output2
                        ),
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

    public interface IMessage { }
}
