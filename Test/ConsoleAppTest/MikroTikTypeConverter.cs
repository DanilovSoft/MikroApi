using System;
using System.Reflection;

namespace MikroApi
{
    static class MikroTikTypeConverter
    {
        public static object ConvertValue(string value, Type targetType)
        {
            if (targetType.IsAssignableFrom(typeof(string)))
            {
                return value;
            }

            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return Convert.ChangeType(value, targetType);
        }

        public static T ConvertValue<T>(string value)
        {
            Type propType = typeof(T);
            return (T)ConvertValue(value, propType);
        }
    }
}
