#pragma warning disable 0649
#pragma warning disable 0414

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ragendom
{
    [Define("MODULE_ADMOB", "GoogleMobileAds.Api.MobileAds")]
    public static class AdsManager
    {
        private const string FIRST_LAUNCH_KEY = "FIRST_LAUNCH";

        public delegate void AdsModuleCallback(AdProvider provider);
        public delegate void AdsEventsCallback(AdProvider provider, AdType adType);
        public delegate bool AdsBoolCallback();

        public static event SimpleCallback ForcedAdDisabled;
        public static event AdsModuleCallback AdProviderInitialized;
        public static event AdsEventsCallback AdLoaded;
        public static event AdsEventsCallback AdDisplayed;
        public static event AdsEventsCallback AdClosed;
        public static AdsBoolCallback InterstitialConditions;

        private static AdProviderHandler[] AD_PROVIDERS;
        private static bool isModuleInitialized;
        private static AdsSettings settings;
        private static MonetizationSettings monetizationSettings;
        private static double lastInterstitialTime;
        private static AdProviderHandler.AdvertisementCallback rewardedVideoCallback;
        private static AdProviderHandler.AdvertisementCallback interstitalCallback;
        private static readonly List<SimpleCallback> mainThreadEvents = new List<SimpleCallback>();
        private static readonly object mainThreadEventsLock = new object();
        private static bool isFirstAdLoaded;
        private static bool waitingForRewardVideoCallback;
        private static bool isBannerActive = true;
        private static Dictionary<AdProvider, AdProviderHandler> advertisingActiveModules = new Dictionary<AdProvider, AdProviderHandler>();
        private static AdSave save;
        private static List<LoadingTask> loadingTasks;
        private static int currentLoadingTaskIndex;

        private static AdEventExecutor eventExecutor;

        public static void Init(MonetizationSettings monetizationSettings)
        {
            if (isModuleInitialized)
            {
                Debug.LogWarning("[AdsManager]: Module is already initialized!");
                return;
            }

            AdsManager.monetizationSettings = monetizationSettings;
            settings = monetizationSettings.AdsSettings;

            // Load save data
            save = LoadAdSave();

            // Get providers
            AD_PROVIDERS = GetProviders();

            // Set initial interstitial delay
            if (!PlayerPrefs.HasKey(FIRST_LAUNCH_KEY))
            {
                lastInterstitialTime = Time.unscaledTime + settings.InterstitialFirstStartDelay;
                PlayerPrefs.SetInt(FIRST_LAUNCH_KEY, 1);
                PlayerPrefs.Save();
            }
            else
            {
                lastInterstitialTime = Time.unscaledTime + settings.InterstitialStartDelay;
            }

            // Create event executor MonoBehaviour
            GameObject executorObject = new GameObject("[Ad Event Executor]");
            Object.DontDestroyOnLoad(executorObject);
            eventExecutor = executorObject.AddComponent<AdEventExecutor>();

            // Link settings and register active modules
            for (int i = 0; i < AD_PROVIDERS.Length; i++)
            {
                AdProviderHandler provider = AD_PROVIDERS[i];

                if (settings.IsModuleEnabled(provider.ProviderType))
                {
                    provider.LinkSettings(monetizationSettings);
                    advertisingActiveModules[provider.ProviderType] = provider;
                }
            }

            // Build loading tasks
            loadingTasks = new List<LoadingTask>();

            if (settings.IsUMPEnabled)
            {
                loadingTasks.Add(new UMPLoadingTask());
            }

#if UNITY_IOS && MODULE_IDFA
            if (settings.IsIDFAEnabled)
            {
                loadingTasks.Add(new IDFALoadingTask(settings.TrackingDescription));
            }
#endif

            if (loadingTasks.Count == 0)
            {
                InitializeModules(settings.LoadAdsOnStart);
            }
            else
            {
                currentLoadingTaskIndex = 0;

                for (int i = 0; i < loadingTasks.Count; i++)
                {
                    loadingTasks[i].OnTaskCompleted += OnLoadingTaskCompleted;
                }

                loadingTasks[0].Activate();
            }

            isModuleInitialized = true;

            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: Module initialized.");
        }

        private static void OnLoadingTaskCompleted(LoadingTask task, LoadingTask.CompleteStatus status)
        {
            currentLoadingTaskIndex++;

            // Loading task callbacks (UMP, IDFA) may fire on background threads.
            // All subsequent work must be marshalled to the Unity main thread.
            CallEventInMainThread(() =>
            {
                if (currentLoadingTaskIndex < loadingTasks.Count)
                {
                    loadingTasks[currentLoadingTaskIndex].Activate();
                }
                else
                {
                    InitializeModules(settings.LoadAdsOnStart);
                }
            });
        }

        private static AdProviderHandler[] GetProviders()
        {
            List<AdProviderHandler> providers = new List<AdProviderHandler>();

            providers.Add(new AdDummyHandler(AdProvider.Dummy));

#if MODULE_ADMOB
            providers.Add(new AdMobHandler(AdProvider.AdMob));
#endif

            return providers.ToArray();
        }

        private static async void InitializeModules(bool loadAds)
        {
            foreach (var kvp in advertisingActiveModules)
            {
                await kvp.Value.InitAsync();
            }

            if (loadAds)
            {
                TryToLoadFirstAds();
            }
        }

        private static void TryToLoadFirstAds()
        {
            if (eventExecutor != null)
                eventExecutor.StartCoroutine(LoadFirstAdsCoroutine());
        }

        private static IEnumerator LoadFirstAdsCoroutine()
        {
            float waitTime = 1f;
            int attempts = 0;

            while (!AreAllProvidersInitialized() && attempts < 10)
            {
                yield return new WaitForSeconds(waitTime);
                waitTime = Mathf.Min(waitTime * 1.5f, 5f);
                attempts++;
            }

            LoadFirstAds();
        }

        private static bool AreAllProvidersInitialized()
        {
            foreach (var kvp in advertisingActiveModules)
            {
                if (!kvp.Value.IsInitialized)
                    return false;
            }
            return true;
        }

        private static void LoadFirstAds()
        {
            if (isFirstAdLoaded)
                return;

            isFirstAdLoaded = true;

            // Request rewarded video (always available regardless of no-ads)
            RequestRewardBasedVideo();

            // Request interstitial and show banner only if forced ads enabled
            if (IsForcedAdEnabled())
            {
                RequestInterstitial();
                ShowBanner();
            }

            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: First ads loaded.");
        }

        #region Update

        public static void Update()
        {
            // Dispatch main thread events
            List<SimpleCallback> eventsToProcess = null;

            lock (mainThreadEventsLock)
            {
                if (mainThreadEvents.Count > 0)
                {
                    eventsToProcess = new List<SimpleCallback>(mainThreadEvents);
                    mainThreadEvents.Clear();
                }
            }

            if (eventsToProcess != null)
            {
                for (int i = 0; i < eventsToProcess.Count; i++)
                {
                    eventsToProcess[i]?.Invoke();
                }
            }

            // Auto-show interstitial
            if (settings != null && settings.AutoShowInterstitial && isModuleInitialized)
            {
                if (lastInterstitialTime < Time.unscaledTime)
                {
                    ShowInterstitial(null);
                    lastInterstitialTime = Time.unscaledTime + settings.InterstitialShowingDelay;
                }
            }
        }

        public static void CallEventInMainThread(SimpleCallback callback)
        {
            if (callback != null)
            {
                lock (mainThreadEventsLock)
                {
                    mainThreadEvents.Add(callback);
                }
            }
        }

        #endregion

        #region Interstitial

        public static bool IsInterstitialLoaded()
        {
            if (!isModuleInitialized || settings == null)
                return false;

            if (settings.InterstitialType == AdProvider.Disable)
                return false;

            if (!advertisingActiveModules.ContainsKey(settings.InterstitialType))
                return false;

            return advertisingActiveModules[settings.InterstitialType].IsInterstitialLoaded();
        }

        public static void RequestInterstitial()
        {
            if (!isModuleInitialized || settings == null)
                return;

            if (settings.InterstitialType == AdProvider.Disable)
                return;

            if (!advertisingActiveModules.ContainsKey(settings.InterstitialType))
                return;

            advertisingActiveModules[settings.InterstitialType].RequestInterstitial();
        }

        public static void ShowInterstitial(AdProviderHandler.AdvertisementCallback callback, bool ignoreConditions = false)
        {
            interstitalCallback = callback;

            if (!isModuleInitialized)
            {
                ExecuteInterstitialCallback(false);
                return;
            }

            if (!IsForcedAdEnabled())
            {
                ExecuteInterstitialCallback(false);
                return;
            }

            if (!Monetization.IsActive)
            {
                ExecuteInterstitialCallback(false);
                return;
            }

            if (settings.InterstitialType == AdProvider.Disable)
            {
                ExecuteInterstitialCallback(false);
                return;
            }

            if (!ignoreConditions && !CheckInterstitialTime())
            {
                ExecuteInterstitialCallback(false);
                return;
            }

            if (!ignoreConditions && !CheckExtraInterstitialCondition())
            {
                ExecuteInterstitialCallback(false);
                return;
            }

            if (!advertisingActiveModules.ContainsKey(settings.InterstitialType))
            {
                ExecuteInterstitialCallback(false);
                return;
            }

            AdProviderHandler provider = advertisingActiveModules[settings.InterstitialType];

            if (!provider.IsInitialized || !provider.IsInterstitialLoaded())
            {
                ExecuteInterstitialCallback(false);
                return;
            }

            if (settings.LoadingAdDuration > 0)
            {
                if (eventExecutor != null)
                    eventExecutor.StartCoroutine(ShowInterstitialWithLoadingCoroutine(provider, callback));
            }
            else
            {
                provider.ShowInterstitial(callback);
            }
        }

        private static IEnumerator ShowInterstitialWithLoadingCoroutine(AdProviderHandler provider, AdProviderHandler.AdvertisementCallback callback)
        {
            // Show loading screen logic can be implemented here
            if (Monetization.VerboseLogging)
                Debug.Log($"[AdsManager]: Loading ad... ({settings.LoadingMessage})");

            yield return new WaitForSeconds(settings.LoadingAdDuration);

            provider.ShowInterstitial(callback);
        }

        public static void ExecuteInterstitialCallback(bool result)
        {
            CallEventInMainThread(() =>
            {
                interstitalCallback?.Invoke(result);
                interstitalCallback = null;
            });
        }

        public static bool CheckInterstitialTime()
        {
            return lastInterstitialTime < Time.unscaledTime;
        }

        public static bool CheckExtraInterstitialCondition()
        {
            if (InterstitialConditions != null)
            {
                System.Delegate[] invocationList = InterstitialConditions.GetInvocationList();
                for (int i = 0; i < invocationList.Length; i++)
                {
                    if (!((AdsBoolCallback)invocationList[i]).Invoke())
                        return false;
                }
            }
            return true;
        }

        public static void ResetInterstitialDelayTime()
        {
            if (settings != null)
                lastInterstitialTime = Time.unscaledTime + settings.InterstitialShowingDelay;
        }

        public static void SetInterstitialDelayTime(float delay)
        {
            lastInterstitialTime = Time.unscaledTime + delay;
        }

        #endregion

        #region Rewarded Video

        public static bool IsRewardBasedVideoLoaded()
        {
            if (!isModuleInitialized || settings == null)
                return false;

            if (settings.RewardedVideoType == AdProvider.Disable)
                return false;

            if (!advertisingActiveModules.ContainsKey(settings.RewardedVideoType))
                return false;

            // NOTE: Does NOT check IsForcedAdEnabled — rewarded videos always available
            return advertisingActiveModules[settings.RewardedVideoType].IsRewardedVideoLoaded();
        }

        public static void RequestRewardBasedVideo()
        {
            if (!isModuleInitialized || settings == null)
                return;

            if (settings.RewardedVideoType == AdProvider.Disable)
                return;

            if (!advertisingActiveModules.ContainsKey(settings.RewardedVideoType))
                return;

            advertisingActiveModules[settings.RewardedVideoType].RequestRewardedVideo();
        }

        public static void ShowRewardBasedVideo(AdProviderHandler.AdvertisementCallback callback, bool showErrorMessage = true)
        {
            rewardedVideoCallback = callback;
            waitingForRewardVideoCallback = true;

            if (!isModuleInitialized)
            {
                ExecuteRewardVideoCallback(false);
                return;
            }

            if (!Monetization.IsActive)
            {
                ExecuteRewardVideoCallback(false);
                return;
            }

            if (settings.RewardedVideoType == AdProvider.Disable)
            {
                ExecuteRewardVideoCallback(false);
                return;
            }

            if (!advertisingActiveModules.ContainsKey(settings.RewardedVideoType))
            {
                ExecuteRewardVideoCallback(false);
                return;
            }

            AdProviderHandler provider = advertisingActiveModules[settings.RewardedVideoType];

            if (!provider.IsInitialized || !provider.IsRewardedVideoLoaded())
            {
                if (showErrorMessage && Monetization.VerboseLogging)
                    Debug.LogWarning("[AdsManager]: Rewarded video is not loaded yet.");

                ExecuteRewardVideoCallback(false);
                return;
            }

            if (settings.LoadingAdDuration > 0)
            {
                if (eventExecutor != null)
                    eventExecutor.StartCoroutine(ShowRewardedVideoWithLoadingCoroutine(provider, callback));
            }
            else
            {
                provider.ShowRewardedVideo(callback);
            }
        }

        private static IEnumerator ShowRewardedVideoWithLoadingCoroutine(AdProviderHandler provider, AdProviderHandler.AdvertisementCallback callback)
        {
            if (Monetization.VerboseLogging)
                Debug.Log($"[AdsManager]: Loading ad... ({settings.LoadingMessage})");

            yield return new WaitForSeconds(settings.LoadingAdDuration);

            provider.ShowRewardedVideo(callback);
        }

        public static void ExecuteRewardVideoCallback(bool result)
        {
            if (!waitingForRewardVideoCallback)
                return;

            waitingForRewardVideoCallback = false;

            CallEventInMainThread(() =>
            {
                rewardedVideoCallback?.Invoke(result);
                rewardedVideoCallback = null;
            });
        }

        #endregion

        #region Banner

        public static void ShowBanner()
        {
            if (!isModuleInitialized || settings == null)
                return;

            if (!IsForcedAdEnabled())
                return;

            if (!isBannerActive)
                return;

            if (settings.BannerType == AdProvider.Disable)
                return;

            if (!advertisingActiveModules.ContainsKey(settings.BannerType))
                return;

            advertisingActiveModules[settings.BannerType].ShowBanner();
        }

        public static void HideBanner()
        {
            if (!isModuleInitialized || settings == null)
                return;

            if (settings.BannerType == AdProvider.Disable)
                return;

            if (!advertisingActiveModules.ContainsKey(settings.BannerType))
                return;

            advertisingActiveModules[settings.BannerType].HideBanner();
        }

        public static void DestroyBanner()
        {
            if (!isModuleInitialized || settings == null)
                return;

            if (settings.BannerType == AdProvider.Disable)
                return;

            if (!advertisingActiveModules.ContainsKey(settings.BannerType))
                return;

            advertisingActiveModules[settings.BannerType].DestroyBanner();
        }

        public static void EnableBanner()
        {
            isBannerActive = true;
            ShowBanner();
        }

        public static void DisableBanner()
        {
            isBannerActive = false;
            HideBanner();
        }

        #endregion

        #region Forced Ad / No-Ads

        public static bool IsForcedAdEnabled()
        {
            return save != null && save.IsForcedAdEnabled;
        }

        public static void DisableForcedAd()
        {
            if (save != null)
            {
                save.IsForcedAdEnabled = false;
                SaveAdData();
            }

            ForcedAdDisabled?.Invoke();
            DestroyBanner();

            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: Forced ads disabled (no-ads purchased).");
        }

        #endregion

        #region UMP / GDPR

        public static bool CanRequestAds()
        {
#if MODULE_ADMOB
            return GoogleMobileAds.Ump.Api.ConsentInformation.CanRequestAds();
#else
            return true;
#endif
        }

        public static ConsentRequirementStatus GetConsentStatus()
        {
#if MODULE_ADMOB
            var status = GoogleMobileAds.Ump.Api.ConsentInformation.PrivacyOptionsRequirementStatus;
            switch (status)
            {
                case GoogleMobileAds.Ump.Api.PrivacyOptionsRequirementStatus.NotRequired:
                    return ConsentRequirementStatus.NotRequired;
                case GoogleMobileAds.Ump.Api.PrivacyOptionsRequirementStatus.Required:
                    return ConsentRequirementStatus.Required;
                default:
                    return ConsentRequirementStatus.Unknown;
            }
#else
            return ConsentRequirementStatus.Unknown;
#endif
        }

        public static void ResetConsentState()
        {
#if MODULE_ADMOB
            GoogleMobileAds.Ump.Api.ConsentInformation.Reset();

            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: UMP consent state reset.");
#endif
        }

        #endregion

        #region IDFA

#if UNITY_IOS && MODULE_IDFA
        public static AuthorizationTrackingStatus GetIDFAStatus()
        {
            var status = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
            return (AuthorizationTrackingStatus)(int)status;
        }

        public static bool IsIDFADetermined()
        {
            return GetIDFAStatus() != AuthorizationTrackingStatus.NOT_DETERMINED;
        }
#endif

        #endregion

        #region Static Event Methods

        public static void OnProviderInitialized(AdProvider provider)
        {
            CallEventInMainThread(() =>
            {
                AdProviderInitialized?.Invoke(provider);

                if (Monetization.VerboseLogging)
                    Debug.Log($"[AdsManager]: Provider initialized: {provider}");
            });
        }

        public static void OnProviderAdLoaded(AdProvider provider, AdType adType)
        {
            CallEventInMainThread(() =>
            {
                AdLoaded?.Invoke(provider, adType);

                if (Monetization.VerboseLogging)
                    Debug.Log($"[AdsManager]: Ad loaded: {provider} {adType}");
            });
        }

        public static void OnProviderAdDisplayed(AdProvider provider, AdType adType)
        {
            CallEventInMainThread(() =>
            {
                AdDisplayed?.Invoke(provider, adType);

                if (adType == AdType.Interstitial || adType == AdType.RewardedVideo)
                    ResetInterstitialDelayTime();

                if (Monetization.VerboseLogging)
                    Debug.Log($"[AdsManager]: Ad displayed: {provider} {adType}");
            });
        }

        public static void OnProviderAdClosed(AdProvider provider, AdType adType)
        {
            CallEventInMainThread(() =>
            {
                AdClosed?.Invoke(provider, adType);

                if (adType == AdType.Interstitial || adType == AdType.RewardedVideo)
                    ResetInterstitialDelayTime();

                if (Monetization.VerboseLogging)
                    Debug.Log($"[AdsManager]: Ad closed: {provider} {adType}");
            });
        }

        #endregion

        #region Save System

        private static AdSave LoadAdSave()
        {
            string json = PlayerPrefs.GetString("ADS_SAVE", "");

            if (string.IsNullOrEmpty(json))
                return new AdSave();

            return JsonUtility.FromJson<AdSave>(json) ?? new AdSave();
        }

        private static void SaveAdData()
        {
            if (save != null)
            {
                string json = JsonUtility.ToJson(save);
                PlayerPrefs.SetString("ADS_SAVE", json);
                PlayerPrefs.Save();
            }
        }

        #endregion

        #region Inner Classes

        private class AdEventExecutor : MonoBehaviour
        {
            private void Update()
            {
                AdsManager.Update();
            }
        }

        #endregion
    }
}
