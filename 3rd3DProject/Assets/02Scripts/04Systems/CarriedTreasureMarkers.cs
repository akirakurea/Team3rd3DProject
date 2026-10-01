using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 쥐가 보물을 집으면 플레이 화면에 보물 아이콘이 뜨고, 쥐를 계속 따라다닌다.
/// - 쥐가 화면 안에 있으면 쥐(보물) 위에 아이콘이 붙어 다닌다.
/// - 화면 밖(옆, 뒤)에 있으면 아이콘이 화면 가장자리에 붙어서 쥐가 있는 방향을 알려준다.
/// - 쥐가 잡혀 보물이 제자리로 돌아가거나, 탈출에 성공해 털리면 아이콘이 사라진다.
///
/// 씬 구성 (Screen Space - Overlay Canvas 필요):
///   Canvas(항상 켜짐) - MarkerLayer(빈 오브젝트, 이 스크립트를 붙임, 화면 전체로 Stretch, Pivot 0.5/0.5)
///                         └ MarkerTemplate(Image, 보물 아이콘 스프라이트, 비활성으로 둔다)
/// 이 스크립트는 보물 이벤트(Treasure.OnPickedUp/OnReleased)만 구독하므로 GameManager와 순서 문제가 없다.
/// </summary>
[DefaultExecutionOrder(100)]   // 카메라와 쥐의 이동이 끝난 뒤에 아이콘 위치를 계산한다
public class CarriedTreasureMarkers : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Image markerTemplate;   // 복제해서 쓸 아이콘 (비활성으로 둘 것)
    [SerializeField] private Canvas canvas;          // 비우면 부모 Canvas를 자동으로 찾는다
    [SerializeField] private Camera targetCamera;    // 비우면 Camera.main

    [Header("표시")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.6f, 0f);   // 쥐 머리 위쪽
    [SerializeField, Min(0f)] private float edgePadding = 60f;                   // 화면 가장자리에서 띄울 거리(픽셀)
    [SerializeField, Min(0f)] private float pulseScale = 0.12f;                  // 아이콘이 콩닥거리는 정도 (0이면 끔)
    [SerializeField, Min(0f)] private float pulseSpeed = 8f;

    private readonly Dictionary<Treasure, RectTransform> active = new Dictionary<Treasure, RectTransform>(8);
    private readonly Stack<RectTransform> pool = new Stack<RectTransform>(8);
    private readonly List<Treasure> removeBuffer = new List<Treasure>(8);

    private RectTransform root;
    private Camera cam;

    private void Awake()
    {
        root = (RectTransform)transform;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;

        if (markerTemplate == null || canvas == null)
        {
            Debug.LogError("CarriedTreasureMarkers: Marker Template 또는 Canvas를 찾을 수 없습니다.");
            enabled = false;
            return;
        }
        markerTemplate.gameObject.SetActive(false);   // 템플릿 자체는 보이지 않게
    }

    // 정적 이벤트라서 OnEnable/OnDisable 쌍으로 구독해도 순서 문제가 없다
    private void OnEnable()
    {
        Treasure.OnPickedUp += HandlePickedUp;
        Treasure.OnReleased += HandleReleased;
    }

    private void OnDisable()
    {
        Treasure.OnPickedUp -= HandlePickedUp;
        Treasure.OnReleased -= HandleReleased;

        foreach (var marker in active.Values)
            if (marker != null) Release(marker);
        active.Clear();
    }

    private void HandlePickedUp(Treasure t)
    {
        if (t == null || active.ContainsKey(t)) return;

        RectTransform marker = pool.Count > 0 ? pool.Pop() : CreateMarker();
        marker.gameObject.SetActive(true);
        active.Add(t, marker);

        EnsureCamera();
        if (cam != null) UpdateMarker(t, marker);   // 첫 프레임부터 올바른 위치에 뜨게
    }

    private void HandleReleased(Treasure t)
    {
        if (t == null) return;
        if (active.TryGetValue(t, out RectTransform marker))
        {
            active.Remove(t);
            Release(marker);
        }
    }

    private void LateUpdate()
    {
        if (active.Count == 0) return;
        EnsureCamera();
        if (cam == null) return;

        removeBuffer.Clear();
        foreach (var pair in active)
        {
            Treasure t = pair.Key;
            // 안전장치: 보물이 사라졌거나 더 이상 들려 있지 않으면 아이콘도 정리
            if (t == null || !t.gameObject.activeInHierarchy || t.State != Treasure.TreasureState.Carried)
            {
                removeBuffer.Add(t);
                continue;
            }
            UpdateMarker(t, pair.Value);
        }

        for (int i = 0; i < removeBuffer.Count; i++)
        {
            Treasure t = removeBuffer[i];
            if (active.TryGetValue(t, out RectTransform marker))
            {
                active.Remove(t);
                Release(marker);
            }
        }
    }

    private void UpdateMarker(Treasure t, RectTransform marker)
    {
        Vector3 sp = cam.WorldToScreenPoint(t.transform.position + worldOffset);
        bool behind = sp.z < 0f;   // 카메라 뒤쪽에 있으면 화면 좌표가 반대로 뒤집혀 나온다

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 dir = new Vector2(sp.x, sp.y) - center;
        if (behind) dir = -dir;

        float halfW = Mathf.Max(1f, center.x - edgePadding);
        float halfH = Mathf.Max(1f, center.y - edgePadding);

        bool inside = !behind && Mathf.Abs(dir.x) <= halfW && Mathf.Abs(dir.y) <= halfH;
        if (!inside)
        {
            // 화면 밖이면 같은 방향의 화면 가장자리(여백 안쪽)로 붙인다
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
            float scale = Mathf.Min(
                halfW / Mathf.Max(Mathf.Abs(dir.x), 0.0001f),
                halfH / Mathf.Max(Mathf.Abs(dir.y), 0.0001f));
            dir *= scale;
        }

        // 화면 픽셀 -> Canvas 좌표 (Canvas Scaler가 있어도 맞도록 scaleFactor로 나눈다)
        marker.anchoredPosition = dir / canvas.scaleFactor;

        float pulse = pulseScale > 0f ? 1f + pulseScale * Mathf.Sin(Time.time * pulseSpeed) : 1f;
        marker.localScale = new Vector3(pulse, pulse, 1f);
    }

    private void EnsureCamera()
    {
        if (cam != null) return;
        cam = targetCamera != null ? targetCamera : Camera.main;
    }

    private RectTransform CreateMarker()
    {
        Image img = Instantiate(markerTemplate, root);
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);   // 화면 중앙 기준으로 위치를 계산하므로 앵커는 항상 중앙
        rt.pivot = new Vector2(0.5f, 0.5f);
        img.gameObject.SetActive(false);
        return rt;
    }

    private void Release(RectTransform marker)
    {
        marker.gameObject.SetActive(false);
        pool.Push(marker);
    }
}