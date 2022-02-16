using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GeBoCommon.Utilities;
using KKAPI.Studio;
using Studio;

namespace GeBoCommon.Studio
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public static partial class StudioUtils
    {
        public static IEnumerable<TreeNodeObject> EnumerateTreeNodeObjects(TreeNodeObject root = null)
        {
            var roots = ListPool<TreeNodeObject>.Get();

#if deadcode
            if (root != null)
            {
                roots.Add(root);
            }
            else
            {
                root = StudioAPI.GetSelectedObjects().FirstOrDefault()?.treeNodeObject;
                if (root != null)
                {
                    roots.AddRange(root.GetTreeNodeCtrl().GetTreeNodeObjects());
                }
            }
#else
            if (!root.SafeProc(roots.Add))
            {
                root = StudioAPI.GetSelectedObjects().FirstOrDefault()?.treeNodeObject;
                root.SafeProc(r => roots.AddRange(r.GetTreeNodeCtrl().GetTreeNodeObjects()));
            }

#endif
            foreach (var entry in roots)
            {
                yield return entry;
                foreach (var tnObj in entry.child.SelectMany(EnumerateTreeNodeObjects))
                {
                    yield return tnObj;
                }
            }

            ListPool<TreeNodeObject>.Release(roots);
        }

        public static IEnumerable<ObjectCtrlInfo> EnumerateObjects(ObjectCtrlInfo root = null)
        {
            TreeNodeObject tnRoot = null;
            root.SafeProc(r => tnRoot = r.treeNodeObject);

            foreach (var tnObj in EnumerateTreeNodeObjects(tnRoot))
            {
                if (Singleton<global::Studio.Studio>.Instance.dicInfo.TryGetValue(tnObj, out var result))
                {
                    yield return result;
                }
            }
        }
    }
}
