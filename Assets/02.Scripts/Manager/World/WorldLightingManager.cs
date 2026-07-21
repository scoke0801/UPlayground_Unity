using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Gameplay.World;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 낮밤 조명 시스템의 진입점.
    /// 씬 전환 시 월드 씬(CurrentMapID 보유)에만 WorldLightingController를 동적 생성하고
    /// 현재 씬의 Light/Volume 레퍼런스를 바인딩한다. 씬에 수동 배치하지 않는다.
    ///
    /// 레퍼런스 우선순위:
    ///   1. 씬의 WorldLightingSceneBinding이 명시한 sunLight/moonLight/globalVolume
    ///   2. RenderSettings.sun → 씬 방향광 자동 검색, 전역 Volume 자동 검색
    ///
    /// 컨트롤러 프리팹은 Addressables 키 "WorldLightingController"로 시도하고,
    /// 없으면 AddComponent + 코드 기본 연출값으로 생성한다.
    /// </summary>
    public class WorldLightingManager : BaseManager<WorldLightingManager>, IManager
    {
        private const string ControllerPrefabKey = "WorldLightingController";

        private GameObject _controllerPrefab; // Addressables 로드 성공 시 캐시
        private WorldLightingController _activeController;

        #region IManager

        public void Init()
        {
            LoadControllerPrefabAsync().Forget();
        }

        public void AfterInit() { }

        public void Dispose()
        {
            DestroyActiveController();
        }

        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            // 컨트롤러는 씬 루트에 생성되므로 씬 전환 시 자동 파괴되지만, 참조는 명시적으로 정리한다.
            _activeController = null;
            EnsureController();
        }

        #endregion

        /// <summary> 현재 씬이 월드 씬이면 컨트롤러를 생성/바인딩한다. </summary>
        private void EnsureController()
        {
            // 월드 씬 판정: 맵 컨텍스트(CurrentMapID)가 있는 씬만. 타이틀/메뉴 씬은 생성하지 않는다.
            string mapId = SceneManager.Instance?.CurrentMapID;
            if (string.IsNullOrEmpty(mapId)) return;

            var binding = UnityEngine.Object.FindFirstObjectByType<WorldLightingSceneBinding>();
            if (binding != null && binding.disableWorldLighting) return;

            Light sun = ResolveSunLight(binding);
            if (sun == null)
            {
                Debug.Log($"[WorldLightingManager] 맵 '{mapId}'에서 태양광을 찾지 못해 낮밤 조명을 생성하지 않습니다.");
                return;
            }

            Light moon = ResolveMoonLight(binding, sun);
            Volume volume = ResolveGlobalVolume(binding);

            _activeController = CreateController();
            _activeController.Bind(sun, moon, volume);

            Debug.Log($"[WorldLightingManager] 맵 '{mapId}' 낮밤 조명 활성화 (sun: {sun.name}, moon: {(moon != null ? moon.name : "없음")}, volume: {(volume != null ? volume.name : "없음")})");
        }

        private WorldLightingController CreateController()
        {
            if (_controllerPrefab != null)
            {
                var instance = UnityEngine.Object.Instantiate(_controllerPrefab);
                var fromPrefab = instance.GetComponent<WorldLightingController>();
                if (fromPrefab != null) return fromPrefab;

                Debug.LogWarning("[WorldLightingManager] 프리팹에 WorldLightingController가 없어 절차 생성으로 대체합니다.");
                UnityEngine.Object.Destroy(instance);
            }

            var go = new GameObject("WorldLightingController (Runtime)");
            var controller = go.AddComponent<WorldLightingController>();
            controller.BuildDefaultProfile();
            return controller;
        }

        // ── 레퍼런스 해석 ────────────────────────────────────────────

        private static Light ResolveSunLight(WorldLightingSceneBinding binding)
        {
            if (binding != null && binding.sunLight != null)
                return binding.sunLight;

            if (RenderSettings.sun != null)
                return RenderSettings.sun;

            // 방향광 자동 검색: 그림자를 만들 수 있는 라이트를 우선하고, 그 안에서 가장 밝은 것을 태양으로 간주
            Light best = null;
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light == null || light.type != LightType.Directional || !light.isActiveAndEnabled) continue;
                if (best == null || CompareSunCandidate(light, best) > 0)
                    best = light;
            }
            return best;
        }

        private static int CompareSunCandidate(Light candidate, Light current)
        {
            bool candidateCastsShadows = candidate.shadows != LightShadows.None;
            bool currentCastsShadows = current.shadows != LightShadows.None;
            if (candidateCastsShadows != currentCastsShadows)
                return candidateCastsShadows ? 1 : -1;

            return candidate.intensity.CompareTo(current.intensity);
        }

        private static Light ResolveMoonLight(WorldLightingSceneBinding binding, Light sun)
        {
            if (binding != null && binding.moonLight != null)
                return binding.moonLight;

            Light best = null;
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light == null || light == sun || light.type != LightType.Directional || !light.isActiveAndEnabled)
                    continue;

                if (best == null || light.intensity > best.intensity)
                    best = light;
            }
            return best;
        }

        private static Volume ResolveGlobalVolume(WorldLightingSceneBinding binding)
        {
            if (binding != null && binding.globalVolume != null)
                return binding.globalVolume;

            Volume best = null;
            var volumes = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            foreach (var volume in volumes)
            {
                if (volume == null || !volume.isGlobal) continue;
                if (best == null || volume.priority > best.priority)
                    best = volume;
            }
            return best;
        }

        private void DestroyActiveController()
        {
            if (_activeController != null)
            {
                UnityEngine.Object.Destroy(_activeController.gameObject);
                _activeController = null;
            }
        }

        // ── 프리팹 로드 ──────────────────────────────────────────────

        private async UniTask LoadControllerPrefabAsync()
        {
            try
            {
                _controllerPrefab = await AssetManager.Instance.TryLoadGlobalAsync<GameObject>(
                    ControllerPrefabKey, nameof(WorldLightingManager));

                if (_controllerPrefab == null)
                    Debug.Log("[WorldLightingManager] WorldLightingController 프리팹이 없어 절차 생성으로 동작합니다.");
            }
            catch (Exception)
            {
                // 프리팹이 Addressables에 없으면 절차 생성 + 코드 기본값으로 동작한다(에러 아님).
                Debug.Log("[WorldLightingManager] WorldLightingController 프리팹이 없어 절차 생성으로 동작합니다.");
            }
        }
    }
}
