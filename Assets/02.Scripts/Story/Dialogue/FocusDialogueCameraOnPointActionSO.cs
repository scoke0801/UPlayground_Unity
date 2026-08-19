using UnityEngine;

namespace UPlayGround.Dialogue
{
    /// <summary>현재 Main 대화 라인의 카메라가 씬의 지정 지점을 바라보게 요청한다.</summary>
    [CreateAssetMenu(
        menuName = "UPlayGround/대화/액션/Focus Camera On Point",
        fileName = "Action_FocusCameraOnPoint_")]
    public sealed class FocusDialogueCameraOnPointActionSO : DialogueActionSO
    {
        [SerializeField, Tooltip("씬의 CameraLookAtPoint에 설정한 고유 ID.")]
        private string _pointId;

        [SerializeField, Tooltip("주시 지점에 더할 월드 공간 오프셋.")]
        private Vector3 _worldOffset;

        public override void Execute()
        {
            DialogueManager.Instance?.RequestCameraLookAtPoint(_pointId, _worldOffset);
        }
    }
}
