using System;
using System.Linq;

namespace DanilovSoft.MikroApi.Mapping;

internal sealed class AnonymousObjectMapper
{
    private readonly Func<object?[], object> _activator;
    private readonly AnonProperty[] _properties;

    public AnonymousObjectMapper(Type type)
    {
        _activator = DynamicReflectionDelegateFactory.CreateAnonimousConstructor(type);
        _properties = type.GetProperties().Select(x => new AnonProperty(x.Name, x.PropertyType)).ToArray();
    }

    public object ReadObject(MikroTikResponseFrameDictionary values)
    {
        var propValues = new object?[_properties.Length];
        var len = _properties.Length;

        for (var i = 0; i < len; i++)
        {
            var (propName, propType) = _properties[i];
            var value = values[propName];
            propValues[i] = MikroTikTypeConverter.ConvertValue(value, propType);
        }

        return _activator(propValues);
    }

    private readonly struct AnonProperty
    {
        public readonly string Name;
        public readonly Type PropertyType;

        public AnonProperty(string name, Type propertyType)
        {
            Name = name;
            PropertyType = propertyType;
        }

        public void Deconstruct(out string name, out Type propertyType)
        {
            name = Name;
            propertyType = PropertyType;
        }
    }
}
