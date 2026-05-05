using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace PixelShooter3D
{
public class PlayerSetupWarningPopup : MonoBehaviour
{
    [Header("UI References")]
    public Transform itemsContainer;
    public GameObject itemPrefab;
    public Button fixButton;
    public Button ignoreButton;

    public void Setup(List<ColorMismatch> mismatches, Action onFix, Action onIgnore)
    {
        // Clear old items
        foreach (Transform child in itemsContainer) Destroy(child.gameObject);

        // Spawn new items
        foreach (var m in mismatches)
        {
            GameObject item = Instantiate(itemPrefab, itemsContainer);
            
            // Find Image for color
            Transform circleTr = item.transform.Find("ColorCircle");
            if (circleTr)
            {
                Image img = circleTr.GetComponent<Image>();
                if (img) img.color = m.color;
            }

            // Find Text for info
            Transform textTr = item.transform.Find("InfoText");
            if (textTr)
            {
                TextMeshProUGUI txt = textTr.GetComponent<TextMeshProUGUI>();
                if (txt)
                {
                    int diff = m.bulletCount - m.blockCount;
                    string diffStr = diff > 0 ? $"+{diff}" : diff.ToString();
                    txt.text = $"Blocks: {m.blockCount}\nBullets: {m.bulletCount}\nDiff: {diffStr}";
                }
            }
            
            item.SetActive(true);
        }

        // Setup Buttons
        if (fixButton)
        {
            fixButton.onClick.RemoveAllListeners();
            fixButton.onClick.AddListener(() => {
                onFix?.Invoke();
                Destroy(gameObject);
            });
        }

        if (ignoreButton)
        {
            ignoreButton.onClick.RemoveAllListeners();
            ignoreButton.onClick.AddListener(() => {
                onIgnore?.Invoke();
                Destroy(gameObject);
            });
        }
    }
}

public struct ColorMismatch
{
    public Color color;
    public int blockCount;
    public int bulletCount;
}
}
