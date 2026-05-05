using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PixelShooter3D
{
public class BlockColorizerUI : MonoBehaviour
{
    [Header("References")]
    public BlockColorizer blockColorizer;

    [Header("UI Controls")]
    public Slider maxColorsSlider;
    public TextMeshProUGUI maxColorsText;
    
    public Slider offsetXSlider;
    public Slider offsetYSlider;
    
    public Slider scaleXSlider;
    public Slider scaleYSlider;

    public TextMeshProUGUI offsetXText;
    public TextMeshProUGUI offsetYText;
    public TextMeshProUGUI scaleXText;
    public TextMeshProUGUI scaleYText;

    public Button pickNewColorsButton;
    public Toggle canColorizeToggle;

    private bool isUpdatingUI = false;

    void Start()
    {
        if (blockColorizer == null)
        {
            Debug.LogError("BlockColorizer reference missing!");
            return;
        }

        // Initialize UI values
        UpdateUIFromColorizer();

        // Add Listeners
        if (maxColorsSlider) maxColorsSlider.onValueChanged.AddListener(OnMaxColorsChanged);
        if (offsetXSlider) offsetXSlider.onValueChanged.AddListener(OnOffsetChanged);
        if (offsetYSlider) offsetYSlider.onValueChanged.AddListener(OnOffsetChanged);
        if (scaleXSlider) scaleXSlider.onValueChanged.AddListener(OnScaleChanged);
        if (scaleYSlider) scaleYSlider.onValueChanged.AddListener(OnScaleChanged);
        if (pickNewColorsButton) pickNewColorsButton.onClick.AddListener(OnPickNewColorsClicked);
        if (canColorizeToggle) canColorizeToggle.onValueChanged.AddListener(OnCanColorizeChanged);
    }

    void Update()
    {
        // Optional: Poll for changes if BlockColorizer is modified externally (e.g. Inspector)
        if (!isUpdatingUI)
        {
            if (maxColorsSlider && Mathf.RoundToInt(maxColorsSlider.value) != blockColorizer.maxColors) UpdateUIFromColorizer();
        }
    }

    void UpdateUIFromColorizer()
    {
        isUpdatingUI = true;

        if (maxColorsSlider) maxColorsSlider.value = blockColorizer.maxColors;
        if (maxColorsText) maxColorsText.text = blockColorizer.maxColors.ToString();

        // Reverse logic: Slider shows -Value
        if (offsetXSlider) offsetXSlider.value = -blockColorizer.overlayOffset.x;
        if (offsetYSlider) offsetYSlider.value = -blockColorizer.overlayOffset.y;
        
        if (offsetXText) offsetXText.text = (-blockColorizer.overlayOffset.x).ToString("F2");
        if (offsetYText) offsetYText.text = (-blockColorizer.overlayOffset.y).ToString("F2");

        // Inverse logic: Slider Low (0.1) -> Scale High (5.0) -> Small Image
        // Slider High (5.0) -> Scale Low (0.1) -> Big Image
        // Formula: Scale = 5.1 - Slider
        // Slider = 5.1 - Scale
        if (scaleXSlider) scaleXSlider.value = 5.1f - Mathf.Abs(blockColorizer.overlayScale.x);
        if (scaleYSlider) scaleYSlider.value = 5.1f - Mathf.Abs(blockColorizer.overlayScale.y);
        
        if (scaleXText) scaleXText.text = (scaleXSlider ? scaleXSlider.value : 1).ToString("F2");
        if (scaleYText) scaleYText.text = (scaleYSlider ? scaleYSlider.value : 1).ToString("F2");

        if (canColorizeToggle) canColorizeToggle.isOn = blockColorizer.canColorize;

        isUpdatingUI = false;
    }

    void OnMaxColorsChanged(float value)
    {
        if (isUpdatingUI) return;
        blockColorizer.maxColors = Mathf.RoundToInt(value);
        if (maxColorsText) maxColorsText.text = blockColorizer.maxColors.ToString();
    }

    void OnOffsetChanged(float value)
    {
        if (isUpdatingUI) return;
        // Reverse logic: Logic gets -SliderValue
        float x = offsetXSlider ? -offsetXSlider.value : blockColorizer.overlayOffset.x;
        float y = offsetYSlider ? -offsetYSlider.value : blockColorizer.overlayOffset.y;
        blockColorizer.overlayOffset = new Vector2(x, y);
        
        // Text shows SliderValue
        if (offsetXText) offsetXText.text = (offsetXSlider ? offsetXSlider.value : 0).ToString("F2");
        if (offsetYText) offsetYText.text = (offsetYSlider ? offsetYSlider.value : 0).ToString("F2");
    }

    void OnScaleChanged(float value)
    {
        if (isUpdatingUI) return;
        // Inverse logic: Scale = 5.1 - Slider
        float x = scaleXSlider ? (5.1f - scaleXSlider.value) : blockColorizer.overlayScale.x;
        float y = scaleYSlider ? (5.1f - scaleYSlider.value) : blockColorizer.overlayScale.y;
        blockColorizer.overlayScale = new Vector2(x, y);
        
        // Text shows SliderValue
        if (scaleXText) scaleXText.text = (scaleXSlider ? scaleXSlider.value : 1).ToString("F2");
        if (scaleYText) scaleYText.text = (scaleYSlider ? scaleYSlider.value : 1).ToString("F2");
    }

    void OnPickNewColorsClicked()
    {
        blockColorizer.pickNewColors = true;
    }

    void OnCanColorizeChanged(bool value)
    {
        if (isUpdatingUI) return;
        blockColorizer.canColorize = value;
        if (value) blockColorizer.ApplyColors();
    }
}
}
