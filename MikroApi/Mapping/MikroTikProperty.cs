using System;
using System.Reflection;

namespace DanilovSoft.MikroApi.Mapping;

internal sealed class MikroTikProperty
{
    public readonly SetMemberValueDelegate SetValueHandler;
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
    }
}
