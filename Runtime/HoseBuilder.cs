using System.Collections.Generic;
using UnityEngine;

namespace FireSafetyVR
{
    /// <summary>
    /// [2차 확장용] Rigidbody 세그먼트 체인으로 호스를 자동 생성한다.
    /// 첫 세그먼트는 startAnchor에 고정, 마지막은 nozzleRigidbody에 연결.
    /// 물리 체인은 불안정할 수 있으므로 1차 프로토타입에서는 비활성화해도 됨.
    /// 생성된 Transform 리스트는 HosePointRenderer가 참조해 LineRenderer로 그릴 수 있다.
    /// </summary>
    public class HoseBuilder : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("호스 첫 세그먼트를 고정할 시작 앵커 (HoseStartPoint)")]
        [SerializeField] private Transform startAnchor;

        [Tooltip("세그먼트로 사용할 프리팹. 없으면 Capsule Primitive를 자동 생성")]
        [SerializeField] private GameObject segmentPrefab;

        [Tooltip("호스 끝에 연결할 노즐 Rigidbody")]
        [SerializeField] private Rigidbody nozzleRigidbody;

        [Header("생성 설정 (VR 성능 고려: 20~30 권장)")]
        [SerializeField] private int segmentCount = 25;
        [SerializeField] private float segmentLength = 0.15f;
        [SerializeField] private float segmentRadius = 0.04f;
        [SerializeField] private float segmentMass = 0.05f;

        [Header("동작")]
        [Tooltip("Start에서 자동 생성 여부. 1차 프로토타입에서는 꺼두기")]
        [SerializeField] private bool buildOnStart = false;

        // 생성된 세그먼트 Transform 목록 (외부 렌더러가 참조)
        private readonly List<Transform> segments = new List<Transform>();

        /// <summary>생성된 세그먼트 Transform 리스트 (읽기 전용 참조).</summary>
        public List<Transform> Segments => segments;

        private void Start()
        {
            if (buildOnStart)
                Build();
        }

        /// <summary>
        /// 세그먼트 체인을 생성하고 Joint로 연결한다.
        /// </summary>
        public void Build()
        {
            if (startAnchor == null)
            {
                Debug.LogWarning("[HoseBuilder] startAnchor가 없어 생성을 중단합니다.", this);
                return;
            }

            Clear();

            Rigidbody previousBody = null;
            Vector3 dir = startAnchor.forward; // 시작 앵커가 바라보는 방향으로 펼침

            for (int i = 0; i < Mathf.Max(1, segmentCount); i++)
            {
                Vector3 pos = startAnchor.position + dir * (segmentLength * i);
                GameObject segObj = CreateSegment(i, pos);

                Rigidbody rb = segObj.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = segObj.AddComponent<Rigidbody>();

                rb.mass = segmentMass;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                if (i == 0)
                {
                    // 첫 세그먼트는 앵커에 고정 (FixedJoint를 앵커 Rigidbody가 없으면 connectedBody=null로 월드 고정)
                    rb.isKinematic = true;
                }
                else
                {
                    rb.isKinematic = false;
                    ConnectJoint(rb, previousBody);
                }

                segments.Add(segObj.transform);
                previousBody = rb;
            }

            // 마지막 세그먼트를 노즐 Rigidbody에 연결
            if (nozzleRigidbody != null && previousBody != null)
            {
                CharacterJoint nozzleJoint = nozzleRigidbody.gameObject.AddComponent<CharacterJoint>();
                nozzleJoint.connectedBody = previousBody;
            }
            else if (nozzleRigidbody == null)
            {
                Debug.LogWarning("[HoseBuilder] nozzleRigidbody가 없어 호스 끝이 노즐에 연결되지 않았습니다.", this);
            }
        }

        /// <summary>
        /// 생성된 세그먼트를 모두 제거한다.
        /// </summary>
        public void Clear()
        {
            foreach (Transform t in segments)
            {
                if (t == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(t.gameObject);
                else
                    DestroyImmediate(t.gameObject);
            }
            segments.Clear();
        }

        /// <summary>
        /// 세그먼트 GameObject 1개 생성 (프리팹 없으면 Capsule Primitive).
        /// </summary>
        private GameObject CreateSegment(int index, Vector3 pos)
        {
            GameObject obj;

            if (segmentPrefab != null)
            {
                obj = Instantiate(segmentPrefab, pos, Quaternion.identity, transform);
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                obj.transform.SetParent(transform, true);
                obj.transform.position = pos;
                obj.transform.localScale = new Vector3(segmentRadius * 2f, segmentLength * 0.5f, segmentRadius * 2f);

                // 기본 Capsule Collider 보장 (Primitive에는 이미 있음)
                if (obj.GetComponent<CapsuleCollider>() == null)
                    obj.AddComponent<CapsuleCollider>();
            }

            obj.name = $"HoseSegment_{index:00}";
            return obj;
        }

        /// <summary>
        /// 두 Rigidbody를 CharacterJoint로 연결한다.
        /// </summary>
        private void ConnectJoint(Rigidbody body, Rigidbody connectTo)
        {
            if (connectTo == null)
                return;

            CharacterJoint joint = body.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = connectTo;

            // 약간의 흔들림 한계 설정 (자연스러운 처짐)
            SoftJointLimit lowTwist = joint.lowTwistLimit;
            lowTwist.limit = -20f;
            joint.lowTwistLimit = lowTwist;

            SoftJointLimit highTwist = joint.highTwistLimit;
            highTwist.limit = 20f;
            joint.highTwistLimit = highTwist;

            SoftJointLimit swing1 = joint.swing1Limit;
            swing1.limit = 40f;
            joint.swing1Limit = swing1;
        }
    }
}
