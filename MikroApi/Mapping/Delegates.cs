namespace DanilovSoft.MikroApi.Mapping
{
    internal delegate void OnDeserializedDelegate(object obj, object streamingContext);
    internal delegate void OnDeserializingDelegate(object obj, object streamingContext);
    internal delegate void SetMemberValueDelegate(object obj, object value);
    internal delegate object CreateInstanceDelegate<T>();
}
