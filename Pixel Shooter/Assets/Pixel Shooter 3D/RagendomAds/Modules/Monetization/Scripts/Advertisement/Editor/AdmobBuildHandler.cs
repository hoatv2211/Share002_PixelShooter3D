#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ragendom
{
    public class AdmobBuildHandler : IPreprocessBuildWithReport
    {
        private const string GOOGLE_ADS_SETTINGS_PATH = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

        public int callbackOrder => -5;

        public void OnPreprocessBuild(BuildReport report)
        {
#if MODULE_ADMOB
            SyncAppIds();
#endif
        }

#if MODULE_ADMOB
        private void SyncAppIds()
        {
            // Find AdsSettings asset
            string[] guids = AssetDatabase.FindAssets("t:AdsSettings");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[AdmobBuildHandler]: No AdsSettings asset found. Skipping AdMob sync.");
                return;
            }

            AdsSettings adsSettings = null;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                adsSettings = AssetDatabase.LoadAssetAtPath<AdsSettings>(path);
                if (adsSettings != null) break;
            }

            if (adsSettings == null)
            {
                Debug.LogWarning("[AdmobBuildHandler]: Could not load AdsSettings. Skipping AdMob sync.");
                return;
            }

            // Load Google Mobile Ads Settings
            Object googleSettingsAsset = AssetDatabase.LoadAssetAtPath<Object>(GOOGLE_ADS_SETTINGS_PATH);

            if (googleSettingsAsset == null)
            {
                // Try to create the settings via reflection
                try
                {
                    System.Type editorType = System.Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettingsEditor, GoogleMobileAds.Editor");
                    if (editorType != null)
                    {
                        System.Reflection.MethodInfo method = editorType.GetMethod("OpenInspector",
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        method?.Invoke(null, null);

                        AssetDatabase.Refresh();
                        googleSettingsAsset = AssetDatabase.LoadAssetAtPath<Object>(GOOGLE_ADS_SETTINGS_PATH);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[AdmobBuildHandler]: Failed to create Google settings: {e.Message}");
                }

                if (googleSettingsAsset == null)
                {
                    Debug.LogWarning("[AdmobBuildHandler]: Google Mobile Ads settings file not found.");
                    return;
                }
            }

            SerializedObject googleSettings = new SerializedObject(googleSettingsAsset);
            SerializedProperty googleAndroidAppId = googleSettings.FindProperty("adMobAndroidAppId");
            SerializedProperty googleIOSAppId = googleSettings.FindProperty("adMobIOSAppId");

            AdMobContainer container = adsSettings.AdMobContainer;
            bool modified = false;

            // Sync Android App ID
            if (googleAndroidAppId != null &&
                string.IsNullOrEmpty(googleAndroidAppId.stringValue) &&
                !string.IsNullOrEmpty(container.AndroidAppId))
            {
                googleAndroidAppId.stringValue = container.AndroidAppId;
                modified = true;
            }

            // Sync iOS App ID
            if (googleIOSAppId != null &&
                string.IsNullOrEmpty(googleIOSAppId.stringValue) &&
                !string.IsNullOrEmpty(container.IOSAppId))
            {
                googleIOSAppId.stringValue = container.IOSAppId;
                modified = true;
            }

            if (modified)
            {
                googleSettings.ApplyModifiedProperties();
                Debug.Log("[AdmobBuildHandler]: Synced app IDs to Google Mobile Ads settings.");
            }
        }
#endif
    }
}
#endif
