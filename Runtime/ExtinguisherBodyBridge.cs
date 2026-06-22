using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FireSafetyVR
{
    /// <summary>
    /// 소화기 본체(왼손으로 잡는 오브젝트)의 XR Grab Interactable을 감지해,
    /// 본체를 잡으면 "사용 준비"(밸브 열림), 놓으면 "사용 불가"(밸브 닫힘) 상태로 전환한다.
    /// 즉, 본체를 들고 있을 때만 노즐 트리거로 분사가 가능해지는 두 손 조작을 구현한다.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ExtinguisherBodyBridge : MonoBehaviour
    {
        [Tooltip("상태를 전달할 소화기 시스템. 비우면 씬에서 자동 검색")]
        [SerializeField] private FireHydrantSystem system;

        private XRGrabInteractable grab;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            if (system == null)
                system = FindFirstObjectByType<FireHydrantSystem>();
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
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            if (system != null)
                system.OpenValve();
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            if (system != null)
                system.CloseValve();
        }
    }
}
