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

        /// <summary>
        /// Returns the <see cref="Refit.RestMethodInfo"/> of the top-level interface
        /// </summary>
        public static string RestMethodInfo { get; } = "Refit.RestMethodInfo";
    }
}
