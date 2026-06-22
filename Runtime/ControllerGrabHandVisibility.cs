using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Left Controller에 부착한다.
// 이 컨트롤러의 인터랙터가 무언가를 잡으면 LeftHand 비주얼을 숨기고, 놓으면 다시 보이게 한다.
public class ControllerGrabHandVisibility : MonoBehaviour
{
    [Tooltip("그랩 이벤트를 받을 인터랙터. 비우면 자식에서 자동 검색.")]
    [SerializeField] private XRBaseInteractor interactor;

    [Tooltip("잡을 때 비활성화할 LeftHand 오브젝트. 비우면 자식에서 이름으로 검색.")]
    [SerializeField] private GameObject leftHand;

    private bool _hiddenByGrab;

    private void Awake() => ResolveRefs();

    private void ResolveRefs()
    {
        if (interactor == null)
            interactor = GetComponentInChildren<XRBaseInteractor>(true);

        if (leftHand == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "LeftHand")
                {
                    leftHand = t.gameObject;
                    break;
                }
            }
        }
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
            Debug.LogWarning("[ControllerGrabHandVisibility] 인터랙터를 찾지 못했습니다.", this);
        }

        if (leftHand == null)
            Debug.LogWarning("[ControllerGrabHandVisibility] LeftHand를 찾지 못했습니다.", this);
    }

    private void OnDisable()
    {
        if (interactor != null)
        {
            interactor.selectEntered.RemoveListener(OnSelectEntered);
            interactor.selectExited.RemoveListener(OnSelectExited);
        }

        RestoreLeftHand();
    }

    private void OnSelectEntered(SelectEnterEventArgs args) => SetLeftHandActive(false);

    private void OnSelectExited(SelectExitEventArgs args) => RestoreLeftHand();

    private void SetLeftHandActive(bool active)
    {
        if (leftHand == null)
            return;

        if (!active)
        {
            if (leftHand.activeSelf)
            {
                leftHand.SetActive(false);
                _hiddenByGrab = true;
            }
        }
        else if (_hiddenByGrab)
        {
            leftHand.SetActive(true);
            _hiddenByGrab = false;
        }
    }

    private void RestoreLeftHand() => SetLeftHandActive(true);
}
