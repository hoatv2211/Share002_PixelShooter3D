using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PixelShooter3D
{
public class MainMenu : MonoBehaviour
{
    [Header("Main Menu References")]
    public GameObject mainMenuPanel;
    public GameObject gameHUDPanel;
    public Transform levelButtonContainer;
    public GameObject levelButtonPrefab;
    public Button playBtn;

    [Header("HUD")]
    public TextMeshProUGUI hudLevelText;

    [Header("Panel: Level Cleared (Win)")]
    public GameObject levelClearedPanel;
    public TextMeshProUGUI currentLevelText;
    public TextMeshProUGUI rewardCoinsText;
    public Button continueBtn;

    [Header("Panel: Level Failed (Lose)")]
    public GameObject gameOverPanel;
    public Button restartBtn;

    [Header("Powerup Colors")]
    public Color powerupNormalColor = Color.white;
    public Color powerupHighlightColor = Color.white; // For Active Hand Picker
    public Color powerupEmptyColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Darker

    [Header("Powerup UI References")]
    public Button handPickerBtn;
    public TextMeshProUGUI handPickerCountText;
    public Button shuffleBtn;
    public TextMeshProUGUI shuffleCountText;
    public Button superShootBtn;
    public TextMeshProUGUI superShootCountText;
    public Button trayBtn;
    public TextMeshProUGUI trayCountText;

    [Header("Powerup Popups (Explainers)")]
    public GameObject handPickerPopup;
    public Button handPickerCloseBtn;
    public GameObject shufflePopup;
    public Button shuffleCloseBtn;
    public GameObject superShootPopup;
    public Button superShootCloseBtn;
    public GameObject trayPopup;
    public Button trayCloseBtn;

    void Start()
    {
        if (playBtn)
        {
            playBtn.onClick.RemoveAllListeners();
            playBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                if (GameManager.Instance != null) OnLevelSelected(GameManager.Instance.currentLevelIndex);
            });
        }

        // --- HOOK UP POWERUPS ---
        if (handPickerBtn)
        {
            handPickerBtn.onClick.RemoveAllListeners();
            handPickerBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                HandlePowerupClick("Seen_HandPicker", handPickerPopup, handPickerCloseBtn, () =>
                {
                    if (GameManager.Instance) GameManager.Instance.ToggleHandPicker();
                });
            });
        }

        if (shuffleBtn)
        {
            shuffleBtn.onClick.RemoveAllListeners();
            shuffleBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                HandlePowerupClick("Seen_Shuffle", shufflePopup, shuffleCloseBtn, () =>
                {
                    if (GameManager.Instance) GameManager.Instance.ActivateShuffle();
                });
            });
        }

        if (superShootBtn)
        {
            superShootBtn.onClick.RemoveAllListeners();
            superShootBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                HandlePowerupClick("Seen_SuperShoot", superShootPopup, superShootCloseBtn, () =>
                {
                    if (GameManager.Instance) GameManager.Instance.ActivateSuperShoot();
                });
            });
        }

        if (trayBtn)
        {
            trayBtn.onClick.RemoveAllListeners();
            trayBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                HandlePowerupClick("Seen_Tray", trayPopup, trayCloseBtn, () =>
                {
                    if (GameManager.Instance) GameManager.Instance.ActivateExtraTray();
                });
            });
        }

        // Default Close Buttons (Fallback)
        BindCloseBtn(handPickerCloseBtn, handPickerPopup);
        BindCloseBtn(shuffleCloseBtn, shufflePopup);
        BindCloseBtn(superShootCloseBtn, superShootPopup);
        BindCloseBtn(trayCloseBtn, trayPopup);

        GenerateLevelButtons();
        ShowMenu();
    }

    void HandlePowerupClick(string prefKey, GameObject popupObj, Button closeBtn, System.Action onPowerupAction)
    {
        // Check if first time seeing this powerup
        if (PlayerPrefs.GetInt(prefKey, 0) == 0 && popupObj != null)
        {
            popupObj.SetActive(true);
            PlayerPrefs.SetInt(prefKey, 1);
            PlayerPrefs.Save();

            // Re-bind the Close button ("GOT IT") to execute the action immediately upon closing
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(() =>
                {
                    if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                    popupObj.SetActive(false);
                    onPowerupAction?.Invoke();
                });
            }
        }
        else
        {
            // Already seen, execute immediately
            onPowerupAction?.Invoke();
        }
    }

    void BindCloseBtn(Button btn, GameObject popup)
    {
        if (btn != null && popup != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                popup.SetActive(false);
            });
        }
    }

    void ShowMenu()
    {
        if (mainMenuPanel)
        {
            mainMenuPanel.SetActive(true);
            if (gameHUDPanel) gameHUDPanel.SetActive(false);
        }
        else
        {
            // If no main menu, show HUD immediately
            if (gameHUDPanel) gameHUDPanel.SetActive(true);
        }

        if (levelClearedPanel) levelClearedPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        if (handPickerPopup) handPickerPopup.SetActive(false);
        if (shufflePopup) shufflePopup.SetActive(false);
        if (superShootPopup) superShootPopup.SetActive(false);
        if (trayPopup) trayPopup.SetActive(false);
    }

    void GenerateLevelButtons()
    {
        if (levelButtonContainer == null) return;
        foreach (Transform child in levelButtonContainer) Destroy(child.gameObject);

        for (int i = 0; i < 4; i++)
        {
            int levelIndex = i;
            GameObject btn = Instantiate(levelButtonPrefab, levelButtonContainer);

            var textComp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp) textComp.text = (levelIndex + 1).ToString();
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                OnLevelSelected(levelIndex);
            });
        }
    }

    public void OnLevelSelected(int levelIndex)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (levelClearedPanel) levelClearedPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (gameHUDPanel) gameHUDPanel.SetActive(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLevel(levelIndex);

            // Update the level text to show "Level X" (1-indexed)
            UpdateCurrentLevelText();
        }
    }

    /// <summary>
    /// Updates the currentLevelText to display the current level number (1-indexed)
    /// </summary>
    public void UpdateCurrentLevelText()
    {
        if (GameManager.Instance == null) return;
        string text = $"Level {GameManager.Instance.currentLevelIndex + 1}";
        if (currentLevelText != null) currentLevelText.text = text;
        if (hudLevelText != null) hudLevelText.text = text;
    }

    public void ShowGameOver(bool win)
    {
        if (gameHUDPanel) gameHUDPanel.SetActive(false);

        if (SoundManager.Instance)
        {
            if (win) SoundManager.Instance.PlayWin();
            else SoundManager.Instance.PlayLose();
        }

        if (win)
        {
            if (gameOverPanel) gameOverPanel.SetActive(false);
            if (levelClearedPanel) levelClearedPanel.SetActive(true);

            UpdateCurrentLevelText();
            if (rewardCoinsText)
                rewardCoinsText.text = "40";

            if (continueBtn)
            {
                continueBtn.onClick.RemoveAllListeners();
                continueBtn.onClick.AddListener(() =>
                {
                    if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                    if (GameManager.Instance)
                    {
                        int nextLevel = GameManager.Instance.currentLevelIndex + 1;

                        // Wrap around to first level if we completed all levels
                        if (nextLevel >= GameManager.Instance.levels.Count)
                        {
                            nextLevel = 0;
                            Debug.Log("[MainMenu] All levels completed! Restarting from first level.");
                        }

                        OnLevelSelected(nextLevel);
                    }
                });
            }
        }
        else
        {
            if (levelClearedPanel) levelClearedPanel.SetActive(false);
            if (gameOverPanel) gameOverPanel.SetActive(true);

            if (restartBtn)
            {
                restartBtn.onClick.RemoveAllListeners();
                restartBtn.onClick.AddListener(() =>
                {
                    if (SoundManager.Instance) SoundManager.Instance.PlayClick();
                    if (GameManager.Instance)
                    {
                        OnLevelSelected(GameManager.Instance.currentLevelIndex);
                    }
                });
            }
        }
    }

    public void UpdatePowerupUI()
    {
        if (GameManager.Instance == null) return;

        // FIXED: Uses the custom colors from Inspector
        void UpdateButtonState(Button btn, TextMeshProUGUI text, int count, bool isActive = false)
        {
            if (text) text.text = count.ToString();
            if (btn)
            {
                var img = btn.GetComponent<Image>();
                if (isActive)
                {
                    btn.interactable = true;
                    if (img) img.color = powerupHighlightColor;
                }
                else if (count > 0)
                {
                    btn.interactable = true;
                    if (img) img.color = powerupNormalColor;
                }
                else
                {
                    btn.interactable = false;
                    if (img) img.color = powerupEmptyColor;
                }
            }
        }

        UpdateButtonState(handPickerBtn, handPickerCountText, GameManager.Instance.handPickerCount, GameManager.Instance.isHandPickerActive);
        UpdateButtonState(shuffleBtn, shuffleCountText, GameManager.Instance.shuffleCount);
        UpdateButtonState(superShootBtn, superShootCountText, GameManager.Instance.superShootCount);
        UpdateButtonState(trayBtn, trayCountText, GameManager.Instance.trayCount);
    }
}
}