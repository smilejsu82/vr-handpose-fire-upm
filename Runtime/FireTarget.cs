using UnityEngine;
using UnityEngine.Events;

namespace FireSafetyVR
{
    /// <summary>
    /// 불 오브젝트의 HP를 관리한다.
    /// 물에 맞으면 Extinguish로 HP가 감소하고, 0 이하가 되면 불이 꺼진다.
    /// </summary>
    public class FireTarget : MonoBehaviour
    {
        [Header("체력")]
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float currentHp;

        [Header("연출 대상 (둘 다 선택)")]
        [Tooltip("꺼질 때 Stop()을 호출할 불 파티클")]
        [SerializeField] private ParticleSystem fireParticle;

        [Tooltip("꺼질 때 SetActive(false)할 불 비주얼 루트")]
        [SerializeField] private GameObject fireVisualRoot;

        [Header("이벤트")]
        [Tooltip("불이 완전히 꺼졌을 때 1회 호출")]
        public UnityEvent onExtinguished;

        // 중복 꺼짐 처리 방지 플래그
        private bool isExtinguished;

        /// <summary>현재 HP 비율 (0~1). UI 게이지 등에 사용.</summary>
        public float HpRatio => maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;

        /// <summary>이미 꺼졌는지 여부.</summary>
        public bool IsExtinguished => isExtinguished;

        private void Start()
        {
            currentHp = maxHp;
            isExtinguished = false;
        }

        /// <summary>
        /// 물에 맞았을 때 호출. amount만큼 HP를 깎는다.
        /// </summary>
        public void Extinguish(float amount)
        {
            if (isExtinguished)
                return;

            if (amount <= 0f)
                return;

            currentHp -= amount;

            if (currentHp <= 0f)
            {
                currentHp = 0f;
                OnExtinguished();
            }
        }

        /// <summary>
        /// 불을 다시 켠다 (재시작/리셋용).
        /// </summary>
        public void ResetFire()
        {
            currentHp = maxHp;
            isExtinguished = false;

            if (fireVisualRoot != null)
                fireVisualRoot.SetActive(true);

            if (fireParticle != null)
                fireParticle.Play();
        }

        /// <summary>
        /// HP가 0 이하가 되어 불이 꺼질 때 1회 실행.
        /// </summary>
        private void OnExtinguished()
        {
            if (isExtinguished)
                return;

            isExtinguished = true;

            if (fireParticle != null)
                fireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (fireVisualRoot != null)
                fireVisualRoot.SetActive(false);

            onExtinguished?.Invoke();
        }
    }
}
