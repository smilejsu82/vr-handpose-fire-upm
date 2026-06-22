using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

// 녹화한 HandPoseData를 "같은 스켈레톤"(LeftHand)에 그대로 적용한다.
// 캡처 대상과 재생 대상이 동일하므로 리타게팅 없이 로컬 회전을 1:1 복사하면 정확히 같은 손모양이 된다.
// LeftHand 루트(또는 본을 포함한 부모)에 부착해서 사용한다.
public class HandPoseDirectApplier : MonoBehaviour
{
    [Tooltip("적용할 녹화 포즈.")]
    public HandPoseData pose;

    [Tooltip("손목(Wrist)은 적용하지 않고 씬에 배치한 손의 위치/방향을 유지한다. 손모양만 바꾸려면 켜둔다.")]
    public bool keepWristPlacement = true;

    [Tooltip("활성화(Play 진입 등) 시 자동으로 포즈를 적용.")]
    public bool applyOnEnable = true;

    // LeftHand fbx 본 이름 = "L_" + XRHandJointID (예: L_IndexProximal)
    const string k_JointPrefix = "L_";

    void OnEnable()
    {
        if (applyOnEnable)
            ApplyPose();
    }

    public void ApplyPose()
    {
        if (pose == null)
        {
            Debug.LogWarning("[HandPoseDirectApplier] pose가 할당되지 않았습니다.", this);
            return;
        }

        var bones = BuildNameMap(transform);
        int applied = 0;
        foreach (var j in pose.joints)
        {
            if (keepWristPlacement && j.jointID == XRHandJointID.Wrist)
                continue;

            if (bones.TryGetValue(k_JointPrefix + j.jointID, out var bone))
            {
                bone.localRotation = j.localRotation;
                applied++;
            }
        }

        if (applied == 0)
            Debug.LogWarning("[HandPoseDirectApplier] 일치하는 본을 찾지 못했습니다. LeftHand 스켈레톤(루트)에 부착했는지 확인하세요.", this);
    }

    static Dictionary<string, Transform> BuildNameMap(Transform root)
    {
        var map = new Dictionary<string, Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            map[t.name] = t; // 본 이름은 스켈레톤 내에서 유일
        return map;
    }
}
