using System;
using System.Collections.Generic;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI.Chara;
using Studio;

#if AI||HS2
using AIChara;
#endif

namespace StudioMultiSelectCharaPlugin
{
    [PublicAPI]
    public static class Extensions
    {
        public static bool IsSelectedInWorkarea(this ObjectCtrlInfo objectCtrlInfo)
        {
            return objectCtrlInfo.treeNodeObject.GetTreeNodeCtrl().CheckSelect(objectCtrlInfo.treeNodeObject);
        }

        public static void UnselectInWorkarea(this ObjectCtrlInfo objectCtrlInfo)
        {
            var treeNodeObject = objectCtrlInfo?.treeNodeObject;
            if (treeNodeObject == null) return;
            var treeNodeCtrl = treeNodeObject.GetTreeNodeCtrl();
            // DeselectNode is slow, even when it does nothing, check first
            if (!treeNodeCtrl.CheckSelect(treeNodeObject)) return;
            DeselectNode(treeNodeCtrl, treeNodeObject);
        }

        public static void MultiSelectInWorkarea(this ObjectCtrlInfo objectCtrlInfo)
        {
            var treeNodeObject = objectCtrlInfo?.treeNodeObject;
            if (treeNodeObject == null) return;
            var treeNodeCtrl = treeNodeObject.GetTreeNodeCtrl();
            // AddSelectNode with multi=true on selected object clears it's selected status, don't want that
            if (treeNodeCtrl.CheckSelect(treeNodeObject)) return;
            AddSelectNode(treeNodeCtrl, treeNodeObject, true);
        }

        public static void SelectInWorkarea(this ObjectCtrlInfo objectCtrlInfo)
        {
            var treeNodeObject = objectCtrlInfo?.treeNodeObject;
            if (treeNodeObject != null)
            {
                AddSelectNode(treeNodeObject.GetTreeNodeCtrl(), treeNodeObject);
            }
        }

        public static List<TreeNodeObject> GetTreeNodeObjects(this TreeNodeCtrl treeNodeCtrl)
        {
            return LazyTreeNodeObjectsGetter.Value(treeNodeCtrl);
        }

        public static TreeNodeCtrl GetTreeNodeCtrl(this TreeNodeObject treeNodeObject)
        {
            return LazyTreeNodeCtrlGetter.Value(treeNodeObject);
        }

        public static CharaId GetMatchId(this ChaFile chaFile)
        {
            return CharaIdCache.Get(chaFile);
        }

        public static CharaId GetMatchId(this OCIChar ociChar)
        {
            return ociChar?.oiCharInfo.charFile.GetMatchId();
        }

        #region TreeNode reflection

        public delegate void AddSelectNodeOpenDelegate(TreeNodeCtrl obj, TreeNodeObject treeNodeObject, bool multiple);

        public delegate void DeselectNodeOpenDelegate(TreeNodeCtrl obj, TreeNodeObject node);

        private static readonly SimpleLazy<AddSelectNodeOpenDelegate> LazyAddSelectNode =
            new SimpleLazy<AddSelectNodeOpenDelegate>(() =>
            {
                var addSelectNode = AccessTools.Method(typeof(TreeNodeCtrl), "AddSelectNode");
                return (AddSelectNodeOpenDelegate)Delegate.CreateDelegate(typeof(AddSelectNodeOpenDelegate), null,
                    addSelectNode);
            });

        private static readonly SimpleLazy<DeselectNodeOpenDelegate> LazyDeselectNode =
            new SimpleLazy<DeselectNodeOpenDelegate>(() =>
            {
                var deselectNode = AccessTools.Method(typeof(TreeNodeCtrl), "DeselectNode");
                return (DeselectNodeOpenDelegate)Delegate.CreateDelegate(typeof(DeselectNodeOpenDelegate), null,
                    deselectNode);
            });

        private static void DeselectNode(TreeNodeCtrl obj, TreeNodeObject node)
        {
            LazyDeselectNode.Value(obj, node);
        }

        private static void AddSelectNode(TreeNodeCtrl obj, TreeNodeObject treeNodeObject, bool multiple = false)
        {
            LazyAddSelectNode.Value(obj, treeNodeObject, multiple);
        }


        private static readonly SimpleLazy<Func<TreeNodeObject, TreeNodeCtrl>> LazyTreeNodeCtrlGetter =
            new SimpleLazy<Func<TreeNodeObject, TreeNodeCtrl>>(() =>
                Delegates.LazyReflectionInstanceGetter<TreeNodeObject, TreeNodeCtrl>("m_TreeNodeCtrl"));

        private static readonly SimpleLazy<Func<TreeNodeCtrl, List<TreeNodeObject>>> LazyTreeNodeObjectsGetter
            = new SimpleLazy<Func<TreeNodeCtrl, List<TreeNodeObject>>>(() =>
                Delegates.LazyReflectionInstanceGetter<TreeNodeCtrl, List<TreeNodeObject>>("m_TreeNodeObject"));

        #endregion TreeNode reflection

        #region CharaId cache

        private static readonly SimpleLazy<HookedSimpleCache<ChaFile, CharaId, ChaControl>> LazyCharaIdCache =
            new SimpleLazy<HookedSimpleCache<ChaFile, CharaId, ChaControl>>(() =>
            {
                var cache = new HookedSimpleCache<ChaFile, CharaId, ChaControl>(IdLoader, CacheConverter, true, true);
                CharacterApi.CharacterReloaded += (sender, e) =>
                {
                    e.ReloadedCharacter.SafeProc(rc => cache.Remove(rc.chaFile));
                };
                return cache;
            });


        internal static HookedSimpleCache<ChaFile, CharaId, ChaControl> CharaIdCache => LazyCharaIdCache.Value;

        private static ChaFile CacheConverter(ChaControl chaControl)
        {
            return chaControl == null ? null : chaControl.chaFile;
        }

        internal static TResult CacheWrapper<T, TResult>(this T obj, Dictionary<T, TResult> cache,
            Func<T, TResult> loader)
        {
            if (cache.TryGetValue(obj, out var cachedResult))
            {
                return cachedResult;
            }

            return cache[obj] = loader(obj);
        }

        private static CharaId IdLoader(ChaFile chaFile)
        {
            return new CharaId(chaFile);
        }

        #endregion CharaId cache
    }
}
