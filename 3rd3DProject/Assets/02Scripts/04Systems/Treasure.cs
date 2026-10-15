using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보물 한 개. 상태: Available(제자리) -> Carried(쥐가 듦) -> Stolen(탈출 성공, 사라짐)
/// 쥐가 탈출 전에 잡히면 Carried -> Available(제자리로 복귀).
/// 쥐 AI(BT)는 이 클래스를 "읽기"만 한다 (목표 보물 탐색용: IsAvailable, State, transform.position).
/// 보물의 상태를 바꾸는 호출(PickUp / ReturnHome / Steal)은 직접 하지 말고 반드시 TreasureCarrier를 통해서 한다.
/// 보물 종류는 "모양"만 다르다 (visuals 중 하나를 라운드마다 무작위로 켠다). 가치(점수) 차이는 확장 요소로 보류.
///
/// 미니맵 연동:
/// - 활성화된 보물은 static Active 리스트에 등록된다 (MinimapController가 Start 시점에 일괄 등록할 때 사용)
/// - 배치(Init)/복귀(ReturnHome)되면 점을 (재)등록, 쥐가 집으면(PickUp) 점이 깜빡이다 사라진다
/// </summary>
public class Treasure : MonoBehaviour
{
    public enum TreasureState { Available, Carried, Stolen }

    /// <summary>현재 활성화된 보물 목록 (MinimapController가 읽는다)</summary>
    public static readonly List<Treasure> Active = new List<Treasure>(16);

    /// <summary>쥐가 보물을 집었을 때 (화면 추적 아이콘 UI가 구독)</summary>
    public static event Action<Treasure> OnPickedUp;
    /// <summary>들고 있던 보물이 놓였을 때: 잡혀서 제자리 복귀 / 탈출 성공 / 라운드 초기화</summary>
    public static event Action<Treasure> OnReleased;
    /// <summary>탈출에 성공해 털렸을 때 (GameManager가 처리한 뒤 호출되므로 GameManager 상태가 이미 갱신돼 있다)</summary>
    public static event Action<Treasure> OnStolen;

    public TreasureState State { get; private set; } = TreasureState.Available;
    public TreasureCarrier Carrier { get; private set; }
    public bool IsAvailable => State == TreasureState.Available;

    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Transform homeParent;
    private Collider[] colliders;

    // 모양 후보: 프리펩 자식으로 모양 오브젝트를 여러 개 넣어두고, Init 때마다 하나만 켠다.
    // 비워두면 아무것도 바꾸지 않는다 (모양이 하나뿐인 기존 프리펩도 그대로 동작).
    [SerializeField] private GameObject[] visuals;

    // Enter Play Mode Options에서 Domain Reload를 꺼둔 경우 static이 남아있는 것을 방지
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active.Clear();
        OnPickedUp = null;
        OnReleased = null;
        OnStolen = null;
    }

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>(true);
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
        var minimap = MinimapController.Instance;
        if (minimap != null) minimap.Remove(this);
    }

    /// <summary>라운드 시작 시 TreasureManager가 호출: 지정한 위치에 새로 놓는다.</summary>
    public void Init(Vector3 position, Quaternion rotation, Transform parent)
    {
        // 쥐가 들고 있는 채로 라운드가 새로 시작되면 화면 추적 아이콘도 정리되게 알린다
        if (State == TreasureState.Carried) OnReleased?.Invoke(this);

        homeParent = parent;
        homePosition = position;
        homeRotation = rotation;
        Carrier = null;
        State = TreasureState.Available;

        transform.SetParent(parent, false);
        transform.SetPositionAndRotation(position, rotation);
        SetColliders(true);
        ApplyRandomVisual();   // 라운드마다 모양을 새로 고른다
        gameObject.SetActive(true);

        // 재사용 보물은 이미 활성 상태라 OnEnable이 다시 불리지 않는다.
        // 위치가 바뀌었으므로 점을 지웠다가 새 위치로 다시 등록한다.
        RegisterOnMinimap(removeFirst: true);
    }

    /// <summary>쥐가 집어 든다. 이미 누가 들었거나 도난당했으면 false.</summary>
    public bool PickUp(TreasureCarrier carrier)
    {
        if (State != TreasureState.Available || carrier == null) return false;

        Carrier = carrier;
        State = TreasureState.Carried;
        SetColliders(false);   // 들고 있는 동안은 충돌/길막 방지

        transform.SetParent(carrier.CarryPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        var minimap = MinimapController.Instance;
        if (minimap != null) minimap.BeginBlink(this);   // 점이 깜빡이다 사라진다

        OnPickedUp?.Invoke(this);
        return true;
    }

    /// <summary>쥐가 탈출 전에 잡혔을 때: 원래 자리로 돌려놓는다.</summary>
    public void ReturnHome()
    {
        if (State != TreasureState.Carried) return;

        Carrier = null;
        State = TreasureState.Available;
        transform.SetParent(homeParent, false);
        transform.SetPositionAndRotation(homePosition, homeRotation);
        SetColliders(true);

        RegisterOnMinimap(removeFirst: false);   // 제자리로 돌아왔으니 점 복구
        OnReleased?.Invoke(this);
    }

    /// <summary>쥐가 탈출에 성공했을 때: 보물이 털린 것으로 확정하고 GameManager에 알린다.</summary>
    public void Steal()
    {
        if (State != TreasureState.Carried) return;

        Carrier = null;
        State = TreasureState.Stolen;
        transform.SetParent(homeParent, true);   // 쥐가 비활성/삭제돼도 같이 사라지지 않게 분리
        gameObject.SetActive(false);             // OnDisable에서 Active 제거 (깜빡이던 점은 스스로 사라짐)

        if (GameManager.Instance != null)
            GameManager.Instance.OnTreasureStolen();

        OnReleased?.Invoke(this);
        OnStolen?.Invoke(this);
    }

    private void RegisterOnMinimap(bool removeFirst)
    {
        var minimap = MinimapController.Instance;
        if (minimap == null) return;
        if (removeFirst) minimap.Remove(this);   // Register는 이미 등록된 키면 무시하므로 먼저 제거
        minimap.Register(this);
    }

    // using System 때문에 Random이 겹치므로 UnityEngine.Random으로 명시
    private void ApplyRandomVisual()
    {
        if (visuals == null || visuals.Length == 0) return;
        int pick = UnityEngine.Random.Range(0, visuals.Length);
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] != null) visuals[i].SetActive(i == pick);
    }

    private void SetColliders(bool enabled)
    {
        if (colliders == null) return;
        foreach (var c in colliders)
            if (c != null) c.enabled = enabled;
    }
}