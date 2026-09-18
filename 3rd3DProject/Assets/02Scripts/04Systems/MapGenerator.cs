using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방1 - 거실 - 방2 - 마당 구조를 절차적으로 생성한다.
///
/// [사용 알고리즘]
/// 1. 구간 분할(Wall Segmentation): 벽 라인에서 문/쥐구멍 구간(Gap)을 뺀 나머지만
///    솔리드 세그먼트로 채워서 벽을 만든다. Gap마다 시작~끝 좌표와 뚫리는 높이를 가진다.
/// 2. 고양이/쥐 통행 제한: 쥐구멍 통로의 폭을 고양이 NavMeshAgent 반지름보다 좁게 만들어서
///    별도 로직 없이 고양이가 물리적으로 통과할 수 없게 한다. (NavMesh Area를 쓰는 대안도
///    하단 주석 참고)
/// 3. 최적화: 생성된 모든 벽 조각을 CombineInstance로 하나의 메쉬로 합쳐 드로우콜을 줄인다.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MapGenerator : MonoBehaviour
{
    [Header("치수 (미터)")]
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float wallThickness = 0.4f;   // 두께감이 보이도록 키운 값
    [SerializeField] private float floorThickness = 0.2f;

    [Tooltip("켜면 벽 끝을 두께의 절반만큼 늘려 모서리에 빈틈이 생기지 않게 한다.")]
    [SerializeField] private bool extendWallEndsForCorners = true;

    [Header("쥐구멍 설정")]
    [SerializeField] private float ratHoleHeight = 0.6f;   // 쥐구멍 높이 (바닥부터)
    [SerializeField] private float ratHoleWidth = 0.6f;
    [SerializeField] private float ratTunnelWidth = 0.4f;  // 고양이 반지름보다 좁게 유지할 것

    [Header("마당 크기")]
    [Tooltip("집 바깥으로 마당이 뻗어나가는 여백(미터). 쥐구멍/통로로 나온 쥐가 이 위를 돌아다닌다.")]
    [SerializeField] private float yardMargin = 12f;
    [Tooltip("정문 앞쪽(남쪽)은 추격전 공간이 더 필요하므로 여백을 추가로 준다.")]
    [SerializeField] private float yardFrontExtra = 8f;

    [Header("머티리얼")]
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material floorMaterial;

    [Header("보물 랜덤 스폰")]
    [SerializeField] private GameObject treasurePrefab;
    [SerializeField] private int treasureCount = 5;
    [SerializeField] private float treasureEdgeMargin = 0.8f; // 벽에 너무 붙어서 스폰되지 않도록 여백
    [SerializeField] private float treasureYOffset = 0.1f;    // 바닥 위로 살짝 띄우기

    [Header("최적화")]
    [SerializeField] private bool combineWallMeshes = true;

    [Header("지형지물 (실내 가구 / 실외 나무·돌)")]
    [SerializeField] private PropSet indoorProps = new PropSet { count = 12, minSpacing = 1.5f };
    [SerializeField] private PropSet outdoorProps = new PropSet { count = 40, minSpacing = 2.5f };
    [Tooltip("문·쥐구멍 앞에 지형지물이 놓이지 않도록 확보할 반경(미터)")]
    [SerializeField] private float passageClearRadius = 1.8f;
    [Tooltip("거부 샘플링 최대 시도 횟수. 공간이 좁으면 count보다 적게 배치될 수 있다.")]
    [SerializeField] private int maxScatterAttempts = 30;

    /// <summary>한 종류의 지형지물 묶음(실내용/실외용) 설정</summary>
    [System.Serializable]
    public class PropSet
    {
        [Tooltip("이 중에서 랜덤으로 하나가 골라진다. 나무·돌·덤불 프리팹을 넣어두면 된다.")]
        public GameObject[] prefabs;
        public int count = 20;
        [Tooltip("오브젝트끼리 최소 이 거리 이상 떨어뜨린다.")]
        public float minSpacing = 2f;
        [Tooltip("Y축 회전을 랜덤으로 줘서 같은 프리팹도 달라 보이게 한다.")]
        public bool randomYRotation = true;
        [Tooltip("크기 랜덤 범위 (x=최소배율, y=최대배율)")]
        public Vector2 scaleRange = new Vector2(0.85f, 1.2f);
    }

    // 이미 배치된 모든 오브젝트 위치 (보물 + 지형지물 공용 — 서로도 겹치지 않게 한다)
    private readonly List<Vector3> _occupiedPoints = new List<Vector3>();
    // 문·쥐구멍 등 막히면 안 되는 지점
    private readonly List<Vector3> _passagePoints = new List<Vector3>();

    // 실내 방 하나의 배치 정보 (바닥 생성 + 보물 스폰에 공용으로 사용)
    private struct RoomDef
    {
        public string name;
        public Vector3 center;
        public Vector2 size;
        public RoomDef(string name, Vector3 center, Vector2 size)
        { this.name = name; this.center = center; this.size = size; }
    }

    // 집 내부(마당 제외) 방 목록 — 보물은 이 방들 안에서만 스폰됨
    private RoomDef[] _indoorRooms;

    // 벽에 뚫을 구간 하나를 표현
    private struct Gap
    {
        public float start;      // 벽 라인 상의 시작 좌표
        public float end;        // 벽 라인 상의 끝 좌표
        public float gapHeight;  // 바닥부터 이 높이까지 뚫림 (문=wallHeight, 쥐구멍=낮게)
        public Gap(float start, float end, float gapHeight)
        {
            this.start = start;
            this.end = end;
            this.gapHeight = gapHeight;
        }
    }

    private readonly List<CombineInstance> _wallCombineList = new List<CombineInstance>();
    private Transform _root;

    private void Start()
    {
        _root = new GameObject("GeneratedMap").transform;
        _root.SetParent(transform, false);

        // ---- 실내 방 정의 (바닥 생성 + 보물 스폰이 이 데이터를 공유) ----
        _indoorRooms = new[]
        {
            new RoomDef("거실", new Vector3(0, 0, 4), new Vector2(8, 8)),
            new RoomDef("방1", new Vector3(-7, 0, 4), new Vector2(6, 8)),
            new RoomDef("방2", new Vector3(7, 0, 4), new Vector2(6, 8)),
        };

        // ---- 마당 (집 전체를 둘러싸는 바닥) ----
        // 집 외곽: x -10~10, z 0~8. 여기에 쥐구멍 통로가 북쪽(z=8 바깥)으로 조금 더 튀어나온다.
        // 서쪽 쥐구멍 / 북쪽 통로 / 남쪽 정문 — 모든 출구가 마당 위로 이어져야 하므로
        // 마당은 집을 완전히 감싸는 하나의 큰 바닥으로 만든다.
        float houseMinX = -10f, houseMaxX = 10f;
        float houseMinZ = 0f;
        float houseMaxZ = 8f + wallThickness + ratTunnelWidth; // 쥐구멍 통로까지 포함한 북쪽 끝

        float yardMinX = houseMinX - yardMargin;
        float yardMaxX = houseMaxX + yardMargin;
        float yardMinZ = houseMinZ - yardMargin - yardFrontExtra; // 정문 앞은 더 넓게
        float yardMaxZ = houseMaxZ + yardMargin;

        CreateFloor("마당_바닥",
            center: new Vector3((yardMinX + yardMaxX) * 0.5f, 0, (yardMinZ + yardMaxZ) * 0.5f),
            size: new Vector2(yardMaxX - yardMinX, yardMaxZ - yardMinZ));

        // ---- 집 안 바닥 (마당 위에 살짝 겹쳐 올라가도록 y를 아주 조금 띄운다) ----
        foreach (var room in _indoorRooms)
            CreateFloor(room.name + "_바닥", room.center + Vector3.up * 0.01f, room.size);

        // ---- 남쪽 벽 (정문 포함) ----
        BuildWallAlongX(z: 0, xStart: -10, xEnd: 10,
            gaps: new List<Gap> { new Gap(-1f, 1f, wallHeight) }); // 정문: 전체 높이 개방

        // ---- 북쪽 벽 (거실 구간은 완전히 막고, 방1/방2 쪽에만 쥐구멍 통로 입구) ----
        float tunnelHalf = ratTunnelWidth * 0.5f;
        BuildWallAlongX(z: 8, xStart: -10, xEnd: 10,
            gaps: new List<Gap>
            {
                new Gap(-9f - tunnelHalf, -9f + tunnelHalf, ratHoleHeight), // 방1측 통로 입구
                new Gap(9f - tunnelHalf, 9f + tunnelHalf, ratHoleHeight),   // 방2측 통로 입구
            });

        // ---- 서쪽 벽 (방1 쥐구멍: 마당으로 직결) ----
        BuildWallAlongZ(x: -10, zStart: 0, zEnd: 8,
            gaps: new List<Gap> { new Gap(0.5f, 0.5f + ratHoleWidth, ratHoleHeight) });

        // ---- 동쪽 벽 (개방) ----
        BuildWallAlongZ(x: 10, zStart: 0, zEnd: 8, gaps: null);

        // ---- 내벽: 방1|거실, 거실|방2 경계 (문 없음 — 게임 로직상 필요하면 여기 Gap 추가) ----
        BuildWallAlongZ(x: -4, zStart: 0, zEnd: 8, gaps: null);
        BuildWallAlongZ(x: 4, zStart: 0, zEnd: 8, gaps: null);

        // ---- 쥐구멍 통로 (건물 뒤편, 거실을 우회해서 방1↔방2 직결) ----
        BuildRatTunnel();

        if (combineWallMeshes) CombineAndFinalize();

        // ---- 막히면 안 되는 통로 지점 등록 (지형지물 배치 시 이 주변은 비워둔다) ----
        _passagePoints.Add(new Vector3(0, 0, 0));          // 정문
        _passagePoints.Add(new Vector3(-10f, 0, 0.8f));    // 방1 서쪽 쥐구멍
        _passagePoints.Add(new Vector3(-9f, 0, 8f));       // 방1측 통로 입구
        _passagePoints.Add(new Vector3(9f, 0, 8f));        // 방2측 통로 입구

        // ---- 집 안 바닥에 보물 랜덤 스폰 ----
        SpawnTreasures();

        // ---- 지형지물 배치 (실내 가구 / 실외 나무·돌) ----
        ScatterIndoorProps();
        ScatterOutdoorProps(yardMinX, yardMaxX, yardMinZ, yardMaxZ,
                            houseMinX, houseMaxX, houseMinZ, houseMaxZ);
    }

    // ================= 지형지물 배치 (거부 샘플링) =================

    /// 실내: 방 안에만 배치
    private void ScatterIndoorProps()
    {
        if (!HasPrefabs(indoorProps)) return;
        Transform root = MakeChild("IndoorProps");

        for (int i = 0; i < indoorProps.count; i++)
        {
            for (int attempt = 0; attempt < maxScatterAttempts; attempt++)
            {
                RoomDef room = _indoorRooms[Random.Range(0, _indoorRooms.Length)];
                float halfW = Mathf.Max(0f, room.size.x * 0.5f - 0.8f);
                float halfD = Mathf.Max(0f, room.size.y * 0.5f - 0.8f);
                Vector3 p = new Vector3(
                    room.center.x + Random.Range(-halfW, halfW),
                    0.01f,
                    room.center.z + Random.Range(-halfD, halfD));

                if (!IsValidSpot(p, indoorProps.minSpacing)) continue;
                PlaceProp(indoorProps, p, root);
                break;
            }
        }
    }

    /// 실외: 마당 범위에서 집 footprint를 제외한 영역에 배치
    private void ScatterOutdoorProps(float yMinX, float yMaxX, float yMinZ, float yMaxZ,
                                     float hMinX, float hMaxX, float hMinZ, float hMaxZ)
    {
        if (!HasPrefabs(outdoorProps)) return;
        Transform root = MakeChild("OutdoorProps");

        // 집 벽에 바짝 붙지 않도록 footprint를 조금 부풀려서 제외한다.
        const float housePadding = 1.5f;
        hMinX -= housePadding; hMaxX += housePadding;
        hMinZ -= housePadding; hMaxZ += housePadding;

        for (int i = 0; i < outdoorProps.count; i++)
        {
            for (int attempt = 0; attempt < maxScatterAttempts; attempt++)
            {
                Vector3 p = new Vector3(
                    Random.Range(yMinX + 1f, yMaxX - 1f),
                    0.01f,
                    Random.Range(yMinZ + 1f, yMaxZ - 1f));

                // 집이 차지한 영역이면 버리고 다시 뽑는다 (집 안에 나무가 생기는 것 방지)
                bool insideHouse = p.x > hMinX && p.x < hMaxX && p.z > hMinZ && p.z < hMaxZ;
                if (insideHouse) continue;

                if (!IsValidSpot(p, outdoorProps.minSpacing)) continue;
                PlaceProp(outdoorProps, p, root);
                break;
            }
        }
    }

    /// 최소 간격 + 통로 확보 조건을 모두 만족하는지 검사
    private bool IsValidSpot(Vector3 p, float minSpacing)
    {
        float sqrSpacing = minSpacing * minSpacing;
        foreach (var o in _occupiedPoints)
            if ((o - p).sqrMagnitude < sqrSpacing) return false;

        float sqrClear = passageClearRadius * passageClearRadius;
        foreach (var pt in _passagePoints)
            if ((pt - p).sqrMagnitude < sqrClear) return false;

        return true;
    }

    private void PlaceProp(PropSet set, Vector3 pos, Transform parent)
    {
        GameObject prefab = set.prefabs[Random.Range(0, set.prefabs.Length)];
        Quaternion rot = set.randomYRotation
            ? Quaternion.Euler(0, Random.Range(0f, 360f), 0)
            : Quaternion.identity;

        GameObject go = Instantiate(prefab, pos, rot, parent);
        float s = Random.Range(set.scaleRange.x, set.scaleRange.y);
        go.transform.localScale *= s;

        // 지형지물은 움직이지 않으므로 NavMesh 장애물로 구워지도록 Static 처리
        GameObjectUtility_SetStatic(go);

        _occupiedPoints.Add(pos);
    }

    private bool HasPrefabs(PropSet set) => set != null && set.prefabs != null && set.prefabs.Length > 0 && set.count > 0;

    private Transform MakeChild(string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(_root, false);
        return t;
    }

    // ================= 보물 랜덤 스폰 =================

    private void SpawnTreasures()
    {
        if (!treasurePrefab || treasureCount <= 0 || _indoorRooms == null || _indoorRooms.Length == 0)
            return;

        var treasureRoot = new GameObject("Treasures").transform;
        treasureRoot.SetParent(_root, false);

        for (int i = 0; i < treasureCount; i++)
        {
            for (int attempt = 0; attempt < maxScatterAttempts; attempt++)
            {
                RoomDef room = _indoorRooms[Random.Range(0, _indoorRooms.Length)];

                float halfW = Mathf.Max(0f, room.size.x * 0.5f - treasureEdgeMargin);
                float halfD = Mathf.Max(0f, room.size.y * 0.5f - treasureEdgeMargin);

                Vector3 spawnPos = new Vector3(
                    room.center.x + Random.Range(-halfW, halfW),
                    treasureYOffset,
                    room.center.z + Random.Range(-halfD, halfD));

                // 지형지물·다른 보물과 겹치지 않는지 검사 (보물은 조금 더 좁은 간격 허용)
                if (!IsValidSpot(spawnPos, 1.0f)) continue;

                Instantiate(treasurePrefab, spawnPos, Quaternion.identity, treasureRoot);
                _occupiedPoints.Add(spawnPos);
                break;
            }
        }

        // [방 넓이에 비례한 가중 랜덤이 필요하다면]
        // 각 RoomDef의 size.x * size.y를 누적합으로 만들어 두고,
        // Random.Range(0, 총합) 값이 어느 구간에 속하는지로 방을 고르면 된다.
        // 지금처럼 방이 3개뿐이고 크기 차이가 크지 않다면 균등 랜덤으로 충분하다.
    }

    // ================= 바닥 =================

    private void CreateFloor(string name, Vector3 center, Vector2 size)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = name;
        floor.transform.SetParent(_root, false);
        floor.transform.position = center + Vector3.down * (floorThickness * 0.5f);
        floor.transform.localScale = new Vector3(size.x, floorThickness, size.y);
        if (floorMaterial) floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

        // 바닥은 정적 + 내비게이션 대상으로 표시 (에디터에서 Navigation Static 체크와 동일한 효과)
        GameObjectUtility_SetStatic(floor);
    }

    // ================= 벽 생성 (구간 분할 알고리즘) =================

    /// X축과 평행한 벽 (z 고정, x 구간)
    private void BuildWallAlongX(float z, float xStart, float xEnd, List<Gap> gaps)
        => BuildWall(z, xStart, xEnd, gaps, alongX: true);

    /// Z축과 평행한 벽 (x 고정, z 구간)
    private void BuildWallAlongZ(float x, float zStart, float zEnd, List<Gap> gaps)
        => BuildWall(x, zStart, zEnd, gaps, alongX: false);

    private void BuildWall(float fixedCoord, float start, float end, List<Gap> gaps, bool alongX)
    {
        gaps ??= new List<Gap>();
        gaps.Sort((a, b) => a.start.CompareTo(b.start));

        // 벽 두께가 두꺼워지면 직각으로 만나는 벽 사이에 정사각형 빈틈이 생긴다.
        // 양 끝을 두께의 절반만큼 늘려서 서로 겹치게 하면 모서리가 깔끔하게 채워진다.
        if (extendWallEndsForCorners)
        {
            float half = wallThickness * 0.5f;
            start -= half;
            end += half;
        }

        float cursor = start;
        foreach (var gap in gaps)
        {
            // Gap 이전의 솔리드 구간
            if (gap.start > cursor)
                AddWallSegment(cursor, gap.start, 0f, wallHeight, fixedCoord, alongX);

            // Gap 위쪽에 남는 부분(인방, lintel) — 쥐구멍처럼 gapHeight < wallHeight일 때만 생성
            if (gap.gapHeight < wallHeight)
                AddWallSegment(gap.start, gap.end, gap.gapHeight, wallHeight, fixedCoord, alongX);

            cursor = gap.end;
        }

        // 마지막 Gap 이후 남은 솔리드 구간
        if (end > cursor)
            AddWallSegment(cursor, end, 0f, wallHeight, fixedCoord, alongX);
    }

    private void AddWallSegment(float from, float to, float yBottom, float yTop, float fixedCoord, bool alongX)
    {
        float length = to - from;
        if (length <= 0.001f) return;

        float height = yTop - yBottom;
        Vector3 center = alongX
            ? new Vector3((from + to) * 0.5f, yBottom + height * 0.5f, fixedCoord)
            : new Vector3(fixedCoord, yBottom + height * 0.5f, (from + to) * 0.5f);

        Vector3 scale = alongX
            ? new Vector3(length, height, wallThickness)
            : new Vector3(wallThickness, height, length);

        GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seg.name = "WallSegment";
        seg.transform.SetParent(_root, false);
        seg.transform.position = center;
        seg.transform.localScale = scale;
        if (wallMaterial) seg.GetComponent<Renderer>().sharedMaterial = wallMaterial;
        GameObjectUtility_SetStatic(seg);

        if (combineWallMeshes)
        {
            var mf = seg.GetComponent<MeshFilter>();
            _wallCombineList.Add(new CombineInstance
            {
                mesh = mf.sharedMesh,
                transform = mf.transform.localToWorldMatrix
            });
            seg.SetActive(false); // 합쳐진 후에는 개별 오브젝트는 비활성화
        }
    }

    // ================= 쥐구멍 통로 (거실 우회) =================

    private void BuildRatTunnel()
    {
        // 건물 뒤편(z=8 바깥쪽)을 따라 방1 쪽부터 방2 쪽까지 좁은 복도를 만든다.
        // 폭이 ratTunnelWidth(고양이 에이전트 반지름보다 좁게 설정)이므로,
        // 고양이 NavMeshAgent는 물리적으로 진입이 불가능하고 쥐만 통과 가능하다.
        //
        // 주의: 벽 두께가 두꺼워지면 벽 중심선 간격과 실제 통행 가능 폭이 달라진다.
        // 아래는 "실제 통행 폭 = ratTunnelWidth"가 되도록 벽 중심선을 두께 절반씩 바깥으로 민 값이다.
        float half = wallThickness * 0.5f;
        float z0 = 8f + half;                       // 건물 외벽 바깥면
        float z1 = z0 + ratTunnelWidth;             // 통로 바깥벽 안쪽면
        float x0 = -9.4f, x1 = 9.4f;

        CreateFloor("쥐구멍통로_바닥",
            center: new Vector3(0, 0.01f, (z0 + z1) * 0.5f),
            size: new Vector2(x1 - x0, ratTunnelWidth));

        // 통로 바깥쪽을 막는 벽 (중심선을 half만큼 더 바깥으로)
        BuildWallAlongX(z: z1 + half, xStart: x0, xEnd: x1, gaps: null);

        // 통로 좌우 끝 마감
        BuildWallAlongZ(x: x0 - half, zStart: z0, zEnd: z1, gaps: null);
        BuildWallAlongZ(x: x1 + half, zStart: z0, zEnd: z1, gaps: null);
    }

    // ================= 유틸 =================

    private void GameObjectUtility_SetStatic(GameObject go)
    {
#if UNITY_EDITOR
        UnityEditor.GameObjectUtility.SetStaticEditorFlags(go,
            UnityEditor.StaticEditorFlags.NavigationStatic | UnityEditor.StaticEditorFlags.BatchingStatic);
#endif
    }

    private void CombineAndFinalize()
    {
        var combinedMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        combinedMesh.CombineMeshes(_wallCombineList.ToArray());

        var combinedGO = new GameObject("Walls_Combined");
        combinedGO.transform.SetParent(_root, false);
        combinedGO.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
        var mr = combinedGO.AddComponent<MeshRenderer>();
        if (wallMaterial) mr.sharedMaterial = wallMaterial;
        combinedGO.AddComponent<MeshCollider>().sharedMesh = combinedMesh;
        GameObjectUtility_SetStatic(combinedGO);
    }
}

/*
 * [NavMesh 베이킹 안내]
 * 이 스크립트는 지오메트리만 생성한다. 씬 재생 전에:
 *   1) Window > AI > Navigation 창에서 Bake 하거나,
 *   2) com.unity.ai.navigation 패키지의 NavMeshSurface 컴포넌트를 빈 오브젝트에 붙이고
 *      런타임에 surface.BuildNavMesh()를 Start()의 CombineAndFinalize() 이후에 호출한다.
 *
 * [고양이/쥐 에이전트 설정]
 *   - 쥐 NavMeshAgent.radius: 0.15~0.2  (ratTunnelWidth=0.4보다 작게)
 *   - 고양이 NavMeshAgent.radius: 0.4~0.5 (ratTunnelWidth보다 크게 → 자동으로 통로 진입 불가)
 *
 * [NavMesh Area를 쓰는 대안]
 * 반지름 트릭 대신 명시적으로 막고 싶다면, 쥐구멍통로 오브젝트에 NavMeshModifier를 붙여
 * "RatOnly" 커스텀 Area로 지정하고, 고양이 NavMeshAgent의 Area Mask에서 해당 Area를
 * 체크 해제하면 된다.
 */