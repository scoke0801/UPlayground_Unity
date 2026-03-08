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

        private List<EnemyBrain> _frozenBrains = new List<EnemyBrain>();
        
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
            _frozenBrains = combat.GetEnemyBrainsInRadius(30.0f);
            foreach (var brain in _frozenBrains)
                brain.Freeze();
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

            foreach (var brain in _frozenBrains)
            {
                if (brain == null)
                {
                    continue;
                }
                brain.Unfreeze();
            }
        }
    }

}