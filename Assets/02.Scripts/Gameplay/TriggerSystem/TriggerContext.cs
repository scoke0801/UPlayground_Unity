using UnityEngine;
using UPlayGround.Group;

namespace UPlayGround.TriggerSystem
{
    /// <summary>
    /// 트리거 발화 원인과 실행 중 공유 데이터를 담는 런타임 컨텍스트.
    /// </summary>
    public sealed class TriggerContext
    {
        public TriggerContext(TriggerComposer composer, TriggerSourceSO source)
        {
            Composer = composer;
            Source = source;
            FiredTime = Time.time;
        }

        public TriggerComposer Composer { get; }
        public TriggerSourceSO Source { get; }
        public float FiredTime { get; }

        public Collider Collider { get; private set; }
        public GameActor Actor { get; private set; }
        public MonsterGroupController Group { get; private set; }

        // 액션이 Execute 도중 "이번 발화를 소모할지"를 결정해 ConsumesTrigger에서 읽도록 실어 나르는 채널.
        // 공유 SO 에셋에 가변 인스턴스 상태를 두지 않기 위함(발화마다 새 컨텍스트라 Composer 간 오염 없음). 기본 true.
        public bool ActionConsumesTrigger { get; set; } = true;

        public TriggerContext WithCollider(Collider collider, GameActor actor)
        {
            Collider = collider;
            Actor = actor;
            return this;
        }

        public TriggerContext WithGroup(MonsterGroupController group)
        {
            Group = group;
            return this;
        }
    }
}
