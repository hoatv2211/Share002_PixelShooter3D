using System.Threading.Tasks;
using UnityEngine;

namespace Ragendom
{
    public class AdDummyHandler : AdProviderHandler
    {
        private bool isBannerLoaded;
        private bool isInterstitialLoaded;
        private bool isRewardedVideoLoaded;

        private AdDummyController dummyController;

        public AdDummyHandler(AdProvider providerType) : base(providerType)
        {
        }

        protected override Task<bool> InitProviderAsync()
        {
            dummyController = AdDummyController.CreateObject();
            dummyController.Init(adsSettings);

            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: Dummy ad provider initialized.");

            return Task.FromResult(true);
        }

        #region Banner

        public override void ShowBanner()
        {
            if (dummyController != null)
            {
                dummyController.ShowBanner();
                OnProviderAdLoaded(AdType.Banner);
            }
        }

        public override void HideBanner()
        {
            if (dummyController != null)
                dummyController.HideBanner();
        }

        public override void DestroyBanner()
        {
            if (dummyController != null)
                dummyController.HideBanner();
        }

        #endregion

        #region Interstitial

        public override void RequestInterstitial()
        {
            isInterstitialLoaded = true;
            OnProviderAdLoaded(AdType.Interstitial);

            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: Dummy interstitial ad loaded.");
        }

        public override void ShowInterstitial(AdvertisementCallback callback)
        {
            if (dummyController != null)
            {
                dummyController.ShowInterstitial();
                OnProviderAdDisplayed(AdType.Interstitial);
                isInterstitialLoaded = false;
            }
        }

        public override bool IsInterstitialLoaded()
        {
            return isInterstitialLoaded;
        }

        #endregion

        #region Rewarded Video

        public override void RequestRewardedVideo()
        {
            isRewardedVideoLoaded = true;
            OnProviderAdLoaded(AdType.RewardedVideo);

            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: Dummy rewarded video ad loaded.");
        }

        public override void ShowRewardedVideo(AdvertisementCallback callback)
        {
            if (dummyController != null)
            {
                dummyController.ShowRewardedVideo();
                OnProviderAdDisplayed(AdType.RewardedVideo);
                isRewardedVideoLoaded = false;
            }
        }

        public override bool IsRewardedVideoLoaded()
        {
            return isRewardedVideoLoaded;
        }

        #endregion
    }
}
