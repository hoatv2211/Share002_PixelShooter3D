using UnityEngine;
using UnityEngine.UI;

namespace PixelShooter3D
{
    [RequireComponent(typeof(Button))]
    public class DeveloperModeButton : MonoBehaviour
    {
        [Tooltip("The panel to toggle. If empty, auto-finds child named 'DeveloperSettingsPanel'.")]
        public GameObject developerSettingsPanel;

        void Start()
        {
            if (developerSettingsPanel == null)
            {
                Transform child = transform.Find("DeveloperSettingsPanel");
                if (child != null)
                    developerSettingsPanel = child.gameObject;
            }

            if (developerSettingsPanel != null)
                developerSettingsPanel.SetActive(false);

            GetComponent<Button>().onClick.AddListener(TogglePanel);
        }

        void TogglePanel()
        {
            if (developerSettingsPanel != null)
                developerSettingsPanel.SetActive(!developerSettingsPanel.activeSelf);
        }
    }
}
