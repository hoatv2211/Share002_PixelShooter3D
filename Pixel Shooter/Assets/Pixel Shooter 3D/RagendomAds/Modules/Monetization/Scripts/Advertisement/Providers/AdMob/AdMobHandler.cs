#if MODULE_ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Ragendom
{
    public class AdMobHandler : AdProviderHandler
    {
        private BannerView bannerView;
        private InterstitialAd interstitial;
        private RewardedAd rewardBasedVideo;
        private AppOpenAd appOpenAd;
        private DateTime appOpenAdExpireTime;
        private bool appOpenAdCanShow = true;

        private AdvertisementCallback interstitialCallback;
        private AdvertisementCallback rewardedVideoCallback;

        private AdMobContainer config;

        public AdMobHandler(AdProvider providerType) : base(providerType)
        {
        }

        protected override Task<bool> InitProviderAsync()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            config = adsSettings.AdMobContainer;

            MobileAds.SetiOSAppPauseOnBackground(true);

            RequestConfiguration requestConfiguration = new RequestConfiguration();
            requestConfiguration.TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified;
            requestConfiguration.TestDeviceIds = monetizationSettings.TestDevices;

            MobileAds.SetRequestConfiguration(requestConfiguration);

            MobileAds.Initialize(initStatus =>
            {
                AdsManager.CallEventInMainThread(() =>
                {
                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: AdMob SDK initialized successfully.");

                    if (config.UseAppOpenAd)
                    {
                        LoadAppOpenAd();
                        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
                    }

                    tcs.SetResult(true);
                });
            });

            return tcs.Task;
        }

        #region Banner

        public override void ShowBanner()
        {
            if (bannerView == null)
            {
                RequestBanner();
            }

            bannerView?.Show();
        }

        private void RequestBanner()
        {
            DestroyBanner();

            bannerView = new BannerView(GetBannerID(), GetAdSize(), GetAdPosition());

            bannerView.OnBannerAdLoaded += () =>
            {
                AdsManager.CallEventInMainThread(() =>
                {
                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: AdMob banner ad loaded.");

                    OnProviderAdLoaded(AdType.Banner);
                });
            };

            bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                AdsManager.CallEventInMainThread(() =>
                {
                    if (Monetization.VerboseLogging)
                        Debug.LogWarning($"[AdsManager]: AdMob banner ad failed to load: {error.GetMessage()}");
                });
            };

            bannerView.OnAdPaid += (AdValue adValue) =>
            {
                AdsManager.CallEventInMainThread(() =>
                {
                    if (Monetization.VerboseLogging)
                        Debug.Log($"[AdsManager]: AdMob banner ad paid: {adValue.Value} {adValue.CurrencyCode}");
                });
            };

            bannerView.OnAdClicked += () =>
            {
                AdsManager.CallEventInMainThread(() =>
                {
                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: AdMob banner ad clicked.");
                });
            };

            bannerView.OnAdFullScreenContentClosed += () =>
            {
                AdsManager.CallEventInMainThread(() =>
                {
                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: AdMob banner ad full screen content closed.");
                });
            };

            bannerView.LoadAd(GetAdRequest());
        }

        public override void HideBanner()
        {
            bannerView?.Hide();
        }

        public override void DestroyBanner()
        {
            if (bannerView != null)
            {
                bannerView.Destroy();
                bannerView = null;
            }
        }

        private AdSize GetAdSize()
        {
            switch (config.BannerType)
            {
                case AdMobContainer.BannerPlacementType.Banner:
                    return AdSize.Banner;
                case AdMobContainer.BannerPlacementType.MediumRectangle:
                    return AdSize.MediumRectangle;
                case AdMobContainer.BannerPlacementType.IABBanner:
                    return AdSize.IABBanner;
                case AdMobContainer.BannerPlacementType.Leaderboard:
                    return AdSize.Leaderboard;
                default:
                    return AdSize.Banner;
            }
        }

        private AdPosition GetAdPosition()
        {
            switch (config.BannerPosition)
            {
                case BannerPosition.Top:
                    return AdPosition.Top;
                case BannerPosition.Bottom:
                    return AdPosition.Bottom;
                default:
                    return AdPosition.Bottom;
            }
        }

        private string GetBannerID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return config.AndroidBannerID;
