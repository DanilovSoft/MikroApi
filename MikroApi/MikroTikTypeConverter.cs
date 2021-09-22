using System;
using System.Globalization;

namespace DanilovSoft.MikroApi
{
    internal static class MikroTikTypeConverter
    {
        /// <summary>
        /// Convert.ChangeType
        /// </summary>
        public static object? ConvertValue(string value, Type targetType)
        {
            if (targetType.IsAssignableFrom(typeof(string)))
            {
                return value;
            }
            else
            {
                targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
        }

        public static T? ConvertValue<T>(string value)
        {
            Type propType = typeof(T);
            return (T?)ConvertValue(value, propType);
        }
    }
}
