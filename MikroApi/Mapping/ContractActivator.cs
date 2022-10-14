using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace DanilovSoft.MikroApi.Mapping;

internal sealed class ContractActivator
{
    private readonly Func<object> _activator;
    //private readonly Func<object[], object>? _anonimousActivator;
    public readonly OnDeserializingDelegate? OnDeserializingHandle;
    public readonly OnDeserializedDelegate? OnDeserializedHandle;

    public ContractActivator(Type type)
    {
        //if(!anonimouseType)
        {
            _activator = DynamicReflectionDelegateFactory.CreateDefaultConstructor<object>(type);
            OnDeserializingHandle = null;

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                if (method.IsDefined(typeof(OnDeserializingAttribute), false))
                {
                    OnDeserializingHandle = DynamicReflectionDelegateFactory.CreateOnDeserializingMethodCall(method, type);
                }

                if (method.IsDefined(typeof(OnDeserializedAttribute), false))
                {
                    OnDeserializedHandle = DynamicReflectionDelegateFactory.CreateOnDeserializedMethodCall(method, type);
                }
            }
        }
        //else
        //{
        //    _anonimousActivator = DynamicReflectionDelegateFactory.Instance.CreateAnonimousConstructor(type);

        //    // поля у анонимных типов не рассматриваются.
        //    // берем только свойства по умолчанию.
        //    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // порядок полей такой же как у конструктора.
        //AnonimousProperties = properties
        //    .Select((x, Index) => new { PropertyInfo = x, Index })
        //    .ToDictionary(x => x.PropertyInfo.Name, x => new AnonimousProperty(x.Index, x.PropertyInfo.PropertyType));
        //}
    }

    //public object CreateAnonimousInstance(object[] args)
    //{
    //    return _anonimousActivator.Invoke(args);
    //}

    public object CreateInstance()
    {
        return _activator.Invoke();
    }

    //public TypeContract GetTypeContract()
    //{
    //    return DynamicMember.GetTypeContract(_type);
    //}
}
