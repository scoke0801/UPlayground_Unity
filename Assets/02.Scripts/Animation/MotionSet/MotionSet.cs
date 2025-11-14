using UnityEngine;
using System.Collections.Generic;
using Animation;

/// <summary>
/// 제네릭한 애니메이션 모션 세트
/// 블렌딩, 방향성, 순차 재생 등 다양한 방식 지원
/// </summary>
[CreateAssetMenu(fileName = "New MotionSet", menuName = "Animation/Motion Set")]
public class MotionSet : ScriptableObject
{
    [Header("기본 정보")]
    public string motionSetName;
    
    [Header("재생 방식")]
    public MotionPlayMode playMode = MotionPlayMode.Sequential;
    public MotionBlendType blendType = MotionBlendType.None;
    
    [Header("애니메이션 데이터")]
    public List<MotionData> motions = new List<MotionData>();
    
    [Header("슬롯 설정")]
    [Tooltip("애니메이션을 재생할 슬롯 (ScriptableObject 참조)")]
    public AnimationSlot targetSlotAsset;
    
    [Tooltip("슬롯 이름 (하위 호환용, targetSlotAsset이 없을 때 사용)")]
    public string targetSlot = "FullBody";
    
    [Header("블렌딩 설정")]
    [Tooltip("블렌딩 파라미터 범위 (Min, Max)")]
    public Vector2 blendParameterRange = new Vector2(0f, 10f);
    
    /// <summary>
    /// 레이어 인덱스 가져오기 (Slot Asset 우선)
    /// </summary>
    public int GetLayerIndex()
    {
        return targetSlotAsset != null ? targetSlotAsset.LayerIndex : 0;
    }
    
    /// <summary>
    /// 슬롯 이름 가져오기 (Slot Asset 우선)
    /// </summary>
    public string GetSlotName()
    {
        return targetSlotAsset != null ? targetSlotAsset.SlotName : targetSlot;
    }
    
    /// <summary>
    /// 파라미터 값에 따라 적절한 모션 반환 (블렌딩용)
    /// </summary>
    public MotionData GetMotionByParameter(float parameter)
    {
        if (motions.Count == 0) return null;
        
        // 파라미터를 0~1 범위로 정규화
        float normalized = Mathf.InverseLerp(blendParameterRange.x, blendParameterRange.y, parameter);
        
        for (int i = 0; i < motions.Count; i++)
        {
            float normalizedThreshold = Mathf.InverseLerp(
                blendParameterRange.x, 
                blendParameterRange.y, 
                motions[i].threshold
            );
            
            if (normalized <= normalizedThreshold)
                return motions[i];
        }
        
        return motions[motions.Count - 1];
    }
    
    /// <summary>
    /// 2D 파라미터로 모션 반환 (Cartesian 블렌딩용)
    /// </summary>
    public MotionData GetMotionByParameter2D(Vector2 parameter)
    {
        if (motions.Count == 0) return null;
        
        // 가장 가까운 모션 찾기
        MotionData closestMotion = motions[0];
        float closestDistance = float.MaxValue;
        
        foreach (var motion in motions)
        {
            Vector2 motionPos = new Vector2(motion.threshold, motion.thresholdY);
            float distance = Vector2.Distance(parameter, motionPos);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestMotion = motion;
            }
        }
        
