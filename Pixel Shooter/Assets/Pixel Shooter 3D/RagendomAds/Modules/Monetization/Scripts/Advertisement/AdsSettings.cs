#pragma warning disable 0649

using UnityEngine;

namespace Ragendom
{
    public class AdsSettings : ScriptableObject
    {
        [SerializeField] AdProvider bannerType = AdProvider.Dummy;
        public AdProvider BannerType => bannerType;

        [SerializeField] AdProvider interstitialType = AdProvider.Dummy;
        public AdProvider InterstitialType => interstitialType;

        [SerializeField] AdProvider rewardedVideoType = AdProvider.Dummy;
        public AdProvider RewardedVideoType => rewardedVideoType;

        [SerializeField] bool loadAdsOnStart = true;
        public bool LoadAdsOnStart => loadAdsOnStart;

        [Header("Interstitial Timing")]
        [SerializeField] float interstitialFirstStartDelay = 40f;
        public float InterstitialFirstStartDelay => interstitialFirstStartDelay;

        [SerializeField] float interstitialStartDelay = 40f;
        public float InterstitialStartDelay => interstitialStartDelay;

        [SerializeField] float interstitialShowingDelay = 30f;
        public float InterstitialShowingDelay => interstitialShowingDelay;

        [SerializeField] bool autoShowInterstitial = false;
        public bool AutoShowInterstitial => autoShowInterstitial;

        [Header("Loading Screen")]
        [SerializeField] float loadingAdDuration = 0f;
        public float LoadingAdDuration => loadingAdDuration;

        [SerializeField] string loadingMessage = "Ad is loading..";
        public string LoadingMessage => loadingMessage;

        [Header("UMP (GDPR)")]
        [SerializeField] bool isUMPEnabled = true;
        public bool IsUMPEnabled => isUMPEnabled;

        [SerializeField] bool umpTagForUnderAgeOfConsent = false;
        public bool UMPTagForUnderAgeOfConsent => umpTagForUnderAgeOfConsent;

        [SerializeField] bool umpDebugMode = false;
        public bool UMPDebugMode => umpDebugMode;

        [SerializeField] DebugGeography umpDebugGeography;
        public DebugGeography UMPDebugGeography => umpDebugGeography;

        [Header("IDFA")]
        [SerializeField] bool isIDFAEnabled = false;
        public bool IsIDFAEnabled => isIDFAEnabled;

        [SerializeField] string trackingDescription = "Your data will be used to deliver personalized ads to you.";
        public string TrackingDescription => trackingDescription;

        [HideInInspector]
        [SerializeField] AdMobContainer adMobContainer;
        public AdMobContainer AdMobContainer => adMobContainer;

        [HideInInspector]
        [SerializeField] AdDummyContainer dummyContainer;
        public AdDummyContainer DummyContainer => dummyContainer;

        public bool IsDummyEnabled()
        {
            return bannerType == AdProvider.Dummy || interstitialType == AdProvider.Dummy || rewardedVideoType == AdProvider.Dummy;
        }

        public bool IsModuleEnabled(AdProvider provider)
        {
            if (provider == AdProvider.Disable)
                return false;

            return bannerType == provider || interstitialType == provider || rewardedVideoType == provider;
        }
    }
}
