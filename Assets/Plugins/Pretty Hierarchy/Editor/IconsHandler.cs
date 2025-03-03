using System;
using System.Collections.Generic;
using System.Text;
using Codice.CM.Common;
using UnityEditor;
using UnityEngine;

namespace Omnix.Hierarchy
{
    public static class IconsHandler
    {
        private static Dictionary<Type, Texture> TYPE_CONTENT = new Dictionary<Type, Texture>();
        private static Texture DefaultImage;

        private static bool TryGetContent(Component component, Type type, out GUIContent content)
        {
            if (TYPE_CONTENT.TryGetValue(type, out Texture texture))
            {
                content = new GUIContent(texture);
                return true;
            }

            content = EditorGUIUtility.ObjectContent(component, type);
            if (content.image == null) return false;
            
            TYPE_CONTENT.Add(type, content.image);
            return true;
        }

        private static bool TryGetContent(GameObject target, out GUIContent content)
        {
            if (ObjectInfoHandler.Instance.TryGetInfo(target, out string info))
            {
                content = EditorGUIUtility.ObjectContent(ObjectInfoHandler.Instance, typeof(ObjectInfo));
                content.text = null;
                content.tooltip = info;
                return true;
            }
            
            content = null;
            Component[] components = target.GetComponents<Component>();
            if (components == null || components.Length == 0)
            {
                return false;
            }

            var currentlyHighest = int.MinValue;
            foreach (Component comp in components)
            {
                if (comp == null) continue; // Possible when missing reference script
                Type type = comp.GetType();
                int priority = Settings.PriorityOf(type);

                if (priority <= currentlyHighest) continue;
                if (Settings.IGNORED_TYPES.Contains(type)) continue;
                if (TryGetContent(comp, type, out GUIContent temp) == false) continue;
                if (Settings.IGNORED_NAMES.Contains(temp.image.name)) continue;

                currentlyHighest = priority;
                content = temp;
            }

            if (content == null)
            {
                return false;
            }

            content.text = null;
            content.tooltip = null;
            return true;
        }

        public static void Handle(GameObject target, Rect rect)
        {
            /*if (PrettyHierarchy.IsInsidePrefab)
            {
                Debug.Log($"Target: {target.name}, isp: {PrettyHierarchy.IsSelfPrefab} - Inside Prefab");
                return;
            }*/

            PrettyHierarchy.HideDefaultIcon();

            if (TryGetContent(target, out GUIContent content))
            {
                EditorGUI.LabelField(rect, content);
            }
            else
            {
                if (DefaultImage == null)
                {
                    GUIContent iconContent = EditorGUIUtility.IconContent("GameObject Icon");
                    DefaultImage = iconContent.image as Texture2D;
                }

                rect.width = rect.height;
                GUI.DrawTexture(rect, DefaultImage);
            }
        }
    }
}