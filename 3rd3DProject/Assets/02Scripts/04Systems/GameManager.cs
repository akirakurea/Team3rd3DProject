using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>한 라운드의 결과 (결과 화면 UI에 표시)</summary>
public struct RoundResult
{
    public int round;
    public int totalRounds;
    public int ratsCaught;
    public int treasuresLost;
    public int treasuresTotal;
    public float elapsed;
    public bool IsPerfect => treasuresLost == 0;   // 보물을 하나도 안 털린 라운드
}

/// <summary>
/// 게임 전체 상태, 라운드 진행, 승패 조건을 관리하는 중심 매니저.
/// - 쥐는 보물을 훔쳐 탈출해도 사라지지 않고 다시 돌아온다. (쥐가 줄어드는 유일한 방법은 "잡는 것")
/// - 라운드 클리어: 그 라운드의 쥐를 전부 잡음 -> 결과 화면 -> 버튼을 누르면 다음 라운드
/// - 최종 승리: 마지막 라운드 클리어
/// - 패배: 한 라운드에서 보물이 전부 털림
/// - 이 컴포넌트는 다른 컴포넌트와 섞지 않은 전용 오브젝트에 붙일 것 (중복 시 오브젝트째 파괴됨)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // RoundClear: 마지막 쥐를 잡은 직후 잠깐 게임이 흐르는 구간 / RoundResult: 결과 화면(시간 정지)
    public enum GameState { Ready, Playing, RoundClear, RoundResult, Win, Lose }

    [Header("라운드 설정")]
    [SerializeField, Min(1)] private int totalRounds = 6;
    [SerializeField, Min(1)] private int firstRoundRats = 3;
    [SerializeField, Min(0)] private int ratsIncreasePerRound = 3;
    [SerializeField, Min(0f)] private float resultDelay = 1.5f;    // 마지막 쥐를 잡고 결과 화면이 뜨기까지 대기(초)

    [Header("보물 설정")]
    [SerializeField, Min(1)] private int treasuresPerRound = 5;

    [Header("재시작 설정")]
    [Tooltip("켜면 승리/패배 후 다시 하기 때 현재 씬을 새로 불러온다(쥐/고양이 상태까지 완전 초기화). 끄면 씬은 그대로 두고 1라운드부터 다시 시작한다. 씬이 Build Settings에 등록돼 있어야 한다.")]
    [SerializeField] private bool reloadSceneOnRestart = true;

    [Header("쥐 스폰/복귀 설정 (스포너, 쥐 AI가 읽어서 사용)")]
    [SerializeField, Min(1)] private int maxActiveRats = 5;
    [SerializeField, Min(0f)] private float spawnInterval = 3f;
    [SerializeField, Min(0f)] private float ratReturnDelay = 6f;

    public GameState State { get; private set; } = GameState.Ready;
    public int CurrentRound { get; private set; }
    public int TotalRounds => totalRounds;
    public int TreasuresPerRound => treasuresPerRound;   // TreasureManager가 스폰할 보물 수 (단일 출처)
    public int TreasuresLeft { get; private set; }
    public int RatsLeft { get; private set; }   // 아직 잡히지 않은 쥐 수 (탈출한 쥐도 포함)

    public int MaxActiveRats => maxActiveRats;
    public float SpawnInterval => spawnInterval;
    public float RatReturnDelay => ratReturnDelay;

    // UI 매니저, 쥐 스포너가 구독할 이벤트
    public event Action<GameState> OnStateChanged;
    public event Action<int, int> OnRoundStarted;   // (현재 라운드, 이번 라운드 쥐 수)
    public event Action<int> OnTreasuresChanged;
    public event Action<int> OnRatsChanged;
    public event Action<RoundResult> OnRoundResult; // 결과 화면 표시용

    private float roundElapsed;   // 이번 라운드 진행 시간 (Playing 중에만 누적)
    private WaitForSeconds resultWait;   // 결과 화면 대기 객체 (재사용)

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
        if (Instance != this) return;
        Instance = null;
        // Win/Lose/결과 화면에서 씬을 나가도 시간이 멈춘 채로 남지 않게 복구
        Time.timeScale = 1f;
    }

    private IEnumerator Start()
    {
        // 한 프레임 기다려서 스포너/UI가 이벤트를 구독할 시간을 확보 (Start 실행 순서 문제 방지)
        yield return null;
        StartGame();
    }

    private void Update()
    {
        if (State == GameState.Playing)
            roundElapsed += Time.deltaTime;
    }

    public void StartGame()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        CurrentRound = 0;
        StartNextRound();
    }

    /// <summary>
    /// 승리/패배 화면과 설정창의 "다시 하기/다시 시작" 버튼이 호출. 게임이 시작된 뒤(Ready 제외) 어느 상태에서든 동작한다.
    /// reloadSceneOnRestart가 켜져 있으면 씬을 새로 불러오고(이때 timeScale은 OnDestroy/StartGame에서 1로 복구),
    /// 꺼져 있으면 씬은 그대로 두고 1라운드부터 다시 시작한다.
    /// </summary>
    public void RestartGame()
    {
        if (State == GameState.Ready) return;

        if (reloadSceneOnRestart)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            StartGame();
        }
    }

    public int GetRatCountForRound(int round)
    {
        return firstRoundRats + (round - 1) * ratsIncreasePerRound;
    }

    private void StartNextRound()
    {
        CurrentRound++;
        TreasuresLeft = treasuresPerRound;
        RatsLeft = GetRatCountForRound(CurrentRound);
        roundElapsed = 0f;

        SetState(GameState.Playing);
        OnRoundStarted?.Invoke(CurrentRound, RatsLeft);
        OnTreasuresChanged?.Invoke(TreasuresLeft);
        OnRatsChanged?.Invoke(RatsLeft);
    }

    /// <summary>
    /// 쥐가 보물을 들고 탈출 지점에 도달했을 때 호출 (탈출 "성공" 시점).
    /// 쥐는 사라지지 않으므로 쥐 수는 줄지 않는다. 탈출 전에 잡히면 OnRatCaught()를 호출한다.
    /// </summary>
    public void OnTreasureStolen()
    {
        if (State != GameState.Playing) return;

        TreasuresLeft--;
        OnTreasuresChanged?.Invoke(TreasuresLeft);

        if (TreasuresLeft <= 0)
            SetState(GameState.Lose);
    }

    /// <summary>고양이가 쥐를 잡았을 때 호출</summary>
    public void OnRatCaught()
    {
        if (State != GameState.Playing) return;

        RatsLeft--;
        OnRatsChanged?.Invoke(RatsLeft);

        if (RatsLeft <= 0)
            OnRoundCleared();
    }

    private void OnRoundCleared()
    {
        if (CurrentRound >= totalRounds)
        {
            SetState(GameState.Win);
            return;
        }
        SetState(GameState.RoundClear);
        StartCoroutine(ShowResultRoutine());
    }

    private IEnumerator ShowResultRoutine()
    {
        if (resultWait == null) resultWait = new WaitForSeconds(resultDelay);
        yield return resultWait;

        var result = new RoundResult
        {
            round = CurrentRound,
            totalRounds = totalRounds,
            ratsCaught = GetRatCountForRound(CurrentRound),
            treasuresLost = treasuresPerRound - TreasuresLeft,
            treasuresTotal = treasuresPerRound,
            elapsed = roundElapsed
        };

        SetState(GameState.RoundResult);   // 시간 정지
        OnRoundResult?.Invoke(result);
    }

    /// <summary>결과 화면의 "다음 라운드" 버튼이 호출</summary>
    public void ContinueToNextRound()
    {
        if (State != GameState.RoundResult) return;
        StartNextRound();
    }

    private void SetState(GameState newState)
    {
        State = newState;

        // 결과 화면/최종 승패에서는 시간 정지. 이벤트보다 먼저 설정해서 구독자가 올바른 timeScale을 보게 한다.
        bool paused = State == GameState.RoundResult || State == GameState.Win || State == GameState.Lose;
        Time.timeScale = paused ? 0f : 1f;

        OnStateChanged?.Invoke(State);
    }
}