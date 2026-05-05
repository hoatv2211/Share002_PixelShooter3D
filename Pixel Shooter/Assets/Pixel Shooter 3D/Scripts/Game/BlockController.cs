using UnityEngine;
using System.Collections;

namespace PixelShooter3D
{
public class BlockController : MonoBehaviour
{
    public int colorCode;
    public bool isReserved = false;
    public bool isDying = false;

    private Color[] blockColors = new Color[] {
        Color.white,
        new Color(1f, 0.4f, 0.7f), // 1: Pink
        new Color(0.2f, 0.6f, 1f), // 2: Blue
        Color.green,               // 3: Green
        Color.yellow               // 4: Yellow
    };

    public void Init(Color? forcedColor = null)
    {
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null)
        {
            if (forcedColor.HasValue)
            {
                r.material.color = forcedColor.Value;
            }
            else if (colorCode > 0 && colorCode < blockColors.Length)
            {
                r.material.color = blockColors[colorCode];
            }
        }
    }

    // Call this method from your BulletController when it hits the block
    public void DestroyBlock()
    {
        if (isDying) return;
        isDying = true;

        // Remove from manager list immediately so pigs don't target it
        if (GameManager.Instance != null)
        {
            GameManager.Instance.activeBlocks.Remove(this);
            GameManager.Instance.CheckWinCondition();
        }

        StartCoroutine(PopAndDestroy());
    }

    IEnumerator PopAndDestroy()
    {
        float elapsed = 0f;
        float duration = 0.1f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 1.2f; // Slight pop up

        // 1. Pop Up
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }
        transform.localScale = targetScale;

        // 2. Shrink
        elapsed = 0f;
        duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, elapsed / duration);
            yield return null;
        }
        transform.localScale = Vector3.zero;

        Destroy(gameObject);
    }
}
}