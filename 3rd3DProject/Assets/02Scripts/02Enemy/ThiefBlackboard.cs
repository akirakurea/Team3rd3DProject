using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 게임 내에서 활용할 모든 에셋을 담는 블랙보드 클래스.
/// 아래쪽에서는 각종 상태와 정보를 담는 필드들을 정의하고, 초기화 및 업데이트 메서드를 제공한다.
/// 인스펙터에서 쉽게 확인할 수 있도록 [Header] 속성을 사용하여 그룹화되어 있다.
/// 해당 클래스는 도둑 AI의 행동을 제어하는 데 필요한 정보를 담고 있으며, 플레이어 감지, 아이템 정보, 상태 및 탈출 로직 등을 포함한다.
/// 도둑 AI의 행동을 구현할 때, 이 블랙보드를 통해 필요한 정보를 쉽게 접근하고 관리할 수 있다.
/// 도둑 오브젝트에 이 블랙보드를 연결하여, 도둑의 상태와 행동을 효과적으로 제어할 수 있다.
/// </summary>
[System.Serializable]
public class ThiefBlackboard
{
    [Header("컴포넌트")]
    public GameObject ThiefGameObject;
    public Transform ThiefTransform;
    public NavMeshAgent Agent;
    public Animator ThiefAnim;
    [Header("플레이어감지")]
    public Transform PlayerTransform;
    public float DistanceToPlayer;
    public float FleeDistanceThreshold = 8.0f;
    public bool IsPlayerNearby => DistanceToPlayer <= FleeDistanceThreshold;

    [Header("아이템정보")]
    public Transform TargetItem;
    public Vector3 TargetDestination;
    public bool IsCarryingItem;
    public GameObject CarriedItem;

    [Header("디버그 및 상태")]
    public bool IsStunned;
    public bool IsFleeing;
    public float StunDuration;
    public float CurrentStunTimer;

    [Header("탈출/도주 로직")]
    public Transform SpawnPosition;
    public bool ShouldDropItemOnFlee;//도망 시 물건 버릴지 여부

    public void Initailize(GameObject thiefObj)
    {
        ThiefGameObject = thiefObj;
        ThiefTransform = thiefObj.transform;
        Agent = thiefObj.GetComponent<NavMeshAgent>();
        ThiefAnim = thiefObj.GetComponentInChildren<Animator>();

        //플레이어 자동검색
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerTransform = playerObj.transform;
        }
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj != null)
        {
            SpawnPosition = baseObj.transform;
        }

    }
    public void PickUpTargetItem()
    {
        if(TargetItem != null)
        {
            CarriedItem = TargetItem.gameObject;
            CarriedItem.SetActive(false);

            IsCarryingItem = true;
            TargetItem = null;
        }
    }
    public void DropCarriedItem()
    {
        if(IsCarryingItem && CarriedItem != null)
        {
            CarriedItem.transform.position = ThiefTransform.position;
            CarriedItem.SetActive(true);
            Debug.Log("[BT]플레이어가 다가와 들고있던 아이템을 바닥에 버리고 도망칩니다.");
            //아이템매니저로 씬 내 아이템 재등록
            ItemManager2.Instance?.AddItem(CarriedItem);

            CarriedItem = null;
            IsCarryingItem = false;
        }

    }

    public void UpdatePerception()
    {
        if(PlayerTransform != null && ThiefTransform != null)
        {
            DistanceToPlayer = Vector3.Distance(ThiefTransform.position, PlayerTransform.position);
        }
    }
}
