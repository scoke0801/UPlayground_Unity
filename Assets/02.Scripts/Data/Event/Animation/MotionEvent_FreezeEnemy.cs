using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Component;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 액터 Freeze 이벤트
    /// </summary>
    [Serializable]
    public class FreezeEnemyEvent : MotionEventBase
    {
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
