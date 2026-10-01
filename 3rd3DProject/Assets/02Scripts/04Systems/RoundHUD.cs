using TMPro;
using UnityEngine;

/// <summary>
/// 게임플레이 화면의 "라운드 N / M"과 "남은 쥐 N마리" 표시 (uGUI + TextMeshPro).
/// GameManager 이벤트로만 갱신하므로 매 프레임 계산이 없다. (값이 바뀔 때만 글자를 바꾼다)
///
/// 씬 구성: "항상 켜져 있는" Canvas 오브젝트(예: MinimapCanvas)에 이 스크립트를 붙이고,
/// 자식으로 TMP 텍스트 2개를 만들어 roundText, ratsText에 연결한다. (화면 모서리 앵커를 미니맵과 맞추면 해상도가 바뀌어도 안 어긋난다)
/// </summary>
public class RoundHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;   // "라운드 1 / 6"
    [SerializeField] private TMP_Text ratsText;    // "남은 쥐 3마리"

    private GameManager gm;

    private void Start()
    {
        // 클릭을 받을 필요가 없는 텍스트는 Raycast Target을 꺼서 UI 레이캐스트 비용을 줄인다
        if (roundText != null) roundText.raycastTarget = false;
        if (ratsText != null) ratsText.raycastTarget = false;

        gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("RoundHUD: GameManager를 찾을 수 없습니다.");
            return;
        }
        gm.OnRoundStarted += HandleRoundStarted;
        gm.OnRatsChanged += SetRats;

        // GameManager는 한 프레임 뒤 게임을 시작하므로 보통은 0이지만,
        // 이미 라운드가 진행 중일 때 늦게 활성화된 경우를 대비해 현재 값으로 한 번 초기화
        if (gm.CurrentRound > 0)
        {
            SetRound(gm.CurrentRound);
            SetRats(gm.RatsLeft);
        }
    }

    private void OnDestroy()
    {
        if (gm == null) return;
        gm.OnRoundStarted -= HandleRoundStarted;
        gm.OnRatsChanged -= SetRats;
    }

    private void HandleRoundStarted(int round, int ratCount)
    {
        SetRound(round);
        SetRats(ratCount);
    }

    private void SetRound(int round)
    {
        if (roundText != null) roundText.text = $"라운드 {round} / {gm.TotalRounds}";
    }

    private void SetRats(int left)
    {
        if (ratsText != null) ratsText.text = $"남은 쥐 {left}마리";
    }
}