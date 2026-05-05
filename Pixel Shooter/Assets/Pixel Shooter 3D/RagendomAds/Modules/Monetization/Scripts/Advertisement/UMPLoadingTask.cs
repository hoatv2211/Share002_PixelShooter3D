using UnityEngine;

namespace Ragendom
{
    public class UMPLoadingTask : LoadingTask
    {
        public UMPLoadingTask() : base("UMP Consent")
        {
        }

        protected override void OnTaskActivated()
        {
            AdsSettings settings = Monetization.Settings?.AdsSettings;

            if (settings == null || !settings.IsUMPEnabled)
            {
                CompleteTask(CompleteStatus.Skipped);
                return;
            }

#if MODULE_ADMOB
            // Check if consent is already available
            if (GoogleMobileAds.Ump.Api.ConsentInformation.CanRequestAds())
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[UMP]: Consent already granted, skipping form.");

                CompleteTask(CompleteStatus.Completed);
                return;
            }

            // Build consent request parameters
            var requestParameters = new GoogleMobileAds.Ump.Api.ConsentRequestParameters();

            if (settings.UMPDebugMode)
            {
                var debugSettings = new GoogleMobileAds.Ump.Api.ConsentDebugSettings();

                switch (settings.UMPDebugGeography)
                {
                    case DebugGeography.EEA:
                        debugSettings.DebugGeography = GoogleMobileAds.Ump.Api.DebugGeography.EEA;
                        break;
                    case DebugGeography.NotEEA:
                        debugSettings.DebugGeography = GoogleMobileAds.Ump.Api.DebugGeography.NotEEA;
                        break;
                    default:
                        debugSettings.DebugGeography = GoogleMobileAds.Ump.Api.DebugGeography.Disabled;
                        break;
                }

                if (Monetization.Settings.TestDevices != null && Monetization.Settings.TestDevices.Count > 0)
                {
                    debugSettings.TestDeviceHashedIds = Monetization.Settings.TestDevices;
                }

                requestParameters.ConsentDebugSettings = debugSettings;
            }

            requestParameters.TagForUnderAgeOfConsent = settings.UMPTagForUnderAgeOfConsent;

            // Request consent information update
            GoogleMobileAds.Ump.Api.ConsentInformation.Update(requestParameters, (GoogleMobileAds.Ump.Api.FormError error) =>
            {
                if (error != null)
                {
                    if (Monetization.VerboseLogging)
                        Debug.LogError($"[UMP]: Consent information update failed: {error.Message}");

                    CompleteTask(CompleteStatus.Failed);
                    return;
                }

                // Load and show consent form if required
                GoogleMobileAds.Ump.Api.ConsentForm.LoadAndShowConsentFormIfRequired((GoogleMobileAds.Ump.Api.FormError formError) =>
                {
                    if (formError != null)
                    {
                        if (Monetization.VerboseLogging)
                            Debug.LogError($"[UMP]: Consent form error: {formError.Message}");

                        CompleteTask(CompleteStatus.Failed);
                        return;
                    }

                    if (GoogleMobileAds.Ump.Api.ConsentInformation.CanRequestAds())
                    {
                        if (Monetization.VerboseLogging)
                            Debug.Log("[UMP]: Consent granted.");

                        CompleteTask(CompleteStatus.Completed);
                    }
                    else
                    {
                        if (Monetization.VerboseLogging)
                            Debug.LogWarning("[UMP]: Consent not granted.");

                        CompleteTask(CompleteStatus.Completed);
                    }
                });
            });
#else
            if (Monetization.VerboseLogging)
                Debug.Log("[UMP]: AdMob module not available, skipping UMP.");

            CompleteTask(CompleteStatus.Skipped);
#endif
        }
    }
}
