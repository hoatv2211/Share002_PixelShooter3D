using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PixelShooter3D
{
    // --- Data Structures REMOVED (Now in LevelData.cs) ---

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("Levels")]
        [Tooltip("Drag and drop your LevelData ScriptableObjects here")]
        public List<LevelData> levels;

        [Header("Settings")]
        public int maxTraces = 5;
        public float slotSpacing = 1.4f;
        public float gridSize = 0.6f;
        public float beltCornerRadius = 1.5f;

        [Header("Spacing Settings")]
        public float trayStackSpacing = 0.15f;
        public float deckColSpacing = 1.4f;
        public float deckRowSpacing = 1.1f;
        public float holdingSpacing = 1.4f;

        [Header("Belt Settings")]
        public Transform trayEquipPos;
        public Transform trayUnequipPos;
        [Tooltip("Drag 4 Transforms here to define the path corners.")]
        public Transform[] beltCorners;

        [Header("Super Pig Settings")]
        public Transform superPigStartPoint;
        public Transform superPigFightPoint;

        [Header("References")]
        public MainMenu uiController;
        public Transform blockContainer;
        public Transform deckContainer;
        public Transform holdingContainer;
        public Transform trayStackPosition;

        [Tooltip("Assign the 3D TextMeshPro object here, not the UI version.")]
        public TextMeshPro traceCounterText;

        [Header("Prefabs")]
        public GameObject blockPrefab;
        public GameObject pigPrefab;
        public GameObject trayPrefab;
        public GameObject bulletPrefab;
        public GameObject superPigPrefab;

        // --- State ---
        [HideInInspector] public int currentLevelIndex;
        [HideInInspector] public int availableTraces;
        [HideInInspector] public bool isGameOver = false;
        [HideInInspector] public bool isHandPickerActive = false;

        public List<BlockController> activeBlocks = new List<BlockController>();
        public List<PigController> holdingPigs = new List<PigController>();
        public List<Transform> holdingCubes = new List<Transform>(); // Visual cube transforms for parenting players
                                                                     // Note: DeckColumn is now defined in LevelData.cs, so this List works fine if LevelData.cs exists
        public List<List<PigController>> deckColumns = new List<List<PigController>>();

        public int handPickerCount = 3;
        public int shuffleCount = 3;
        public int superShootCount = 1;
        public int trayCount = 3;

        private List<GameObject> trayVisuals = new List<GameObject>();
        private float lastTrayEquipTime = -100f; // Debounce timer

        void Awake()
        {
            Instance = this;
            availableTraces = maxTraces;
        }

        void Update()
        {
            if (levels == null || levels.Count == 0) return;

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                int next = (currentLevelIndex + 1) % levels.Count;
                Debug.Log($"[GameManager] Skipping to next level: {levels[next].levelName} ({next})");
                LoadLevel(next);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                int prev = (currentLevelIndex - 1 + levels.Count) % levels.Count;
                Debug.Log($"[GameManager] Going to previous level: {levels[prev].levelName} ({prev})");
                LoadLevel(prev);
            }
        }

        void Start()
        {
            Debug.Log("[GameManager] Start called.");
            LoadAllJsonLevels();

            // Load level immediately after Resources.LoadAll
            if (levels != null && levels.Count > 0)
            {
                // Load saved level progress from PlayerPrefs (by name, not index)
                string savedLevelName = PlayerPrefs.GetString("CurrentLevelName", "");
                int savedLevel = 0;

                // Find the level by name
                if (!string.IsNullOrEmpty(savedLevelName))
                {
                    for (int i = 0; i < levels.Count; i++)
                    {
                        if (levels[i].levelName == savedLevelName)
                        {
                            savedLevel = i;
                            break;
                        }
                    }
                }

                Debug.Log($"[GameManager] Found {levels.Count} levels. Loading level: {savedLevel} ({levels[savedLevel].levelName})");

                // Force HUD to show if MainMenu is missing
                if (uiController != null && uiController.gameHUDPanel != null)
                {
                    uiController.gameHUDPanel.SetActive(true);
                    if (uiController.mainMenuPanel != null) uiController.mainMenuPanel.SetActive(false);
                }

                LoadLevel(savedLevel);
            }
            else
            {
                Debug.LogWarning("[GameManager] No levels found to load!");
            }
        }

        /// <summary>
        /// Path to the Levels folder inside Resources
        /// </summary>
        public const string LEVELS_RESOURCES_PATH = "Levels";
        public const string LEVELS_FOLDER_PATH = "Pixel Shooter 3D/Resources/Levels";

        void LoadAllJsonLevels()
        {
            LoadLevelsFromResources();
        }

        /// <summary>
        /// Loads levels from Resources folder (Editor/Standalone)
        /// </summary>
        void LoadLevelsFromResources()
        {
            Debug.Log("[GameManager] Loading JSON levels from Resources...");
            // Clear existing levels
            if (levels == null) levels = new List<LevelData>();
            else levels.Clear();

            // Load all TextAssets from Resources/Levels folder
            TextAsset[] levelFiles = Resources.LoadAll<TextAsset>(LEVELS_RESOURCES_PATH);
            Debug.Log($"[GameManager] Found {levelFiles.Length} level files in Resources/{LEVELS_RESOURCES_PATH}.");

            foreach (TextAsset textAsset in levelFiles)
            {
                try
                {
                    string json = textAsset.text;
                    LevelData data = LevelData.CreateFromJson(json);
                    if (data != null)
                    {
                        // Use the asset name as level name
                        data.levelName = textAsset.name;
                        levels.Add(data);
                        Debug.Log($"[GameManager] Loaded level: {data.levelName} from Resources");
                    }
                    else
                    {
                        Debug.LogError($"[GameManager] Failed to parse JSON from {textAsset.name}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[GameManager] Failed to load level {textAsset.name}: {e.Message}");
                }
            }

            if (levels.Count == 0)
            {
                Debug.LogWarning($"[GameManager] No levels found in Resources/{LEVELS_RESOURCES_PATH} folder!");
            }

            // Sort and log levels after loading
            SortAndLogLevels();
        }

        /// <summary>
        /// Sorts and logs the loaded levels (called after platform-specific loading)
        /// </summary>
        void SortAndLogLevels()
        {
            // Sort levels naturally (so NewLevel_2 comes before NewLevel_10)
            levels.Sort((a, b) => NaturalCompare(a.levelName, b.levelName));

            // Debug: Print sorted level order
            Debug.Log("[GameManager] Levels sorted in order:");
            for (int i = 0; i < levels.Count; i++)
            {
                Debug.Log($"  [{i}] {levels[i].levelName}");
            }
        }

        /// <summary>
        /// Natural string comparison that handles numbers correctly.
        /// E.g., "Level2" < "Level10" (instead of "Level10" < "Level2" with standard string compare)
        /// </summary>
        private int NaturalCompare(string a, string b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            int ia = 0, ib = 0;
            while (ia < a.Length && ib < b.Length)
            {
                if (char.IsDigit(a[ia]) && char.IsDigit(b[ib]))
                {
                    // Extract numbers and compare numerically
                    int numA = 0, numB = 0;
                    while (ia < a.Length && char.IsDigit(a[ia]))
                    {
                        numA = numA * 10 + (a[ia] - '0');
                        ia++;
                    }
                    while (ib < b.Length && char.IsDigit(b[ib]))
                    {
                        numB = numB * 10 + (b[ib] - '0');
                        ib++;
                    }
                    if (numA != numB) return numA.CompareTo(numB);
                }
                else
                {
                    // Compare characters
                    if (a[ia] != b[ib]) return a[ia].CompareTo(b[ib]);
                    ia++;
                    ib++;
                }
            }
            return a.Length.CompareTo(b.Length);
        }

        // --- LEVEL LOADING ---
        public void LoadLevel(int levelIndex)
        {
            // Force containers to center
            if (blockContainer) blockContainer.localPosition = new Vector3(0, blockContainer.localPosition.y, blockContainer.localPosition.z);
            if (deckContainer) deckContainer.localPosition = new Vector3(0, deckContainer.localPosition.y, deckContainer.localPosition.z);
            if (holdingContainer) holdingContainer.localPosition = new Vector3(0, holdingContainer.localPosition.y, holdingContainer.localPosition.z);

            currentLevelIndex = levelIndex;

            // Safety Check
            if (levels == null || levels.Count == 0)
            {
                Debug.LogError("[GameManager] No levels assigned in GameManager!");
                return;
            }

            if (currentLevelIndex >= levels.Count) currentLevelIndex = 0;

            LevelData data = levels[currentLevelIndex];

            // Save current level NAME to PlayerPrefs (not index, so it survives level reordering)
            PlayerPrefs.SetString("CurrentLevelName", data.levelName);
            PlayerPrefs.Save();

            Debug.Log($"[GameManager] Loading level: {data.levelName} (Index: {currentLevelIndex}) - Saved to PlayerPrefs");

            // Apply Settings from LevelData
            maxTraces = data.maxTraces;
            gridSize = data.gridSize;
            slotSpacing = data.slotSpacing;
            beltCornerRadius = data.beltCornerRadius;
            trayStackSpacing = data.trayStackSpacing;
            deckColSpacing = data.deckColSpacing;
            deckRowSpacing = data.deckRowSpacing;
            holdingSpacing = data.holdingSpacing;

            ClearScene();

            isGameOver = false;
            availableTraces = maxTraces;
            isHandPickerActive = false;
            if (CameraController.Instance) CameraController.Instance.SetTarget(false);

            handPickerCount = 3;
            shuffleCount = 3;
            superShootCount = 1;
            trayCount = 3;

            // Setup Holding Pigs List
            int hCount = data.holdingCount > 0 ? data.holdingCount : 5;
            holdingPigs.Clear();
            for (int i = 0; i < hCount; i++) holdingPigs.Add(null);

            // Setup Holding Visuals
            SetupHoldingVisuals(hCount, holdingSpacing);

            // Build Grid using LevelData helper properties
            int rows = data.Rows;
            int cols = data.Cols;

            float startX = -((cols * gridSize) / 2f) + gridSize / 2f;
            float startZ = -((rows * gridSize) / 2f) + gridSize / 2f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int val = data.GetCell(r, c); // Using helper method from LevelData
                    if (val != 0)
                    {
                        GameObject b = Instantiate(blockPrefab, blockContainer);
                        b.transform.localPosition = new Vector3(startX + c * gridSize, 0, startZ + r * gridSize);
                        float s = data.blockScale > 0 ? data.blockScale : 1.0f;
                        b.transform.localScale = new Vector3(s, 1f, s);
                        BlockController bc = b.GetComponent<BlockController>();
                        bc.colorCode = val;

                        // Apply color from palette
                        Color? forcedColor = null;
                        if (val > 0 && val <= data.palette.Count)
                        {
                            forcedColor = data.palette[val - 1];
                        }

                        bc.Init(forcedColor);
                        activeBlocks.Add(bc);
                    }
                }
            }

            // Build Deck
            deckColumns.Clear();
            for (int i = 0; i < data.deck.Count; i++)
            {
                List<PigController> newCol = new List<PigController>();
                deckColumns.Add(newCol);

                var colData = data.deck[i];
                for (int j = 0; j < colData.pigs.Count; j++)
                {
                    PigInfo pData = colData.pigs[j];
                    GameObject p = Instantiate(pigPrefab, deckContainer);
                    PigController pc = p.GetComponent<PigController>();

                    pc.colorCode = pData.c;
                    pc.ammo = pData.a;
                    pc.colIndex = i;
                    pc.rowIndex = j;

                    // Apply color from palette
                    Color? forcedColor = null;
                    if (pData.c > 0 && pData.c <= data.palette.Count)
                    {
                        forcedColor = data.palette[pData.c - 1];
                    }

                    pc.Init(forcedColor);

                    float x = (i - (data.deck.Count - 1) / 2f) * deckColSpacing;
                    float z = (j * deckRowSpacing);
                    pc.transform.localPosition = new Vector3(x, 0, z);
                    pc.transform.localRotation = Quaternion.Euler(0, 180, 0);

                    newCol.Add(pc);
                }
            }

            UpdateTraceUI();
            if (uiController) uiController.UpdatePowerupUI();
            if (uiController) uiController.UpdateCurrentLevelText();
        }

        void SetupHoldingVisuals(int count, float spacing)
        {
            // Find all potential cubes in the scene
            List<Transform> cubes = new List<Transform>();

            // Search everywhere for objects named "HoldingCube" that aren't pigs
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                if (go.name.Contains("HoldingCube") && go.GetComponent<PigController>() == null)
                {
                    // We want the parent object (e.g. "HoldingCube (1)") not the mesh child
                    Transform target = go.transform;
                    if (target.parent != null && target.parent.name.Contains("HoldingCube"))
                    {
                        target = target.parent;
                    }

                    if (target.parent == null ||
                        target.parent.name == "HoldingContainers_Visual" ||
                        target.parent == holdingContainer)
                    {
                        if (!cubes.Contains(target)) cubes.Add(target);
                    }
                }
            }

            // Sort by current world X position to maintain order
            cubes.Sort((a, b) => a.position.x.CompareTo(b.position.x));

            // Clear and rebuild holdingCubes list
            holdingCubes.Clear();

            // Reposition and hide/show
            for (int i = 0; i < cubes.Count; i++)
            {
                if (i < count)
                {
                    cubes[i].gameObject.SetActive(true);
                    float x = (i - (count - 1) / 2f) * spacing;

                    if (holdingContainer != null)
                    {
                        cubes[i].SetParent(holdingContainer);
                        // Position them slightly above the ground (Y=0 is ground level for the container)
                        cubes[i].localPosition = new Vector3(x, -0.05f, 0);
                        cubes[i].localRotation = Quaternion.identity;
                        cubes[i].localScale = Vector3.one;

                        // Ensure child mesh is centered
                        foreach (Transform child in cubes[i])
                        {
                            if (child.GetComponent<PigController>() == null)
                                child.localPosition = Vector3.zero;
                        }
                    }

                    // Store reference for player parenting
                    holdingCubes.Add(cubes[i]);
                }
                else
                {
                    cubes[i].gameObject.SetActive(false);
                }
            }
        }

        void ClearScene()
        {
            if (blockContainer)
            {
                List<GameObject> toDestroy = new List<GameObject>();
                foreach (Transform t in blockContainer) toDestroy.Add(t.gameObject);
                foreach (GameObject go in toDestroy) Destroy(go);
            }
            if (deckContainer)
            {
                List<GameObject> toDestroy = new List<GameObject>();
                foreach (Transform t in deckContainer) toDestroy.Add(t.gameObject);
                foreach (GameObject go in toDestroy) Destroy(go);
            }
            if (holdingContainer)
            {
                // Destroy pigs inside HoldingCubes (and any direct children that are pigs)
                List<GameObject> toDestroy = new List<GameObject>();
                foreach (Transform cube in holdingContainer)
                {
                    if (cube.GetComponent<PigController>() != null)
                    {
                        toDestroy.Add(cube.gameObject);
                    }
                    else
                    {
                        // Check children of HoldingCubes for pigs
                        foreach (Transform child in cube)
                        {
                            if (child.GetComponent<PigController>() != null)
                                toDestroy.Add(child.gameObject);
                        }
                    }
                }
                foreach (GameObject go in toDestroy) Destroy(go);
            }

            // --- FIX: Cleanup orphaned pigs (on belt/jumping) ---
            PigController[] allPigs = FindObjectsOfType<PigController>();
            foreach (var pig in allPigs)
            {
                Destroy(pig.gameObject);
            }
            // ----------------------------------------------------

            activeBlocks.Clear();
            holdingPigs.Clear();
        }

        // --- GAME LOGIC ---
        public void CheckWinCondition()
        {
            if (activeBlocks.Count == 0 && !isGameOver)
            {
                if (uiController) uiController.ShowGameOver(true);
                isGameOver = true;
            }
        }

        public void TriggerGameOver()
        {
            if (isGameOver) return;
            if (uiController) uiController.ShowGameOver(false);
            isGameOver = true;
        }

        public void UpdateTraceUI()
        {
            foreach (var t in trayVisuals) Destroy(t);
            trayVisuals.Clear();

            int visualCount = Mathf.Min(availableTraces, 20);

            for (int i = 0; i < visualCount; i++)
            {
                GameObject t = Instantiate(trayPrefab, trayStackPosition);
                t.transform.localPosition = new Vector3(0, i * trayStackSpacing, 0);
                trayVisuals.Add(t);
            }

            if (traceCounterText != null)
                traceCounterText.text = $"{availableTraces}/{maxTraces}";
        }

        // --- TRAY ANIMATION SYSTEM ---

        // 1. Send Tray FROM Stack TO Equip Position
        public void SendTrayToEquip(float duration)
        {
            // SAFETY CHECK: Ensure we actually have traces left
            if (availableTraces <= 0)
            {
                Debug.LogWarning("[GameManager] SendTrayToEquip called but no traces left.");
                return;
            }

            // DEBOUNCE CHECK: Increased to 0.5s to handle "held down" inputs or double-clicks
            float timeSinceLast = Time.unscaledTime - lastTrayEquipTime;
            if (timeSinceLast < 0.5f)
            {
                Debug.Log($"[GameManager] Blocked duplicate call. Time Delta: {timeSinceLast:F3}s (Limit: 0.5s)");
                return;
            }

            lastTrayEquipTime = Time.unscaledTime;

            // Top of stack
            Vector3 startPos = trayStackPosition.position + new Vector3(0, (availableTraces - 1) * trayStackSpacing, 0);

            // Remove from logic immediately so UI updates
            availableTraces--;

            // DEBUG LOG: Includes Frame and Time to diagnose double-counting
            Debug.Log($"[GameManager] ✅ Sending Tray. Frame: {Time.frameCount} | Time: {Time.unscaledTime:F2} | Traces Remaining: {availableTraces}");

            UpdateTraceUI();

            // --- SOUND UPDATE ---
            if (SoundManager.Instance) SoundManager.Instance.PlayJump();

            StartCoroutine(TrayMoveRoutine(startPos, trayEquipPos.position, duration, true));
        }

        // 2. Return Tray FROM Unequip Position TO Stack
        public void ReturnTray(Vector3 startPos)
        {
            // Target is new top of stack
            Vector3 targetPos = trayStackPosition.position + new Vector3(0, availableTraces * trayStackSpacing, 0);

            // --- SOUND UPDATE ---
            if (SoundManager.Instance) SoundManager.Instance.PlayJump();

            StartCoroutine(TrayMoveRoutine(startPos, targetPos, 0.6f, false));
        }

        IEnumerator TrayMoveRoutine(Vector3 start, Vector3 end, float duration, bool isEquipping)
        {
            GameObject t = Instantiate(trayPrefab, start, Quaternion.identity);
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float val = elapsed / duration;

                // Parabolic arc
                Vector3 pos = Vector3.Lerp(start, end, val);
                pos.y += Mathf.Sin(val * Mathf.PI) * 2.0f;

                t.transform.position = pos;
                yield return null;
            }

            // Arrival
            Destroy(t);

            if (!isEquipping)
            {
                // If returning, add back to logic now
                availableTraces++;
                if (availableTraces > maxTraces) availableTraces = maxTraces;
                UpdateTraceUI();
            }
        }

        // --- MATH HELPERS ---
        public Vector3 GetSmoothPathPoint(float t)
        {
            if (beltCorners == null || beltCorners.Length < 4) return Vector3.zero;

            t = Mathf.Clamp01(t);
            float totalLen = 0;
            List<float> lengths = new List<float>();

            for (int i = 0; i < beltCorners.Length; i++)
            {
                Transform curr = beltCorners[i];
                Transform next = beltCorners[(i + 1) % beltCorners.Length];
                float dist = Vector3.Distance(curr.position, next.position);
                float straightLen = Mathf.Max(0, dist - (beltCornerRadius * 2));
                float arcLen = (Mathf.PI * beltCornerRadius) / 2.0f;
                lengths.Add(straightLen);
                lengths.Add(arcLen);
                totalLen += (straightLen + arcLen);
            }

            float targetDist = t * totalLen;
            float currentDist = 0;

            for (int i = 0; i < beltCorners.Length; i++)
            {
                Transform p1 = beltCorners[i];
                Transform p2 = beltCorners[(i + 1) % beltCorners.Length];
                Vector3 dir = (p2.position - p1.position).normalized;
                Vector3 lineStart = p1.position + dir * beltCornerRadius;
                Vector3 lineEnd = p2.position - dir * beltCornerRadius;

                float straightLen = lengths[i * 2];
                if (targetDist <= currentDist + straightLen)
                {
                    float segmentT = (targetDist - currentDist) / straightLen;
                    return Vector3.Lerp(lineStart, lineEnd, segmentT);
                }
                currentDist += straightLen;

                float arcLen = lengths[i * 2 + 1];
                if (targetDist <= currentDist + arcLen)
                {
                    float segmentT = (targetDist - currentDist) / arcLen;
                    Transform p3 = beltCorners[(i + 2) % beltCorners.Length];
                    Vector3 nextDir = (p3.position - p2.position).normalized;
                    Vector3 nextLineStart = p2.position + nextDir * beltCornerRadius;
                    return GetBezierPoint(segmentT, lineEnd, p2.position, nextLineStart);
                }
                currentDist += arcLen;
            }
            return beltCorners[0].position;
        }

        Vector3 GetBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
        }

        // --- POWERUPS ---
        public void ActivateExtraTray()
        {
            if (trayCount > 0)
            {
                trayCount--;
                maxTraces++;
                availableTraces++;
                UpdateTraceUI();
                // Sound
                if (SoundManager.Instance) SoundManager.Instance.PlayPowerup();
                if (uiController) uiController.UpdatePowerupUI();
            }
        }

        public void ActivateSuperShoot()
        {
            if (superShootCount > 0)
            {
                superShootCount--;
                Instantiate(superPigPrefab, blockContainer.position + new Vector3(0, -5, 6), Quaternion.identity);
                // Sound
                if (SoundManager.Instance) SoundManager.Instance.PlayPowerup();
                if (uiController) uiController.UpdatePowerupUI();
            }
        }

        public void ActivateShuffle()
        {
            if (shuffleCount > 0)
            {
                shuffleCount--;
                ShuffleDeck();
                // Sound
                if (SoundManager.Instance) SoundManager.Instance.PlayPowerup();
                if (uiController) uiController.UpdatePowerupUI();
            }
        }

        public void ToggleHandPicker()
        {
            if (handPickerCount > 0 || isHandPickerActive)
            {
                isHandPickerActive = !isHandPickerActive;
                if (CameraController.Instance) CameraController.Instance.SetTarget(isHandPickerActive);
                // Sound
                if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                if (uiController) uiController.UpdatePowerupUI();
            }
        }

        public void UseHandPicker()
        {
            handPickerCount--;
            isHandPickerActive = false;
            if (CameraController.Instance) CameraController.Instance.SetTarget(false);
            // Sound
            if (SoundManager.Instance) SoundManager.Instance.PlayPowerup();
            if (uiController) uiController.UpdatePowerupUI();
        }

        void ShuffleDeck()
        {
            List<PigController> allPigs = new List<PigController>();
            foreach (var col in deckColumns) foreach (var pig in col) allPigs.Add(pig);
            if (allPigs.Count == 0) return;

            for (int i = 0; i < allPigs.Count; i++)
            {
                PigController temp = allPigs[i]; int rnd = Random.Range(i, allPigs.Count);
                allPigs[i] = allPigs[rnd]; allPigs[rnd] = temp;
            }
            foreach (var col in deckColumns) col.Clear();
            int pigIdx = 0; int numCols = deckColumns.Count;
            while (pigIdx < allPigs.Count)
            {
                for (int c = 0; c < numCols; c++)
                {
                    if (pigIdx >= allPigs.Count) break;
                    PigController pig = allPigs[pigIdx];
                    deckColumns[c].Add(pig);
                    pig.colIndex = c; pig.rowIndex = deckColumns[c].Count - 1;
                    pigIdx++;
                }
            }
        }
    }
}
