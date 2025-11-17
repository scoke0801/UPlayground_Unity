using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 모션 재생 방식
/// </summary>
public enum MotionPlayMode
{
    Single,       // 단일 재생
    Sequential,   // 순차 재생
    Blend,        // 블렌딩
    Directional,  // 방향성
    Random        // 랜덤
}

/// <summary>
/// 블렌딩 타입
/// </summary>
public enum MotionBlendType
{
    Linear,      // 1D 블렌딩
    Cartesian,   // 2D 블렌딩 (X, Y)
    Directional  // 2D 방향 블렌딩
}

/// <summary>
/// 모션 세트 - 여러 애니메이션을 그룹으로 관리
/// AvatarMask를 사용하여 신체 부위별 재생 제어
/// </summary>
[CreateAssetMenu(fileName = "New Motion Set", menuName = "Animation/Motion Set")]
public class MotionSet : ScriptableObject
{
    [Header("기본 설정")]
    [SerializeField] private string motionSetName;
    [SerializeField] private MotionPlayMode playMode = MotionPlayMode.Single;
    [SerializeField] private AvatarMask avatarMask; // targetSlot 대체
    
    [Header("블렌딩 설정")]
    [SerializeField] private MotionBlendType blendType = MotionBlendType.Linear;
    [SerializeField] private float blendParameterMax = 10f;
    
    [Header("모션 리스트")]
    [SerializeField] private List<MotionData> motions = new List<MotionData>();
    
    public string MotionSetName => motionSetName;
    public MotionPlayMode PlayMode => playMode;
    public AvatarMask AvatarMask => avatarMask;
    public MotionBlendType BlendType => blendType;
    public float BlendParameterMax => blendParameterMax;
    public List<MotionData> Motions => motions;
    
    #region 모션 검색
    
    /// <summary>
    /// 파라미터 값으로 모션 찾기 (Blend 모드)
    /// </summary>
    public MotionData GetMotionByParameter(float parameter)
    {
        if (motions.Count == 0)
            return null;
        
        // 파라미터에 가장 가까운 모션 찾기
        MotionData closestMotion = motions[0];
        float minDiff = Mathf.Abs(parameter - closestMotion.threshold);
        
        foreach (var motion in motions)
        {
            float diff = Mathf.Abs(parameter - motion.threshold);
            if (diff < minDiff)
            {
                minDiff = diff;
                closestMotion = motion;
            }
        }
        
        return closestMotion;
    }
    
    /// <summary>
    /// 방향으로 모션 찾기 (Directional 모드)
    /// </summary>
    public MotionData GetMotionByDirection(Vector2 direction)
    {
        if (motions.Count == 0)
            return null;
        
        // 입력 방향의 각도 계산
        float inputAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (inputAngle < 0)
            inputAngle += 360f;
        
        // 가장 가까운 각도의 모션 찾기
        MotionData closestMotion = motions[0];
        float minAngleDiff = GetAngleDifference(inputAngle, closestMotion.directionAngle);
        
        foreach (var motion in motions)
        {
            float angleDiff = GetAngleDifference(inputAngle, motion.directionAngle);
            if (angleDiff < minAngleDiff)
            {
                minAngleDiff = angleDiff;
                closestMotion = motion;
            }
        }
        
        return closestMotion;
    }
    
    /// <summary>
    /// 인덱스로 모션 찾기
    /// </summary>
    public MotionData GetMotionByIndex(int index)
    {
        if (index < 0 || index >= motions.Count)
        {
            Debug.LogWarning($"[MotionSet] 인덱스 범위 초과: {index}");
            return null;
        }
        
        return motions[index];
    }
    
    /// <summary>
    /// 랜덤 모션 가져오기
    /// </summary>
    public MotionData GetRandomMotion()
    {
        if (motions.Count == 0)
            return null;
        
        int randomIndex = UnityEngine.Random.Range(0, motions.Count);
        return motions[randomIndex];
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 두 각도의 최소 차이 계산
    /// </summary>
    private float GetAngleDifference(float angle1, float angle2)
    {
        float diff = Mathf.Abs(angle1 - angle2);
        if (diff > 180f)
            diff = 360f - diff;
        return diff;
    }
    
    /// <summary>
    /// 모션 추가
    /// </summary>
    public void AddMotion(MotionData motion)
    {
        motions.Add(motion);
    }
    
    /// <summary>
    /// 모션 제거
    /// </summary>
    public void RemoveMotion(MotionData motion)
    {
        motions.Remove(motion);
    }
    
    /// <summary>
    /// 모든 모션 클리어
    /// </summary>
    public void ClearMotions()
    {
        motions.Clear();
    }
    
    #endregion
}

/// <summary>
/// 모션 데이터 - 개별 애니메이션 정보
/// </summary>
[Serializable]
public class MotionData
{
    [Header("애니메이션")]
    [Tooltip("AnimationClip 또는 AnimationMontage 중 하나만 사용")]
    public AnimationClip clip;
    public AnimationMontage montage;
    
    [Header("설정")]
    public string motionName;
    public bool loopable = false;
    
    [Header("블렌딩 (Blend 모드)")]
    [Tooltip("블렌딩 임계값")]
    public float threshold = 0f;
    
    [Header("방향성 (Directional 모드)")]
    [Tooltip("방향 각도 (0~360)")]
    [Range(0f, 360f)]
    public float directionAngle = 0f;
    
    /// <summary>
    /// 유효한 애니메이션이 있는지 확인
    /// </summary>
    public bool HasValidAnimation => clip != null || (montage != null && montage.AnimationClip != null);
    
    /// <summary>
    /// 실제 재생할 AnimationClip 가져오기
    /// </summary>
    public AnimationClip GetClip()
    {
        if (clip != null)
            return clip;
        
        if (montage != null)
            return montage.AnimationClip;
        
        return null;
    }
}
