using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelShooter3D
{
public class CreateColor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider hueSlider;        // 0..1
    [SerializeField] private Slider brightnessSlider; // 0..1
    [SerializeField] private Image previewImage;      // Optional UI preview (e.g. a panel)

    [Space]
    public Color finalColor;

    public event Action<Color> OnColorChanged;

    public void SetColor(Color color, Image targetImage = null)
    {
        if (targetImage != null)
        {
            previewImage = targetImage;
        }

        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);

        if (hueSlider) hueSlider.value = h;
        
        // Calculate brightness from V and S
        // Logic: 
        // if brightness < 0.5: Lerp(Black, Pure, t) -> V varies, S=1. t = brightness * 2. V = t.
        // if brightness > 0.5: Lerp(Pure, White, t) -> V=1, S varies. t = (brightness - 0.5) * 2. S = 1 - t.
        
        float brightness = 0.5f;
        
        // Check if color is closer to black (darker side) or white (lighter side)
        // Darker side: V varies, S is typically high (or 1)
        // Lighter side: V is high (or 1), S varies
        
        // If Value is significantly less than 1, we are likely on the darker side (0 to 0.5 brightness)
        if (v < 0.99f) 
        {
            // t = brightness * 2 = V
            // brightness = V / 2
            brightness = v * 0.5f;
        }
        else 
        {
            // We are on the lighter side (0.5 to 1 brightness) or pure color
            // t = (brightness - 0.5) * 2
            // S = 1 - t
            // t = 1 - S
            // (brightness - 0.5) * 2 = 1 - S
            // brightness - 0.5 = (1 - S) / 2
            // brightness = 0.5 + (1 - S) / 2
            
            float t = 1f - s;
            brightness = 0.5f + (t * 0.5f);
        }

        if (brightnessSlider) brightnessSlider.value = brightness;

        UpdateColor();
    }

    //[System.Obsolete]
    public void UpdateColor()
    {
        // 1) Get the slider values
        float hue = hueSlider.value;          // 0..1 => Hue in HSV
        float brightness = brightnessSlider.value; // 0..1 => 0=black, 0.5=pure color, 1=white

        // 2) Convert Hue to a pure color (full saturation, full value)
        Color pureHue = Color.HSVToRGB(hue, 1f, 1f); // Full saturation & value

        // 3) Blend based on brightness
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

        // 4) Apply the resulting color somewhere. For example, a UI element:
        if (previewImage != null)
        {
            previewImage.color = finalColor;
        }

        OnColorChanged?.Invoke(finalColor);
    }

    public void SetPreviewImage(Image prvIm)
    {
        previewImage = prvIm;
    }

    public void RandomizeColors()
    {
        hueSlider.value = UnityEngine.Random.Range(0f, 1f);
        brightnessSlider.value = UnityEngine.Random.Range(0f, 1f);
    }
}
}
