using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Omnix.Hierarchy
{
    [InitializeOnLoad]
    public static class MonoBehaviorDropHandler
    {
        static MonoBehaviorDropHandler()
        {
            DragAndDrop.AddDropHandler(HandleDrop);
        }

        private static DragAndDropVisualMode HandleDrop(int dropTargetInstanceId, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            MonoScript script = GetScriptBeingDragged();
            if (script == null) return DragAndDropVisualMode.None;

            Type type = script.GetClass();
            if (type.IsAbstract || type.IsGenericType) return DragAndDropVisualMode.None;

            if (perform == false) return DragAndDropVisualMode.Copy;

            GameObject go = CreateGameObject(ObjectNames.NicifyVariableName(type.Name), dropTargetInstanceId, dropMode, parentForDraggedObjects);
            if (go == null) return DragAndDropVisualMode.None;

            go.AddComponent(type);
            Undo.RegisterCreatedObjectUndo(go, $"Created {go.name}");
            HierarchyUtils.StartRenamingObject(go);
            return DragAndDropVisualMode.None;
        }

        private static GameObject CreateGameObject(string name, int dropTargetInstanceId, HierarchyDropFlags dropMode, Transform parentForDraggedObjects)
        {
            var target = EditorUtility.InstanceIDToObject(dropTargetInstanceId) as GameObject;
            
            if (dropMode == HierarchyDropFlags.DropUpon && target != null)
            {
                // dropping over some gameObject, Unity will handle this.
                return null;
            }

            if (dropMode == HierarchyDropFlags.SearchActive)
            {
                return null;
            }


            Transform created = new GameObject(name).transform;
            created.SetParent(parentForDraggedObjects);
            if (dropMode.HasFlag(HierarchyDropFlags.DropBetween) && target != null)
            {
                created.SetParent(target.transform.parent);
                if (dropMode.HasFlag(HierarchyDropFlags.DropAfterParent)) created.SetSiblingIndex(0);
                else if (dropMode.HasFlag(HierarchyDropFlags.DropAbove)) created.SetSiblingIndex(target.transform.GetSiblingIndex());
                else created.SetSiblingIndex(target.transform.GetSiblingIndex() + 1);
            }

            created.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            return created.gameObject;
        }


        private static MonoScript GetScriptBeingDragged()
        {
            foreach (Object reference in DragAndDrop.objectReferences)
            {
                if (reference is not MonoScript script) continue;

                Type type = script.GetClass();
                if (type != null && type.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    return script;
                }
            }

            return null;
        }
    }
}