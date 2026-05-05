using UnityEngine;

namespace PixelShooter3D
{
public class InputManager : MonoBehaviour
{
    void Start()
    {
        // SANITY CHECK: If you don't see this, the script is NOT attached to an active GameObject.
        Debug.Log("✅ InputManager Script has started running!");
    }

    void Update()
    {
        // FIX 1: Use GetMouseButtonUp (Action happens on release) 
        // This prevents accidental double-triggers if the mouse is held down slightly too long
        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("🖱️ Mouse Click Released!");

            // 2. Check Critical References
            if (GameManager.Instance == null)
            {
                Debug.LogError("❌ GameManager.Instance is NULL!");
                return;
            }

            if (GameManager.Instance.isGameOver)
            {
                Debug.Log("⚠️ Click ignored: Game Over state is true.");
                return;
            }

            if (Camera.main == null)
            {
                Debug.LogError("❌ CRITICAL: No Camera tagged 'MainCamera' found!");
                return;
            }

            // 3. Perform Raycast
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // DRAW RAY (Visible in SCENE tab, not Game tab)
            Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red, 2.0f);

            if (Physics.Raycast(ray, out hit, 1000f))
            {
                Debug.Log("🎯 RAY HIT: " + hit.collider.name);

                // Check for Pig
                PigController pig = hit.collider.GetComponentInParent<PigController>();
                if (pig != null)
                {
                    Debug.Log("🐷 Found PigController on: " + pig.gameObject.name);
                    TryActivatePig(pig);
                }
                else
                {
                    Debug.Log("⚠️ Hit object '" + hit.collider.name + "' has no PigController.");
                }
            }
            else
            {
                Debug.Log("💨 Raycast hit nothing (Air).");
            }
        }
    }

    void TryActivatePig(PigController pig)
    {
        if (pig.state == PigState.Deck)
        {
            if (GameManager.Instance.availableTraces <= 0)
            {
                Debug.Log("No traces left!");
                return;
            }

            // Check if valid selection (Front of line OR Hand Picker)
            bool isFrontOfLine = false;

            // Safety check for empty columns
            if (pig.colIndex < GameManager.Instance.deckColumns.Count)
            {
                var col = GameManager.Instance.deckColumns[pig.colIndex];
                if (col.Count > 0 && col[0] == pig) isFrontOfLine = true;
            }

            if (isFrontOfLine || GameManager.Instance.isHandPickerActive)
            {
                if (GameManager.Instance.isHandPickerActive) GameManager.Instance.UseHandPicker();

                // Remove from deck logic
                var col = GameManager.Instance.deckColumns[pig.colIndex];
                col.Remove(pig);

                // Shift remaining pigs in that column
                for (int i = 0; i < col.Count; i++)
                {
                    col[i].rowIndex = i;
                    col[i].UpdateDeckPosition(); // Lerp to new spot
                }

                GameManager.Instance.holdingPigs.Add(pig);

                // --- PLAY SOUND: Pig Select ---
                if (SoundManager.Instance) SoundManager.Instance.PlayPigSelect();

                // This function eventually calls GameManager.SendTrayToEquip, 
                // which handles the trace decrement logic.
                pig.JumpToBelt();
            }
            else
            {
                Debug.Log("Pig is not at front of line.");
            }
        }
        else if (pig.state == PigState.Holding)
        {
            // Remove from holding array
            int index = GameManager.Instance.holdingPigs.IndexOf(pig);
            if (index != -1) GameManager.Instance.holdingPigs[index] = null;

            // --- PLAY SOUND: Pig Select ---
            if (SoundManager.Instance) SoundManager.Instance.PlayPigSelect();

            pig.JumpToBelt();
        }
    }
}
}