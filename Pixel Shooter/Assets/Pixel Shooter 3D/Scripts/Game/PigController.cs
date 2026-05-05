using System.Collections;
using UnityEngine;
using TMPro;

namespace PixelShooter3D
{
public enum PigState { Deck, Jumping, OnBelt, Returning, Holding, JumpingToHolding }

public class PigController : MonoBehaviour
{
    public int colorCode;
    public int ammo;
    public int colIndex;
    public int rowIndex;
    public PigState state = PigState.Deck;
    public int holdingIndex = -1;

    [Header("Settings")]
    public float fireRate = 0.12f;
    [Tooltip("Controls how fast the pig moves along the conveyor path. Default is around 0.12.")]
    public float beltSpeed = 0.12f;

    private float lastFireTime;
    private float jumpDuration = 0.6f;
    private int pendingHoldingIndex = -1; // Target slot when jumping between holding cubes

    private float pathProgress;
    public GameObject traceObj;
    public TextMeshPro ammoText;
    private MeshRenderer meshRenderer;
    private Color baseColor;

    public void Init(Color? forcedColor = null)
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (forcedColor.HasValue)
            {
                baseColor = forcedColor.Value;
            }
            else
            {
                if (colorCode == 1)
                    baseColor = new Color(1f, 0.4f, 0.7f); // Pink
                else if (colorCode == 2)
                    baseColor = new Color(0.2f, 0.6f, 1f); // Custom Blue (Matches Blocks)
                else
                    baseColor = Color.white;
            }

