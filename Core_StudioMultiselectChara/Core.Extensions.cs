using System;
using System.Collections.Generic;
using GeBoCommon.Utilities;
using HarmonyLib;
using Studio;
#if AI||HS2
using AIChara;
#endif

namespace StudioMultiSelectCharaPlugin
{
    public static class Extensions
    {
        #region TreeNode reflection

        public delegate void AddSelectNodeOpenDelegate(TreeNodeCtrl obj, TreeNodeObject treeNodeObject, bool multiple);
        public delegate void DeselectNodeOpenDelegate(TreeNodeCtrl obj, TreeNodeObject _node);

        private static readonly SimpleLazy<AddSelectNodeOpenDelegate> _addSelectNode = new SimpleLazy<AddSelectNodeOpenDelegate>(() =>
        {
            var AddSelectNode = AccessTools.Method(typeof(TreeNodeCtrl), "AddSelectNode");
            return (AddSelectNodeOpenDelegate)Delegate.CreateDelegate(typeof(AddSelectNodeOpenDelegate), null, AddSelectNode);
        });

        private static readonly SimpleLazy<DeselectNodeOpenDelegate> _deselectNode = new SimpleLazy<DeselectNodeOpenDelegate>(() =>
        {
            var DeselectNode = AccessTools.Method(typeof(TreeNodeCtrl), "DeselectNode");
            return (DeselectNodeOpenDelegate)Delegate.CreateDelegate(typeof(DeselectNodeOpenDelegate), null, DeselectNode);
        });

        private static void DeselectNode(TreeNodeCtrl obj, TreeNodeObject node) => _deselectNode.Value(obj, node);
        private static void AddSelectNode(TreeNodeCtrl obj, TreeNodeObject treeNodeObject, bool multiple = false) => _addSelectNode.Value(obj, treeNodeObject, multiple);

        private static readonly SimpleLazy<GetterHandler<TreeNodeObject, TreeNodeCtrl>> _treeNodeCtrlGetter = new SimpleLazy<GetterHandler<TreeNodeObject, TreeNodeCtrl>>(
            () => FastAccess.CreateGetterHandler<TreeNodeObject, TreeNodeCtrl>(AccessTools.Field(typeof(TreeNodeObject), "m_TreeNodeCtrl")));

        private static readonly SimpleLazy<GetterHandler<TreeNodeCtrl, List<TreeNodeObject>>> _treeNodeObjectsGetter = new SimpleLazy<GetterHandler<TreeNodeCtrl, List<TreeNodeObject>>>(
            () => FastAccess.CreateGetterHandler<TreeNodeCtrl, List<TreeNodeObject>>(AccessTools.Field(typeof(TreeNodeCtrl), "m_TreeNodeObject")));

        #endregion TreeNode reflection

        #region CharaId cache

        private static readonly SimpleLazy<HookedSimpleCache<ChaFile, CharaId, ChaControl>> _charaIdCache = new SimpleLazy<HookedSimpleCache<ChaFile, CharaId, ChaControl>>(() =>
           new HookedSimpleCache<ChaFile, CharaId, ChaControl>(IdLoader, CacheConverter, true, true));

        internal static HookedSimpleCache<ChaFile, CharaId, ChaControl> CharaIdCache => _charaIdCache.Value;

        private static ChaFile CacheConverter(ChaControl chaControl) => chaControl?.chaFile;

        internal static TResult CacheWrapper<T, TResult>(this T obj, Dictionary<T, TResult> cache, Func<T, TResult> loader)
        {
            if (cache.TryGetValue(obj, out var cachedResult))
            {
                return cachedResult;
            }
            return cache[obj] = loader(obj);
        }

        private static CharaId IdLoader(ChaFile chaFile) => new CharaId(chaFile);

        #endregion CharaId cache

        public static bool IsSelectedInWorkarea(this ObjectCtrlInfo objectCtrlInfo) => objectCtrlInfo.treeNodeObject.GetTreeNodeCtrl().CheckSelect(objectCtrlInfo.treeNodeObject);

        public static void UnselectInWorkarea(this ObjectCtrlInfo objectCtrlInfo)
        {
            var treeNodeObject = objectCtrlInfo?.treeNodeObject;
            if (treeNodeObject != null)
            {
                var treeNodeCtrl = treeNodeObject.GetTreeNodeCtrl();
                // DeselectNode is slow, even when it does nothing, check first
                if (treeNodeCtrl.CheckSelect(treeNodeObject))
                {
                    DeselectNode(treeNodeCtrl, treeNodeObject);
                }
            }
        }

        public static void MultiSelectInWorkarea(this ObjectCtrlInfo objectCtrlInfo)
        {
            var treeNodeObject = objectCtrlInfo?.treeNodeObject;
            if (treeNodeObject != null)
            {
                var treeNodeCtrl = treeNodeObject.GetTreeNodeCtrl();
                // AddSelectNode with multi=true on selected object clears it's selected status, don't want that
                if (!treeNodeCtrl.CheckSelect(treeNodeObject))
                {
                    AddSelectNode(treeNodeCtrl, treeNodeObject, true);
                }
            }
        }

        public static void SelectInWorkarea(this ObjectCtrlInfo objectCtrlInfo)
        {
            var treeNodeObject = objectCtrlInfo?.treeNodeObject;
            if (treeNodeObject != null)
            {
                AddSelectNode(treeNodeObject.GetTreeNodeCtrl(), treeNodeObject);
            }
        }

        public static List<TreeNodeObject> GetTreeNodeObjects(this TreeNodeCtrl treeNodeCtrl) => _treeNodeObjectsGetter.Value(treeNodeCtrl);

        public static TreeNodeCtrl GetTreeNodeCtrl(this TreeNodeObject treeNodeObject) => _treeNodeCtrlGetter.Value(treeNodeObject);

        public static CharaId GetMatchId(this ChaFile chaFile) => CharaIdCache.Get(chaFile);

        public static CharaId GetMatchId(this OCIChar ociChar) => ociChar?.oiCharInfo.charFile.GetMatchId();
    }
}
