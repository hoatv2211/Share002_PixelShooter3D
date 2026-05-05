using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace PixelShooter3D
{
public class DeckContainerEdit : MonoBehaviour
{
    [Header("References")]
    public HoldingContainerEdit holdingContainerEdit;
    public GameObject pigPrefab;

    [Header("UI References")]
    public Slider columnCountSlider;
    public TextMeshProUGUI columnCountText;

    public GameObject pigCountSliderPrefab;
    public Transform slidersContainer;

    [Header("Settings")]
    public float pigWidth = 2f; // Approximate width
    public float sliderScale = 0.0277f;
    public float sliderYOffset = -150f;

    [Header("Slider Settings")]
    [SerializeField] private int colMin = 1;
    [SerializeField] private int colMax = 5;
    [SerializeField] private int pigMin = 1;
    [SerializeField] private int pigMax = 9;

    private List<GameObject> spawnedPigs = new List<GameObject>();
    private List<Slider> pigCountSliders = new List<Slider>();

    void Start()
    {
        // Clear any existing children to avoid conflicts with other systems
        List<Transform> children = new List<Transform>();
        foreach (Transform child in transform) children.Add(child);
        foreach (Transform child in children)
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        spawnedPigs.Clear();

        // Setup Sliders
        if (columnCountSlider)
        {
            columnCountSlider.minValue = colMin;
            columnCountSlider.maxValue = colMax;
            columnCountSlider.wholeNumbers = true;
            columnCountSlider.onValueChanged.RemoveAllListeners();
            columnCountSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        // Listen to HoldingContainer spacing changes
        if (holdingContainerEdit && holdingContainerEdit.spacingSlider)
        {
            holdingContainerEdit.spacingSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        // Initial Layout Update
        UpdateLayout();
    }

    void OnSliderChanged(float value)
    {
        UpdateLayout();
    }

    void UpdateLayout()
    {
        int cols = Mathf.RoundToInt(columnCountSlider.value);

        // Update Column Text
        if (columnCountText) columnCountText.text = cols.ToString();

        // Get spacing from HoldingContainer
        float spacing = 2.44f; // Default
        if (holdingContainerEdit && holdingContainerEdit.spacingSlider)
        {
            spacing = holdingContainerEdit.spacingSlider.value;
        }

        // Manage Sliders
        UpdateSliders(cols);

        // Position Sliders
        float strideX = pigWidth + spacing;
        for (int i = 0; i < pigCountSliders.Count; i++)
        {
            RectTransform rt = pigCountSliders[i].GetComponent<RectTransform>();
            if (rt)
            {
                // Calculate column local X in Deck_Container
                float colLocalX = (i - (cols - 1) / 2f) * strideX;

                // Convert to World Position
                Vector3 worldPos = transform.TransformPoint(new Vector3(colLocalX, 0, 0));

                // Convert to SlidersContainer Local Position
                Vector3 localPos = slidersContainer.InverseTransformPoint(worldPos);

                // Apply X from calculation, and fixed Y/Z/Scale from user request
                rt.localPosition = new Vector3(localPos.x, -12.8f, -6.5f);
                rt.localScale = new Vector3(0.02782612f, 0.02782612f, 0.02782612f);
                rt.localRotation = Quaternion.identity;
            }
        }

        // Calculate total pigs needed and per-column counts
        int totalPigs = 0;
        List<int> pigsPerCol = new List<int>();
        for (int i = 0; i < pigCountSliders.Count; i++)
        {
            Slider slider = pigCountSliders[i];
            int count = Mathf.RoundToInt(slider.value);
            pigsPerCol.Add(count);
            totalPigs += count;

            // Update slider text
            Transform textTrans = slider.transform.Find("Deck_PigsText");
            if (textTrans)
            {
                TextMeshProUGUI valText = textTrans.GetComponent<TextMeshProUGUI>();
                if (valText) valText.text = $"Col {i + 1}: {count}";
            }
        }

        // Clean up nulls
        spawnedPigs.RemoveAll(item => item == null);

        // Add new pigs if needed
        while (spawnedPigs.Count < totalPigs)
        {
            GameObject p = Instantiate(pigPrefab, transform);

            // Disable scripts to prevent logic interference
            MonoBehaviour[] scripts = p.GetComponentsInChildren<MonoBehaviour>();
            foreach (var script in scripts)
            {
                script.enabled = false;
            }

            // Enable Ammo Text
            PigController pc = p.GetComponent<PigController>();
            if (pc && pc.ammoText)
            {
                pc.ammoText.enabled = true;
                pc.ammoText.gameObject.SetActive(true);
                // Ensure alpha is 1
                pc.ammoText.color = new Color(pc.ammoText.color.r, pc.ammoText.color.g, pc.ammoText.color.b, 1f);
            }

            spawnedPigs.Add(p);
        }

        // Remove extra pigs if needed
        while (spawnedPigs.Count > totalPigs)
        {
            GameObject p = spawnedPigs[spawnedPigs.Count - 1];
            spawnedPigs.RemoveAt(spawnedPigs.Count - 1);
            Destroy(p);
        }

        // Update Positions
        // float strideX = pigWidth + spacing; // Already defined above
        float strideZ = pigWidth + spacing; // Same as X distance

        int pigIndex = 0;
        for (int c = 0; c < cols; c++)
        {
            int countInThisCol = (c < pigsPerCol.Count) ? pigsPerCol[c] : 0;
            for (int r = 0; r < countInThisCol; r++)
            {
                if (pigIndex >= spawnedPigs.Count) break;

                GameObject p = spawnedPigs[pigIndex];
                p.SetActive(true);

                // Calculate Position
                // Center columns
                float x = (c - (cols - 1) / 2f) * strideX;
                float z = r * strideZ;

                p.transform.localPosition = new Vector3(x, 0, z);
                p.transform.localRotation = Quaternion.Euler(0, 180, 0); // Face camera

                pigIndex++;
            }
        }
    }

    void UpdateSliders(int targetCount)
    {
        // Add sliders
        while (pigCountSliders.Count < targetCount)
        {
            GameObject sObj = Instantiate(pigCountSliderPrefab, slidersContainer);
            Slider s = sObj.GetComponent<Slider>();

            s.minValue = pigMin;
            s.maxValue = pigMax;
            s.wholeNumbers = true;
            s.value = 3; // Default
            s.onValueChanged.AddListener(OnSliderChanged);

            pigCountSliders.Add(s);
        }

        // Remove sliders
        while (pigCountSliders.Count > targetCount)
        {
            Slider s = pigCountSliders[pigCountSliders.Count - 1];
            pigCountSliders.RemoveAt(pigCountSliders.Count - 1);
            Destroy(s.gameObject);
        }
    }
}
}
