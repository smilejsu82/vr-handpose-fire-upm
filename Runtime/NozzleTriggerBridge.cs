using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FireSafetyVR
{
    /// <summary>
    /// 노즐의 XR Grab Interactable 트리거(Activate/Deactivate)를
    /// FireHydrantSystem.SetTriggerPressed로 코드에서 자동 연결한다.
    /// Inspector에서 UnityEvent를 손으로 연결하지 않아도 동작하도록 하는 브리지.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class NozzleTriggerBridge : MonoBehaviour
    {
        [Tooltip("트리거 입력을 전달할 소화전 시스템. 비우면 씬에서 자동 검색")]
        [SerializeField] private FireHydrantSystem hydrant;

        private XRGrabInteractable grab;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();

            if (hydrant == null)
                hydrant = FindFirstObjectByType<FireHydrantSystem>();
        }

        private void OnEnable()
        {
            if (grab == null)
                return;

            grab.activated.AddListener(OnActivated);
            grab.deactivated.AddListener(OnDeactivated);
        }

        private void OnDisable()
        {
            if (grab == null)
                return;

            grab.activated.RemoveListener(OnActivated);
            grab.deactivated.RemoveListener(OnDeactivated);
        }

        private void OnActivated(ActivateEventArgs args)
        {
            if (hydrant != null)
                hydrant.SetTriggerPressed(true);
        }

        private void OnDeactivated(DeactivateEventArgs args)
        {
            if (hydrant != null)
                hydrant.SetTriggerPressed(false);
        }
    }
}
