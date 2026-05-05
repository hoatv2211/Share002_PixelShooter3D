#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ragendom
{
    [CustomEditor(typeof(AdsSettings))]
    public class AdsSettingsEditor : Editor
    {
        private EditorAdsContainer[] containers;

        private void OnEnable()
        {
            containers = new EditorAdsContainer[]
            {
                new EditorDummyContainer(),
                new EditorAdMobContainer()
            };

            for (int i = 0; i < containers.Length; i++)
            {
                containers[i].Init(serializedObject);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Provider Containers", EditorStyles.boldLabel);

            for (int i = 0; i < containers.Length; i++)
            {
                containers[i].DrawContainer();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
