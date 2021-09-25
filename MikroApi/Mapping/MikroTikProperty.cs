using System;
using System.Reflection;

namespace DanilovSoft.MikroApi.Mapping
{
    internal class MikroTikProperty
    {
        //private static readonly ConcurrentDictionary<Type, ISqlConverter> _converters = new ConcurrentDictionary<Type, ISqlConverter>();
        public readonly SetMemberValueDelegate SetValueHandler;
        //public readonly TypeConverterAttribute Converter;
        public readonly Type MemberType;

        public MikroTikProperty(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo propertyInfo)
            {
                MemberType = propertyInfo.PropertyType;
            }
            else
            {
                var fieldInfo = (FieldInfo)memberInfo;
                MemberType = fieldInfo.FieldType;
            }

            SetValueHandler = new SetMemberValueDelegate(DynamicReflectionDelegateFactory.CreateSet<object>(memberInfo));
            //var attribute = memberInfo.GetCustomAttribute<TypeConverterAttribute>();
            //if (attribute != null)
            //{
            //    Converter = _converters.GetOrAdd(attribute.ConverterType, ConverterValueFactory);
            //}
        }

        //private ISqlConverter ConverterValueFactory(Type converterType)
        //{
        //    var ctor = DynamicReflectionDelegateFactory.Instance.CreateDefaultConstructor<ISqlConverter>(converterType);
        //    return ctor.Invoke();
        //}

        //private object Convert(object value, Type columnType, string columnName)
        //{
        //    if (Converter != null)
        //    {
        //        return Converter.Convert(value, MemberType);
        //    }
        //    else
        //    {
        //        return SqlTypeConverter.ChangeType(value, MemberType, columnType, columnName);
        //    }
        //}

        //public void SetValue(object obj, object value, Type columnType, string columnName)
        //{
        //    object finalValue = Convert(value, columnType, columnName);

        //    SetValueHandler.Invoke(obj, finalValue);
        //}
    }
}
