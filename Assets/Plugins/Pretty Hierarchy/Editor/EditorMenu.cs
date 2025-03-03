using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Omnix.Hierarchy
{
    public static class EditorMenu
    {
        private const string MENU_PATH = "Assets/Copy asset GUID";

        [MenuItem(MENU_PATH, priority = 0)]
        private static void ShowMenuItem()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            GUIUtility.systemCopyBuffer = AssetDatabase.AssetPathToGUID(path);
            EditorUtility.DisplayDialog("Done", "Copied object guid to clipboard", "Okay");
        }

        [MenuItem(MENU_PATH, validate = true)]
        private static bool ShowMenuItemValidation()
        {
            return Selection.objects.Length == 1;
        }


        [MenuItem("GameObject/Create Folder", true)]
        private static bool ValidateCreate() => Selection.objects.Length <= 1;

        [MenuItem("GameObject/Turn Into Folder", true)]
        private static bool ValidateTurnInto() => Selection.gameObjects.Length == 1 && Selection.gameObjects[0].CompareTag("Folder") == false;

        [MenuItem("GameObject/Edit Info", true)]
        private static bool ValidateEditInfo() => Selection.gameObjects.Length == 1;

        [MenuItem("GameObject/Create Folder", false, -1)]
        private static void Create()
        {
            var folder = new GameObject("New Folder");
            Undo.RegisterCreatedObjectUndo(folder, folder.name);
            Transform activeTransform = Selection.activeTransform;
            if (activeTransform != null)
            {
                folder.transform.SetParent(activeTransform);
                if (activeTransform.GetComponentInParent<RectTransform>(true) != null)
                {
                    folder.AddComponent<RectTransform>();
                }
            }

            FolderHandler.ResetPosition(folder);
            BasicSetup(folder);
            HierarchyUtils.StartRenamingObject(folder);
        }

        [MenuItem("GameObject/Turn Into Folder", false, -2)]
        private static void TurnTo()
        {
            GameObject active = Selection.activeGameObject;
            if (active == null) return;

            if (GetComponentsCount(active) != 0)
            {
                bool shouldContinue = EditorUtility.DisplayDialog("Confirm", $"The object {active.name} has component(s) other than Transform.\nAll these components will be destroyed.\nWish to continue?", "Yes", "No");
                if (shouldContinue == false) return;
            }

            foreach (Component component in active.GetComponents<Component>().Where(component => component is not Transform).ToList())
            {
                Object.DestroyImmediate(component);
            }

            FolderHandler.ResetPositionAdvanced(active.transform);
            BasicSetup(active);
        }

        [MenuItem("GameObject/Edit Info", false, priority = -3)]
        private static void EditInfo()
        {
            EditorWindow.GetWindow<EditObjectInfoWindow>();
        }

        private static int GetComponentsCount(GameObject target)
        {
            int count = 0;
            foreach (Component component in target.GetComponents<Component>())
            {
                if (component is RectTransform or Transform) continue;
                count++;
            }

            return count;
        }

        private static void BasicSetup(GameObject folder)
        {
            FolderHandler.EnsureTagExists();
            folder.tag = Settings.FOLDER_TAG;

            EditorUtility.SetDirty(folder);
            Selection.activeGameObject = folder;
        }
    }
}