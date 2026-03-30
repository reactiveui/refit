//HintName: IMyRefitApi.g.cs
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
    partial class RefitTestsIMyRefitApi
        : global::Refit.Tests.IMyRefitApi
    {
        /// <inheritdoc />
        public global::System.Net.Http.HttpClient Client { get; }
        readonly global::Refit.IRequestBuilder requestBuilder;

        /// <inheritdoc />
        public RefitTestsIMyRefitApi(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
        {
            Client = client;
            this.requestBuilder = requestBuilder;
        }


        /// <inheritdoc />
        public async Task<List<string>> GetUsers()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUsers", global::System.Array.Empty<global::System.Type>() );

            return await ((Task<List<string>>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }
    }
    }
}

#pragma warning restore
