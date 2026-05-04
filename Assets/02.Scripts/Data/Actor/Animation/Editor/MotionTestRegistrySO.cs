using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Animation.Editor
{
    [CreateAssetMenu(fileName = "MotionTestRegistry", menuName = "UPlayGround/Editor/Motion Test Registry")]
    public class MotionTestRegistrySO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public ActorDefinitionSO actorDef;
            public AnimationClip idleClip;
            public Vector3 spawnOffset = Vector3.zero;
        }

        [Header("자동 동기화")]
        public ActorDatabase sourceDatabase;

        [Header("항목 목록")]
        public List<Entry> entries = new();
    }
}
