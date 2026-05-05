#pragma warning disable 0649
#pragma warning disable 0414

using UnityEngine;

namespace Ragendom
{
    [System.Serializable]
    public class AdMobContainer
    {
        public static readonly string ANDROID_BANNER_TEST_ID = "ca-app-pub-3940256099942544/6300978111";
        public static readonly string IOS_BANNER_TEST_ID = "ca-app-pub-3940256099942544/2934735716";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "ca-app-pub-3940256099942544/1033173712";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "ca-app-pub-3940256099942544/4411468910";
        public static readonly string ANDROID_REWARDED_VIDEO_TEST_ID = "ca-app-pub-3940256099942544/5224354917";
        public static readonly string IOS_REWARDED_VIDEO_TEST_ID = "ca-app-pub-3940256099942544/1712485313";
        public static readonly string ANDROID_OPEN_TEST_ID = "ca-app-pub-3940256099942544/9257395921";
        public static readonly string IOS_OPEN_TEST_ID = "ca-app-pub-3940256099942544/5575463023";

        public static readonly string TEST_APP_ID = "ca-app-pub-3940256099942544~3347511713";

        public enum BannerPlacementType
        {
            Banner = 0,
            MediumRectangle = 1,
            IABBanner = 2,
            Leaderboard = 3
        }

        [SerializeField] string androidAppId = "ca-app-pub-3940256099942544~3347511713";
        public string AndroidAppId => androidAppId;

        [SerializeField] string iosAppId = "ca-app-pub-3940256099942544~3347511713";
        public string IOSAppId => iosAppId;

        [SerializeField] string androidBannerID = "ca-app-pub-3940256099942544/6300978111";
        public string AndroidBannerID => androidBannerID;

        [SerializeField] string iOSBannerID = "ca-app-pub-3940256099942544/2934735716";
        public string IOSBannerID => iOSBannerID;

        [SerializeField] string androidInterstitialID = "ca-app-pub-3940256099942544/1033173712";
        public string AndroidInterstitialID => androidInterstitialID;

        [SerializeField] string iOSInterstitialID = "ca-app-pub-3940256099942544/4411468910";
        public string IOSInterstitialID => iOSInterstitialID;

        [SerializeField] string androidRewardedVideoID = "ca-app-pub-3940256099942544/5224354917";
        public string AndroidRewardedVideoID => androidRewardedVideoID;

        [SerializeField] string iOSRewardedVideoID = "ca-app-pub-3940256099942544/1712485313";
        public string IOSRewardedVideoID => iOSRewardedVideoID;

        [SerializeField] bool useAppOpenAd = false;
        public bool UseAppOpenAd => useAppOpenAd;

        [SerializeField] string androidAppOpenAdID = "ca-app-pub-3940256099942544/9257395921";
        public string AndroidAppOpenAdID => androidAppOpenAdID;

        [SerializeField] string iOSAppOpenAdID = "ca-app-pub-3940256099942544/5575463023";
        public string IOSAppOpenAdID => iOSAppOpenAdID;

        [SerializeField] int appOpenAdExpirationHoursTime = 4;
        public int AppOpenAdExpirationHoursTime => appOpenAdExpirationHoursTime;

        [SerializeField] BannerPlacementType bannerType = BannerPlacementType.Banner;
        public BannerPlacementType BannerType => bannerType;

        [SerializeField] BannerPosition bannerPosition = BannerPosition.Bottom;
        public BannerPosition BannerPosition => bannerPosition;
    }
}
