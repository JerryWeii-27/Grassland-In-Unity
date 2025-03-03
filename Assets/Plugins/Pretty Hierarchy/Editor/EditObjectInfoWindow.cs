using UnityEditor;
using UnityEngine;

namespace Omnix.Hierarchy
{
    public class EditObjectInfoWindow : EditorWindow
    {
        private GUIStyle _buttonStyle;
        private GameObject _target;
        private string _currentText;
        
        private void OnEnable()
        {
            _target = Selection.activeGameObject;
            if (_target == null)
            {
                EditorApplication.delayCall += Close;
                return;
            }
            
            if (ObjectInfoHandler.Instance.TryGetInfo(_target, out _currentText) == false)
                _currentText = "";
        }

        private void OnGUI()
        {
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(EditorStyles.toolbarButton);
                _buttonStyle.fixedHeight = 50f;
            }
            if (_target == null)
            {
                EditorGUILayout.HelpBox("Something went wrong", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField($"Editing {_target.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            _currentText = EditorGUILayout.TextArea(_currentText, GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", _buttonStyle, GUILayout.Height(50f)))
                Save();
            
            if (GUILayout.Button("Cancel", _buttonStyle, GUILayout.Height(50f)))
                Close();
            EditorGUILayout.EndHorizontal();
        }

        private void Save()
        {
            if (_target == null || string.IsNullOrEmpty(_currentText))
            {
                EditorUtility.DisplayDialog("Error", "Something went wrong, try again", "Okay");
                return;
            }
            
            ObjectInfoHandler.Instance.SetInfo(_target, _currentText);
            Close();
        }
    }
}