using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;

namespace PixelShooter3D
{
public class PigEditorPopup : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    [Header("UI References")]
    public Transform colorsContainer;
    public TMP_InputField ammoInput;
    public GameObject colorButtonPrefab;

    private Action<Color> onColorSelected;
    private Action<int> onAmmoChanged;
    private bool isHovering = false;

    public void Setup(List<Color> availableColors, Color currentColor, int currentAmmo, Action<Color> colorCallback, Action<int> ammoCallback)
    {
        onColorSelected = colorCallback;
        onAmmoChanged = ammoCallback;

        // Setup Ammo
        if (ammoInput)
        {
            ammoInput.text = currentAmmo.ToString();
            ammoInput.onValueChanged.RemoveAllListeners();
            ammoInput.onValueChanged.AddListener(OnAmmoInputChanged);
        }

        // Setup Colors
        foreach (Transform child in colorsContainer)
        {
            Destroy(child.gameObject);
        }

        if (colorButtonPrefab)
        {
            foreach (Color c in availableColors)
            {
                GameObject btnObj = Instantiate(colorButtonPrefab, colorsContainer);
                
                // Scale up 2x
                btnObj.transform.localScale = new Vector3(2f, 2f, 2f);

                Image btnImg = btnObj.GetComponent<Image>();
                if (btnImg) btnImg.color = c;

                Button btn = btnObj.GetComponent<Button>();
                if (btn)
                {
                    Color capturedColor = c;
                    btn.onClick.AddListener(() => OnColorClicked(capturedColor));
                }
            }
        }
        
        gameObject.SetActive(true);
    }

    void OnAmmoInputChanged(string value)
    {
        if (int.TryParse(value, out int result))
        {
            onAmmoChanged?.Invoke(result);
        }
    }

    void OnColorClicked(Color c)
    {
        onColorSelected?.Invoke(c);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        // Close when hovering away
        gameObject.SetActive(false);
    }
}
}
