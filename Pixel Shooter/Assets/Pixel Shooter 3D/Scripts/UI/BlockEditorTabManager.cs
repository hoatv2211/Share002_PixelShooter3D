using UnityEngine;
using UnityEngine.UI;

namespace PixelShooter3D
{
public class BlockEditorTabManager : MonoBehaviour
{
    [Header("Tabs")]
    public Button spawnTabButton;
    public Button colorizeTabButton;

    [Header("Panels")]
    public GameObject spawnPropertiesPanel;
    public GameObject colorizePropertiesPanel;

    [Header("Colors")]
    public Color selectedColor = new Color(1f, 0.92f, 0.016f, 1f); // Yellowish
    public Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Grayish

    void Start()
    {
        // Setup listeners
        if (spawnTabButton) spawnTabButton.onClick.AddListener(ShowSpawnTab);
        if (colorizeTabButton) colorizeTabButton.onClick.AddListener(ShowColorizeTab);

        // Default to Spawn tab
        ShowSpawnTab();
    }

    public void ShowSpawnTab()
    {
        SetTabActive(spawnTabButton, spawnPropertiesPanel, true);
        SetTabActive(colorizeTabButton, colorizePropertiesPanel, false);
    }

    public void ShowColorizeTab()
    {
        SetTabActive(spawnTabButton, spawnPropertiesPanel, false);
        SetTabActive(colorizeTabButton, colorizePropertiesPanel, true);
    }

    void SetTabActive(Button btn, GameObject panel, bool isActive)
    {
        if (panel) panel.SetActive(isActive);
        
        if (btn)
        {
            Image img = btn.GetComponent<Image>();
            if (img)
            {
                img.color = isActive ? selectedColor : unselectedColor;
            }
        }
    }
}
}
