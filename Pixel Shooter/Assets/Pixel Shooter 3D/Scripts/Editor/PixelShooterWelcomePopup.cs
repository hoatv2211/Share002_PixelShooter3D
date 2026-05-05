#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PixelShooter3D
{
    public class PixelShooterWelcomePopup : EditorWindow
    {
        private const string PREF_HIDE_FOREVER = "PixelShooter3D_HideWelcomePopup";
        private const string SESSION_KEY = "PixelShooter3D_WelcomeShownThisSession";

        private const string PIXEL_SHOOTER_URL =
            "https://assetstore.unity.com/packages/templates/packs/pixel-shooter-3d-jam-template-editor-154253";

        private const string HYPERCASUAL_URL =
            "https://assetstore.unity.com/packages/templates/packs/hypercasual-game-engine-mobile-puzzle-templates-136631";

        private bool doNotShowAgain;

        [InitializeOnLoadMethod]
        private static void ShowOnStartup()
        {
            if (EditorPrefs.GetBool(PREF_HIDE_FOREVER, false))
                return;

            if (SessionState.GetBool(SESSION_KEY, false))
                return;

            SessionState.SetBool(SESSION_KEY, true);

            EditorApplication.delayCall += () =>
            {
                var window = GetWindow<PixelShooterWelcomePopup>(true, "Pixel Shooter 3D", true);
                window.minSize = new Vector2(480, 380);
                window.maxSize = new Vector2(480, 380);
                window.ShowUtility();
            };
        }

        private void OnGUI()
        {
            GUILayout.Space(16);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUILayout.Label("Thank you for using\nPixel Shooter 3D!", titleStyle);

            GUILayout.Space(14);

            // --- Review section ---
            var bodyStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUILayout.Label(
                "Your reviews help us keep improving this asset.\n" +
                "If more people leave a review, more developers can discover\n" +
                "Pixel Shooter 3D, and that helps us continue development.\n" +
                "Please take a moment to leave a review — it really helps!",
                bodyStyle);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var reviewBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 34,
                fixedWidth = 320
            };

            if (GUILayout.Button("\u2b50  Leave a Review on the Asset Store", reviewBtnStyle))
            {
                Application.OpenURL(PIXEL_SHOOTER_URL);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(18);

            // --- HyperCasual Engine section ---
            var separator = new GUIStyle(GUI.skin.box) { fixedHeight = 1, stretchWidth = true };
            GUILayout.Box(GUIContent.none, separator);

            GUILayout.Space(12);

            GUILayout.Label(
                "Pixel Shooter 3D is made with the HyperCasual Game Engine.\n" +
                "Get even more games and templates like this one!",
                bodyStyle);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var hcBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 34,
                fixedWidth = 320
            };

            if (GUILayout.Button("\ud83d\ude80  Get HyperCasual Game Engine", hcBtnStyle))
            {
                Application.OpenURL(HYPERCASUAL_URL);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            // --- Do not show again ---
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            doNotShowAgain = GUILayout.Toggle(doNotShowAgain, " Do not show this popup again");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
        }

        private void OnDestroy()
        {
            if (doNotShowAgain)
            {
                EditorPrefs.SetBool(PREF_HIDE_FOREVER, true);
            }
        }
    }
}
#endif
