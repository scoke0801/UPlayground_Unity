using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Debugging
{
    public class DebugGizmoManager : BaseManager<DebugGizmoManager>, IManager,
        IAsyncInitializableManager, IUpdatableManager
    {
        // Addressable로 로드하는 설정(에디터 전용). 빌드에는 포함하지 않으며,
        // 로드 전/실패 시 아래 기본값을 사용한다.
        private DebugGizmoSettingsSO _settings;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private DebugGizmoCategory _enabledCategories =
            DebugGizmoCategory.Combat | DebugGizmoCategory.AI | DebugGizmoCategory.Movement;
        [SerializeField] private DebugGizmoContentType _enabledContentTypes = DebugGizmoContentType.All;
        [SerializeField] private bool _drawLabels = true;
        [SerializeField] private bool _drawOnlyFocus;
        [SerializeField] private float _maxDrawDistance = 60f;

        private readonly List<IDebugGizmoProvider> _providers = new();
        private readonly DebugGizmoDrawContext _drawContext = new();
        private readonly DebugGizmoFrameRecorder _recorder = new();
        private GameObject _focusObject;

        public bool Enabled => _enabled;
        public DebugGizmoCategory EnabledCategories => _enabledCategories;
        public DebugGizmoContentType EnabledContentTypes => _enabledContentTypes;
        public bool DrawLabels => _drawLabels;
        public bool DrawOnlyFocus => _drawOnlyFocus;
        public float MaxDrawDistance => _maxDrawDistance;
        public GameObject FocusObject => _focusObject;
        public IReadOnlyList<IDebugGizmoProvider> Providers => _providers;
        public DebugGizmoFrameRecorder Recorder => _recorder;

        // 디버그 기즈모는 에디터 전용 도구이므로, 빌드에서는 아래 static 진입점들이 모두 no-op이 된다.
        public static void RegisterProvider(IDebugGizmoProvider provider)
        {
#if UNITY_EDITOR
            if (provider == null || !Application.isPlaying)
                return;

            Instance.Register(provider);
#endif
        }

        public static void UnregisterProvider(IDebugGizmoProvider provider)
        {
#if UNITY_EDITOR
            if (provider == null)
                return;

            var manager = FindFirstObjectByType<DebugGizmoManager>();
            manager?.Unregister(provider);
#endif
        }

        public static bool ShouldSuppressLocalGizmos(
            DebugGizmoCategory category,
            GameObject owner,
            DebugGizmoContentType contentType = DebugGizmoContentType.All)
        {
#if UNITY_EDITOR
            var manager = FindFirstObjectByType<DebugGizmoManager>();
            if (manager == null
                || !manager.Enabled
                || !manager.IsCategoryEnabled(category)
                || !manager.IsContentTypeEnabled(contentType))
                return false;

            if (!manager._drawOnlyFocus || manager._focusObject == null || owner == null)
                return true;

            return manager.IsFocusMatch(owner);
#else
            return false;
#endif
        }

        /// <summary>
        /// 중앙에서 그리지 않고 로컬 컴포넌트가 직접 그리는 기즈모(예: HitBox 스윙 궤적)의 표시 허용 여부.
        /// 중앙 핸들러가 있어 중복을 막는 <see cref="ShouldSuppressLocalGizmos"/> 와 반대로,
        /// 여기서는 "그려도 되는가"를 카테고리/콘텐츠 토글 기준으로 판단한다.
        /// 매니저가 없으면(에디트 모드 등) 로컬 컴포넌트 자체 토글에 위임하도록 true 를 반환한다.
        /// </summary>
        public static bool IsLocalContentEnabled(
            DebugGizmoCategory category,
            DebugGizmoContentType contentType)
        {
#if UNITY_EDITOR
            var manager = FindFirstObjectByType<DebugGizmoManager>();
            if (manager == null)
                return true;

            return manager.Enabled
                   && manager.IsCategoryEnabled(category)
                   && manager.IsContentTypeEnabled(contentType);
#else
            return false;
#endif
        }

        public void SetEnabled(bool value) => _enabled = value;
        public void SetCategory(DebugGizmoCategory category, bool enabled)
        {
            _enabledCategories = enabled ? _enabledCategories | category : _enabledCategories & ~category;
        }
        public void SetContentType(DebugGizmoContentType contentType, bool enabled)
        {
            _enabledContentTypes = enabled ? _enabledContentTypes | contentType : _enabledContentTypes & ~contentType;
        }
        public void SetDrawLabels(bool value) => _drawLabels = value;
        public void SetDrawOnlyFocus(bool value) => _drawOnlyFocus = value;
        public void SetMaxDrawDistance(float value) => _maxDrawDistance = Mathf.Max(0f, value);
        public void SetFocusObject(GameObject focusObject) => _focusObject = focusObject;
        public bool IsCategoryEnabled(DebugGizmoCategory category) => (_enabledCategories & category) != 0;
        public bool IsContentTypeEnabled(DebugGizmoContentType contentType)
        {
            return contentType != DebugGizmoContentType.None && (_enabledContentTypes & contentType) != 0;
        }

        public bool PassesProviderFilters(IDebugGizmoProvider provider, bool checkDistance)
        {
            if (provider == null || !provider.IsAvailable || !IsCategoryEnabled(provider.Category))
                return false;

            if (!IsContentTypeEnabled(provider.ContentType))
                return false;

            if (_drawOnlyFocus && _focusObject != null && !IsFocusMatch(provider.Owner))
                return false;

            return !checkDistance || PassesDistance(provider.Owner);
        }

        public string GetProviderDisplayName(IDebugGizmoProvider provider)
        {
            if (provider == null)
                return string.Empty;

            UnityEngine.Object owner = provider.Owner;
            string ownerName = owner != null ? owner.name : "None";
            return $"{provider.ContentType} / {provider.Category} / {provider.GetType().Name} / {ownerName}";
        }

        public void Register(IDebugGizmoProvider provider)
        {
            if (provider == null || _providers.Contains(provider))
                return;

            _providers.Add(provider);
        }

        public void Unregister(IDebugGizmoProvider provider)
        {
            _providers.Remove(provider);
        }

        public void Init()
        {
            DebugGizmoBridge.RegisterHandler = Register;
            DebugGizmoBridge.UnregisterHandler = Unregister;
            DebugGizmoBridge.SuppressLocalHandler = ShouldSuppressLocalGizmosForActor;
            DebugGizmoBridge.IsLocalContentEnabledHandler =
                (category, contentType) =>
                    Enabled && IsCategoryEnabled(category) && IsContentTypeEnabled(contentType);
            Debug.Log("[DebugGizmoManager] 초기화");
        }

        private bool ShouldSuppressLocalGizmosForActor(
            DebugGizmoCategory category,
            GameObject owner,
            DebugGizmoContentType contentType)
        {
            if (!Enabled || !IsCategoryEnabled(category) || !IsContentTypeEnabled(contentType))
                return false;

            if (!_drawOnlyFocus || _focusObject == null || owner == null)
                return true;

            return IsFocusMatch(owner);
        }

#if UNITY_EDITOR
        private const string SettingsAddressableKey = "DebugGizmoSettings";

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                var settings = await AssetManager.Instance.LoadGlobalAsync<DebugGizmoSettingsSO>(
                    SettingsAddressableKey,
                    nameof(DebugGizmoManager),
                    cancellationToken);
                if (settings == null)
                {
                    Debug.LogWarning($"[DebugGizmoManager] '{SettingsAddressableKey}' Addressable 로드 실패: 기본값 사용");
                    return;
                }

                _settings = settings;
                _enabledCategories = settings.defaultCategories;
                _enabledContentTypes = settings.defaultContentTypes;
                _drawLabels = settings.drawLabels;
                _drawOnlyFocus = settings.drawOnlyFocus;
                _maxDrawDistance = settings.maxDrawDistance;
                _recorder.SetRecording(settings.recordFrames);

                Debug.Log("[DebugGizmoManager] 설정 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[DebugGizmoManager] 설정 로드 예외(기본값 사용): {e.Message}");
            }
        }
