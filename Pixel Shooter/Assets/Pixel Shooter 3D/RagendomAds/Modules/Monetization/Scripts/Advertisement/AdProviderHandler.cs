using System.Threading.Tasks;
using UnityEngine;

namespace Ragendom
{
    public abstract class AdProviderHandler
    {
        public delegate void AdvertisementCallback(bool result);

        protected const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;
        protected const int MAX_RETRY_ATTEMPTS = 5;

        protected int interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        protected int rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

        protected AdProvider providerType;
        protected MonetizationSettings monetizationSettings;
        protected AdsSettings adsSettings;

        public AdProvider ProviderType => providerType;
        public bool IsInitialized { get; private set; }

        public AdProviderHandler(AdProvider providerType)
        {
            this.providerType = providerType;
        }

        public void LinkSettings(MonetizationSettings monetizationSettings)
        {
            this.monetizationSettings = monetizationSettings;
            this.adsSettings = monetizationSettings.AdsSettings;
        }

        public async Task<bool> InitAsync()
        {
            bool result = await InitProviderAsync();

            if (result)
            {
                IsInitialized = true;
                AdsManager.OnProviderInitialized(providerType);

                if (Monetization.VerboseLogging)
                    Debug.Log($"[AdsManager]: {providerType} provider initialized successfully.");
            }
            else
            {
                if (Monetization.VerboseLogging)
                    Debug.LogWarning($"[AdsManager]: {providerType} provider failed to initialize.");
            }

            return result;
        }

        protected abstract Task<bool> InitProviderAsync();

        // Banner
        public abstract void ShowBanner();
        public abstract void HideBanner();
        public abstract void DestroyBanner();

        // Interstitial
        public abstract void RequestInterstitial();
        public abstract void ShowInterstitial(AdvertisementCallback callback);
        public abstract bool IsInterstitialLoaded();

        // Rewarded Video
        public abstract void RequestRewardedVideo();
        public abstract void ShowRewardedVideo(AdvertisementCallback callback);
        public abstract bool IsRewardedVideoLoaded();

        protected void HandleAdLoadFailure(AdType adType, string errorMessage, ref int retryAttempt, SimpleCallback retryAction)
        {
            if (retryAttempt <= MAX_RETRY_ATTEMPTS)
            {
                float delay = Mathf.Pow(2, retryAttempt);

                if (Monetization.VerboseLogging)
                    Debug.LogWarning($"[AdsManager]: {providerType} {adType} ad failed to load: {errorMessage}. Retrying in {delay}s (attempt {retryAttempt}/{MAX_RETRY_ATTEMPTS})");

                retryAttempt++;

                int delayMs = (int)(delay * 1000);
                Task.Delay(delayMs).ContinueWith(_ =>
                {
                    AdsManager.CallEventInMainThread(retryAction);
                });
            }
            else
            {
                if (Monetization.VerboseLogging)
                    Debug.LogError($"[AdsManager]: {providerType} {adType} ad failed to load after {MAX_RETRY_ATTEMPTS} attempts: {errorMessage}");
            }
        }

        protected void OnProviderAdLoaded(AdType adType)
        {
            AdsManager.OnProviderAdLoaded(providerType, adType);
        }

        protected void OnProviderAdDisplayed(AdType adType)
        {
            AdsManager.OnProviderAdDisplayed(providerType, adType);
        }

        protected void OnProviderAdClosed(AdType adType)
        {
            AdsManager.OnProviderAdClosed(providerType, adType);
        }
    }
}
