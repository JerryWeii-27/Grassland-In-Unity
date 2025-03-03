#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;



namespace Omnix.Hierarchy
{
    [ExecuteAlways]
    public class ObjectInfoHandler : MonoBehaviour, ISerializationCallbackReceiver
    {
        private static ObjectInfoHandler _instance;

        public static ObjectInfoHandler Instance
        {
            get
            {
                if (_instance == null) _instance = FindObjectOfType<ObjectInfoHandler>(true);
                if (_instance == null)
                    _instance = new GameObject("ObjectInfoHandler").AddComponent<ObjectInfoHandler>();
                return _instance;
            }
        }

        [Serializable]
        public class Info
        {
            public GameObject target;
            public string info;
        }

        [SerializeField] private Info[] _infos;
        private Dictionary<GameObject, string> _infosDictionary = new Dictionary<GameObject, string>();

        private void Awake()
        {
            gameObject.hideFlags = HideFlags.HideInHierarchy;
            EditorApplication.RepaintHierarchyWindow();
        }

        public bool TryGetInfo(GameObject target, out string info)
        {
            return _infosDictionary.TryGetValue(target, out info);
        }

        public void SetInfo(GameObject target, string info)
        {
            _infosDictionary[target] = info;
            EditorUtility.SetDirty(this);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _instance = this;
            _infos = new Info[_infosDictionary.Count];
            
            int i = 0;
            foreach (KeyValuePair<GameObject,string> pair in _infosDictionary)
            {
                _infos[i] = new Info() { target = pair.Key, info = pair.Value };
            }
        }

        
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _instance = this;
            _infosDictionary.Clear();
            
            if (_infos == null)
            {
                _infos = Array.Empty<Info>();
                return;
            }
            
            foreach (Info info in _infos)
            {
                _infosDictionary[info.target] = info.info;
            }
        }
    }
}
#endif