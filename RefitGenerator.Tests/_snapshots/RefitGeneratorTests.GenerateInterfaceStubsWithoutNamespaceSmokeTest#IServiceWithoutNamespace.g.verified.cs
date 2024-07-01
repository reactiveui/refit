//HintName: IServiceWithoutNamespace.g.cs
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
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetRoot", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        /// <inheritdoc />
        public async global::System.Threading.Tasks.Task PostRoot()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("PostRoot", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task global::IServiceWithoutNamespace.GetRoot()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetRoot", global::System.Array.Empty<global::System.Type>() );
            try
            {
                await ((global::System.Threading.Tasks.Task)______func(this.Client, ______arguments)).ConfigureAwait(false);
            }
            catch (global::System.Exception ______ex)
            {
                throw ______ex;
            }
        }

        /// <inheritdoc />
        async global::System.Threading.Tasks.Task global::IServiceWithoutNamespace.PostRoot()
        {
            var ______arguments = global::System.Array.Empty<object>();
            var ______func = requestBuilder.BuildRestResultFuncForMethod("PostRoot", global::System.Array.Empty<global::System.Type>() );
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
