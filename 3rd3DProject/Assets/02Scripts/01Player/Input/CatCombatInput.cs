using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>장치와 무관한 조준·발사 입력 값. 검증 시 같은 값으로 입력을 대체할 수 있습니다.</summary>
public readonly struct CatCombatInputFrame
{
    public readonly bool Enabled, AimHeld, FirePressed;

    public CatCombatInputFrame(bool enabled, bool aimHeld, bool firePressed)
    { Enabled = enabled; AimHeld = aimHeld; FirePressed = firePressed; }
}

public interface ICatCombatInputSource
{
    CatCombatInputFrame Read();
}

/// <summary>우클릭 유지와 좌클릭 누름을 읽는 Unity 연결부. 재잠금 클릭은 발사로 사용하지 않습니다.</summary>
public sealed class CatCombatInput : ICatCombatInputSource
{
    bool wasEnabled;
    int enabledSinceFrame;

    /// <summary>포커스 상실·비활성화 중 Read가 멈춰도 복귀 클릭으로 오발하지 않도록 다시 준비합니다.</summary>
    public void Reset()
    {
        wasEnabled = false;
        enabledSinceFrame = 0;
    }

    public CatCombatInputFrame Read()
    {
        var mouse = Mouse.current;
        bool enabled = Application.isFocused && Cursor.lockState == CursorLockMode.Locked && mouse != null;
        if (!enabled)
        {
            wasEnabled = false;
            return default;
        }

        // 커서 제어(-100)가 클릭으로 잠근 뒤 전투 입력(-90)이 실행됩니다.
        // 최초 활성 프레임 전체를 제외하여 같은 프레임에 두 번 읽어도 오발하지 않습니다.
        if (!wasEnabled) enabledSinceFrame = Time.frameCount;
        wasEnabled = true;
        bool firePressed = Time.frameCount > enabledSinceFrame && mouse.leftButton.wasPressedThisFrame;
        return new CatCombatInputFrame(true, mouse.rightButton.isPressed, firePressed);
    }
}
