using UnityEngine;
using UnityEngine.UI;

namespace PixelShooter3D
{
public class CameraFocusController : MonoBehaviour
{
    [Header("Targets")]
    public Transform playersTarget;
    
    [Header("UI to Enable")]
    public GameObject playersUI;

    [Header("Settings")]
    public float moveSpeed = 5f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    public void FocusOnPlayers()
    {
        if (playersTarget)
        {
            targetPosition = playersTarget.position;
            isMoving = true;
        }

        if (playersUI)
        {
            playersUI.SetActive(true);
        }
    }
}
}
