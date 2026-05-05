using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PixelShooter3D
{
public class PlayerSetupManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject firstSetupBlocksPopup;
    public TextMeshProUGUI popupText;
    public GameObject slidersPlayersPanel;
    public Button setupPlayersButton;
    public Button randomizeButton;
    public Button backButton;
    public GameObject setupButtonContainer;
    public CameraFocusController cameraController;
    public CameraController mainCameraController;
    public PigEditorPopup pigEditorPopupPrefab;

    [Header("Data References")]
    public Transform blockContainer;
    public Transform deckContainer;

    [Header("Settings")]
    public int minBlocksRequired = 5;
    public int minColorsRequired = 2;
    public float popupDuration = 1.0f;

    private PigEditorPopup activePopup;
    private List<Color> cachedAvailableColors = new List<Color>();

    [HideInInspector] public bool playersAreSetUp = false;

    void Start()
    {
        if (setupPlayersButton)
        {
            setupPlayersButton.onClick.RemoveAllListeners();
            setupPlayersButton.onClick.AddListener(OnSetupButtonClicked);
        }

        if (randomizeButton)
        {
            randomizeButton.onClick.RemoveAllListeners();
            randomizeButton.onClick.AddListener(SetupPlayers);
        }

        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        if (firstSetupBlocksPopup) firstSetupBlocksPopup.SetActive(false);
        if (slidersPlayersPanel) slidersPlayersPanel.SetActive(false);
    }

    void Update()
    {
        // Only active during setup phase
        if (slidersPlayersPanel != null && slidersPlayersPanel.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
        }
    }

    void HandleClick()
    {
        // Raycast for pigs
        // Use Camera.main if available, otherwise try to find the camera used for rendering
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        Debug.Log("LevelEditorClick: Attempting raycast...");

        // Use a layer mask or check all hits if needed, but standard Raycast should work if pigs have colliders
        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("LevelEditorClick: Hit " + hit.collider.name);
            PigController pig = hit.collider.GetComponentInParent<PigController>();
            if (pig != null)
            {
                Debug.Log("LevelEditorClick: Found PigController on " + pig.name);
                ShowPigPopup(pig);
                return;
            }
            else
            {
                Debug.Log("LevelEditorClick: Hit object has no PigController.");
            }
        }
        else
        {
            Debug.Log("LevelEditorClick: Raycast hit nothing.");
        }
    }

    void ShowPigPopup(PigController pig)
    {
        if (pigEditorPopupPrefab == null) return;

        // Close existing
        if (activePopup != null)
        {
            Destroy(activePopup.gameObject);
        }

        // Instantiate new popup
        // Position it near the pig. Since pig is in world space, we can put popup in world space canvas or overlay.
        // Assuming Canvas_DeckContainer is WorldSpace, we can parent it there.
        
        // Use the canvas attached to this script's GameObject (Canvas_DeckContainer)
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("PlayerSetupManager is not attached to a Canvas!");
            return;
        }

        Transform parentCanvas = canvas.transform;
        activePopup = Instantiate(pigEditorPopupPrefab, parentCanvas);
        
        // Position next to pig
        // Convert pig world pos to local pos of canvas
        Vector3 pigWorldPos = pig.transform.position;
        Vector3 localPos = parentCanvas.InverseTransformPoint(pigWorldPos);
        
        // Offset slightly to the right/up
        activePopup.transform.localPosition = localPos + new Vector3(1.5f, 1.0f, 0);
        activePopup.transform.localRotation = Quaternion.identity;
        // Scale down to match world space UI scale (approx 0.02)
        activePopup.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

        // Setup Popup
        activePopup.Setup(
            cachedAvailableColors, 
            pig.GetComponentInChildren<Renderer>().material.color, 
            pig.ammo,
            (Color c) => {
                // On Color Selected
                Renderer r = pig.GetComponentInChildren<Renderer>();
                if (r) r.material.color = c;
                // Update color code? We need reverse lookup or just trust visual
                // For now just visual update as requested
            },
            (int ammo) => {
                // On Ammo Changed
                pig.ammo = ammo;
                if (pig.ammoText) pig.ammoText.text = ammo.ToString();
            }
        );
    }

    public void OnSetupButtonClicked()
    {
        string errorMessage;
        if (CheckConditions(out errorMessage))
        {
            // Success: Enable sliders, disable button, move camera
            if (slidersPlayersPanel) slidersPlayersPanel.SetActive(true);
            
            // Disable container if assigned, otherwise fallback to button
            if (setupButtonContainer) setupButtonContainer.SetActive(false);
            else if (setupPlayersButton) setupPlayersButton.gameObject.SetActive(false);
            
            if (mainCameraController) mainCameraController.enabled = false;

            if (cameraController)
            {
                cameraController.FocusOnPlayers();
            }

            SetupPlayers();
        }
        else
        {
            // Failure: Show popup with specific message
            if (popupText) popupText.text = errorMessage;
            StartCoroutine(ShowPopupRoutine());
        }
    }

    bool CheckConditions(out string errorMessage)
    {
        errorMessage = "";
        if (blockContainer == null) return false;

        int blockCount = 0;
        HashSet<Color> uniqueColors = new HashSet<Color>();

        foreach (Transform child in blockContainer)
        {
            if (!child.gameObject.activeSelf) continue;
            
            blockCount++;
            Renderer r = child.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                uniqueColors.Add(r.material.color);
            }
        }

        if (blockCount < minBlocksRequired)
        {
            errorMessage = "First Set Up Blocks";
            return false;
        }

        if (uniqueColors.Count < minColorsRequired)
        {
            errorMessage = $"Add min. {minColorsRequired} different colors to blocks";
            return false;
        }

        return true;
    }

    void SetupPlayers()
    {
        if (blockContainer == null || deckContainer == null) return;

        playersAreSetUp = true;

        // 1. Analyze Blocks
        Dictionary<Color, int> colorCounts = new Dictionary<Color, int>();
        Dictionary<Color, int> colorToId = new Dictionary<Color, int>();
        int nextId = 1;

        foreach (Transform child in blockContainer)
        {
            if (!child.gameObject.activeSelf) continue;

            Renderer r = child.GetComponentInChildren<Renderer>();
            BlockController block = child.GetComponent<BlockController>();

            if (r != null && block != null)
            {
                Color c = r.material.color;
                if (!colorCounts.ContainsKey(c))
                {
                    colorCounts[c] = 0;
                    colorToId[c] = nextId++;
                }
                colorCounts[c]++;
                
                block.colorCode = colorToId[c];
                // block.enabled = true; // Keep disabled in Editor
            }
        }

        // 2. Get Players
        List<PigController> pigs = new List<PigController>();
        foreach (Transform child in deckContainer)
        {
            if (!child.gameObject.activeSelf) continue;
            PigController pig = child.GetComponent<PigController>();
            if (pig != null)
            {
                pigs.Add(pig);
                // Do NOT enable script in Editor to prevent movement logic
                // pig.enabled = true; 
            }
        }

        if (pigs.Count == 0) return;

        // 3. Assign Colors and Distribute Ammo
        List<Color> availableColors = colorCounts.Keys.ToList();
        cachedAvailableColors = new List<Color>(availableColors); // Cache for popup

        // Shuffle pigs for random assignment
        pigs = pigs.OrderBy(x => Random.value).ToList();

        // Ensure every color is assigned at least once if possible
        // Or just assign randomly from available colors
        
        // Strategy: 
        // If pigs.Count >= colors.Count, ensure coverage.
        // If pigs.Count < colors.Count, some colors won't be shot (impossible to win? maybe warn?)
        
        Dictionary<Color, List<PigController>> pigsByColor = new Dictionary<Color, List<PigController>>();
        foreach (Color c in availableColors) pigsByColor[c] = new List<PigController>();

        for (int i = 0; i < pigs.Count; i++)
        {
            Color assignedColor;
            if (i < availableColors.Count)
            {
                assignedColor = availableColors[i];
            }
            else
            {
                assignedColor = availableColors[Random.Range(0, availableColors.Count)];
            }
            
            pigsByColor[assignedColor].Add(pigs[i]);
            
            // Set Visuals
            pigs[i].colorCode = colorToId[assignedColor];
            Renderer pr = pigs[i].GetComponentInChildren<Renderer>();
            if (pr != null) pr.material.color = assignedColor;
        }

        // 4. Distribute Ammo
        foreach (var kvp in pigsByColor)
        {
            Color c = kvp.Key;
            List<PigController> group = kvp.Value;
            int totalAmmo = colorCounts[c];

            if (group.Count == 0) continue;

            if (group.Count == 1)
            {
                group[0].ammo = totalAmmo;
            }
            else
            {
                // Random distribution
                // Ensure each gets at least 1 if totalAmmo >= group.Count
                int remainingAmmo = totalAmmo;
                
                // Give 1 to each first
                if (remainingAmmo >= group.Count)
                {
                    foreach (var p in group)
                    {
                        p.ammo = 1;
                        remainingAmmo--;
                    }
                }

                // Distribute rest randomly
                for (int i = 0; i < group.Count - 1; i++)
                {
                    if (remainingAmmo <= 0) break;
                    int take = Random.Range(0, remainingAmmo + 1); // 0 to remaining
                    // Bias towards even distribution? Or purely random? User said "random but correct".
                    // Let's do simple random split
                    group[i].ammo += take;
                    remainingAmmo -= take;
                }
                group[group.Count - 1].ammo += remainingAmmo;
            }

            // Update Text
            foreach (var p in group)
            {
                if (p.ammoText != null) p.ammoText.text = p.ammo.ToString();
            }
        }
    }

    IEnumerator ShowPopupRoutine()
    {
        if (firstSetupBlocksPopup)
        {
            firstSetupBlocksPopup.SetActive(true);
            yield return new WaitForSeconds(popupDuration);
            firstSetupBlocksPopup.SetActive(false);
        }
    }

    public void OnBackButtonClicked()
    {
        PerformBack();
    }

    void PerformBack()
    {
        // Disable sliders panel
        if (slidersPlayersPanel) slidersPlayersPanel.SetActive(false);
        
        // Enable setup button container
        if (setupButtonContainer) setupButtonContainer.SetActive(true);
        else if (setupPlayersButton) setupPlayersButton.gameObject.SetActive(true);

        // Re-enable main camera controller to take control back
        if (mainCameraController)
        {
            mainCameraController.enabled = true;
            // Reset target to default just in case
            mainCameraController.SetTarget(false);
        }

        // Switch back to Colorize tab if TabManager exists
        BlockEditorTabManager tabManager = Object.FindAnyObjectByType<BlockEditorTabManager>();
        if (tabManager)
        {
            tabManager.ShowColorizeTab();
        }
    }

}
}
