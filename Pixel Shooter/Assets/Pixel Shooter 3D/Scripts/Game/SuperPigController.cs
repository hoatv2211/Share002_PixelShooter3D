using UnityEngine;
using System.Collections;

namespace PixelShooter3D
{
public class SuperPigController : MonoBehaviour
{
    [Header("Settings")]
    public int ammo = 25;
    public float fireRate = 0.08f;

    // Movement handled dynamically via GameManager settings
    private float lastFireTime;
    private Vector3 fightPosition;
    private Vector3 exitPosition;

    void Start()
    {
        if (GameManager.Instance == null) return;

        // 1. Get Positions directly from GameManager Transforms
        Transform startT = GameManager.Instance.superPigStartPoint;
        Transform fightT = GameManager.Instance.superPigFightPoint;

        // Default calculations if transforms are missing
        Vector3 spawnPos = transform.position;
        Vector3 targetPos = transform.position;

        if (startT != null) spawnPos = startT.position;
        if (fightT != null) targetPos = fightT.position;

        // Save for later
        fightPosition = targetPos;
        exitPosition = spawnPos;

        // 2. Force Start Position
        transform.position = spawnPos;

        // 3. Fly In (Start -> Fight)
        StartCoroutine(FlyRoutine(fightPosition, 1.5f, true));
    }

    void Update()
    {
        // Hover logic (check distance to fight position)
        if (Vector3.Distance(transform.position, fightPosition) < 0.5f && ammo > 0)
        {
            // Bobbing animation around the fight position
            float bobY = fightPosition.y + Mathf.Sin(Time.time * 2f) * 0.25f;
            transform.position = new Vector3(fightPosition.x, bobY, fightPosition.z);

            if (Time.time - lastFireTime > fireRate && ammo > 0)
            {
                Fire();
            }
        }

        if (ammo <= 0 && ammo != 999)
        {
            // Fly Away (Fight -> Exit)
            StartCoroutine(FlyRoutine(exitPosition, 1.5f, false));
            ammo = 999; // Prevent re-triggering
        }
    }

    void Fire()
    {
        if (GameManager.Instance.activeBlocks.Count == 0) return;

        // Find valid target
        int rnd = Random.Range(0, GameManager.Instance.activeBlocks.Count);
        BlockController target = GameManager.Instance.activeBlocks[rnd];

        if (target != null && !target.isDying && !target.isReserved)
        {
            target.isReserved = true;

            GameObject b = Instantiate(GameManager.Instance.bulletPrefab, transform.position, Quaternion.identity);

            // Visual: Make bullet Gold
            Renderer r = b.GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = new Color(1f, 0.84f, 0f); // Gold

            // Logic
            BulletController bulletScript = b.GetComponent<BulletController>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(target.transform, 99);
            }

            // Audio
            if (SoundManager.Instance) SoundManager.Instance.PlayShoot();

            ammo--;
            lastFireTime = Time.time;
        }
    }

    IEnumerator FlyRoutine(Vector3 targetPos, float duration, bool isIntro)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Smooth step easing
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;

        if (!isIntro)
        {
            Destroy(gameObject);
        }
    }
}
}