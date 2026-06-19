//HintName: IContainedInterface.g.cs
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
    partial class RefitGeneratorTestContainerTypeIContainedInterface
        : global::RefitGeneratorTest.ContainerType.IContainedInterface
    {
        /// <inheritdoc />
        public global::System.Net.Http.HttpClient Client { get; }
        readonly global::Refit.IRequestBuilder requestBuilder;

        /// <inheritdoc />
        public RefitGeneratorTestContainerTypeIContainedInterface(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
        {
            Client = client;
            this.requestBuilder = requestBuilder;
        }


        /// <inheritdoc />
        public global::System.Threading.Tasks.Task<string> Get()
        {
            var ______settings = requestBuilder.Settings;
            var ______basePath = this.Client.BaseAddress?.AbsolutePath ?? throw new global::System.InvalidOperationException("BaseAddress must be set on the HttpClient instance");
            ______basePath = ______basePath == "/" ? string.Empty : ______basePath.TrimEnd('/');
            var ______rq = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Get, new global::System.Uri(______basePath + "/users", global::System.UriKind.Relative));
            #if NET6_0_OR_GREATER
            ______rq.Version = ______settings.Version;
            ______rq.VersionPolicy = ______settings.VersionPolicy;
            #endif
            global::Refit.GeneratedRequestRunner.AddConfiguredRequestOptions(______rq, ______settings, typeof(global::RefitGeneratorTest.ContainerType.IContainedInterface));
            return global::Refit.GeneratedRequestRunner.SendAsync<string, string>(
                this.Client,
                ______rq,
                ______settings,
                false,
                true,
                false,
                global::System.Threading.CancellationToken.None);
        }
    }
    }
}

#pragma warning restore
