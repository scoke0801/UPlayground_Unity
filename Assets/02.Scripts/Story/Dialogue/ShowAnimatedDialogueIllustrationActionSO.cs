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

        [SerializeField] private Vector2 _startOffset;
        [SerializeField] private Vector2 _endOffset;
        [SerializeField, Min(0.01f)] private float _startScale = 1f;
        [SerializeField, Min(0.01f)] private float _endScale = 1f;
        [SerializeField, Min(0f)] private float _motionDuration;
        [SerializeField, Tooltip("대사 타이핑을 기다리지 않고 노드 진입과 동시에 삽화를 표시합니다.")]
        private bool _revealImmediately;

        public override void Execute()
        {
            var presentation = new DialogueIllustrationPresentation(
                _startOffset,
                _endOffset,
                _startScale,
                _endScale,
                _motionDuration,
                _revealImmediately);
            DialogueManager.Instance?.RequestLineIllustration(
                _illustration,
                _tint,
                presentation);
        }
    }
}
