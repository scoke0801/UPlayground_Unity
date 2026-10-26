using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>지도·저장에 쓰는 원본 지역 좌표와 연속 월드 좌표를 연결한다.</summary>
    public interface IWorldCoordinateService : IGameService
    {
        Vector3 ToSourcePosition(string mapId, Vector3 position);
        Vector3 ToWorldPosition(string mapId, Vector3 position);
        Quaternion ToSourceRotation(string mapId, Quaternion rotation);
        Quaternion ToWorldRotation(string mapId, Quaternion rotation);
    }

    /// <summary>단일 씬에서는 좌표를 유지하고 연속 월드에서만 지역 변환을 적용한다.</summary>
    public static class WorldCoordinates
    {
        /// <summary>실제 위치를 원본 지도 좌표로 변환한다.</summary>
        public static Vector3 ToSource(string mapId, Vector3 position) =>
            Services.TryGet<IWorldCoordinateService>(out var service) ? service.ToSourcePosition(mapId, position) : position;
        /// <summary>원본 저장 위치를 실제 월드 좌표로 변환한다.</summary>
        public static Vector3 ToWorld(string mapId, Vector3 position) =>
            Services.TryGet<IWorldCoordinateService>(out var service) ? service.ToWorldPosition(mapId, position) : position;
        /// <summary>실제 회전을 원본 지역 방향으로 변환한다.</summary>
        public static Quaternion ToSource(string mapId, Quaternion rotation) =>
            Services.TryGet<IWorldCoordinateService>(out var service) ? service.ToSourceRotation(mapId, rotation) : rotation;
        /// <summary>원본 저장 회전을 실제 월드 방향으로 변환한다.</summary>
        public static Quaternion ToWorld(string mapId, Quaternion rotation) =>
            Services.TryGet<IWorldCoordinateService>(out var service) ? service.ToWorldRotation(mapId, rotation) : rotation;
    }
}
