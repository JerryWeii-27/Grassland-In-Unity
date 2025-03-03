using System;
using UnityEditor;
using UnityEngine;

namespace Omnix.Hierarchy
{
    public static class HierarchyUtils
    {
        public struct ColorVariants
        {
            public Color OutsidePrefab;
            public Color InsidePrefab;

            public ColorVariants(Color outsidePrefab, Color insidePrefab)
            {
                OutsidePrefab = outsidePrefab;
                InsidePrefab = insidePrefab;
            }

            public static explicit operator Color(ColorVariants variants)
            {
                return PrettyHierarchy.IsInsidePrefab ? variants.InsidePrefab : variants.OutsidePrefab;
            }
        }

        public struct ColorScheme
        {
            public ColorVariants Default;
            public ColorVariants Selected;
            public ColorVariants SelectedUnfocused;
            public ColorVariants Hovered;

            public ColorScheme(ColorVariants defaultColor, ColorVariants selectedColor, ColorVariants selectedUnfocusedColor, ColorVariants hoveredColor)
            {
                Default = defaultColor;
                Selected = selectedColor;
                SelectedUnfocused = selectedUnfocusedColor;
                Hovered = hoveredColor;
            }
        }

        private static readonly ColorScheme LightThemeColors = new ColorScheme(
            new ColorVariants(new Color(0.784f, 0.784f, 0.784f), new Color(0.639f, 0.812f, 0.820f)), // Default
            new ColorVariants(new Color(0.227f, 0.447f, 0.690f), new Color(0.282f, 0.549f, 0.851f)), // Selected
            new ColorVariants(new Color(0.680f, 0.680f, 0.680f), new Color(0.576f, 0.678f, 0.678f)), // SelectedUnfocused
            new ColorVariants(new Color(0.698f, 0.698f, 0.698f), new Color(0.608f, 0.690f, 0.690f)) // Hovered
        );

        private static readonly ColorScheme DarkThemeColors = new ColorScheme(
            new ColorVariants(new Color(0.219f, 0.219f, 0.219f), new Color(0.176f, 0.259f, 0.259f)), // Default
            new ColorVariants(new Color(0.172f, 0.364f, 0.529f), new Color(0.255f, 0.412f, 0.412f)), // Selected
            new ColorVariants(new Color(0.300f, 0.300f, 0.300f), new Color(0.204f, 0.329f, 0.329f)), // SelectedUnfocused
            new ColorVariants(new Color(0.270f, 0.270f, 0.270f), new Color(0.231f, 0.341f, 0.341f)) // Hovered
        );

        private static bool _shouldAssignedColorScheme = true;
        private static ColorScheme _currentScheme;
        public static readonly Type HIERARCHY_TYPE = Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor");
        
        #region Fields: Icon Names
        private const string EMPTY_ICON = "FolderEmpty Icon";
        private const string EMPTY_PRO_ICON = "FolderEmpty On Icon";
        private const string FILED_ICON = "FolderOpened Icon";
        private const string FILED_PRO_ICON = "FolderOpened On Icon";
        #endregion
        
        public static Color GetBackgroundColor(bool isSelected, bool isHovered, bool isWindowFocused)
        {
            if (_shouldAssignedColorScheme)
                _currentScheme = EditorGUIUtility.isProSkin ? DarkThemeColors : LightThemeColors;


            if (isSelected)
                return (Color)(isWindowFocused ? _currentScheme.Selected : _currentScheme.SelectedUnfocused);

            return (Color)(isHovered ? _currentScheme.Hovered : _currentScheme.Default);
        }

        public static GUIContent GetFolderIcon(bool isFilled)
        {
            if (isFilled) return EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? FILED_PRO_ICON : FILED_ICON);
            return EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? EMPTY_PRO_ICON : EMPTY_ICON);
        }

        public static void StartRenamingObject(GameObject target)
        {
            Selection.activeGameObject = target;
            EditorApplication.delayCall += () =>
            {
                if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == HIERARCHY_TYPE)
                {
                    EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Rename"));
                }
            };
        }
    }
}