        return closestMotion;
    }
    
    /// <summary>
    /// 방향에 따라 적절한 모션 반환 (방향성용)
    /// </summary>
    public MotionData GetMotionByDirection(Vector2 direction)
    {
        if (motions.Count == 0) return null;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 가장 가까운 방향 찾기
        MotionData closestMotion = motions[0];
        float closestAngleDiff = float.MaxValue;
        
        foreach (var motion in motions)
        {
            if (motion.directionAngle < 0) continue; // 방향 설정 안된 모션 제외
            
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle, motion.directionAngle));
            if (angleDiff < closestAngleDiff)
            {
                closestAngleDiff = angleDiff;
                closestMotion = motion;
            }
        }
        
        return closestMotion;
    }
    
    /// <summary>
    /// 인덱스로 모션 반환 (순차 재생용)
    /// </summary>
    public MotionData GetMotionByIndex(int index)
    {
        if (index < 0 || index >= motions.Count) return null;
        return motions[index];
    }
    
    /// <summary>
    /// 랜덤 모션 반환
    /// </summary>
    public MotionData GetRandomMotion()
    {
        if (motions.Count == 0) return null;
        int randomIndex = Random.Range(0, motions.Count);
        return motions[randomIndex];
    }
}

/// <summary>
/// 재생 방식
/// </summary>
public enum MotionPlayMode
{
    Sequential,     // 순차 재생 (콤보, 스킬 체인 등)
    Blend,          // 블렌딩 (이동 속도에 따른 Idle->Walk->Run)
    Directional,    // 방향성 (8방향 이동)
    Random,         // 랜덤 (Idle 배리에이션)
    Single          // 단일 재생 (일반 애니메이션)
}

/// <summary>
/// 블렌딩 타입 (Blend 모드일 때만 사용)
/// </summary>
public enum MotionBlendType
{
    None,           // 블렌딩 없음
    Linear,         // 1D 블렌딩 (속도)
    Cartesian,      // 2D 블렌딩 (X, Y)
    Directional     // 2D 방향 블렌딩
}

/// <summary>
/// 모션 소스 타입
/// </summary>
public enum MotionSourceType
{
    Clip,           // AnimationClip 사용
    Montage         // AnimationMontage 사용
}

/// <summary>
/// 개별 모션 데이터
/// </summary>
[System.Serializable]
public class MotionData
{
    [Header("모션 소스")]
    [Tooltip("어떤 타입의 애니메이션을 사용할지 선택")]
    public MotionSourceType sourceType = MotionSourceType.Clip;
    
    [Header("애니메이션")]
    [Tooltip("일반 AnimationClip")]
    public AnimationClip clip;
    
    [Tooltip("Montage (섹션, 노티파이가 필요한 경우)")]
    public AnimationMontage montage;
    
    [Header("블렌딩 설정")]
    [Tooltip("블렌딩 임계값 X (Linear: 속도, Cartesian: X좌표)")]
    public float threshold;
    
    [Tooltip("블렌딩 임계값 Y (Cartesian 블렌딩 전용)")]
    public float thresholdY;
    
    [Header("방향 설정")]
    [Tooltip("방향 각도 (0=오른쪽, 90=위, 180=왼쪽, 270=아래), -1=사용안함")]
    public float directionAngle = -1f;
    
    [Header("메타데이터")]
    public string motionName;
    public bool loopable = true;
    
    /// <summary>
    /// 모션이 유효한지 확인
    /// </summary>
    public bool IsValid()
    {
        return sourceType == MotionSourceType.Clip ? clip != null : montage != null;
    }
    
    /// <summary>
    /// 모션 길이 반환
    /// </summary>
    public float GetDuration()
    {
        if (sourceType == MotionSourceType.Clip && clip != null)
            return clip.length;
        
        if (sourceType == MotionSourceType.Montage && montage != null)
        {
            var firstSection = montage.GetFirstSection();
            return firstSection?.Clip?.length ?? 0f;
        }
        
        return 0f;
    }
    
    /// <summary>
    /// 2D 위치 반환 (Cartesian 블렌딩용)
    /// </summary>
    public Vector2 GetPosition2D()
    {
        return new Vector2(threshold, thresholdY);
    }
    
    /// <summary>
    /// 방향 벡터 반환 (Directional 블렌딩용)
    /// </summary>
    public Vector2 GetDirectionVector()
    {
        if (directionAngle < 0) return Vector2.zero;
        
        float rad = directionAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
