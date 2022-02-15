namespace Refit
{
    /// <summary>
    /// Contains Refit-defined properties on the HttpRequestMessage.Properties/Options
    /// </summary>
    public static class HttpRequestMessageOptions
    {
        /// <summary>
        /// Returns the <see cref="System.Type"/> of the top-level interface where the method was called from
        /// </summary>
        public static string InterfaceType { get; } = "Refit.InterfaceType";

#if NET5_0_OR_GREATER
        /// <summary>
        /// A typed key to access the <see cref="System.Type"/> of the top-level interface where the method was called from
        /// on the <see cref="System.Net.Http.HttpRequestMessage.Options"/>.
        /// </summary>
        public static System.Net.Http.HttpRequestOptionsKey<System.Type> InterfaceTypeKey { get; } = new(InterfaceType);
#endif

        /// <summary>
        /// Returns the <see cref="Refit.RestMethodInfo"/> of the method that was called
        /// </summary>
        public static string RestMethodInfo { get; } = "Refit.RestMethodInfo";

#if NET5_0_OR_GREATER
        /// <summary>
        /// A typed key to access the <see cref="Refit.RestMethodInfo"/> of the method that was called
        /// </summary>
        public static System.Net.Http.HttpRequestOptionsKey<RestMethodInfo> RestMethodInfoKey { get; } = new(RestMethodInfo);
#endif
    }
}
