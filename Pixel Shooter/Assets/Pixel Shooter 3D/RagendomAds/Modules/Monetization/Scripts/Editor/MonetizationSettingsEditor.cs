#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ragendom
{
    [CustomEditor(typeof(MonetizationSettings))]
    public class MonetizationSettingsEditor : Editor
    {
        private SerializedProperty isModuleActiveProperty;
        private SerializedProperty adsSettingsProperty;
        private SerializedProperty verboseLoggingProperty;
        private SerializedProperty debugModeProperty;
        private SerializedProperty testDevicesProperty;
        private SerializedProperty privacyLinkProperty;
        private SerializedProperty termsOfUseLinkProperty;

        private Editor adsSettingsEditor;
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Ads" };

        private void OnEnable()
        {
            isModuleActiveProperty = serializedObject.FindProperty("isModuleActive");
            adsSettingsProperty = serializedObject.FindProperty("adsSettings");
            verboseLoggingProperty = serializedObject.FindProperty("verboseLogging");
            debugModeProperty = serializedObject.FindProperty("debugMode");
            testDevicesProperty = serializedObject.FindProperty("testDevices");
            privacyLinkProperty = serializedObject.FindProperty("privacyLink");
            termsOfUseLinkProperty = serializedObject.FindProperty("termsOfUseLink");

            if (adsSettingsProperty.objectReferenceValue != null)
            {
                adsSettingsEditor = CreateEditor(adsSettingsProperty.objectReferenceValue);
            }
        }

        private void OnDisable()
        {
            if (adsSettingsEditor != null)
            {
                DestroyImmediate(adsSettingsEditor);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Mobile Monetization", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.PropertyField(isModuleActiveProperty, new GUIContent("Module Active"));

            if (!isModuleActiveProperty.boolValue)
            {
                EditorGUILayout.HelpBox("Monetization module is disabled.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(verboseLoggingProperty, new GUIContent("Verbose Logging"));
            EditorGUILayout.PropertyField(debugModeProperty, new GUIContent("Debug Mode"));

            if (debugModeProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(testDevicesProperty, new GUIContent("Test Devices"), true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(privacyLinkProperty, new GUIContent("Privacy Policy Link"));
            EditorGUILayout.PropertyField(termsOfUseLinkProperty, new GUIContent("Terms of Use Link"));

            EditorGUILayout.Space(10);

            // Tab toolbar
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            EditorGUILayout.Space(5);

            switch (selectedTab)
            {
                case 0: // Ads
                    DrawAdsTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAdsTab()
        {
            if (adsSettingsProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("AdsSettings sub-asset is not assigned. Click 'Create AdsSettings' to fix this.", MessageType.Error);

                if (GUILayout.Button("Create AdsSettings"))
                {
                    CreateAdsSettingsSubAsset();
                }
                return;
            }

            if (adsSettingsEditor == null)
            {
                adsSettingsEditor = CreateEditor(adsSettingsProperty.objectReferenceValue);
            }

            if (adsSettingsEditor != null)
            {
                adsSettingsEditor.OnInspectorGUI();
            }
        }

        private void CreateAdsSettingsSubAsset()
        {
            MonetizationSettings monetizationSettings = (MonetizationSettings)target;

            AdsSettings adsSettings = CreateInstance<AdsSettings>();
            adsSettings.name = "AdsSettings";

            string assetPath = AssetDatabase.GetAssetPath(monetizationSettings);

            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[MonetizationSettingsEditor]: MonetizationSettings must be saved as an asset first.");
                return;
            }

            AssetDatabase.AddObjectToAsset(adsSettings, monetizationSettings);

            adsSettingsProperty.objectReferenceValue = adsSettings;
            serializedObject.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            adsSettingsEditor = CreateEditor(adsSettings);

            Debug.Log("[MonetizationSettingsEditor]: AdsSettings sub-asset created successfully.");
        }

        [MenuItem("Assets/Create/Data/Core/Monetization Settings")]
        public static void CreateAsset()
        {
            MonetizationSettings monetizationSettings = CreateInstance<MonetizationSettings>();

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
                path = "Assets";
            else if (System.IO.Path.GetExtension(path) != "")
                path = path.Replace(System.IO.Path.GetFileName(path), "");

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/MonetizationSettings.asset");
            AssetDatabase.CreateAsset(monetizationSettings, assetPath);

            // Create AdsSettings as sub-asset
            AdsSettings adsSettings = CreateInstance<AdsSettings>();
            adsSettings.name = "AdsSettings";
            AssetDatabase.AddObjectToAsset(adsSettings, monetizationSettings);

            // Link via SerializedObject
            SerializedObject so = new SerializedObject(monetizationSettings);
            so.FindProperty("adsSettings").objectReferenceValue = adsSettings;
            so.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = monetizationSettings;

            Debug.Log($"[MonetizationSettingsEditor]: MonetizationSettings created at {assetPath}");
        }
    }
}
#endif
