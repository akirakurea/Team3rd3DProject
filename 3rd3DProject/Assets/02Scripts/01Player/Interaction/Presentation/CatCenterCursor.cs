using UnityEngine;

/// <summary>카메라 중심에 고정된 작은 반투명 구체. 충돌과 입력을 받지 않습니다.</summary>
[DefaultExecutionOrder(11000)]
public sealed class CatCenterCursor : MonoBehaviour
{
    public Camera view;
    public Transform sphere;
    [Range(4, 24)] public float diameterPixels = 6;
    void LateUpdate()
    {
        if (sphere == null || view == null) return;
        bool visible = Cursor.lockState == CursorLockMode.Locked;
        sphere.gameObject.SetActive(visible);
        if (!visible) return;
        float depth = Mathf.Max(view.nearClipPlane + .08f, .2f);
        sphere.position = view.transform.position + view.transform.forward * depth;
        sphere.rotation = view.transform.rotation;
        float height = view.orthographic ? 2 * view.orthographicSize : 2 * depth * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
        sphere.localScale = Vector3.one * (height * diameterPixels / Mathf.Max(1, view.pixelHeight));
    }
    void OnDisable() { if (sphere != null) sphere.gameObject.SetActive(false); }
}
