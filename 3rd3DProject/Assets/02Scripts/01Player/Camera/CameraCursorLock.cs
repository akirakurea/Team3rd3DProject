using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineInputAxisController))]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "CatCinemachineCursor")]
public sealed class CameraCursorLock : MonoBehaviour
{
    CinemachineInputAxisController cameraInput;
    bool wasAllowed;
    bool capturePending;
    int captureRequestedFrame;

    public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

    void Awake()
    {
        cameraInput = GetComponent<CinemachineInputAxisController>();
    }

    void OnEnable()
    {
        GameplayInput.PermissionChanged += RefreshPermission;
        wasAllowed = false;
        RefreshPermission();
    }

    void Update()
    {
        RefreshPermission();
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            GameplayInput.SuppressCurrentFrame();
            // 메뉴가 먼저 닫힌 실행 순서에서도 그 복귀 요청을 지우지 않는다.
            if (!capturePending) SetLocked(false);
        }
        else if (!IsLocked && GameplayInput.CanCaptureCursor && mouse != null && mouse.leftButton.wasPressedThisFrame)
            SetLocked(true);

        // 에디터 자체의 ESC 커서 해제보다 나중에 적용한다. Alt+Tab 해제는 강제로 되잠그지 않는다.
        if (capturePending)
        {
            if (!GameplayInput.CanCaptureCursor) capturePending = false;
            else if (Time.frameCount > captureRequestedFrame && (keyboard == null || !keyboard.escapeKey.isPressed))
                SetLocked(true);
        }

        // OS나 에디터가 커서를 해제한 경우 카메라 입력도 함께 멈춥니다.
        if (cameraInput) cameraInput.enabled = GameplayInput.IsEnabled;
    }

    void RefreshPermission()
    {
        bool allowed = GameplayInput.AllowsGameplay;
        if (!allowed) SetLocked(false);
        else if (!wasAllowed && Application.isFocused)
        {
            capturePending = true;
            captureRequestedFrame = Time.frameCount;
            GameplayInput.SuppressCurrentFrame();
        }
        wasAllowed = allowed;
    }

    /// <summary>실제 커서 제어점. 메뉴 표시 중의 잠금 요청은 거부한다.</summary>
    public void SetLocked(bool locked)
    {
        capturePending = false;
        locked &= GameplayInput.CanCaptureCursor;
        if (locked && !IsLocked) GameplayInput.SuppressCurrentFrame();
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        if (cameraInput) cameraInput.enabled = locked && GameplayInput.IsEnabled;
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused) SetLocked(false);
    }

    void OnDisable()
    {
        GameplayInput.PermissionChanged -= RefreshPermission;
        SetLocked(false);
    }
}
