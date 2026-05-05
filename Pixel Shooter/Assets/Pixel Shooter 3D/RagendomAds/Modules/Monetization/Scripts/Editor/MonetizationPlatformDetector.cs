#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ragendom
{
    [InitializeOnLoad]
    public static class MonetizationPlatformDetector
    {
        private const string REMIND_LATER_KEY = "MONETIZATION_PLATFORM_REMIND_TIME";

        static MonetizationPlatformDetector()
        {
            EditorApplication.delayCall += CheckPlatform;
        }

        private static void CheckPlatform()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += CheckPlatform;
                return;
            }

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

            if (target == BuildTarget.Android || target == BuildTarget.iOS)
                return;

            // Check remind later
            if (PlayerPrefs.HasKey(REMIND_LATER_KEY))
            {
                long remindTime = long.Parse(PlayerPrefs.GetString(REMIND_LATER_KEY));
                if (System.DateTime.Now.Ticks < remindTime)
                    return;
            }

            int option = EditorUtility.DisplayDialogComplex(
                "Monetization Platform Warning",
                "The current build target is not Android or iOS. The monetization module only works on mobile platforms.\n\nWould you like to disable the module or keep it enabled?",
                "Keep Enabled",
                "Disable Module",
                "Remind Me Later");

            switch (option)
            {
                case 0: // Keep Enabled
                    break;
                case 1: // Disable Module
                    DisableModule();
                    break;
                case 2: // Remind Me Later (120 minutes)
                    long remindTicks = System.DateTime.Now.AddMinutes(120).Ticks;
                    PlayerPrefs.SetString(REMIND_LATER_KEY, remindTicks.ToString());
                    PlayerPrefs.Save();
                    break;
            }
        }

        private static void DisableModule()
        {
            string[] guids = AssetDatabase.FindAssets("t:MonetizationSettings");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                MonetizationSettings settings = AssetDatabase.LoadAssetAtPath<MonetizationSettings>(path);

                if (settings != null)
                {
                    SerializedObject so = new SerializedObject(settings);
                    SerializedProperty isActiveProperty = so.FindProperty("isModuleActive");

                    if (isActiveProperty != null)
                    {
                        isActiveProperty.boolValue = false;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            Debug.Log("[MonetizationPlatformDetector]: Monetization module disabled for non-mobile platform.");
        }
    }
}
#endif
