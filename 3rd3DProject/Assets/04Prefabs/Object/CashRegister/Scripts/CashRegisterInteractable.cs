using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TexasCashRegisterComplete
{
    [SelectionBase]
    public class CashRegisterInteractable : MonoBehaviour
    {
        [Header("Drawer")]
        public Transform drawer;
        public Vector3 openOffset = new Vector3(0f, 0f, -0.26f);
        [Min(0.01f)] public float moveDuration = 0.3f;
        public bool startOpen;
        [Header("Interaction")]
        [Tooltip("고양이 캐릭터의 루트 Transform을 넣으세요. 비어 있으면 Player 태그로 찾습니다.")]
        public Transform player;
        public Transform interactionPoint;
        [Min(0.1f)] public float interactionDistance = 1.8f;
        public bool enableKeyboard = true;
        public bool showPrompt = true;
        [Header("Events (optional)")]
        public UnityEvent onOpened = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();
        public bool IsOpen { get { return targetOpen; } }
        public bool IsMoving { get { return moving; } }
        Vector3 closedPosition;
        float progress;
        bool targetOpen, moving;
        float nextPlayerSearch;

        void Awake()
        {
            if (drawer == null)
            {
                Debug.LogError("Cash register: Drawer reference is missing.", this);
                enabled = false; return;
            }
            closedPosition = drawer.localPosition;
            targetOpen = startOpen;
            progress = startOpen ? 1f : 0f;
            ApplyPosition();
            FindPlayer();
        }
        void FindPlayer()
        {
            if (player != null) return;
            // Unity's built-in Player tag is available without creating a new tag.
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
            nextPlayerSearch = Time.unscaledTime + 1f;
        }
        public bool CanInteract()
        {
            if (player == null) return false;
            Vector3 point = interactionPoint == null ? transform.position : interactionPoint.position;
            return (player.position - point).sqrMagnitude <= interactionDistance * interactionDistance;
        }
        void Update()
        {
            if (player == null && Time.unscaledTime >= nextPlayerSearch) FindPlayer();
            if (enableKeyboard && CanInteract() && PressedE()) ToggleDrawer();
            if (!moving) return;
            progress = Mathf.MoveTowards(progress, targetOpen ? 1f : 0f, Time.deltaTime / Mathf.Max(.01f, moveDuration));
            ApplyPosition();
            if (progress == (targetOpen ? 1f : 0f))
            {
                moving = false;
                if (targetOpen) onOpened.Invoke(); else onClosed.Invoke();
            }
        }
        static bool PressedE()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }
        void ApplyPosition()
        {
            if (drawer != null) drawer.localPosition = closedPosition + openOffset * Mathf.SmoothStep(0f, 1f, progress);
        }
        public void ToggleDrawer() { SetOpen(!targetOpen); }
        public void OpenDrawer() { SetOpen(true); }
        public void CloseDrawer() { SetOpen(false); }
        public void SetOpen(bool open)
        {
            if (drawer == null || targetOpen == open) return;
            targetOpen = open; moving = true;
        }
        void OnGUI()
        {
            if (!showPrompt || !enableKeyboard || !CanInteract()) return;
            Rect rect = new Rect(Screen.width * .5f - 130f, Screen.height - 75f, 260f, 40f);
            GUI.Box(rect, targetOpen ? "E : CLOSE DRAWER" : "E : OPEN DRAWER");
        }
    }
}
