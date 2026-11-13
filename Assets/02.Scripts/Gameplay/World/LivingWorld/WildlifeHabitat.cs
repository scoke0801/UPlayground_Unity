using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>CPU 야생 생물의 개별 행동을 일괄 갱신한다. GPU 장식 군집은 별도 서식지가 소유한다.</summary>
    [DisallowMultipleComponent]
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
            var candidates = GetComponentsInChildren<WildlifeCreature>(true);
            var owned = new System.Collections.Generic.List<WildlifeCreature>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                // 중첩 서식지는 자신의 이웃 배열과 Tick을 독점해야 한다.
                if (candidates[i].GetComponentInParent<WildlifeHabitat>(true) == this)
                    owned.Add(candidates[i]);
            }
            _creatures = owned.ToArray();
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
