using System.Collections.Generic;
using UnityEngine;

namespace FireSafetyVR
{
    /// <summary>
    /// [2차 확장용] HoseBuilder가 만든 물리 세그먼트 Transform들을 따라
    /// LineRenderer로 호스 외형을 그린다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class HosePointRenderer : MonoBehaviour
    {
        [Header("렌더링")]
        [SerializeField] private LineRenderer lineRenderer;

        [Tooltip("세그먼트 Transform 목록. HoseBuilder.Segments를 SetPoints로 넘겨도 됨")]
        [SerializeField] private List<Transform> hosePoints = new List<Transform>();

        [Tooltip("HoseBuilder가 있으면 시작 시 자동으로 세그먼트를 가져온다")]
        [SerializeField] private HoseBuilder sourceBuilder;

        private void Reset()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        private void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            // HoseBuilder가 연결돼 있으면 그 세그먼트 리스트를 참조
            if (sourceBuilder != null && sourceBuilder.Segments != null && sourceBuilder.Segments.Count > 0)
                hosePoints = sourceBuilder.Segments;
        }

        /// <summary>
        /// 외부에서 포인트 목록을 주입한다.
        /// </summary>
        public void SetPoints(List<Transform> points)
        {
            hosePoints = points;
        }

        private void LateUpdate()
        {
            if (lineRenderer == null || hosePoints == null)
                return;

            // 유효한(null 아닌) 포인트만 모은다
            int valid = 0;
            for (int i = 0; i < hosePoints.Count; i++)
            {
                if (hosePoints[i] != null)
                    valid++;
            }

            // 2개 미만이면 렌더링하지 않음
            if (valid < 2)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            if (lineRenderer.positionCount != valid)
                lineRenderer.positionCount = valid;

            int idx = 0;
            for (int i = 0; i < hosePoints.Count; i++)
            {
                if (hosePoints[i] == null)
                    continue; // null point는 스킵

                lineRenderer.SetPosition(idx, hosePoints[i].position);
                idx++;
            }
        }
    }
}
