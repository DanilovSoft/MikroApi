using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MikroApi
{
    class AnonymousObjectMapper<T>
    {
        readonly Type _type;
        readonly List<PropertyInfo> _propertyes;

        public AnonymousObjectMapper()
        {
            _type = typeof(T);
            _propertyes = _type.GetRuntimeProperties().ToList();
        }

        public T ReadObject(MikroTikResponseValues values)
        {
            object[] propValues = new object[_propertyes.Count];
            for (int i = 0; i < _propertyes.Count; i++)
            {
                string propName = _propertyes[i].Name;
                string value = values[propName];

                Type propType = _propertyes[i].PropertyType;
                propValues[i] = MikroTikTypeConverter.ConvertValue(value, propType);
            }

            T obj = (T)Activator.CreateInstance(_type, propValues);
            return obj;
        }
    }
}
