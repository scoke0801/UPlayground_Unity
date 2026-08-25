using UnityEngine;
using UPlayGround.UI;

namespace UPlayGround.Dialogue
{
    /// <summary>현재 Main 대사 한 줄에 이동·확대 연출이 포함된 삽화를 표시한다.</summary>
    [CreateAssetMenu(
        menuName = "UPlayGround/대화/액션/Show Animated Illustration",
        fileName = "Action_ShowAnimatedIllustration_")]
    public sealed class ShowAnimatedDialogueIllustrationActionSO : DialogueActionSO
    {
        [SerializeField, Tooltip("현재 대사와 함께 표시할 삽화 Sprite.")]
        private Sprite _illustration;

        [SerializeField, Tooltip("삽화에 곱할 색상. 흰색이면 원본 색상을 유지합니다.")]
        private Color _tint = Color.white;

        // 기존 에셋은 이 필드가 직렬화되어 있지 않아 초기값 Custom을 유지한다. 손으로 맞춘 수치가 그대로 재생된다.
        [SerializeField, Tooltip("삽화 연출 프리셋. Custom을 고르면 아래 수치를 그대로 사용합니다.")]
        private DialogueIllustrationMotion _motion = DialogueIllustrationMotion.Custom;

        [Header("Custom 연출 수치")]
        [SerializeField] private Vector2 _startOffset;
        [SerializeField] private Vector2 _endOffset;
        [SerializeField, Min(0.01f)] private float _startScale = 1f;
        [SerializeField, Min(0.01f)] private float _endScale = 1f;
        [SerializeField, Min(0f), Tooltip("0이면 기본 프리셋 연출로 대체합니다.")]
        private float _motionDuration;
        [SerializeField, Tooltip("Custom 연출의 가속 곡선.")]
        private DialogueIllustrationEase _motionEase = DialogueIllustrationEase.Linear;

        [SerializeField, Tooltip("대사 타이핑을 기다리지 않고 노드 진입과 동시에 삽화를 표시합니다.")]
        private bool _revealImmediately;

        public override void Execute()
        {
            DialogueIllustrationPresentation presentation = DialogueIllustrationMotionLibrary
                .Resolve(
                    _motion,
                    _startOffset,
                    _endOffset,
                    _startScale,
                    _endScale,
                    _motionDuration,
                    _motionEase)
                .ToPresentation(_revealImmediately);
            DialogueManager.Instance?.RequestLineIllustration(
                _illustration,
                _tint,
                presentation);
        }
    }
}
