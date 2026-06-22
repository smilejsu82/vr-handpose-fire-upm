using UnityEngine;

// HandPoseRecorder로 저장한 포즈 스냅샷 루트.
// 프리팹 Instantiate/드래그 시 월드 위치가 먼저 잡힌 뒤 부모가 붙으면 localPos가 어긋난다.
// 손 위치는 L_Wrist 등 본 로컬 포즈에 baked 되어 있으므로, 루트는 항상 부모 원점(0,0,0)에 둔다.
[DisallowMultipleComponent]
[ExecuteAlways]
[DefaultExecutionOrder(32000)]
public class HandPoseSnapshotRoot : MonoBehaviour
{
    // Attach 베이크 당시 타깃 lossyScale. 프리팹 에셋(parent 없음) 검증 시 local→world 환산에 사용.
    [SerializeField] float m_BakeReferenceScale = 1f;
    [SerializeField] bool m_MeshFrozen;
    // 이 스냅샷을 베이크한 Hand Pose Recorder 버전. 옛 버전 프리팹(좌표 버그 포함)을 구분하는 데 사용.
    [SerializeField] string m_BakedVersion = "";

    public float BakeReferenceScale => m_BakeReferenceScale;
    public bool MeshFrozen => m_MeshFrozen;
    public string BakedVersion => m_BakedVersion;

    public void SetMeshFrozen(bool frozen) => m_MeshFrozen = frozen;
    public void SetBakedVersion(string version) => m_BakedVersion = version;

    public void SetBakeReferenceScale(Transform attachTarget)
    {
        if (attachTarget == null)
        {
            m_BakeReferenceScale = 1f;
            return;
        }

        var s = attachTarget.lossyScale;
        m_BakeReferenceScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        if (m_BakeReferenceScale <= 0f)
            m_BakeReferenceScale = 1f;
    }

    void Awake() => SnapToParentOrigin();
    void OnEnable() => SnapToParentOrigin();
    void OnTransformParentChanged() => SnapToParentOrigin();

    // Instantiate 직후·에디터 드래그 등 Awake 이후 transform이 다시 쓰이는 경우를 LateUpdate에서 보정한다.
    void LateUpdate() => SnapToParentOrigin();

    public void SnapToParentOrigin()
    {
        if (transform.parent == null)
            return;

        if (transform.localPosition == Vector3.zero && transform.localRotation == Quaternion.identity)
            return;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

#if UNITY_EDITOR
    void Reset() => SnapToParentOrigin();
#endif
}
