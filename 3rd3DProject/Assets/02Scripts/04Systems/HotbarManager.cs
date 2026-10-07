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
/// - 직접 입력이 꺼졌거나 창의 포커스가 없거나 시간이 정지된 동안에는 키/휠 입력을 무시한다
///
/// 고양이 컨트롤러는 SelectedIndex / SelectedItem을 읽어서 사용하면 된다.
///   예) var item = HotbarManager.Instance.SelectedItem;
/// </summary>
public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance { get; private set; }

    [SerializeField] private HotbarItemData[] slots = new HotbarItemData[5];
    [Tooltip("끄면 숫자키·휠로 칸을 선택하지 않습니다. 코드와 UI의 Select 호출은 유지됩니다.")]
    public bool readSelectionInput = true;

    public int SlotCount => slots.Length;
    public bool HasSpace => FirstEmptySlot >= 0;
    /// <summary>가장 왼쪽 빈 칸의 번호입니다. 빈 칸이 없으면 -1입니다.</summary>
    public int FirstEmptySlot
    {
        get
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == null) return i;
            return -1;
        }
    }
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
        // 다른 입력 담당자가 선택을 제어할 때는 직접 키·휠 입력을 읽지 않습니다.
        if (!readSelectionInput || !Application.isFocused || Time.timeScale <= 0f) return;

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
    public bool TryAdd(HotbarItemData item) => TryAdd(item, out _);

    /// <summary>아이템을 넣은 칸도 반환합니다. null 또는 빈 칸 없음이면 false와 -1입니다.</summary>
    public bool TryAdd(HotbarItemData item, out int slotIndex)
    {
        slotIndex = -1;
        if (item == null) return false;
        slotIndex = FirstEmptySlot;
        if (slotIndex < 0) return false;

        slots[slotIndex] = item;
        OnSlotChanged?.Invoke(slotIndex);
        return true;
    }

    /// <summary>지정한 칸에 예상한 아이템이 있을 때만 제거합니다. 다른 아이템은 변경하지 않습니다.</summary>
    public bool TryRemove(int slotIndex, HotbarItemData expected)
    {
        if (expected == null || slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex] != expected)
            return false;

        slots[slotIndex] = null;
        OnSlotChanged?.Invoke(slotIndex);
        return true;
    }

    /// <summary>선택 칸의 아이템을 사용한 것으로 처리해 칸을 비운다.</summary>
    public void ConsumeSelected()
    {
        TryRemove(SelectedIndex, SelectedItem);
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
