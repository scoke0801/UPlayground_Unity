using System;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 투사체 발사 이벤트
    /// </summary>
    [Serializable]
    public class HealSkillEvent : MotionEventBase
    {
        public string vfxPrefabKey;
        
        public override string GetDisplayName() => "HealSKill";

        public override string GetShortLabel()
        {
            return "HealSKill:";
        }

        public override void Execute(GameObject target)
        {
            // [TODO]actor로 부터 대상 정보를 가져와서 힐 처리
            // 대상 위치에 힐 이펙트도 붙여주고..
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

}