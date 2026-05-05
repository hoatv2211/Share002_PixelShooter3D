#pragma warning disable 0649

using UnityEngine;

namespace Ragendom
{
    [System.Serializable]
    public class AdSave : ISaveObject
    {
        [SerializeField] bool isForcedAdEnabled = true;

        public bool IsForcedAdEnabled
        {
            get => isForcedAdEnabled;
            set => isForcedAdEnabled = value;
        }

        public void Flush()
        {
            // Intentionally empty
        }
    }
}
