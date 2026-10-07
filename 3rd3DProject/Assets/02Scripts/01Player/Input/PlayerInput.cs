using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Unity 입력을 순수 이동 의도로 바꿉니다. 기존 Vector2 입력 API도 유지합니다.</summary>
public readonly struct PlayerInputFrame
{
    public MovementIntent Intent { get; }
    public Vector2 Move => new Vector2(Intent.Horizontal, Intent.Forward);
    public bool Sprint => Intent.Sprint;
    public bool IsMoving => Intent.IsMoving;

    public PlayerInputFrame(Vector2 move, bool sprint)
    {
        // 대각선 길이 제한은 Unity 기본 함수를 재사용합니다.
        Vector2 limited = Vector2.ClampMagnitude(move, 1);
        Intent = new MovementIntent(limited.x, limited.y, sprint);
    }
}

public interface IPlayerInputSource
{
    PlayerInputFrame Read();
}

/// <summary>현재 프로젝트의 Input System 키보드 입력. 커서 해제 시 이동 입력을 차단합니다.</summary>
public sealed class KeyboardInput : IPlayerInputSource
{
    public PlayerInputFrame Read()
    {
        var keyboard = Keyboard.current;
        if (Cursor.lockState != CursorLockMode.Locked || keyboard == null) return default;

        var move = new Vector2(
            (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
            (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
        return new PlayerInputFrame(move, keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }
}
