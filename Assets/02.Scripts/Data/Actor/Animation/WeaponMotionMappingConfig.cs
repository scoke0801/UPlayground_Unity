using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Actor.Animation
{
    /// <summary>
    /// AnimationClip → AnimKey 매핑 정의를 저장하는 SO.
    /// 같은 팩의 다른 캐릭터에 재사용 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponMotionMapping", menuName = "UPlayGround/ActorData/Motion/WeaponMotionMapping")]
    public class WeaponMotionMappingConfig : ScriptableObject
    {
        [Serializable]
        public class ClipEntry
        {
            public AnimationClip clip;
            // FBX 파일명 기반 식별자 (clip.name이 "Take 001"일 때도 의미있는 이름 유지)
            // 다른 캐릭터 팩 재사용 시 이름으로 매핑 복원
            public string        clipDisplayName = "";
            public AnimKey       animKey    = AnimKey.None;
            public int           orderInSet = 0;
            public bool          skip       = false;
        }

        public List<ClipEntry> entries = new();
    }
}
