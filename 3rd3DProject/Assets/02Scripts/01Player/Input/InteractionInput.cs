using UnityEngine;
using UnityEngine.InputSystem;

public interface IInteractionInputSource
{
    HandInput Read();
}

/// <summary>Unity 장치 상태를 엔진 독립 명령 값으로 바꾸는 연결부.</summary>
public sealed class InteractionInput : IInteractionInputSource, IResettableInputSource
{
    readonly InputActivationGate activation = new InputActivationGate();

    public void Reset() => activation.Reset();

    public HandInput Read()
    {
        bool enabled = Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
        bool acceptsPress = activation.AllowsPress(enabled, Time.frameCount);
        var mouse = Mouse.current;
        return new HandInput(enabled,
            acceptsPress && mouse != null && mouse.leftButton.wasPressedThisFrame,
            enabled && mouse != null && mouse.leftButton.isPressed,
            enabled && Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame);
    }
}
