using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Omnix.Hierarchy
{
    public static class Settings
    {
        public const string FOLDER_TAG = "Folder";

        /// <summary> Priority of component not listed in <see cref="PRIORITIES"/> list </summary>
        private const int DEFAULT_PRIORITY = 0;

        // @formatter:off
        /// <summary>
        /// Icon of Component with highest number will be displayed 
        /// </summary>
        private static readonly Dictionary<Type, int> PRIORITIES = new Dictionary<Type, int>()
        {
            { typeof(Button),         -1 },        ////////////////////////////////////////////
            { typeof(TMP_InputField), -1 },        // If an GameObject has any of these,     //
            { typeof(TMP_Dropdown),   -1 },        // then most often that GameObject        //
            { typeof(InputField),     -1 },        // also has an Image, in which chase      //
            { typeof(Scrollbar),      -1 },        // these components should be prioritized //
            { typeof(ScrollRect),     -1 },        // over the image component               //
            { typeof(Dropdown),       -1 },        ////////////////////////////////////////////
            { typeof(Image),          -2 },
            
            { typeof(BoxCollider),         1 },
            { typeof(CapsuleCollider),     1 },
            { typeof(MeshCollider),        1 },
            { typeof(SphereCollider),      1 },
            { typeof(TerrainCollider),     1 },
            { typeof(WheelCollider),       1 },
            { typeof(BoxCollider2D),       1 },
            { typeof(CapsuleCollider2D),   1 },
            { typeof(CompositeCollider2D), 1 },
            { typeof(CustomCollider2D),    1 },
            { typeof(EdgeCollider2D),      1 },
            { typeof(PolygonCollider2D),   1 },
            { typeof(TilemapCollider2D),   1 },
        };
        // @formatter:on

        /// <summary>
        /// Types of components whose icon shall never be shown
        /// </summary>
        public static readonly HashSet<Type> IGNORED_TYPES = new HashSet<Type>
        {
            typeof(Transform),
            typeof(RectTransform),
            typeof(CanvasRenderer) // Canvas renderer is never alone, so ignore it
        };

        /// <summary>
        /// Names of icons that shall never be shown
        /// </summary>
        public static readonly HashSet<string> IGNORED_NAMES = new HashSet<string>
        {
            "d_cs Script Icon" // Icon of default mono-script.
        };

        public static int PriorityOf(Type type)
        {
            if (PRIORITIES.TryGetValue(type, out int value)) return value;
            return DEFAULT_PRIORITY;
        }
    }
}