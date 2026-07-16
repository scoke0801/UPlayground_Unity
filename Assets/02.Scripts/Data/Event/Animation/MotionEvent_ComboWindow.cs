using System;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 충돌 판정 활성화 이벤트
    /// </summary>
    [Serializable]
    [MotionEventMeta("ComboWindow", Category = "Combat", CategoryOrder = 0,
        Description = "다음 콤보 입력을 받을 수 있는 구간을 엽니다.",
        Aliases = new[] { "combo", "cancel", "chain", "연계", "콤보" },
        Icon = "🔓", Color = new[] { 0.98f, 0.55f, 0.15f })]
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