using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TexasJawTrap
{
    // Optional placement component. Add to the cat character for a small consumable inventory.
    public class TrapConsumableInventory : MonoBehaviour
    {
        public ConsumableJawTrap trapPrefab;
        [Min(0)] public int remaining = 3;
        [Min(.1f)] public float placeDistance = 1.1f;
        public LayerMask groundLayers = ~0;
        [Range(0, 80)] public float maximumSlope = 45;
        public bool enableGKey = true;
        void Update()
        {
            if (enableGKey && PressedG()) TryPlace();
        }
        static bool PressedG()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.G);
#else
            return false;
#endif
        }
        public bool TryPlace()
        {
            if (remaining <= 0 || trapPrefab == null) return false;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            Vector3 position = transform.position + forward * placeDistance;
            RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 2f, Vector3.down, 4f, groundLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                if (Vector3.Angle(hit.normal, Vector3.up) > maximumSlope) continue;
                Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * facing;
                ConsumableJawTrap placed = Instantiate(trapPrefab, hit.point + hit.normal * .01f, rotation);
                placed.SetOwner(transform);
                remaining--; return true;
            }
            return false;
        }
    }
}
