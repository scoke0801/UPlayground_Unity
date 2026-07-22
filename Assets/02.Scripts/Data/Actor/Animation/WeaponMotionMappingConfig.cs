using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Actor.Animation
{
    /// <summary>
    /// AnimationClip → Motion Slot/콘텐츠 그룹 매핑 정의를 저장하는 SO.
    /// 같은 팩의 다른 캐릭터에 재사용 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponMotionMapping", menuName = "UPlayGround/애니메이션/Weapon Motion Mapping")]
    public class WeaponMotionMappingConfig : ScriptableObject
    {
        [Serializable]
        public class ClipEntry
        {
            public AnimationClip clip;
            // FBX 파일명 기반 식별자 (clip.name이 "Take 001"일 때도 의미있는 이름 유지)
            // 다른 캐릭터 팩 재사용 시 이름으로 매핑 복원
            public string        clipDisplayName = "";
            public GameplayTag motionSlot;
            [Tooltip("콘텐츠 모션을 묶는 에디터 전용 식별자입니다. 생성된 MotionSetAsset은 Payload에서 직접 참조합니다.")]
            public string contentGroup;
            public int           orderInSet = 0;
            public bool          skip       = false;
        }

        public List<ClipEntry> entries = new();
    }
}
