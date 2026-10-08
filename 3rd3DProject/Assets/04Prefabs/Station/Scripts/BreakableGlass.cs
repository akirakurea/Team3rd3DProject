using UnityEngine;
using UnityEngine.Events;

// 창문이나 문에 있는 유리 전용 오브젝트에 붙입니다.
// AI/BT가 Break()를 호출하면 깨집니다. 단순 접촉으로는 깨지지 않습니다.
public class BreakableGlass : MonoBehaviour
{
    [Header("유리 모델")]
    [Tooltip("유리 모델만 연결하세요. 문 전체나 문틀은 연결하지 마세요.")]
    public GameObject intactVisual;
    [Tooltip("선택 사항: 깨진 유리 모델입니다.")]
    public GameObject brokenVisual;
    [Tooltip("깨졌을 때 끌 유리 전용 Collider입니다.")]
    public Collider[] glassColliders;

    [Header("파괴 효과 (선택 사항)")]
    public GameObject breakEffectPrefab;
    public Transform effectPoint;
    [Tooltip("효과를 삭제할 시간입니다. 0 이하면 자동 삭제하지 않습니다.")]
    public float effectLifetime = 3f;
    public AudioSource audioSource;
    public AudioClip breakSound;

    // 깨진 뒤 실행할 추가 기능을 Inspector에서 연결할 수 있습니다.
    public UnityEvent onBroken = new UnityEvent();

    // AI/BT에서 이미 깨졌는지 확인할 수 있습니다.
    public bool IsBroken { get; private set; }

    private void Awake()
    {
        // 시작할 때는 깨진 모델을 숨깁니다.
        if (brokenVisual != null) brokenVisual.SetActive(false);

        // 직접 연결하지 않았다면 이 오브젝트의 Collider를 사용합니다.
        if (glassColliders == null || glassColliders.Length == 0)
            glassColliders = GetComponents<Collider>();
    }

    // AI/BT의 유리 파괴 행동에서 호출하세요. 한 번만 실행됩니다.
    public void Break()
    {
        if (IsBroken) return;
        IsBroken = true;

        // 유리의 충돌을 꺼서 통과할 수 있게 합니다.
        foreach (Collider glass in glassColliders)
        {
            if (glass != null) glass.enabled = false;
        }

        // 멀쩡한 유리를 숨깁니다. 이 스크립트의 오브젝트 자체는 끄지 않습니다.
        // 같은 오브젝트라면 Renderer만 꺼서 소리와 이벤트가 실행되게 합니다.
        if (intactVisual != null && intactVisual != gameObject)
            intactVisual.SetActive(false);
        else
        {
            foreach (Renderer visual in GetComponents<Renderer>())
                visual.enabled = false;
        }

        if (brokenVisual != null) brokenVisual.SetActive(true);

        // 효과와 소리는 연결했을 때만 실행합니다.
        if (breakEffectPrefab != null)
        {
            Transform point = effectPoint != null ? effectPoint : transform;
            GameObject effect = Instantiate(breakEffectPrefab, point.position, point.rotation);
            if (effectLifetime > 0f) Destroy(effect, effectLifetime);
        }
        if (audioSource != null && breakSound != null)
            audioSource.PlayOneShot(breakSound);

        onBroken.Invoke();
    }

    // Play 중 컴포넌트 메뉴에서 AI 없이 파괴 동작을 확인할 수 있습니다.
    [ContextMenu("테스트: 유리 깨기 (Play 중)")]
    private void TestBreak()
    {
        if (Application.isPlaying) Break();
    }
}
