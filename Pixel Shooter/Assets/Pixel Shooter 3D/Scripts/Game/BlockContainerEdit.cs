using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace PixelShooter3D
{
public class BlockContainerEdit : MonoBehaviour
{
    [Header("UI References")]
    public Slider rowsSlider;
    public Slider colsSlider;
    public Slider sizeSlider;
    public TextMeshProUGUI rowsText;
    public TextMeshProUGUI colsText;
    public TextMeshProUGUI sizeText;

    [Header("Settings")]
    public GameObject blockPrefab;
    public float blockSpacing = 0.6f; // Default spacing between blocks

    [Header("Slider Settings")]
    [SerializeField] private int rowsMin = 1;
    [SerializeField] private int rowsMax = 10;
    [SerializeField] private int colsMin = 1;
    [SerializeField] private int colsMax = 10;
    [SerializeField] private float sizeMin = 0.5f;
    [SerializeField] private float sizeMax = 2.0f;

    [HideInInspector] public List<GameObject> spawnedBlocks = new List<GameObject>();
    private BlockColorizer colorizer;

    void Start()
    {
        // Clear any existing children to avoid conflicts with other systems (like LevelEditorManager)
        List<Transform> children = new List<Transform>();
        foreach (Transform child in transform) children.Add(child);
        foreach (Transform child in children)
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        spawnedBlocks.Clear();

        colorizer = GetComponent<BlockColorizer>();
        // Setup Sliders
        if (rowsSlider)
        {
            rowsSlider.minValue = rowsMin;
            rowsSlider.maxValue = rowsMax;
            rowsSlider.wholeNumbers = true;
            rowsSlider.onValueChanged.RemoveAllListeners();
            rowsSlider.onValueChanged.AddListener(OnSliderChanged);
            // rowsSlider.value = 5; // Keep current value if already set
        }

        if (colsSlider)
        {
            colsSlider.minValue = colsMin;
            colsSlider.maxValue = colsMax;
            colsSlider.wholeNumbers = true;
            colsSlider.onValueChanged.RemoveAllListeners();
            colsSlider.onValueChanged.AddListener(OnSliderChanged);
            // colsSlider.value = 5; // Keep current value if already set
        }

        if (sizeSlider)
        {
            sizeSlider.minValue = sizeMin;
            sizeSlider.maxValue = sizeMax;
            sizeSlider.onValueChanged.RemoveAllListeners();
            sizeSlider.onValueChanged.AddListener(OnSliderChanged);
            // sizeSlider.value = 1.0f; // Keep current value if already set
        }

        UpdateLayout();
    }

    void OnSliderChanged(float value)
    {
        UpdateLayout();
    }

    void UpdateLayout()
    {
        int rows = Mathf.RoundToInt(rowsSlider.value);
        int cols = Mathf.RoundToInt(colsSlider.value);
        float sizeScale = sizeSlider.value;

        // Update Text
        if (rowsText) rowsText.text = rows.ToString();
        if (colsText) colsText.text = cols.ToString();
        if (sizeText) sizeText.text = sizeScale.ToString("F2");

        // Scale the container
        transform.localScale = new Vector3(sizeScale, 1f, sizeScale);

        int totalBlocks = rows * cols;

        // Clean up nulls
        spawnedBlocks.RemoveAll(item => item == null);

        // Add new blocks if needed
        while (spawnedBlocks.Count < totalBlocks)
        {
            GameObject b = Instantiate(blockPrefab, transform);
            
            // Disable scripts to prevent logic interference
            MonoBehaviour[] scripts = b.GetComponentsInChildren<MonoBehaviour>();
            foreach (var script in scripts)
            {
                script.enabled = false;
            }

            spawnedBlocks.Add(b);
        }

        // Remove extra blocks if needed
        while (spawnedBlocks.Count > totalBlocks)
        {
            GameObject b = spawnedBlocks[spawnedBlocks.Count - 1];
            spawnedBlocks.RemoveAt(spawnedBlocks.Count - 1);
            Destroy(b);
        }

        // Update Positions
        // Center the grid
        float startX = -((cols * blockSpacing) / 2f) + blockSpacing / 2f;
        float startZ = -((rows * blockSpacing) / 2f) + blockSpacing / 2f;

        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (index >= spawnedBlocks.Count) break;

                GameObject b = spawnedBlocks[index];
                b.SetActive(true);

                // Calculate Position
                float x = startX + c * blockSpacing;
                float z = startZ + r * blockSpacing;

                b.transform.localPosition = new Vector3(x, 0, z);
                b.transform.localRotation = Quaternion.identity;
                
                index++;
            }
        }

        if (colorizer != null)
        {
            colorizer.ApplyColors();
        }
    }
}
}
