using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Story
{
    /// <summary>
    /// 진행도 상승만으로 순서대로 재생할 스토리 Entry 목록.
    /// 월드 트리거가 필요한 서브 스토리와 분리해 메인 진행 이벤트에만 사용한다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/스토리/진행 시퀀스", fileName = "StorySequence_")]
    public class StorySequenceSO : ScriptableObject
    {
        public List<StoryEntrySO> entries = new();
    }
}
