using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>장치와 무관한 조준·발사 입력 값. 검증 시 같은 값으로 입력을 대체할 수 있습니다.</summary>
public readonly struct CombatInputFrame
{
    public readonly bool Enabled, AimHeld, FirePressed;

    public CombatInputFrame(bool enabled, bool aimHeld, bool firePressed)
    { Enabled = enabled; AimHeld = aimHeld; FirePressed = firePressed; }
}

public interface ICombatInputSource
{
    CombatInputFrame Read();
}

/// <summary>포커스 상실·비활성화 때 내부 상태를 비워야 하는 입력 공급자만 구현합니다.</summary>
public interface IResettableInputSource
{
    void Reset();
}

/// <summary>입력이 다시 활성화된 첫 프레임의 클릭을 소비합니다. 장치나 Unity 상태를 직접 읽지 않습니다.</summary>
internal sealed class InputActivationGate
{
    bool wasEnabled;
    int enabledSinceFrame;

    public bool AllowsPress(bool enabled, int frame)
    {
        if (!enabled)
        {
            Reset();
            return false;
        }

        if (!wasEnabled) enabledSinceFrame = frame;
        wasEnabled = true;
        // 한 프레임에 여러 번 읽어도 재잠금용 클릭은 집기·발사로 전달하지 않습니다.
        return frame > enabledSinceFrame;
    }

    public void Reset()
    {
        wasEnabled = false;
        enabledSinceFrame = 0;
    }
}

/// <summary>우클릭 유지와 좌클릭 누름을 읽는 Unity 연결부. 재잠금 클릭은 발사로 사용하지 않습니다.</summary>
public sealed class CombatInput : ICombatInputSource, IResettableInputSource
{
    readonly InputActivationGate activation = new InputActivationGate();

    /// <summary>포커스 상실·비활성화 중 Read가 멈춰도 복귀 클릭으로 오발하지 않도록 다시 준비합니다.</summary>
    public void Reset() => activation.Reset();

    public CombatInputFrame Read()
    {
        var mouse = Mouse.current;
        bool enabled = Application.isFocused && Cursor.lockState == CursorLockMode.Locked && mouse != null;
        bool acceptsPress = activation.AllowsPress(enabled, Time.frameCount);
        if (!enabled) return default;

        bool firePressed = acceptsPress && mouse.leftButton.wasPressedThisFrame;
        return new CombatInputFrame(true, mouse.rightButton.isPressed, firePressed);
    }
}
