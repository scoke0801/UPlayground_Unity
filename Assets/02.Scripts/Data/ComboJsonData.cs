using System;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 콤보 공격 데이터
    /// Json에서 로드하여 순차적인 콤보 공격 애니메이션을 관리합니다.
    /// </summary>
    [Serializable]
    public class ComboJsonData : ILoader<int, ComboJsonData>
    {
        public int comboID;
        public string comboName;
        public string[] animationKeys;  // 콤보 애니메이션 키 배열
        public float[] hitStarts;       // 각 공격의 판정 시작 시점
        public float[] hitEnds;         // 각 공격의 판정 종료 시점
        public float comboResetTime;    // 콤보 리셋 시간
        public bool useHitBox;          // 히트박스 사용 여부
        
        public int GetKey() => comboID;
        
        // 프로퍼티
        public int ComboID => comboID;
        public string ComboName => comboName;
        public string[] AnimationKeys => animationKeys;
        public float[] HitStarts => hitStarts;
        public float[] HitEnds => hitEnds;
        public float ComboResetTime => comboResetTime;
        public bool UseHitBox => useHitBox;
        
        // 유효성 검사
        public bool IsValid()
        {
            if (animationKeys == null || animationKeys.Length == 0)
                return false;
                
            // hitStarts와 hitEnds가 animationKeys와 같은 길이여야 함
            if (hitStarts != null && hitStarts.Length != animationKeys.Length)
            {
                Debug.LogWarning($"[ComboJsonData] ComboID {comboID}: hitStarts 길이가 animationKeys와 다릅니다.");
            }
            
            if (hitEnds != null && hitEnds.Length != animationKeys.Length)
            {
                Debug.LogWarning($"[ComboJsonData] ComboID {comboID}: hitEnds 길이가 animationKeys와 다릅니다.");
            }
            
            return true;
        }
        
        // 특정 인덱스의 히트 타이밍 가져오기
        public void GetHitTiming(int index, out float hitStart, out float hitEnd)
        {
            hitStart = (hitStarts != null && index < hitStarts.Length) ? hitStarts[index] : 0.3f;
            hitEnd = (hitEnds != null && index < hitEnds.Length) ? hitEnds[index] : 0.6f;
        }
    }
    
    /// <summary>
    /// Json 파싱용 래퍼
    /// </summary>
    [Serializable]
    public class ComboDataWrapper
    {
        public ComboJsonData[] combos;
    }
}
