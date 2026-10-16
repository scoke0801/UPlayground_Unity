using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.State;

namespace UPlayGround
{
    public partial class MonsterActor : IDialogueFaceActor
    {
        [SerializeField] private DialogueFaceBinding _dialogueFace = new();
        private GameActor _dialogueFaceTargetActor;
        private bool _suppressFaceLook;
        private bool _suppressFaceBlink;

        /// <summary>애니메이션 평가 전에 직전 프레임의 얼굴 보정만 되돌린다.</summary>
        public void RestoreDialogueFace() => _dialogueFace?.Restore();

        /// <summary>명시 연기를 우선하도록 자동 얼굴 보정의 사용 여부를 받는다.</summary>
        public void SetDialogueFaceIntent(bool suppressLook, bool suppressBlink)
        { _suppressFaceLook = suppressLook; _suppressFaceBlink = suppressBlink; }

        /// <summary>화린 구조 대역도 같은 모델 바인딩으로 제어하되 전투·피격에는 개입하지 않는다.</summary>
        public void ApplyDialogueFace(bool paused)
        {
            if (!isActiveAndEnabled || !IsDialogueStaged || IsEngagedInCombat
                || MovementController?.CurrentState?.StateId != ActorStateId.Idle)
            { _dialogueFace?.Clear(); return; }
            Transform target = null;
            if (_dialogueFaceTargetActor != null && _dialogueFaceTargetActor.isActiveAndEnabled)
                _dialogueFaceTargetActor.TryGetSocket(ActorSocketType.Head, out target);
            _dialogueFace?.Apply(target, paused, _suppressFaceLook, _suppressFaceBlink, Time.unscaledDeltaTime);
        }

        private void ClearDialogueFace()
        {
            _dialogueFace?.Clear();
            _dialogueFaceTargetActor = null;
            _suppressFaceLook = false;
            _suppressFaceBlink = false;
        }

        private void OnDisable() => ClearDialogueFace();
    }
}
