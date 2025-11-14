using UnityEngine;
using Animancer;

namespace Animation
{
    /// <summary>
    /// 애니메이션 몽타쥬 사용 예제
    /// </summary>
    public class MontagePlayerExample : MonoBehaviour
    {
        [Header("Montage Settings")]
        [SerializeField] private AnimationMontage attackMontage;
        [SerializeField] private AnimationMontage reloadMontage;
        
        private MontagePlayer _montagePlayer;
        
        private void Awake()
        {
            _montagePlayer = GetComponent<MontagePlayer>();
            
            // 이벤트 구독
            _montagePlayer.OnMontageStarted += OnMontageStarted;
            _montagePlayer.OnMontageEnded += OnMontageEnded;
            _montagePlayer.OnSectionChanged += OnSectionChanged;
            _montagePlayer.OnNotifyTriggered += OnNotifyTriggered;
        }
        
        private void Update()
        {
            // 테스트 입력
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                // 공격 몽타쥬 재생
                _montagePlayer.PlayMontage(attackMontage);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                // 특정 섹션부터 재생
                _montagePlayer.PlayMontage(attackMontage, "ComboFinish");
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                // 재장전 몽타쥬 재생
                _montagePlayer.PlayMontage(reloadMontage);
            }
            
            if (Input.GetKeyDown(KeyCode.J))
            {
                // 섹션 점프
                _montagePlayer.JumpToSection("Loop");
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                // 몽타쥬 정지
                _montagePlayer.StopMontage();
            }
            
            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                // 재생 속도 증가
                _montagePlayer.SetPlayRate(2.0f);
            }
            
            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                // 재생 속도 감소
                _montagePlayer.SetPlayRate(0.5f);
            }
        }
        
        private void OnMontageStarted(AnimationMontage montage)
        {
            Debug.Log($"Montage Started: {montage.MontageName}");
        }
        
        private void OnMontageEnded(AnimationMontage montage)
        {
            Debug.Log($"Montage Ended: {montage.MontageName}");
        }
        
        private void OnSectionChanged(MontageSection section)
        {
            Debug.Log($"Section Changed: {section.SectionName}");
        }
        
        private void OnNotifyTriggered(string notifyName)
        {
            Debug.Log($"Notify Triggered: {notifyName}");
            
            // 노티파이에 따라 특정 로직 실행
            switch (notifyName)
            {
                case "SpawnProjectile":
                    // 발사체 생성
                    break;
                case "PlaySound":
                    // 사운드 재생
                    break;
                case "EnableWeaponCollision":
                    // 무기 콜라이더 활성화
                    break;
            }
        }
        
        private void OnDestroy()
        {
            if (_montagePlayer != null)
            {
                _montagePlayer.OnMontageStarted -= OnMontageStarted;
                _montagePlayer.OnMontageEnded -= OnMontageEnded;
                _montagePlayer.OnSectionChanged -= OnSectionChanged;
                _montagePlayer.OnNotifyTriggered -= OnNotifyTriggered;
            }
        }
    }
}
