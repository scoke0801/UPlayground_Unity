using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Cycle
{
    // 파일명과 클래스명이 일치해야 씬 저장 시 정식 MonoScript로 직렬화된다 (CycleBossRuntimeHandle.cs에서 분리).
    public sealed class CycleBossEncounterTrigger : MonoBehaviour
    {
        private CycleBossRuntimeHandle _owner;
        public void Initialize(CycleBossRuntimeHandle owner) => _owner = owner;

        private void OnTriggerEnter(Collider other)
        {
            GameActor actor = other.GetComponentInParent<GameActor>();
            if (actor != null && actor.HasActorType(ActorType.Player))
                _owner?.Discover();
        }
    }
}
