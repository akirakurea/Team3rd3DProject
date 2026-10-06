using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 핫바 5칸(uGUI). 선택된 칸에만 테두리를 표시한다.
///
/// 씬 구성 (보물 칸과 동일한 방식):
///   Canvas - Hotbar(빈 오브젝트, Horizontal Layout Group) - Slot 0~4
/// 각 Slot 오브젝트는:
///   Slot (Image, 배경)
///    ├ Icon (Image, 아이템 아이콘)
///    └ Border (Image, 테두리 전용 - 평소 비활성, 선택 시에만 활성화)
///
/// Border는 배경과 같은 크기에 테두리만 있는 스프라이트(가운데가 비고 외곽선만 있는 이미지)를 쓰거나,
/// Outline 컴포넌트를 Slot에 붙여서 활성/비활성으로 대체해도 된다.
///
/// 최적화: Icon/Border는 클릭을 받을 필요가 없으므로 Raycast Target을 끈다.
/// (버튼으로 쓰는 Slot 배경 Image만 켜둔다)
/// </summary>
public class HotbarUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotView
    {
        public Image icon;
        public GameObject border;   // 선택 시에만 SetActive(true)
    }

    [SerializeField] private SlotView[] slots = new SlotView[5];
    [SerializeField] private Button[] buttons = new Button[5];   // 마우스 클릭으로도 선택하고 싶으면 연결 (선택)

    private HotbarManager hb;

    private void Start()
    {
        // 모든 오브젝트의 Awake가 끝난 뒤 Start가 실행되는 걸 보장받아서, HotbarManager.Instance가 확실히 채워진 상태다
        hb = HotbarManager.Instance;
        if (hb == null)
        {
            Debug.LogWarning("HotbarUI: HotbarManager를 찾을 수 없습니다.");
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].icon != null) slots[i].icon.raycastTarget = false;
            RefreshSlot(i);
            if (slots[i].border != null)
            {
                var borderImage = slots[i].border.GetComponent<Graphic>();
                if (borderImage != null) borderImage.raycastTarget = false;
            }
            if (i < buttons.Length && buttons[i] != null)
            {
                // 클릭 후 포커스가 남아 Space/Enter로 다시 눌리거나 방향키로 이동하는 것을 막는다
                buttons[i].navigation = new Navigation { mode = Navigation.Mode.None };

                int index = i;   // 클로저 캡처용 지역 변수
                buttons[i].onClick.AddListener(() => hb.Select(index));
            }
        }

        hb.OnSelectedChanged += HandleSelectedChanged;
        hb.OnSlotChanged += RefreshSlot;
        HandleSelectedChanged(hb.SelectedIndex);
    }

    private void OnDestroy()
    {
        if (hb == null) return;
        hb.OnSelectedChanged -= HandleSelectedChanged;
        hb.OnSlotChanged -= RefreshSlot;
    }

    private void RefreshSlot(int i)
    {
        var icon = slots[i].icon;
        if (icon == null) return;   // Inspector에 Icon이 연결 안 된 칸은 건너뜀

        var item = hb.GetSlot(i);
        icon.enabled = item != null && item.icon != null;
        icon.sprite = item != null ? item.icon : null;
    }

    private void HandleSelectedChanged(int selectedIndex)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].border != null)
                slots[i].border.SetActive(i == selectedIndex);
        }
    }
}