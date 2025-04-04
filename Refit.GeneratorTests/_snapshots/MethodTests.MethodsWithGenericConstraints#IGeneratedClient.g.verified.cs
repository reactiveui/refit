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
        public async global::System.Threading.Tasks.Task<string> Get<T1, T2, T3, T4, T5>()
         where T1 : class
         where T2 : unmanaged
         where T3 : struct
         where T4 : notnull
         where T5 : class, global::System.IDisposable, new()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("Get", global::System.Array.Empty<global::System.Type>(), new global::System.Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) } );

            return await ((global::System.Threading.Tasks.Task<string>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task<string> global::RefitGeneratorTest.IGeneratedClient.Get<T1, T2, T3, T4, T5>()
         where T1 : class
         where T3 : struct
         where T5 : class
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("Get", global::System.Array.Empty<global::System.Type>(), new global::System.Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) } );

            return await ((global::System.Threading.Tasks.Task<string>)______func(this.Client, ______arguments)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        void global::RefitGeneratorTest.IGeneratedClient.NonRefitMethod<T1, T2, T3, T4, T5>()
         where T1 : class
         where T3 : struct
         where T5 : class
        {
            throw new global::System.NotImplementedException("Either this method has no Refit HTTP method attribute or you've used something other than a string literal for the 'path' argument.");
        }
    }
    }
}

#pragma warning restore
