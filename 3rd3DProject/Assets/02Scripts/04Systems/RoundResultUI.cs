using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 종료 결과 화면 UI (uGUI + TextMeshPro). 잡은 쥐/털린 보물/걸린 시간을 보여주고
/// "다음 라운드" 버튼을 누르면 다음 라운드가 시작된다.
/// 이 스크립트는 "항상 켜져 있는" Canvas 오브젝트에 붙이고, 화면 전체 패널(panel)은 자식으로 둔다.
/// 시간이 정지(Time.timeScale = 0)된 상태에서 뜨므로, 애니메이션을 넣는다면 Unscaled Time으로 만들어야 한다.
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

    private GameManager gm;

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
        continueButton.onClick.AddListener(Continue);
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
            panel.SetActive(false);
    }

    private void HandleRoundResult(RoundResult result)
    {
        titleText.text = $"라운드 {result.round} / {result.totalRounds} 클리어!";
        ratsText.text = $"잡은 쥐: {result.ratsCaught}마리";
        treasureText.text = $"털린 보물: {result.treasuresLost} / {result.treasuresTotal}";
        timeText.text = $"걸린 시간: {FormatTime(result.elapsed)}";
        if (perfectBadge != null) perfectBadge.SetActive(result.IsPerfect);

        panel.SetActive(true);
    }

    private void Continue()
    {
        panel.SetActive(false);
        gm.ContinueToNextRound();
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }
}