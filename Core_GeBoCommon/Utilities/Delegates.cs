﻿using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GeBoCommon.Utilities
{
    public static class Delegates
    {
        private static ManualLogSource Logger => GeBoAPI.Instance?.Logger;
        //public static Func<T> BuildFieldGetter<T>(object instance, string fieldName) => BuildFieldGetter<T>(() => instance, fieldName);
        public static Func<T> LazyReflectionGetter<T>(SimpleLazy<object> instLoader, string fieldName) => LazyReflectionGetter<T>(() => instLoader.Value, fieldName);
        public static Func<T> LazyReflectionGetter<T>(Func<object> instLoader, string fieldName) => LazyReflectionGetter<T>(() => instLoader().GetType(), instLoader, fieldName);
        //public static Func<T> BuildFieldGetter<T>(Type type, string fieldName) => BuildFieldGetter<T>(() => type, fieldName);
        public static Func<T> LazyReflectionGetter<T>(SimpleLazy<Type> typeLoader, string fieldName) => LazyReflectionGetter<T>(() => typeLoader.Value, fieldName);

        public static Func<T> LazyReflectionGetter<T>(Func<Type> typeLoader, string fieldName) => LazyReflectionGetter<T>(typeLoader, () => null, fieldName);

        public static Func<T> LazyReflectionGetter<T>(Func<Type> typeLoader, Func<object> objLoader, string fieldName)
        {
            Logger.LogDebug($"{nameof(LazyReflectionGetter)}<{typeof(T).Name}>({typeLoader}, {objLoader}, {fieldName}");
            SimpleLazy<FieldInfo> fieldInfo = new SimpleLazy<FieldInfo>(() => AccessTools.Field(typeLoader(), fieldName));

            SimpleLazy<object> instance = new SimpleLazy<object>(objLoader);
            return new Func<T>(() => (T)fieldInfo.Value?.GetValue(instance.Value));
        }
    }
}