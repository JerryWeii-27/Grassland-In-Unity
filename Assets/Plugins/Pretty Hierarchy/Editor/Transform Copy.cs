using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Omnix.Editor.CopyPasteHelpers
{
    public interface ISourceHolder
    {
        public void Paste(Transform target);
    }

    public class TransformHolder : ISourceHolder
    {
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;
        private readonly Vector3 _localScale;


        public TransformHolder(Transform source)
        {
            _position = source.position;
            _rotation = source.rotation;
            _localScale = source.localScale;
        }

        public void Paste(Transform target)
        {
            if (target is RectTransform rect)
            {
                Undo.RecordObject(rect, $"Paste Transform {target.name}");
                rect.position = _position;
                rect.rotation = _rotation;
                rect.localScale = _localScale;
                EditorUtility.SetDirty(rect);
            }
            else
            {
                Undo.RecordObject(target, $"Paste Transform {target.name}");
                target.position = _position;
                target.rotation = _rotation;
                target.localScale = _localScale;
                EditorUtility.SetDirty(target);
            }
            
        }
    }

    public class RectTransformHolder : ISourceHolder
    {
        private readonly Vector2 _pivot;
        private readonly Vector2 _anchorMax;
        private readonly Vector2 _anchorMin;
        private readonly Vector2 _anchoredPosition;
        private readonly Vector2 _sizeDelta;
        
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;
        private readonly Vector3 _localScale;

        public RectTransformHolder(RectTransform target)
        {
            _pivot = target.pivot;
            _anchorMax = target.anchorMax;
            _anchorMin = target.anchorMin;
            _anchoredPosition = target.anchoredPosition;
            _sizeDelta = target.sizeDelta;
            _position = target.position;
            _rotation = target.rotation;
            _localScale = target.localScale;
        }

        public void Paste(Transform target)
        {
            if (target is RectTransform rect)
            {
                Undo.RecordObject(rect, $"Paste RectTransform {target.name}");
                rect.pivot = _pivot;
                rect.anchorMax = _anchorMax;
                rect.anchorMin = _anchorMin;
                rect.anchoredPosition = _anchoredPosition;
                rect.sizeDelta = _sizeDelta;
                rect.rotation = _rotation;
                rect.localScale = _localScale;
                EditorUtility.SetDirty(rect);
            }
            else
            {
                Undo.RecordObject(target, $"Paste Transform {target.name}");
                target.position = _position;
                target.rotation = _rotation;
                target.localScale = _localScale;
                EditorUtility.SetDirty(target);
            }
        }
    }
    
    public static class CopyPasteHelpers
    {
        public const string OBJECT_MENU = "GameObject/Utils";
        public const string SELECT_MENU = "Utils/Selections/";
        private const string COPY_TRANSFORM = "Copy Transform";
        private const string PASTE_TRANSFORM = "Paste Transform";

        private static ISourceHolder copied;

        [MenuItem(SELECT_MENU + COPY_TRANSFORM + " &C")]
        [MenuItem(OBJECT_MENU + COPY_TRANSFORM)]
        private static void CopyTransform()
        {
            Transform source = Selection.activeTransform;
            if (source == null) return;
            
            if (source.TryGetComponent(out RectTransform sourceRect))
            {
                copied = new RectTransformHolder(sourceRect);
            }
            else
            {
                copied = new TransformHolder(source);
            }
        }

        [MenuItem(SELECT_MENU + COPY_TRANSFORM, true)]
        [MenuItem(OBJECT_MENU + COPY_TRANSFORM, true)]
        public static bool IsSingleObjectSelected() => Selection.activeGameObject != null && Selection.gameObjects.Length == 1;

        
        [MenuItem(SELECT_MENU + PASTE_TRANSFORM + " &V")]
        [MenuItem(OBJECT_MENU + PASTE_TRANSFORM)]
        private static void PasteTransform()
        {
            Transform[] targets = Selection.transforms;
            if (targets != null && targets.Length > 0 && copied != null)
            {
                WrapInUndo("Paste RectTransform", targets, copied.Paste);
            }
        }
        
        /// <summary> Performs given operation on all selected gameObjects and collapse all that in Single Undo operation </summary>
        /// <remarks> Operation must account for undo </remarks>
        public static void WrapInUndo<T>(string undoName, IEnumerable<T> targets, Action<T> operation)
        {
            int undoGroupIndex = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            foreach (T target in targets)
            {
                operation(target);
            }
            Undo.CollapseUndoOperations(undoGroupIndex);
        }
    }
}