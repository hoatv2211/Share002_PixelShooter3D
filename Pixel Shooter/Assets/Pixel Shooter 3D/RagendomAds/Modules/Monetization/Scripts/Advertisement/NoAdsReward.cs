using UnityEngine;

namespace Ragendom
{
    public class NoAdsReward : MonoBehaviour
    {
        [SerializeField] GameObject noAdsOfferObject;

        private void OnEnable()
        {
            AdsManager.ForcedAdDisabled += OnForcedAdDisabled;

            if (CheckDisableState())
            {
                HideOffer();
            }
        }

        private void OnDisable()
        {
            AdsManager.ForcedAdDisabled -= OnForcedAdDisabled;
        }

        public void ApplyReward()
        {
            AdsManager.DisableForcedAd();
        }

        public bool CheckDisableState()
        {
            return !AdsManager.IsForcedAdEnabled();
        }

        private void OnForcedAdDisabled()
        {
            HideOffer();
        }

        private void HideOffer()
        {
            if (noAdsOfferObject != null)
                noAdsOfferObject.SetActive(false);
        }
    }
}
