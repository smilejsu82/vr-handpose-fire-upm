using UnityEngine;

namespace FireSafetyVR
{
    /// <summary>
    /// HoseStartPoint와 Nozzle 사이를 LineRenderer로 연결하는 1차 프로토타입용 호스.
    /// 중간 포인트를 자동 생성하고, Mathf.Sin 곡선으로 살짝 아래로 처지게 표현한다.
    /// 노즐 이동에 따라 LateUpdate에서 매 프레임 갱신한다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class SimpleFireHose : MonoBehaviour
    {
        [Header("연결 대상")]
        [Tooltip("호스가 시작되는 위치 (소화전 릴/밸브 근처)")]
        [SerializeField] private Transform hoseStart;

        [Tooltip("호스 끝이 따라갈 노즐")]
        [SerializeField] private Transform nozzle;

        [Tooltip("호스를 그릴 LineRenderer. 비워두면 같은 오브젝트에서 자동 할당")]
        [SerializeField] private LineRenderer lineRenderer;

        [Header("호스 모양")]
        [Tooltip("호스를 구성하는 점 개수 (최소 2)")]
        [SerializeField] private int segmentCount = 24;

        [Tooltip("호스가 아래로 처지는 정도 (m)")]
        [SerializeField] private float sagAmount = 0.3f;

        [Header("디버그")]
        [SerializeField] private bool drawGizmo = true;

        private void Reset()
        {
            // 컴포넌트 추가 시 같은 오브젝트의 LineRenderer 자동 연결
            lineRenderer = GetComponent<LineRenderer>();
        }

        private void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();
        }

        private void LateUpdate()
        {
            UpdateHose();
        }

        /// <summary>
        /// 시작점~노즐 사이를 보간해 LineRenderer 위치를 갱신한다.
        /// </summary>
        private void UpdateHose()
        {
            // 필수 참조가 없으면 에러 없이 조용히 종료
            if (hoseStart == null || nozzle == null || lineRenderer == null)
                return;

            int count = Mathf.Max(2, segmentCount); // 최소 2 보정

            if (lineRenderer.positionCount != count)
                lineRenderer.positionCount = count;

            Vector3 start = hoseStart.position;
            Vector3 end = nozzle.position;

            for (int i = 0; i < count; i++)
            {
                float t = (count == 1) ? 0f : (float)i / (count - 1);

                // 시작점과 끝점을 직선 보간
                Vector3 point = Vector3.Lerp(start, end, t);

                // 중간을 sin 곡선으로 아래로 처지게 (양 끝은 0)
                point.y -= Mathf.Sin(t * Mathf.PI) * sagAmount;

                lineRenderer.SetPosition(i, point);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo || hoseStart == null || nozzle == null)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(hoseStart.position, 0.03f);
            Gizmos.DrawSphere(nozzle.position, 0.03f);
            Gizmos.DrawLine(hoseStart.position, nozzle.position);
        }
    }
}
