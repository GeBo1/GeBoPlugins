﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using GeBoCommon.Chara;
using HarmonyLib;
using JetBrains.Annotations;

namespace TranslationHelperPlugin.Utils
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal static partial class CharaFileInfoWrapper
    {
        private static readonly Dictionary<string, Type> WrapperTypes = new Dictionary<string, Type>();

        public static ICharaFileInfo CreateWrapper(Type targetType, object target)
        {
            if (WrapperTypes.TryGetValue(targetType.FullName ?? targetType.Name, out var wrapperType))
            {
                try
                {
                    return (ICharaFileInfo)Activator.CreateInstance(wrapperType, target);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception err)
                {
                    TranslationHelper.Logger.LogDebug(
                        $"CreateWrapper: Registered wrapper {wrapperType} for {targetType} failed, falling back to default wrapper: {err}");
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            wrapperType = typeof(CharaFileInfoWrapper<>).MakeGenericType(targetType);
            return (ICharaFileInfo)Activator.CreateInstance(wrapperType, target);
        }

        public static ICharaFileInfo CreateWrapper<T>(T target)
        {
            return CreateWrapper(typeof(T), target);
        }

        [UsedImplicitly]
        public static void RegisterWrapperType(Type targetType, Type wrapperType)
        {
            WrapperTypes[targetType.FullName ?? targetType.Name] = wrapperType;
        }
    }


    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal partial class CharaFileInfoWrapper<T> : ICharaFileInfo
    {
        private static readonly Func<T, int> IndexGetter = CreateGetter<int>("index");
        private static readonly Func<T, string> NameGetter = CreateGetter<string>("name");
        private static readonly Action<T, string> NameSetter = CreateSetter<string>("name");

        // ReSharper disable once StringLiteralTypo - that's the whole point
        private static readonly Func<T, string> FullPathGetter =
            CreateGetter<string>("FullPath", "fullpath", "fullPath");

        private static readonly Func<T, byte> InnerSexGetter = CreateGetter<byte>("sex");
        private readonly T _target;

        public CharaFileInfoWrapper(T target)
        {
            _target = target;
        }


        public int Index => IndexGetter(_target);

        public string Name
        {
            get => NameGetter(_target);
            set => NameSetter(_target, value);
        }

        public string FullPath => FullPathGetter(_target);

        public CharacterSex Sex
        {
            get
            {
                try
                {
                    return (CharacterSex)InnerSexGetter(_target);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
                {
                    return this.GuessSex();
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }

        private static Action<T, TValue> CreateSetter<TValue>(params string[] names)
        {
            if (names.Length < 1) throw new ArgumentException($"{nameof(names)} can not be empty");
            var targetType = typeof(T);

            foreach (var name in names)
            {
                var prop = targetType.GetProperty(name, AccessTools.all);
                //var propGetter = targetType.GetProperty(name).AccessTools.PropertySetter(targetType, name);
                var propSetter = prop?.GetSetMethod(false) ?? prop?.GetSetMethod(true);
                if (propSetter == null) continue;

                TranslationHelper.Logger.LogDebug(
                    $"Found {names[0]} property for type {targetType.FullName} with name {name}");

                try
                {
                    return AccessTools.MethodDelegate<Action<T, TValue>>(propSetter);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
                {
                    Expression<Action<T, TValue>> setter =
                        (obj, value) => propSetter.Invoke(obj, new object[] {value});
                    return setter.Compile();
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            foreach (var name in names)
            {
                var field = targetType.GetField(name, AccessTools.all);
                if (field == null) continue;
                Expression<Action<T, TValue>> setter = (obj, value) => field.SetValue(obj, value);
                // log if found as field because AccessTools will have warned of missing property
                TranslationHelper.Logger.LogDebug(
                    $"Found field {names[0]} for type {targetType.FullName} with name {name}");
                return setter.Compile();
            }

            var msg = $"unable to expose setter for {names[0]} on {targetType.FullName}";
            TranslationHelper.Logger.LogWarning(msg);
            return (obj, value) => throw new NotSupportedException(msg);
        }

        private static Func<T, TResult> CreateGetter<TResult>(params string[] names)
        {
            if (names.Length < 1) throw new ArgumentException($"{nameof(names)} can not be empty");
            var targetType = typeof(T);
            // prefer properties

            foreach (var name in names)
            {
                var prop = targetType.GetProperty(name, AccessTools.all);
                //var propGetter = targetType.GetProperty(name). AccessTools.PropertyGetter(targetType, name);
                var propGetter = prop?.GetGetMethod(false) ?? prop?.GetGetMethod(true);

                if (propGetter == null) continue;


                TranslationHelper.Logger.LogDebug(
                    $"Found property {names[0]} for type {targetType.FullName} with name {name}");


                try
                {
                    return AccessTools.MethodDelegate<Func<T, TResult>>(propGetter);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
                {
                    Expression<Func<T, TResult>> getter =
                        obj => (TResult)propGetter.Invoke(obj, new object[0]);
                    return getter.Compile();
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            foreach (var name in names)
            {
                //var field = AccessTools.Field(targetType, name);
                var field = targetType.GetField(name, AccessTools.all);
                if (field == null) continue;
                Expression<Func<T, TResult>> getter = obj => (TResult)field.GetValue(obj);
                TranslationHelper.Logger.LogDebug(
                    $"Found field {names[0]} for type {targetType.FullName} with name {name}");
                return getter.Compile();
            }

            var msg = $"unable to expose getter for {names[0]} on {targetType.FullName}";
            TranslationHelper.Logger.LogWarning(msg);
            return obj => throw new NotSupportedException(msg);
        }
    }
}
