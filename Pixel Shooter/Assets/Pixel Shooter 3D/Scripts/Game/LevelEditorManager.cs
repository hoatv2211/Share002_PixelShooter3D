using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

namespace PixelShooter3D
{
    public class LevelEditorManager : MonoBehaviour
    {
        public static LevelEditorManager Instance;

        [Header("Configuration")]
        public int gridWidth = 12;
        public int gridHeight = 12;

        [Header("UI References")]
        public Transform gridContainer;
        public Transform deckContainer;
        public TMP_InputField ammoInput;
        public TMP_InputField jsonInputField; // Optional: To paste JSON manually
        public Button saveLevelButton;
        public GameObject firstSetUpPlayersPopup;
        public TMP_InputField levelNameInput;

        [Header("Managers")]
        public PlayerSetupManager playerSetupManager;
        public BlockColorizer blockColorizer;
        public BlockContainerEdit blockContainerEdit;
        public HoldingContainerEdit holdingContainerEdit;

        [Header("Prefabs")]
        public GameObject cellPrefab;
        public GameObject columnPrefab;
        public GameObject pigSlotPrefab;

        [Header("Colors (Must match BlockController logic)")]
        public Color pinkColor = new Color(1f, 0.4f, 0.7f);
        public Color blueColor = new Color(0.2f, 0.6f, 1f);
        public Color greenColor = Color.green;
        public Color yellowColor = Color.yellow;
        public Color emptyColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

        // 0=Empty, 1=Pink, 2=Blue, 3=Green, 4=Yellow
        private int selectedToolColor = 1;
        private bool isEditingDeck = false; // Toggle between painting blocks vs placing pigs

        // Data Store
        private int[,] gridData;
        private List<List<EditorPigData>> deckData = new List<List<EditorPigData>>();

        [System.Serializable]
        public class EditorPigData
        {
            public int colorCode;
            public int ammo;
        }

        private List<LevelEditorCell> gridCells = new List<LevelEditorCell>();

        void Awake()
        {
            Instance = this;
            gridData = new int[gridWidth, gridHeight];
            InitializeGrid();

            if (saveLevelButton)
            {
                saveLevelButton.onClick.RemoveAllListeners();
                saveLevelButton.onClick.AddListener(OnSaveLevelButtonClicked);
            }

            if (firstSetUpPlayersPopup) firstSetUpPlayersPopup.SetActive(false);
        }

        public void OnSaveLevelButtonClicked()
        {
            if (playerSetupManager != null && !playerSetupManager.playersAreSetUp)
            {
                if (firstSetUpPlayersPopup)
                {
                    StartCoroutine(ShowPopupRoutine(firstSetUpPlayersPopup));
                }
                return;
            }

            // Check if file already exists
            string levelName = levelNameInput != null ? levelNameInput.text : "NewLevel";
            if (string.IsNullOrEmpty(levelName)) levelName = "NewLevel";

            string path = GetLevelsFolderPath();
            string fileName = levelName + ".json";
            string fullPath = System.IO.Path.Combine(path, fileName);

            if (System.IO.File.Exists(fullPath))
            {
                // File exists - show Editor confirmation dialog
#if UNITY_EDITOR
                int choice = UnityEditor.EditorUtility.DisplayDialogComplex(
                    "Level Already Exists",
                    $"A level named \"{levelName}\" already exists!\n\nDo you want to overwrite it?",
                    "Overwrite",      // Option 0
                    "Cancel",         // Option 1
                    "Change Name"     // Option 2 (Alt)
                );

                switch (choice)
                {
                    case 0: // Overwrite
                        SaveLevelToFile();
                        Debug.Log($"Level '{levelName}' overwritten by user confirmation.");
                        break;
                    case 1: // Cancel
                        Debug.Log("Level save cancelled by user.");
                        break;
                    case 2: // Change Name
                        string suggestedName = GetUniqueFileName(levelName);
                        if (levelNameInput != null)
                        {
                            levelNameInput.text = suggestedName;
                            levelNameInput.Select();
                            levelNameInput.ActivateInputField();
                        }
                        Debug.Log($"Level name changed to: {suggestedName}. Click Save again to save with the new name.");
                        break;
                }
#else
                // In builds, just warn and save (no Editor dialogs available)
                Debug.LogWarning($"Level '{levelName}' already exists and will be overwritten!");
                SaveLevelToFile();
#endif
            }
            else
            {
                // File doesn't exist - save directly
                SaveLevelToFile();
            }
        }

