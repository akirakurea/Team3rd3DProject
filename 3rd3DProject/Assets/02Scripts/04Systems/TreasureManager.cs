using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 라운드가 시작될 때마다 TreasureSpawnPoint 중 무작위로 골라 보물을 배치한다. (씬에 하나만 둔다)
/// - 보물은 HouseZone(집 안) 안에 있는 스폰 포인트에만 놓인다. 집 밖 포인트는 제외되고 경고가 뜬다.
/// - 보물 수는 GameManager의 treasuresPerRound를 그대로 따른다 (보물 수 설정이 두 곳으로 갈라지지 않게).
/// - 보물 오브젝트는 매 라운드 새로 만들지 않고 재사용한다.
/// - 이 컴포넌트는 다른 컴포넌트와 섞지 않은 전용 오브젝트에 붙일 것 (중복 시 오브젝트째 파괴됨)
/// </summary>
public class TreasureManager : MonoBehaviour
{
    public static TreasureManager Instance { get; private set; }

    [SerializeField] private Treasure[] treasurePrefab;

    private readonly List<Treasure> treasures = new List<Treasure>();
    private readonly List<TreasureSpawnPoint> validPoints = new List<TreasureSpawnPoint>();
    private readonly List<int> shuffleOrder = new List<int>();   // 라운드마다 새로 만들지 않고 재사용
    private GameManager gm;

    /// <summary>
    /// 씬의 보물 전체(읽기 전용). 쥐 AI가 목표를 고를 때 순회해서 쓴다.
    /// 털렸거나 비활성인 보물도 들어 있으므로 반드시 거른다: t != null && t.IsAvailable && t.gameObject.activeInHierarchy
    /// 가장 가까운 것만 필요하면 GetNearestAvailable(위치)를 쓴다.
    /// </summary>
    public IReadOnlyList<Treasure> Treasures => treasures;

    /// <summary>지금 제자리에 있고 집을 수 있는 보물 수</summary>
    public int AvailableCount
    {
        get
        {
            int n = 0;
            foreach (var t in treasures)
                if (t != null && t.IsAvailable && t.gameObject.activeInHierarchy) n++;
            return n;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        CollectSpawnPoints();

        // GameManager는 한 프레임 뒤 게임을 시작하므로 Start에서 구독해도 안전
        gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("TreasureManager: GameManager를 찾을 수 없습니다.");
            return;
        }
        gm.OnRoundStarted += HandleRoundStarted;
    }

    private void OnDestroy()
    {
        if (gm != null) gm.OnRoundStarted -= HandleRoundStarted;
        if (Instance == this) Instance = null;
    }

    // 씬의 스폰 포인트 중 "집 안"인 것만 추린다
    private void CollectSpawnPoints()
    {
        validPoints.Clear();

        // Unity 6.4+에서는 정렬 모드 파라미터가 deprecated. 그 이전 버전은 SortMode.None을 넘긴다.
#if UNITY_6000_4_OR_NEWER
        var all = FindObjectsByType<TreasureSpawnPoint>();
#else
        var all = FindObjectsByType<TreasureSpawnPoint>(FindObjectsSortMode.None);
#endif
        HouseZone zone = HouseZone.Current;

        if (zone == null)
        {
            Debug.LogWarning("TreasureManager: 씬에 HouseZone이 없어 모든 스폰 포인트를 사용합니다. 집 안에만 놓으려면 HouseZone을 추가하세요.");
            validPoints.AddRange(all);
            return;
        }

        int excluded = 0;
        foreach (var p in all)
        {
            if (zone.Contains(p.transform.position)) validPoints.Add(p);
            else excluded++;
        }
        if (excluded > 0)
            Debug.LogWarning($"TreasureManager: 스폰 포인트 {excluded}개가 집 밖에 있어 제외했습니다. (Scene 뷰에서 빨간색으로 표시됨)");
    }

    private void HandleRoundStarted(int round, int ratCount)
    {
        SpawnTreasures(gm.TreasuresPerRound);
    }

    private void SpawnTreasures(int count)
    {
        if (treasurePrefab == null || treasurePrefab.Length == 0)
        {
            Debug.LogError("TreasureManager: Treasure Prefab이 비어 있습니다.");
            return;
        }
        if (validPoints.Count < count)
        {
            // 보물이 모자라면 패배 조건(보물 전부 털림)에 영영 도달할 수 없어 게임이 깨진다
            Debug.LogError($"TreasureManager: 집 안 보물 스폰 포인트가 부족합니다. (필요 {count}개 / 현재 {validPoints.Count}개)");
            count = validPoints.Count;
        }

        //성능 영향 없음으로 풀링 포기.
        foreach (var t in treasures)
            if (t != null) Destroy(t.gameObject);
        treasures.Clear();
        // 필요한 만큼만 만들어 두고 재사용
        //while (treasures.Count < count)
        //    treasures.Add(Instantiate(treasurePrefab, transform));

        // 스폰 포인트를 섞어서 앞에서부터 count개 사용 (Fisher-Yates, 리스트 재사용)
        shuffleOrder.Clear();
        for (int i = 0; i < validPoints.Count; i++) shuffleOrder.Add(i);
        for (int i = shuffleOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffleOrder[i], shuffleOrder[j]) = (shuffleOrder[j], shuffleOrder[i]);
        }

        for (int i = 0; i < count; i++)
        {
            
                Transform p = validPoints[shuffleOrder[i]].transform;
            Treasure prefab = treasurePrefab[Random.Range(0, treasurePrefab.Length)];
            Treasure newTreasure = Instantiate(prefab, transform);
                newTreasure.Init(p.position, p.rotation, transform);
            treasures.Add(newTreasure);
            //treasures[i].gameObject.SetActive(false);   // 이번 라운드에 안 쓰는 여분
            /*마지막 for 루프를 treasures.Count 기준(비어있던 대상)이 아니라 count 기준(스폰해야 할 목표 개수)으로 돌리고,
             * 그 안에서 Instantiate + Init + treasures.Add를 한 번에 처리하도록 바꿨어요.
             * else 분기(여분 비활성화)는 매 라운드 새로 만드는 방식에선 필요 없어져서(남는 걸 재사용 안 하니까) 뺐습니다.*/

        }
    }

    /// <summary>주어진 위치에서 가장 가까운, 집을 수 있는 보물. 없으면 null. (쥐 AI가 목표를 고를 때 사용)</summary>
    public Treasure GetNearestAvailable(Vector3 from)
    {
        Treasure best = null;
        float bestSqr = float.MaxValue;
        foreach (var t in treasures)
        {
            if (t == null || !t.IsAvailable || !t.gameObject.activeInHierarchy) continue;
            float sqr = (t.transform.position - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = t;
            }
        }
        return best;
    }
}