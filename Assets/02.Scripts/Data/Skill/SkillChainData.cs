using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 스킬 연계 정보
    /// 현재 스킬에서 특정 키 입력 시 다음에 실행될 스킬을 정의합니다.
    /// </summary>
    [Serializable]
    public class SkillChainData : ILoader<int, SkillChainData>
    {
        public int currentSkillID;           // 현재 스킬 ID
        public SkillChainBranch[] branches;  // 분기 가능한 다음 스킬들
        public float inputWindowStart;       // 입력 받기 시작 시점 (애니메이션 진행률 0~1)
        public float inputWindowEnd;         // 입력 받기 종료 시점 (애니메이션 진행률 0~1)
        
        public int GetKey() => currentSkillID;
        
        // 프로퍼티
        public int CurrentSkillID => currentSkillID;
        public SkillChainBranch[] Branches => branches;
        public float InputWindowStart => inputWindowStart;
        public float InputWindowEnd => inputWindowEnd;
        
        /// <summary>
        /// 특정 입력 키에 해당하는 다음 스킬 ID 찾기
        /// </summary>
        public bool TryGetNextSkill(string inputKey, out int nextSkillID)
        {
            nextSkillID = 0;
            
            if (branches == null || branches.Length == 0)
                return false;
            
            foreach (var branch in branches)
            {
                if (branch.inputKey == inputKey)
                {
                    nextSkillID = branch.nextSkillID;
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 연계 가능 여부 확인
        /// </summary>
        public bool IsChainable()
        {
            return branches != null && branches.Length > 0;
        }
    }
    
    /// <summary>
    /// 스킬 체인 분기 정보
    /// </summary>
    [Serializable]
    public class SkillChainBranch
    {
        public string inputKey;      // 입력 키 ("X", "Y", "A", "B" 등)
        public int nextSkillID;      // 다음 스킬 ID
        public string description;   // 설명 (디버그/UI용)
    }
    
    /// <summary>
    /// Json 파싱용 래퍼
    /// </summary>
    [Serializable]
    public class SkillChainDataWrapper
    {
        public SkillChainData[] chains;
    }
}