        /// <summary>
        /// Returns the path to the Levels folder inside Resources
        /// </summary>
        private string GetLevelsFolderPath()
        {
            return System.IO.Path.Combine(
                Application.dataPath,
                "Pixel Shooter 3D",
                "Resources",
                "Levels"
            );
        }

        /// <summary>
        /// Generates a unique filename by appending a number to the base name.
        /// E.g., "Level1" -> "Level1_2", "Level1_2" -> "Level1_3"
        /// </summary>
        private string GetUniqueFileName(string baseName)
        {
            string path = GetLevelsFolderPath();

            // Check if name already ends with _N pattern
            var match = System.Text.RegularExpressions.Regex.Match(baseName, @"^(.+)_(\d+)$");
            string nameBase;
            int counter;

            if (match.Success)
            {
                nameBase = match.Groups[1].Value;
                counter = int.Parse(match.Groups[2].Value) + 1;
            }
            else
            {
                nameBase = baseName;
                counter = 2;
            }

            // Find next available number
            string newName = $"{nameBase}_{counter}";
            while (System.IO.File.Exists(System.IO.Path.Combine(path, newName + ".json")))
            {
                counter++;
                newName = $"{nameBase}_{counter}";
            }

            return newName;
        }

        private IEnumerator ShowPopupRoutine(GameObject popup)
        {
            popup.SetActive(true);
            yield return new WaitForSeconds(2f);
            popup.SetActive(false);
        }

