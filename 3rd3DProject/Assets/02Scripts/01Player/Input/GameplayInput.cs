using System;
using UnityEngine;

/// <summary>게임 상태·메뉴 요청을 Unity 입력에 연결하는 공통 진입점. Cursor는 여기서 변경하지 않는다.</summary>
public static class GameplayInput
{
    static InputPermission permission = new InputPermission();
    static int suppressedFrame = -1;
    public static event Action PermissionChanged;

    // 현재 Instance를 읽으므로 GameUI가 늦게 로드되거나 GameManager가 교체되어도 구독이 남지 않는다.
    public static bool StateAllowsGameplay
    {
        get
        {
            var game = GameManager.Instance;
            // GameManager가 없는 단독 플레이어 시험 씬의 기존 조작을 유지한다.
            return !game || game.State == GameManager.GameState.Playing ||
                game.State == GameManager.GameState.RoundClear;
        }
    }

    public static bool AllowsGameplay => permission.AllowsGameplay(StateAllowsGameplay, Time.timeScale > 0f);
    public static bool CanCaptureCursor => AllowsGameplay && Application.isFocused;
    public static bool IsEnabled => CanCaptureCursor && Cursor.lockState == CursorLockMode.Locked &&
        Time.frameCount > suppressedFrame;

    /// <summary>창을 열 때 true, 닫거나 비활성화할 때 false. 같은 owner로 짝을 맞춘다.</summary>
    public static void SetBlocked(object owner, bool blocked)
    {
        if (!permission.SetBlocked(owner, blocked)) return;
        SuppressCurrentFrame();
        PermissionChanged?.Invoke();
    }

    // 메뉴를 닫거나 커서를 다시 잡은 클릭을 발사·집기·점프로 재사용하지 않는다.
    public static void SuppressCurrentFrame() => suppressedFrame = Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        permission = new InputPermission();
        suppressedFrame = -1;
        PermissionChanged = null;
    }
}
