using System.Reflection;

namespace Refit
{
    /// <summary>
    /// Provides utility methods for reflection-based operations.
    /// </summary>
    public static class ReflectionHelpers
    {
        /// <summary>
        /// Retrieves the path prefix defined by a <see cref="PathPrefixAttribute"/> on a specified interface or its inherited interfaces.
        /// </summary>
        /// <param name="targetInterface">The interface type from which to retrieve the path prefix.</param>
        /// <returns>
        /// The path prefix if a <see cref="PathPrefixAttribute"/> is found; otherwise, an empty string.
        /// </returns>
        /// <remarks>
        /// This method first checks the specified interface for the <see cref="PathPrefixAttribute"/>. If not found,
        /// it then checks each interface inherited by the target interface. If no attribute is found after all checks,
        /// the method returns an empty string.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="targetInterface"/> is null.
        /// </exception>
        public static string GetPathPrefixFor(Type targetInterface)
        {
            // Manual null check for compatibility with older .NET versions
            if (targetInterface == null)
            {
                throw new ArgumentNullException(nameof(targetInterface));
            }

            // Check if the attribute is applied to the type T itself
            var attribute = targetInterface.GetCustomAttribute<PathPrefixAttribute>();
            if (attribute != null)
            {
                return attribute.PathPrefix;
            }

            // If the attribute is not found on T, check its interfaces
            foreach (var interfaceType in targetInterface.GetInterfaces())
            {
                attribute = interfaceType.GetCustomAttribute<PathPrefixAttribute>();
                if (attribute != null)
                {
                    return attribute.PathPrefix;
                }
            }

            // If the attribute is still not found, return empty string
            return string.Empty;
        }
    }
}
