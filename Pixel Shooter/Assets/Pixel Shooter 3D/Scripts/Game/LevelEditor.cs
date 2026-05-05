using UnityEngine;
using System.IO;

namespace PixelShooter3D
{
public class LevelEditor : MonoBehaviour
{
    public void AddPigToDeck(int colIndex, int colorCode)
    {
        // Add to GameManager.Instance.deckColumns[colIndex]
        // Instantiate Pig Prefab visually
    }

    public void ExportToJSON()
    {
        LevelData data = new LevelData();
        // Populate data from GameManager state
        string json = JsonUtility.ToJson(data);
        Debug.Log(json);
        // Write to file
    }

    // Link these to UI Buttons in the "EditorOverlay" Canvas
}
}