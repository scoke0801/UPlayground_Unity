using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Data.Save;
using UPlayGround.Manager;
using UnityEngine;

namespace UPlayGround
{
    /// <summary>지역 날씨가 낙뢰를 확정하고 연출은 반환된 접점만 소비한다.</summary>
    public interface IEnvironmentLightningService : IGameService
    {
        bool TryAdvanceLightning(string mapId, EnvironmentLightningProfileSO profile,
            double gameMinute, float storm, out EnvironmentLightningStrike strike);
        bool TryRequestLightning(string mapId, EnvironmentLightningProfileSO profile,
            int areaIndex, Vector3 sourcePoint, double gameMinute, out EnvironmentLightningStrike strike);
        void SuspendLightning(string mapId);
    }
}
