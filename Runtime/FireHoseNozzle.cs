using UnityEngine;

namespace FireSafetyVR
{
    /// <summary>
    /// 노즐의 물 분사를 제어한다.
    /// 밸브가 열린 상태(valveOpened)에서 SetSpray(true)일 때만 물이 나오며,
    /// 분사 중에만 Raycast로 FireTarget을 감지해 소화한다.
    /// </summary>
    public class FireHoseNozzle : MonoBehaviour
    {
        [Header("분사 지점")]
        [Tooltip("물이 나가는 시작점/방향. 비워두면 자기 자신 사용")]
        [SerializeField] private Transform shootPoint;

        [Tooltip("물줄기 파티클. 없으면 시각효과 없이 판정만 동작")]
        [SerializeField] private ParticleSystem waterParticle;

        [Header("소화 설정")]
        [Tooltip("물이 닿는 최대 거리 (m)")]
        [SerializeField] private float range = 8f;

        [Tooltip("초당 소화량")]
        [SerializeField] private float extinguishPower = 10f;

        [Tooltip("Raycast가 맞출 레이어 (불/벽 등)")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("디버그")]
        [SerializeField] private bool drawDebugRay = true;

        // 현재 분사 요청 상태 (트리거 입력)
        private bool isSpraying;

        // 밸브 열림 상태
        private bool valveOpened;

        /// <summary>실제로 물이 나오고 있는지 (요청 && 밸브열림).</summary>
        public bool IsActuallySpraying => isSpraying && valveOpened;

        private void Awake()
        {
            if (shootPoint == null)
                shootPoint = transform;
        }

        private void Update()
        {
            // 실제 분사 중일 때만 판정 실행 (성능 기준 준수)
            if (IsActuallySpraying)
                DoSprayRaycast();
        }

        /// <summary>
        /// 트리거 입력에 따른 분사 요청. 밸브가 닫혀 있으면 무시된다.
        /// </summary>
        public void SetSpray(bool value)
        {
            isSpraying = value;
            UpdateParticleState();
        }

        /// <summary>
        /// 밸브 열림 상태 전달. 닫히면 즉시 분사 중지.
        /// </summary>
        public void SetValveOpened(bool value)
        {
            valveOpened = value;
            UpdateParticleState();
        }

        /// <summary>
        /// 외부에서 강제로 한 번 분사 판정을 돌리고 싶을 때 사용.
        /// </summary>
        public void Spray()
        {
            if (IsActuallySpraying)
                DoSprayRaycast();
        }

        /// <summary>
        /// 현재 상태에 맞춰 파티클 재생/정지를 동기화한다.
        /// </summary>
        private void UpdateParticleState()
        {
            if (waterParticle == null)
                return; // 파티클 없어도 에러 없이 동작

            if (IsActuallySpraying)
            {
                if (!waterParticle.isPlaying)
                    waterParticle.Play();
            }
            else
            {
                if (waterParticle.isPlaying)
                    waterParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// 분사 방향으로 Raycast를 쏴 FireTarget이면 소화량을 적용한다.
        /// </summary>
        private void DoSprayRaycast()
        {
            Transform origin = shootPoint != null ? shootPoint : transform;
            Vector3 dir = origin.forward;

            if (drawDebugRay)
                Debug.DrawRay(origin.position, dir * range, Color.cyan);

            if (Physics.Raycast(origin.position, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                FireTarget target = hit.collider.GetComponentInParent<FireTarget>();
                if (target != null)
                {
                    float amount = extinguishPower * Time.deltaTime;
                    target.Extinguish(amount);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = shootPoint != null ? shootPoint : transform;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(origin.position, origin.position + origin.forward * range);
        }
    }
}
