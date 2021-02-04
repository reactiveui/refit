using System;

namespace Refit
{
    class UniqueName
    {
        public static string ForType<T>()
        {
            return ForType(typeof(T));
        }

        public static string ForType(Type refitInterfaceType)
        {
            var interfaceTypeName = refitInterfaceType.FullName!;

            // remove namespace/nested, up to anything before a `
            var searchEnd = interfaceTypeName.IndexOf('`');
            if (searchEnd < 0)
                searchEnd = interfaceTypeName.Length;

            var lastDot = interfaceTypeName.Substring(0, searchEnd).LastIndexOf('.');
            if (lastDot > 0)
            {
                interfaceTypeName = interfaceTypeName.Substring(lastDot + 1);
            }

            // Now we have the interface name like IFooBar`1[[Some Generic Args]]
            // Or Nested+IFrob
            var genericArgs = string.Empty;
            // if there's any generics, split that
            if(refitInterfaceType.IsGenericType)
            {
                genericArgs = interfaceTypeName.Substring(interfaceTypeName.IndexOf("["));
                interfaceTypeName = interfaceTypeName.Substring(0, interfaceTypeName.Length - genericArgs.Length);
            }

            // Remove any + from the type name portion
            interfaceTypeName = interfaceTypeName.Replace("+", "");

            // Get the namespace and remove the dots
            var ns = refitInterfaceType.Namespace?.Replace(".", "");

            // Refit types will be generated as private classes within a Generated type in namespace
            // Refit.Implementation
            // E.g., Refit.Implementation.Generated.NamespaceContaingTpeInterfaceType

            var refitTypeName = $"Refit.Implementation.Generated+{ns}{interfaceTypeName}{genericArgs}";

            var assmQualified = $"{refitTypeName}, {refitInterfaceType.Assembly.FullName}";

            return assmQualified;
        }
    }
}
