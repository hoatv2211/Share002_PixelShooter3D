using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Added for cleaning JSON comments

namespace PixelShooter3D
{
// --- Data Structures ---

[System.Serializable]
public class PigInfo
{
    public int c; // Color Code: 1=Pink, 2=Blue, etc.
    public int a; // Ammo Count
    public PigInfo(int color, int ammo) { c = color; a = ammo; }
}

[System.Serializable]
public class DeckColumn
{
    public List<PigInfo> pigs = new List<PigInfo>();
}

// Wrapper class because Unity Inspector cannot serialize 2D arrays (int[,]) directly.
[System.Serializable]
public class LevelRow
{
    public List<int> cells = new List<int>();
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "PigGame/LevelData")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName = "New Level";

    [Header("Palette")]
    public List<Color> palette = new List<Color>();

    [Header("Grid Layout")]
    [Tooltip("Define the rows of the grid. 0 = Empty, 1 = Index 0 in palette, 2 = Index 1, etc.")]
    public List<LevelRow> layoutRows = new List<LevelRow>();

    [Header("Pig Deck")]
    [Tooltip("Columns of pigs from Left to Right.")]
    public List<DeckColumn> deck = new List<DeckColumn>();

    [Header("Game Settings")]
    public int maxTraces = 5;
    public float gridSize = 0.6f;
    public float blockScale = 1.0f;
    public float slotSpacing = 1.4f;
    public float beltCornerRadius = 1.5f;
    public float trayStackSpacing = 0.15f;
    public float deckColSpacing = 1.4f;
    public float deckRowSpacing = 1.1f;
    public int holdingCount = 5;
    public float holdingSpacing = 1.4f;

    [Header("Colorizer Settings")]
    public bool canColorize = false;
    public int maxColors = 5;
    public Vector2 overlayOffset = Vector2.zero;
    public Vector2 overlayScale = Vector2.one;

    [Header("JSON Import Tool")]
    [TextArea(15, 30)]
    [Tooltip("Paste the level JSON here and right-click component header -> 'Import Level From JSON'")]
    public string jsonInput;

    // --- Helpers for GameManager ---
    public int Rows => layoutRows.Count;
    public int Cols => layoutRows.Count > 0 ? layoutRows[0].cells.Count : 0;

    public int GetCell(int r, int c)
    {
        if (r < 0 || r >= layoutRows.Count) return 0;
        if (c < 0 || c >= layoutRows[r].cells.Count) return 0;
        return layoutRows[r].cells[c];
    }

    // --- JSON PARSING LOGIC ---

    [ContextMenu("Import Level From JSON")]
    public static LevelData CreateFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            string cleanJson = Regex.Replace(json, @"/\*[\s\S]*?\*/", "");
            cleanJson = Regex.Replace(cleanJson, @"//.*", "");

