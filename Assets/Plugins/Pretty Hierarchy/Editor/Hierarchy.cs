using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Omnix.Hierarchy
{
    [InitializeOnLoad]
    public static class PrettyHierarchy
    {
        private static bool hierarchyHasFocus;
        public static bool IsInsidePrefab { get; private set; }
        private static Color curBackColor;
        private static Rect curRect;
        private static GameObject target;

        static PrettyHierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
            EditorApplication.update += ApplicationUpdate;
        }

        private static void ApplicationUpdate()
        {
            hierarchyHasFocus = EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == HierarchyUtils.HIERARCHY_TYPE;
        }

        private static void HierarchyWindowItemOnGUI(int instanceID, Rect rect)
        {
            target = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (target == null) return;

            IsInsidePrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(target) != null;
            bool isSelected = Selection.instanceIDs.Contains(instanceID);
            bool isHovering = IsHovering(rect);
            
            curRect = rect;
            curBackColor = HierarchyUtils.GetBackgroundColor(isSelected, isHovering, hierarchyHasFocus);

            FolderHandler.EnsureTagExists();
            if (target.CompareTag(Settings.FOLDER_TAG)) FolderHandler.Handle(target, rect);
            else IconsHandler.Handle(target, rect);
        }

        private static bool IsHovering(Rect rect)
        {
            rect.width += rect.x;
            rect.x = 0f;
            var mousePos = Event.current.mousePosition;
            return rect.Contains(mousePos);
        }
        
        public static void HideDefaultIcon()
        {
            //Rect totalRect = curRect;
            //totalRect.width += totalRect.x;
            //totalRect.x = 0;
            EditorGUI.DrawRect(curRect, curBackColor);
            
            Rect backRect = new Rect(curRect.x + 18.5f, curRect.y, curRect.width - 18.5f, curRect.height);
            Color color;
            if (IsInsidePrefab) color = target.activeInHierarchy ? new Color(0.573f, 0.918f, 0.929f) : new Color(0.502f, 0.702f, 0.710f);
            else color = target.activeInHierarchy ? Color.white : new Color(0.729f, 0.729f, 0.729f);
            var guiColor= GUI.contentColor;
            GUI.contentColor = color;
            EditorGUI.LabelField(backRect, target.name);
            GUI.contentColor = guiColor;
        }
    }
}