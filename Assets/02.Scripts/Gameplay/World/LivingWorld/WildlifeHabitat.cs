using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>필드 전체 환경 생물을 한 번의 갱신으로 처리하고 활성 플레이어를 관측점으로 전달한다.</summary>
    public sealed class WildlifeHabitat : MonoBehaviour
    {
        private WildlifeCreature[] _creatures;

        private void Awake() => _creatures = GetComponentsInChildren<WildlifeCreature>(true);

        // GameActor가 아닌 장식 생물이므로 액터 등록 없이 필드 단위로 묶는다.
        private void Update()
        {
            var player = Services.TryGet<IActorQueryService>(out var actors) ? actors.PlayerTransform : null;
            bool hasObserver = player != null;
            Vector3 observer = hasObserver ? player.position : default;
            for (int i = 0; i < _creatures.Length; i++)
            {
                var creature = _creatures[i];
                if (creature != null && creature.isActiveAndEnabled)
                    creature.Tick(Time.deltaTime, observer, hasObserver);
            }
        }
    }
}
