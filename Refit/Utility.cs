using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Refit
{
    public static class EnumerableEx
    {
        public static IEnumerable<T> Return<T>(T value)
        {
            yield return value;
        }
    }
#if NETSTANDARD1_4 
    // Shims for old-style reflection
    static class ReflectionExtensions
    {
        public static bool IsInterface(this Type type) 
        {            
            return type.GetTypeInfo().IsInterface;
        }

        public static bool IsGenericType(this Type type) 
        {
            return type.GetTypeInfo().IsGenericType;
        }

        public static MethodInfo[] GetMethods(this Type type) 
        {
            return type.GetRuntimeMethods()
                .Where(m => m.IsPublic && !m.IsStatic)
                .ToArray();
        }

        public static Attribute[] GetCustomAttributes(this Type type, bool inherit) 
        {
            return type.GetTypeInfo()
                .GetCustomAttributes(inherit)
                .ToArray();
        }

        public static MethodInfo GetMethod(this Type type, string name, BindingFlags bindingFlags) {
            var isPublic = !bindingFlags.HasFlag(BindingFlags.NonPublic);
            var isStatic = bindingFlags.HasFlag(BindingFlags.Static);

            return type.GetRuntimeMethods()
                .Where(m => m.IsPublic == isPublic && m.IsStatic == isStatic)
                .FirstOrDefault(m => m.Name == name);
        }

        public static Type[] GetGenericArguments(this Type type) 
        {
            return type.GetTypeInfo()
                .GenericTypeArguments;
        }

        public static PropertyInfo[] GetProperties(this Type type) 
        {
            return type.GetRuntimeProperties()
                .Where(p => !p.GetMethod.IsStatic)
                .Where(p => p.GetMethod.IsPublic || p.SetMethod.IsPublic)
                .ToArray();
        }
    }

    [Flags]
    internal enum BindingFlags
    {
        Instance = 4,
        Static = 8,
        Public = 16,
        NonPublic = 32
    }

#else
    static class ReflectionExtensions
    {
        public static bool IsInterface(this Type type) 
        {            
            return type.IsInterface;
        }

        public static bool IsGenericType(this Type type) 
        {
            return type.IsGenericType;
        }
    }
#endif
}

