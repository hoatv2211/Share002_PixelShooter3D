using UnityEngine;

namespace PixelShooter3D
{
public class BulletController : MonoBehaviour
{
    private Transform target;
    private int colorCode;
    private float speed = 25f; // Fast bullet speed
    private bool hasHit = false;

    public void Initialize(Transform targetTransform, int cCode)
    {
        target = targetTransform;
        colorCode = cCode;
    }

    void Update()
    {
        // If target disappeared mid-air, destroy bullet
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Move towards target
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        transform.LookAt(target);

        // Hit Check
        if (Vector3.Distance(transform.position, target.position) < 0.2f && !hasHit)
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        hasHit = true;

        BlockController block = target.GetComponent<BlockController>();
        if (block != null)
        {
            // FIX: Call DestroyBlock() instead of TakeDamage()
            block.DestroyBlock();

            // Sound: Use the 'pop' clip for impact
            if (SoundManager.Instance) SoundManager.Instance.PlayPigSelect();
        }

        Destroy(gameObject);
    }
}
}