using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    internal static class MikroTikTypeConverter
    {
        /// <summary>
        /// Convert.ChangeType
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static object ConvertValue(string value, Type targetType)
        {
            if (targetType.IsAssignableFrom(typeof(string)))
            {
                return value;
            }
            else
            {
                targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return Convert.ChangeType(value, targetType);
            }
        }

        public static T ConvertValue<T>(string value)
        {
            Type propType = typeof(T);
            return (T)ConvertValue(value, propType);
        }
    }
}