#elif UNITY_IOS
            return config.IOSBannerID;
#else
            return "unexpected_platform";
#endif
        }

        #endregion

        #region Interstitial

        public override void RequestInterstitial()
        {
            if (interstitial != null)
            {
                interstitial.Destroy();
                interstitial = null;
            }

            InterstitialAd.Load(GetInterstitialID(), GetAdRequest(), (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    AdsManager.CallEventInMainThread(() =>
                    {
                        int retryAttempt = interstitialRetryAttempt;
                        HandleAdLoadFailure(AdType.Interstitial, error?.GetMessage() ?? "Unknown error", ref interstitialRetryAttempt, RequestInterstitial);
                    });
                    return;
                }

                AdsManager.CallEventInMainThread(() =>
                {
                    interstitial = ad;
                    interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: AdMob interstitial ad loaded.");

                    OnProviderAdLoaded(AdType.Interstitial);

                    interstitial.OnAdFullScreenContentOpened += () =>
                    {
                        AdsManager.CallEventInMainThread(() =>
                        {
                            appOpenAdCanShow = false;
                            OnProviderAdDisplayed(AdType.Interstitial);
                        });
                    };

                    interstitial.OnAdFullScreenContentClosed += () =>
                    {
                        AdsManager.CallEventInMainThread(() =>
                        {
                            appOpenAdCanShow = false;
                            OnProviderAdClosed(AdType.Interstitial);
                            ExecuteInterstitialCallback(true);
                            AdsManager.ResetInterstitialDelayTime();
                            RequestInterstitial();
                        });
                    };

                    interstitial.OnAdFullScreenContentFailed += (AdError adError) =>
                    {
                        AdsManager.CallEventInMainThread(() =>
                        {
                            if (Monetization.VerboseLogging)
                                Debug.LogError($"[AdsManager]: AdMob interstitial ad failed to show: {adError.GetMessage()}");

                            ExecuteInterstitialCallback(false);
                            RequestInterstitial();
                        });
                    };
                });
            });
        }

        public override void ShowInterstitial(AdvertisementCallback callback)
        {
            interstitialCallback = callback;
            interstitial?.Show();
        }

        public override bool IsInterstitialLoaded()
        {
            return interstitial != null && interstitial.CanShowAd();
        }

        private void ExecuteInterstitialCallback(bool result)
        {
            AdsManager.ExecuteInterstitialCallback(result);
        }

        private string GetInterstitialID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return config.AndroidInterstitialID;
#elif UNITY_IOS
            return config.IOSInterstitialID;
#else
            return "unexpected_platform";
