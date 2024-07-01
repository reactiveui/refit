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
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters0 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserObservable(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservable", ______typeParameters0 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters1 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.Tests.User> GetUserCamelCase(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserCamelCase", ______typeParameters1 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters2 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> GetOrgMembers(string @orgName)
        {
            var ______arguments = new object[] { @orgName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetOrgMembers", ______typeParameters2 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters3 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> FindUsers(string @q)
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("FindUsers", ______typeParameters3 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
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
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
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
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
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
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters4 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.TestNested.INestedGitHubApi.GetUser(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUser", ______typeParameters4 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters5 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.TestNested.INestedGitHubApi.GetUserObservable(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservable", ______typeParameters5 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters6 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.TestNested.INestedGitHubApi.GetUserCamelCase(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserCamelCase", ______typeParameters6 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters7 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> global::Refit.Tests.TestNested.INestedGitHubApi.GetOrgMembers(string @orgName)
        {
            var ______arguments = new object[] { @orgName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetOrgMembers", ______typeParameters7 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters8 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> global::Refit.Tests.TestNested.INestedGitHubApi.FindUsers(string @q)
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("FindUsers", ______typeParameters8 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
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
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
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
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
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
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }
    }
    }
}

#pragma warning restore
