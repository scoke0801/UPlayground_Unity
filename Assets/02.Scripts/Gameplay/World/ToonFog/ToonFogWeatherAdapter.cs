using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Rendering;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World.ToonFog
{
    /// <summary>기존 날씨 시간표와 교전 판정을 안개 입력으로 변환한다.</summary>
    public sealed class ToonFogWeatherAdapter : MonoBehaviour, IToonFogEnvironmentSource
    {
        [SerializeField] private EnvironmentController _weather;
        [SerializeField] private ToonFogQualitySettings _quality;

        public bool TryGetInput(Camera camera, out ToonFogEnvironmentInput input)
        {
            input = ToonFogEnvironmentInput.Identity;
            if (_weather != null && _weather.IsReady && _quality != null)
            {
                input.DensityMultiplier = Mathf.Lerp(1, _quality.rainDensityMultiplier,
                    Mathf.Max(_weather.Current.Rain, _weather.Current.Storm));
                input.WindVelocityXZ = _weather.Current.Wind * _quality.weatherWindMetersPerSecond;
            }
            if (camera.cameraType == CameraType.Game && Application.isPlaying &&
                Services.TryGet<ICombatEncounterService>(out var combat))
                input.CombatWeight = combat.CurrentEncounter == CombatEncounterKind.Explore ? 0 : 1;
            if (camera.cameraType == CameraType.Game && Application.isPlaying
                && Services.TryGet<IActorQueryService>(out var actorQuery))
                input.PlayerAnchor = actorQuery.PlayerTransform;
            return true;
        }
    }
}
