using UnityEngine;

namespace UPlayGround.Manager.Cinematic
{
    /// <summary>Additive 씬 또는 무대 프리팹의 선택적 저작 바인딩.</summary>
    public sealed class CinematicStageRoot : MonoBehaviour
    {
        [SerializeField] private Transform _actorRoot;
        [SerializeField] private Transform _casterAnchor;

        public Transform ActorRoot => _actorRoot != null ? _actorRoot : transform;
        public Transform CasterAnchor => _casterAnchor != null ? _casterAnchor : transform;
    }
}
