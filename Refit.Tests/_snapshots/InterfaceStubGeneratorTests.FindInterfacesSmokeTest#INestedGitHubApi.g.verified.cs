//HintName: INestedGitHubApi.g.cs
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


        private static readonly global::System.Type[] ______typeParameters = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.User> GetUser(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUser", ______typeParameters );

            return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }

        private static readonly global::System.Type[] ______typeParameters0 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserObservable(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservable", ______typeParameters0 );

            return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        private static readonly global::System.Type[] ______typeParameters1 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserCamelCase(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserCamelCase", ______typeParameters1 );

            return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
        }

        private static readonly global::System.Type[] ______typeParameters2 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> GetOrgMembers(string @orgName)
        {
            var ______arguments = new object[] { @orgName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetOrgMembers", ______typeParameters2 );

            return await ((global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }

        private static readonly global::System.Type[] ______typeParameters3 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> FindUsers(string @q)
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("FindUsers", ______typeParameters3 );

            return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> GetIndex()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndex", global::System.Array.Empty<global::System.Type>() );

            return await ((global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public global::System.IObservable<string> GetIndexObservable()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndexObservable", global::System.Array.Empty<global::System.Type>() );

            return (global::System.IObservable<string>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task NothingToSeeHere()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("NothingToSeeHere", global::System.Array.Empty<global::System.Type>() );

            await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }
    }
    }
}

#pragma warning restore
