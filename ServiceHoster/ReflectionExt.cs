using System;
using System.Linq;
using System.ServiceModel;

namespace ServiceHoster
{
    public static class ReflectionExt
    {
        public static bool IsWcfService(this Type type)
        {
            if (!type.IsClass || !type.HasAttrbibute<ServiceContractAttribute>())
                return false;

            return !IsDerivedFrom(type, typeof(ClientBase<>));
        }

        private static bool HasAttrbibute<TAttribute>(this Type type)
        {
            if (type.IsDefined(typeof(TAttribute), false))
                return true;

            return type.GetInterfaces().Any(i => i.IsDefined(typeof(TAttribute), false));
        }

        private static bool IsDerivedFrom(Type type, Type baseType)
        {
            if (type.BaseType == null)
                return false;

            if (type.BaseType.GUID == baseType.GUID)
                return true;

            return IsDerivedFrom(type.BaseType, baseType);
        }
    }
}
