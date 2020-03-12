using BepInEx.Logging;
using GeBoCommon.Utilities;
using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
#if AI
using AIChara;
#endif

namespace StudioMultiSelectCharaPlugin
{
    public static class Extensions
    {
        public delegate void AddSelectNodeOpenDelegate(TreeNodeCtrl obj, TreeNodeObject treeNodeObject, bool multiple);
        public delegate void DeselectNodeOpenDelegate(TreeNodeCtrl obj, TreeNodeObject _node);

        //private static ManualLogSource Logger => StudioMultiSelectChara.Logger;

        private static readonly SimpleLazy<HookedSimpleCache<ChaFile, CharaId, ChaControl>> _charaIdCache = new SimpleLazy<HookedSimpleCache<ChaFile, CharaId, ChaControl>>(() =>
            new HookedSimpleCache<ChaFile, CharaId, ChaControl>(IdLoader, CacheConverter, true, true));

        private static readonly SimpleLazy<Action<TreeNodeObject>> _SelectInWorkareaAction = new SimpleLazy<Action<TreeNodeObject>>(() =>
        {
            var AddSelectNode = AccessTools.Method(typeof(TreeNodeCtrl), "AddSelectNode");
            var AddSelectNodeDelegate = (AddSelectNodeOpenDelegate)Delegate.CreateDelegate(typeof(AddSelectNodeOpenDelegate), null, AddSelectNode);
            return new Action<TreeNodeObject>((tno) => AddSelectNodeDelegate(tno.GetTreeNodeCtrl(), tno, true));
        });

        private static readonly SimpleLazy<Action<TreeNodeObject>> _UnselectInWorkareaAction = new SimpleLazy<Action<TreeNodeObject>>(() =>
        {
            var DeselectNode = AccessTools.Method(typeof(TreeNodeCtrl), "DeselectNode");
            var DeselectNodeDelegate = (DeselectNodeOpenDelegate)Delegate.CreateDelegate(typeof(DeselectNodeOpenDelegate), null, DeselectNode);
            return new Action<TreeNodeObject>((tno) => DeselectNodeDelegate(tno.GetTreeNodeCtrl(), tno));
        });

        private static readonly SimpleLazy<GetterHandler<TreeNodeObject, TreeNodeCtrl>> _treeNodeCtrlGetter = new SimpleLazy<GetterHandler<TreeNodeObject, TreeNodeCtrl>>(
            () => FastAccess.CreateGetterHandler<TreeNodeObject, TreeNodeCtrl>(AccessTools.Field(typeof(TreeNodeObject), "m_TreeNodeCtrl")));

        internal static HookedSimpleCache<ChaFile, CharaId, ChaControl> CharaIdCache => _charaIdCache.Value;

        private static ChaFile CacheConverter(ChaControl chaControl)
        {
            return chaControl?.chaFile;
        }

        public static bool IsSelectedInWorkarea(this ObjectCtrlInfo objectCtrlInfo)
        {
            return objectCtrlInfo.treeNodeObject.GetTreeNodeCtrl().CheckSelect(objectCtrlInfo.treeNodeObject);
        }

        public static TreeNodeCtrl GetTreeNodeCtrl(this TreeNodeObject obj) => _treeNodeCtrlGetter.Value(obj);

        private static readonly SimpleLazy<GetterHandler<TreeNodeCtrl, List<TreeNodeObject>>> _treeNodeObjectsGetter = new SimpleLazy<GetterHandler<TreeNodeCtrl, List<TreeNodeObject>>>(
            () => FastAccess.CreateGetterHandler<TreeNodeCtrl, List<TreeNodeObject>>(AccessTools.Field(typeof(TreeNodeCtrl), "m_TreeNodeObject")));

        public static List<TreeNodeObject> GetTreeNodeObjects(this TreeNodeCtrl obj) => _treeNodeObjectsGetter.Value(obj);

        public static void UnselectInWorkarea(this ObjectCtrlInfo objectCtrlInfo) => _UnselectInWorkareaAction.Value(objectCtrlInfo?.treeNodeObject);

        public static void SelectInWorkarea(this ObjectCtrlInfo objectCtrlInfo) => _SelectInWorkareaAction.Value(objectCtrlInfo?.treeNodeObject);

        internal static TResult CacheWrapper<T, TResult>(this T obj, Dictionary<T, TResult> cache, Func<T, TResult> loader)
        {
            if (cache.TryGetValue(obj, out TResult cachedResult))
            {
                return cachedResult;
            }
            return cache[obj] = loader(obj);
        }

        private static CharaId IdLoader(ChaFile chaFile)
        {
            return new CharaId(chaFile);
        }

        public static CharaId GetMatchId(this ChaFile chaFile)
        {
            return CharaIdCache.Get(chaFile);
        }

        public static CharaId GetMatchId(this OCIChar ociChar)
        {
            return ociChar?.oiCharInfo.charFile.GetMatchId();
        }
    }
}