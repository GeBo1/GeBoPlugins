using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

namespace GeBoCommon.Utilities
{
    [PublicAPI]
    public class CoroutineHelper
    {
        private const string CoroutineStubHolderName = "GeBoCommon_Utilities_CoroutineHelper_Holder";
        private readonly SimpleLazy<GameObject> _coroutineStubHolderLoader;
        private readonly SimpleLazy<CoroutineStub> _coroutineStubLoader;

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
            var obj = Holder.GetComponent<CoroutineStub>();
            if (obj != null) return obj;
            obj = Holder.AddComponent<CoroutineStub>();
            return obj;
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
            Launcher.SafeProc(l => l.StopAllCoroutines());
        }

        private class CoroutineStub : MonoBehaviour { }
    }
}
