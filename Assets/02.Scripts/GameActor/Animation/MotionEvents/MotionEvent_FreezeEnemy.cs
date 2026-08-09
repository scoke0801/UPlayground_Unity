using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Components;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 액터 Freeze 이벤트
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("FreezeEnemy", "Utility", 0, "적을 일시 정지시킵니다.", "freeze", "enemy", "stop", "빙결", "정지")]
    public class FreezeEnemyEvent : MotionEventBase
    {
        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy =>
            MotionEventEnemyExecutionPolicy.Ignored;

        public override string GetDisplayName() => "FreezeEnemy";

        [NonSerialized] private List<IEnemyAIController> _frozenEnemyControllers;
        
        public override string GetShortLabel()
        {
            return "FreezeEnemy";
        }

        public override void Execute(GameObject target)
        {
            PlayerActor player = target.GetComponent<PlayerActor>();
            if(player == null)
            {
                return;
            }
            
            PlayerCombat combat = player.GetCombat();
            if(combat == null)
            {
                return;
            }
            
            // 주변 모든 적 Freeze
            _frozenEnemyControllers ??= new List<IEnemyAIController>();
            combat.FillEnemyAIControllersInRadius(30.0f, _frozenEnemyControllers);
            foreach (var brain in _frozenEnemyControllers)
            {
                if (IsValidAIController(brain))
                    brain.Freeze();
            }
        }

        public override void OnCompleteEvent(GameObject target)
        {
            PlayerActor player = target.GetComponent<PlayerActor>();
            if (player == null)
            {
                return;
            }

            PlayerCombat combat = player.GetCombat();
            if (combat == null)
            {
                return;
            }

            if (_frozenEnemyControllers == null)
            {
                return;
            }

            foreach (var brain in _frozenEnemyControllers)
            {
                if (!IsValidAIController(brain))
                {
                    continue;
                }
                brain.Unfreeze();
            }
        }

        private static bool IsValidAIController(IEnemyAIController controller)
        {
            return controller is UnityEngine.Object unityObject && unityObject != null;
        }
    }

}
