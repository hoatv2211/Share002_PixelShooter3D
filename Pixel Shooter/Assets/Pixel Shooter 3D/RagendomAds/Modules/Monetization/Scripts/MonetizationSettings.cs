#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace Ragendom
{
    public class MonetizationSettings : ScriptableObject
    {
        [SerializeField] AdsSettings adsSettings;
        public AdsSettings AdsSettings => adsSettings;

        [SerializeField] bool isModuleActive = true;
        public bool IsModuleActive => isModuleActive;

        [SerializeField] bool verboseLogging = false;
        public bool VerboseLogging => verboseLogging;

        [SerializeField] bool debugMode = false;
        public bool DebugMode => debugMode;

        [SerializeField] List<string> testDevices = new List<string>();
        public List<string> TestDevices => testDevices;

        [SerializeField] string privacyLink = "";
        public string PrivacyLink => privacyLink;

        [SerializeField] string termsOfUseLink = "";
        public string TermsOfUseLink => termsOfUseLink;
    }
}
