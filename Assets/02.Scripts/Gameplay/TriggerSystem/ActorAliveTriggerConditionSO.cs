using UnityEngine;
using UPlayGround;
using UPlayGround.Group;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/트리거/조건/Actor Alive")]
    public sealed class ActorAliveTriggerConditionSO : TriggerConditionSO
    {
        [Tooltip("프리팹 에셋 참조 전용. 씬 객체는 SO 에셋에 저장되지 않으므로 Composer의 TriggerSceneReferences.Actor를 사용한다.")]
        [SerializeField] private MonsterActor _targetActor;
        [Tooltip("프리팹 에셋 참조 전용. 씬 객체는 TriggerSceneReferences.Group을 사용한다.")]
        [SerializeField] private MonsterGroupController _targetGroup;
        [SerializeField] private bool _expectedAlive = true;

        public override bool Evaluate(TriggerContext context)
        {
            // 명시적으로 지정된 직렬화 필드 우선 (프리팹 에셋 참조 전용)
            if (_targetGroup != null)
                return GroupMatches(_targetGroup);
            if (_targetActor != null)
                return ActorMatches(_targetActor);

            // 씬 객체 폴백: SO 에셋은 씬 참조를 직렬화하지 못하므로 TriggerSceneReferences에서 해석
            var references = context?.Composer != null
                ? context.Composer.GetComponent<TriggerSceneReferences>()
                : null;
            if (references != null)
            {
                if (references.Group != null)
                    return GroupMatches(references.Group);
                if (references.Actor != null)
                    return ActorMatches(references.Actor);
            }

            // 발화 컨텍스트의 그룹 (예: GroupDefeated 소스가 실어준 그룹)
            if (context?.Group != null)
                return GroupMatches(context.Group);

            return false;
        }

        private bool GroupMatches(MonsterGroupController group) => (group.AliveCount > 0) == _expectedAlive;
        private bool ActorMatches(MonsterActor actor) => actor.IsAlive() == _expectedAlive;
    }
}