            meshRenderer.material.color = baseColor;
        }
        if (ammoText) ammoText.text = ammo.ToString();
    }

    void Start() { if (baseColor == Color.clear) Init(); }

    void Update()
    {
        if (state == PigState.Deck) UpdateDeckPosition();
        else if (state == PigState.OnBelt) { MoveAlongBelt(); HandleShooting(); }
        else if (state == PigState.Holding) UpdateHoldingPosition();
        else if (state == PigState.JumpingToHolding) ValidateHoldingTarget();

        if (ammoText) ammoText.text = ammo.ToString();
    }

    public void UpdateDeckPosition()
    {
        if (GameManager.Instance == null) return;

        float x = (colIndex - (GameManager.Instance.deckColumns.Count - 1) / 2f) * GameManager.Instance.deckColSpacing;
        float z = (rowIndex * GameManager.Instance.deckRowSpacing);
        Vector3 targetLocalPos = new Vector3(x, 0, z);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * 10f);

        // Determine active state (Front row OR Hand Picker)
        bool isActive = (rowIndex == 0) || GameManager.Instance.isHandPickerActive;

        if (meshRenderer != null)
        {
            Color targetColor = isActive ? baseColor : Color.Lerp(baseColor, Color.black, 0.6f);
            meshRenderer.material.color = Color.Lerp(meshRenderer.material.color, targetColor, Time.deltaTime * 5f);
        }

        // --- VISUAL POLISH: Fade text opacity for back rows ---
        if (ammoText != null)
        {
            float targetAlpha = isActive ? 1.0f : 0.4f;
            Color textColor = ammoText.color;
            textColor.a = Mathf.Lerp(textColor.a, targetAlpha, Time.deltaTime * 5f);
            ammoText.color = textColor;
        }

        if (GameManager.Instance.isHandPickerActive)
            transform.localPosition += Vector3.up * Mathf.Sin(Time.time * 5) * 0.005f;
        else
        {
            Vector3 p = transform.localPosition;
            p.y = Mathf.Lerp(p.y, 0, Time.deltaTime * 10f);
            transform.localPosition = p;
        }
    }

    public void JumpToBelt()
    {
        state = PigState.Jumping;
        transform.SetParent(null);

        if (meshRenderer) meshRenderer.material.color = baseColor;

        // Reset text opacity to full when jumping
        if (ammoText != null)
        {
            Color c = ammoText.color;
            c.a = 1f;
            ammoText.color = c;
        }

        GameManager.Instance.SendTrayToEquip(jumpDuration);

        Vector3 jumpTarget = Vector3.zero;

        if (GameManager.Instance.trayEquipPos != null)
        {
            jumpTarget = GameManager.Instance.trayEquipPos.position;
        }
        else if (GameManager.Instance.beltCorners != null && GameManager.Instance.beltCorners.Length > 0)
        {
            jumpTarget = GameManager.Instance.beltCorners[0].position;
        }

        StartCoroutine(JumpRoutine(jumpTarget, PigState.OnBelt));
    }

    public void ReturnToHolding()
    {
        if (state == PigState.Returning || state == PigState.Holding || state == PigState.JumpingToHolding) return;
        state = PigState.Returning;

        int slotIndex = FindAvailableHoldingSlot();

        if (slotIndex == -1)
        {
            Debug.LogWarning("[PigController] No holding slots available!");
            GameManager.Instance.TriggerGameOver();
            return;
        }

        // Reserve the slot immediately
        GameManager.Instance.holdingPigs[slotIndex] = this;
        this.holdingIndex = slotIndex;
        this.pendingHoldingIndex = slotIndex;

        if (traceObj) traceObj.SetActive(false);

        Vector3 returnStartPos = transform.position;
        if (GameManager.Instance.trayUnequipPos != null) returnStartPos = GameManager.Instance.trayUnequipPos.position;
        GameManager.Instance.ReturnTray(returnStartPos);

        // Parent to specific HoldingCube and jump to its position
        Vector3 worldDest;
        if (slotIndex < GameManager.Instance.holdingCubes.Count && GameManager.Instance.holdingCubes[slotIndex] != null)
        {
            Transform targetCube = GameManager.Instance.holdingCubes[slotIndex];
            transform.SetParent(targetCube);
            worldDest = targetCube.position;
        }
        else
        {
            // Fallback to old behavior if holdingCubes not set up
            if (GameManager.Instance.holdingContainer)
                transform.SetParent(GameManager.Instance.holdingContainer);
            float targetLocalX = (slotIndex - (GameManager.Instance.holdingPigs.Count - 1) / 2f) * GameManager.Instance.holdingSpacing;
            Vector3 targetLocal = new Vector3(targetLocalX, 0, 0);
            worldDest = GameManager.Instance.holdingContainer.TransformPoint(targetLocal);
        }

        StartCoroutine(ReturnToHoldingRoutine(worldDest));
    }

    IEnumerator ReturnToHoldingRoutine(Vector3 destination)
    {
        Vector3 start = transform.position;
        float elapsed = 0;
        float checkInterval = 0.1f;
        float lastCheckTime = 0;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            Vector3 currentPos = Vector3.Lerp(start, destination, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 4.0f;
            transform.position = currentPos;
            transform.rotation = Quaternion.Euler(t * 360, 0, 0);

            // Periodic validation check during return jump
            if (elapsed - lastCheckTime >= checkInterval)
            {
                lastCheckTime = elapsed;

                // Check if another pig is ALSO jumping to our slot - this is a collision!
                if (pendingHoldingIndex >= 0 && IsAnotherPigJumpingToSameSlot(pendingHoldingIndex))
                {
                    Debug.LogError($"[PigController] COLLISION DETECTED! Multiple pigs jumping to slot {pendingHoldingIndex}. Game Over!");
                    GameManager.Instance.TriggerGameOver();
                    yield break;
                }

                // Check if another pig has taken our slot (already landed)
                if (pendingHoldingIndex >= 0 &&
                    GameManager.Instance.holdingPigs[pendingHoldingIndex] != null &&
                    GameManager.Instance.holdingPigs[pendingHoldingIndex] != this)
                {
                    // Our slot was taken! Try to find a new one
                    int newSlot = FindAvailableHoldingSlot();
                    if (newSlot >= 0)
                    {
                        holdingIndex = newSlot;
                        pendingHoldingIndex = newSlot;
                        GameManager.Instance.holdingPigs[newSlot] = this;

                        if (newSlot < GameManager.Instance.holdingCubes.Count && GameManager.Instance.holdingCubes[newSlot] != null)
                        {
                            Transform targetCube = GameManager.Instance.holdingCubes[newSlot];
                            transform.SetParent(targetCube);
                            destination = targetCube.position;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PigController] No holding slots available during return validation!");
                        GameManager.Instance.TriggerGameOver();
                        yield break;
                    }
                }
            }

            yield return null;
        }

        transform.position = destination;
        transform.rotation = Quaternion.identity;
        pendingHoldingIndex = -1;
        state = PigState.Holding;
        pathProgress = 0;

        if (traceObj != null) traceObj.SetActive(false);

        // FINAL VALIDATION: Check for overlap after landing
        ValidateNoOverlapOnLanding();
    }

    /// <summary>
    /// Checks if another pig is currently jumping to the same slot as us.
    /// If true, it's a collision and game should be over.
    /// </summary>
    bool IsAnotherPigJumpingToSameSlot(int slotIndex)
    {
        PigController[] allPigs = FindObjectsOfType<PigController>();
        foreach (var pig in allPigs)
        {
            if (pig == null || pig == this) continue;
            if ((pig.state == PigState.Returning || pig.state == PigState.JumpingToHolding) &&
                pig.pendingHoldingIndex == slotIndex)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Final check after landing - detects if another pig occupies the same slot or is too close.
    /// This is the last line of defense against the overlap bug.
    /// </summary>
    void ValidateNoOverlapOnLanding()
    {
        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return;

        // Check 1: Verify our slot in holdingPigs array is actually us
        if (holdingIndex >= 0 && holdingIndex < GameManager.Instance.holdingPigs.Count)
        {
            PigController slotOccupant = GameManager.Instance.holdingPigs[holdingIndex];
            if (slotOccupant != null && slotOccupant != this)
            {
                Debug.LogError($"[PigController] OVERLAP DETECTED! Slot {holdingIndex} is occupied by another pig. Game Over!");
                GameManager.Instance.TriggerGameOver();
                return;
            }
        }

        // Check 2: Physical proximity check - are we too close to another pig in holding?
        float overlapThreshold = 0.5f; // Pigs closer than this are overlapping
        for (int i = 0; i < GameManager.Instance.holdingPigs.Count; i++)
        {
            PigController otherPig = GameManager.Instance.holdingPigs[i];
            if (otherPig == null || otherPig == this) continue;
            if (otherPig.state != PigState.Holding && otherPig.state != PigState.JumpingToHolding) continue;

            float distance = Vector3.Distance(transform.position, otherPig.transform.position);
            if (distance < overlapThreshold)
            {
                Debug.LogError($"[PigController] PHYSICAL OVERLAP DETECTED! Distance to pig at slot {i}: {distance:F2}. Game Over!");
                GameManager.Instance.TriggerGameOver();
                return;
            }
        }

        // Check 3: Count total pigs in holding area - should not exceed slot count
        int pigsInHolding = 0;
        for (int i = 0; i < GameManager.Instance.holdingPigs.Count; i++)
        {
            if (GameManager.Instance.holdingPigs[i] != null)
                pigsInHolding++;
        }

        // Also count pigs physically parented to holding cubes
        int physicalPigsInHolding = 0;
        foreach (var cube in GameManager.Instance.holdingCubes)
        {
            if (cube == null) continue;
            foreach (Transform child in cube)
            {
                PigController pig = child.GetComponent<PigController>();
                if (pig != null && (pig.state == PigState.Holding || pig.state == PigState.JumpingToHolding || pig.state == PigState.Returning))
                    physicalPigsInHolding++;
            }
        }

        if (physicalPigsInHolding > GameManager.Instance.holdingPigs.Count)
        {
            Debug.LogError($"[PigController] TOO MANY PIGS IN HOLDING! Physical: {physicalPigsInHolding}, Slots: {GameManager.Instance.holdingPigs.Count}. Game Over!");
            GameManager.Instance.TriggerGameOver();
            return;
        }
    }

    IEnumerator JumpRoutine(Vector3 destination, PigState nextState)
    {
        Vector3 start = transform.position;
        float elapsed = 0;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            Vector3 currentPos = Vector3.Lerp(start, destination, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 4.0f;
            transform.position = currentPos;
            transform.rotation = Quaternion.Euler(t * 360, 0, 0);
            yield return null;
        }

        transform.position = destination;
        transform.rotation = Quaternion.identity;
        state = nextState;
        pathProgress = 0;

        if (nextState == PigState.OnBelt && traceObj != null) traceObj.SetActive(true);
    }

    void MoveAlongBelt()
    {
        pathProgress += Time.deltaTime * beltSpeed;

        Vector3 pos = GameManager.Instance.GetSmoothPathPoint(pathProgress);
        transform.position = pos;

        // Check distance to Unequip Point to exit belt
        if (GameManager.Instance.trayUnequipPos != null)
        {
            // FIX: Increased threshold to 0.5f (was 0.1f) to ensure reliable exit even at high speeds
            if (Vector3.Distance(transform.position, GameManager.Instance.trayUnequipPos.position) < 0.5f && pathProgress > 0.1f)
            {
                ReturnToHolding();
                return;
            }
        }
        else if (pathProgress >= 1.0f)
        {
            ReturnToHolding();
            return;
        }

        // --- VISUAL POLISH: Conveyor Belt Rotation ---
        // Look ahead to find the tangent direction
        Vector3 nextPos = GameManager.Instance.GetSmoothPathPoint(pathProgress + 0.02f);
        Vector3 moveDir = (nextPos - transform.position).normalized;

        moveDir.y = 0; // Flatten to avoid tilting up/down
        moveDir.Normalize();

        if (moveDir != Vector3.zero)
        {
            // Rotate -90 degrees around Y axis to face "inward" relative to the path
            transform.rotation = Quaternion.LookRotation(moveDir) * Quaternion.Euler(0, -90, 0);
        }
    }

    void HandleShooting()
    {
        Vector3 rayOrigin = transform.position + transform.forward * 0.8f;

        if (Time.time - lastFireTime > fireRate && ammo > 0)
        {
            RaycastHit hit;
            if (Physics.SphereCast(rayOrigin, 0.4f, transform.forward, out hit, 20f))
            {
                BlockController block = hit.collider.GetComponentInParent<BlockController>();
                if (block != null && block.colorCode == this.colorCode && !block.isReserved)
                {
                    block.isReserved = true;
                    ammo--;
                    lastFireTime = Time.time;

                    GameObject b = Instantiate(GameManager.Instance.bulletPrefab, transform.position, Quaternion.identity);

                    Renderer bulletRenderer = b.GetComponentInChildren<Renderer>();
                    if (bulletRenderer != null && meshRenderer != null)
                    {
                        bulletRenderer.material.color = meshRenderer.material.color;
                    }

                    // --- SOUND INTEGRATION ---
                    if (SoundManager.Instance) SoundManager.Instance.PlayShoot();

                    BulletController bullet = b.GetComponent<BulletController>();
                    if (bullet != null) bullet.Initialize(block.transform, colorCode);

                    if (ammo <= 0)
                    {
                        GameManager.Instance.ReturnTray(transform.position);
                        StartCoroutine(PopAndDestroy());
                    }
                }
            }
        }
    }

    IEnumerator PopAndDestroy()
    {
        this.enabled = false;
        if (traceObj) traceObj.SetActive(false);

        float elapsed = 0f;
        float duration = 0.1f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 1.2f;

        // 1. Pop Up
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }
        transform.localScale = targetScale;

        // 2. Shrink
        elapsed = 0f;
        duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, elapsed / duration);
            yield return null;
        }
        transform.localScale = Vector3.zero;

        Destroy(gameObject);
    }

    private float lastOverlapCheckTime = 0f;
    private const float OVERLAP_CHECK_INTERVAL = 0.5f; // Check every 0.5 seconds while in Holding state

    void UpdateHoldingPosition()
    {
        // Periodic overlap detection while in Holding state (catch any bugs)
        if (Time.time - lastOverlapCheckTime > OVERLAP_CHECK_INTERVAL)
        {
            lastOverlapCheckTime = Time.time;
            CheckForOverlapWhileHolding();
        }

        if (holdingIndex > 0 && CanClaimHoldingSlot(holdingIndex - 1))
        {
            int targetSlot = holdingIndex - 1;

            // Reserve the target slot immediately to prevent race conditions
            GameManager.Instance.holdingPigs[targetSlot] = this;
            GameManager.Instance.holdingPigs[holdingIndex] = null;

            int previousIndex = holdingIndex;
            holdingIndex = targetSlot;
            pendingHoldingIndex = targetSlot;

            // Re-parent to the new HoldingCube and jump there
            Vector3 wDest;
            if (holdingIndex < GameManager.Instance.holdingCubes.Count && GameManager.Instance.holdingCubes[holdingIndex] != null)
            {
                Transform targetCube = GameManager.Instance.holdingCubes[holdingIndex];
                transform.SetParent(targetCube);
                wDest = targetCube.position;
            }
            else
            {
                float tX = (holdingIndex - (GameManager.Instance.holdingPigs.Count - 1) / 2f) * GameManager.Instance.holdingSpacing;
                Vector3 tLocal = new Vector3(tX, 0, 0);
                wDest = GameManager.Instance.holdingContainer.TransformPoint(tLocal);
            }

            state = PigState.JumpingToHolding;
            StartCoroutine(JumpToHoldingRoutine(wDest));
            return;
        }

        // Smoothly center on current HoldingCube
        Vector3 targetLocal = Vector3.zero;

        if (Vector3.Distance(transform.localPosition, targetLocal) > 0.01f)
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocal, Time.deltaTime * 5f);
        else
            transform.localPosition = targetLocal;

        transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Lightweight overlap check while in Holding state - runs periodically to catch edge cases.
    /// </summary>
    void CheckForOverlapWhileHolding()
    {
        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return;

        float overlapThreshold = 0.5f;
        for (int i = 0; i < GameManager.Instance.holdingPigs.Count; i++)
        {
            PigController otherPig = GameManager.Instance.holdingPigs[i];
            if (otherPig == null || otherPig == this) continue;
            if (otherPig.state != PigState.Holding) continue;

            float distance = Vector3.Distance(transform.position, otherPig.transform.position);
            if (distance < overlapThreshold)
            {
                Debug.LogError($"[PigController] OVERLAP while holding! This pig at slot {holdingIndex}, other at slot {i}, distance: {distance:F2}. Game Over!");
                GameManager.Instance.TriggerGameOver();
                return;
            }
        }
    }

    /// <summary>
    /// Checks if a holding slot can be claimed by this pig.
    /// A slot is claimable if it's null AND no other pig is currently jumping to it.
    /// </summary>
    bool CanClaimHoldingSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= GameManager.Instance.holdingPigs.Count)
            return false;

        // Check if slot is already occupied in the array
        if (GameManager.Instance.holdingPigs[slotIndex] != null)
            return false;

        // Check ALL pigs that might be targeting this slot (including those on belt about to return)
        if (IsSlotTargetedByAnyPig(slotIndex))
            return false;

        // Also check HoldingCubes for pigs that might be parented there but not in the list yet
        if (slotIndex < GameManager.Instance.holdingCubes.Count &&
            GameManager.Instance.holdingCubes[slotIndex] != null)
        {
            foreach (Transform child in GameManager.Instance.holdingCubes[slotIndex])
            {
                PigController pig = child.GetComponent<PigController>();
                if (pig != null && pig != this)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if any pig in the scene is targeting this slot (returning or jumping to it).
    /// This is more expensive but necessary to prevent race conditions.
    /// </summary>
    bool IsSlotTargetedByAnyPig(int slotIndex)
    {
        // Check pigs in holding array
        for (int i = 0; i < GameManager.Instance.holdingPigs.Count; i++)
        {
            PigController pig = GameManager.Instance.holdingPigs[i];
            if (pig == null || pig == this) continue;

            if ((pig.state == PigState.JumpingToHolding || pig.state == PigState.Returning) &&
                pig.pendingHoldingIndex == slotIndex)
                return true;
        }

        // Check pigs in deck columns (they might be mid-jump)
        foreach (var column in GameManager.Instance.deckColumns)
        {
            foreach (var pig in column)
            {
                if (pig == null || pig == this) continue;
                if ((pig.state == PigState.Returning || pig.state == PigState.JumpingToHolding) &&
                    pig.pendingHoldingIndex == slotIndex)
                    return true;
            }
        }

        // Check pigs that might be on the belt or jumping (not in any list)
        // We need to find all PigControllers in the scene
        PigController[] allPigs = FindObjectsOfType<PigController>();
        foreach (var pig in allPigs)
        {
            if (pig == null || pig == this) continue;
            if ((pig.state == PigState.Returning || pig.state == PigState.JumpingToHolding) &&
                pig.pendingHoldingIndex == slotIndex)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Validates during jump that our target slot hasn't been taken by another pig.
    /// Called every frame while in JumpingToHolding state.
    /// </summary>
    void ValidateHoldingTarget()
    {
        // This is called from Update while jumping between holding slots
        // The actual validation happens in the coroutine, this just ensures
        // we don't get stuck if something goes wrong
        if (pendingHoldingIndex < 0)
        {
            state = PigState.Holding;
        }
    }

    IEnumerator JumpToHoldingRoutine(Vector3 destination)
    {
        Vector3 start = transform.position;
        float elapsed = 0;
        float checkInterval = 0.1f; // Check every 100ms instead of every frame for optimization
        float lastCheckTime = 0;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            Vector3 currentPos = Vector3.Lerp(start, destination, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 1.5f; // Smaller arc for holding jumps
            transform.position = currentPos;
            transform.rotation = Quaternion.Euler(t * 360, 0, 0);

            // Periodic validation check (optimized - not every frame)
            if (elapsed - lastCheckTime >= checkInterval)
            {
                lastCheckTime = elapsed;

                // Check if another pig is ALSO jumping to our slot - this is a collision!
                if (pendingHoldingIndex >= 0 && IsAnotherPigJumpingToSameSlot(pendingHoldingIndex))
                {
                    Debug.LogError($"[PigController] COLLISION DETECTED during holding jump! Multiple pigs targeting slot {pendingHoldingIndex}. Game Over!");
                    GameManager.Instance.TriggerGameOver();
                    yield break;
                }

                // Check if another pig has taken our slot (shouldn't happen but safety check)
                if (pendingHoldingIndex >= 0 &&
                    GameManager.Instance.holdingPigs[pendingHoldingIndex] != null &&
                    GameManager.Instance.holdingPigs[pendingHoldingIndex] != this)
                {
                    // Our slot was taken! Find a new one
                    int newSlot = FindAvailableHoldingSlot();
                    if (newSlot >= 0)
                    {
                        // Claim new slot
                        holdingIndex = newSlot;
                        pendingHoldingIndex = newSlot;
                        GameManager.Instance.holdingPigs[newSlot] = this;

                        // Update destination
                        if (newSlot < GameManager.Instance.holdingCubes.Count && GameManager.Instance.holdingCubes[newSlot] != null)
                        {
                            Transform targetCube = GameManager.Instance.holdingCubes[newSlot];
                            transform.SetParent(targetCube);
                            destination = targetCube.position;
                        }
                    }
                    else
                    {
                        // No slots available - game over
                        Debug.LogWarning("[PigController] No holding slots available during jump validation!");
                        GameManager.Instance.TriggerGameOver();
                        yield break;
                    }
                }
            }

            yield return null;
        }

        transform.position = destination;
        transform.rotation = Quaternion.identity;
        pendingHoldingIndex = -1;
        state = PigState.Holding;

        // FINAL VALIDATION: Check for overlap after landing
        ValidateNoOverlapOnLanding();
    }

    /// <summary>
    /// Finds the first available holding slot, checking both null slots and slots not being jumped to.
    /// Prioritizes lower indices (leftmost slots) for proper stacking.
    /// </summary>
    int FindAvailableHoldingSlot()
    {
        for (int i = 0; i < GameManager.Instance.holdingPigs.Count; i++)
        {
            if (CanClaimHoldingSlot(i))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Called when this pig is destroyed - clean up any reservations.
    /// </summary>
    void OnDestroy()
    {
        // Clean up holding slot reservation if we had one
        if (holdingIndex >= 0 &&
            holdingIndex < GameManager.Instance.holdingPigs.Count &&
            GameManager.Instance.holdingPigs[holdingIndex] == this)
        {
            GameManager.Instance.holdingPigs[holdingIndex] = null;
        }
    }
}
}