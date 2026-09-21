using UnityEngine;
using UnityEngine.AI;


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
    public bool IsCarrytingItem;
    public GameObject CarriedItem;

    [Header("디버그 및 상태")]
    public bool IsStunned;
    public float StunDuration;
    public float CurrentStunTimer;

    [Header("탈출/도주 로직")]
    public Vector3 EscapeDestination;
    public bool ShouldDropItemOnFlee;//도망 시 물건 버릴지 여부

    public void Initailize(GameObject thiefObj)
    {
        ThiefGameObject = thiefObj;
        ThiefTransform = thiefObj.transform;
        Agent = thiefObj.GetComponent<NavMeshAgent>();
        ThiefAnim = thiefObj.GetComponentInChildren<Animator>();

        //플레이어 자동검색
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if(playerObj != null )
        {
            PlayerTransform = playerObj.transform;
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
