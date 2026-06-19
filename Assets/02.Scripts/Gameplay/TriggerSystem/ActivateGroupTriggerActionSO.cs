using System.Collections;
using UnityEngine;
using UPlayGround.Group;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/트리거/액션/Activate Group")]
    public sealed class ActivateGroupTriggerActionSO : TriggerActionSO
    {
        [Tooltip("프리팹 에셋 참조 전용. 씬 객체는 SO 에셋에 저장되지 않으므로(리로드 시 null) Composer의 TriggerSceneReferences.Group 또는 발화 컨텍스트의 그룹으로 자동 해석된다.")]
        [SerializeField] private MonsterGroupController _targetGroup;

        public override bool CanExecute(TriggerContext context)
        {
            return ResolveGroup(context) != null;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            ResolveGroup(context)?.Activate();
            yield break;
        }

        private MonsterGroupController ResolveGroup(TriggerContext context)
        {
            if (_targetGroup != null)
                return _targetGroup;

            var references = context?.Composer != null ? context.Composer.GetComponent<TriggerSceneReferences>() : null;
            if (references != null && references.Group != null)
                return references.Group;

            return context?.Group;
        }
    }
}
