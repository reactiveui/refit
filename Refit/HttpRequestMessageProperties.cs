using System;

namespace Refit
{
    /// <summary>
    /// Contains Refit-defined properties on the HttpRequestMessage.Properties/Options
    /// </summary>
    public static class HttpRequestMessageOptions
    {
        /// <summary>
        /// Returns the <see cref="Type"/> of the top-level interface where the method was called from.
        /// </summary>
        public static string InterfaceType => "Refit.InterfaceType";

        /// <summary>
        /// Returns the <see cref="System.Reflection.MethodInfo"/> of the executing method.
        /// </summary>
        public static string MethodInfo => "Refit.MethodInfo";
    }
}
