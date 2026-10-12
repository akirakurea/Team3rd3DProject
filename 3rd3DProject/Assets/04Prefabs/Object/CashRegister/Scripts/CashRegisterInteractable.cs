using UnityEngine;
using UnityEngine.Events;

namespace TexasCashRegisterComplete
{
    [System.Serializable]
    public class MoneyTakenEvent : UnityEvent<int> { }

    // 금전등록기에 붙입니다. 플레이어와 AI 모두 같은 함수를 사용합니다.
    [SelectionBase]
    public class CashRegisterInteractable : MonoBehaviour
    {
        [Header("서랍")]
        public Transform drawer;
        public Vector3 openOffset = new Vector3(0f, 0f, -0.26f);
        [Min(0.01f)] public float moveDuration = 0.3f;
        public bool startOpen;

        [Header("서랍 안의 돈")]
        [Tooltip("돈 모델만 연결하세요. 서랍이나 금전등록기 전체는 연결하지 마세요.")]
        public GameObject moneyVisual;
        [Min(0)] public int startingMoney = 100;

        [Header("추가 동작 (선택 사항)")]
        public UnityEvent onOpened = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();
        public MoneyTakenEvent onMoneyTaken = new MoneyTakenEvent();

        // IsOpen은 목표 상태입니다. 완전히 열린 상태는 CanTakeMoney로 확인하세요.
        public bool IsOpen => targetOpen;
        public bool IsMoving => moving;
        public int RemainingMoney { get; private set; }
        public bool HasMoney => RemainingMoney > 0;
        public bool CanTakeMoney => ready && targetOpen && !moving && HasMoney;

        private Vector3 closedPosition;
        private float progress;
        private bool targetOpen;
        private bool moving;
        private bool ready;

        private void Awake()
        {
            if (drawer == null)
            {
                Debug.LogError("금전등록기의 Drawer에 서랍을 연결하세요.", this);
                enabled = false;
                return;
            }

            // 모델은 닫힌 위치에 배치하세요. 그 위치를 기준으로 움직입니다.
            closedPosition = drawer.localPosition;
            targetOpen = startOpen;
            progress = startOpen ? 1f : 0f;
            RemainingMoney = Mathf.Max(0, startingMoney);
            ready = true;
            ApplyPosition();
            UpdateMoneyVisual();
        }

        private void Update()
        {
            if (!moving) return;

            float target = targetOpen ? 1f : 0f;
            progress = Mathf.MoveTowards(progress, target,
                Time.deltaTime / Mathf.Max(0.01f, moveDuration));
            ApplyPosition();

            // 서랍이 끝까지 움직인 뒤 이벤트를 실행합니다.
            if (progress != target) return;
            moving = false;
            if (targetOpen) onOpened.Invoke();
            else onClosed.Invoke();
        }

        private void ApplyPosition()
        {
            // 시작과 끝에서 부드럽게 움직이도록 보간합니다.
            float smooth = Mathf.SmoothStep(0f, 1f, progress);
            drawer.localPosition = closedPosition + openOffset * smooth;
        }

        // 캐릭터 구분 없이 상호작용 코드나 BT에서 호출하세요.
        public void ToggleDrawer() => SetOpen(!targetOpen);
        public void OpenDrawer() => SetOpen(true);
        public void CloseDrawer() => SetOpen(false);

        public void SetOpen(bool open)
        {
            if (!ready || targetOpen == open) return;
            targetOpen = open;
            moving = true;
        }

        // 완전히 열린 서랍에서 돈을 전부 꺼냅니다.
        // 꺼낸 금액을 반환합니다. 돈이 없거나 닫혀 있으면 0입니다.
        public int TakeMoney()
        {
            if (!CanTakeMoney) return 0;

            int amount = RemainingMoney;
            RemainingMoney = 0;
            UpdateMoneyVisual();
            onMoneyTaken.Invoke(amount);
            return amount;
        }

        // 반환값을 사용하지 않는 Inspector 이벤트에서 연결할 수 있습니다.
        public void CollectMoney()
        {
            TakeMoney();
        }

        private void UpdateMoneyVisual()
        {
            if (moneyVisual != null) moneyVisual.SetActive(HasMoney);
        }
    }
}