#else
        public UniTask InitializeAsync(CancellationToken cancellationToken) =>
            UniTask.CompletedTask;
#endif

        public void AfterInit() { }
        public void Dispose()
        {
            DebugGizmoBridge.Clear();
            _providers.Clear();
            _recorder.Clear();
        }

        public void OnUpdate()
        {
            if (!_enabled || !_recorder.IsRecording)
                return;

            var snapshot = _recorder.BeginFrame(_settings != null ? _settings.recordSeconds : 10f);
            PruneInvalidProviders();
            for (int i = 0; i < _providers.Count; i++)
            {
                var provider = _providers[i];
                if (PassesProviderFilters(provider, false))
                    provider.CollectSnapshot(snapshot);
            }
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) => PruneInvalidProviders();

        private void OnDrawGizmos()
        {
            if (!_enabled || !Application.isPlaying)
                return;

#if UNITY_EDITOR
            Camera sceneCamera = UnityEditor.SceneView.lastActiveSceneView != null
                ? UnityEditor.SceneView.lastActiveSceneView.camera
                : null;
#else
            Camera sceneCamera = Camera.main;
#endif
            _drawContext.Reset(_enabledCategories, _focusObject, _drawLabels, _drawOnlyFocus, _maxDrawDistance, Time.time, sceneCamera);

            PruneInvalidProviders();
            for (int i = 0; i < _providers.Count; i++)
            {
                var provider = _providers[i];
                if (!PassesProviderFilters(provider, true))
                    continue;

                provider.DrawGizmos(_drawContext);
            }
        }

        private bool PassesDistance(UnityEngine.Object owner)
        {
            if (_maxDrawDistance <= 0f || owner == null)
                return true;

            GameObject go = GetOwnerGameObject(owner);
            return go == null || _drawContext.PassesDistance(go.transform.position);
        }

        private bool IsFocusMatch(UnityEngine.Object owner)
        {
            GameObject ownerObject = GetOwnerGameObject(owner);
            return ownerObject != null && IsFocusMatch(ownerObject);
        }

        private bool IsFocusMatch(GameObject ownerObject)
        {
            if (_focusObject == null || ownerObject == null)
                return false;

            return ownerObject == _focusObject
                   || ownerObject.transform.IsChildOf(_focusObject.transform)
                   || _focusObject.transform.IsChildOf(ownerObject.transform);
        }

        private static GameObject GetOwnerGameObject(UnityEngine.Object owner)
        {
            return owner switch
            {
                GameObject go => go,
                UnityEngine.Component component => component.gameObject,
                _ => null,
            };
        }

        private void PruneInvalidProviders()
        {
            for (int i = _providers.Count - 1; i >= 0; i--)
            {
                if (_providers[i] == null || _providers[i].Owner == null)
                    _providers.RemoveAt(i);
            }
        }
    }
}
