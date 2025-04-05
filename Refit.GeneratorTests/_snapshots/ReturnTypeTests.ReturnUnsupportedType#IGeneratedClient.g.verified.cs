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


        private static readonly global::System.Type[] ______typeParameters = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        public string GetUser(string @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUser", ______typeParameters );

            return (string)______func(this.Client, ______arguments);
        }

        private static readonly global::System.Type[] ______typeParameters0 = new global::System.Type[] {typeof(string) };

        /// <inheritdoc />
        string global::RefitGeneratorTest.IGeneratedClient.GetUser(string @user)
        {
            var ______arguments = new object[] { @user };
            var ______func = requestBuilder.BuildRestResultFuncForMethod("GetUser", ______typeParameters0 );

            return (string)______func(this.Client, ______arguments);
        }
    }
    }
}

#pragma warning restore
