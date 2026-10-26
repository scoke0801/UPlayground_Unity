using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Data.Combat
{
    /// <summary>모델과 무기 조건에서 베이크한 타격 구간의 운동 정보를 런타임에 제공한다.</summary>
    public sealed class MotionImpactCatalogSO : ScriptableObject
    {
        public List<MotionImpactWindow> windows = new();

        /// <summary>같은 페이즈가 재개되더라도 현재 타격 구간 안에서만 표본을 선택한다.</summary>
        public bool TrySample(MotionKey key, int phase, float time, out MotionImpactWindow window,
            out Vector3 localVelocity)
        {
            window = null;
            localVelocity = default;
            if (!key.IsValid || float.IsNaN(time) || float.IsInfinity(time)) return false;
            for (int i = 0; i < windows.Count; i++)
            {
                var candidate = windows[i];
                if (candidate == null || candidate.motionKey != key || candidate.hitPhaseIndex != phase
                    || time < candidate.start || time > candidate.end) continue;
                if (!candidate.TrySampleVelocity(time, out localVelocity)) return false;
                window = candidate;
                return true;
            }
            return false;
        }
    }

    /// <summary>루트 병진을 제외한 칼끝 속도와 현재 루트 회전 기준 방향을 저장한다.</summary>
    [Serializable]
    public sealed class MotionImpactWindow
    {
        public MotionKey motionKey;
        public int hitPhaseIndex;
        public float start, end, meanSpeed, peakSpeed;
        public string sourceFingerprint;
        public List<MotionImpactSample> samples = new();

        /// <summary>인접한 두 표본을 보간하며 타격 구간 밖은 평가하지 않는다.</summary>
        public bool TrySampleVelocity(float time, out Vector3 velocity)
        {
            velocity = default;
            if (samples == null || samples.Count < 2 || time < start || time > end) return false;
            int low = 0, high = samples.Count - 1;
            while (high - low > 1)
            {
                int middle = (low + high) / 2;
                if (samples[middle].time <= time) low = middle;
                else high = middle;
            }
            var before = samples[low];
            var after = samples[high];
            velocity = Vector3.Lerp(before.localVelocity, after.localVelocity,
                Mathf.InverseLerp(before.time, after.time, time));
            return !float.IsNaN(velocity.sqrMagnitude) && !float.IsInfinity(velocity.sqrMagnitude);
        }
    }

    /// <summary>저작 타임라인 초와 루트 로컬 공간의 무기 속도 표본.</summary>
    [Serializable]
    public struct MotionImpactSample
    {
        public float time;
        public Vector3 localVelocity;
    }
}
