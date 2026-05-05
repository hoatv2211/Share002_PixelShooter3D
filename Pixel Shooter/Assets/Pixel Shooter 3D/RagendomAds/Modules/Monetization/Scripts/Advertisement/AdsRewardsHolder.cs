using UnityEngine;
using UnityEngine.UI;

namespace Ragendom
{
    public class AdsRewardsHolder : MonoBehaviour
    {
        [SerializeField] Button rewardButton;
        [SerializeField] string rewardId = "default_reward";

        private void OnEnable()
        {
            if (rewardButton != null)
                rewardButton.onClick.AddListener(OnRewardButtonClicked);
        }

        private void OnDisable()
        {
            if (rewardButton != null)
                rewardButton.onClick.RemoveListener(OnRewardButtonClicked);
        }

        private void OnRewardButtonClicked()
        {
            AdsManager.ShowRewardBasedVideo((bool result) =>
            {
                if (result)
                {
                    ApplyReward();
                }
                else
                {
                    if (Monetization.VerboseLogging)
                        Debug.Log($"[AdsRewardsHolder]: Reward '{rewardId}' was not granted.");
                }
            });
        }

        protected virtual void ApplyReward()
        {
            if (Monetization.VerboseLogging)
                Debug.Log($"[AdsRewardsHolder]: Reward '{rewardId}' granted!");
        }
    }
}
