using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PixelShooter3D
{
public class BlockColorizer : MonoBehaviour
{
    [Header("Settings")]
    public Texture2D sourceImage;
    [Range(1, 32)]
    public int maxColors = 5;
    
    public bool canColorize = false;

    [Tooltip("Click to re-randomize color selection")]
    public bool pickNewColors = false;

    [Header("Overlay Settings")]
    public Vector2 overlayOffset = Vector2.zero;
    public Vector2 overlayScale = Vector2.one;

    private BlockContainerEdit containerEdit;
    
    // State tracking for updates
    private Vector2 lastOffset;
    private Vector2 lastScale;
    private int lastMaxColors;
    private Texture2D lastTexture;
    private int colorSeed = 0;

    void Start()
    {
        containerEdit = GetComponent<BlockContainerEdit>();
        // Initialize last state
        lastOffset = overlayOffset;
        lastScale = overlayScale;
        lastMaxColors = maxColors;
        lastTexture = sourceImage;
        colorSeed = Random.Range(0, 10000);
    }

    void Update()
    {
        if (pickNewColors)
        {
            pickNewColors = false;
            colorSeed = Random.Range(0, 10000);
            ApplyColors();
        }

        // Check for changes in Inspector
        if (overlayOffset != lastOffset || 
            overlayScale != lastScale || 
            maxColors != lastMaxColors || 
            sourceImage != lastTexture)
        {
            ApplyColors();
            
            lastOffset = overlayOffset;
            lastScale = overlayScale;
            lastMaxColors = maxColors;
            lastTexture = sourceImage;
        }
    }

    public void ApplyColors()
    {
        if (containerEdit == null || !canColorize) return;

        // Ensure we have the latest spawned blocks
        if (containerEdit.spawnedBlocks == null || containerEdit.spawnedBlocks.Count == 0) return;
        
        if (sourceImage == null)
        {
            Debug.LogWarning("BlockColorizer: No Source Image assigned!");
            return;
        }

        if (!sourceImage.isReadable)
        {
            Debug.LogError($"BlockColorizer: Texture '{sourceImage.name}' is not readable. Please enable 'Read/Write' in its Import Settings.");
            return;
        }

        int rows = Mathf.RoundToInt(containerEdit.rowsSlider.value);
        int cols = Mathf.RoundToInt(containerEdit.colsSlider.value);
        
        List<Color> sampledColors = new List<Color>();
        
        // Use the list from containerEdit to ensure correct order and only manage our blocks
        List<Renderer> blockRenderers = new List<Renderer>();
        foreach (GameObject block in containerEdit.spawnedBlocks)
        {
            if (block == null || !block.activeSelf) continue;
            Renderer r = block.GetComponentInChildren<Renderer>();
            if (r != null) blockRenderers.Add(r);
        }

        if (blockRenderers.Count == 0) return;

        // Calculate aspect ratios for Overlay mode
        float imageAspect = (float)sourceImage.width / sourceImage.height;
        float gridAspect = (float)cols / rows; // Assuming blocks are square and spacing is uniform

        for (int i = 0; i < blockRenderers.Count; i++)
        {
            // Calculate Grid Coordinates
            int currentRow = i / cols;
            int currentCol = i % cols;

            // Base UV (0 to 1)
            // Use center of block for sampling
            float u = (currentCol + 0.5f) / cols;
            float v = (currentRow + 0.5f) / rows;

            // Apply Overlay Transformation (Always active now)
            
            // Correct for aspect ratio to prevent stretching
            // We want the image to retain its aspect ratio within the grid
            if (gridAspect > imageAspect)
            {
                // Grid is wider than image
                float scaleFactor = imageAspect / gridAspect;
                v = (v - 0.5f) / scaleFactor + 0.5f;
            }
            else
            {
                // Grid is taller than image
                float scaleFactor = gridAspect / imageAspect;
                u = (u - 0.5f) / scaleFactor + 0.5f;
            }

            // Apply user transform
            u = (u - 0.5f) * overlayScale.x + 0.5f + overlayOffset.x;
            v = (v - 0.5f) * overlayScale.y + 0.5f + overlayOffset.y;

            // Check bounds to avoid tiling/smearing
            float eps = 0.001f;
            if (u < -eps || u > 1f + eps || v < -eps || v > 1f + eps)
            {
                sampledColors.Add(Color.clear);
            }
            else
            {
                float cu = Mathf.Clamp01(u);
                float cv = Mathf.Clamp01(v);
                Color c = sourceImage.GetPixelBilinear(cu, cv);
                if (c.a < 0.1f) sampledColors.Add(Color.clear);
                else sampledColors.Add(c);
            }
        }

        // Quantize
        // Filter out clear/empty pixels for palette generation
        List<Color> validPixels = sampledColors.Where(c => c.a > 0.1f).ToList();
        List<Color> palette = QuantizeColors(validPixels, maxColors);

        // Apply
        for (int i = 0; i < blockRenderers.Count; i++)
        {
            Color original = sampledColors[i];
            
            if (original.a <= 0.1f)
            {
                // Make "empty" blocks dark and semi-transparent
                blockRenderers[i].material.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);
            }
            else
            {
                Color nearest = FindNearestColor(original, palette);
                blockRenderers[i].material.color = nearest;
            }
        }
    }

    // Simple K-Means Clustering for Color Quantization
    List<Color> QuantizeColors(List<Color> pixels, int k)
    {
        if (pixels.Count == 0) return new List<Color>();
        if (pixels.Count <= k) return pixels.Distinct().ToList();

        // Use seed for deterministic randomness
        Random.InitState(colorSeed);

        // Initialize centroids randomly
        List<Color> centroids = new List<Color>();
        for (int i = 0; i < k; i++)
        {
            centroids.Add(pixels[Random.Range(0, pixels.Count)]);
        }

        bool changed = true;
        int maxIterations = 20;
        int iter = 0;

        while (changed && iter < maxIterations)
        {
            changed = false;
            iter++;

            // Assign pixels to nearest centroid
            List<List<Color>> clusters = new List<List<Color>>();
            for (int i = 0; i < k; i++) clusters.Add(new List<Color>());

            foreach (Color p in pixels)
            {
                int nearestIndex = 0;
                float minDist = float.MaxValue;

                for (int i = 0; i < k; i++)
                {
                    float dist = ColorDistance(p, centroids[i]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestIndex = i;
                    }
                }
                clusters[nearestIndex].Add(p);
            }

            // Recalculate centroids
            for (int i = 0; i < k; i++)
            {
                if (clusters[i].Count == 0) continue;

                Color newCentroid = new Color(0, 0, 0, 0);
                foreach (Color c in clusters[i]) newCentroid += c;
                newCentroid /= clusters[i].Count;

                if (ColorDistance(newCentroid, centroids[i]) > 0.001f)
                {
                    centroids[i] = newCentroid;
                    changed = true;
                }
            }
        }

        return centroids;
    }

    float ColorDistance(Color a, Color b)
    {
        // Euclidean distance in RGB space
        return Mathf.Pow(a.r - b.r, 2) + Mathf.Pow(a.g - b.g, 2) + Mathf.Pow(a.b - b.b, 2);
    }

    Color FindNearestColor(Color target, List<Color> palette)
    {
        if (palette.Count == 0) return target;

        Color nearest = Color.black;
        float minDist = float.MaxValue;

        foreach (Color c in palette)
        {
            float dist = ColorDistance(target, c);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = c;
            }
        }
        return nearest;
    }
}
}
