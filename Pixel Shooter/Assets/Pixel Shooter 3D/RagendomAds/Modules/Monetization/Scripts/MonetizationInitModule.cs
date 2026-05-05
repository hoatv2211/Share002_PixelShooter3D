using UnityEngine;

namespace Ragendom
{
    public class MonetizationInitModule : MonoBehaviour
    {
        [SerializeField] MonetizationSettings settings;

        private void Awake()
        {
            CreateComponent();
        }

        public void CreateComponent()
        {
            if (settings == null)
            {
                Debug.LogError("[Monetization]: MonetizationSettings is not assigned!");
                return;
            }

            Monetization.Init(settings);
            AdsManager.Init(settings);
        }
    }
}
