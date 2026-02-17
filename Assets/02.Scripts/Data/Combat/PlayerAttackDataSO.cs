using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Combat
{
    /// <summary>
    /// 플레이어 공격 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerAttackData", menuName = "UPlayGround/Player/Attack Data")]
    public class PlayerAttackDataSO : ScriptableObject
    {
        [Header("Attack Pool")]
        [Tooltip("약 공격 리스트")]
        public List<PlayerAttackInfo> liteComboAttackList  = new List<PlayerAttackInfo>();
        
        [Tooltip("강 공격 리스트")]
        public List<PlayerAttackInfo> heavyComboAttackList  = new List<PlayerAttackInfo>();
        
        [Tooltip("점프 공격 리스트")]
        public List<PlayerAttackInfo> jumpAttackList  = new List<PlayerAttackInfo>();
        
        [Tooltip("대쉬 공격 리스트")]
        public List<PlayerAttackInfo> dashAttackList  = new List<PlayerAttackInfo>();
        
        [Tooltip("스킬 공격 리스트")]
        public List<PlayerAttackInfo> skillAttackList  = new List<PlayerAttackInfo>();
    }
}