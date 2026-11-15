using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>시전자 정의의 얼굴 패널을 실시간으로 재생하고 모션 중단 시 정리한다.</summary>
    [Serializable]
    [MotionEventDescriptor("UltimateFaceStrip", "Camera", 0,
        "궁극기 얼굴 패널을 표시합니다.", "ultimate", "face", "궁극기", "얼굴")]
    public sealed class UltimateFaceStripMotionEvent : MotionEventBase
    {
        public Color accentColor = Color.white;
        [Min(0f)] public float inDuration = 0.18f;
        [Min(0f)] public float hold = 0.5f;
        [Min(0f)] public float outDuration = 0.2f;
        [Min(0f)] public float faceDelay = 0.04f;
        [Range(0f, 0.02f)] public float breathScale = 0.015f;
        [Min(0f)] public float flashDuration = 0.1f;

        private struct ActivePanel
        {
            public IFaceStripPresenter Presenter;
            public long Token;
        }

        [NonSerialized] private readonly Dictionary<int, ActivePanel> _activePanels = new(2);

        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => MotionEventEnemyExecutionPolicy.Ignored;
        public override string GetDisplayName() => "궁극기 얼굴 패널";

        public override void Execute(GameObject target)
        {
            PlayerActor player = SignatureMotionEventTarget.ResolvePlayer(target);
            if (player == null) return;
            Release(target.GetInstanceID());
            Sprite sprite = Svc.Party?.GetCharacterDefinition(player.CharacterType)?.ultimateFaceSprite;
            if (sprite == null || !Services.TryGet(out IFaceStripPresenter presenter)) return;
            long token = presenter.Show(new FaceStripRequest(sprite, accentColor, inDuration, hold,
                outDuration, faceDelay, breathScale, flashDuration));
            _activePanels[target.GetInstanceID()] = new ActivePanel { Presenter = presenter, Token = token };
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (target != null) Release(target.GetInstanceID());
        }

        private void Release(int targetKey)
        {
            if (!_activePanels.TryGetValue(targetKey, out ActivePanel panel)) return;
            _activePanels.Remove(targetKey);
            panel.Presenter?.Hide(panel.Token);
        }
    }
}
