using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

// LeftHand(XR Hands)에서 녹화한 HandPoseData를 OculusHand_L 본 구조로 "리타게팅"해 적용한다.
//
// 두 스켈레톤은 본 이름/계층/관절 수가 달라 로컬 회전을 그대로 복사하면 손이 깨진다.
// 그래서 손목 기준(모델 공간)에서의 회전 변화량(D = pose * bind^-1)을 계산해,
// 타깃의 바인드 포즈에 같은 변화량을 적용한다. 이때 각 스켈레톤의 바인드 축 차이는 자동으로 상쇄된다.
public class HandPoseApplier : MonoBehaviour
{
    [Tooltip("적용할 녹화 포즈 (LeftHand 기준).")]
    public HandPoseData pose;

    [Tooltip("리타게팅 기준이 되는 소스 스켈레톤(LeftHand fbx). 바인드 포즈 회전을 읽는다.")]
    public GameObject sourceBindSkeleton;

    [Tooltip("타깃 바인드 기준(OculusHand_L 프리팹). 비우면 이 오브젝트의 현재 포즈를 바인드로 간주한다.")]
    public GameObject targetBindSkeleton;

    [Tooltip("활성화(Play 진입 등) 시 자동으로 포즈를 적용.")]
    public bool applyOnEnable = true;

    // XR 손가락별 관절 순서 (Wrist 제외, Metacarpal부터 말단까지). 모델 공간 누적에 사용.
    static readonly XRHandJointID[][] k_Fingers =
    {
        new[]{ XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal },
        new[]{ XRHandJointID.IndexMetacarpal, XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal },
        new[]{ XRHandJointID.MiddleMetacarpal, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal },
        new[]{ XRHandJointID.RingMetacarpal, XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal },
        new[]{ XRHandJointID.LittleMetacarpal, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal },
    };

    // XR 관절 -> Oculus 본 이름. 대응 본이 없는 관절(예: 검지/중지/약지 Metacarpal, Palm, Tip)은 제외한다.
    static readonly Dictionary<XRHandJointID, string> k_JointToBone = new Dictionary<XRHandJointID, string>
    {
        { XRHandJointID.ThumbMetacarpal, "b_l_thumb0" },
        { XRHandJointID.ThumbProximal,   "b_l_thumb1" },
        { XRHandJointID.ThumbDistal,     "b_l_thumb2" },

        { XRHandJointID.IndexProximal,     "b_l_index1" },
        { XRHandJointID.IndexIntermediate, "b_l_index2" },
        { XRHandJointID.IndexDistal,       "b_l_index3" },

        { XRHandJointID.MiddleProximal,     "b_l_middle1" },
        { XRHandJointID.MiddleIntermediate, "b_l_middle2" },
        { XRHandJointID.MiddleDistal,       "b_l_middle3" },

        { XRHandJointID.RingProximal,     "b_l_ring1" },
        { XRHandJointID.RingIntermediate, "b_l_ring2" },
        { XRHandJointID.RingDistal,       "b_l_ring3" },

        { XRHandJointID.LittleMetacarpal,   "b_l_pinky0" },
        { XRHandJointID.LittleProximal,     "b_l_pinky1" },
        { XRHandJointID.LittleIntermediate, "b_l_pinky2" },
        { XRHandJointID.LittleDistal,       "b_l_pinky3" },
    };

    const string k_TargetWristName = "b_l_wrist";
    const string k_SourceJointPrefix = "L_"; // LeftHand fbx 본 이름 = "L_" + XRHandJointID

    void OnEnable()
    {
        if (applyOnEnable)
            ApplyPose();
    }

    public void ApplyPose()
    {
        if (pose == null)
        {
            Debug.LogWarning("[HandPoseApplier] pose가 할당되지 않았습니다.", this);
            return;
        }
        if (sourceBindSkeleton == null)
        {
            Debug.LogWarning("[HandPoseApplier] sourceBindSkeleton(LeftHand fbx)이 할당되지 않았습니다.", this);
            return;
        }

        var sceneBones = BuildNameMap(transform);
        if (!sceneBones.TryGetValue(k_TargetWristName, out var sceneWrist))
        {
            Debug.LogWarning($"[HandPoseApplier] 타깃에서 '{k_TargetWristName}' 본을 찾지 못했습니다.", this);
            return;
        }

        // 소스 바인드 로컬 회전 (LeftHand fbx)
        var srcBones = BuildNameMap(sourceBindSkeleton.transform);

        // 녹화된 소스 포즈 로컬 회전
        var poseLocal = new Dictionary<XRHandJointID, Quaternion>();
        foreach (var j in pose.joints)
            poseLocal[j.jointID] = j.localRotation;

        // 타깃 바인드 기준(프리팹). 없으면 자기 자신을 바인드로 사용.
        Transform tgtBindRoot = targetBindSkeleton != null ? targetBindSkeleton.transform : transform;
        var tgtBindBones = BuildNameMap(tgtBindRoot);
        tgtBindBones.TryGetValue(k_TargetWristName, out var tgtBindWrist);

        foreach (var finger in k_Fingers)
        {
            // 손목 기준 누적 회전 (모델 공간)
            Quaternion accPose = Quaternion.identity;
            Quaternion accBind = Quaternion.identity;

            foreach (var jid in finger)
            {
                if (!poseLocal.TryGetValue(jid, out var localPose))
                    localPose = Quaternion.identity;

                Quaternion localBind = Quaternion.identity;
                if (srcBones.TryGetValue(k_SourceJointPrefix + jid, out var srcBone))
                    localBind = srcBone.localRotation;

                accPose = accPose * localPose;
                accBind = accBind * localBind;

                // 매핑되는 본만 실제로 구동 (Metacarpal 등 미매핑 관절도 누적에는 기여)
                if (!k_JointToBone.TryGetValue(jid, out var boneName))
                    continue;
                if (!sceneBones.TryGetValue(boneName, out var sceneBone))
                    continue;

                // 타깃 바인드의 손목 기준 회전
                Quaternion mTargetBind = Quaternion.identity;
                if (tgtBindWrist != null && tgtBindBones.TryGetValue(boneName, out var tgtBindBone))
                    mTargetBind = Quaternion.Inverse(tgtBindWrist.rotation) * tgtBindBone.rotation;

                // 소스의 손목 기준 회전 변화량 D, 타깃 바인드에 적용
                Quaternion delta = accPose * Quaternion.Inverse(accBind);
                sceneBone.rotation = sceneWrist.rotation * delta * mTargetBind;
            }
        }
    }

    // 타깃 본들을 바인드 포즈(프리팹 기준)로 되돌린다.
    public void ResetToBind()
    {
        if (targetBindSkeleton == null)
        {
            Debug.LogWarning("[HandPoseApplier] targetBindSkeleton이 없어 바인드로 되돌릴 수 없습니다.", this);
            return;
        }

        var bind = BuildNameMap(targetBindSkeleton.transform);
        var scene = BuildNameMap(transform);
        foreach (var kv in scene)
            if (bind.TryGetValue(kv.Key, out var b))
                kv.Value.localRotation = b.localRotation;
    }

    static Dictionary<string, Transform> BuildNameMap(Transform root)
    {
        var map = new Dictionary<string, Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            map[t.name] = t; // 본 이름은 손 스켈레톤 내에서 유일
        return map;
    }

#if UNITY_EDITOR
    void Reset()
    {
        if (sourceBindSkeleton == null)
            sourceBindSkeleton = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Models/LeftHand.fbx");
        if (targetBindSkeleton == null)
            targetBindSkeleton = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/OculusHand_L.prefab");
    }
#endif
}
