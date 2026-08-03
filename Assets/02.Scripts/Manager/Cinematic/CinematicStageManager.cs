using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UPlayGround.Components;
using UPlayGround.Data.Cinematic;
using UPlayGround.Gameplay.World;

namespace UPlayGround.Manager.Cinematic
{
    /// <summary>
    /// 전역 단일 연출 무대의 소유권, 포즈 미러, 카메라 격리와 복구를 관리한다.
    /// 실제 액터의 위치·레이어·전투 상태는 변경하지 않는다.
    /// </summary>
    public sealed class CinematicStageManager : BaseManager<CinematicStageManager>,
        IManager,
        IAsyncInitializableManager,
        IUpdatableManager,
        ILateUpdatableManager,
        ICinematicStageService
    {
        private const string PreloadCatalogResourcePath =
            "UPlayGround/CinematicStagePreloadCatalog";

        private readonly CinematicCloneFactory _cloneFactory = new();
        private readonly Dictionary<string, GameObject> _preloadedStageRoots =
            new(StringComparer.Ordinal);
        private CinematicStageInstance _active;
        private ulong _nextTicketValue = 1;
        private CanvasGroup _transitionGroup;
        private Image _transitionImage;
        private Coroutine _transitionRoutine;
        private bool _isExiting;
        private GameObject _letterboxRoot;
        private RectTransform _letterboxTop;
        private RectTransform _letterboxBottom;
        private Image _letterboxTopImage;
        private Image _letterboxBottomImage;
        private Coroutine _letterboxRoutine;
        private float _letterboxProgress;
        private float _letterboxHeightRatio = 0.1f;
        private CinematicStagePreloadCatalogSO _preloadCatalog;

        public bool IsActive => _active != null || _isExiting;
        public CinematicStageTicket ActiveTicket => _active?.Ticket ?? default;
        public Matrix4x4 StageTransform => _active?.StageTransform ?? Matrix4x4.identity;

        public void Init()
        {
            _cloneFactory.Configure(transform);
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            _preloadCatalog = Resources.Load<CinematicStagePreloadCatalogSO>(
                PreloadCatalogResourcePath);
            if (_preloadCatalog == null)
                return;

            IReadOnlyList<string> sceneNames = _preloadCatalog.SceneNames;
            for (int i = 0; i < sceneNames.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await PreloadStageSceneAsync(sceneNames[i], cancellationToken);
            }
        }

        public void AfterInit() { }

        public void Dispose()
        {
            ForceExit(CinematicStageExitReason.Disabled, playTransition: false);
            _cloneFactory.Dispose();
            DestroyTransitionOverlay();
            DestroyLetterboxOverlay();

            foreach (GameObject root in _preloadedStageRoots.Values)
            {
                if (root != null)
                    Destroy(root);
            }
            _preloadedStageRoots.Clear();

            if (_preloadCatalog != null)
                Resources.UnloadAsset(_preloadCatalog);
            _preloadCatalog = null;
        }

        public void OnUpdate()
        {
            if (_active == null)
                return;

            if (_active.Owner == null)
            {
                Debug.LogWarning("[CinematicStage] 소유자가 파괴되어 무대를 강제 종료합니다.");
                ForceExit(CinematicStageExitReason.OwnerLost);
                return;
            }

            if (Time.unscaledTime - _active.StartedAtUnscaledTime
                > _active.Definition.maxStageSeconds)
            {
                Debug.LogWarning(
                    $"[CinematicStage] 워치독 제한({_active.Definition.maxStageSeconds:F1}초)을 초과해 무대를 강제 종료합니다.");
                ForceExit(CinematicStageExitReason.WatchdogTimeout);
            }
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate()
        {
            _active?.LateUpdate();
        }

        public void OnSceneChanged(string sceneType)
        {
            HideLetterbox(0f);
            ForceExit(CinematicStageExitReason.SceneChanged, playTransition: false);
            // 종료 트랜지션 코루틴이 씬 전환으로 중단되면 finally가 실행되지 않을 수 있으므로
            // 조건 없이 트랜지션 상태를 원복해 암전 오버레이와 _isExiting 잔존을 막는다.
            CancelTransitionState();
        }

        public bool TryEnter(
            in CinematicStageRequest request,
            out CinematicStageTicket ticket)
        {
            ticket = default;
            // 거부 사유를 구분해 남긴다. 종료 트랜지션 대기 때문인지, 이미 활성 무대가 있는지에 따라
            // 호출부에서 취해야 할 대응(재시도 vs 강등)이 다르다.
            if (_isExiting)
            {
                Debug.LogWarning(
                    "[CinematicStage] 이전 연출 무대의 종료 트랜지션이 진행 중이라 요청을 강등합니다.");
                return false;
            }

            if (_active != null)
            {
                Debug.LogWarning("[CinematicStage] 이미 다른 연출 무대가 활성 상태라 요청을 강등합니다.");
                return false;
            }

            if (request.Stage == null
                || request.Stage.tier is CinematicStageTier.None
                    or CinematicStageTier.CameraOnly)
            {
                return false;
            }

            if (request.Owner == null || request.Caster == null || request.CasterModelRoot == null)
            {
                Debug.LogWarning("[CinematicStage] 소유자, 시전자 또는 시전자 Model 루트가 없어 요청을 강등합니다.");
                return false;
            }

            int actorLayer = LayerMask.NameToLayer("UltimateActor");
            if (actorLayer < 0)
            {
                Debug.LogError("[CinematicStage] UltimateActor 레이어가 없습니다.");
                return false;
            }

            var nextTicket = new CinematicStageTicket(_nextTicketValue++);
            if (_nextTicketValue == 0)
                _nextTicketValue = 1;

            GameObject preloadedStageRoot = null;
            if (!string.IsNullOrWhiteSpace(request.Stage.stageSceneName))
            {
                _preloadedStageRoots.TryGetValue(
                    request.Stage.stageSceneName,
                    out preloadedStageRoot);
            }

            if (!CinematicStageInstance.TryCreate(
                    request,
                    nextTicket,
                    _cloneFactory,
                    preloadedStageRoot,
                    actorLayer,
                    out CinematicStageInstance instance,
                    out string error))
            {
                Debug.LogWarning($"[CinematicStage] 진입 실패, 기존 연출로 강등합니다: {error}");
                return false;
            }

            _active = instance;
            ticket = nextTicket;
            PlayTransition(
                request.Stage.enterTransition,
                request.Stage.enterTransitionDuration);
            return true;
        }

        public void RegisterTransient(
            in CinematicStageTicket ticket,
            GameObject instance)
        {
            if (instance == null || _active == null || ticket != _active.Ticket)
                return;

            _active.RegisterTransient(instance);
        }

        public void ShowLetterbox(UltimateLetterboxSettings settings)
        {
            if (settings?.enabled != true)
                return;

            EnsureLetterboxOverlay();
            if (_letterboxRoutine != null)
                StopCoroutine(_letterboxRoutine);

            _letterboxHeightRatio = Mathf.Clamp(settings.heightRatio, 0.02f, 0.3f);
            _letterboxTopImage.color = settings.color;
            _letterboxBottomImage.color = settings.color;
            _letterboxRoot.SetActive(true);
            if (settings.enterDuration <= 0f)
            {
                SetLetterboxProgress(1f);
                _letterboxRoutine = null;
                return;
            }
            _letterboxRoutine = StartCoroutine(
                AnimateLetterbox(_letterboxProgress, 1f, settings.enterDuration));
        }

        public void HideLetterbox(float duration)
        {
            if (_letterboxRoot == null)
                return;

            if (_letterboxRoutine != null)
                StopCoroutine(_letterboxRoutine);
            if (duration <= 0f)
            {
                SetLetterboxProgress(0f);
                _letterboxRoot.SetActive(false);
                _letterboxRoutine = null;
                return;
            }
            _letterboxRoutine = StartCoroutine(
                AnimateLetterbox(_letterboxProgress, 0f, Mathf.Max(0f, duration)));
        }

        public bool TryResolvePresentationTransform(
            Transform source,
            out Transform presentation)
        {
            presentation = null;
            return _active != null
                   && _active.TryResolvePresentationTransform(
                       source,
                       out presentation);
        }

        public void Exit(
            in CinematicStageTicket ticket,
            CinematicStageExitReason reason)
        {
            if (_active == null || !ticket.IsValid || ticket != _active.Ticket)
                return;

            ForceExit(reason);
        }

        private void ForceExit(
            CinematicStageExitReason reason,
            bool playTransition = true)
        {
            // 종료 트랜지션의 리빌 단계에서는 _active가 이미 null이지만 _isExiting은 true다.
            // 이때도 정리 경로에 들어와야 암전 오버레이와 _isExiting이 잔존하지 않는다.
            if (_active == null && !_isExiting)
                return;

            if (_isExiting && playTransition)
                return;

            CinematicStageInstance ending = _active;
            if (playTransition
                && ending != null
                && ending.Definition.exitTransition != CinematicStageTransitionType.None
                && ending.Definition.exitTransitionDuration > 0f)
            {
                BeginExitTransition(
                    ending,
                    reason,
                    ending.Definition.exitTransition,
                    ending.Definition.exitTransitionDuration);
                return;
            }

            _active = null;
            // 리빌 단계에서 진입한 경우 ending은 null이며 무대는 이미 Dispose된 상태다.
            ending?.Dispose(_cloneFactory);
            CancelTransitionState();

            if (reason is CinematicStageExitReason.WatchdogTimeout
                or CinematicStageExitReason.Failed)
            {
                Debug.LogWarning($"[CinematicStage] 비정상 종료: {reason}");
            }
        }

        private void PlayTransition(CinematicStageTransitionType type, float duration)
        {
            if (type == CinematicStageTransitionType.None || duration <= 0f)
                return;

            EnsureTransitionOverlay();
            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);

            _transitionImage.color = type == CinematicStageTransitionType.WhiteFlash
                ? Color.white
                : Color.black;
            _transitionRoutine = StartCoroutine(FadeTransitionRoutine(duration));
        }

        private IEnumerator FadeTransitionRoutine(float duration)
        {
            _transitionGroup.alpha = 1f;
            _transitionGroup.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / duration));
                _transitionGroup.alpha = 1f - progress;
                yield return null;
            }

            ResetTransitionOverlay();
            _transitionRoutine = null;
        }

        private void BeginExitTransition(
            CinematicStageInstance ending,
            CinematicStageExitReason reason,
            CinematicStageTransitionType type,
            float duration)
        {
            EnsureTransitionOverlay();
            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);

            _isExiting = true;
            ending.FreezePresentation();
            _transitionImage.color = type == CinematicStageTransitionType.WhiteFlash
                ? Color.white
                : Color.black;
            _transitionRoutine = StartCoroutine(
                ExitTransitionRoutine(ending, reason, duration));
        }

        private IEnumerator ExitTransitionRoutine(
            CinematicStageInstance ending,
            CinematicStageExitReason reason,
            float duration)
        {
            // 중간에 예외가 발생해도 _isExiting과 암전 오버레이가 잔존하지 않도록 finally로 감싼다.
            try
            {
                _transitionGroup.blocksRaycasts = true;
                float coverDuration = Mathf.Max(0.01f, duration * 0.45f);
                float revealDuration = Mathf.Max(0.01f, duration - coverDuration);
                float startAlpha = _transitionGroup.alpha;
                float elapsed = 0f;

                while (elapsed < coverDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(elapsed / coverDuration));
                    _transitionGroup.alpha = Mathf.Lerp(startAlpha, 1f, progress);
                    yield return null;
                }

                _transitionGroup.alpha = 1f;
                if (_active == ending)
                    _active = null;
                ending.Dispose(_cloneFactory);

                elapsed = 0f;
                while (elapsed < revealDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(elapsed / revealDuration));
                    _transitionGroup.alpha = 1f - progress;
                    yield return null;
                }

                if (reason is CinematicStageExitReason.WatchdogTimeout
                    or CinematicStageExitReason.Failed)
                {
                    Debug.LogWarning($"[CinematicStage] 비정상 종료: {reason}");
                }
            }
            finally
            {
                // 커버 단계에서 예외로 빠져나온 경우 무대가 아직 살아 있을 수 있으므로 함께 정리한다.
                if (_active == ending)
                {
                    _active = null;
                    ending.Dispose(_cloneFactory);
                }

                _isExiting = false;
                _transitionRoutine = null;
                ResetTransitionOverlay();
            }
        }

        /// <summary>
        /// 진행 중인 트랜지션 코루틴을 중단하고 종료 상태와 오버레이를 조건 없이 원복한다.
        /// 코루틴이 강제 중단되어 finally가 실행되지 않는 경로의 최종 방어선이다.
        /// </summary>
        private void CancelTransitionState()
        {
            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
            _isExiting = false;
            ResetTransitionOverlay();
        }

        private void ResetTransitionOverlay()
        {
            if (_transitionGroup == null)
                return;

            _transitionGroup.alpha = 0f;
            _transitionGroup.blocksRaycasts = false;
        }

        private void EnsureLetterboxOverlay()
        {
            if (_letterboxRoot != null)
                return;

            _letterboxRoot = new GameObject(
                "Ultimate Letterbox",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            _letterboxRoot.transform.SetParent(transform, false);

            Canvas canvas = _letterboxRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue - 10;
            CanvasScaler scaler = _letterboxRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _letterboxTop = CreateLetterboxBar("Top", out _letterboxTopImage);
            _letterboxBottom = CreateLetterboxBar("Bottom", out _letterboxBottomImage);
            SetLetterboxProgress(0f);
            _letterboxRoot.SetActive(false);
        }

        private RectTransform CreateLetterboxBar(string name, out Image image)
        {
            var barObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(_letterboxRoot.transform, false);
            var rect = barObject.GetComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            image = barObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            return rect;
        }

        private IEnumerator AnimateLetterbox(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetLetterboxProgress(to);
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(elapsed / duration));
                    SetLetterboxProgress(Mathf.Lerp(from, to, progress));
                    yield return null;
                }
                SetLetterboxProgress(to);
            }

            if (to <= 0f && _letterboxRoot != null)
                _letterboxRoot.SetActive(false);
            _letterboxRoutine = null;
        }

        private void SetLetterboxProgress(float progress)
        {
            _letterboxProgress = Mathf.Clamp01(progress);
            if (_letterboxTop == null || _letterboxBottom == null)
                return;

            float hiddenOffset = (1f - _letterboxProgress) * _letterboxHeightRatio;
            _letterboxTop.anchorMin = new Vector2(
                0f,
                1f - _letterboxHeightRatio + hiddenOffset);
            _letterboxTop.anchorMax = new Vector2(1f, 1f + hiddenOffset);
            _letterboxBottom.anchorMin = new Vector2(0f, -hiddenOffset);
            _letterboxBottom.anchorMax = new Vector2(
                1f,
                _letterboxHeightRatio - hiddenOffset);
        }

        private void DestroyLetterboxOverlay()
        {
            if (_letterboxRoutine != null)
                StopCoroutine(_letterboxRoutine);
            _letterboxRoutine = null;

            if (_letterboxRoot != null)
                Destroy(_letterboxRoot);
            _letterboxRoot = null;
            _letterboxTop = null;
            _letterboxBottom = null;
            _letterboxTopImage = null;
            _letterboxBottomImage = null;
            _letterboxProgress = 0f;
        }

        private void EnsureTransitionOverlay()
        {
            if (_transitionGroup != null)
                return;

            var root = new GameObject(
                "Cinematic Stage Transition",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            root.transform.SetParent(transform, false);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            _transitionGroup = root.GetComponent<CanvasGroup>();
            _transitionGroup.alpha = 0f;
            _transitionGroup.blocksRaycasts = false;

            var imageObject = new GameObject("Transition Image", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(root.transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _transitionImage = imageObject.GetComponent<Image>();
            _transitionImage.raycastTarget = false;
        }

        private void DestroyTransitionOverlay()
        {
            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
            _isExiting = false;

            if (_transitionGroup != null)
                Destroy(_transitionGroup.gameObject);
            _transitionGroup = null;
            _transitionImage = null;
        }

        private async UniTask PreloadStageSceneAsync(
            string sceneName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sceneName)
                || _preloadedStageRoots.ContainsKey(sceneName))
            {
                return;
            }

            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            bool loadedHere = !scene.IsValid() || !scene.isLoaded;
            if (loadedHere)
            {
                AsyncOperation loadOperation =
                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                        sceneName,
                        UnityEngine.SceneManagement.LoadSceneMode.Additive);
                if (loadOperation == null)
                {
                    Debug.LogError($"[CinematicStage] 사전 로드 요청에 실패했습니다: {sceneName}");
                    return;
                }

                await loadOperation.ToUniTask(cancellationToken: cancellationToken);
                scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError($"[CinematicStage] 사전 로드 씬을 찾을 수 없습니다: {sceneName}");
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            CinematicStageRoot stageRoot = null;
            for (int i = 0; i < roots.Length; i++)
            {
                CinematicStageRoot candidate = roots[i].GetComponent<CinematicStageRoot>();
                if (candidate == null)
                    continue;

                if (stageRoot != null)
                {
                    stageRoot = null;
                    break;
                }
                stageRoot = candidate;
            }

            if (stageRoot == null || roots.Length != 1)
            {
                Debug.LogError(
                    $"[CinematicStage] '{sceneName}' 씬은 CinematicStageRoot 하나만 루트로 가져야 합니다.");
                if (loadedHere)
                    await UnloadSceneAsync(scene, cancellationToken);
                return;
            }

            GameObject root = stageRoot.gameObject;
            root.SetActive(false);
            DontDestroyOnLoad(root);
            _preloadedStageRoots.Add(sceneName, root);

            if (loadedHere)
                await UnloadSceneAsync(scene, cancellationToken);
        }

        private static async UniTask UnloadSceneAsync(
            UnityEngine.SceneManagement.Scene scene,
            CancellationToken cancellationToken)
        {
            AsyncOperation unloadOperation =
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            if (unloadOperation != null)
                await unloadOperation.ToUniTask(cancellationToken: cancellationToken);
        }
    }

    internal sealed class CinematicStageInstance
    {
        private readonly List<CinematicPoseMirror> _mirrors = new();
        private readonly List<GameObject> _clones = new();
        private readonly List<GameObject> _transients = new();
        private readonly List<ActorPresentation> _hiddenPresentations = new();
        private readonly List<AnimatorState> _animatorStates = new();
        private readonly List<SkinnedRendererState> _skinnedRendererStates = new();
        private readonly List<LayerState> _stageLayerStates = new();
        private readonly CinematicStageLightingContext _lightingContext = new();

        private GameObject _stageRoot;
        private Transform _actorRoot;
        private bool _ownsStageRoot;
        private bool _stageRootWasActive;
        private Vector3 _originalStagePosition;
        private Quaternion _originalStageRotation;
        private Camera _sourceCamera;
        private bool _sourceCameraWasEnabled;
        private Camera _stageCamera;
        private bool _ownsStageCamera;
        private bool _stageCameraWasEnabled;
        private int _stageCullingMask;
        private bool _presentationFrozen;

        private CinematicStageInstance(
            CinematicStageRequest request,
            CinematicStageTicket ticket)
        {
            Definition = request.Stage;
            Owner = request.Owner;
            Ticket = ticket;
            StartedAtUnscaledTime = Time.unscaledTime;
        }

        public CinematicStageSO Definition { get; }
        public UnityEngine.Object Owner { get; }
        public CinematicStageTicket Ticket { get; }
        public float StartedAtUnscaledTime { get; }
        public Matrix4x4 StageTransform { get; private set; }

        public static bool TryCreate(
            CinematicStageRequest request,
            CinematicStageTicket ticket,
            CinematicCloneFactory cloneFactory,
            GameObject preloadedStageRoot,
            int actorLayer,
            out CinematicStageInstance instance,
            out string error)
        {
            instance = new CinematicStageInstance(request, ticket);
            try
            {
                if (!instance.Build(
                        request,
                        cloneFactory,
                        preloadedStageRoot,
                        actorLayer,
                        out error))
                {
                    instance.Dispose(cloneFactory);
                    instance = null;
                    return false;
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                instance.Dispose(cloneFactory);
                instance = null;
                return false;
            }
        }

        public void LateUpdate()
        {
            if (_presentationFrozen)
                return;

            for (int i = 0; i < _mirrors.Count; i++)
                _mirrors[i].Apply(StageTransform);

            if (_sourceCamera == null || _stageCamera == null)
                return;

            _stageCamera.CopyFrom(_sourceCamera);
            _stageCamera.enabled = true;
            _stageCamera.cullingMask = _stageCullingMask;
            _stageCamera.transform.SetPositionAndRotation(
                StageTransform.MultiplyPoint3x4(_sourceCamera.transform.position),
                StageTransform.rotation * _sourceCamera.transform.rotation);

            UniversalAdditionalCameraData cameraData =
                _stageCamera.GetUniversalAdditionalCameraData();
            cameraData.volumeLayerMask = _stageCullingMask;
            cameraData.renderPostProcessing = Definition.stageVolumeProfile != null;
        }

        public void FreezePresentation()
        {
            if (_presentationFrozen)
                return;

            // Sequence 종료 후 원본 Animator와 카메라가 즉시 복귀하더라도,
            // 화면이 완전히 가려질 때까지 Stage의 마지막 Ultimate 프레임을 유지한다.
            LateUpdate();
            _presentationFrozen = true;
        }

        public void RegisterTransient(GameObject instance)
        {
            if (instance != null && !_transients.Contains(instance))
                _transients.Add(instance);
        }

        public bool TryResolvePresentationTransform(
            Transform source,
            out Transform presentation)
        {
            presentation = null;
            if (source == null)
                return false;

            for (int i = 0; i < _mirrors.Count; i++)
            {
                if (_mirrors[i].TryResolveCloneTransform(
                        source,
                        out presentation))
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose(CinematicCloneFactory cloneFactory)
        {
            RestoreCamera();
            _lightingContext.Restore();

            for (int i = _hiddenPresentations.Count - 1; i >= 0; i--)
            {
                if (_hiddenPresentations[i] != null)
                    _hiddenPresentations[i].Show();
            }
            _hiddenPresentations.Clear();

            for (int i = 0; i < _animatorStates.Count; i++)
                _animatorStates[i].Restore();
            _animatorStates.Clear();

            for (int i = 0; i < _skinnedRendererStates.Count; i++)
                _skinnedRendererStates[i].Restore();
            _skinnedRendererStates.Clear();

            for (int i = _transients.Count - 1; i >= 0; i--)
            {
                if (_transients[i] != null)
                    UnityEngine.Object.Destroy(_transients[i]);
            }
            _transients.Clear();

            _mirrors.Clear();
            for (int i = _clones.Count - 1; i >= 0; i--)
                cloneFactory.Release(_clones[i]);
            _clones.Clear();

            if (_ownsStageRoot && _stageRoot != null)
                UnityEngine.Object.Destroy(_stageRoot);
            else if (_stageRoot != null)
            {
                for (int i = 0; i < _stageLayerStates.Count; i++)
                    _stageLayerStates[i].Restore();
                _stageRoot.transform.SetPositionAndRotation(
                    _originalStagePosition,
                    _originalStageRotation);
                _stageRoot.SetActive(_stageRootWasActive);
            }
            _stageLayerStates.Clear();

            _stageRoot = null;
            _actorRoot = null;
        }

        private bool Build(
            CinematicStageRequest request,
            CinematicCloneFactory cloneFactory,
            GameObject preloadedStageRoot,
            int actorLayer,
            out string error)
        {
            ResolveStageRoot(
                preloadedStageRoot,
                out _stageRoot,
                out _ownsStageRoot);
            if (_stageRoot == null)
            {
                error = "무대 루트를 만들 수 없습니다.";
                return false;
            }

            _stageRootWasActive = _stageRoot.activeSelf;
            _stageRoot.SetActive(true);
            _originalStagePosition = _stageRoot.transform.position;
            _originalStageRotation = _stageRoot.transform.rotation;
            PositionStageRoot(request);
            ApplyStageEnvironmentLayer();
            RefreshStageTerrains();

            CinematicStageRoot binding = _stageRoot.GetComponent<CinematicStageRoot>();
            _actorRoot = binding != null ? binding.ActorRoot : _stageRoot.transform;
            Transform casterAnchor = binding != null ? binding.CasterAnchor : _stageRoot.transform;
            StageTransform = Matrix4x4.TRS(
                                 casterAnchor.position,
                                 casterAnchor.rotation,
                                 Vector3.one)
                             * Matrix4x4.TRS(
                                 request.CasterModelRoot.position,
                                 request.CasterModelRoot.rotation,
                                 Vector3.one).inverse;

            GameObject casterClone = cloneFactory.Acquire(
                request.CasterModelRoot,
                _actorRoot,
                actorLayer);
            if (casterClone == null)
            {
                error = "시전자 클론 생성에 실패했습니다.";
                return false;
            }
            AddMirror(request.CasterModelRoot, casterClone);

            bool targetRepresentationReady = BuildTargetRepresentation(
                request,
                cloneFactory,
                actorLayer);
            if (!targetRepresentationReady
                && Definition.tier == CinematicStageTier.BothClones
                && Definition.fallback == CinematicStageFallback.Abort)
            {
                error = "타깃 표현 생성에 실패했습니다.";
                return false;
            }

            PrepareSourceAnimation(request.Caster);
            if (request.Target != null && Definition.tier == CinematicStageTier.BothClones)
                PrepareSourceAnimation(request.Target);

            if (Definition.hideSourceRenderers)
            {
                HidePresentation(request.Caster);
                if (Definition.tier == CinematicStageTier.BothClones && request.Target != null)
                    HidePresentation(request.Target);
            }

            _stageCullingMask = Definition.stageCullingMask.value != 0
                ? Definition.stageCullingMask.value
                : ResolveDefaultStageMask();
            if (!PrepareCamera())
            {
                error = "게임플레이 카메라 또는 전용 카메라를 준비하지 못했습니다.";
                return false;
            }

            _lightingContext.Apply(_stageRoot.transform, _stageCullingMask, Definition);
            LateUpdate();
            error = string.Empty;
            return true;
        }

        private void ResolveStageRoot(
            GameObject preloadedStageRoot,
            out GameObject root,
            out bool ownsRoot)
        {
            root = preloadedStageRoot;
            ownsRoot = false;

            if (root != null)
                return;

            if (Definition.stagePrefab != null)
                root = UnityEngine.Object.Instantiate(Definition.stagePrefab);
            else if (string.IsNullOrWhiteSpace(Definition.stageSceneName))
                root = new GameObject($"Cinematic Stage - {Definition.name}");
            ownsRoot = root != null;
        }

        private void PositionStageRoot(CinematicStageRequest request)
        {
            // Unity Terrain은 부모 Transform의 회전을 지원하지 않는다.
            // Terrain 무대는 저작된 로컬 배치를 유지하고 위치만 격리 공간으로 옮긴다.
            bool hasTerrain = _stageRoot.GetComponentInChildren<Terrain>(true) != null;
            if (hasTerrain)
            {
                _stageRoot.transform.SetPositionAndRotation(
                    request.Caster.transform.position + Definition.anchorOffset,
                    Quaternion.identity);
                return;
            }

            Vector3 forward = request.Caster.transform.forward;
            if (Definition.alignStageYawToTarget && request.Target != null)
            {
                Vector3 toTarget = request.Target.transform.position
                                   - request.Caster.transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                    forward = toTarget.normalized;
            }

            forward.y = 0f;
            Quaternion rotation = forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward.normalized, Vector3.up)
                : Quaternion.identity;
            _stageRoot.transform.SetPositionAndRotation(
                request.Caster.transform.position + Definition.anchorOffset,
                rotation);
        }

        private void ApplyStageEnvironmentLayer()
        {
            int stageLayer = LayerMask.NameToLayer("UltimateStage");
            if (stageLayer < 0 || _stageRoot == null)
                return;

            Transform[] transforms = _stageRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform current = transforms[i];
                _stageLayerStates.Add(new LayerState(current.gameObject, current.gameObject.layer));
                current.gameObject.layer = stageLayer;
            }
        }

        private void RefreshStageTerrains()
        {
            if (_stageRoot == null)
                return;

            Terrain[] terrains = _stageRoot.GetComponentsInChildren<Terrain>(true);
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null)
                    continue;

                bool wasEnabled = terrain.enabled;
                if (wasEnabled)
                    terrain.enabled = false;

                terrain.Flush();
                terrain.enabled = wasEnabled;
            }
        }

        private bool BuildTargetRepresentation(
            CinematicStageRequest request,
            CinematicCloneFactory cloneFactory,
            int actorLayer)
        {
            if (request.Target == null
                || Definition.targetMode == CinematicTargetRepresentation.None
                || Definition.targetMode == CinematicTargetRepresentation.VfxOnly)
            {
                return true;
            }

            if (Definition.tier == CinematicStageTier.BothClones
                && Definition.targetMode == CinematicTargetRepresentation.Clone
                && request.TargetModelRoot != null)
            {
                GameObject targetClone = cloneFactory.Acquire(
                    request.TargetModelRoot,
                    _actorRoot,
                    actorLayer);
                if (targetClone == null)
                    return false;
                AddMirror(request.TargetModelRoot, targetClone);
                return true;
            }

            bool useSilhouette = Definition.targetMode is
                CinematicTargetRepresentation.Silhouette
                or CinematicTargetRepresentation.DummyRig;
            if (!useSilhouette || Definition.silhouettePrefab == null)
                return Definition.fallback != CinematicStageFallback.Abort;

            Bounds bounds = CalculateBounds(request.Target);
            UltimateTargetSize size = Definition.ClassifyTarget(bounds.size.y);
            Vector3 position = StageTransform.MultiplyPoint3x4(request.Target.transform.position)
                               + Definition.sizeAnchors.GetOffset(size);
            Quaternion rotation = StageTransform.rotation * request.Target.transform.rotation;
            GameObject silhouette = UnityEngine.Object.Instantiate(
                Definition.silhouettePrefab,
                position,
                rotation,
                _actorRoot);
            SetLayerRecursively(silhouette.transform, actorLayer);
            _transients.Add(silhouette);
            return true;
        }

        private void AddMirror(Transform sourceRoot, GameObject clone)
        {
            var mirror = new CinematicPoseMirror(sourceRoot, clone.transform);
            _clones.Add(clone);
            _mirrors.Add(mirror);
            mirror.Apply(StageTransform);
        }

        private void PrepareSourceAnimation(GameObject source)
        {
            if (source == null)
                return;

            Animator[] animators = source.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                _animatorStates.Add(new AnimatorState(animator, animator.cullingMode));
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            SkinnedMeshRenderer[] renderers =
                source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                _skinnedRendererStates.Add(
                    new SkinnedRendererState(renderer, renderer.updateWhenOffscreen));
                renderer.updateWhenOffscreen = true;
            }
        }

        private void HidePresentation(GameObject actor)
        {
            if (actor == null)
                return;

            ActorPresentation presentation = actor.GetComponent<ActorPresentation>();
            if (presentation == null)
                presentation = actor.AddComponent<ActorPresentation>();
            presentation.Hide();
            _hiddenPresentations.Add(presentation);
        }

        private bool PrepareCamera()
        {
            _sourceCamera = CameraManager.Instance?.GetMainCamera();
            if (_sourceCamera == null)
                _sourceCamera = Camera.main;
            if (_sourceCamera == null)
                return false;

            _sourceCameraWasEnabled = _sourceCamera.enabled;
            _stageCamera = _stageRoot.GetComponentInChildren<Camera>(true);
            if (_stageCamera == null)
            {
                var cameraObject = new GameObject("Ultimate Camera");
                cameraObject.transform.SetParent(_stageRoot.transform, false);
                _stageCamera = cameraObject.AddComponent<Camera>();
                _ownsStageCamera = true;
            }
            else
            {
                _stageCameraWasEnabled = _stageCamera.enabled;
            }

            _sourceCamera.enabled = false;
            _stageCamera.enabled = true;
            CameraManager.Instance?.RegisterShakeCamera(_stageCamera);
            return true;
        }

        private void RestoreCamera()
        {
            if (_stageCamera != null)
            {
                CameraManager.Instance?.UnregisterShakeCamera(_stageCamera);
                if (_ownsStageCamera)
                    UnityEngine.Object.Destroy(_stageCamera.gameObject);
                else
                    _stageCamera.enabled = _stageCameraWasEnabled;
            }

            if (_sourceCamera != null)
                _sourceCamera.enabled = _sourceCameraWasEnabled;

            _stageCamera = null;
            _sourceCamera = null;
        }

        private static Bounds CalculateBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(target.transform.position, Vector3.one * 2f);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static int ResolveDefaultStageMask()
        {
            int mask = 0;
            AddLayer("UltimateStage", ref mask);
            AddLayer("UltimateActor", ref mask);
            AddLayer("UltimateVFX", ref mask);
            return mask;
        }

        private static void AddLayer(string layerName, ref int mask)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                mask |= 1 << layer;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null)
                return;
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }

        private readonly struct AnimatorState
        {
            private readonly Animator _animator;
            private readonly AnimatorCullingMode _cullingMode;

            public AnimatorState(Animator animator, AnimatorCullingMode cullingMode)
            {
                _animator = animator;
                _cullingMode = cullingMode;
            }

            public void Restore()
            {
                if (_animator != null)
                    _animator.cullingMode = _cullingMode;
            }
        }

        private readonly struct SkinnedRendererState
        {
            private readonly SkinnedMeshRenderer _renderer;
            private readonly bool _updateWhenOffscreen;

            public SkinnedRendererState(
                SkinnedMeshRenderer renderer,
                bool updateWhenOffscreen)
            {
                _renderer = renderer;
                _updateWhenOffscreen = updateWhenOffscreen;
            }

            public void Restore()
            {
                if (_renderer != null)
                    _renderer.updateWhenOffscreen = _updateWhenOffscreen;
            }
        }

        private readonly struct LayerState
        {
            private readonly GameObject _gameObject;
            private readonly int _layer;

            public LayerState(GameObject gameObject, int layer)
            {
                _gameObject = gameObject;
                _layer = layer;
            }

            public void Restore()
            {
                if (_gameObject != null)
                    _gameObject.layer = _layer;
            }
        }
    }

    internal sealed class CinematicStageLightingContext
    {
        private readonly List<LightState> _worldLights = new();
        private WorldLightingController _worldLighting;
        private bool _worldLightingWasEnabled;
        private GameObject _volumeObject;

        public void Apply(
            Transform stageRoot,
            int stageMask,
            CinematicStageSO definition)
        {
            _worldLighting = UnityEngine.Object.FindFirstObjectByType<WorldLightingController>();
            if (_worldLighting != null)
            {
                _worldLightingWasEnabled = _worldLighting.enabled;
                _worldLighting.enabled = false;
            }

            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null)
                    continue;

                _worldLights.Add(new LightState(light, light.cullingMask));
                if (stageRoot != null && light.transform.IsChildOf(stageRoot))
                {
                    light.cullingMask = stageMask;
                    continue;
                }

                light.cullingMask &= ~stageMask;
            }

            if (definition.stageVolumeProfile == null)
                return;

            int volumeLayer = LayerMask.NameToLayer("UltimateStage");
            _volumeObject = new GameObject("Cinematic Stage Volume");
            _volumeObject.layer = volumeLayer >= 0 ? volumeLayer : 0;
            _volumeObject.transform.SetParent(stageRoot, false);
            Volume volume = _volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.sharedProfile = definition.stageVolumeProfile;
        }

        public void Restore()
        {
            for (int i = 0; i < _worldLights.Count; i++)
                _worldLights[i].Restore();
            _worldLights.Clear();

            if (_worldLighting != null)
                _worldLighting.enabled = _worldLightingWasEnabled;
            _worldLighting = null;

            if (_volumeObject != null)
                UnityEngine.Object.Destroy(_volumeObject);
            _volumeObject = null;
        }

        private readonly struct LightState
        {
            private readonly Light _light;
            private readonly int _mask;

            public LightState(Light light, int mask)
            {
                _light = light;
                _mask = mask;
            }

            public void Restore()
            {
                if (_light != null)
                    _light.cullingMask = _mask;
            }
        }
    }
}
