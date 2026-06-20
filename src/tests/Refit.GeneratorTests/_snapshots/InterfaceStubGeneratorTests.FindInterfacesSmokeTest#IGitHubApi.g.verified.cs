//HintName: IGitHubApi.g.cs
#nullable disable
// This file is generated into consumer projects; suppress all analyzers so
// consumer analyzer policy does not report Refit implementation details.
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

        private static readonly global::System.Type[] ______typeParameters2 = new global::System.Type[] {typeof(string), typeof(global::System.Threading.CancellationToken) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::Refit.Tests.User>> GetOrgMembers(string @orgName, global::System.Threading.CancellationToken @cancellationToken)
        {
            var ______arguments = new object[] { @orgName, @cancellationToken };
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
        public global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> GetIndex()
        {
            var ______settings = requestBuilder.Settings;
            var ______basePath = this.Client.BaseAddress?.AbsolutePath ?? throw new global::System.InvalidOperationException("BaseAddress must be set on the HttpClient instance");
            ______basePath = ______basePath == "/" ? string.Empty : ______basePath.TrimEnd('/');
            var ______rq = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Get, new global::System.Uri(______basePath + "/", global::System.UriKind.Relative));
            #if NET6_0_OR_GREATER
            ______rq.Version = ______settings.Version;
            ______rq.VersionPolicy = ______settings.VersionPolicy;
            #endif
            global::Refit.GeneratedRequestRunner.SetHeader(______rq, "User-Agent", "Refit Integration Tests");
            global::Refit.GeneratedRequestRunner.AddConfiguredRequestOptions(______rq, ______settings, typeof(global::Refit.Tests.IGitHubApi));
            return global::Refit.GeneratedRequestRunner.SendAsync<global::System.Net.Http.HttpResponseMessage, global::System.Net.Http.HttpResponseMessage>(
                this.Client,
                ______rq,
                ______settings,
                false,
                false,
                false,
                global::System.Threading.CancellationToken.None);
        }

        /// <inheritdoc />
        public global::System.IObservable<string> GetIndexObservable()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetIndexObservable", global::System.Array.Empty<global::System.Type>() );

            return (global::System.IObservable<string>)______func(this.Client, ______arguments);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.Tests.User> NothingToSeeHere()
        {
            var ______settings = requestBuilder.Settings;
            var ______basePath = this.Client.BaseAddress?.AbsolutePath ?? throw new global::System.InvalidOperationException("BaseAddress must be set on the HttpClient instance");
            ______basePath = ______basePath == "/" ? string.Empty : ______basePath.TrimEnd('/');
            var ______rq = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Get, new global::System.Uri(______basePath + "/give-me-some-404-action", global::System.UriKind.Relative));
            #if NET6_0_OR_GREATER
            ______rq.Version = ______settings.Version;
            ______rq.VersionPolicy = ______settings.VersionPolicy;
            #endif
            global::Refit.GeneratedRequestRunner.SetHeader(______rq, "User-Agent", "Refit Integration Tests");
            global::Refit.GeneratedRequestRunner.AddConfiguredRequestOptions(______rq, ______settings, typeof(global::Refit.Tests.IGitHubApi));
            return global::Refit.GeneratedRequestRunner.SendAsync<global::Refit.Tests.User, global::Refit.Tests.User>(
                this.Client,
                ______rq,
                ______settings,
                false,
                true,
                false,
                global::System.Threading.CancellationToken.None);
        }

        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> NothingToSeeHereWithMetadata()
        {
            var ______settings = requestBuilder.Settings;
            var ______basePath = this.Client.BaseAddress?.AbsolutePath ?? throw new global::System.InvalidOperationException("BaseAddress must be set on the HttpClient instance");
            ______basePath = ______basePath == "/" ? string.Empty : ______basePath.TrimEnd('/');
            var ______rq = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Get, new global::System.Uri(______basePath + "/give-me-some-404-action", global::System.UriKind.Relative));
            #if NET6_0_OR_GREATER
            ______rq.Version = ______settings.Version;
            ______rq.VersionPolicy = ______settings.VersionPolicy;
            #endif
            global::Refit.GeneratedRequestRunner.SetHeader(______rq, "User-Agent", "Refit Integration Tests");
            global::Refit.GeneratedRequestRunner.AddConfiguredRequestOptions(______rq, ______settings, typeof(global::Refit.Tests.IGitHubApi));
            return global::Refit.GeneratedRequestRunner.SendAsync<global::Refit.ApiResponse<global::Refit.Tests.User>, global::Refit.Tests.User>(
                this.Client,
                ______rq,
                ______settings,
                true,
                true,
                false,
                global::System.Threading.CancellationToken.None);
        }

        private static readonly global::System.Type[] ______typeParameters4 = new global::System.Type[] {typeof(string), typeof(global::System.Threading.CancellationToken) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> GetUserWithMetadata(string @userName, global::System.Threading.CancellationToken @cancellationToken)
        {
            var ______arguments = new object[] { @userName, @cancellationToken };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserWithMetadata", ______typeParameters4 );

            return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }

        private static readonly global::System.Type[] ______typeParameters5 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>> GetUserObservableWithMetadata(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserObservableWithMetadata", ______typeParameters5 );

            return (global::System.IObservable<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        private static readonly global::System.Type[] ______typeParameters6 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public global::System.IObservable<global::Refit.IApiResponse<global::Refit.Tests.User>> GetUserIApiResponseObservableWithMetadata(string @userName)
        {
            var ______arguments = new object[] { @userName };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUserIApiResponseObservableWithMetadata", ______typeParameters6 );

            return (global::System.IObservable<global::Refit.IApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments);
        }

        private static readonly global::System.Type[] ______typeParameters7 = new global::System.Type[] {typeof(global::Refit.Tests.User) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.Tests.User> CreateUser(global::Refit.Tests.User @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("CreateUser", ______typeParameters7 );

            return await ((global::System.Threading.Tasks.Task<global::Refit.Tests.User>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }

        private static readonly global::System.Type[] ______typeParameters8 = new global::System.Type[] {typeof(global::Refit.Tests.User) };

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>> CreateUserWithMetadata(global::Refit.Tests.User @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("CreateUserWithMetadata", ______typeParameters8 );

            return await ((global::System.Threading.Tasks.Task<global::Refit.ApiResponse<global::Refit.Tests.User>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }
    }
    }
}

#pragma warning restore
