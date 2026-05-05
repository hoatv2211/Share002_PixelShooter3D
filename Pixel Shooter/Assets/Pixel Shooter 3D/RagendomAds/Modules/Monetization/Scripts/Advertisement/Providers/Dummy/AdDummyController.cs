using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ragendom
{
    public class AdDummyController : MonoBehaviour
    {
        private GameObject bannerObject;
        private GameObject interstitialObject;
        private GameObject rewardedVideoObject;

        private AdsSettings adsSettings;

        public static AdDummyController CreateObject()
        {
            GameObject canvasObject = new GameObject("[Ad Dummy Controller]");
            DontDestroyOnLoad(canvasObject);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            AdDummyController controller = canvasObject.AddComponent<AdDummyController>();
            controller.CreateBannerUI(canvasObject.transform);
            controller.CreateInterstitialUI(canvasObject.transform);
            controller.CreateRewardedVideoUI(canvasObject.transform);

            return controller;
        }

        public void Init(AdsSettings adsSettings)
        {
            this.adsSettings = adsSettings;

            SetBannerPosition(adsSettings.DummyContainer.BannerPosition);

            bannerObject.SetActive(false);
            interstitialObject.SetActive(false);
            rewardedVideoObject.SetActive(false);
        }

        #region Banner

        private void CreateBannerUI(Transform parent)
        {
            bannerObject = new GameObject("Dummy Banner");
            bannerObject.transform.SetParent(parent, false);

            RectTransform bannerRect = bannerObject.AddComponent<RectTransform>();
            bannerRect.sizeDelta = new Vector2(0, 84);

            Image bannerBg = bannerObject.AddComponent<Image>();
            bannerBg.color = new Color(0.2f, 0.6f, 0.2f, 0.9f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(bannerObject.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "TEST BANNER";
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private void SetBannerPosition(BannerPosition position)
        {
            RectTransform rect = bannerObject.GetComponent<RectTransform>();

            if (position == BannerPosition.Top)
            {
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.anchoredPosition = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.anchoredPosition = Vector2.zero;
            }
        }

        public void ShowBanner()
        {
            bannerObject.SetActive(true);
        }

        public void HideBanner()
        {
            bannerObject.SetActive(false);
        }

        #endregion

        #region Interstitial

        private void CreateInterstitialUI(Transform parent)
        {
            interstitialObject = new GameObject("Dummy Interstitial");
            interstitialObject.transform.SetParent(parent, false);

            RectTransform rect = interstitialObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = interstitialObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(interstitialObject.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, 50);
            titleRect.sizeDelta = new Vector2(600, 80);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "TEST INTERSTITIAL";
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // Close button
            CreateButton(interstitialObject.transform, "CLOSE", new Vector2(0, -80), new Color(0.8f, 0.2f, 0.2f), () =>
            {
                interstitialObject.SetActive(false);
                AdsManager.ExecuteInterstitialCallback(true);
                AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.Interstitial);
            });
        }

        public void ShowInterstitial()
        {
            interstitialObject.SetActive(true);
        }

        #endregion

        #region Rewarded Video

        private void CreateRewardedVideoUI(Transform parent)
        {
            rewardedVideoObject = new GameObject("Dummy Rewarded Video");
            rewardedVideoObject.transform.SetParent(parent, false);

            RectTransform rect = rewardedVideoObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = rewardedVideoObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.3f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(rewardedVideoObject.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, 80);
            titleRect.sizeDelta = new Vector2(600, 80);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "TEST REWARDED VIDEO";
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // Close button (no reward)
            CreateButton(rewardedVideoObject.transform, "CLOSE", new Vector2(0, -20), new Color(0.8f, 0.2f, 0.2f), () =>
            {
                rewardedVideoObject.SetActive(false);
                AdsManager.ExecuteRewardVideoCallback(false);
                AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.RewardedVideo);
            });

            // Get Reward button
            CreateButton(rewardedVideoObject.transform, "GET REWARD", new Vector2(0, -120), new Color(0.2f, 0.6f, 0.2f), () =>
            {
                rewardedVideoObject.SetActive(false);
                AdsManager.ExecuteRewardVideoCallback(true);
                AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.RewardedVideo);
            });
        }

        public void ShowRewardedVideo()
        {
            rewardedVideoObject.SetActive(true);
        }

        #endregion

        #region Helpers

        private void CreateButton(Transform parent, string label, Vector2 position, Color color, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject($"Button_{label}");
            buttonObj.transform.SetParent(parent, false);

            RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = position;
            btnRect.sizeDelta = new Vector2(300, 60);

            Image btnImage = buttonObj.AddComponent<Image>();
            btnImage.color = color;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = btnImage;
            button.onClick.AddListener(onClick);

            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = btnTextObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = label;
            btnText.fontSize = 24;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
        }

        #endregion
    }
}
