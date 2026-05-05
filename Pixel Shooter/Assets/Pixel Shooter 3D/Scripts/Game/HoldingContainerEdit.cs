using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace PixelShooter3D
{
public class HoldingContainerEdit : MonoBehaviour
{
    [Header("UI References")]
    public Slider countSlider;
    public Slider spacingSlider;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI spacingText;

    [Header("Settings")]
    public float cubeWidth = 2f; // Approximate width based on bounds

    [Header("Count Slider Settings")]
    [SerializeField] private int countMin = 1;
    [SerializeField] private int countMax = 5;
    [SerializeField] private int countDefault = 5;

    [Header("Spacing Slider Settings")]
    [SerializeField] private float spacingMin = 2f;
    [SerializeField] private float spacingMax = 3f;
    [SerializeField] private float spacingDefault = 2.44f;

    private List<Transform> children = new List<Transform>();

    void Start()
    {
        // Initialize children list
        foreach (Transform child in transform)
        {
            children.Add(child);
        }

        // Ensure we have enough children (instantiate copies of the first one if needed)
        if (children.Count > 0 && children.Count < countMax)
        {
            Transform prefab = children[0];
            while (children.Count < countMax)
            {
                Transform newChild = Instantiate(prefab, transform);
                newChild.localPosition = prefab.localPosition; // Keep original pos initially
                newChild.localRotation = prefab.localRotation;
                newChild.localScale = prefab.localScale;
                children.Add(newChild);
            }
        }

        // Setup Sliders
        if (countSlider)
        {
            countSlider.minValue = countMin;
            countSlider.maxValue = countMax;
            countSlider.wholeNumbers = true;
            countSlider.onValueChanged.AddListener(OnCountChanged);
            // Initialize value
            countSlider.value = countDefault;
        }

        if (spacingSlider)
        {
            spacingSlider.minValue = spacingMin;
            spacingSlider.maxValue = spacingMax;
            spacingSlider.onValueChanged.AddListener(OnSpacingChanged);
            // Initialize value
            spacingSlider.value = spacingDefault;
        }

        UpdateLayout();
    }

    void OnCountChanged(float value)
    {
        UpdateLayout();
    }

    void OnSpacingChanged(float value)
    {
        UpdateLayout();
    }

    void UpdateLayout()
    {
        int count = Mathf.RoundToInt(countSlider.value);
        float spacing = spacingSlider.value;

        // Update Text
        if (countText) countText.text = count.ToString();
        if (spacingText) spacingText.text = spacing.ToString("F2");

        // Enable/Disable
        int activeCount = 0;
        for (int i = 0; i < children.Count; i++)
        {
            bool isActive = i < count;
            children[i].gameObject.SetActive(isActive);
            if (isActive) activeCount++;
        }

        // Calculate positions (Center the group)
        // Assuming X axis layout
        // Stride = Width + Spacing
        float stride = cubeWidth + spacing;
        float totalWidth = (activeCount - 1) * stride;
        float startX = -totalWidth / 2f;

        int current = 0;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i].gameObject.activeSelf)
            {
                Vector3 pos = children[i].localPosition;
                pos.x = startX + (current * stride);
                children[i].localPosition = pos;
                current++;
            }
        }
    }
}
}
