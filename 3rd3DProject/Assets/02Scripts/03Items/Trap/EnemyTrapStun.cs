using UnityEngine;

namespace TexasJawTrap
{
    // 도둑 본체에 붙입니다. BT가 읽을 스턴 상태와 시간만 관리합니다.
    // 이동, 공격, 애니메이션은 BT에서 IsStunned를 보고 처리하세요.
    [DisallowMultipleComponent]
    public class EnemyTrapStun : MonoBehaviour
    {
        public bool IsStunned => Time.time < endTime;
        private float endTime;

        // 덫을 밟으면 호출됩니다. 다시 밟으면 종료 시간을 연장합니다.
        public void ApplyStun(float seconds)
        {
            endTime = Mathf.Max(endTime, Time.time + Mathf.Max(0f, seconds));
        }

        // BT나 다른 코드에서 스턴을 즉시 끝낼 때 사용할 수 있습니다.
        public void ReleaseStun()
        {
            endTime = Time.time;
        }

        // 도둑이 비활성화되었다 다시 등장할 때 스턴이 남지 않게 합니다.
        private void OnDisable()
        {
            ReleaseStun();
        }
    }
}
