using UnityEngine;

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

        public override void Execute()
        {
            DialogueManager.Instance?.RequestLineIllustration(_illustration, _tint);
        }
    }
}
