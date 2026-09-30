using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 라운드 종료 결과 화면 UI (uGUI + TextMeshPro). 잡은 쥐/털린 보물/걸린 시간을 보여주고
/// "다음 라운드" 버튼을 누르면 (카운트다운 후) 다음 라운드가 시작된다.
/// 이 스크립트는 "항상 켜져 있는" Canvas 오브젝트에 붙이고, 화면 전체 패널(panel)은 자식으로 둔다.
///
/// 시간이 정지(Time.timeScale = 0)된 상태에서 뜨므로 카운트다운/연출은 Realtime(Unscaled)으로 처리한다.
/// 카운트다운 중에도 GameManager 상태는 RoundResult 그대로라서 timeScale은 0으로 유지되고,
/// 끝나는 순간 GameManager.ContinueToNextRound()만 호출한다. (timeScale은 건드리지 않는다)
///
/// [기존 씬 호환] 기존 필드(panel, titleText, ratsText, treasureText, timeText, perfectBadge, continueButton)는
/// 그대로라서 스크립트만 교체해도 연결이 유지된다. "추가 연출" 항목은 전부 선택이며 비워 두면 건너뛴다.
/// </summary>
public class RoundResultUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject panel;

    [Header("결과 텍스트")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text ratsText;
    [SerializeField] private TMP_Text treasureText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private GameObject perfectBadge;   // "완벽 방어!" 표시(선택)

    [Header("버튼")]
    [SerializeField] private Button continueButton;     // "다음 라운드"

    [Header("추가 연출 (모두 선택)")]
    [SerializeField] private TMP_Text messageText;      // 한 줄 평가 ("하나도 안 털렸어요!" 등)
    [SerializeField] private TMP_Text nextInfoText;     // "다음 라운드: 쥐 6마리"
    [SerializeField] private Image[] cheeseIcons;       // 치즈 등급 아이콘 (보통 3개)
    [SerializeField] private Color cheeseOn = Color.white;
    [SerializeField] private Color cheeseOff = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private bool revealCheeseOneByOne = true;        // 켜면 얻은 치즈가 하나씩 차례로 나타난다
    [SerializeField, Min(0f)] private float cheeseFirstDelay = 0.3f;  // 결과 화면이 뜬 뒤 첫 치즈까지 대기(초)
    [SerializeField, Min(0f)] private float cheeseRevealInterval = 0.35f; // 치즈 사이 간격(초)
    [Tooltip("남은 보물이 이 개수 이상이면 치즈 2개 (전부 지키면 3개, 그보다 적으면 1개)")]
    [SerializeField, Min(1)] private int twoCheeseMinKept = 3;
    [SerializeField] private TMP_Text continueLabel;    // 버튼 안의 글자 (카운트다운 표시용)
    [SerializeField, Min(0)] private int countdownSeconds = 3;      // 0이면 카운트다운 없이 바로 시작
    [SerializeField, Min(0.1f)] private float countdownStep = 0.8f; // 숫자 하나당 시간(초)

    [Header("커서")]
    [Tooltip("켜면 결과 화면이 뜰 때 마우스 커서를 풀고, 닫힐 때 원래대로 되돌린다. (게임이 커서를 잠그고 있을 때 버튼을 누르려면 필요)")]
    [SerializeField] private bool manageCursor = true;

    private GameManager gm;
    private Coroutine countdownRoutine;
    private bool cursorSaved;
    private CursorLockMode prevLockState;
    private bool prevCursorVisible;

    private void Start()
    {
        panel.SetActive(false);

        // GameManager는 한 프레임 뒤 게임을 시작하므로 Start에서 구독해도 안전
        gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("RoundResultUI: GameManager를 찾을 수 없습니다.");
            return;
        }
        gm.OnRoundResult += HandleRoundResult;
        gm.OnStateChanged += HandleStateChanged;
        continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void OnDestroy()
    {
        if (gm == null) return;
        gm.OnRoundResult -= HandleRoundResult;
        gm.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        // 결과 화면이 아닌 상태로 바뀌면(다음 라운드 시작, 재시작 등) 패널을 닫는다
        if (state != GameManager.GameState.RoundResult)
            ClosePanel();
    }

    private void HandleRoundResult(RoundResult result)
    {
        titleText.text = $"라운드 {result.round} / {result.totalRounds} 클리어!";
        ratsText.text = $"잡은 쥐: {result.ratsCaught}마리";
        treasureText.text = $"털린 보물: {result.treasuresLost} / {result.treasuresTotal}";
        timeText.text = $"걸린 시간: {FormatTime(result.elapsed)}";
        if (perfectBadge != null) perfectBadge.SetActive(result.IsPerfect);

        // ── 추가 연출 ──
        int cheese = GetCheeseCount(result);
        if (messageText != null)
            messageText.text = cheese >= 3 ? "하나도 안 털렸어요!" : cheese == 2 ? "잘 막아냈어요!" : "아슬아슬하게 막았어요.";

        StartCheeseReveal(cheese);

        if (nextInfoText != null)
            nextInfoText.text = $"다음 라운드: 쥐 {gm.GetRatCountForRound(result.round + 1)}마리";

        SetContinueLabel($"라운드 {result.round + 1} 시작");
        continueButton.interactable = true;

        if (manageCursor && !cursorSaved)
        {
            cursorSaved = true;
            prevLockState = Cursor.lockState;
            prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        panel.SetActive(true);

        // Enter/Space(Submit)로도 누를 수 있도록 버튼을 선택 상태로 만든다
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
    }

    private void OnContinueClicked()
    {
        if (countdownRoutine != null) return;   // 카운트다운 중 중복 클릭 방지

        if (countdownSeconds <= 0)
        {
            gm.ContinueToNextRound();   // 상태가 바뀌면 HandleStateChanged가 패널을 닫는다
            return;
        }
        countdownRoutine = StartCoroutine(CountdownThenContinue());
    }

    private IEnumerator CountdownThenContinue()
    {
        continueButton.interactable = false;

        for (int c = countdownSeconds; c > 0; c--)
        {
            SetContinueLabel($"{c}...");
            yield return new WaitForSecondsRealtime(countdownStep);   // timeScale이 0이어도 진행
        }

        SetContinueLabel("시작!");
        countdownRoutine = null;
        gm.ContinueToNextRound();
    }

    private void ClosePanel()
    {
        StopCheeseReveal();
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }
        panel.SetActive(false);

        if (cursorSaved)
        {
            cursorSaved = false;
            Cursor.lockState = prevLockState;
            Cursor.visible = prevCursorVisible;
        }
    }

    // ───────────── 치즈 등급 연출 ─────────────

    private Coroutine cheeseRoutine;

    private void StartCheeseReveal(int count)
    {
        StopCheeseReveal();
        if (cheeseIcons == null) return;

        // 먼저 전부 흐린 빈 칸으로 만든다 (못 얻은 칸은 계속 흐리게 남는다)
        for (int i = 0; i < cheeseIcons.Length; i++)
        {
            if (cheeseIcons[i] == null) continue;
            cheeseIcons[i].color = cheeseOff;
            cheeseIcons[i].rectTransform.localScale = Vector3.one;
        }

        if (count <= 0) return;

        if (!revealCheeseOneByOne)
        {
            for (int i = 0; i < count && i < cheeseIcons.Length; i++)
                if (cheeseIcons[i] != null) cheeseIcons[i].color = cheeseOn;
            return;
        }

        cheeseRoutine = StartCoroutine(RevealCheeseRoutine(count));
    }

    private void StopCheeseReveal()
    {
        if (cheeseRoutine != null)
        {
            StopCoroutine(cheeseRoutine);
            cheeseRoutine = null;
        }
        if (cheeseIcons == null) return;
        for (int i = 0; i < cheeseIcons.Length; i++)
            if (cheeseIcons[i] != null) cheeseIcons[i].rectTransform.localScale = Vector3.one;
    }

    private IEnumerator RevealCheeseRoutine(int count)
    {
        // 결과 화면은 timeScale = 0 이므로 전부 Realtime/Unscaled로 진행
        yield return new WaitForSecondsRealtime(cheeseFirstDelay);

        for (int i = 0; i < count && i < cheeseIcons.Length; i++)
        {
            Image icon = cheeseIcons[i];
            if (icon != null)
            {
                icon.color = cheeseOn;
                yield return PopRoutine(icon.rectTransform);
            }
            yield return new WaitForSecondsRealtime(cheeseRevealInterval);
        }
        cheeseRoutine = null;
    }

    /// <summary>0에서 살짝 커졌다가 원래 크기로 돌아오는 "톡" 튀어나오는 효과</summary>
    private static IEnumerator PopRoutine(RectTransform rt)
    {
        const float duration = 0.25f;
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration) - 1f;
            float s = 1f + c3 * k * k * k + c1 * k * k;   // easeOutBack: 0 -> (약간 넘침) -> 1
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private void SetContinueLabel(string text)
    {
        if (continueLabel != null) continueLabel.text = text;
    }

    /// <summary>
    /// 치즈 등급 (남은 보물 = 안 털린 보물 기준)
    ///  - 전부 지킴(5/5)        -> 치즈 3개
    ///  - 3~4개 남음            -> 치즈 2개
    ///  - 2개 이하 남음         -> 치즈 1개
    /// </summary>
    private int GetCheeseCount(RoundResult r)
    {
        int kept = r.treasuresTotal - r.treasuresLost;
        if (kept >= r.treasuresTotal) return 3;
        if (kept >= twoCheeseMinKept) return 2;
        return 1;
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }
}