        public void SaveLevelToFile()
        {
            LevelData tempLevel = ScriptableObject.CreateInstance<LevelData>();

            // Level Name
            tempLevel.levelName = levelNameInput != null ? levelNameInput.text : "NewLevel";
            if (string.IsNullOrEmpty(tempLevel.levelName)) tempLevel.levelName = "NewLevel";

            // Palette Management
            tempLevel.palette.Clear();
            Dictionary<Color, int> colorToPaletteIndex = new Dictionary<Color, int>();

            int GetPaletteIndex(Color c)
            {
                if (c.a < 0.1f) return 0; // Empty

                // Check if color already in palette
                foreach (var kvp in colorToPaletteIndex)
                {
                    if (ColorMatch(kvp.Key, c)) return kvp.Value;
                }

                // Add new color
                int newIndex = tempLevel.palette.Count + 1;
                tempLevel.palette.Add(c);
                colorToPaletteIndex[c] = newIndex;
                return newIndex;
            }

            // Grid Layout
            int rows = blockContainerEdit != null ? Mathf.RoundToInt(blockContainerEdit.rowsSlider.value) : 0;
            int cols = blockContainerEdit != null ? Mathf.RoundToInt(blockContainerEdit.colsSlider.value) : 0;

            tempLevel.layoutRows.Clear();

            // Collect blocks from container
            List<GameObject> blocks = new List<GameObject>();
            if (blockContainerEdit != null)
            {
                // If spawnedBlocks is empty (e.g. in editor before play), try to get from children
                if (blockContainerEdit.spawnedBlocks == null || blockContainerEdit.spawnedBlocks.Count == 0)
                {
                    foreach (Transform child in blockContainerEdit.transform)
                    {
                        // Only include objects that look like blocks (not LevelEditorCells)
                        if (child.name.Contains("Block"))
                        {
                            blocks.Add(child.gameObject);
                        }
                    }
                }
                else
                {
                    blocks = blockContainerEdit.spawnedBlocks;
                }
            }

            for (int r = 0; r < rows; r++)
            {
                LevelRow row = new LevelRow();
                for (int c = 0; c < cols; c++)
                {
                    int index = r * cols + c;
                    if (index < blocks.Count && blocks[index] != null && blocks[index].activeSelf)
                    {
                        Renderer rend = blocks[index].GetComponentInChildren<Renderer>();
                        if (rend != null)
                        {
                            row.cells.Add(GetPaletteIndex(rend.material.color));
                        }
                        else
                        {
                            row.cells.Add(0);
                        }
                    }
                    else
                    {
                        row.cells.Add(0);
                    }
                }
                tempLevel.layoutRows.Add(row);
            }

            // Deck
            tempLevel.deck.Clear();
            DeckContainerEdit deckEdit = FindFirstObjectByType<DeckContainerEdit>();
            if (deckEdit != null)
            {
                int deckCols = Mathf.RoundToInt(deckEdit.columnCountSlider.value);
                for (int i = 0; i < deckCols; i++)
                {
                    tempLevel.deck.Add(new DeckColumn());
                }

                List<PigController> pigs = new List<PigController>();
                foreach (Transform child in deckEdit.transform)
                {
                    if (child.gameObject.activeSelf)
                    {
                        PigController pc = child.GetComponent<PigController>();
                        if (pc != null) pigs.Add(pc);
                    }
                }

                List<Slider> pigSliders = new List<Slider>();
                foreach (Transform child in deckEdit.slidersContainer)
                {
                    Slider s = child.GetComponent<Slider>();
                    if (s != null) pigSliders.Add(s);
                }

                int pigIdx = 0;
                for (int i = 0; i < pigSliders.Count; i++)
                {
                    int count = Mathf.RoundToInt(pigSliders[i].value);
                    for (int j = 0; j < count; j++)
                    {
                        if (pigIdx < pigs.Count)
                        {
                            PigController pc = pigs[pigIdx];
                            Renderer rend = pc.GetComponentInChildren<Renderer>();
                            int colorCode = rend != null ? GetPaletteIndex(rend.material.color) : 1;
                            tempLevel.deck[i].pigs.Add(new PigInfo(colorCode, pc.ammo));
                            pigIdx++;
                        }
                    }
                }

                // Spacings from DeckEdit
                float spacing = deckEdit.holdingContainerEdit != null ? deckEdit.holdingContainerEdit.spacingSlider.value : 2.44f;
                tempLevel.deckColSpacing = deckEdit.pigWidth + spacing;
                tempLevel.deckRowSpacing = deckEdit.pigWidth + spacing;
            }

            // Settings from GameManager (or defaults if not found)
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                tempLevel.maxTraces = gm.maxTraces;
                tempLevel.gridSize = blockContainerEdit != null ? blockContainerEdit.blockSpacing * blockContainerEdit.sizeSlider.value : gm.gridSize;
                tempLevel.blockScale = blockContainerEdit != null ? blockContainerEdit.sizeSlider.value : 1.0f;
                tempLevel.slotSpacing = gm.slotSpacing;
                tempLevel.beltCornerRadius = gm.beltCornerRadius;
                tempLevel.trayStackSpacing = gm.trayStackSpacing;
                // tempLevel.deckColSpacing = gm.deckColSpacing; // Overridden by deckEdit if available
                // tempLevel.deckRowSpacing = gm.deckRowSpacing;
                tempLevel.holdingSpacing = gm.holdingSpacing;
            }

            // Holding Settings from HoldingContainerEdit
            if (holdingContainerEdit != null)
            {
                tempLevel.holdingCount = Mathf.RoundToInt(holdingContainerEdit.countSlider.value);
                // Save the STRIDE (Width + Gap) instead of just the gap
                tempLevel.holdingSpacing = holdingContainerEdit.cubeWidth + holdingContainerEdit.spacingSlider.value;
            }

            // Colorizer Settings
            if (blockColorizer != null)
            {
                tempLevel.canColorize = blockColorizer.canColorize;
                tempLevel.maxColors = blockColorizer.maxColors;
                tempLevel.overlayOffset = blockColorizer.overlayOffset;
                tempLevel.overlayScale = blockColorizer.overlayScale;
            }

            // Save to JSON in Resources/Levels folder
            string json = tempLevel.ExportToJson();
            string path = GetLevelsFolderPath();
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

