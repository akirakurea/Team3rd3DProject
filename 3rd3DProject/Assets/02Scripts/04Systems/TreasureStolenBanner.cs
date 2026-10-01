using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 쥐가 보물을 들고 탈출에 성공하면 화면에 크게 "보물 1개가 탈취되었습니다!!"를 띄운다.
/// - 처음에 커졌다가 제자리로 줄어드는 "팝" 연출 -> 잠깐 유지 -> 서서히 사라짐
/// - 연달아 털리면 배너가 다시 처음부터 재생된다
/// - 마지막 보물이 털려서 패배하는 경우는 패배 화면이 뜨므로 배너를 띄우지 않는다
/// - 결과/승리/패배 등 상태가 바뀌면 즉시 사라진다
///
/// 씬 구성: "항상 켜져 있는" Canvas 오브젝트에 이 스크립트를 붙이고,
///   Banner(CanvasGroup 컴포넌트가 있는 오브젝트, 화면 위쪽 가운데) - Message(TMP), SubMessage(TMP, 선택)
/// Banner 오브젝트는 켜 둔 채로 둔다. (알파 0으로 숨기며, 이 스크립트가 알파로 켜고 끈다)
/// </summary>
public class TreasureStolenBanner : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CanvasGroup group;      // 배너 전체 (알파와 크기 연출 대상)
    [SerializeField] private TMP_Text messageText;   // 큰 문구
    [SerializeField] private TMP_Text subText;       // 선택: "남은 보물 4개"

    [Header("문구")]
    [SerializeField] private string message = "보물 1개가 탈취되었습니다!!";

    [Header("연출")]
    [SerializeField, Min(0.5f)] private float showDuration = 2f;      // 전체 표시 시간(초)
    [SerializeField, Min(0.05f)] private float popDuration = 0.2f;    // 커졌다가 줄어드는 시간
    [SerializeField, Min(0.05f)] private float fadeOutDuration = 0.4f;
    [SerializeField, Min(1f)] private float popStartScale = 1.6f;

    private GameManager gm;
    private Coroutine routine;

    private void Start()
    {
        if (group == null || messageText == null)
        {
            Debug.LogError("TreasureStolenBanner: Group 또는 Message Text가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        gm = GameManager.Instance;
        if (gm != null) gm.OnStateChanged += HandleStateChanged;
    }

    private void OnEnable()
    {
        Treasure.OnStolen += HandleStolen;
    }

    private void OnDisable()
    {
        Treasure.OnStolen -= HandleStolen;
    }

    private void OnDestroy()
    {
        if (gm != null) gm.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStolen(Treasure t)
    {
        if (!enabled) return;

        // 패배를 일으킨 마지막 보물이거나 결과/승리 화면 중이면 배너를 띄우지 않는다
        if (gm != null && gm.State != GameManager.GameState.Playing) return;

        messageText.text = message;
        if (subText != null && gm != null) subText.text = $"남은 보물 {gm.TreasuresLeft}개";

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        // 상태가 바뀌면(결과 화면, 승리, 패배, 새 라운드) 떠 있던 배너는 바로 숨긴다
        HideNow();
    }

    private void HideNow()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        if (group == null) return;
        group.alpha = 0f;
        group.transform.localScale = Vector3.one;
    }

    private IEnumerator ShowRoutine()
    {
        float t = 0f;
        while (t < showDuration)
        {
            t += Time.deltaTime;   // 게임이 멈추면(설정창) 배너도 같이 멈춘다

            float pop = Mathf.Clamp01(t / popDuration);
            float easedPop = 1f - (1f - pop) * (1f - pop);              // ease-out
            float scale = Mathf.Lerp(popStartScale, 1f, easedPop);
            float fadeOut = Mathf.Clamp01((showDuration - t) / fadeOutDuration);

            group.alpha = Mathf.Min(pop, fadeOut);
            group.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        group.alpha = 0f;
        group.transform.localScale = Vector3.one;
        routine = null;
    }
}