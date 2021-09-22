using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MikroApi
{
    class DataBinder<T> where T : new()
    {
        Dictionary<string, PropertyInfo> _props;
        MethodInfo _onDeserialized;

        public DataBinder()
        {
            _props = GetProperties();
            _onDeserialized = OnDeserialized();
        }

        static Dictionary<string, PropertyInfo> GetProperties()
        {
            PropertyInfo[] props = typeof(T).GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            var dict = new Dictionary<string, PropertyInfo>();
            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];
                var attrib = p.GetCustomAttribute<MikroTikPropertyAttribute>();
                if (attrib != null && p.CanWrite)
                {
                    dict.Add(attrib.Name, p);
                }
            }
            return dict;
        }

        static MethodInfo OnDeserialized()
        {
            MethodInfo[] methods = typeof(T).GetTypeInfo().GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].IsDefined(typeof(OnDeserializedAttribute), false))
                    return methods[i];
            }

            return null;
        }

        public T Bind(MikroTikResponseValues values)
        {
            T item = new T();

            foreach (var keyValue in values)
            {
                string key = keyValue.Key;
                string value = keyValue.Value;

                PropertyInfo prop;
                if(_props.TryGetValue(key, out prop))
                {
                    prop.SetValue(item, ConvertValue(prop, value));
                }
            }

            if(_onDeserialized != null)
            {
                var onDeserialized = (Action<MikroTikResponseValues>)_onDeserialized.CreateDelegate(typeof(Action<MikroTikResponseValues>), item);
                onDeserialized(values);
            }

            return item;
        }

        object ConvertValue(PropertyInfo prop, string value)
        {
            var typeConverter = prop.GetCustomAttribute<TypeConverterAttribute>(false);
            if (typeConverter == null)
            {
                return MikroTikTypeConverter.ConvertValue(value, prop.PropertyType);
            }
            else
            {
                return ConvertValue(typeConverter.ConverterTypeName, value);
            }
        }

        object ConvertValue(string typeName, string value)
        {
            var type = Type.GetType(typeName);
            var converter = (TypeConverter)Activator.CreateInstance(type);
            return converter.ConvertFromString(value);
        }
    }
}
