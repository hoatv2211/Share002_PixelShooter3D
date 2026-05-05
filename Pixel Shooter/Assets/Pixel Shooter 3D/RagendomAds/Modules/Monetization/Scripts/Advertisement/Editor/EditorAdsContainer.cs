#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ragendom
{
    public abstract class EditorAdsContainer
    {
        protected SerializedProperty containerProperty;
        protected string containerName;
        protected string propertyName;
        protected bool isExpanded = true;

        public EditorAdsContainer(string containerName, string propertyName)
        {
            this.containerName = containerName;
            this.propertyName = propertyName;
        }

        public virtual void Init(SerializedObject serializedObject)
        {
            containerProperty = serializedObject.FindProperty(propertyName);
        }

        public virtual void DrawContainer()
        {
            if (containerProperty == null)
                return;

            EditorGUILayout.Space(5);

            isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(isExpanded, containerName);

            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                DrawContainerProperties();

                EditorGUILayout.Space(5);

                SpecialButtons();

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected virtual void DrawContainerProperties()
        {
            if (containerProperty == null)
                return;

            SerializedProperty iterator = containerProperty.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                        break;

                    EditorGUILayout.PropertyField(iterator, true);
                }
                while (iterator.NextVisible(false));
            }
        }

        protected abstract void SpecialButtons();
    }
}
#endif
