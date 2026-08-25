using UnityEngine;
using UPlayGround.UI;

namespace UPlayGround.Dialogue
{
    /// <summary>현재 Main 대사 한 줄에 지정한 삽화를 표시한다.</summary>
    [CreateAssetMenu(
        menuName = "UPlayGround/대화/액션/Show Illustration",
        fileName = "Action_ShowIllustration_")]
    public sealed class ShowDialogueIllustrationActionSO : DialogueActionSO
    {
        [SerializeField, Tooltip("현재 대사와 함께 표시할 삽화 Sprite.")]
        private Sprite _illustration;

        [SerializeField, Tooltip("삽화에 곱할 색상. 흰색이면 원본 색상을 유지합니다.")]
        private Color _tint = Color.white;

        [SerializeField, Tooltip("삽화가 떠 있는 동안 재생할 연출 프리셋. Custom은 Show Animated Illustration에서 사용합니다.")]
        private DialogueIllustrationMotion _motion = DialogueIllustrationMotion.Auto;

        [SerializeField, Tooltip("대사 타이핑을 기다리지 않고 노드 진입과 동시에 삽화를 표시합니다.")]
        private bool _revealImmediately;

        public override void Execute()
        {
            DialogueIllustrationPresentation presentation = DialogueIllustrationMotionLibrary
                .Resolve(_motion)
                .ToPresentation(_revealImmediately);
            DialogueManager.Instance?.RequestLineIllustration(
                _illustration,
                _tint,
                presentation);
        }
    }
}
