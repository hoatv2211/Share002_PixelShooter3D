#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ragendom
{
    public class EditorAdMobContainer : EditorAdsContainer
    {
        private const string GOOGLE_ADS_SETTINGS_PATH = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string ADMOB_PLUGIN_URL = "https://github.com/googleads/googleads-mobile-unity/releases";
        private const string ADMOB_DASHBOARD_URL = "https://admob.google.com/";
        private const string ADMOB_QUICKSTART_URL = "https://developers.google.com/admob/unity/quick-start";

        private SerializedProperty androidAppIdProperty;
        private SerializedProperty iosAppIdProperty;
        private SerializedProperty androidBannerIDProperty;
        private SerializedProperty iOSBannerIDProperty;
        private SerializedProperty androidInterstitialIDProperty;
        private SerializedProperty iOSInterstitialIDProperty;
        private SerializedProperty androidRewardedVideoIDProperty;
        private SerializedProperty iOSRewardedVideoIDProperty;
        private SerializedProperty useAppOpenAdProperty;
        private SerializedProperty androidAppOpenAdIDProperty;
        private SerializedProperty iOSAppOpenAdIDProperty;
        private SerializedProperty appOpenAdExpirationHoursTimeProperty;
        private SerializedProperty bannerTypeProperty;
        private SerializedProperty bannerPositionProperty;

        public EditorAdMobContainer() : base("AdMob", "adMobContainer")
        {
        }

        public override void Init(SerializedObject serializedObject)
        {
            base.Init(serializedObject);

            if (containerProperty == null)
                return;

            androidAppIdProperty = containerProperty.FindPropertyRelative("androidAppId");
            iosAppIdProperty = containerProperty.FindPropertyRelative("iosAppId");
            androidBannerIDProperty = containerProperty.FindPropertyRelative("androidBannerID");
            iOSBannerIDProperty = containerProperty.FindPropertyRelative("iOSBannerID");
            androidInterstitialIDProperty = containerProperty.FindPropertyRelative("androidInterstitialID");
            iOSInterstitialIDProperty = containerProperty.FindPropertyRelative("iOSInterstitialID");
            androidRewardedVideoIDProperty = containerProperty.FindPropertyRelative("androidRewardedVideoID");
            iOSRewardedVideoIDProperty = containerProperty.FindPropertyRelative("iOSRewardedVideoID");
            useAppOpenAdProperty = containerProperty.FindPropertyRelative("useAppOpenAd");
            androidAppOpenAdIDProperty = containerProperty.FindPropertyRelative("androidAppOpenAdID");
            iOSAppOpenAdIDProperty = containerProperty.FindPropertyRelative("iOSAppOpenAdID");
            appOpenAdExpirationHoursTimeProperty = containerProperty.FindPropertyRelative("appOpenAdExpirationHoursTime");
            bannerTypeProperty = containerProperty.FindPropertyRelative("bannerType");
            bannerPositionProperty = containerProperty.FindPropertyRelative("bannerPosition");

            SyncWithGoogleSettings();
        }

        protected override void DrawContainerProperties()
        {
            // Application IDs
            EditorGUILayout.LabelField("Application IDs", EditorStyles.boldLabel);

            DrawPropertyWithTestWarning(androidAppIdProperty, "Android App ID", AdMobContainer.TEST_APP_ID);
            DrawPropertyWithTestWarning(iosAppIdProperty, "iOS App ID", AdMobContainer.TEST_APP_ID);

            EditorGUILayout.Space(5);

            // Banner
            EditorGUILayout.LabelField("Banner", EditorStyles.boldLabel);
            DrawPropertyWithTestWarning(androidBannerIDProperty, "Android Banner ID", AdMobContainer.ANDROID_BANNER_TEST_ID);
            DrawPropertyWithTestWarning(iOSBannerIDProperty, "iOS Banner ID", AdMobContainer.IOS_BANNER_TEST_ID);
            EditorGUILayout.PropertyField(bannerTypeProperty, new GUIContent("Banner Type"));
            EditorGUILayout.PropertyField(bannerPositionProperty, new GUIContent("Banner Position"));

            EditorGUILayout.Space(5);

            // Interstitial
            EditorGUILayout.LabelField("Interstitial", EditorStyles.boldLabel);
            DrawPropertyWithTestWarning(androidInterstitialIDProperty, "Android Interstitial ID", AdMobContainer.ANDROID_INTERSTITIAL_TEST_ID);
            DrawPropertyWithTestWarning(iOSInterstitialIDProperty, "iOS Interstitial ID", AdMobContainer.IOS_INTERSTITIAL_TEST_ID);

            EditorGUILayout.Space(5);

            // Rewarded Video
            EditorGUILayout.LabelField("Rewarded Video", EditorStyles.boldLabel);
            DrawPropertyWithTestWarning(androidRewardedVideoIDProperty, "Android Rewarded Video ID", AdMobContainer.ANDROID_REWARDED_VIDEO_TEST_ID);
            DrawPropertyWithTestWarning(iOSRewardedVideoIDProperty, "iOS Rewarded Video ID", AdMobContainer.IOS_REWARDED_VIDEO_TEST_ID);

            EditorGUILayout.Space(5);

            // App Open Ad
            EditorGUILayout.LabelField("App Open Ad", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useAppOpenAdProperty, new GUIContent("Use App Open Ad"));

            if (useAppOpenAdProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithTestWarning(androidAppOpenAdIDProperty, "Android App Open ID", AdMobContainer.ANDROID_OPEN_TEST_ID);
                DrawPropertyWithTestWarning(iOSAppOpenAdIDProperty, "iOS App Open ID", AdMobContainer.IOS_OPEN_TEST_ID);
                EditorGUILayout.PropertyField(appOpenAdExpirationHoursTimeProperty, new GUIContent("Expiration Hours"));
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPropertyWithTestWarning(SerializedProperty property, string label, string testId)
        {
            if (property == null)
                return;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(property, new GUIContent(label));

            if (property.stringValue == testId)
            {
                GUIContent warningIcon = EditorGUIUtility.IconContent("console.warnicon.sml");
                warningIcon.tooltip = "Using test ID. Replace with your real ad unit ID before publishing.";
                GUILayout.Label(warningIcon, GUILayout.Width(20), GUILayout.Height(18));
            }

            EditorGUILayout.EndHorizontal();
        }

        protected override void SpecialButtons()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Set Test App ID"))
            {
                if (androidAppIdProperty != null) androidAppIdProperty.stringValue = AdMobContainer.TEST_APP_ID;
                if (iosAppIdProperty != null) iosAppIdProperty.stringValue = AdMobContainer.TEST_APP_ID;
            }

            if (GUILayout.Button("Set Test IDs"))
            {
                if (androidBannerIDProperty != null) androidBannerIDProperty.stringValue = AdMobContainer.ANDROID_BANNER_TEST_ID;
                if (iOSBannerIDProperty != null) iOSBannerIDProperty.stringValue = AdMobContainer.IOS_BANNER_TEST_ID;
                if (androidInterstitialIDProperty != null) androidInterstitialIDProperty.stringValue = AdMobContainer.ANDROID_INTERSTITIAL_TEST_ID;
                if (iOSInterstitialIDProperty != null) iOSInterstitialIDProperty.stringValue = AdMobContainer.IOS_INTERSTITIAL_TEST_ID;
                if (androidRewardedVideoIDProperty != null) androidRewardedVideoIDProperty.stringValue = AdMobContainer.ANDROID_REWARDED_VIDEO_TEST_ID;
                if (iOSRewardedVideoIDProperty != null) iOSRewardedVideoIDProperty.stringValue = AdMobContainer.IOS_REWARDED_VIDEO_TEST_ID;
                if (androidAppOpenAdIDProperty != null) androidAppOpenAdIDProperty.stringValue = AdMobContainer.ANDROID_OPEN_TEST_ID;
                if (iOSAppOpenAdIDProperty != null) iOSAppOpenAdIDProperty.stringValue = AdMobContainer.IOS_OPEN_TEST_ID;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Useful Links", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Download AdMob Plugin"))
            {
                Application.OpenURL(ADMOB_PLUGIN_URL);
            }

            if (GUILayout.Button("AdMob Dashboard"))
            {
                Application.OpenURL(ADMOB_DASHBOARD_URL);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("AdMob Quick Start Guide"))
            {
                Application.OpenURL(ADMOB_QUICKSTART_URL);
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox("Tested with AdMob Plugin v9.6.0", MessageType.Info);
        }

        private void SyncWithGoogleSettings()
        {
#if MODULE_ADMOB
            try
            {
                SerializedObject googleSettings = LoadGoogleMobileAdsSettings();

                if (googleSettings != null)
                {
                    SerializedProperty googleAndroidAppId = googleSettings.FindProperty("adMobAndroidAppId");
                    SerializedProperty googleIOSAppId = googleSettings.FindProperty("adMobIOSAppId");

                    if (googleAndroidAppId != null && androidAppIdProperty != null)
                    {
                        if (!string.IsNullOrEmpty(androidAppIdProperty.stringValue) &&
                            androidAppIdProperty.stringValue != AdMobContainer.TEST_APP_ID &&
                            (string.IsNullOrEmpty(googleAndroidAppId.stringValue) || googleAndroidAppId.stringValue != androidAppIdProperty.stringValue))
                        {
                            googleAndroidAppId.stringValue = androidAppIdProperty.stringValue;
                            googleSettings.ApplyModifiedProperties();
                        }
                        else if (!string.IsNullOrEmpty(googleAndroidAppId.stringValue) &&
                                 googleAndroidAppId.stringValue != AdMobContainer.TEST_APP_ID &&
                                 (string.IsNullOrEmpty(androidAppIdProperty.stringValue) || androidAppIdProperty.stringValue == AdMobContainer.TEST_APP_ID))
                        {
                            androidAppIdProperty.stringValue = googleAndroidAppId.stringValue;
                        }
                    }

                    if (googleIOSAppId != null && iosAppIdProperty != null)
                    {
                        if (!string.IsNullOrEmpty(iosAppIdProperty.stringValue) &&
                            iosAppIdProperty.stringValue != AdMobContainer.TEST_APP_ID &&
                            (string.IsNullOrEmpty(googleIOSAppId.stringValue) || googleIOSAppId.stringValue != iosAppIdProperty.stringValue))
                        {
                            googleIOSAppId.stringValue = iosAppIdProperty.stringValue;
                            googleSettings.ApplyModifiedProperties();
                        }
                        else if (!string.IsNullOrEmpty(googleIOSAppId.stringValue) &&
                                 googleIOSAppId.stringValue != AdMobContainer.TEST_APP_ID &&
                                 (string.IsNullOrEmpty(iosAppIdProperty.stringValue) || iosAppIdProperty.stringValue == AdMobContainer.TEST_APP_ID))
                        {
                            iosAppIdProperty.stringValue = googleIOSAppId.stringValue;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EditorAdMobContainer]: Failed to sync with Google settings: {e.Message}");
            }
#endif
        }

        private SerializedObject LoadGoogleMobileAdsSettings()
        {
            UnityEngine.Object settingsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GOOGLE_ADS_SETTINGS_PATH);

            if (settingsAsset != null)
                return new SerializedObject(settingsAsset);

            return null;
        }
    }
}
#endif
