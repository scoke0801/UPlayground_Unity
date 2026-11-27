using UnityEngine;

namespace UPlayGround
{
    /// <summary>스킨 얼굴의 머리 방향과 피격색을 같은 렌더러 속성 블록에 유지한다.</summary>
    [ExecuteAlways]
    public sealed class CharacterToonHeadFrame : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Renderer face;
        [SerializeField] private Vector3 localForward = Vector3.forward;
        [SerializeField] private Vector3 localRight = Vector3.right;

        private static readonly int ForwardId = Shader.PropertyToID("_HeadForward");
        private static readonly int RightId = Shader.PropertyToID("_HeadRight");
        private MaterialPropertyBlock properties;

        private void LateUpdate()
        {
            if (!head || !face) return;
            properties ??= new MaterialPropertyBlock();
            // 슬롯별 블록을 설정하면 얼굴 슬롯에서 렌더러 전체의 피격색이 가려진다.
            face.GetPropertyBlock(properties);
            properties.SetVector(ForwardId, head.TransformDirection(localForward));
            properties.SetVector(RightId, head.TransformDirection(localRight));
            face.SetPropertyBlock(properties);
        }
    }
}
