using System;
using System.Collections.Generic;
using System.Reflection;

namespace DanilovSoft.MikroApi.Mapping;

internal class ObjectMapper
{
    private readonly Dictionary<string, MikroTikProperty> _properties;
    private readonly ContractActivator _activator;

    public ObjectMapper(Type type)
    {
        _activator = new ContractActivator(type);
        _properties = GetProperties(type);
    }

    private static Dictionary<string, MikroTikProperty> GetProperties(Type type)
    {
        var props = type.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var dict = new Dictionary<string, MikroTikProperty>();

        for (var i = 0; i < props.Length; i++)
        {
            var propertyInfo = props[i];
            var attrib = propertyInfo.GetCustomAttribute<MikroTikPropertyAttribute>();
            if (attrib != null)
            {
                dict.Add(attrib.Name, new MikroTikProperty(propertyInfo));
            }
        }

        return dict;
    }

    public object ReadObject(MikroTikResponseFrameDictionary frame)
    {
        var obj = _activator.CreateInstance();

        _activator.OnDeserializingHandle?.Invoke(obj, default);

        if (frame.Count > 0)
        {
            foreach (var (key, value) in frame)
            {
                if (_properties.TryGetValue(key, out var mikroTikProperty))
                {
                    var compatibleValue = MikroTikTypeConverter.ConvertValue(value, mikroTikProperty.MemberType);
                    mikroTikProperty.SetValueHandler(obj, compatibleValue);
                }
            }
        }
        _activator.OnDeserializedHandle?.Invoke(obj, default);
        return obj;
    }
}
