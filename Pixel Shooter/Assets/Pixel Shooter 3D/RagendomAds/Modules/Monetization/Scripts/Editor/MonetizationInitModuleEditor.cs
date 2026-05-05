#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ragendom
{
    [CustomEditor(typeof(MonetizationInitModule))]
    public class MonetizationInitModuleEditor : Editor
    {
        private SerializedProperty settingsProperty;

        private void OnEnable()
        {
            settingsProperty = serializedObject.FindProperty("settings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Monetization Init Module", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.PropertyField(settingsProperty, new GUIContent("Monetization Settings"));

            if (settingsProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Please assign MonetizationSettings asset. Create one via Assets > Create > Data > Core > Monetization Settings.", MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
