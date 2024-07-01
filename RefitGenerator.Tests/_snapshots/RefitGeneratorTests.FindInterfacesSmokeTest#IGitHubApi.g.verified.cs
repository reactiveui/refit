//HintName: IGitHubApi.g.cs
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

        private static readonly global::System.Type[] ______typeParameters2 = new global::System.Type[] {typeof(string), typeof(global::System.Threading.CancellationToken) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> GetOrgMembers(string @orgName, global::System.Threading.CancellationToken @cancellationToken)
        {
            var ______arguments = new object[] { @orgName, @cancellationToken };
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
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.User> NothingToSeeHere()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("NothingToSeeHere", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
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
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters4 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> GetUserWithMetadata(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserWithMetadata", ______typeParameters4 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters5 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>> GetUserObservableWithMetadata(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservableWithMetadata", ______typeParameters5 );
            try
            {
                return (global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters6 = new global::System.Type[] {typeof(global::Refit.Tests.User) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.User> CreateUser(global::Refit.Tests.User @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("CreateUser", ______typeParameters6 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters7 = new global::System.Type[] {typeof(global::Refit.Tests.User) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> CreateUserWithMetadata(global::Refit.Tests.User @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("CreateUserWithMetadata", ______typeParameters7 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters8 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.GetUser(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUser", ______typeParameters8 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters9 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.GetUserObservable(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservable", ______typeParameters9 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters10 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        global::System.IObservable<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.GetUserCamelCase(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserCamelCase", ______typeParameters10 );
            try
            {
                return (global::System.IObservable<global::Refit.Tests.User>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters11 = new global::System.Type[] {typeof(string), typeof(global::System.Threading.CancellationToken) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.GetOrgMembers(string @orgName, global::System.Threading.CancellationToken @cancellationToken)
        {
            var ______arguments = new object[] { @orgName, @cancellationToken };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetOrgMembers", ______typeParameters11 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters12 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.UserSearchResult> global::Refit.Tests.IGitHubApi.FindUsers(string @q)
        {
            var ______arguments = new object[] { @q };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("FindUsers", ______typeParameters12 );
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
        async global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> global::Refit.Tests.IGitHubApi.GetIndex()
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
        global::System.IObservable<string> global::Refit.Tests.IGitHubApi.GetIndexObservable()
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
        async global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.NothingToSeeHere()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("NothingToSeeHere", global::System.Array.Empty<global::System.Type>() );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
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
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters13 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.GetUserWithMetadata(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserWithMetadata", ______typeParameters13 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters14 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.GetUserObservableWithMetadata(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservableWithMetadata", ______typeParameters14 );
            try
            {
                return (global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters15 = new global::System.Type[] {typeof(global::Refit.Tests.User) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.Tests.User> global::Refit.Tests.IGitHubApi.CreateUser(global::Refit.Tests.User @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("CreateUser", ______typeParameters15 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        private static readonly global::System.Type[] ______typeParameters16 = new global::System.Type[] {typeof(global::Refit.Tests.User) };

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> global::Refit.Tests.IGitHubApi.CreateUserWithMetadata(global::Refit.Tests.User @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("CreateUserWithMetadata", ______typeParameters16 );
            try
            {
                return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
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
