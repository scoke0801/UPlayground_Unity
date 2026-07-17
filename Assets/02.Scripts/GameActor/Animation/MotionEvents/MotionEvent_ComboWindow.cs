using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 충돌 판정 활성화 이벤트
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
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
            if (actor != null && actor.HasActorType(ActorType.Player))
            {
                HandlePlayerCombat(actor as PlayerActor, true);
            }
        }

        public override void OnCompleteEvent(GameObject target)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if (actor != null && actor.HasActorType(ActorType.Player))
            {
                HandlePlayerCombat(actor as PlayerActor, false);
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
    }
}
