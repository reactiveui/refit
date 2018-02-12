using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Refit
{
    public static class TypeExtensions
    {
        public static bool IsGenericTypeOf<T>(this Type type)
        {
            return IsGenericTypeOf(type, typeof(T));
        }
        public static bool IsGenericTypeOf(this Type type, Type genericType)
        {
            if(type == null)
                throw new ArgumentNullException(nameof(type));

            if (genericType == null)
                throw new ArgumentNullException(nameof(genericType));

            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == genericType;
        }
    }
}
