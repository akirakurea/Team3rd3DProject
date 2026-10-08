using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Unity 입력을 순수 이동 의도로 바꿉니다. 기존 Vector2 입력 API도 유지합니다.</summary>
public readonly struct PlayerInputFrame
{
    public MovementIntent Intent { get; }
    public Vector2 Move => new Vector2(Intent.Horizontal, Intent.Forward);
    public bool Sprint => Intent.Sprint;
    public bool JumpPressed { get; }
    // 정지 입력과 메뉴·포커스 상실로 차단된 입력을 구분합니다.
    public bool InputBlocked { get; }
    public bool IsMoving => Intent.IsMoving;

    public PlayerInputFrame(Vector2 move, bool sprint, bool jumpPressed = false, bool inputBlocked = false)
    {
        // 대각선 길이 제한은 Unity 기본 함수를 재사용합니다.
        Vector2 limited = Vector2.ClampMagnitude(move, 1);
        Intent = new MovementIntent(limited.x, limited.y, sprint);
        JumpPressed = jumpPressed && !inputBlocked;
        InputBlocked = inputBlocked;
    }
}

public interface IPlayerInputSource
{
    PlayerInputFrame Read();
}

/// <summary>현재 프로젝트의 Input System 키보드 입력. 커서 해제 시 이동 입력을 차단합니다.</summary>
public sealed class KeyboardInput : IPlayerInputSource, IResettableInputSource
{
    readonly InputActivationGate jumpActivation = new InputActivationGate();
    public void Reset() => jumpActivation.Reset();

    public PlayerInputFrame Read()
    {
        var keyboard = Keyboard.current;
        bool enabled = Application.isFocused && Cursor.lockState == CursorLockMode.Locked && keyboard != null;
        bool acceptsJump = jumpActivation.AllowsPress(enabled, Time.frameCount);
        if (!enabled) return new PlayerInputFrame(Vector2.zero, false, inputBlocked: true);

        var move = new Vector2(
            (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
            (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
        return new PlayerInputFrame(move, keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed,
            acceptsJump && keyboard.spaceKey.wasPressedThisFrame);
    }
}
