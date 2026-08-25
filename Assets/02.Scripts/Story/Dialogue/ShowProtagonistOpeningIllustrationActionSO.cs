using System;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.UI;

namespace UPlayGround.Dialogue
{
    /// <summary>새 게임에서 선택한 서사 주인공에 대응하는 오프닝 삽화를 표시한다.</summary>
    [CreateAssetMenu(
        menuName = "UPlayGround/대화/액션/Show Protagonist Opening Illustration",
        fileName = "Action_ShowProtagonistOpeningIllustration")]
    public sealed class ShowProtagonistOpeningIllustrationActionSO : DialogueActionSO
    {
        [Serializable]
        private struct CharacterIllustration
        {
            public CharacterActorType characterType;
            public Sprite illustration;
            public Vector2 startOffset;
            public Vector2 endOffset;
            [Min(0.01f)] public float startScale;
            [Min(0.01f)] public float endScale;
            [Min(0f)] public float enterDuration;
        }

        [SerializeField, Tooltip("주인공 전경 뒤에 표시할 오프닝 배경 Sprite.")]
        private Sprite _backgroundIllustration;
        [SerializeField, Tooltip("선택한 주인공 전경을 함께 표시합니다. 끄면 배경만 표시합니다.")]
        private bool _showProtagonist = true;
        [SerializeField] private CharacterIllustration[] _illustrations = Array.Empty<CharacterIllustration>();
        [SerializeField] private Sprite _fallbackIllustration;
        [SerializeField] private Color _tint = Color.white;
        [SerializeField] private Vector2 _startOffset;
        [SerializeField] private Vector2 _endOffset;
        [SerializeField, Min(0.01f)] private float _startScale = 1f;
        [SerializeField, Min(0.01f)] private float _endScale = 1f;
        [SerializeField, Min(0f), Tooltip("0이면 기본 프리셋 연출로 대체합니다.")]
        private float _motionDuration;
        [SerializeField, Tooltip("배경 삽화 연출 프리셋. Custom을 고르면 위 수치를 그대로 사용합니다.")]
        private DialogueIllustrationMotion _motion = DialogueIllustrationMotion.Custom;
        [SerializeField, Tooltip("Custom 배경 연출의 가속 곡선.")]
        private DialogueIllustrationEase _motionEase = DialogueIllustrationEase.Linear;
        [SerializeField, Tooltip("대사 타이핑을 기다리지 않고 오프닝 삽화를 즉시 표시합니다.")]
        private bool _revealImmediately = true;

        public override void Execute()
        {
            if (_backgroundIllustration == null)
            {
                Debug.LogWarning("[Dialogue] 오프닝 배경 삽화가 비어 있습니다.", this);
                return;
            }

            CharacterIllustration characterIllustration = default;
            Sprite foregroundIllustration = null;
            if (_showProtagonist)
            {
                CharacterActorType protagonist = Svc.Party?.StoryProtagonistType
                                                   ?? CharacterActorType.None;
                if (TryResolveIllustration(protagonist, out characterIllustration))
                {
                    foregroundIllustration = characterIllustration.illustration;
                }
                else
                {
                    foregroundIllustration = _fallbackIllustration;
                    characterIllustration.startScale = 1f;
                    characterIllustration.endScale = 1f;
                    characterIllustration.enterDuration = 0.3f;
                    Debug.LogWarning($"[Dialogue] 주인공 오프닝 삽화를 찾지 못했습니다: {protagonist}", this);
                }
            }

            var foregroundValues = new DialogueIllustrationMotionValues(
                characterIllustration.startOffset,
                characterIllustration.endOffset,
                characterIllustration.startScale,
                characterIllustration.endScale,
                characterIllustration.enterDuration,
                DialogueIllustrationEase.EaseOut);
            DialogueIllustrationPresentation presentation = DialogueIllustrationMotionLibrary
                .Resolve(
                    _motion,
                    _startOffset,
                    _endOffset,
                    _startScale,
                    _endScale,
                    _motionDuration,
                    _motionEase)
                .ToPresentation(
                    foregroundValues,
                    _revealImmediately,
                    DialogueIllustrationPlacement.BehindDialogue,
                    DialogueIllustrationPresentationMode.CinematicNarration,
                    persistAcrossFollowingLines: true);
            DialogueManager.Instance?.RequestLineIllustration(
                _backgroundIllustration,
                foregroundIllustration,
                _tint,
                presentation);
        }

        private bool TryResolveIllustration(
            CharacterActorType protagonist,
            out CharacterIllustration illustration)
        {
            for (int i = 0; i < _illustrations.Length; i++)
            {
                CharacterIllustration entry = _illustrations[i];
                if (entry.characterType == protagonist && entry.illustration != null)
                {
                    illustration = entry;
                    return true;
                }
            }

            illustration = default;
            return false;
        }
    }
}
