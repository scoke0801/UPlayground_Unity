using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Group;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Source/Group Defeated")]
    public sealed class GroupDefeatedTriggerSourceSO : TriggerSourceSO
    {
        [Tooltip("프리팹 에셋 참조 전용. 씬 객체는 SO 에셋에 저장되지 않으므로(리로드 시 null) Composer의 TriggerSceneReferences.Group 또는 같은 오브젝트의 MonsterGroupController로 자동 해석된다.")]
        [SerializeField] private MonsterGroupController _targetGroup;

        // 해제 시 실제 구독했던 그룹에서 제거해야 누수가 없으므로, 구독한 그룹과 핸들러를 함께 보관한다.
        private readonly Dictionary<TriggerComposer, Subscription> _subscriptions = new();

        private readonly struct Subscription
        {
            public Subscription(MonsterGroupController group, Action handler)
            {
                Group = group;
                Handler = handler;
            }

            public MonsterGroupController Group { get; }
            public Action Handler { get; }
        }

        public override void Subscribe(TriggerComposer composer, Action<TriggerContext> onFire)
        {
            var group = ResolveGroup(composer);
            if (group == null || composer == null)
                return;

            // 중복 구독 방지: 이전 구독은 반드시 그때 구독했던 그룹에서 해제한다.
            RemoveSubscription(composer);

            Action handler = () =>
            {
                onFire?.Invoke(new TriggerContext(composer, this).WithGroup(group));
            };

            _subscriptions[composer] = new Subscription(group, handler);
            group.OnGroupDefeated += handler;
        }

        public override void Unsubscribe(TriggerComposer composer, Action<TriggerContext> onFire)
        {
            if (composer == null)
                return;

            RemoveSubscription(composer);
        }

        private void RemoveSubscription(TriggerComposer composer)
        {
            if (!_subscriptions.TryGetValue(composer, out var subscription))
                return;

            if (subscription.Group != null)
                subscription.Group.OnGroupDefeated -= subscription.Handler;

            _subscriptions.Remove(composer);
        }

        private MonsterGroupController ResolveGroup(TriggerComposer composer)
        {
            if (_targetGroup != null)
                return _targetGroup;

            var references = composer != null ? composer.GetComponent<TriggerSceneReferences>() : null;
            if (references != null && references.Group != null)
                return references.Group;

            return composer != null ? composer.GetComponent<MonsterGroupController>() : null;
        }
    }
}
