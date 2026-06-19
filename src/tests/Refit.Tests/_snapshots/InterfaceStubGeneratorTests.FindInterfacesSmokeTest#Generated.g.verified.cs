//HintName: Generated.g.cs

#pragma warning disable
namespace Refit.Implementation
{

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [global::RefitInternalGenerated.PreserveAttribute]
    [global::System.Reflection.Obfuscation(Exclude=true)]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static partial class Generated
    {
#if NET5_0_OR_GREATER
        [System.Runtime.CompilerServices.ModuleInitializer]
        [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(global::Refit.Implementation.Generated))]
        public static void Initialize()
        {
                        global::Refit.RestService.RegisterGeneratedFactory(typeof(global::Refit.Tests.IGitHubApi), static (client, requestBuilder) => new global::Refit.Implementation.Generated.RefitTestsIGitHubApi(client, requestBuilder));
                        global::Refit.RestService.RegisterGeneratedFactory(typeof(global::Refit.Tests.IGitHubApiDisposable), static (client, requestBuilder) => new global::Refit.Implementation.Generated.RefitTestsIGitHubApiDisposable(client, requestBuilder));
                        global::Refit.RestService.RegisterGeneratedFactory(typeof(global::Refit.Tests.TestNested.INestedGitHubApi), static (client, requestBuilder) => new global::Refit.Implementation.Generated.RefitTestsTestNestedINestedGitHubApi(client, requestBuilder));
        }
#endif
    }
}
#pragma warning restore
