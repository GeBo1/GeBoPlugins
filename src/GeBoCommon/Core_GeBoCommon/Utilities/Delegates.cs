using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace GeBoCommon.Utilities
{
    public static class Delegates
    {
        private static ManualLogSource Logger => GeBoAPI.Instance?.Logger;

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

        public static Func<T> LazyReflectionGetter<T>(Func<Type> typeLoader, Func<object> objLoader, string fieldName)
        {
            Logger.DebugLogDebug(
                $"{nameof(LazyReflectionGetter)}<{typeof(T).Name}>({typeLoader}, {objLoader}, {fieldName}");
            var fieldInfo = new SimpleLazy<FieldInfo>(() => AccessTools.Field(typeLoader(), fieldName));

            var instance = new SimpleLazy<object>(objLoader);
            return () => (T)fieldInfo.Value?.GetValue(instance.Value);
        }
    }
}
