using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임플레이 화면 하단 중앙의 보물 칸 5개(uGUI). 결과 창(RoundResultUI)과 동일한 uGUI로 통일했다.
///
/// 씬 구성:
///   Canvas(항상 켜짐) - TreasureBar(빈 오브젝트, Horizontal Layout Group) - Slot 0~4(Image 5개)
/// 이 스크립트는 TreasureBar 오브젝트에 붙이고, slots 배열에 Slot 0~4의 Image를 순서대로 드래그한다.
///
/// 최적화 포인트:
/// - 슬롯 5개를 매번 새로 만들지 않고 씬에 고정해두고 참조만 쓴다 (런타임 Instantiate 없음)
/// - 값이 바뀔 때(색상 교체)만 갱신하고, 매 프레임 갱신하지 않는다
/// - 클릭을 받지 않는 Image는 Raycast Target을 꺼서 UI 레이캐스트 비용을 줄인다
/// - 자주 바뀌는 HUD는 정적인 UI와 다른 (자식) Canvas에 두면 리빌드 범위가 줄어든다
/// </summary>
public class TreasureHUD : MonoBehaviour
{
    [SerializeField] private Image[] slots = new Image[5];   // Slot 0~4 순서대로 연결

    [Header("색상")]
    [SerializeField] private Color filledColor = new Color(1f, 0.8f, 0.16f);   // 금색: 보물 안전
    [SerializeField] private Color lostColor = new Color(0.86f, 0.24f, 0.2f);  // 빨강: 방금 털림
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 회색: 빈 칸

    [SerializeField, Min(0.1f)] private float lostFlashDuration = 0.4f;   // "방금 털림" 강조 표시 시간(초)

    private GameManager gm;
    private int previousLeft = -1;
    private Coroutine flashRoutine;

    private void Start()
    {
        // 씬의 모든 오브젝트는 Awake가 전부 끝난 뒤에 Start가 실행되는 게 Unity가 보장하는 순서라서,
        // GameManager.Awake()에서 Instance가 채워진 뒤에 안전하게 구독할 수 있다.
        // (OnEnable은 다른 오브젝트의 Awake보다 먼저 실행될 수 있어 Instance가 아직 null일 위험이 있다)
        // Start에서 구독했으니 해제도 OnDestroy에서 한다. (OnDisable에서 해제하면 다시 켜졌을 때 재구독이 안 된다)
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null) slots[i].raycastTarget = false;

        gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("TreasureHUD: GameManager를 찾을 수 없습니다.");
            return;
        }
        gm.OnTreasuresChanged += HandleTreasuresChanged;
        gm.OnRoundStarted += HandleRoundStarted;

        // 이미 라운드가 진행 중일 때 늦게 활성화된 경우를 대비해 현재 값으로 한 번 초기화
        Refresh(gm.TreasuresLeft, instant: true);
    }

    private void OnDestroy()
    {
        if (gm == null) return;
        gm.OnTreasuresChanged -= HandleTreasuresChanged;
        gm.OnRoundStarted -= HandleRoundStarted;
    }

    private void HandleRoundStarted(int round, int ratCount)
    {
        StopFlash();   // 이전 라운드의 깜빡임 코루틴이 새 라운드 색을 덮어쓰지 않게
        previousLeft = -1;
        Refresh(gm.TreasuresPerRound, instant: true);   // 새 라운드는 5칸 전부 채워서 시작
    }

    private void HandleTreasuresChanged(int left)
    {
        Refresh(left, instant: false);
    }

    private void Refresh(int left, bool instant)
    {
        int total = slots.Length;
        left = Mathf.Clamp(left, 0, total);

        // 왼쪽부터 left개는 채움, 나머지는 빈 칸. 방금 줄어든 칸만 잠깐 강조한다.
        int justLostIndex = (!instant && previousLeft > left) ? left : -1;

        for (int i = 0; i < total; i++)
        {
            if (slots[i] == null) continue;
            slots[i].color = (i == justLostIndex) ? lostColor : (i < left ? filledColor : emptyColor);
        }

        if (justLostIndex >= 0)
        {
            StopFlash();
            flashRoutine = StartCoroutine(SettleAfterFlash(justLostIndex));
        }

        previousLeft = left;
    }

    private void StopFlash()
    {
        if (flashRoutine == null) return;
        StopCoroutine(flashRoutine);
        flashRoutine = null;
    }

    private IEnumerator SettleAfterFlash(int index)
    {
        // 마지막 보물이 털리면 GameManager가 timeScale을 0으로 만든다.
        // 스케일 시간을 쓰면 코루틴이 영영 안 끝나므로 실시간(Realtime)으로 기다린다.
        yield return new WaitForSecondsRealtime(lostFlashDuration);
        if (index < slots.Length && slots[index] != null)
            slots[index].color = emptyColor;
        flashRoutine = null;
    }
}