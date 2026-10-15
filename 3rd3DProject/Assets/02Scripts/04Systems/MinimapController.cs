using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 중심 스크롤 미니맵 (최적화 버전)
/// - 플레이어 마커는 미니맵 중앙에 고정, DotRoot 하나만 움직여서 지도가 스크롤됨
/// - 플레이어가 실제로 움직였을 때만 갱신 (임계값 이하 이동은 무시)
/// - 점은 풀링, 좌표 변환은 등록 시 1회
/// - 깜빡임은 이 스크립트가 일괄 처리 (점마다 코루틴/Update 없음)
/// - 미니맵 밖 점은 RectMask2D가 렌더링에서 제외
/// </summary>
public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance { get; private set; }

    public enum WorldPlane { XZ_3D, XY_2D }

    [Header("References")]
    [Tooltip("점들이 놓일 RectTransform. Anchor/Pivot = 중앙, 부모(MinimapPanel)에 RectMask2D 필요")]
    [SerializeField] RectTransform dotRoot;
    [Tooltip("빨간 원 Image 프리팹 (Raycast Target 해제)")]
    [SerializeField] Image dotPrefab;
    [SerializeField] Transform player;

    [Header("Map")]
    [SerializeField] WorldPlane plane = WorldPlane.XZ_3D;
    [Tooltip("월드 1m가 미니맵에서 몇 픽셀인지 (클수록 확대)")]
    [SerializeField] float pixelsPerUnit = 4f;
    [Tooltip("켜면 플레이어가 바라보는 방향이 항상 위쪽 (마커는 고정)")]
    [SerializeField] bool rotateWithPlayer = false;
    [Tooltip("이 거리(월드 단위) 이하의 이동은 갱신하지 않음")]
    [SerializeField] float moveThreshold = 0.02f;
    [Tooltip("이 각도(도) 이하의 회전은 갱신하지 않음")]
    [SerializeField] float angleThreshold = 0.5f;

    [Header("Blink")]
    [SerializeField] float blinkDuration = 1.5f;
    [SerializeField] float blinkPerSecond = 4f;

    [Header("Pool")]
    [SerializeField] int prewarm = 32;

    class Dot
    {
        public RectTransform rect;
        public CanvasRenderer cr;
        public float t;
        public bool visible = true;
    }

    readonly Dictionary<Treasure, Dot> _active = new Dictionary<Treasure, Dot>(64);
    readonly List<Dot> _blinking = new List<Dot>(16);
    readonly Stack<Dot> _pool = new Stack<Dot>(64);

    Vector2 _lastPos;
    float _lastAngle;
    bool _forceRefresh = true;
    bool _ready;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < prewarm; i++) _pool.Push(CreateDot());

        _ready = true;
        // Start 이전에 활성화된 보물 일괄 등록
        var list = Treasure.Active;
        for (int i = 0; i < list.Count; i++) Register(list[i]);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ───────────── Public API ─────────────

    public void SetPlayer(Transform p)
    {
        player = p;
        _forceRefresh = true;
    }

    public void Register(Treasure t)
    {
        if (!_ready) return;
        if (_active.ContainsKey(t)) return;

        Dot d = Rent();
        // 월드 원점 기준 좌표. 지도 스크롤은 dotRoot가 담당하므로 1회만 계산
        d.rect.anchoredPosition = ToPlane(t.transform.position) * pixelsPerUnit;
        _active.Add(t, d);
    }

    /// <summary>보물이 사라졌지만 점은 즉시 제거 (예: 플레이어가 획득)</summary>
    public void Remove(Treasure t)
    {
        if (_active.TryGetValue(t, out Dot d))
        {
            _active.Remove(t);
            Release(d);
        }
    }

    /// <summary>쥐가 가져감 → 점 깜빡임 시작 후 자동 제거</summary>
    public void BeginBlink(Treasure t)
    {
        if (!_active.TryGetValue(t, out Dot d)) return;
        _active.Remove(t);
        d.t = 0f;
        _blinking.Add(d);
    }

    // ───────────── 플레이어 추적 (스크롤) ─────────────

    void LateUpdate() // 플레이어 이동이 끝난 뒤 갱신
    {
        if (player == null) return;

        Vector2 pos = ToPlane(player.position);
        float ang = rotateWithPlayer ? GetMapAngle() : 0f;

        bool moved = (pos - _lastPos).sqrMagnitude > moveThreshold * moveThreshold;
        bool turned = Mathf.Abs(Mathf.DeltaAngle(ang, _lastAngle)) > angleThreshold;
        if (!_forceRefresh && !moved && !turned) return; // 안 움직였으면 아무것도 안 함

        _forceRefresh = false;
        _lastPos = pos;
        _lastAngle = ang;

        Vector2 offset = -pos * pixelsPerUnit; // 플레이어가 중앙에 오도록 반대로 이동

        if (rotateWithPlayer)
        {
            Quaternion q = Quaternion.Euler(0f, 0f, ang);
            dotRoot.localRotation = q;
            dotRoot.anchoredPosition = q * offset; // 회전 중심이 중앙이므로 오프셋도 같이 회전
        }
        else
        {
            dotRoot.anchoredPosition = offset;
        }
    }

    // ───────────── 깜빡임 일괄 처리 ─────────────

    void Update()
    {
        int count = _blinking.Count;
        if (count == 0) return;

        float dt = Time.deltaTime;
        float rate = blinkPerSecond * 2f; // 켜짐+꺼짐 = 1회

        for (int i = count - 1; i >= 0; i--)
        {
            Dot d = _blinking[i];
            d.t += dt;

            if (d.t >= blinkDuration)
            {
                int last = _blinking.Count - 1; // swap-remove
                _blinking[i] = _blinking[last];
                _blinking.RemoveAt(last);
                Release(d);
                continue;
            }

            bool vis = ((int)(d.t * rate) & 1) == 0;
            if (vis != d.visible)
            {
                d.visible = vis;
                d.cr.SetAlpha(vis ? 1f : 0f); // 메시 재생성 없이 알파만 변경
            }
        }
    }

    // ───────────── Helpers ─────────────

    Vector2 ToPlane(Vector3 p)
    {
        return plane == WorldPlane.XZ_3D ? new Vector2(p.x, p.z) : new Vector2(p.x, p.y);
    }

    /// <summary>플레이어 정면이 화면 위쪽이 되도록 지도를 돌릴 각도</summary>
    float GetMapAngle()
    {
        return plane == WorldPlane.XZ_3D
            ? player.eulerAngles.y      // 3D: 시계 방향 yaw → 지도는 반시계로
            : -player.eulerAngles.z;    // 2D: 위(+Y)가 정면 기준
    }

    Dot CreateDot()
    {
        Image img = Instantiate(dotPrefab, dotRoot);
        img.raycastTarget = false;
        img.gameObject.SetActive(false);
        return new Dot { rect = img.rectTransform, cr = img.canvasRenderer };
    }

    Dot Rent()
    {
        Dot d = _pool.Count > 0 ? _pool.Pop() : CreateDot();
        d.visible = true;
        d.cr.SetAlpha(1f);
        d.rect.gameObject.SetActive(true);
        return d;
    }

    void Release(Dot d)
    {
        d.rect.gameObject.SetActive(false);
        _pool.Push(d);
    }
}