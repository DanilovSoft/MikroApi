using System.Runtime.Serialization;

namespace DanilovSoft.MikroApi.Mapping;

internal delegate void OnDeserializedDelegate(object obj, StreamingContext streamingContext);
internal delegate void OnDeserializingDelegate(object obj, StreamingContext streamingContext);
internal delegate void SetMemberValueDelegate(object obj, object? value);
internal delegate object CreateInstanceDelegate<T>();
