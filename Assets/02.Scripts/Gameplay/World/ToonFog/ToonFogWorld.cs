using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Data.Rendering;

namespace UPlayGround.Gameplay.World.ToonFog
{
    public struct ToonFogEnvironmentInput
    {
        public float DensityMultiplier;
        public Color TintMultiplier;
        public Vector2 WindVelocityXZ;
        public float CombatWeight;
        public Transform PlayerAnchor;
        public bool ResetTransition;

        public static ToonFogEnvironmentInput Identity => new()
        {
            DensityMultiplier = 1,
            TintMultiplier = Color.white
        };

        internal bool IsValid => float.IsFinite(DensityMultiplier) && DensityMultiplier >= 0
            && float.IsFinite(TintMultiplier.r) && float.IsFinite(TintMultiplier.g)
            && float.IsFinite(TintMultiplier.b) && TintMultiplier.r >= 0 && TintMultiplier.g >= 0 && TintMultiplier.b >= 0
            && float.IsFinite(WindVelocityXZ.x) && float.IsFinite(WindVelocityXZ.y)
            && float.IsFinite(CombatWeight);
    }

    public interface IToonFogEnvironmentSource
    {
        bool TryGetInput(Camera camera, out ToonFogEnvironmentInput input);
    }

    /// <summary>씬의 안개 프로필과 환경 입력을 하나의 진입점에서 공급한다.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ToonFogWorld : MonoBehaviour
    {
        private static readonly List<ToonFogWorld> Worlds = new();
        internal static readonly List<ToonFogRegion> Regions = new();
        internal static readonly List<ToonFogLocalVolume> Volumes = new();

        public enum View { Final, LinearDistance, WorldHeight, Transmittance, DistanceTau, HeightTau, LocalTau, LocalCount, VolumeId }

        public bool fogEnabled = true;
        public ToonFogProfile profile;
        public ToonFogQualitySettings quality;
        public Transform playerAnchor;
        public MonoBehaviour environmentSource;
        public bool captureLegacyRefraction;
        public bool showInSceneView = true;
        public View diagnostics;

        private uint _transitionRevision;
        internal uint TransitionRevision => _transitionRevision;

        /// <summary>텔레포트나 컷에서 해당 월드의 카메라 보간을 한 번 초기화한다.</summary>
        public void ResetTransition() => _transitionRevision++;

        private void OnEnable() { if (!Worlds.Contains(this)) Worlds.Add(this); }
        private void OnDisable() => Worlds.Remove(this);

        internal static ToonFogWorld Find(Camera camera)
        {
            Scene active = SceneManager.GetActiveScene();
            ToonFogWorld best = null;
            int bestPriority = -1;
            foreach (var candidate in Worlds)
            {
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
                Scene scene = candidate.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded || scene.isSubScene) continue;
                if (scene != active && scene != camera.gameObject.scene) continue;
                int priority = scene == camera.gameObject.scene ? 1 : 0;
                if (priority > bestPriority || (priority == bestPriority && candidate.GetInstanceID() < best.GetInstanceID()))
                {
                    best = candidate;
                    bestPriority = priority;
                }
            }
            return best;
        }

        internal ToonFogEnvironmentInput GetInput(Camera camera)
        {
            if (environmentSource != null && environmentSource.isActiveAndEnabled
                && environmentSource is IToonFogEnvironmentSource source
                && source.TryGetInput(camera, out var input) && input.IsValid)
                return input;
            return ToonFogEnvironmentInput.Identity;
        }
    }
}
