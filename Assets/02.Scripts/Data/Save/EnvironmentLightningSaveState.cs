using System;
using UnityEngine;

namespace UPlayGround.Data.Save
{
    /// <summary>지역 낙뢰의 난수·순번·남은 게임 시간을 환경 입력과 같은 저장 경계로 보존한다.</summary>
    [Serializable]
    public sealed class EnvironmentLightningSaveState
    {
        public string mapId, profileId;
        public int schemaVersion = 1;
        public uint randomState;
        public ulong sequence, committedSequence;
        public double remainingMinutes;
        [NonSerialized] public bool isResumed;
        [NonSerialized] public double lastMinute;
        public EnvironmentLightningSaveState Copy() => (EnvironmentLightningSaveState)MemberwiseClone();
    }

    /// <summary>연출과 논리 입력이 공유하는 한 번의 확정된 지표 타격.</summary>
    public readonly struct EnvironmentLightningStrike
    {
        public readonly string MapId, ProfileId;
        public readonly ulong StrikeId;
        public readonly double GameMinute;
        public readonly Vector3 Point, Normal;
        public readonly int SurfaceLayerId;
        public readonly float Radius, Heat;
        public readonly bool IsWaterSurface;
        public EnvironmentLightningStrike(string mapId, string profileId, ulong id, double minute,
            Vector3 point, Vector3 normal, int layer, float radius, float heat, bool isWaterSurface = false)
        {
            MapId = mapId; ProfileId = profileId; StrikeId = id; GameMinute = minute;
            Point = point; Normal = normal; SurfaceLayerId = layer; Radius = radius; Heat = heat;
            IsWaterSurface = isWaterSurface;
        }
    }
}
