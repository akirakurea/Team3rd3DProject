using UnityEngine;

/// <summary>기존 라운드 결과를 구독해 3치즈일 때 빈 인벤토리 칸에 덫 한 개를 지급합니다.</summary>
[DisallowMultipleComponent]
public sealed class TrapRoundReward : MonoBehaviour
{
    public HotbarManager inventory;
    public HotbarItemData trapItem;

    GameManager game;
    int lastProcessedRound;

    void OnEnable() { BindGame(); }
    // 모든 Awake가 끝난 뒤 GameManager를 다시 확인합니다.
    void Start() { BindGame(); }

    void BindGame()
    {
        var current = GameManager.Instance;
        if (game == current) return;
        UnbindGame();
        game = current;
        if (!game) return;
        game.OnRoundStarted += HandleRoundStarted;
        game.OnRoundResult += HandleRoundResult;
        game.OnStateChanged += HandleStateChanged;
    }

    void HandleRoundStarted(int round, int ratCount)
    {
        if (round == 1) lastProcessedRound = 0;
    }

    void HandleRoundResult(RoundResult result)
    {
        // 기존 결과 UI의 3치즈 조건: 보물을 하나도 잃지 않음.
        GrantOnce(result.round, result.treasuresTotal > 0 && result.IsPerfect);
    }

    void HandleStateChanged(GameManager.GameState state)
    {
        // 마지막 라운드는 OnRoundResult 없이 Win으로 바로 이동합니다.
        if (state != GameManager.GameState.Win || !game) return;
        // 최종 승리도 기존 결과 형식으로 바꿔 같은 완벽 방어 판정을 사용합니다.
        HandleRoundResult(new RoundResult
        {
            round = game.CurrentRound,
            treasuresTotal = game.TreasuresPerRound,
            treasuresLost = game.TreasuresPerRound - game.TreasuresLeft
        });
    }

    void GrantOnce(int round, bool perfect)
    {
        if (round <= lastProcessedRound) return;
        lastProcessedRound = round;
        if (perfect && inventory && trapItem)
            inventory.TryAdd(trapItem); // 가득 찬 경우 보상을 보류하거나 기존 물건을 교체하지 않습니다.
    }

    void UnbindGame()
    {
        if (game)
        {
            game.OnRoundStarted -= HandleRoundStarted;
            game.OnRoundResult -= HandleRoundResult;
            game.OnStateChanged -= HandleStateChanged;
        }
        game = null;
    }

    void OnDisable() { UnbindGame(); }
}
