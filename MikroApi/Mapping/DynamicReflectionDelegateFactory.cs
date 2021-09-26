using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace DanilovSoft.MikroApi.Mapping
{
    internal class DynamicReflectionDelegateFactory
    {
        private static readonly Type[] ObjectArrayTypes = new[] { typeof(object[]) };

        public static Action<T, object> CreateSet<T>(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo propertyInfo)
            {
                return CreateSet<T>(propertyInfo);
            }

            if (memberInfo is FieldInfo fieldInfo)
            {
                return CreateSet<T>(fieldInfo);
            }

            throw new InvalidOperationException($"Could not create setter for {memberInfo}.");
        }

        public static Action<T, object> CreateSet<T>(PropertyInfo propertyInfo)
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod("Set" + propertyInfo.Name, null, new[] { typeof(T), typeof(object) }, propertyInfo.DeclaringType!);
            ILGenerator generator = dynamicMethod.GetILGenerator();

            GenerateCreateSetPropertyIL(propertyInfo, generator);

            return (Action<T, object>)dynamicMethod.CreateDelegate(typeof(Action<T, object>));
        }

        public static Action<T, object> CreateSet<T>(FieldInfo fieldInfo)
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod("Set" + fieldInfo.Name, null, new[] { typeof(T), typeof(object) }, fieldInfo.DeclaringType!);
            ILGenerator generator = dynamicMethod.GetILGenerator();

            GenerateCreateSetFieldIL(fieldInfo, generator);

            return (Action<T, object>)dynamicMethod.CreateDelegate(typeof(Action<T, object>));
        }

        public static Func<T> CreateDefaultConstructor<T>(Type type)
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod("",
                returnType: typeof(T),
                parameterTypes: Type.EmptyTypes,
                owner: type);

            dynamicMethod.InitLocals = true;
            ILGenerator generator = dynamicMethod.GetILGenerator();

            GenerateCreateDefaultConstructorIL(type, generator, typeof(T));

            return (Func<T>)dynamicMethod.CreateDelegate(typeof(Func<T>));
        }

        public static Func<object[], object> CreateAnonimousConstructor(Type type)
        {
            // У анонимных типов всегда есть 1 конструктор, принимающий параметры.
            ConstructorInfo ctor = type.GetConstructors()[0];

            DynamicMethod dynamicMethod = CreateDynamicMethod("",
                returnType: typeof(object),
                parameterTypes: ObjectArrayTypes,
                owner: type);

            dynamicMethod.InitLocals = true;
            ILGenerator generator = dynamicMethod.GetILGenerator();

            AnonGenerateMethodCall(ctor, generator, 0);

            return (Func<object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object[], object>));
        }

        public static OnDeserializingDelegate CreateOnDeserializingMethodCall(MethodInfo method, Type type)
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod("", returnType: typeof(void), parameterTypes: new[] { typeof(object), typeof(object) }, owner: method.DeclaringType!);
            ILGenerator generator = dynamicMethod.GetILGenerator();

            GenerateCreateMethodCallIL(method, generator, type);

            return (OnDeserializingDelegate)dynamicMethod.CreateDelegate(typeof(OnDeserializingDelegate));
        }

        public static OnDeserializedDelegate CreateOnDeserializedMethodCall(MethodInfo method, Type type)
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod("", returnType: typeof(void), parameterTypes: new[] { typeof(object), typeof(object) }, owner: type);
            ILGenerator generator = dynamicMethod.GetILGenerator();

            GenerateCreateMethodCallIL(method, generator, type);

            return (OnDeserializedDelegate)dynamicMethod.CreateDelegate(typeof(OnDeserializedDelegate));
        }

        private static DynamicMethod CreateDynamicMethod(string name, Type? returnType, Type[] parameterTypes, Type owner)
        {
            DynamicMethod dynamicMethod = !owner.IsInterface
                ? new DynamicMethod(name, returnType, parameterTypes, owner, true)
                : new DynamicMethod(name, returnType, parameterTypes, owner.Module, true);

            return dynamicMethod;
        }

        private static void AnonGenerateMethodCall(MethodBase method, ILGenerator generator, int argsIndex)
        {
            ParameterInfo[] args = method.GetParameters();

            //Label argsOk = generator.DefineLabel();

            //// throw an error if the number of argument values doesn't match method parameters
            //generator.Emit(OpCodes.Ldarg, argsIndex);
            //generator.Emit(OpCodes.Ldlen);
            //generator.Emit(OpCodes.Ldc_I4, args.Length);
            //generator.Emit(OpCodes.Beq, argsOk);
            //generator.Emit(OpCodes.Newobj, typeof(TargetParameterCountException).GetConstructor(Type.EmptyTypes));
            //generator.Emit(OpCodes.Throw);

            //generator.MarkLabel(argsOk);

            if (!method.IsConstructor && !method.IsStatic)
            {
                generator.PushInstance(method.DeclaringType!);
            }

            //LocalBuilder localConvertible = generator.DeclareLocal(typeof(IConvertible));
            LocalBuilder localObject = generator.DeclareLocal(typeof(object));

            for (int i = 0; i < args.Length; i++)
            {
                ParameterInfo parameter = args[i];
                Type parameterType = parameter.ParameterType;

                if (parameterType.IsValueType)
                {
                    generator.PushArrayInstance(argsIndex, i);
                    generator.Emit(OpCodes.Stloc_S, localObject);

                    // have to check that value type parameters aren't null
                    // otherwise they will error when unboxed
                    Label skipSettingDefault = generator.DefineLabel();
                    Label finishedProcessingParameter = generator.DefineLabel();

                    // check if parameter is not null
                    generator.Emit(OpCodes.Ldloc_S, localObject);
                    generator.Emit(OpCodes.Brtrue_S, skipSettingDefault);

                    // parameter has no value, initialize to default
                    LocalBuilder localVariable = generator.DeclareLocal(parameterType);
                    generator.Emit(OpCodes.Ldloca_S, localVariable);
                    generator.Emit(OpCodes.Initobj, parameterType);
                    generator.Emit(OpCodes.Ldloc_S, localVariable);
                    generator.Emit(OpCodes.Br_S, finishedProcessingParameter);

                    // argument has value, try to convert it to parameter type
                    generator.MarkLabel(skipSettingDefault);

                    //if (parameterType.IsPrimitive)
                    //{
                    //    // for primitive types we need to handle type widening (e.g. short -> int)
                    //    MethodInfo toParameterTypeMethod = typeof(IConvertible)
                    //        .GetMethod("To" + parameterType.Name, new[] { typeof(IFormatProvider) });

                    //    if (toParameterTypeMethod != null)
                    //    {
                    //        Label skipConvertible = generator.DefineLabel();

                    //        // check if argument type is an exact match for parameter type
                    //        // in this case we may use cheap unboxing instead
                    //        generator.Emit(OpCodes.Ldloc_S, localObject);
                    //        generator.Emit(OpCodes.Isinst, parameterType);
                    //        generator.Emit(OpCodes.Brtrue_S, skipConvertible);

                    //        // types don't match, check if argument implements IConvertible
                    //        generator.Emit(OpCodes.Ldloc_S, localObject);
                    //        generator.Emit(OpCodes.Isinst, typeof(IConvertible));
                    //        generator.Emit(OpCodes.Stloc_S, localConvertible);
                    //        generator.Emit(OpCodes.Ldloc_S, localConvertible);
                    //        generator.Emit(OpCodes.Brfalse_S, skipConvertible);

                    //        // convert argument to parameter type
                    //        generator.Emit(OpCodes.Ldloc_S, localConvertible);
                    //        generator.Emit(OpCodes.Ldnull);
                    //        generator.Emit(OpCodes.Callvirt, toParameterTypeMethod);
                    //        generator.Emit(OpCodes.Br_S, finishedProcessingParameter);

                    //        generator.MarkLabel(skipConvertible);
                    //    }
                    //}

                    // we got here because either argument type matches parameter (conversion will succeed),
                    // or argument type doesn't match parameter, but we're out of options (conversion will fail)
                    generator.Emit(OpCodes.Ldloc_S, localObject);

                    generator.UnboxIfNeeded(parameterType);

                    // parameter finished, we out!
                    generator.MarkLabel(finishedProcessingParameter);
                }
                else
                {
                    generator.PushArrayInstance(argsIndex, i);
                    generator.UnboxIfNeeded(parameterType);
                }
            }

            if (method.IsConstructor)
            {
                generator.Emit(OpCodes.Newobj, (ConstructorInfo)method);
            }
            else
            {
                generator.CallMethod((MethodInfo)method);
            }

            var returnType = method.IsConstructor
                ? method.DeclaringType
                : ((MethodInfo)method).ReturnType;

            if (returnType != typeof(void))
            {
                generator.BoxIfNeeded(returnType!);
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);
            }
            generator.Return();
        }

        private static void GenerateCreateDefaultConstructorIL(Type type, ILGenerator generator, Type delegateType)
        {
            if (type.IsValueType)
            {
                generator.DeclareLocal(type);
                generator.Emit(OpCodes.Ldloc_0);

                // only need to box if the delegate isn't returning the value type
                if (type != delegateType)
                {
                    generator.Emit(OpCodes.Box, type);
                }
            }
            else
            {
                var constructorInfo =
                    type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);

                if (constructorInfo == null)
                {
                    throw new ArgumentException($"Could not get constructor for {type}.");
                }

                generator.Emit(OpCodes.Newobj, constructorInfo);
            }

            generator.Return();
        }

        private static void GenerateCreateMethodCallIL(MethodInfo method, ILGenerator generator, Type type)
        {
            generator.PushInstance(type);
            generator.Emit(OpCodes.Ldarg_1);
            generator.UnboxIfNeeded(typeof(StreamingContext));
            generator.CallMethod(method);
            generator.Return();
        }

        private static void GenerateCreateSetPropertyIL(PropertyInfo propertyInfo, ILGenerator generator)
        {
            var setMethod = propertyInfo.GetSetMethod(true);
            if (!setMethod!.IsStatic)
            {
                generator.PushInstance(propertyInfo.DeclaringType!);
            }

            generator.Emit(OpCodes.Ldarg_1);
            generator.UnboxIfNeeded(propertyInfo.PropertyType);
            generator.CallMethod(setMethod);
            generator.Return();
        }

        private static void GenerateCreateSetFieldIL(FieldInfo fieldInfo, ILGenerator generator)
        {
            if (!fieldInfo.IsStatic)
            {
                generator.PushInstance(fieldInfo.DeclaringType!);
            }

            generator.Emit(OpCodes.Ldarg_1);
            generator.UnboxIfNeeded(fieldInfo.FieldType);

            if (!fieldInfo.IsStatic)
            {
                generator.Emit(OpCodes.Stfld, fieldInfo);
            }
            else
            {
                generator.Emit(OpCodes.Stsfld, fieldInfo);
            }

            generator.Return();
        }
    }
}
