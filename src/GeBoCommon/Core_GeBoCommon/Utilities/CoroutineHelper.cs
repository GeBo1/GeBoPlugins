using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GeBoCommon.Utilities
{
    [PublicAPI]
    public class CoroutineHelper
    {
        private const string CoroutineStubHolderName = "GeBoCommon_Utilities_CoroutineHelper_Holder";
        private readonly SimpleLazy<GameObject> _coroutineStubHolderLoader;
        private readonly SimpleLazy<CoroutineStub> _coroutineStubLoader;

        [PublicAPI]
        public EventHandler ApplicationQuit;
        [PublicAPI]
        public EventHandler FixedUpdate;
        [PublicAPI]
        public EventHandler LateUpdate;
        [PublicAPI]
        public EventHandler Update;

        public CoroutineHelper()
        {
            _coroutineStubHolderLoader = new SimpleLazy<GameObject>(InitHolder);
            _coroutineStubLoader = new SimpleLazy<CoroutineStub>(InitStub);
        }


        private GameObject Holder => _coroutineStubHolderLoader.Value;
        protected MonoBehaviour Launcher => _coroutineStubLoader.Value;

        private GameObject InitHolder()
        {
            var obj = new GameObject(CoroutineStubHolderName);
            Object.DontDestroyOnLoad(obj);
            return obj;
        }

        private CoroutineStub InitStub()
        {
            return Holder.GetOrAddComponent<CoroutineStub>(o => o.Helper = this);
        }

        public Coroutine Start(IEnumerator routine)
        {
            return Launcher == null ? null : Launcher.StartCoroutine(routine);
        }

        public Coroutine Start(string methodName)
        {
            return Launcher == null ? null : Launcher.StartCoroutine(methodName);
        }

        public Coroutine Start(string methodName, object value)
        {
            return Launcher == null ? null : Launcher.StartCoroutine(methodName, value);
        }

        public void Stop(IEnumerator routine)
        {
            Launcher.SafeProc(l => l.StopCoroutine(routine));
        }

        public void Stop(string methodName)
        {
            Launcher.SafeProc(l => l.StopCoroutine(methodName));
        }

        public void StopAll()
        {
            if (!_coroutineStubLoader.IsValueCreated) return;
            Launcher.SafeProc(l => l.StopAllCoroutines());
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        internal class CoroutineStub : MonoBehaviour
        {
            internal CoroutineHelper Helper;

            private void Update()
            {
                Helper?.Update?.SafeInvoke(this, EventArgs.Empty);
            }

            private void FixedUpdate()
            {
                Helper?.FixedUpdate?.SafeInvoke(this, EventArgs.Empty);
            }

            private void LateUpdate()
            {
                Helper?.LateUpdate?.SafeInvoke(this, EventArgs.Empty);
            }
        }
    }
}
