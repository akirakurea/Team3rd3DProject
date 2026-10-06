using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 고양이가 쓸 아이템/도구를 고르는 핫바(5칸)의 선택 상태와 입력을 관리한다.
/// 보물 칸(TreasureHUD)과는 완전히 별개의 시스템이다.
///
/// - 숫자키 1~5로 직접 선택
/// - 마우스 휠로 이전/다음 칸 순환 선택
/// - 결과 화면/승패 화면처럼 시간이 정지(Time.timeScale = 0)된 동안에는 키/휠 입력을 무시한다
///
/// 고양이 컨트롤러는 SelectedIndex / SelectedItem을 읽어서 사용하면 된다.
///   예) var item = HotbarManager.Instance.SelectedItem;
/// </summary>
public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance { get; private set; }

    [SerializeField] private HotbarItemData[] slots = new HotbarItemData[5];

    public int SlotCount => slots.Length;
    public int SelectedIndex { get; private set; } = 0;
    public HotbarItemData SelectedItem => (SelectedIndex >= 0 && SelectedIndex < slots.Length) ? slots[SelectedIndex] : null;
    public HotbarItemData GetSlot(int index) => (index >= 0 && index < slots.Length) ? slots[index] : null;

    /// <summary>선택 칸이 바뀔 때마다 UI가 구독해서 테두리를 갱신한다.</summary>
    public event Action<int> OnSelectedChanged;

    /// <summary>한 칸의 아이템이 바뀔 때(획득/사용) UI가 구독해서 그 칸을 갱신한다.</summary>
    public event Action<int> OnSlotChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // 시작 시 0번 칸 선택 상태를 UI에 한 번 알려준다
        OnSelectedChanged?.Invoke(SelectedIndex);
    }

    private void Update()
    {
        // 시간이 멈춘 화면(결과창, 승리/패배)에서는 핫바 선택을 바꾸지 않는다
        if (Time.timeScale <= 0f) return;

        HandleNumberKeys();
        HandleScrollWheel();
    }

    /// <summary>UI의 슬롯을 직접 클릭했을 때도 이 메서드로 선택할 수 있다.</summary>
    public void Select(int index)
    {
        if (index < 0 || index >= slots.Length || index == SelectedIndex) return;
        SelectedIndex = index;
        OnSelectedChanged?.Invoke(SelectedIndex);
    }

    /// <summary>아이템 획득. 왼쪽 빈 칸부터 채운다. 가득 차면 false.</summary>
    public bool TryAdd(HotbarItemData item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) continue;
            slots[i] = item;
            OnSlotChanged?.Invoke(i);
            return true;
        }
        return false;
    }

    /// <summary>선택 칸의 아이템을 사용한 것으로 처리해 칸을 비운다.</summary>
    public void ConsumeSelected()
    {
        if (SelectedItem == null) return;
        slots[SelectedIndex] = null;
        OnSlotChanged?.Invoke(SelectedIndex);
    }

    private void HandleNumberKeys()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame) Select(0);
        else if (kb.digit2Key.wasPressedThisFrame) Select(1);
        else if (kb.digit3Key.wasPressedThisFrame) Select(2);
        else if (kb.digit4Key.wasPressedThisFrame) Select(3);
        else if (kb.digit5Key.wasPressedThisFrame) Select(4);
#else
        for (int i = 0; i < slots.Length && i < 5; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { Select(i); break; }
#endif
    }

    private void HandleScrollWheel()
    {
        float scroll;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return;
        scroll = Mouse.current.scroll.ReadValue().y;
#else
        scroll = Input.mouseScrollDelta.y;
#endif
        if (Mathf.Abs(scroll) < 0.01f) return;

        int count = slots.Length;
        int next = ((SelectedIndex + (scroll > 0f ? -1 : 1)) % count + count) % count;   // 순환 이동
        Select(next);
    }
}