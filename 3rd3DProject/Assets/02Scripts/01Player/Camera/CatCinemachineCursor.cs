using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineInputAxisController))]
public sealed class CatCinemachineCursor : MonoBehaviour
{
    CinemachineInputAxisController cameraInput;

    public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

    void Awake()
    {
        cameraInput = GetComponent<CinemachineInputAxisController>();
    }

    void Start() => SetLocked(true);

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            SetLocked(false);
        else if (!IsLocked && mouse != null && mouse.leftButton.wasPressedThisFrame)
            SetLocked(true);

        // OS나 에디터가 커서를 해제한 경우 카메라 입력도 함께 멈춥니다.
        if (cameraInput.enabled != IsLocked) cameraInput.enabled = IsLocked;
    }

    /// <summary>향후 메뉴·대화창에서 사용할 커서/카메라 입력의 단일 제어점입니다.</summary>
    public void SetLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        if (cameraInput) cameraInput.enabled = locked;
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused) SetLocked(false);
    }

    void OnDisable() => SetLocked(false);
}
