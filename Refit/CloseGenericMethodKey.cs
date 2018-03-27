using System;
using System.Linq;
using System.Reflection;

namespace Refit
{
    struct CloseGenericMethodKey : IEquatable<CloseGenericMethodKey>
    {
        internal CloseGenericMethodKey(MethodInfo openMethodInfo, Type[] types)
        {
            OpenMethodInfo = openMethodInfo;
            Types = types;
        }

        public MethodInfo OpenMethodInfo { get; }
        public Type[] Types { get; }

        public bool Equals(CloseGenericMethodKey other) => OpenMethodInfo == other.OpenMethodInfo && Types.SequenceEqual(other.Types);

        public override bool Equals(object obj)
        {
            if (obj is CloseGenericMethodKey closeGenericMethodKey)
            {
                return Equals(closeGenericMethodKey);
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + OpenMethodInfo.GetHashCode();
                foreach (Type type in Types)
                {
                    hash = hash * 23 + type.GetHashCode();
                }
                return hash;
            }
        }
    }
}