            LevelJsonWrapper wrapper = JsonUtility.FromJson<LevelJsonWrapper>(cleanJson);
            if (wrapper == null || wrapper.level == null) return null;

            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.ImportData(wrapper.level);
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}");
            return null;
        }
    }

    public void ImportFromJson()
    {
        if (string.IsNullOrEmpty(jsonInput))
        {
            Debug.LogError("JSON Input is empty!");
            return;
        }

        try
        {
            // 1. Pre-process to remove comments (JsonUtility doesn't support them)
            // Remove block comments /* ... */
            string cleanJson = Regex.Replace(jsonInput, @"/\*[\s\S]*?\*/", "");
            // Remove line comments // ...
            cleanJson = Regex.Replace(cleanJson, @"//.*", "");

            // Parse JSON using wrapper
            LevelJsonWrapper wrapper = JsonUtility.FromJson<LevelJsonWrapper>(cleanJson);
            if (wrapper == null || wrapper.level == null)
            {
                Debug.LogError("Failed to parse JSON. Ensure it starts with { \"level\": ... }");
                return;
            }

            ImportData(wrapper.level);
            Debug.Log($"Level Imported Successfully! Size: {wrapper.level.gridWidth}x{wrapper.level.gridHeight}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}");
        }
    }

    private void ImportData(LevelJsonLvl data)
    {
        levelName = data.levelName;
        maxTraces = data.maxTraces;
        gridSize = data.gridSize;
        blockScale = data.blockScale > 0 ? data.blockScale : 1.0f;
        slotSpacing = data.slotSpacing;
        beltCornerRadius = data.beltCornerRadius;
        trayStackSpacing = data.trayStackSpacing;
        deckColSpacing = data.deckColSpacing;
        deckRowSpacing = data.deckRowSpacing;
        holdingCount = data.holdingCount > 0 ? data.holdingCount : 5;
        holdingSpacing = data.holdingSpacing;

        canColorize = data.canColorize;
        maxColors = data.maxColors;
        overlayOffset = data.overlayOffset;
        overlayScale = data.overlayScale;

        // Palette
        palette.Clear();
        if (data.palette != null)
        {
            foreach (var hex in data.palette)
            {
                if (ColorUtility.TryParseHtmlString(hex, out Color c))
                {
                    palette.Add(c);
                }
            }
        }

        // 1. Setup Grid
        layoutRows.Clear();
        for (int r = 0; r < data.gridHeight; r++)
        {
            LevelRow newRow = new LevelRow();
            for (int c = 0; c < data.gridWidth; c++)
            {
                newRow.cells.Add(0); // Initialize with 0 (Empty)
            }
            layoutRows.Add(newRow);
        }

        // 2. Fill Blocks
        if (data.blocks != null)
        {
            foreach (var b in data.blocks)
            {
                // Ensure coordinates are within bounds
                if (b.y < layoutRows.Count && b.x < layoutRows[b.y].cells.Count)
                {
                    if (int.TryParse(b.color, out int code))
                    {
                        layoutRows[b.y].cells[b.x] = code;
                    }
                    else
                    {
                        layoutRows[b.y].cells[b.x] = GetColorCode(b.color);
                    }
                }
            }
        }

        // 3. Setup Deck
        deck.Clear();
        // Find how many columns we need (max slot index)
        int maxSlot = -1;
        if (data.pigs != null)
        {
            foreach (var p in data.pigs)
                if (p.slot > maxSlot) maxSlot = p.slot;
        }

        // Create empty columns
        for (int i = 0; i <= maxSlot; i++)
        {
            deck.Add(new DeckColumn());
        }

        // Fill Deck
        if (data.pigs != null)
        {
            foreach (var p in data.pigs)
            {
                if (p.slot < deck.Count)
                {
                    int code = 0;
                    if (int.TryParse(p.color, out int parsedCode))
                    {
                        code = parsedCode;
                    }
                    else
                    {
                        code = GetColorCode(p.color);
                    }
                    deck[p.slot].pigs.Add(new PigInfo(code, p.ammo));
                }
            }
        }
    }

    public string ExportToJson()
    {
        LevelJsonLvl data = new LevelJsonLvl();
        data.levelName = levelName;
        data.gridWidth = Cols;
        data.gridHeight = Rows;
        data.maxTraces = maxTraces;
        data.gridSize = gridSize;
        data.blockScale = blockScale;
        data.slotSpacing = slotSpacing;
        data.beltCornerRadius = beltCornerRadius;
        data.trayStackSpacing = trayStackSpacing;
        data.deckColSpacing = deckColSpacing;
        data.deckRowSpacing = deckRowSpacing;
        data.holdingCount = holdingCount;
        data.holdingSpacing = holdingSpacing;

        data.canColorize = canColorize;
        data.maxColors = maxColors;
        data.overlayOffset = overlayOffset;
        data.overlayScale = overlayScale;

        data.palette = new List<string>();
        foreach (var c in palette)
        {
            data.palette.Add("#" + ColorUtility.ToHtmlStringRGBA(c));
        }

        data.blocks = new List<BlockJson>();
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                int code = GetCell(r, c);
                if (code != 0)
                {
                    data.blocks.Add(new BlockJson { x = c, y = r, color = code.ToString() });
                }
            }
        }

        data.pigs = new List<PigJson>();
        for (int i = 0; i < deck.Count; i++)
        {
            foreach (var p in deck[i].pigs)
            {
                data.pigs.Add(new PigJson { slot = i, color = p.c.ToString(), ammo = p.a });
            }
        }

        LevelJsonWrapper wrapper = new LevelJsonWrapper { level = data };
        return JsonUtility.ToJson(wrapper, true);
    }

    private string GetColorName(int code)
    {
        switch (code)
        {
            case 1: return "pink";
            case 2: return "blue";
            case 3: return "green";
            case 4: return "yellow";
            default: return "empty";
        }
    }

    private int GetColorCode(string colorName)
    {
        switch (colorName.ToLower())
        {
            case "pink": return 1;
            case "blue": return 2;
            case "green": return 3;
            case "yellow": return 4;
            default: return 0;
        }
    }

    // --- JSON Helper Classes ---
    [System.Serializable]
    public class LevelJsonWrapper
    {
        public LevelJsonLvl level;
    }

    [System.Serializable]
    public class LevelJsonLvl
    {
        public string levelName;
        public int gridWidth;
        public int gridHeight;
        public List<string> palette;
        public List<BlockJson> blocks;
        public List<PigJson> pigs;

        // Settings
        public int maxTraces;
        public float gridSize;
        public float blockScale;
        public float slotSpacing;
        public float beltCornerRadius;
        public float trayStackSpacing;
        public float deckColSpacing;
        public float deckRowSpacing;
        public int holdingCount;
        public float holdingSpacing;

        // Colorizer
        public bool canColorize;
        public int maxColors;
        public Vector2 overlayOffset;
        public Vector2 overlayScale;
    }

    [System.Serializable]
    public class BlockJson
    {
        public int x;
        public int y;
        public string color;
    }

    [System.Serializable]
    public class PigJson
    {
        public int slot;
        public string color;
        public int ammo;
    }
}
}