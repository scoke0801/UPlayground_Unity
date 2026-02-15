using System;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 충돌 판정 활성화 이벤트
    /// </summary>
    [Serializable]
    public class ComboWindowEvent : MotionEventBase
    {

        public override string GetDisplayName() => "ComboWindow";

        public override string GetShortLabel()
        {
            return "ComboWindow";
        }

        public override void Execute(GameObject target)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if (actor != null)
            {
                switch (actor.ActorType)
                {
                    case ActorType.Player:
                        HandlePlayerCombat(actor as PlayerActor, true);
                        break;
                    case ActorType.Monster:
                        HandleMonsterCombat(actor as MonsterActor, true);
                        break;
                }
            }
        }

        public override void OnCompleteEvent(GameObject target)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if (actor != null)
            {
                switch (actor.ActorType)
                {
                    case ActorType.Player:
                        HandlePlayerCombat(actor as PlayerActor, false);
                        break;
                    case ActorType.Monster:
                        HandleMonsterCombat(actor as MonsterActor, false);
                        break;
                }
            }
        }

        private void HandlePlayerCombat(PlayerActor playerActor, bool isOpenComboWindow)
        {
            if (playerActor == null)
            {
                return;
            }

            PlayerCombat playerCombat = playerActor.GetCombat();
            if (playerCombat == null)
            {
                return;
            }

            if (isOpenComboWindow)
            {
                playerCombat.OpenComboWindow();
            }
            else
            {
                playerCombat.CloseComboWindow();
            }
        }

        private void HandleMonsterCombat(MonsterActor monsterActor, bool isOpenComboWindow)
        {
            if (monsterActor == null)
            {
                return;
            }

            EnemyCombat enemyCombat = monsterActor.Combat;
            if (enemyCombat == null)
            {
                return;
            }
            if (isOpenComboWindow)
            {
                enemyCombat.OpenComboWindow();
            }
            else
            {
                enemyCombat.CloseComboWindow();
            }
        }
    }
}