            string fileName = tempLevel.levelName + ".json";
            string fullPath = System.IO.Path.Combine(path, fileName);

            System.IO.File.WriteAllText(fullPath, json);
            Debug.Log($"Level saved to: {fullPath}");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        private int GetColorCodeFromColor(Color c)
        {
            if (ColorMatch(c, pinkColor)) return 1;
            if (ColorMatch(c, blueColor)) return 2;
            if (ColorMatch(c, greenColor)) return 3;
            if (ColorMatch(c, yellowColor)) return 4;
            return 0;
        }

        private bool ColorMatch(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
        }

        // --- INITIALIZATION ---
        public void InitializeGrid()
        {
            // Clear existing
            foreach (Transform child in gridContainer) Destroy(child.gameObject);
            gridCells.Clear();

            // Spawn 12x12 grid
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    GameObject obj = Instantiate(cellPrefab, gridContainer);
                    LevelEditorCell cell = obj.GetComponent<LevelEditorCell>();
                    cell.x = x;
                    cell.y = y;
                    cell.SetColor(emptyColor);
                    gridCells.Add(cell);
                }
            }
        }

        // --- TOOL SELECTION ---
        public void SelectColorTool(int colorCode)
        {
            selectedToolColor = colorCode;
            Debug.Log($"Selected Color: {colorCode}");
        }

        public void SelectPigTool(int colorCode)
        {
            selectedToolColor = colorCode;
            isEditingDeck = true;
            Debug.Log($"Selected Pig Color: {colorCode}");
        }

        // --- INTERACTION ---
        public void OnCellClicked(LevelEditorCell cell)
        {
            gridData[cell.x, cell.y] = selectedToolColor;
            cell.SetColor(GetColorFromCode(selectedToolColor));
        }

        public void AddDeckColumn()
        {
            deckData.Add(new List<EditorPigData>());
            RefreshDeckUI();
        }

        public void AddPigToColumn(int colIndex)
        {
            int ammo = 30;
            if (ammoInput != null && int.TryParse(ammoInput.text, out int parsed)) ammo = parsed;

            deckData[colIndex].Add(new EditorPigData { colorCode = selectedToolColor, ammo = ammo });
            RefreshDeckUI();
        }

        public void RemoveColumn(int colIndex)
        {
            if (colIndex < deckData.Count)
            {
                deckData.RemoveAt(colIndex);
                RefreshDeckUI();
            }
        }

        void RefreshDeckUI()
        {
            foreach (Transform t in deckContainer) Destroy(t.gameObject);

            for (int i = 0; i < deckData.Count; i++)
            {
                int colIndex = i; // Closure capture
                GameObject colObj = Instantiate(columnPrefab, deckContainer);

                // Add Listener to column background to add pig? 
                // Better: Add a "+" button at the bottom of each column
                Button addBtn = colObj.GetComponentInChildren<Button>();
                if (addBtn == null)
                {
                    // If prefab doesn't have a button, assume whole column is clickable or handle differently
                    // For simplicity, let's assume the columnPrefab has a Button component on itself
                    addBtn = colObj.GetComponent<Button>();
                }
                if (addBtn) addBtn.onClick.AddListener(() => AddPigToColumn(colIndex));

                // Populate Pigs
                foreach (var pig in deckData[i])
                {
                    GameObject pSlot = Instantiate(pigSlotPrefab, colObj.transform);
                    pSlot.GetComponent<Image>().color = GetColorFromCode(pig.colorCode);
                    var text = pSlot.GetComponentInChildren<Text>();
                    if (text) text.text = pig.ammo.ToString();
                }
            }
        }

        // --- JSON EXPORT ---
        public void SaveToClipboard()
        {
            string json = GenerateJSON();
            GUIUtility.systemCopyBuffer = json;
            Debug.Log("JSON copied to clipboard!");

            if (jsonInputField) jsonInputField.text = json;
        }

        public void LoadFromInput()
        {
            if (jsonInputField && !string.IsNullOrEmpty(jsonInputField.text))
            {
                ParseJSON(jsonInputField.text);
            }
        }

        // --- HELPERS ---
        Color GetColorFromCode(int code)
        {
            switch (code)
            {
                case 1: return pinkColor;
                case 2: return blueColor;
                case 3: return greenColor;
                case 4: return yellowColor;
                default: return emptyColor;
            }
        }

        string GetColorName(int code)
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

        int GetCodeFromName(string name)
        {
            switch (name.ToLower())
            {
                case "pink": return 1;
                case "blue": return 2;
                case "green": return 3;
                case "yellow": return 4;
                default: return 0;
            }
        }

        // --- MANUAL JSON GENERATOR (To match your specific format) ---
        string GenerateJSON()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"level\": {");
            sb.AppendLine($"    \"gridWidth\": {gridWidth},");
            sb.AppendLine($"    \"gridHeight\": {gridHeight},");
            sb.AppendLine("    \"blocks\": [");

            List<string> blockEntries = new List<string>();
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int c = gridData[x, y];
                    if (c != 0)
                    {
                        blockEntries.Add($"      {{\"x\":{x},\"y\":{y},\"color\":\"{GetColorName(c)}\"}}");
                    }
                }
            }
            sb.AppendLine(string.Join(",\n", blockEntries));
            sb.AppendLine("\n    ],");

            sb.AppendLine("    \"pigs\": [");
            List<string> pigEntries = new List<string>();
            for (int i = 0; i < deckData.Count; i++)
            {
                for (int j = 0; j < deckData[i].Count; j++)
                {
                    var pig = deckData[i][j];
                    pigEntries.Add($"      {{ \"slot\": {i}, \"color\": \"{GetColorName(pig.colorCode)}\", \"ammo\": {pig.ammo} }}");
                }
            }
            sb.AppendLine(string.Join(",\n", pigEntries));
            sb.AppendLine("    ]");
            sb.AppendLine("  }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        // --- MANUAL JSON PARSER (Basic) ---
        void ParseJSON(string json)
        {
            // Simple Regex parsing to avoid complex object mapping for this utility
            // Clear current
            gridData = new int[gridWidth, gridHeight];
            deckData.Clear();
            InitializeGrid();

            // Remove comments
            string clean = Regex.Replace(json, @"/\*[\s\S]*?\*/", "");
            clean = Regex.Replace(clean, @"//.*", "");

            // Parse Blocks
            // Matches: {"x":0,"y":0,"color":"pink"}
            var blockMatches = Regex.Matches(clean, @"{""x"":(\d+),""y"":(\d+),""color"":""(\w+)""}");
            foreach (Match m in blockMatches)
            {
                int x = int.Parse(m.Groups[1].Value);
                int y = int.Parse(m.Groups[2].Value);
                string color = m.Groups[3].Value;

                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                {
                    int code = GetCodeFromName(color);
                    gridData[x, y] = code;

                    // Visual update
                    LevelEditorCell cell = gridCells.Find(c => c.x == x && c.y == y);
                    if (cell) cell.SetColor(GetColorFromCode(code));
                }
            }

            // Parse Pigs
            // Matches: { "slot": 0, "color": "pink", "ammo": 40 }
            // Note: Regex handles whitespace roughly
            var pigMatches = Regex.Matches(clean, @"{.*?slot""\s*:\s*(\d+).*?color""\s*:\s*""(\w+)"".*?ammo""\s*:\s*(\d+).*?}");

            // Find max slot to init lists
            int maxSlot = -1;
            foreach (Match m in pigMatches)
            {
                int s = int.Parse(m.Groups[1].Value);
                if (s > maxSlot) maxSlot = s;
            }

            for (int i = 0; i <= maxSlot; i++) deckData.Add(new List<EditorPigData>());

            foreach (Match m in pigMatches)
            {
                int slot = int.Parse(m.Groups[1].Value);
                string color = m.Groups[2].Value;
                int ammo = int.Parse(m.Groups[3].Value);

                if (slot < deckData.Count)
                {
                    deckData[slot].Add(new EditorPigData { colorCode = GetCodeFromName(color), ammo = ammo });
                }
            }

            RefreshDeckUI();
            Debug.Log("Level Loaded from JSON!");
        }
    }
}