using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 문 오브젝트에 붙입니다. hinge는 경첩 위치의 빈 오브젝트입니다.
public class InteractableDoor : MonoBehaviour
{
    public Transform hinge;
    public Transform player;
    public string playerTag = "Player";
    public float interactionDistance = 2f;
    public float openAngle = 90f;
    public float rotationSpeed = 180f;
    public bool readInteractKey = true;
    public UnityEvent onOpened;
    public UnityEvent onClosed;
    public bool IsOpen { get; private set; }
    private Quaternion closedRotation;

    private void Awake()
    {
        if (hinge == null) hinge = transform;
        closedRotation = hinge.localRotation;
    }

    private void Start()
    {
        if (player != null || string.IsNullOrEmpty(playerTag)) return;
        try
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null) player = found.transform;
        }
        catch (UnityException) { Debug.LogWarning("Player 태그를 등록하거나 Player를 직접 연결하세요.", this); }
    }

    private void Update()
    {
        bool pressed = false;
        if (readInteractKey)
        {
#if ENABLE_INPUT_SYSTEM
            pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            pressed = Input.GetKeyDown(KeyCode.E);
#endif
        }
        if (pressed && player != null &&
            Vector3.Distance(player.position, transform.position) <= interactionDistance)
            Interact();

        Quaternion target = closedRotation * Quaternion.Euler(0f, IsOpen ? openAngle : 0f, 0f);
        hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, target, rotationSpeed * Time.deltaTime);
    }

    // 기존 상호작용 시스템에서도 호출할 수 있습니다. 거리 검사는 호출 측에서 처리합니다.
    public void Interact() { SetOpen(!IsOpen); }
    public void Open() { SetOpen(true); }
    public void Close() { SetOpen(false); }
    public void SetOpen(bool value)
    {
        if (IsOpen == value) return;
        IsOpen = value;
        if (value) onOpened.Invoke(); else onClosed.Invoke();
    }
}
