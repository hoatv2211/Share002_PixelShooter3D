using UnityEngine;
using UnityEditor;

namespace PixelShooter3D
{
    public class LevelProgressEditor : Editor
    {
        [MenuItem("Tools/Pixel Shooter 3D/Reset Level Progress")]
        public static void ResetLevelProgress()
        {
            PlayerPrefs.DeleteKey("CurrentLevel");
            PlayerPrefs.DeleteKey("CurrentLevelName");
            PlayerPrefs.Save();
            Debug.Log("[LevelProgressEditor] Level progress reset! Game will start from first level.");
        }

        [MenuItem("Tools/Pixel Shooter 3D/Show Current Level")]
        public static void ShowCurrentLevel()
        {
            string currentLevelName = PlayerPrefs.GetString("CurrentLevelName", "(not set)");
            Debug.Log($"[LevelProgressEditor] Current saved level: {currentLevelName}");
        }
    }
}
