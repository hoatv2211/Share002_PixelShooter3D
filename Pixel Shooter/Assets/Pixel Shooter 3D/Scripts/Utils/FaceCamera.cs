using UnityEngine;

namespace PixelShooter3D
{
public class FaceCamera : MonoBehaviour
{
    [Tooltip("If true, the object will face away from the camera (often needed for TextMeshPro to be readable).")]
    public bool reverseFace = true;

    [Header("Axis Locking")]
    [Tooltip("Freeze rotation on the X axis (Pitch). Stops tilting up/down.")]
    public bool lockX = false;
    [Tooltip("Freeze rotation on the Y axis (Yaw). Stops turning left/right.")]
    public bool lockY = false;
    [Tooltip("Freeze rotation on the Z axis (Roll). Stops tilting sideways.")]
    public bool lockZ = false;

    [Tooltip("Additional rotation to apply after facing the camera (e.g., 0, 180, 0 to flip manually).")]
    public Vector3 rotationOffset;

    private Camera _mainCamera;

    void Start()
    {
        CacheCamera();
    }

    void LateUpdate()
    {
        // Ensure we have a camera
        if (_mainCamera == null)
        {
            CacheCamera();
            if (_mainCamera == null) return;
        }

        Vector3 direction;

        if (reverseFace)
        {
            // Rotates the object so its forward vector points AWAY from the camera
            direction = transform.position - _mainCamera.transform.position;
        }
        else
        {
            // Rotates the object so its forward vector points AT the camera
            direction = _mainCamera.transform.position - transform.position;
        }

        // Apply rotation only if we have a valid direction
        if (direction != Vector3.zero)
        {
            // Get the base look rotation
            Quaternion baseLookRotation = Quaternion.LookRotation(direction);
            Vector3 targetEuler = baseLookRotation.eulerAngles;

            // Apply Axis Locking
            // We force the axis to 0 (World align) if locked, before applying the offset.
            if (lockX) targetEuler.x = 0;
            if (lockY) targetEuler.y = 0;
            if (lockZ) targetEuler.z = 0;

            // Combine the calculated look rotation with the manual offset
            transform.rotation = Quaternion.Euler(targetEuler) * Quaternion.Euler(rotationOffset);
        }
    }

    private void CacheCamera()
    {
        _mainCamera = Camera.main;
    }
}
}