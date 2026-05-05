using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace PixelShooter3D
{
public class ColorPaletteManager : MonoBehaviour
{
    [Header("References")]
    public Transform colorListContainer;
    public GameObject colorButtonPrefab;
    public GameObject colorPickerPanel;
    public CreateColor createColorPicker; // Reference to the CreateColor script
    public Button closePickerButton;
    public Button removeColorButton;
    public BlockContainerEdit blockContainer;

    private List<Color> uniqueColors = new List<Color>();
    private Color currentColor;
    private int currentColorIndex = -1;

    void Start()
    {
        if (closePickerButton) closePickerButton.onClick.AddListener(CloseColorPicker);
        if (removeColorButton) removeColorButton.onClick.AddListener(RemoveCurrentColorBlocks);

        if (createColorPicker)
        {
            createColorPicker.OnColorChanged += OnColorChanged;
        }

        colorPickerPanel.SetActive(false);
    }

    void Update()
    {
        // Periodically scan for colors (or trigger manually)
        // For now, let's scan every frame or when block layout changes
        // Better: BlockContainerEdit or BlockColorizer should trigger this.
        // But for simplicity, let's scan every 0.5s or check if child count changed

        // Actually, let's just scan every frame for now to be responsive to the colorizer
        ScanColors();
    }

    public void ScanColors()
    {
        if (blockContainer == null) return;

        HashSet<Color> foundColors = new HashSet<Color>();
        foreach (Transform child in blockContainer.transform)
        {
            if (!child.gameObject.activeSelf) continue;
            Renderer r = child.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                foundColors.Add(r.material.color);
            }
        }

        // Check if colors changed
        if (!foundColors.SetEquals(uniqueColors))
        {
            uniqueColors = foundColors.ToList();
            RefreshColorList();
        }
    }

    void RefreshColorList()
    {
        foreach (Transform child in colorListContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < uniqueColors.Count; i++)
        {
            Color c = uniqueColors[i];
            GameObject btnObj = Instantiate(colorButtonPrefab, colorListContainer);
            Image img = btnObj.GetComponent<Image>();
            if (img) img.color = c;

            int index = i;
            Button btn = btnObj.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => OpenColorPicker(index));
        }
    }

    void OpenColorPicker(int index)
    {
        if (index < 0 || index >= uniqueColors.Count) return;

        currentColorIndex = index;
        currentColor = uniqueColors[index];

        // Get the button image to use as preview
        Image btnImage = null;
        if (index < colorListContainer.childCount)
        {
            btnImage = colorListContainer.GetChild(index).GetComponent<Image>();
        }

        if (createColorPicker)
        {
            createColorPicker.SetColor(currentColor, btnImage);
        }

        colorPickerPanel.SetActive(true);
    }

    public void CloseColorPicker()
    {
        colorPickerPanel.SetActive(false);
        currentColorIndex = -1;
    }

    void OnColorChanged(Color newColor)
    {
        if (currentColorIndex == -1) return;

        Color oldColor = uniqueColors[currentColorIndex];

        // Update unique colors list
        uniqueColors[currentColorIndex] = newColor;

        // Update active blocks
        foreach (Transform child in blockContainer.transform)
        {
            if (!child.gameObject.activeSelf) continue;
            Renderer r = child.GetComponentInChildren<Renderer>();
            if (r != null && r.material.color == oldColor)
            {
                r.material.color = newColor;
            }
        }

        // Update button color
        if (currentColorIndex < colorListContainer.childCount)
        {
            Transform btn = colorListContainer.GetChild(currentColorIndex);
            Image img = btn.GetComponent<Image>();
            if (img) img.color = newColor;
        }
    }

    void RemoveCurrentColorBlocks()
    {
        if (currentColorIndex == -1) return;

        Color colorToRemove = uniqueColors[currentColorIndex];

        // Remove blocks with this color
        List<GameObject> blocksToRemove = new List<GameObject>();
        foreach (Transform child in blockContainer.transform)
        {
            if (!child.gameObject.activeSelf) continue;
            Renderer r = child.GetComponentInChildren<Renderer>();
            if (r != null && r.material.color == colorToRemove)
            {
                blocksToRemove.Add(child.gameObject);
            }
        }

        foreach (var block in blocksToRemove)
        {
            Destroy(block);
        }

        CloseColorPicker();

        // Force scan to update UI
        ScanColors();
    }
}
}
