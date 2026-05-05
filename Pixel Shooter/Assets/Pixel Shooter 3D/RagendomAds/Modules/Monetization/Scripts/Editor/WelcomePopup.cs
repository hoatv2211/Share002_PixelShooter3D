#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ragendom
{
    public class WelcomePopup : EditorWindow
    {
        private const string PREF_KEY = "Ragendom_AdMob_HideWelcome";
        private const string PUBLISHER_URL = "https://assetstore.unity.com/publishers/39886";

        private bool doNotShowAgain;

        [InitializeOnLoadMethod]
        private static void ShowOnStartup()
        {
            if (EditorPrefs.GetBool(PREF_KEY, false))
                return;

            EditorApplication.delayCall += () =>
            {
                var window = GetWindow<WelcomePopup>(true, "Ragendom", true);
                window.minSize = new Vector2(420, 280);
                window.maxSize = new Vector2(420, 280);
                window.ShowUtility();
            };
        }

        private void OnGUI()
        {
            GUILayout.Space(20);

            // Title
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUILayout.Label("Thank you for using\nAdMob Ads asset!", titleStyle);

            GUILayout.Space(16);

            // Subtitle
            var subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUILayout.Label("If you enjoy this asset, please consider\nchecking out our other assets as well!", subtitleStyle);

            GUILayout.Space(20);

            // Browse button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 36,
                fixedWidth = 260
            };

            if (GUILayout.Button("Browse Ragendom's Assets", buttonStyle))
            {
                Application.OpenURL(PUBLISHER_URL);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            // Do not show again checkbox
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            doNotShowAgain = GUILayout.Toggle(doNotShowAgain, " Do not show this again");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
        }

        private void OnDestroy()
        {
            ApplyPreference();
        }

        private void ApplyPreference()
        {
            if (doNotShowAgain)
            {
                EditorPrefs.SetBool(PREF_KEY, true);
            }
        }
    }
}
#endif
