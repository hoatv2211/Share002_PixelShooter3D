using UnityEngine;
using UnityEngine.UI;

namespace PixelShooter3D
{
public class LevelEditorCell : MonoBehaviour
{
    public int x;
    public int y;
    public Image icon;
    private Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
        if (icon == null) icon = GetComponent<Image>();
    }

    void OnClick()
    {
        LevelEditorManager.Instance.OnCellClicked(this);
    }

    public void SetColor(Color c)
    {
        icon.color = c;
    }
}
}