using UnityEngine;

namespace PixelShooter3D
{
public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("Settings")]
    public Vector3 defaultPos = new Vector3(0, 20, 15);
    public Vector3 pickerPos = new Vector3(0, 12, 18); // Lower and angled

    private Vector3 targetPos;
    private float moveSpeed = 2.0f;

    void Awake()
    {
        Instance = this;
        targetPos = defaultPos;
    }

    void Update()
    {
        // Smoothly move camera
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSpeed);
    }

    public void SetTarget(bool isPickerActive)
    {
        targetPos = isPickerActive ? pickerPos : defaultPos;
    }
}
}