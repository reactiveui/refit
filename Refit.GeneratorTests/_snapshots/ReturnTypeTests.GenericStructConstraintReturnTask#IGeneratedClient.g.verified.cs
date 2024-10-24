//HintName: IGeneratedClient.g.cs
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
    partial class RefitGeneratorTestIGeneratedClient
        : global::RefitGeneratorTest.IGeneratedClient

    {
        /// <inheritdoc />
        public global::System.Net.Http.HttpClient Client { get; }
        readonly global::Refit.IRequestBuilder requestBuilder;

        /// <inheritdoc />
        public RefitGeneratorTestIGeneratedClient(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
        {
            Client = client;
            this.requestBuilder = requestBuilder;
        }


        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task<string> Get<T>()
         where T : struct
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("Get", global::System.Array.Empty<global::System.Type>(), new global::System.Type[] { typeof(T) } );

            return await ((global::System.Threading.Tasks.Task<string>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<string> global::RefitGeneratorTest.IGeneratedClient.Get<T>()
         where T : struct
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("Get", global::System.Array.Empty<global::System.Type>(), new global::System.Type[] { typeof(T) } );

            return await ((global::System.Threading.Tasks.Task<string>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }
    }
    }
}

#pragma warning restore
