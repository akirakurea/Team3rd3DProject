using UnityEngine;
using UnityEngine.InputSystem;

public interface ICatInteractionInputSource { CatHandInput Read(); }

/// <summary>Unity 장치 상태를 엔진 독립 명령 값으로 바꾸는 연결부.</summary>
public sealed class CatInteractionInput : ICatInteractionInputSource
{
    public CatHandInput Read()
    {
        bool enabled = Cursor.lockState == CursorLockMode.Locked;
        var mouse = Mouse.current;
        return new CatHandInput(enabled,
            enabled && mouse != null && mouse.rightButton.wasPressedThisFrame,
            enabled && mouse != null && mouse.rightButton.isPressed,
            enabled && Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame);
    }
}
