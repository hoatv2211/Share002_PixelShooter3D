#if MODULE_ADMOB
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PixelShooter3D
{
    public class DeveloperSettingsController : MonoBehaviour
    {
        [Header("Level Control Buttons")]
        public Button nextLevelButton;
        public Button previousLevelButton;
        public Button reloadLevelButton;
        public Button triggerWinButton;
        public Button triggerLoseButton;

        [Header("Visual Theme Control")]
        public Button previousVisualButton;
        public Button nextVisualButton;
        public TextMeshProUGUI visualsIndexText;

        [Header("Ad Control Buttons")]
        public Button showBannerButton;
        public Button hideBannerButton;
        public Button showInterstitialButton;
        public Button requestInterstitialButton;
        public Button showRewardedButton;
        public Button requestRewardedButton;

        private void Start()
        {
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);

            if (previousLevelButton != null)
                previousLevelButton.onClick.AddListener(OnPreviousLevelClicked);

            if (reloadLevelButton != null)
                reloadLevelButton.onClick.AddListener(OnReloadLevelClicked);

            if (triggerWinButton != null)
                triggerWinButton.onClick.AddListener(OnTriggerWinClicked);

            if (triggerLoseButton != null)
                triggerLoseButton.onClick.AddListener(OnTriggerLoseClicked);

            if (previousVisualButton != null)
                previousVisualButton.onClick.AddListener(OnPreviousVisualClicked);

            if (nextVisualButton != null)
                nextVisualButton.onClick.AddListener(OnNextVisualClicked);

            // Ad buttons
            if (showBannerButton != null)
                showBannerButton.onClick.AddListener(OnShowBannerClicked);

            if (hideBannerButton != null)
                hideBannerButton.onClick.AddListener(OnHideBannerClicked);

            if (showInterstitialButton != null)
                showInterstitialButton.onClick.AddListener(OnShowInterstitialClicked);

            if (requestInterstitialButton != null)
                requestInterstitialButton.onClick.AddListener(OnRequestInterstitialClicked);

            if (showRewardedButton != null)
                showRewardedButton.onClick.AddListener(OnShowRewardedClicked);

            if (requestRewardedButton != null)
                requestRewardedButton.onClick.AddListener(OnRequestRewardedClicked);
        }

        private void OnNextLevelClicked()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.levels == null || gm.levels.Count == 0) return;
            int next = (gm.currentLevelIndex + 1) % gm.levels.Count;
            gm.LoadLevel(next);
        }

        private void OnPreviousLevelClicked()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.levels == null || gm.levels.Count == 0) return;
            int prev = (gm.currentLevelIndex - 1 + gm.levels.Count) % gm.levels.Count;
            gm.LoadLevel(prev);
        }

        private void OnReloadLevelClicked()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.LoadLevel(gm.currentLevelIndex);
        }

        private void OnTriggerWinClicked()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Clear all remaining blocks to trigger win
            for (int i = gm.activeBlocks.Count - 1; i >= 0; i--)
            {
                if (gm.activeBlocks[i] != null)
                    Destroy(gm.activeBlocks[i].gameObject);
            }
            gm.activeBlocks.Clear();
            gm.CheckWinCondition();
        }

        private void OnTriggerLoseClicked()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.TriggerGameOver();
        }

        private void OnPreviousVisualClicked()
        {
            // TODO: Wire up when VisualThemeManager is implemented
            Debug.Log("[DevSettings] Previous Visual - no VisualThemeManager found.");
        }

        private void OnNextVisualClicked()
        {
            // TODO: Wire up when VisualThemeManager is implemented
            Debug.Log("[DevSettings] Next Visual - no VisualThemeManager found.");
        }

        // --- Ad Controls ---

        private void OnShowBannerClicked()
        {
            Debug.Log("[DevSettings] Show Banner");
            Ragendom.AdsManager.ShowBanner();
        }

        private void OnHideBannerClicked()
        {
            Debug.Log("[DevSettings] Hide Banner");
            Ragendom.AdsManager.HideBanner();
        }

        private void OnShowInterstitialClicked()
        {
            Debug.Log("[DevSettings] Show Interstitial");
            Ragendom.AdsManager.ShowInterstitial((success) =>
            {
                Debug.Log($"[DevSettings] Interstitial closed. Success: {success}");
                Ragendom.AdsManager.RequestInterstitial();
            }, ignoreConditions: true);
        }

        private void OnRequestInterstitialClicked()
        {
            Debug.Log("[DevSettings] Request Interstitial");
            Ragendom.AdsManager.RequestInterstitial();
        }

        private void OnShowRewardedClicked()
        {
            Debug.Log("[DevSettings] Show Rewarded");
            Ragendom.AdsManager.ShowRewardBasedVideo((success) =>
            {
                Debug.Log($"[DevSettings] Rewarded closed. Rewarded: {success}");
                Ragendom.AdsManager.RequestRewardBasedVideo();
            });
        }

        private void OnRequestRewardedClicked()
        {
            Debug.Log("[DevSettings] Request Rewarded");
            Ragendom.AdsManager.RequestRewardBasedVideo();
        }
    }
}

#endif