using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Left Controller에 부착한다.
// 이 컨트롤러의 인터랙터가 무언가를 잡았을 때, 잡힌 오브젝트의 Cube.cubeData가 HandPoseData이면
// 기본 Grip 애니메이션 대신 그 포즈로 손모양을 고정한다(Animator를 끔). 놓으면 Animator를 복구한다.
// 손 비주얼이 LeftHand(XR Hands) 스켈레톤이라 같은 본 이름(L_*)으로 로컬 회전을 직접 복사한다.
public class ControllerGrabHandPose : MonoBehaviour
{
    [Tooltip("그랩 이벤트를 받을 인터랙터. 비우면 자식에서 자동 검색.")]
    [SerializeField] private XRBaseInteractor interactor;

    [Tooltip("손 비주얼의 Animator. 비우면 자식에서 자동 검색.")]
    [SerializeField] private Animator handAnimator;

    [Tooltip("손 본 루트(L_Wrist를 포함하는 손 비주얼). 비우면 handAnimator의 오브젝트 사용.")]
    [SerializeField] private Transform handRoot;

    const string k_JointPrefix = "L_";

    private bool _poseApplied;

    private void Awake() => ResolveRefs();

    private void ResolveRefs()
    {
        if (interactor == null)
            interactor = GetComponentInChildren<XRBaseInteractor>(true);
        if (handAnimator == null)
            handAnimator = GetComponentInChildren<Animator>(true);
        if (handRoot == null && handAnimator != null)
            handRoot = handAnimator.transform;
    }

    private void OnEnable()
    {
        ResolveRefs();
        if (interactor != null)
        {
            interactor.selectEntered.AddListener(OnSelectEntered);
            interactor.selectExited.AddListener(OnSelectExited);
        }
        else
        {
            Debug.LogWarning("[ControllerGrabHandPose] 인터랙터를 찾지 못했습니다.", this);
        }
    }

    private void OnDisable()
    {
        if (interactor != null)
        {
            interactor.selectEntered.RemoveListener(OnSelectEntered);
            interactor.selectExited.RemoveListener(OnSelectExited);
        }
        RestoreAnimator();
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var pose = ResolvePose(args.interactableObject);
        if (pose != null)
            ApplyPose(pose);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (_poseApplied)
            RestoreAnimator();
    }

    private static HandPoseData ResolvePose(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        if (interactable?.transform == null)
            return null;

        var cube = interactable.transform.GetComponentInParent<Cube>();
        return cube != null ? cube.cubeData as HandPoseData : null;
    }

    private void ApplyPose(HandPoseData pose)
    {
        if (handRoot == null)
        {
            Debug.LogWarning("[ControllerGrabHandPose] handRoot가 없어 포즈를 적용할 수 없습니다.", this);
            return;
        }

        // Grip 애니메이션이 매 프레임 본을 덮어쓰지 않도록 Animator를 끈다.
        if (handAnimator != null)
            handAnimator.enabled = false;

        var bones = BuildNameMap(handRoot);
        foreach (var j in pose.joints)
        {
            if (j.jointID == XRHandJointID.Wrist)
                continue; // 손목은 컨트롤러 배치 유지

            if (bones.TryGetValue(k_JointPrefix + j.jointID, out var bone))
                bone.localRotation = j.localRotation;
        }

        _poseApplied = true;
    }

    private void RestoreAnimator()
    {
        if (handAnimator != null)
            handAnimator.enabled = true;
        _poseApplied = false;
    }

    private static Dictionary<string, Transform> BuildNameMap(Transform root)
    {
        var map = new Dictionary<string, Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            map[t.name] = t;
        return map;
    }
}
