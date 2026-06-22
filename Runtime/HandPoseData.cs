using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

// 핸드트래킹으로 캡처한 한 순간의 손모양(각 관절의 로컬 회전)을 저장하는 에셋.
// 같은 LeftHand 스켈레톤에 그대로 다시 적용하면 동일한 포즈가 재현된다.
[CreateAssetMenu(fileName = "HandPose", menuName = "Hand Pose/Hand Pose Data")]
public class HandPoseData : ScriptableObject
{
    public Handedness handedness = Handedness.Left;

    [Tooltip("손목(루트) 트랜스폼의 로컬 포즈도 저장할지 여부. 손모양만 재현하려면 불필요하다.")]
    public bool hasRootPose;
    public Vector3 rootLocalPosition;
    public Quaternion rootLocalRotation = Quaternion.identity;

    [Header("Attach 기준 손목 포즈 (Attach to Target 시 저장 · 타깃 로컬)")]
    [Tooltip("Attach to Target으로 저장됐는지. true면 그립 시 손목을 타깃(예: 라이플) 기준 이 포즈에 고정할 수 있다.")]
    public bool hasAttachPose;
    [Tooltip("Attach 타깃 기준 손목 로컬 위치.")]
    public Vector3 attachLocalPosition;
    [Tooltip("Attach 타깃 기준 손목 로컬 회전.")]
    public Quaternion attachLocalRotation = Quaternion.identity;

    public List<JointPose> joints = new List<JointPose>();

    // XR Hands 관절 ID와 그 관절을 구동하는 트랜스폼의 로컬 포즈.
    [Serializable]
    public struct JointPose
    {
        public XRHandJointID jointID;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
}
