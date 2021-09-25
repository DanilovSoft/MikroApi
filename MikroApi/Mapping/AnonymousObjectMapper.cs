using System;
using System.Linq;

namespace DanilovSoft.MikroApi.Mapping
{
    internal class AnonymousObjectMapper
    {
        private readonly Func<object[], object> _activator;
        private readonly AnonProperty[] _properties;

        public AnonymousObjectMapper(Type type)
        {
            _activator = DynamicReflectionDelegateFactory.CreateAnonimousConstructor(type);
            _properties = type.GetProperties().Select(x => new AnonProperty
            {
                Name = x.Name,
                PropertyType = x.PropertyType,
            }).ToArray();
        }

        public object ReadObject(MikroTikResponseFrameDictionary values)
        {
            object[] propValues = new object[_properties.Length];
            for (int i = 0; i < _properties.Length; i++)
            {
                string propName = _properties[i].Name;
                string value = values[propName];

                Type propType = _properties[i].PropertyType;
                propValues[i] = MikroTikTypeConverter.ConvertValue(value, propType);
            }
            return _activator(propValues);
            //return Activator.CreateInstance(_type, propValues);
        }

        private struct AnonProperty
        {
            public string Name;
            public Type PropertyType;
        }
    }
}
