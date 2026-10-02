using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// [테스트 전용 - 팀원의 쥐/고양이가 완성되면 삭제]
/// 가짜 쥐 역할을 해서, 쥐/고양이 없이도 보물 + 라운드 흐름을 키보드로 검증한다.
/// TreasureCarrier가 붙은 빈 오브젝트에 같이 붙여서 사용한다.
///   F1: 가장 가까운 보물 줍기   F2: 탈출 성공(보물 털림)   F3: 잡힘(보물 복귀 + 쥐 1마리 잡음)
/// (숫자키 1~5는 핫바가 사용하므로 겹치지 않게 F1~F3을 쓴다)
/// 화면 텍스트(OnGUI)는 에디터/개발 빌드에서만 나오며, 문자열은 0.2초마다만 다시 만든다.
/// </summary>
[RequireComponent(typeof(TreasureCarrier))]
public class TreasureDebugTester : MonoBehaviour
{
    private TreasureCarrier carrier;

    private void Awake()
    {
        carrier = GetComponent<TreasureCarrier>();
    }

    private void Update()
    {
        if (Pressed(1))
        {
            var t = TreasureManager.Instance != null ? TreasureManager.Instance.GetNearestAvailable(transform.position) : null;
            if (!carrier.TryPickUp(t)) Debug.Log("[Tester] 집을 수 있는 보물이 없거나 이미 들고 있음");
        }
        if (Pressed(2))
        {
            if (!carrier.CompleteEscape()) Debug.Log("[Tester] 들고 있는 보물이 없음");
        }
        if (Pressed(3))
        {
            carrier.DropOnCaught();
            if (GameManager.Instance != null) GameManager.Instance.OnRatCaught();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RefreshLabel();
#endif
    }

    private static bool Pressed(int n)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return false;
        switch (n)
        {
            case 1: return kb.f1Key.wasPressedThisFrame;
            case 2: return kb.f2Key.wasPressedThisFrame;
            case 3: return kb.f3Key.wasPressedThisFrame;
            default: return false;
        }
#else
        return Input.GetKeyDown(KeyCode.F1 + (n - 1));
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private string label = "";
    private float nextLabelTime;

    // 시간이 정지(timeScale = 0)돼도 갱신되도록 unscaledTime 기준
    private void RefreshLabel()
    {
        if (Time.unscaledTime < nextLabelTime) return;
        nextLabelTime = Time.unscaledTime + 0.2f;

        var gm = GameManager.Instance;
        if (gm == null) { label = ""; return; }
        int available = TreasureManager.Instance != null ? TreasureManager.Instance.AvailableCount : 0;
        label = $"[테스트] F1:줍기  F2:탈출  F3:잡힘 | 상태 {gm.State} | 라운드 {gm.CurrentRound}/{gm.TotalRounds} | 남은 보물 {gm.TreasuresLeft} (제자리 {available}) | 남은 쥐 {gm.RatsLeft} | 들고있음 {carrier.IsCarrying}";
    }

    private void OnGUI()
    {
        if (label.Length == 0) return;
        GUI.Label(new Rect(10, 10, 900, 25), label);
    }
#endif
}