#endif
        }

        #endregion

        #region Rewarded Video

        public override void RequestRewardedVideo()
        {
            if (rewardBasedVideo != null)
            {
                rewardBasedVideo.Destroy();
                rewardBasedVideo = null;
            }

            RewardedAd.Load(GetRewardedVideoID(), GetAdRequest(), (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    AdsManager.CallEventInMainThread(() =>
                    {
                        HandleAdLoadFailure(AdType.RewardedVideo, error?.GetMessage() ?? "Unknown error", ref rewardedRetryAttempt, RequestRewardedVideo);
                    });
                    return;
                }

                AdsManager.CallEventInMainThread(() =>
                {
                    rewardBasedVideo = ad;
                    rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: AdMob rewarded video ad loaded.");

                    OnProviderAdLoaded(AdType.RewardedVideo);

                    rewardBasedVideo.OnAdFullScreenContentOpened += () =>
                    {
                        AdsManager.CallEventInMainThread(() =>
                        {
                            appOpenAdCanShow = false;
                        });
                    };

                    rewardBasedVideo.OnAdFullScreenContentClosed += () =>
                    {
                        AdsManager.CallEventInMainThread(() =>
                        {
                            appOpenAdCanShow = false;
                            OnProviderAdClosed(AdType.RewardedVideo);
                            AdsManager.ExecuteRewardVideoCallback(false);
                            RequestRewardedVideo();
                        });
                    };

                    rewardBasedVideo.OnAdFullScreenContentFailed += (AdError adError) =>
                    {
                        AdsManager.CallEventInMainThread(() =>
                        {
                            if (Monetization.VerboseLogging)
                                Debug.LogError($"[AdsManager]: AdMob rewarded video ad failed to show: {adError.GetMessage()}");

                            AdsManager.ExecuteRewardVideoCallback(false);
                            HandleAdLoadFailure(AdType.RewardedVideo, adError.GetMessage(), ref rewardedRetryAttempt, RequestRewardedVideo);
                        });
                    };
                });
            });
        }

        public override void ShowRewardedVideo(AdvertisementCallback callback)
        {
            rewardedVideoCallback = callback;

            rewardBasedVideo?.Show((Reward reward) =>
            {
                AdsManager.CallEventInMainThread(() =>
                {
                    OnProviderAdDisplayed(AdType.RewardedVideo);
                    AdsManager.ExecuteRewardVideoCallback(true);
                    AdsManager.ResetInterstitialDelayTime();
                });
            });
        }

        public override bool IsRewardedVideoLoaded()
        {
            return rewardBasedVideo != null && rewardBasedVideo.CanShowAd();
        }

        private string GetRewardedVideoID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return config.AndroidRewardedVideoID;
#elif UNITY_IOS
            return config.IOSRewardedVideoID;
#else
            return "unexpected_platform";
#endif
        }

        #endregion

        #region App Open Ad

        private void LoadAppOpenAd()
        {
            if (appOpenAd != null)
            {
                appOpenAd.Destroy();
                appOpenAd = null;
            }

            AppOpenAd.Load(GetAppOpenID(), GetAdRequest(), (AppOpenAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    AdsManager.CallEventInMainThread(() =>
                    {
                        if (Monetization.VerboseLogging)
                            Debug.LogWarning($"[AdsManager]: AdMob app open ad failed to load: {error?.GetMessage()}");
                    });
                    return;
                }

                AdsManager.CallEventInMainThread(() =>
                {
                    appOpenAd = ad;
                    appOpenAdExpireTime = DateTime.Now + TimeSpan.FromHours(config.AppOpenAdExpirationHoursTime);

                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: AdMob app open ad loaded.");

                    appOpenAd.OnAdFullScreenContentClosed += () =>
                    {
                        AdsManager.CallEventInMainThread(() =>
                        {
                            if (Monetization.VerboseLogging)
                                Debug.Log("[AdsManager]: AdMob app open ad closed.");

                            LoadAppOpenAd();
                            AdsManager.EnableBanner();
                        });
                    };

                    appOpenAd.OnAdFullScreenContentFailed += (AdError adError) =>
                    {
                        AdsManager.CallEventInMainThread(() =>
                        {
                            if (Monetization.VerboseLogging)
                                Debug.LogError($"[AdsManager]: AdMob app open ad failed to show: {adError.GetMessage()}");

                            LoadAppOpenAd();
                            AdsManager.EnableBanner();
                        });
                    };
                });
            });
        }

        private bool IsAppOpenAdAvailable()
        {
            return appOpenAd != null && appOpenAd.CanShowAd() && DateTime.Now < appOpenAdExpireTime;
        }

        private void OnAppStateChanged(AppState state)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (state == AppState.Foreground)
                {
                    if (appOpenAdCanShow && IsAppOpenAdAvailable())
                    {
                        appOpenAd.Show();
                        AdsManager.DisableBanner();
                    }

                    appOpenAdCanShow = true;
                }
                else if (state == AppState.Background)
                {
                    appOpenAdCanShow = true;
                }
            });
        }

        private string GetAppOpenID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return config.AndroidAppOpenAdID;
#elif UNITY_IOS
            return config.IOSAppOpenAdID;
#else
            return "unexpected_platform";
#endif
        }

        #endregion

        private AdRequest GetAdRequest()
        {
            return new AdRequest();
        }
    }
}
#endif
