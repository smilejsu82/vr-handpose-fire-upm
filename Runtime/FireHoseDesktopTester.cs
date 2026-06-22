using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FireSafetyVR
{
    /// <summary>
    /// VR 헤드셋 없이 에디터 Play 모드에서 소화전 동작을 검증하기 위한 데스크톱 테스터.
    /// V 키: 밸브 토글 / B 키(누르고 있는 동안): 트리거 분사 / R 키: 불 리셋.
    /// 실제 빌드에서는 비활성화하거나 제거해도 무방하다.
    /// </summary>
    public class FireHoseDesktopTester : MonoBehaviour
    {
        [SerializeField] private FireHydrantSystem hydrant;
        [SerializeField] private FireTarget fireTarget;

        private void Awake()
        {
            if (hydrant == null)
                hydrant = FindFirstObjectByType<FireHydrantSystem>();
            if (fireTarget == null)
                fireTarget = FindFirstObjectByType<FireTarget>();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null)
                return;

            if (kb.vKey.wasPressedThisFrame && hydrant != null)
                hydrant.ToggleValve();

            if (hydrant != null)
            {
                if (kb.bKey.wasPressedThisFrame)
                    hydrant.SetTriggerPressed(true);
                if (kb.bKey.wasReleasedThisFrame)
                    hydrant.SetTriggerPressed(false);
            }

            if (kb.rKey.wasPressedThisFrame && fireTarget != null)
                fireTarget.ResetFire();
#else
            if (Input.GetKeyDown(KeyCode.V) && hydrant != null)
                hydrant.ToggleValve();
            if (Input.GetKeyDown(KeyCode.B) && hydrant != null)
                hydrant.SetTriggerPressed(true);
            if (Input.GetKeyUp(KeyCode.B) && hydrant != null)
                hydrant.SetTriggerPressed(false);
            if (Input.GetKeyDown(KeyCode.R) && fireTarget != null)
                fireTarget.ResetFire();
#endif
        }
    }
}
