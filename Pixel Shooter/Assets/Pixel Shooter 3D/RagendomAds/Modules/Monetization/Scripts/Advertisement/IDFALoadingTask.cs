using UnityEngine;
using System.Collections;

namespace Ragendom
{
    public class IDFALoadingTask : LoadingTask
    {
        private string trackingDescription;

        public IDFALoadingTask(string trackingDescription) : base("IDFA Tracking")
        {
            this.trackingDescription = trackingDescription;
        }

        protected override void OnTaskActivated()
        {
#if UNITY_IOS && MODULE_IDFA
            if (AdsManager.IsIDFADetermined())
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[IDFA]: Tracking status already determined.");

                CompleteTask(CompleteStatus.Completed);
                return;
            }

            Unity.Advertisement.IosSupport.ATTrackingStatusBinding.RequestAuthorizationTracking();

            // Start polling coroutine
            CoroutineRunner.StartRoutine(PollIDFAStatus());
#else
            if (Monetization.VerboseLogging)
                Debug.Log("[IDFA]: Not on iOS or IDFA module not available, skipping.");

            CompleteTask(CompleteStatus.Skipped);
#endif
        }

#if UNITY_IOS && MODULE_IDFA
        private IEnumerator PollIDFAStatus()
        {
            float maxWaitTime = 30f;
            float elapsedTime = 0f;

            while (!AdsManager.IsIDFADetermined() && elapsedTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.3f);
                elapsedTime += 0.3f;
            }

            if (AdsManager.IsIDFADetermined())
            {
                if (Monetization.VerboseLogging)
                    Debug.Log($"[IDFA]: Tracking status determined: {AdsManager.GetIDFAStatus()}");

                CompleteTask(CompleteStatus.Completed);
            }
            else
            {
                if (Monetization.VerboseLogging)
                    Debug.LogWarning("[IDFA]: Tracking status polling timed out.");

                CompleteTask(CompleteStatus.Failed);
            }
        }
#endif
    }

    // Simple coroutine runner for non-MonoBehaviour contexts
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner instance;

        private static CoroutineRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("[Coroutine Runner]");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<CoroutineRunner>();
                }
                return instance;
            }
        }

        public static Coroutine StartRoutine(IEnumerator routine)
        {
            return Instance.StartCoroutine(routine);
        }
    }
}
