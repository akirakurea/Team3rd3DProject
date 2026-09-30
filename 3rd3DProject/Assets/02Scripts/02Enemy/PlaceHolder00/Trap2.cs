using UnityEngine;

public class Trap2 : MonoBehaviour
{
    public float stunDuration = 3.0f; // 덫에 걸렸을 때 스턴 시간

    private void OnTriggerEnter(Collider other)
    {
        // 도둑 태그를 확인하거나 ThiefController 컴포넌트가 있는지 확인
        if (other.CompareTag("Thief"))
        {
            if (other.TryGetComponent<ThiefController>(out var thief))
            {
                Debug.Log("돈까스 냠냠!");
                thief.TakeStun(stunDuration); // 도둑에게 3초 스턴 부여!

                // 덫 일회성이면 파괴/비활성화
                //gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }
    }
}