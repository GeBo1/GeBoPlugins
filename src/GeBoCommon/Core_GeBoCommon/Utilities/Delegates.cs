using System;
using System.Linq.Expressions;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;

namespace GeBoCommon.Utilities
{
    [PublicAPI]
    public static class Delegates
    {
        private static ManualLogSource Logger => Common.CurrentLogger;

        public static Func<T> LazyReflectionGetter<T>(SimpleLazy<object> instLoader, string fieldName)
        {
            return LazyReflectionGetter<T>(() => instLoader.Value, fieldName);
        }

        public static Func<T> LazyReflectionGetter<T>(Func<object> instLoader, string fieldName)
        {
            return LazyReflectionGetter<T>(() => instLoader().GetType(), instLoader, fieldName);
        }

        public static Func<T> LazyReflectionGetter<T>(SimpleLazy<Type> typeLoader, string fieldName)
        {
            return LazyReflectionGetter<T>(() => typeLoader.Value, fieldName);
        }

        public static Func<T> LazyReflectionGetter<T>(Func<Type> typeLoader, string fieldName)
        {
            return LazyReflectionGetter<T>(typeLoader, () => null, fieldName);
        }

        public static Func<TField> LazyReflectionGetter<TObj, TField>(string fieldName)
        {
            return LazyReflectionGetter<TField>(() => typeof(TObj), () => null, fieldName);
        }

        public static Func<T> LazyReflectionGetter<T>(Func<Type> typeLoader, Func<object> objLoader, string fieldName)
        {
            Logger?.DebugLogDebug(
                $"{nameof(LazyReflectionGetter)}<{typeof(T).Name}>({typeLoader}, {objLoader}, {fieldName}");

            var innerGetter =
                new SimpleLazy<Func<object, T>>(() => LazyReflectionInstanceGetter<T>(typeLoader, fieldName));
            var instance = new SimpleLazy<object>(objLoader);


            Expression<Func<T>> getter = () =>
                innerGetter.Value(instance.Value);
            return getter.Compile();
        }

        private static Func<object, T> LazyReflectionInstanceGetter<T>(Type type, string fieldName)
        {
            var fieldInfo = new SimpleLazy<FieldInfo>(() => AccessTools.Field(type, fieldName));

            Expression<Func<object, T>> getter = obj =>
                (T)(fieldInfo.Value == null ? null : fieldInfo.Value.GetValue(obj));
            return getter.Compile();
        }

        private static Func<object, T> LazyReflectionInstanceGetter<T>(Func<Type> typeLoader, string fieldName)
        {
            var innerGetter = new SimpleLazy<Func<object, T>>(() =>
                LazyReflectionInstanceGetter<T>(typeLoader(), fieldName));

            Expression<Func<object, T>> getter = obj => innerGetter.Value(obj);
            return getter.Compile();
        }

        public static Func<TObj, TField> LazyReflectionInstanceGetter<TObj, TField>(string fieldName)
        {
            var innerGetter = new SimpleLazy<Func<object, TField>>(() =>
                LazyReflectionInstanceGetter<TField>(typeof(TObj), fieldName));

            Expression<Func<TObj, TField>> getter = obj => innerGetter.Value(obj);
            return getter.Compile();
        }

        public static Action<T> LazyReflectionSetter<T>(SimpleLazy<object> instLoader, string fieldName)
        {
            return LazyReflectionSetter<T>(() => instLoader.Value, fieldName);
        }

        public static Action<T> LazyReflectionSetter<T>(Func<object> instLoader, string fieldName)
        {
            return LazyReflectionSetter<T>(() => instLoader().GetType(), instLoader, fieldName);
        }

        public static Action<T> LazyReflectionSetter<T>(SimpleLazy<Type> typeLoader, string fieldName)
        {
            return LazyReflectionSetter<T>(() => typeLoader.Value, fieldName);
        }

        public static Action<T> LazyReflectionSetter<T>(Func<Type> typeLoader, string fieldName)
        {
            return LazyReflectionSetter<T>(typeLoader, () => null, fieldName);
        }

        public static Action<TField> LazyReflectionSetter<TObj, TField>(string fieldName)
        {
            return LazyReflectionSetter<TField>(() => typeof(TObj), () => null, fieldName);
        }

        public static Action<T> LazyReflectionSetter<T>(Func<Type> typeLoader, Func<object> objLoader, string fieldName)
        {
            Logger?.DebugLogDebug(
                $"{nameof(LazyReflectionSetter)}<{typeof(T).Name}>({typeLoader}, {objLoader}, {fieldName})");


            var innerSetter =
                new SimpleLazy<Action<object, T>>(() => LazyReflectionInstanceSetter<T>(typeLoader, fieldName));
            var instance = new SimpleLazy<object>(objLoader);

            Expression<Action<T>> setter = value => innerSetter.Value(instance.Value, value);
            return setter.Compile();
        }

        private static Action<object, T> LazyReflectionInstanceSetter<T>(Type type, string fieldName)
        {
            var fieldInfo = new SimpleLazy<FieldInfo>(() => AccessTools.Field(type, fieldName));
            Expression<Action<object, T>> setter = (obj, value) => fieldInfo.Value.SetValue(obj, value);
            return setter.Compile();
        }

        private static Action<object, T> LazyReflectionInstanceSetter<T>(Func<Type> typeLoader, string fieldName)
        {
            var innerSetter = new SimpleLazy<Action<object, T>>(() =>
                LazyReflectionInstanceSetter<T>(typeLoader(), fieldName));
            Expression<Action<object, T>> setter = (obj, value) => innerSetter.Value(obj, value);
            return setter.Compile();
        }

        public static Action<TObj, TField> LazyReflectionInstanceSetter<TObj, TField>(string fieldName)
        {
            var innerSetter = new SimpleLazy<Action<object, TField>>(() =>
                LazyReflectionInstanceSetter<TField>(typeof(TObj), fieldName));

            Expression<Action<TObj, TField>> setter = (obj, value) => innerSetter.Value(obj, value);
            return setter.Compile();
        }
    }
}
