using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// 라이플을 오른손으로 잡으면:
//  - 오른손 손 모델의 Animator를 정지하고
//  - 지정한 HandPoseData(녹화된 그립 포즈)대로 손가락 본을 고정한다.
// 그립을 풀면:
//  - Animator를 다시 켜서 원래 상태(open 등)의 애니메이션이 이어지게 한다.
//
// 손가락 본은 R_ + XRHandJointID 이름 규칙(R_Wrist, R_IndexProximal ...)을 사용한다.
[RequireComponent(typeof(XRGrabInteractable))]
public class Rifle : MonoBehaviour
{
    [Header("그립 포즈")]
    [Tooltip("오른손으로 잡았을 때 적용할 녹화 포즈(HandPoseData ScriptableObject).")]
    [SerializeField] private HandPoseData rightHandPose;

    [Tooltip("손가락 본 이름 접두사. 본 이름 = 접두사 + XRHandJointID (예: R_IndexProximal).")]
    [SerializeField] private string boneNamePrefix = "R_";

    [Tooltip("손목(Wrist)은 적용하지 않아 손의 부착 위치/방향을 유지한다. 손가락 모양만 바꾸려면 켜둔다.")]
    [SerializeField] private bool keepWristPlacement = true;

    [Header("선택")]
    [Tooltip("비워두면 잡은 인터랙터에서 오른손 Animator를 자동으로 찾는다.")]
    [SerializeField] private Animator rightHandAnimatorOverride;

    private XRGrabInteractable grab;

    // 현재 정지시킨 애니메이터 (그립 해제 시 다시 켠다)
    private Animator suspendedAnimator;
    // 정지 직전 enabled 상태 복원용
    private bool animatorWasEnabled;

    // 그립 중 라이플에 고정할 손목 본 (attachPose 있을 때만)
    private Transform lockedWrist;
    private bool lockToRifle;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (grab == null)
            return;
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        if (grab == null)
            return;
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);

        // 비활성화될 때 정지해둔 애니메이터가 있으면 복구
        RestoreAnimator();
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (rightHandPose == null)
        {
            Debug.LogWarning("[Rifle] rightHandPose(HandPoseData)가 할당되지 않았습니다.", this);
            return;
        }

        var hand = ResolveRightHandAnimator(args.interactorObject);
        if (hand == null)
            return; // 오른손(애니메이터+R_본)이 아니면 무시 (왼손/핸드트래킹 등)

        // 이미 다른 손을 정지 중이면 먼저 복구
        if (suspendedAnimator != null && suspendedAnimator != hand)
            RestoreAnimator();

        suspendedAnimator = hand;
        animatorWasEnabled = hand.enabled;
        hand.enabled = false; // 애니메이터 정지 → 우리가 설정한 손가락 회전이 유지됨

        ApplyPose(hand.transform);

        // 손목을 라이플 기준 포즈에 고정 (attachPose가 저장된 SO일 때만)
        lockToRifle = lockedWrist != null && rightHandPose.hasAttachPose;
        if (rightHandPose.hasAttachPose && lockedWrist == null)
            Debug.LogWarning("[Rifle] attachPose는 있는데 손목 본을 못 찾았습니다.", this);
        else if (!rightHandPose.hasAttachPose)
            Debug.LogWarning("[Rifle] 이 포즈에는 attachPose가 없습니다(옛 SO). Attach to Target으로 다시 저장하면 손목까지 고정됩니다.", this);
    }

    private void LateUpdate()
    {
        // 그립 중에는 매 프레임 손목을 라이플 그립 포즈로 고정 (컨트롤러를 따라가지 않게)
        if (!lockToRifle || lockedWrist == null)
            return;

        lockedWrist.position = transform.TransformPoint(rightHandPose.attachLocalPosition);
        lockedWrist.rotation = transform.rotation * rightHandPose.attachLocalRotation;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        lockToRifle = false;
        lockedWrist = null;
        RestoreAnimator();
    }

    // 정지해둔 애니메이터를 다시 켜서 원래 애니메이션(open 등)을 이어가게 한다.
    private void RestoreAnimator()
    {
        if (suspendedAnimator == null)
            return;

        suspendedAnimator.enabled = animatorWasEnabled;
        suspendedAnimator = null;
    }

    // 잡은 인터랙터에서 위로 올라가며 R_ 본을 가진 손 Animator를 찾는다.
    private Animator ResolveRightHandAnimator(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor)
    {
        if (rightHandAnimatorOverride != null)
            return rightHandAnimatorOverride;

        if (interactor == null)
            return null;

        string wristName = boneNamePrefix + XRHandJointID.Wrist; // "R_Wrist"
        Transform t = interactor.transform;
        while (t != null)
        {
            foreach (var anim in t.GetComponentsInChildren<Animator>(true))
            {
                if (HasBone(anim.transform, wristName))
                    return anim;
            }
            t = t.parent;
        }
        return null;
    }

    // 포즈의 각 관절 로컬 회전을 같은 이름(R_ + jointID)의 본에 그대로 적용한다.
    private void ApplyPose(Transform handRoot)
    {
        var map = new Dictionary<string, Transform>();
        foreach (var tr in handRoot.GetComponentsInChildren<Transform>(true))
            map[tr.name] = tr;

        // 라이플 고정에 쓸 손목 본 확보
        map.TryGetValue(boneNamePrefix + XRHandJointID.Wrist, out lockedWrist);

        int applied = 0;
        foreach (var j in rightHandPose.joints)
        {
            if (keepWristPlacement && j.jointID == XRHandJointID.Wrist)
                continue;

            if (map.TryGetValue(boneNamePrefix + j.jointID, out var bone))
            {
                bone.localRotation = j.localRotation;
                applied++;
            }
        }

        if (applied == 0)
            Debug.LogWarning($"[Rifle] 일치하는 본을 찾지 못했습니다. 접두사 '{boneNamePrefix}'와 손 스켈레톤을 확인하세요.", this);
    }

    private static bool HasBone(Transform root, string boneName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == boneName)
                return true;
        return false;
    }
}
