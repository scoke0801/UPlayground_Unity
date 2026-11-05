using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>필드 전체 환경 생물을 한 번의 갱신으로 처리하고 활성 플레이어를 관측점으로 전달한다.</summary>
    public sealed class WildlifeHabitat : MonoBehaviour
    {
        private WildlifeCreature[] _creatures;
        [Tooltip("지면 NavMesh가 수면 아래까지 이어지는 경우, 실제 수면 렌더러를 지정합니다.")]
        [SerializeField] private Renderer[] _waterSurfaces = System.Array.Empty<Renderer>();

        /// <summary>수면 아래 지면을 목적지나 경유지로 선택하지 않게 한다.</summary>
        public bool CanOccupy(Vector3 position)
        {
            for (int i = 0; i < _waterSurfaces.Length; i++)
            {
                var surface = _waterSurfaces[i];
                if (surface == null) continue;
                var bounds = surface.bounds;
                if (position.x >= bounds.min.x && position.x <= bounds.max.x
                    && position.z >= bounds.min.z && position.z <= bounds.max.z
                    && position.y <= bounds.max.y) return false;
            }
            return true;
        }

        private void Awake()
        {
            _creatures = GetComponentsInChildren<WildlifeCreature>(true);
            for (int i = 0; i < _creatures.Length; i++) _creatures[i].BindHabitat(this, _creatures);
        }

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
