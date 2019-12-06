using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace DanilovSoft.MikroApi.Mapping
{
    internal class DynamicActivator
    {
        private static readonly ConcurrentDictionary<Type, ObjectMapper> _dict = new ConcurrentDictionary<Type, ObjectMapper>();
        private static readonly ConcurrentDictionary<Type, AnonymousObjectMapper> _anonDict = new ConcurrentDictionary<Type, AnonymousObjectMapper>();

        public static ObjectMapper GetMapper<T>()
        {
            return _dict.GetOrAdd(typeof(T), BinderFactory);
        }

        private static ObjectMapper BinderFactory(Type type)
        {
            return new ObjectMapper(type);
        }

        internal static AnonymousObjectMapper GetAnonymousMappger<T>()
        {
            return _anonDict.GetOrAdd(typeof(T), AnonFactory);
        }

        private static AnonymousObjectMapper AnonFactory(Type type)
        {
            return new AnonymousObjectMapper(type);
        }
    }
}
