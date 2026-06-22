using UnityEngine;
using UnityEngine.Events;

namespace FireSafetyVR
{
    /// <summary>
    /// 소화전 전체 상태(밸브 열림/닫힘)를 관리하고 노즐에 전달한다.
    /// VR 입력(XR Interaction Toolkit) UnityEvent에서 연결하기 쉬운 public 메서드 중심.
    /// </summary>
    public class FireHydrantSystem : MonoBehaviour
    {
        [Header("밸브 상태")]
        [SerializeField] private bool valveOpened;

        [Header("연결")]
        [Tooltip("이 소화전이 제어하는 노즐")]
        [SerializeField] private FireHoseNozzle nozzle;

        [Header("이벤트 (선택)")]
        public UnityEvent onValveOpened;
        public UnityEvent onValveClosed;

        // 트리거 입력 상태 보관 (밸브를 나중에 열어도 일관되게 반영)
        private bool triggerPressed;

        /// <summary>현재 밸브가 열려 있는지.</summary>
        public bool ValveOpened => valveOpened;

        private void Start()
        {
            // 초기 상태를 노즐에 동기화
            ApplyValveState();
            ApplySprayState();
        }

        /// <summary>밸브 열기.</summary>
        public void OpenValve()
        {
            valveOpened = true;
            ApplyValveState();
            ApplySprayState();
            onValveOpened?.Invoke();
        }

        /// <summary>밸브 닫기.</summary>
        public void CloseValve()
        {
            valveOpened = false;
            ApplyValveState();
            ApplySprayState(); // 닫히면 분사 중지
            onValveClosed?.Invoke();
        }

        /// <summary>밸브 토글 (버튼 1개로 여닫기).</summary>
        public void ToggleValve()
        {
            if (valveOpened)
                CloseValve();
            else
                OpenValve();
        }

        /// <summary>
        /// 노즐 트리거 입력 상태 전달.
        /// XR Grab Interactable의 Activate(true)/Deactivate(false)에 연결.
        /// </summary>
        public void SetTriggerPressed(bool pressed)
        {
            triggerPressed = pressed;
            ApplySprayState();
        }

        // --- 내부 동기화 ---

        private void ApplyValveState()
        {
            if (nozzle != null)
                nozzle.SetValveOpened(valveOpened);
            else
                Debug.LogWarning("[FireHydrantSystem] nozzle이 연결되지 않았습니다.", this);
        }

        private void ApplySprayState()
        {
            if (nozzle == null)
                return;

            // 밸브가 열려 있고 트리거가 눌렸을 때만 분사
            nozzle.SetSpray(valveOpened && triggerPressed);
        }
    }
}
