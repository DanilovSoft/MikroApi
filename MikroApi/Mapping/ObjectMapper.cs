using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace DanilovSoft.MikroApi.Mapping
{
    internal class ObjectMapper
    {
        private readonly static StreamingContext _defaultStreamingContext = new();
        private readonly Dictionary<string, MikroTikProperty> _properties;
        private readonly ContractActivator _activator;

        public ObjectMapper(Type type)
        {
            _activator = new ContractActivator(type);
            _properties = GetProperties(type);
        }

        private static Dictionary<string, MikroTikProperty> GetProperties(Type type)
        {
            PropertyInfo[] props = type.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var dict = new Dictionary<string, MikroTikProperty>();
            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo propertyInfo = props[i];
                var attrib = propertyInfo.GetCustomAttribute<MikroTikPropertyAttribute>();
                if (attrib != null)
                {
                    dict.Add(attrib.Name, new MikroTikProperty(propertyInfo));
                }
            }
            return dict;
        }

        public object ReadObject(MikroTikResponseFrame frame)
        {
            object obj = _activator.CreateInstance();

            _activator.OnDeserializingHandle?.Invoke(obj, _defaultStreamingContext);

            if (frame.Count > 0)
            {
                foreach (KeyValuePair<string, string> keyValue in frame)
                {
                    string key = keyValue.Key;
                    string value = keyValue.Value;

                    if (_properties.TryGetValue(key, out MikroTikProperty mikroTikProperty))
                    {
                        object compatibleValue = MikroTikTypeConverter.ConvertValue(value, mikroTikProperty.MemberType);
                        mikroTikProperty.SetValueHandler(obj, compatibleValue);
                    }
                }
            }
            _activator.OnDeserializedHandle?.Invoke(obj, _defaultStreamingContext);
            return obj;
        }
    }
}
