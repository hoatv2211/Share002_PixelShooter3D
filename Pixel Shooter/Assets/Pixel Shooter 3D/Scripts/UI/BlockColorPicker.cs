using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelShooter3D
{
public class BlockColorPicker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider hueSlider;        // 0..1
    [SerializeField] private Slider brightnessSlider; // 0..1
    [SerializeField] private Image previewImage;      // UI preview

    public event Action<Color> OnColorChanged;

    private void Start()
    {
        if (hueSlider != null)
            hueSlider.onValueChanged.AddListener(delegate { UpdateColor(); });
        
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(delegate { UpdateColor(); });
    }

    public void SetColor(Color color, Image targetImage = null)
    {
        if (targetImage != null)
        {
            previewImage = targetImage;
        }

        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        
        if (hueSlider) hueSlider.value = h;
        if (brightnessSlider) brightnessSlider.value = 0.5f; // Default to pure color
        
        UpdateColor();
    }

    public void UpdateColor()
    {
        if (hueSlider == null || brightnessSlider == null) return;

        // 1) Get the slider values
        float hue = hueSlider.value;          // 0..1 => Hue in HSV
        float brightness = brightnessSlider.value; // 0..1 => 0=black, 0.5=pure color, 1=white

        // 2) Convert Hue to a pure color (full saturation, full value)
        Color pureHue = Color.HSVToRGB(hue, 1f, 1f); // Full saturation & value

        // 3) Blend based on brightness
        Color finalColor;
        if (brightness < 0.5f)
        {
            // Brightness from 0..0.5 => black to pure color
            float t = brightness * 2f;
            finalColor = Color.Lerp(Color.black, pureHue, t);
        }
        else
        {
            // Brightness from 0.5..1 => pure color to white
            float t = (brightness - 0.5f) * 2f;
            finalColor = Color.Lerp(pureHue, Color.white, t);
        }

        // 4) Apply the resulting color
        if (previewImage != null)
        {
            previewImage.color = finalColor;
        }

        OnColorChanged?.Invoke(finalColor);
    }

    public void RandomizeColors()
    {
        if (hueSlider) hueSlider.value = UnityEngine.Random.Range(0f, 1f);
        if (brightnessSlider) brightnessSlider.value = UnityEngine.Random.Range(0f, 1f);
    }
}
}
