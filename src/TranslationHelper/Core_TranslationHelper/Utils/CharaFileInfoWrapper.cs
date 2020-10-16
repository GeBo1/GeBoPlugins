using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using BepInEx.Logging;
using GeBoCommon.Chara;
using HarmonyLib;

namespace TranslationHelperPlugin.Utils
{
    // ReSharper disable once PartialTypeWithSinglePart
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
                catch (Exception err)
                {
                    TranslationHelper.Logger.LogDebug(
                        $"CreateWrapper: Registered wrapper {wrapperType} for {targetType} failed, falling back to default wrapper: {err}");
                }
            }

            wrapperType = typeof(CharaFileInfoWrapper<>).MakeGenericType(targetType);
            return (ICharaFileInfo)Activator.CreateInstance(wrapperType, target);
        }

        public static ICharaFileInfo CreateWrapper<T>(T target)
        {
            return CreateWrapper(typeof(T), target);
        }

        public static void RegisterWrapperType(Type targetType, Type wrapperType)
        {
            WrapperTypes[targetType.FullName ?? targetType.Name] = wrapperType;
        }
    }


    // ReSharper disable once PartialTypeWithSinglePart
    internal partial class CharaFileInfoWrapper<T> : ICharaFileInfo
    {
        private static readonly Func<T, int> IndexGetter = CreateGetter<int>("index");
        private static readonly Func<T, string> NameGetter = CreateGetter<string>("name");
        private static readonly Action<T, string> NameSetter = CreateSetter<string>("name");

        // ReSharper disable once StringLiteralTypo
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
                catch
                {
                    return this.GuessSex();
                }
            }
        }

        private static Action<T, TValue> CreateSetter<TValue>(params string[] names)
        {
            if (names.Length < 1) throw new ArgumentException($"{nameof(names)} can not be empty");
            var targetType = typeof(T);

            var first = true;
            foreach (var name in names)
            {
                var propSetter = AccessTools.PropertySetter(targetType, name);
                if (propSetter == null)
                {
                    first = false;
                    continue;
                }

                if (!first)
                {
                    // log if found beyond first attempt because AccessTools will have warned of missing property
                    TranslationHelper.Logger.LogInfo(
                        $"Found property for type {targetType.FullName} with name {name}");
                }

                try
                {
                    return (Action<T, TValue>)Delegate.CreateDelegate(typeof(Action<T, TValue>), null,
                        propSetter);
                }
                catch
                {
                    Expression<Action<T, TValue>> setter =
                        (obj, value) => propSetter.Invoke(obj, new object[] {value});
                    return setter.Compile();
                }
            }

            foreach (var name in names)
            {
                var field = AccessTools.Field(targetType, name);
                if (field == null) continue;
                Expression<Action<T, TValue>> setter = (obj, value) => field.SetValue(obj, value);
                // log if found as field because AccessTools will have warned of missing property
                TranslationHelper.Logger.LogInfo($"Found field for type {targetType.FullName} with name {name}");
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
            var first = true;
            foreach (var name in names)
            {
                var propGetter = AccessTools.PropertyGetter(targetType, name);

                if (propGetter == null)
                {
                    first = false;
                    continue;
                }
                if (!first)
                {
                    // log if found beyond first attempt because AccessTools will have warned of missing property
                    TranslationHelper.Logger.LogInfo(
                        $"Found property for type {targetType.FullName} with name {name}");
                }

                try
                {
                    return (Func<T, TResult>)Delegate.CreateDelegate(typeof(Func<T, TResult>), null,
                        propGetter);
                }
                catch
                {
                    Expression<Func<T, TResult>> setter =
                        obj => (TResult)propGetter.Invoke(obj, new object[0]);
                    return setter.Compile();
                }
            }

            foreach (var name in names)
            {
                var field = AccessTools.Field(targetType, name);
                if (field == null) continue;
                Expression<Func<T, TResult>> getter = obj => (TResult)field.GetValue(obj);
                // log if found as field because AccessTools will have warned of missing property
                TranslationHelper.Logger.LogInfo($"Found field for type {targetType.FullName} with name {name}");
                return getter.Compile();
            }

            var msg = $"unable to expose getter for {names[0]} on {targetType.FullName}";
            TranslationHelper.Logger.LogWarning(msg);
            return obj => throw new NotSupportedException(msg);
        }
    }
}
