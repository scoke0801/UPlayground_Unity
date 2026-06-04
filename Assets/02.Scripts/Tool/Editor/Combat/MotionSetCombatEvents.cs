#if UNITY_EDITOR
using System.Collections.Generic;
using UPlayGround.Animation;
using UPlayGround.Data.Event;

namespace UPlayGround.Tool.Editor.Combat
{
    /// <summary>
    /// MotionSet 타임라인에서 전투 관련 이벤트(Collision, Telegraph)를 추출하는 에디터 헬퍼.
    /// 검증 규칙 판정(<see cref="CombatDataValidator"/>)과 분리해 에셋 검색/이벤트 추출 책임만 담당한다.
    /// </summary>
    public sealed class MotionSetCombatEvents
    {
        public readonly List<BeginCollisionEvent> Collisions = new();
        public readonly List<TelegraphEvent> Telegraphs = new();

        public bool HasCollision => Collisions.Count > 0;
        public bool HasTelegraph => Telegraphs.Count > 0;

        /// <summary>위치를 시작 시점에 고정하는 TelegraphEvent(AOE 장판 등)가 하나라도 있는가.</summary>
        public bool HasPositionLockedTelegraph
        {
            get
            {
                foreach (TelegraphEvent telegraph in Telegraphs)
                    if (telegraph != null && telegraph.lockPositionOnStart)
                        return true;
                return false;
            }
        }

        /// <summary>Collision 이벤트들이 참조하는 고유 hitPhaseIndex 집합.</summary>
        public HashSet<int> CollisionPhaseIndices()
        {
            var set = new HashSet<int>();
            foreach (BeginCollisionEvent collision in Collisions)
                if (collision != null)
                    set.Add(collision.hitPhaseIndex);
            return set;
        }

        /// <summary>globalEvents와 각 Motion의 events를 모두 훑어 전투 이벤트를 수집한다.</summary>
        public static MotionSetCombatEvents Collect(MotionSet motionSet)
        {
            var result = new MotionSetCombatEvents();
            if (motionSet == null)
                return result;

            if (motionSet.globalEvents != null)
            {
                foreach (MotionEventBase evt in motionSet.globalEvents)
                    result.Add(evt);
            }

            if (motionSet.motions != null)
            {
                foreach (Motion motion in motionSet.motions)
                {
                    if (motion?.events == null)
                        continue;
                    foreach (MotionEventBase evt in motion.events)
                        result.Add(evt);
                }
            }

            return result;
        }

        private void Add(MotionEventBase evt)
        {
            switch (evt)
            {
                case BeginCollisionEvent collision:
                    Collisions.Add(collision);
                    break;
                case TelegraphEvent telegraph:
                    Telegraphs.Add(telegraph);
                    break;
            }
        }
    }
}
#endif
