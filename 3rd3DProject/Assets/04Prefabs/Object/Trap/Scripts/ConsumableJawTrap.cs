using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;

namespace TexasJawTrap
{
    [System.Serializable] public class TrapActorEvent : UnityEvent<GameObject> { }
    [SelectionBase]
    [DisallowMultipleComponent]
    public class ConsumableJawTrap : MonoBehaviour
    {
        [Header("Model")]
        public Transform frontJaw;
        public Transform backJaw;
        public Transform pressurePlate;
        public Collider contactTrigger;
        [Header("Activation")]
        [Min(0)] public float armDelay = .6f;
        [Min(.01f)] public float closeDuration = .16f;
        [Range(0, 89)] public float closeAngle = 82f;
        public LayerMask targetLayers = ~0;
        public string requiredTag = "";
        [Tooltip("Rigidbody, CharacterController 또는 NavMeshAgent가 있는 캐릭터를 감지합니다.")]
        public bool requireActorComponent = true;
        public bool ignoreOwner = true;
        [Header("Consumption")]
        public bool removeAfterUse = true;
        [Tooltip("적이 밟은 순간부터 스턴과 덫 유지 시간이 함께 시작됩니다.")]
        [Min(.01f)] public float stunDuration = 3f;
        // 이전 프리팹/외부 코드와 호환하기 위한 필드. 삭제 시점은 stunDuration으로 결정합니다.
        [HideInInspector] public float removeDelay = 3f;
        public TrapActorEvent onTriggered = new TrapActorEvent();
        public UnityEvent onClosed = new UnityEvent();
        public bool IsUsed { get; private set; }
        public GameObject CaughtActor { get; private set; }
        public bool IsArmed { get { return initialized && !IsUsed && Time.time >= readyTime; } }
        Quaternion frontOpen, backOpen;
        Vector3 plateOpen;
        float readyTime, progress, consumedTime;
        bool initialized, closing;
        Transform owner;

        void Awake() { InitializeState(); }
        void InitializeState()
        {
            if (initialized) return;
            if (frontJaw == null || backJaw == null || contactTrigger == null)
            {
                Debug.LogError("Jaw trap: model/trigger references are missing.", this); enabled = false; return;
            }
            frontOpen = frontJaw.localRotation; backOpen = backJaw.localRotation;
            if (pressurePlate != null) plateOpen = pressurePlate.localPosition;
            contactTrigger.isTrigger = true;
            readyTime = Time.time + Mathf.Max(0, armDelay);
            initialized = true;
        }
        public void SetOwner(Transform placer) { owner = placer; }
        void OnTriggerEnter(Collider other) { TryTrigger(other); }
        void OnTriggerStay(Collider other) { TryTrigger(other); }
        bool IsTarget(Collider other, out GameObject actor)
        {
            actor = null;
            if (other == null || other.isTrigger || other.transform.IsChildOf(transform)) return false;
            Rigidbody body = other.attachedRigidbody;
            CharacterController controller = other.GetComponentInParent<CharacterController>();
            NavMeshAgent agent = other.GetComponentInParent<NavMeshAgent>();
            EnemyTrapStun stun = other.GetComponentInParent<EnemyTrapStun>();
            Transform actorRoot = stun != null ? stun.transform : controller != null ? controller.transform : agent != null ? agent.transform : body != null ? body.transform : other.transform;
            actor = actorRoot.gameObject;
            if (ignoreOwner && owner != null && (actorRoot == owner || actorRoot.IsChildOf(owner) || owner.IsChildOf(actorRoot))) return false;
            bool layerMatches = ((1 << other.gameObject.layer) & targetLayers.value) != 0 || ((1 << actor.layer) & targetLayers.value) != 0;
            if (!layerMatches) return false;
            if (!string.IsNullOrEmpty(requiredTag) && other.gameObject.tag != requiredTag && actor.tag != requiredTag) return false;
            if (requireActorComponent && body == null && controller == null && agent == null) return false;
            return true;
        }
        public bool TryTrigger(Collider other)
        {
            if (!IsArmed || !IsTarget(other, out GameObject actor)) return false;
            Trigger(actor); return true;
        }
        // External game interaction code may also activate this one-use item.
        public void Trigger(GameObject actor)
        {
            if (!IsArmed) return;
            IsUsed = true; CaughtActor = actor; closing = true; progress = 0;
            contactTrigger.enabled = false;
            float duration = Mathf.Max(.01f, stunDuration);
            consumedTime = Time.time + duration;
            if (actor != null)
            {
                EnemyTrapStun stun = actor.GetComponent<EnemyTrapStun>();
                if (stun == null) stun = actor.AddComponent<EnemyTrapStun>();
                stun.ApplyStun(duration);
            }
            onTriggered.Invoke(actor);
        }
        void Update()
        {
            // 설치할 때는 삭제하지 않습니다. 적이 밟고 스턴 시간이 끝났을 때만 삭제합니다.
            if (IsUsed && removeAfterUse && Time.time >= consumedTime)
            {
                Destroy(gameObject);
                return;
            }
            if (!closing) return;
            progress = Mathf.MoveTowards(progress, 1f, Time.deltaTime / Mathf.Max(.01f, closeDuration));
            float t = 1f - (1f - progress) * (1f - progress);
            frontJaw.localRotation = frontOpen * Quaternion.AngleAxis(-closeAngle * t, Vector3.right);
            backJaw.localRotation = backOpen * Quaternion.AngleAxis(closeAngle * t, Vector3.right);
            if (pressurePlate != null) pressurePlate.localPosition = plateOpen + Vector3.down * (.012f * t);
            if (progress >= 1f)
            {
                closing = false;
                onClosed.Invoke();
            }
        }
    }
}
