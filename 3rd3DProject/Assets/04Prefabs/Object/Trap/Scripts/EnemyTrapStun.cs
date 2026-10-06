using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TexasJawTrap
{
    // Enemy 루트에 추가하고 이동/공격 스크립트를 scriptsToPause에 연결하세요.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public class EnemyTrapStun : MonoBehaviour
    {
        [Tooltip("스턴 동안 멈출 이동 AI와 공격 스크립트만 넣으세요. 체력/보물 스크립트는 넣지 마세요.")]
        public MonoBehaviour[] scriptsToPause = new MonoBehaviour[0];
        [Tooltip("체크하면 적 애니메이션도 멈춥니다. 덫의 닫히는 모션에는 영향이 없습니다.")]
        public bool pauseAnimation;

        public bool IsStunned { get; private set; }
        readonly List<MonoBehaviour> pausedScripts = new List<MonoBehaviour>();
        float endTime;
        Vector3 lockedPosition;
        Quaternion lockedRotation;
        Rigidbody body;
        bool wasKinematic;
        RigidbodyConstraints originalConstraints;
        NavMeshAgent agent;
        bool agentCaptured, originalStopped, originalUpdatePosition, originalUpdateRotation;
        Animator animator;
        float originalAnimationSpeed;
        bool animationCaptured;

        public void ApplyStun(float seconds)
        {
            // 여러 덫을 밟아도 먼저 끝난 덫이 다른 스턴을 해제하지 않게 합니다.
            float until = Time.time + Mathf.Max(.01f, seconds);
            if (IsStunned)
            {
                endTime = Mathf.Max(endTime, until);
                return;
            }

            IsStunned = true;
            endTime = until;
            lockedPosition = transform.position;
            lockedRotation = transform.rotation;
            pausedScripts.Clear();
            body = GetComponent<Rigidbody>();
            if (body != null)
            {
                wasKinematic = body.isKinematic;
                originalConstraints = body.constraints;
                if (!wasKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
                body.constraints = RigidbodyConstraints.FreezeAll;
            }

            agent = GetComponent<NavMeshAgent>();
            agentCaptured = agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
            if (agentCaptured)
            {
                originalStopped = agent.isStopped;
                originalUpdatePosition = agent.updatePosition;
                originalUpdateRotation = agent.updateRotation;
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.updatePosition = false;
                agent.updateRotation = false;
            }

            animator = GetComponentInChildren<Animator>();
            animationCaptured = pauseAnimation && animator != null;
            if (animationCaptured)
            {
                originalAnimationSpeed = animator.speed;
                animator.speed = 0f;
            }

            // 원래 물리/이동 상태를 먼저 저장한 뒤 AI를 잠시 끕니다.
            // 이미 실행 중인 코루틴은 해당 AI에서 IsStunned를 확인해야 합니다.
            if (scriptsToPause != null)
            {
                foreach (MonoBehaviour script in scriptsToPause)
                {
                    if (script == null || script == this || !script.enabled) continue;
                    pausedScripts.Add(script);
                    script.enabled = false;
                }
            }
        }

        void Update()
        {
            if (IsStunned && Time.time >= endTime) ReleaseStun();
        }

        void LateUpdate()
        {
            if (!IsStunned) return;
            // CharacterController/Transform으로 움직이는 적도 제자리에 유지합니다.
            transform.SetPositionAndRotation(lockedPosition, lockedRotation);
            if (agentCaptured && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.nextPosition = lockedPosition;
            }
        }

        void ReleaseStun()
        {
            if (!IsStunned) return;
            IsStunned = false;
            if (body != null)
            {
                body.constraints = originalConstraints;
                body.isKinematic = wasKinematic;
            }
            if (agentCaptured && agent != null)
            {
                agent.updatePosition = originalUpdatePosition;
                agent.updateRotation = originalUpdateRotation;
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.nextPosition = transform.position;
                    agent.isStopped = originalStopped;
                }
            }
            if (animationCaptured && animator != null) animator.speed = originalAnimationSpeed;
            // 스턴 전에 켜져 있던 스크립트만 다시 켭니다.
            foreach (MonoBehaviour script in pausedScripts)
                if (script != null) script.enabled = true;
            pausedScripts.Clear();
        }

        // 덫이 먼저 없어져도 스턴은 Enemy에서 독립적으로 유지됩니다.
        // Enemy가 비활성화되면 잠금 상태가 남지 않도록 정리합니다.
        void OnDisable() { ReleaseStun(); }
    }
}
