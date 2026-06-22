using UnityEngine;
using UPlayGround.Data.Event;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 영역 진입/이탈 또는 외부 호출로 BGM 전환 이벤트를 발화하는 트리거.
    /// 보스룸·연출 구간 등에 배치하거나, 보스 스크립트가 Activate()/Deactivate()로 직접 구동한다.
    ///
    /// SoundManager가 BgmEvent를 Global 스코프로 구독하므로 이 컴포넌트는 SoundManager를
    /// 직접 참조하지 않는다(이벤트 기반 디커플).
    /// </summary>
    public class BgmTrigger : MonoBehaviour
    {
        public enum BgmTriggerMode
        {
            [Tooltip("현재 BGM 위에 임시로 덮어쓰고, 이탈/Deactivate 시 직전 곡으로 복귀 (보스전 등)")]
            Override,

            [Tooltip("평시 BGM 자체를 교체 (지역 BGM 등). 복귀 개념 없음")]
            ChangeBase,
        }

        [Tooltip("재생할 BGM의 SoundDatabase key")]
        [SerializeField] private string _bgmKey;

        [Tooltip("여러 곡을 번갈아 재생할 플레이리스트. 설정 시 _bgmKey보다 우선(ChangeBase 모드 권장). Override 모드에서는 무시됨.")]
        [SerializeField] private Data.Sound.BgmPlaylistSO _playlist;

        [SerializeField] private float _fadeTime = 1.5f;

        [SerializeField] private BgmTriggerMode _mode = BgmTriggerMode.Override;

        [Header("콜라이더 트리거")]
        [Tooltip("true면 플레이어가 콜라이더 영역에 진입할 때 자동 발화. false면 Activate() 호출로만 동작.")]
        [SerializeField] private bool _useColliderTrigger = true;

        [Tooltip("Override 모드에서 영역 이탈 시 직전 BGM으로 복귀한다.")]
        [SerializeField] private bool _restoreOnExit = true;

        [Tooltip("한 번만 발화하고 이후 진입은 무시한다.")]
        [SerializeField] private bool _triggerOnce = false;

        private bool _fired;

        private void Awake()
        {
            if (_useColliderTrigger && TryGetComponent(out Collider col))
                col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_useColliderTrigger) return;
            if (!IsPlayer(other)) return;

            Activate();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_useColliderTrigger || !_restoreOnExit) return;
            if (_mode != BgmTriggerMode.Override) return;
            if (!IsPlayer(other)) return;

            Deactivate();
        }

        /// <summary>외부(보스 스크립트·스토리 연출 등)에서 BGM 전환을 발화한다.</summary>
        public void Activate()
        {
            if (_triggerOnce && _fired) return;
            _fired = true;

            BgmEvent eventType = _mode == BgmTriggerMode.Override
                ? BgmEvent.Override
                : BgmEvent.Change;

            // 플레이리스트는 평시 BGM 교체(ChangeBase) 용도. Override(보스전 등)는 단일 곡만 사용한다.
            var playlist = _mode == BgmTriggerMode.ChangeBase ? _playlist : null;

            EventManager.Instance?.Send<BgmEvent, BgmRequestData>(
                eventType,
                new BgmRequestData { bgmKey = _bgmKey, playlist = playlist, fadeTime = _fadeTime });
        }

        /// <summary>Override 모드에서 직전 BGM으로 복귀시킨다(보스전 종료 등). ChangeBase 모드에서는 무효.</summary>
        public void Deactivate()
        {
            if (_mode != BgmTriggerMode.Override) return;

            EventManager.Instance?.Send<BgmEvent, BgmRequestData>(
                BgmEvent.Restore,
                new BgmRequestData { fadeTime = _fadeTime });
        }

        private static bool IsPlayer(Collider other)
        {
            var actor = other.GetComponent<GameActor>();
            return actor != null && actor.HasActorType(ActorType.Player);
        }
    }
}
