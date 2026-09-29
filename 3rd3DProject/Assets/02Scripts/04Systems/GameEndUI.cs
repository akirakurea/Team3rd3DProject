using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 최종 승리 / 패배 화면 UI (uGUI + TextMeshPro). "다시 하기" 버튼을 누르면 게임이 처음부터 다시 시작된다.
/// RoundResultUI와 같은 방식: "항상 켜져 있는" Canvas 오브젝트에 붙이고, 화면 전체 패널(panel)은 자식으로 둔다.
/// Win/Lose 상태에서는 Time.timeScale = 0 이지만 uGUI 버튼은 정상 동작한다. (씬에 EventSystem 필요)
///
/// 씬 구성:
///   Canvas(항상 켜짐, 이 스크립트) - Panel(기본 비활성) - Title(TMP), Message(TMP), RestartButton(Button)
/// </summary>
public class GameEndUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject panel;

    [Header("텍스트")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;

    [Header("버튼")]
    [SerializeField] private Button restartButton;   // "다시 하기"

    [Header("문구")]
    [SerializeField] private string winTitle = "승리!";
    [SerializeField] private string loseTitle = "패배...";

    private GameManager gm;

    private void Start()
    {
        panel.SetActive(false);

        // GameManager는 한 프레임 뒤 게임을 시작하므로 Start에서 구독해도 안전
        gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("GameEndUI: GameManager를 찾을 수 없습니다.");
            return;
        }
        gm.OnStateChanged += HandleStateChanged;
        restartButton.onClick.AddListener(Restart);
    }

    private void OnDestroy()
    {
        if (gm == null) return;
        gm.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Win:
                titleText.text = winTitle;
                messageText.text = $"{gm.TotalRounds}개 라운드를 모두 막아냈어요!";
                panel.SetActive(true);
                break;

            case GameManager.GameState.Lose:
                titleText.text = loseTitle;
                messageText.text = $"라운드 {gm.CurrentRound} / {gm.TotalRounds}에서 보물을 모두 털렸어요";
                panel.SetActive(true);
                break;

            default:
                // 재시작 등으로 Win/Lose가 아닌 상태가 되면 패널을 닫는다
                panel.SetActive(false);
                break;
        }
    }

    private void Restart()
    {
        // 씬을 다시 불러오는 경우 이 오브젝트도 새로 만들어지므로 패널은 굳이 닫지 않아도 된다
        gm.RestartGame();
    }
}