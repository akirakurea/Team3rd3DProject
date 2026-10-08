using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TexasJawTrap
{
    // 플레이어에 붙입니다. 덫 설치와 남은 개수만 관리합니다.
    public class TrapConsumableInventory : MonoBehaviour
    {
        public ConsumableJawTrap trapPrefab;
        [Min(0)] public int remaining = 3;
        [Min(0.1f)] public float placeDistance = 1.1f;
        [Tooltip("덫을 놓을 바닥 레이어를 선택하세요.")]
        public LayerMask groundLayers = ~0;
        [Range(0f, 80f)] public float maximumSlope = 45f;
        public bool enableGKey = true;

        private void Update()
        {
            if (enableGKey && PressedG()) TryPlace();
        }

        // 프로젝트의 입력 방식에 맞춰 G키를 확인합니다.
        private static bool PressedG()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.G);
#else
            return false;
#endif
        }

        // 다른 조작 코드에서도 호출할 수 있습니다. 설치 성공 시 true입니다.
        public bool TryPlace()
        {
            if (remaining <= 0 || trapPrefab == null) return false;

            // 플레이어 앞쪽에서 위아래 방향을 빼고 설치 위치를 정합니다.
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Vector3 position = transform.position + forward * placeDistance;

            // 위에서 아래로 바닥을 찾고, 가까운 표면부터 확인합니다.
            RaycastHit[] hits = Physics.RaycastAll(
                position + Vector3.up * 2f, Vector3.down, 4f,
                groundLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                // 자신의 몸과 너무 가파른 표면에는 설치하지 않습니다.
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (Vector3.Angle(hit.normal, Vector3.up) > maximumSlope) continue;

                // 덫의 방향을 플레이어 앞쪽과 바닥 기울기에 맞춥니다.
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal)
                    * Quaternion.LookRotation(forward, Vector3.up);
                Instantiate(trapPrefab, hit.point + hit.normal * 0.01f, rotation);

                // 실제 설치에 성공한 경우에만 개수를 줄입니다.
                remaining--;
                return true;
            }
            return false;
        }
    }
}
