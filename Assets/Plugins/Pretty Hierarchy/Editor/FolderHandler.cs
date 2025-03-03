using UnityEngine;
using UnityEditor;

namespace Omnix.Hierarchy
{
    public static class FolderHandler
    {
        public static void EnsureTagExists()
        {
            Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset == null || asset.Length <= 0)
            {
                Debug.LogError("Something is seriously wrong. TagManager does not exit.");
                return;
            }

            var so = new SerializedObject(asset[0]);
            SerializedProperty tags = so.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; ++i)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == Settings.FOLDER_TAG)
                {
                    return; // Tag already present, nothing to do.
                }
            }

            tags.arraySize++;
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = Settings.FOLDER_TAG;
            so.ApplyModifiedProperties();
            so.Update();
        }

        public static void ResetPosition(GameObject target)
        {
            if (target.TryGetComponent(out RectTransform rect) == false)
            {
                if (PrettyHierarchy.IsInsidePrefab == false && target.GetComponentInParent<RectTransform>(true) != null)
                {
                    rect = target.AddComponent<RectTransform>();
                }
            }
            
            if (rect != null)
            {
                rect.anchorMax = Vector2.one;
                rect.anchorMin = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
            else
            {
                target.transform.localPosition = Vector3.zero;
            }

            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
        }

        public static void ResetPositionAdvanced(Transform target)
        {
            var children = new Transform[target.childCount];
            for (int i = 0; i < target.childCount; i++)
            {
                children[i] = target.GetChild(i);
            }

            foreach (Transform child in children)
            {
                Undo.RecordObject(child, "Changed parent");
                child.SetParent(null);
            }

            ResetPosition(target.gameObject);

            foreach (Transform child in children)
            {
                Undo.RecordObject(child, "Changed parent");
                child.SetParent(target);
            }
        }
        
        private static void DrawButtons(GameObject target, Rect selectionRect, string info)
        {
            Rect rect = new Rect(selectionRect);
            rect.x += rect.width - rect.height;
            rect.width = rect.height;
            GUIContent iconContent = target.activeInHierarchy ? EditorGUIUtility.IconContent("d_toggle_on_focus") : EditorGUIUtility.IconContent("d_toggle_bg");
            if (GUI.Button(rect, "    "))
            {
                Undo.RecordObject(target, "Change Object Active State");
                target.SetActive(!target.activeInHierarchy);
                EditorUtility.SetDirty(target);
            }

            GUI.Label(rect, iconContent);
            if (info == null) return;
            var aboutContent = EditorGUIUtility.ObjectContent(ObjectInfoHandler.Instance, typeof(ObjectInfoHandler));
            aboutContent.text = null;
            aboutContent.tooltip = null;
            rect.x -= rect.width;
            GUI.Label(rect, aboutContent);
            
            
        }
        
        private static void DrawInfoGizmo()
        {
            
        }

        public static void Handle(GameObject target, Rect rect)
        {
            bool hasInfo = ObjectInfoHandler.Instance.TryGetInfo(target, out string info);
            DrawButtons(target, rect, info);
            ResetPosition(target);
            if (hasInfo) DrawInfoGizmo();

            target.transform.hideFlags = HideFlags.NotEditable | HideFlags.HideInInspector;
            target.hideFlags = HideFlags.HideInInspector;
            SceneVisibilityManager.instance.DisablePicking(target, false);

            PrettyHierarchy.HideDefaultIcon();
            GUIContent guiContent = HierarchyUtils.GetFolderIcon(target.transform.childCount > 0);
            if (hasInfo) guiContent.tooltip = info;
            EditorGUI.LabelField(rect, guiContent);
        }
    }
}