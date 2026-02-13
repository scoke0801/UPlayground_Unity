    using System;
    using System.Collections.Generic;
    using UnityEngine;

    namespace UPlayGround.Animation
    {    
        [Serializable]
        public class Motion
        {
            public string motionName;
            public AnimationClip motionClip;
            public List<MotionEvent> eventList;
            
            // 유효성 검사
            public bool IsValid() => motionClip != null;
            public float Duration => motionClip != null ? motionClip.length : 0f;

        }
        
        [Serializable]
        public class MotionEvent
        {
            public float startTime;
            public float endTime;
            
            public string param;    // ;단위로 끊어서 입력
            
            // 파라미터 파싱
            public string[] GetParseParams() => param?.Split(';') ?? new string[0];
            
            // 이벤트가 특정 시간에 활성화되는지 확인
            public bool IsActiveAt(float time) => time >= startTime && time <= endTime;
        }
        
        [Serializable]
        public class MotionSet
        {
            public string motionSetName;
            public List<Motion> motions;
            
            public List<MotionEvent> eventList;
            
            // 전체 재생 시간
            public float TotalDuration
            {
                get
                {
                    float total = 0f;
                    foreach (var motion in motions)
                    {
                        total += motion.Duration;
                    }
                    return total;
                }
            }
        
            // 유효성 검사
            public bool IsValid() => motions != null && motions.Count > 0;
        }
    }