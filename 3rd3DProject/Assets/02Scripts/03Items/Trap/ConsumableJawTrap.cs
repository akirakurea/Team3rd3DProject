using UnityEngine;
using UnityEngine.Events;

namespace TexasJawTrap
{
    // 도둑이 잡혔을 때 다른 기능에 알려주는 이벤트입니다.
    [System.Serializable]
    public class TrapActorEvent : UnityEvent<GameObject> { }

    [DisallowMultipleComponent]
    public class ConsumableJawTrap : MonoBehaviour
    {
        [Header("덫 부품")]
        public Transform frontJaw;
        public Transform backJaw;
        public Transform pressurePlate;
        public Collider contactTrigger;

        [Header("도둑 감지")]
        [Tooltip("도둑 본체에 지정한 태그와 같게 입력하세요.")]
        public string requiredTag = "Thief";
        [Min(0f)] public float armDelay = 0.6f;

        [Header("닫히는 동작")]
        [Min(0.01f)] public float closeDuration = 0.16f;
        [Range(0f, 89f)] public float closeAngle = 82f;

        [Header("사용 후 삭제")]
        public bool removeAfterUse = true;
        [Tooltip("도둑이 밟은 순간부터 덫이 사라질 때까지의 시간입니다. 같은 시간 동안 도둑의 스턴 상태도 유지됩니다.")]
        [Min(0.01f)] public float removeDelay = 3f;

        public TrapActorEvent onTriggered = new TrapActorEvent();
        public UnityEvent onClosed = new UnityEvent();

        // BT에서 읽을 수 있는 정보입니다. 이 덫은 한 번만 작동합니다.
        public bool IsUsed { get; private set; }
        public GameObject CaughtActor { get; private set; }
        public bool IsArmed => enabled && !IsUsed && Time.time >= readyTime;

        private Quaternion frontOpen;
        private Quaternion backOpen;
        private Vector3 plateOpen;
        private float readyTime;
        private float usedTime;
        private bool closed;

        private void Awake()
        {
            if (frontJaw == null || backJaw == null || contactTrigger == null)
            {
                Debug.LogError("덫의 턱과 감지 Collider를 연결하세요.", this);
                enabled = false;
                return;
            }

            // 덫이 열린 모양을 기억하고 설치 대기 시간을 정합니다.
            frontOpen = frontJaw.localRotation;
            backOpen = backJaw.localRotation;
            if (pressurePlate != null) plateOpen = pressurePlate.localPosition;
            contactTrigger.isTrigger = true;
            readyTime = Time.time + armDelay;
        }

        private void OnTriggerEnter(Collider other) => TryTrigger(other);

        // 대기 시간이 끝날 때 이미 덫 위에 있는 도둑도 감지합니다.
        private void OnTriggerStay(Collider other) => TryTrigger(other);

        public bool TryTrigger(Collider other)
        {
            if (!IsArmed || other == null || other.isTrigger) return false;
            GameObject thief = FindThief(other.transform);
            if (thief == null) return false;
            Activate(thief);
            return true;
        }

        // Collider가 자식에 있어도 부모의 도둑 태그를 찾습니다.
        private GameObject FindThief(Transform target)
        {
            if (string.IsNullOrEmpty(requiredTag)) return null;
            while (target != null)
            {
                if (target.gameObject.tag == requiredTag) return target.gameObject;
                target = target.parent;
            }
            return null;
        }

        // BT에서 직접 호출해도 도둑 태그를 확인합니다.
        public void Trigger(GameObject actor)
        {
            if (!IsArmed || actor == null) return;
            GameObject thief = FindThief(actor.transform);
            if (thief != null) Activate(thief);
        }

        private void Activate(GameObject thief)
        {
            IsUsed = true;
            CaughtActor = thief;
            usedTime = Time.time;
            contactTrigger.enabled = false;

            // 덫 유지 시간만큼 스턴 상태를 설정합니다. 실제 행동 중지는 BT가 담당합니다.
            EnemyTrapStun stun = thief.GetComponent<EnemyTrapStun>();
            if (stun != null) stun.ApplyStun(Mathf.Max(0.01f, removeDelay));

            // 잡힌 도둑을 다른 기능에 알립니다.
            onTriggered.Invoke(thief);
        }

        private void Update()
        {
            if (!IsUsed) return;
            float elapsed = Time.time - usedTime;

            if (!closed)
            {
                // 0은 열린 상태, 1은 완전히 닫힌 상태입니다.
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, closeDuration));
                frontJaw.localRotation = frontOpen * Quaternion.Euler(-closeAngle * progress, 0f, 0f);
                backJaw.localRotation = backOpen * Quaternion.Euler(closeAngle * progress, 0f, 0f);
                if (pressurePlate != null)
                    pressurePlate.localPosition = plateOpen + Vector3.down * (0.012f * progress);

                if (progress >= 1f)
                {
                    closed = true;
                    onClosed.Invoke();
                }
            }

            // 스턴 상태가 끝나는 시간에 덫을 삭제합니다.
            if (removeAfterUse && elapsed >= Mathf.Max(0.01f, removeDelay))
                Destroy(gameObject);
        }
    }
}
