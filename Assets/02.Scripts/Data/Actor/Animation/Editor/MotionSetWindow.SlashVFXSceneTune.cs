using System;
using System.Linq;
using FX;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public partial class MotionSetEditorWindow
    {
        const string SlashVfxPreviewName = "[Preview] SlashVFX Scene Tune";

        enum SlashVfxGizmoMode
        {
            Active,
            Blade,
            World,
            Both
        }

        static readonly GUIContent[] SlashVfxGizmoModeLabels =
        {
            new GUIContent("Active", "현재 이벤트의 space 설정에 맞는 기준 축을 표시합니다."),
            new GUIContent("Blade", "Blade Base/Tip에서 계산한 칼날 기준 축과 오프셋을 표시합니다."),
            new GUIContent("World", "Actor Root 회전 기준 축과 오프셋을 표시합니다."),
            new GUIContent("Both", "Blade 기준과 World/Actor 기준을 함께 표시합니다."),
        };

        static readonly Color SlashVfxBladeColor = new Color(1f, 0.82f, 0.18f, 0.95f);
        static readonly Color SlashVfxWorldColor = new Color(1f, 0.25f, 0.9f, 0.9f);
        static readonly Color SlashVfxSpawnColor = Color.cyan;

        bool _slashVfxSceneTuneEnabled;
        SlashVFXEvent _slashVfxSceneTuneEvent;
        GameObject _slashVfxPreviewObject;
        SlashVFXEvent _slashVfxPreviewEvent;
        GameObject _slashVfxPreviewPrefab;
        float _slashVfxPreviewAppliedScale = 1f;
        SlashVfxSceneTuneGizmoOverlay _slashVfxGameViewGizmoOverlay;
        float _slashVfxPreviewTime = 0.25f;
        SlashVfxGizmoMode _slashVfxGizmoMode = SlashVfxGizmoMode.Both;
        string _slashVfxSceneTuneStatus = "";

        void DrawSlashVfxSceneTunePanel(MotionSet currentSet, SlashVFXEvent slashEvent)
        {
            if (currentSet == null || slashEvent == null)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Slash VFX Scene Tune", EditorStyles.boldLabel);

                float globalStart = GetSelectedEventGlobalStartTime(currentSet, slashEvent);
                string targetLabel = _targetActor != null ? _targetActor.name : "대상 액터 없음";
                EditorGUILayout.LabelField(
                    $"Target: {targetLabel} / Event: {globalStart:0.###}s",
                    EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool nextEnabled = GUILayout.Toggle(
                        _slashVfxSceneTuneEnabled,
                        new GUIContent("Scene Tune", "Scene View에서 선택 SlashVFX의 위치/회전 핸들을 표시합니다."),
                        EditorStyles.miniButton,
                        GUILayout.Width(96));
                    if (nextEnabled != _slashVfxSceneTuneEnabled)
                    {
                        _slashVfxSceneTuneEnabled = nextEnabled;
                        if (nextEnabled)
                            _slashVfxSceneTuneEvent = slashEvent;
                        SceneView.RepaintAll();
                    }

                    using (new EditorGUI.DisabledScope(_targetActor == null))
                    {
                        if (GUILayout.Button("이벤트 프레임으로", GUILayout.Width(110)))
                        {
                            _slashVfxSceneTuneEvent = slashEvent;
                            RequestSeekToSelectedSlashVfxFrame(currentSet, slashEvent);
                        }

                        if (GUILayout.Button("스폰+이펙트 보기", GUILayout.Width(112)))
                        {
                            _slashVfxSceneTuneEvent = slashEvent;
                            RequestFrameSelectedSlashVfx(currentSet, slashEvent);
                        }
                    }

                    if (GUILayout.Button("프리뷰 정리", GUILayout.Width(78)))
                        DestroySlashVfxPreview();

                    using (new EditorGUI.DisabledScope(slashEvent.overrideSpawnerTransform))
                    {
                        if (GUILayout.Button("오버라이드 켜기", GUILayout.Width(96)))
                        {
                            _slashVfxSceneTuneEvent = slashEvent;
                            RecordSelectedSlashVfxUndo("Enable SlashVFX Override");
                            slashEvent.overrideSpawnerTransform = true;
                            MarkSelectedSlashVfxDirty();
                        }
                    }

                    if (GUILayout.Button("프리셋 저장", GUILayout.Width(86)))
                        SaveSlashVfxPreset(slashEvent);

                    if (GUILayout.Button("프리셋 적용", GUILayout.Width(86)))
                    {
                        _slashVfxSceneTuneEvent = slashEvent;
                        ShowApplySlashVfxPresetMenu(slashEvent);
                    }
                }

                EditorGUILayout.LabelField(
                    "위치 핸들: Blade 기준 오프셋 / 회전 핸들: Blade 또는 Actor Root 기준 Rotation Offset",
                    EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Gizmo", GUILayout.Width(44));
                    EditorGUI.BeginChangeCheck();
                    _slashVfxGizmoMode = (SlashVfxGizmoMode)GUILayout.Toolbar(
                        (int)_slashVfxGizmoMode,
                        SlashVfxGizmoModeLabels,
                        EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                        SceneView.RepaintAll();
                }

                EditorGUI.BeginChangeCheck();
                float nextPreviewTime = Mathf.Max(0f, EditorGUILayout.FloatField(
                    new GUIContent("Preview Time", "파티클을 이 시간만큼 진행시켜 첫 프레임에서 너무 안 보이는 VFX도 확인합니다."),
                    _slashVfxPreviewTime));
                if (EditorGUI.EndChangeCheck())
                {
                    _slashVfxPreviewTime = nextPreviewTime;
                    ResampleSlashVfxPreviewParticles();
                    SceneView.RepaintAll();
                }

                if (!string.IsNullOrEmpty(_slashVfxSceneTuneStatus))
                    EditorGUILayout.HelpBox(_slashVfxSceneTuneStatus, MessageType.None);
            }
        }

        void DrawSlashVfxSceneTuneHandle(SceneView sceneView)
        {
            if (!_slashVfxSceneTuneEnabled)
                return;

            MotionSet currentSet = GetCurrentMotionSet();
            if (currentSet == null || _drawer == null)
                return;

            SlashVFXEvent slashEvent = ResolveActiveSlashVfxTuneEvent(currentSet);
            if (slashEvent == null)
                return;

            if (_targetActor == null)
            {
                Handles.Label(Vector3.zero, "SlashVFX Scene Tune: 대상 액터를 지정하세요.");
                return;
            }

            if (!TryResolveSlashVfxContext(slashEvent, out Transform bladeBase, out Transform bladeTip, out GameObject prefab))
            {
                Handles.Label(
                    _targetActor.transform.position + Vector3.up * 2f,
                    "SlashVFX Scene Tune: Blade Base/Tip 또는 VFX Prefab을 찾지 못했습니다.");
                return;
            }

            if (!TryGetBladePose(bladeBase, bladeTip, _targetActor.transform, out Vector3 center, out Quaternion bladeRotation))
                return;

            Quaternion rotationBase = slashEvent.rotationSpace == SlashVFXRotationSpace.World
                ? _targetActor.transform.rotation
                : bladeRotation;

            // 위치 핸들의 기저는 ResolveSlashVfxSpawnPosition / 런타임 스폰 기저와 반드시 일치해야 한다.
            // World 모드는 actorRoot, Blade 모드는 칼날 기준. (기저가 어긋나면 드래그 즉시 오프셋이 튄다.)
            Quaternion positionBase = slashEvent.positionSpace == SlashVFXPositionSpace.World
                ? _targetActor.transform.rotation
                : bladeRotation;

            Vector3 spawnPosition = ResolveSlashVfxSpawnPosition(center, bladeRotation, _targetActor.transform.rotation, slashEvent);
            Quaternion vfxRotation = rotationBase * Quaternion.Euler(slashEvent.rotationOffset);

            DrawSlashVfxGizmos(bladeBase, bladeTip, center, spawnPosition, bladeRotation, _targetActor.transform.rotation, rotationBase, vfxRotation, slashEvent, prefab);

            if (!slashEvent.overrideSpawnerTransform)
            {
                Handles.Label(
                    spawnPosition + Vector3.up * HandleUtility.GetHandleSize(spawnPosition) * 0.2f,
                    "Spawner Transform 사용 중입니다. 애니메이션 에디터에서 오버라이드를 켜면 이벤트 값을 직접 튜닝합니다.");
                return;
            }

            EditorGUI.BeginChangeCheck();
            Vector3 newSpawnPosition = Handles.PositionHandle(spawnPosition, positionBase);
            if (EditorGUI.EndChangeCheck())
            {
                RecordSelectedSlashVfxUndo("Move SlashVFX Offset");
                slashEvent.positionOffset = Quaternion.Inverse(positionBase) * (newSpawnPosition - center);
                MarkSelectedSlashVfxDirty();
            }

            EditorGUI.BeginChangeCheck();
            Quaternion newVfxRotation = Handles.RotationHandle(vfxRotation, spawnPosition);
            if (EditorGUI.EndChangeCheck())
            {
                RecordSelectedSlashVfxUndo("Rotate SlashVFX Offset");
                Quaternion localRotation = Quaternion.Inverse(rotationBase) * newVfxRotation;
                slashEvent.rotationOffset = NormalizeEuler(localRotation.eulerAngles);
                MarkSelectedSlashVfxDirty();
            }
        }

        void DrawSlashVfxGizmos(
            Transform bladeBase,
            Transform bladeTip,
            Vector3 center,
            Vector3 spawnPosition,
            Quaternion bladeRotation,
            Quaternion actorRootRotation,
            Quaternion rotationBase,
            Quaternion vfxRotation,
            SlashVFXEvent slashEvent,
            GameObject prefab)
        {
            float centerSize = HandleUtility.GetHandleSize(center);
            float spawnSize = HandleUtility.GetHandleSize(spawnPosition);

            using (new Handles.DrawingScope(SlashVfxBladeColor))
            {
                Handles.DrawAAPolyLine(4f, bladeBase.position, bladeTip.position);
                Handles.SphereHandleCap(0, bladeBase.position, Quaternion.identity, centerSize * 0.05f, EventType.Repaint);
                Handles.SphereHandleCap(0, bladeTip.position, Quaternion.identity, centerSize * 0.05f, EventType.Repaint);
                DrawColoredHandleLabel(bladeBase.position, "Blade Base", SlashVfxBladeColor);
                DrawColoredHandleLabel(bladeTip.position, "Blade Tip", SlashVfxBladeColor);
            }

            DrawSlashVfxSpaceGizmos(center, bladeRotation, actorRootRotation, slashEvent);

            using (new Handles.DrawingScope(SlashVfxSpawnColor))
            {
                Handles.DrawDottedLine(center, spawnPosition, 4f);
                Handles.SphereHandleCap(0, spawnPosition, Quaternion.identity, spawnSize * 0.07f, EventType.Repaint);
            }

            DrawBasis(spawnPosition, rotationBase, GetRotationBaseLabel(slashEvent), 0.36f, SlashVfxSpawnColor);
            DrawBasis(spawnPosition + Vector3.up * spawnSize * 0.12f, vfxRotation, "VFX Rotation", 0.32f, SlashVfxSpawnColor);

            string prefabName = prefab != null ? prefab.name : "(Prefab 없음)";
            DrawColoredHandleLabel(
                spawnPosition + Vector3.up * spawnSize * 0.32f,
                $"SlashVFX: {prefabName}\nPosition: {GetPositionBaseLabel(slashEvent)}\nRotation: {GetRotationBaseLabel(slashEvent)}",
                SlashVfxSpawnColor);
        }

        void DrawSlashVfxSpaceGizmos(Vector3 center, Quaternion bladeRotation, Quaternion actorRootRotation, SlashVFXEvent slashEvent)
        {
            GetSlashVfxSpaceVisibility(slashEvent, out bool showBlade, out bool showWorld);

            Vector3 bladeOffsetPosition = center + bladeRotation * slashEvent.positionOffset;
            Vector3 worldOffsetPosition = center + actorRootRotation * slashEvent.positionOffset;

            if (showBlade)
            {
                DrawOffsetSpaceGizmo(
                    center,
                    bladeOffsetPosition,
                    bladeRotation,
                    "Blade Offset Space",
                    SlashVfxBladeColor,
                    0.46f);
            }

            if (showWorld)
            {
                DrawOffsetSpaceGizmo(
                    center,
                    worldOffsetPosition,
                    actorRootRotation,
                    "World / Actor Offset Space",
                    SlashVfxWorldColor,
                    0.54f);
            }
        }

        static void DrawOffsetSpaceGizmo(Vector3 origin, Vector3 offsetPosition, Quaternion basis, string label, Color color, float size)
        {
            float handleSize = HandleUtility.GetHandleSize(origin);
            using (new Handles.DrawingScope(color))
            {
                Handles.DrawDottedLine(origin, offsetPosition, 3f);
                Handles.DrawWireDisc(offsetPosition, basis * Vector3.forward, handleSize * 0.08f);
                Handles.SphereHandleCap(0, offsetPosition, Quaternion.identity, handleSize * 0.045f, EventType.Repaint);
                DrawColoredHandleLabel(
                    offsetPosition + basis * Vector3.up * handleSize * 0.12f,
                    $"{label}\nOffset Target",
                    color);
            }

            DrawBasis(origin, basis, $"{label}\nRGB Axis", size, color);
        }

        void PublishSlashVfxSceneTuneGameViewGizmo()
        {
            if (!_slashVfxSceneTuneEnabled || _targetActor == null)
            {
                SlashVfxSceneTuneGizmoOverlay.Clear();
                return;
            }

            MotionSet currentSet = GetCurrentMotionSet();
            SlashVFXEvent slashEvent = ResolveActiveSlashVfxTuneEvent(currentSet);
            if (slashEvent == null ||
                !TryResolveSlashVfxContext(slashEvent, out Transform bladeBase, out Transform bladeTip, out _) ||
                !TryGetBladePose(bladeBase, bladeTip, _targetActor.transform, out Vector3 center, out Quaternion bladeRotation))
            {
                SlashVfxSceneTuneGizmoOverlay.Clear();
                return;
            }

            EnsureSlashVfxGameViewGizmoOverlay();

            Quaternion actorRootRotation = _targetActor.transform.rotation;
            Quaternion rotationBase = slashEvent.rotationSpace == SlashVFXRotationSpace.World
                ? actorRootRotation
                : bladeRotation;
            Vector3 spawnPosition = ResolveSlashVfxSpawnPosition(center, bladeRotation, actorRootRotation, slashEvent);
            Quaternion vfxRotation = rotationBase * Quaternion.Euler(slashEvent.rotationOffset);
            Vector3 bladeOffsetPosition = center + bladeRotation * slashEvent.positionOffset;
            Vector3 worldOffsetPosition = center + actorRootRotation * slashEvent.positionOffset;
            GetSlashVfxSpaceVisibility(slashEvent, out bool showBlade, out bool showWorld);

            SlashVfxSceneTuneGizmoOverlay.Publish(
                _targetActor,
                true,
                showBlade,
                showWorld,
                bladeBase.position,
                bladeTip.position,
                center,
                spawnPosition,
                bladeOffsetPosition,
                worldOffsetPosition,
                bladeRotation,
                actorRootRotation,
                rotationBase,
                vfxRotation,
                GetRotationBaseLabel(slashEvent));
        }

        void ClearSlashVfxSceneTuneGameViewGizmo()
        {
            SlashVfxSceneTuneGizmoOverlay.Clear();
            DestroySlashVfxGameViewGizmoOverlayComponent();
        }

        void EnsureSlashVfxGameViewGizmoOverlay()
        {
            if (_targetActor == null)
                return;

            if (_slashVfxGameViewGizmoOverlay != null &&
                _slashVfxGameViewGizmoOverlay.gameObject == _targetActor)
                return;

            DestroySlashVfxGameViewGizmoOverlayComponent();
            _slashVfxGameViewGizmoOverlay = _targetActor.GetComponent<SlashVfxSceneTuneGizmoOverlay>();
            if (_slashVfxGameViewGizmoOverlay == null)
                _slashVfxGameViewGizmoOverlay = _targetActor.AddComponent<SlashVfxSceneTuneGizmoOverlay>();

            _slashVfxGameViewGizmoOverlay.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }

        void DestroySlashVfxGameViewGizmoOverlayComponent()
        {
            if (_slashVfxGameViewGizmoOverlay == null)
                return;

            if (Application.isPlaying)
                Destroy(_slashVfxGameViewGizmoOverlay);
            else
                DestroyImmediate(_slashVfxGameViewGizmoOverlay);

            _slashVfxGameViewGizmoOverlay = null;
        }

        void GetSlashVfxSpaceVisibility(SlashVFXEvent slashEvent, out bool showBlade, out bool showWorld)
        {
            showBlade = _slashVfxGizmoMode == SlashVfxGizmoMode.Both
                        || _slashVfxGizmoMode == SlashVfxGizmoMode.Blade
                        || (_slashVfxGizmoMode == SlashVfxGizmoMode.Active &&
                            (slashEvent.positionSpace == SlashVFXPositionSpace.Blade ||
                             slashEvent.rotationSpace == SlashVFXRotationSpace.BladeOffset));

            showWorld = _slashVfxGizmoMode == SlashVfxGizmoMode.Both
                        || _slashVfxGizmoMode == SlashVfxGizmoMode.World
                        || (_slashVfxGizmoMode == SlashVfxGizmoMode.Active &&
                            (slashEvent.positionSpace == SlashVFXPositionSpace.World ||
                             slashEvent.rotationSpace == SlashVFXRotationSpace.World));

            if (_slashVfxGizmoMode == SlashVfxGizmoMode.Active && !showBlade && !showWorld)
                showBlade = true;
        }

        static void DrawBasis(Vector3 origin, Quaternion rotation, string label, float size, Color labelColor)
        {
            float handleSize = HandleUtility.GetHandleSize(origin) * size;
            Handles.color = Color.red;
            Handles.DrawLine(origin, origin + rotation * Vector3.right * handleSize);
            Handles.color = Color.green;
            Handles.DrawLine(origin, origin + rotation * Vector3.up * handleSize);
            Handles.color = Color.blue;
            Handles.DrawLine(origin, origin + rotation * Vector3.forward * handleSize);
            DrawColoredHandleLabel(origin + rotation * Vector3.up * handleSize, label, labelColor);
        }

        static void DrawColoredHandleLabel(Vector3 position, string text, Color color)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = color;
            Handles.Label(position, text, style);
        }

        bool TryResolveSlashVfxContext(SlashVFXEvent slashEvent, out Transform bladeBase, out Transform bladeTip, out GameObject prefab)
        {
            bladeBase = null;
            bladeTip = null;
            prefab = slashEvent.vfxPrefab;

            if (_targetActor == null)
                return false;

            WeaponSlashVfxSpawner spawner = ResolveSlashVfxSpawner(slashEvent);
            if (spawner != null)
            {
                bladeBase = spawner.BladeBase;
                bladeTip = spawner.BladeTip;
                if (prefab == null)
                    prefab = spawner.SlashVfxPrefab;
            }

            if (bladeBase == null || bladeTip == null)
            {
                Transform searchRoot = ResolveSearchRoot(_targetActor.transform, slashEvent.weaponRootName);
                bladeBase = FindTransformByName(searchRoot, slashEvent.basePointName);
                bladeTip = FindTransformByName(searchRoot, slashEvent.tipPointName);
            }

            return bladeBase != null && bladeTip != null && prefab != null;
        }

        WeaponSlashVfxSpawner ResolveSlashVfxSpawner(SlashVFXEvent slashEvent)
        {
            if (_targetActor == null)
                return null;

            if (!string.IsNullOrEmpty(slashEvent.spawnerObjectName))
            {
                Transform spawnerRoot = FindTransformByName(_targetActor.transform, slashEvent.spawnerObjectName);
                return SelectBestSpawner(spawnerRoot != null
                    ? spawnerRoot.GetComponentsInChildren<WeaponSlashVfxSpawner>(true)
                    : null);
            }

            if (!string.IsNullOrEmpty(slashEvent.weaponRootName))
            {
                Transform weaponRoot = FindTransformByName(_targetActor.transform, slashEvent.weaponRootName);
                if (weaponRoot != null)
                    return SelectBestSpawner(weaponRoot.GetComponentsInChildren<WeaponSlashVfxSpawner>(true));
            }

            return SelectBestSpawner(_targetActor.GetComponentsInChildren<WeaponSlashVfxSpawner>(true));
        }

        static WeaponSlashVfxSpawner SelectBestSpawner(WeaponSlashVfxSpawner[] candidates)
        {
            if (candidates == null || candidates.Length == 0)
                return null;

            return candidates.FirstOrDefault(spawner => spawner != null && spawner.isActiveAndEnabled && spawner.BladeBase != null && spawner.BladeTip != null)
                   ?? candidates.FirstOrDefault(spawner => spawner != null && spawner.BladeBase != null && spawner.BladeTip != null)
                   ?? candidates.FirstOrDefault(spawner => spawner != null && spawner.isActiveAndEnabled)
                   ?? candidates.FirstOrDefault(spawner => spawner != null);
        }

        static Transform ResolveSearchRoot(Transform target, string rootName)
        {
            if (target == null || string.IsNullOrEmpty(rootName))
                return target;

            return FindTransformByName(target, rootName) ?? target;
        }

        static Transform FindTransformByName(Transform parent, string transformName)
        {
            if (parent == null || string.IsNullOrEmpty(transformName))
                return null;

            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == transformName)
                    return child;
            }

            return null;
        }

        static bool TryGetBladePose(Transform bladeBase, Transform bladeTip, Transform upFallback, out Vector3 center, out Quaternion rotation)
        {
            center = default;
            rotation = default;

            if (bladeBase == null || bladeTip == null)
                return false;

            Vector3 bladeDirection = bladeTip.position - bladeBase.position;
            if (bladeDirection.sqrMagnitude < 0.0001f)
                return false;

            bladeDirection.Normalize();

            Vector3 upDirection = Vector3.ProjectOnPlane(bladeBase.up, bladeDirection);
            if (upDirection.sqrMagnitude < 0.0001f && upFallback != null)
                upDirection = Vector3.ProjectOnPlane(upFallback.up, bladeDirection);
            if (upDirection.sqrMagnitude < 0.0001f)
                upDirection = Vector3.up;

            upDirection.Normalize();
            center = Vector3.Lerp(bladeBase.position, bladeTip.position, 0.5f);
            rotation = Quaternion.LookRotation(bladeDirection, upDirection);
            return true;
        }

        void SeekToSelectedSlashVfxFrame(MotionSet currentSet, SlashVFXEvent slashEvent)
        {
            float globalStart = GetSelectedEventGlobalStartTime(currentSet, slashEvent);
            _playbackTime = Mathf.Clamp(globalStart, 0f, currentSet.TotalDuration);
            _drawer.cursorTime = _playbackTime;

            if (Application.isPlaying && _animancer != null)
            {
                SeekToTime(_playbackTime, executeEvents: false);
                _slashVfxSceneTuneStatus = $"이벤트 프레임 {_playbackTime:0.###}s로 이동했습니다.";
            }
            else
            {
                _slashVfxSceneTuneStatus = "커서만 이동했습니다. 실제 포즈 샘플링은 플레이 모드에서 대상 액터/Animancer가 있을 때 동작합니다.";
                Repaint();
            }

            SceneView.RepaintAll();
        }

        void RequestSeekToSelectedSlashVfxFrame(MotionSet currentSet, SlashVFXEvent slashEvent)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null || currentSet == null || slashEvent == null)
                    return;

                SeekToSelectedSlashVfxFrame(currentSet, slashEvent);
            };
        }

        void RequestFrameSelectedSlashVfx(MotionSet currentSet, SlashVFXEvent slashEvent)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null || currentSet == null || slashEvent == null)
                    return;

                FrameSelectedSlashVfx(currentSet, slashEvent);
            };
        }

        void FrameSelectedSlashVfx(MotionSet currentSet, SlashVFXEvent slashEvent)
        {
            SeekToSelectedSlashVfxFrame(currentSet, slashEvent);

            if (!TryResolveSlashVfxPreviewPose(
                    slashEvent,
                    out Transform bladeBase,
                    out Transform bladeTip,
                    out GameObject prefab,
                    out Vector3 spawnPosition,
                    out Quaternion rotation,
                    out float scale))
                return;

            if (_targetActor == null)
                return;

            Bounds bounds = new Bounds(spawnPosition, Vector3.one * 0.1f);
            bounds.Encapsulate(bladeBase.position);
            bounds.Encapsulate(bladeTip.position);

            SpawnSlashVfxPreview(prefab, spawnPosition, rotation, scale, slashEvent.attachToActor);

            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.Frame(bounds, false);
        }

        bool TryResolveSlashVfxPreviewPose(
            SlashVFXEvent slashEvent,
            out Transform bladeBase,
            out Transform bladeTip,
            out GameObject prefab,
            out Vector3 spawnPosition,
            out Quaternion rotation,
            out float scale)
        {
            spawnPosition = default;
            rotation = default;
            scale = 1f;

            bladeBase = null;
            bladeTip = null;
            prefab = slashEvent != null ? slashEvent.vfxPrefab : null;

            if (slashEvent == null || _targetActor == null)
                return false;

            WeaponSlashVfxSpawner spawner = ResolveSlashVfxSpawner(slashEvent);
            if (spawner != null)
            {
                bladeBase = spawner.BladeBase;
                bladeTip = spawner.BladeTip;
                if (prefab == null)
                    prefab = spawner.SlashVfxPrefab;
            }

            if (bladeBase == null || bladeTip == null)
            {
                Transform searchRoot = ResolveSearchRoot(_targetActor.transform, slashEvent.weaponRootName);
                bladeBase = FindTransformByName(searchRoot, slashEvent.basePointName);
                bladeTip = FindTransformByName(searchRoot, slashEvent.tipPointName);
            }

            if (bladeBase == null || bladeTip == null || prefab == null)
            {
                _slashVfxSceneTuneStatus = "프리뷰 실패: Blade Base/Tip 또는 VFX Prefab을 찾지 못했습니다.";
                return false;
            }

            Vector3 offset = slashEvent.overrideSpawnerTransform || spawner == null
                ? slashEvent.positionOffset
                : spawner.PositionOffset;
            Vector3 rotationOffset = slashEvent.overrideSpawnerTransform || spawner == null
                ? slashEvent.rotationOffset
                : spawner.RotationOffsetEuler;
            bool useWorldRotation = slashEvent.overrideSpawnerTransform && slashEvent.rotationSpace == SlashVFXRotationSpace.World;
            bool useWorldPosition = slashEvent.overrideSpawnerTransform && slashEvent.positionSpace == SlashVFXPositionSpace.World;
            scale = slashEvent.overrideSpawnerTransform || spawner == null
                ? slashEvent.scale
                : spawner.Scale;

            if (!WeaponSlashVfxSpawner.TryGetSpawnPose(
                    bladeBase,
                    bladeTip,
                    _targetActor.transform,
                    offset,
                    useWorldPosition,
                    rotationOffset,
                    useWorldRotation,
                    _targetActor.transform.rotation,
                    out spawnPosition,
                    out rotation))
            {
                _slashVfxSceneTuneStatus = "프리뷰 실패: Blade Base/Tip 방향이 유효하지 않습니다.";
                return false;
            }

            return true;
        }

        void SpawnSlashVfxPreview(GameObject prefab, Vector3 spawnPosition, Quaternion rotation, float scale, bool attachToActor)
        {
            DestroySlashVfxPreview();

            if (prefab == null)
                return;

            _slashVfxPreviewObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (_slashVfxPreviewObject == null)
                _slashVfxPreviewObject = Instantiate(prefab);

            _slashVfxPreviewEvent = _slashVfxSceneTuneEvent;
            _slashVfxPreviewPrefab = prefab;
            _slashVfxPreviewAppliedScale = scale;
            _slashVfxPreviewObject.name = SlashVfxPreviewName;
            _slashVfxPreviewObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            _slashVfxPreviewObject.SetActive(true);
            _slashVfxPreviewObject.transform.SetPositionAndRotation(spawnPosition, rotation);
            _slashVfxPreviewObject.transform.localScale *= scale;

            if (attachToActor && _targetActor != null)
                _slashVfxPreviewObject.transform.SetParent(_targetActor.transform, true);

            ResampleSlashVfxPreviewParticles();

            Selection.activeGameObject = _slashVfxPreviewObject;
            _slashVfxSceneTuneStatus = $"프리뷰 생성: {prefab.name} @ {_playbackTime:0.###}s";
            SceneView.RepaintAll();
        }

        void UpdateSlashVfxPreviewPose()
        {
            if (_slashVfxPreviewObject == null || _slashVfxPreviewEvent == null)
                return;

            if (!ContainsEvent(GetCurrentMotionSet(), _slashVfxPreviewEvent))
            {
                DestroySlashVfxPreview();
                return;
            }

            if (!TryResolveSlashVfxPreviewPose(
                    _slashVfxPreviewEvent,
                    out _,
                    out _,
                    out GameObject prefab,
                    out Vector3 spawnPosition,
                    out Quaternion rotation,
                    out float scale))
            {
                DestroySlashVfxPreview();
                return;
            }

            if (prefab != _slashVfxPreviewPrefab || !Mathf.Approximately(scale, _slashVfxPreviewAppliedScale))
            {
                SpawnSlashVfxPreview(prefab, spawnPosition, rotation, scale, _slashVfxPreviewEvent.attachToActor);
                return;
            }

            _slashVfxPreviewObject.transform.SetPositionAndRotation(spawnPosition, rotation);
            if (_slashVfxPreviewEvent.attachToActor && _targetActor != null && _slashVfxPreviewObject.transform.parent != _targetActor.transform)
                _slashVfxPreviewObject.transform.SetParent(_targetActor.transform, true);
            else if (!_slashVfxPreviewEvent.attachToActor && _slashVfxPreviewObject.transform.parent != null)
                _slashVfxPreviewObject.transform.SetParent(null, true);

        }

        void ResampleSlashVfxPreviewParticles()
        {
            if (_slashVfxPreviewObject == null)
                return;

            Transform[] previewTransforms = _slashVfxPreviewObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform previewTransform in previewTransforms)
            {
                if (previewTransform != null && !previewTransform.gameObject.activeSelf)
                    previewTransform.gameObject.SetActive(true);
            }

            ParticleSystem[] particleSystems = _slashVfxPreviewObject.GetComponentsInChildren<ParticleSystem>(true);
            Renderer[] renderers = _slashVfxPreviewObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer previewRenderer in renderers)
            {
                if (previewRenderer != null)
                    previewRenderer.enabled = true;
            }

            if (particleSystems.Length == 0)
            {
                _slashVfxSceneTuneStatus = "프리뷰 생성됨: ParticleSystem을 찾지 못했습니다.";
                return;
            }

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem == null)
                    continue;

                if (!particleSystem.gameObject.activeSelf)
                    particleSystem.gameObject.SetActive(true);

                particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Clear(false);
            }

            bool sampledAny = false;
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem == null || HasParentParticleSystem(particleSystem, _slashVfxPreviewObject.transform))
                    continue;

                if (_slashVfxPreviewTime > 0f)
                    particleSystem.Simulate(_slashVfxPreviewTime, true, true, false);
                particleSystem.Pause(true);
                sampledAny = true;
            }

            if (!sampledAny)
            {
                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    if (particleSystem == null)
                        continue;

                    if (_slashVfxPreviewTime > 0f)
                        particleSystem.Simulate(_slashVfxPreviewTime, true, true, false);
                    particleSystem.Pause(true);
                    sampledAny = true;
                    break;
                }
            }

            _slashVfxSceneTuneStatus = $"프리뷰 정지 샘플: {_slashVfxPreviewTime:0.###}s / ParticleSystem {particleSystems.Length}개";
            Repaint();
        }

        static bool HasParentParticleSystem(ParticleSystem particleSystem, Transform previewRoot)
        {
            Transform parent = particleSystem.transform.parent;
            while (parent != null && parent != previewRoot)
            {
                if (parent.GetComponent<ParticleSystem>() != null)
                    return true;
                parent = parent.parent;
            }

            return false;
        }

        void DestroySlashVfxPreview()
        {
            if (_slashVfxPreviewObject != null)
            {
                DestroySlashVfxPreviewObject(_slashVfxPreviewObject);
                _slashVfxPreviewObject = null;
            }

            _slashVfxPreviewEvent = null;
            _slashVfxPreviewPrefab = null;
            _slashVfxPreviewAppliedScale = 1f;

            GameObject[] previewObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go != null && go.name == SlashVfxPreviewName)
                .ToArray();

            foreach (GameObject previewObject in previewObjects)
                DestroySlashVfxPreviewObject(previewObject);
        }

        static void DestroySlashVfxPreviewObject(GameObject previewObject)
        {
            if (previewObject == null)
                return;

            if (Application.isPlaying)
                Destroy(previewObject);
            else
                DestroyImmediate(previewObject);
        }

        float GetSelectedEventGlobalStartTime(MotionSet currentSet, MotionEventBase evt)
        {
            if (currentSet == null || evt == null || _drawer == null)
                return 0f;

            if (_drawer.GetSelectedEvent(currentSet) == evt && _drawer.selectedEventIsSetEvent)
                return evt.startTime;

            float offset = 0f;
            if (currentSet.motions != null)
            {
                int count = _drawer.GetSelectedEvent(currentSet) == evt
                    ? Mathf.Clamp(_drawer.selectedEventMotionIndex, 0, currentSet.motions.Count)
                    : FindEventMotionIndex(currentSet, evt);
                for (int i = 0; i < count; i++)
                    offset += currentSet.motions[i]?.Duration ?? 0f;
            }

            return offset + evt.startTime;
        }

        SlashVFXEvent ResolveActiveSlashVfxTuneEvent(MotionSet currentSet)
        {
            if (_slashVfxSceneTuneEvent != null && ContainsEvent(currentSet, _slashVfxSceneTuneEvent))
                return _slashVfxSceneTuneEvent;

            if (_drawer?.GetSelectedEvent(currentSet) is SlashVFXEvent selected)
                return selected;

            return null;
        }

        static bool ContainsEvent(MotionSet currentSet, MotionEventBase evt)
        {
            if (currentSet == null || evt == null)
                return false;

            if (currentSet.globalEvents != null && currentSet.globalEvents.Contains(evt))
                return true;

            if (currentSet.motions == null)
                return false;

            foreach (var motion in currentSet.motions)
            {
                if (motion?.events != null && motion.events.Contains(evt))
                    return true;
            }

            return false;
        }

        static int FindEventMotionIndex(MotionSet currentSet, MotionEventBase evt)
        {
            if (currentSet?.motions == null || evt == null)
                return 0;

            for (int i = 0; i < currentSet.motions.Count; i++)
            {
                if (currentSet.motions[i]?.events != null && currentSet.motions[i].events.Contains(evt))
                    return i;
            }

            return 0;
        }

        void SaveSlashVfxPreset(SlashVFXEvent slashEvent)
        {
            var library = MotionEventPresetLibraryUtility.LoadOrCreate();
            if (library == null)
                return;

            string prefabName = slashEvent.vfxPrefab != null ? slashEvent.vfxPrefab.name : "SpawnerPrefab";
            var entry = MotionEventPresetEntry.FromEvents(
                $"SlashVFX {prefabName}",
                "애니메이션 에디터 SlashVFX Scene Tune에서 저장한 프리셋입니다.",
                "slash,vfx,scene-tune",
                new MotionEventBase[] { slashEvent });

            library.presets ??= new System.Collections.Generic.List<MotionEventPresetEntry>();
            library.presets.Add(entry);
            MotionEventPresetLibraryUtility.Save(library);

            _slashVfxSceneTuneStatus = $"프리셋 저장: {entry.displayName}";
        }

        void ShowApplySlashVfxPresetMenu(SlashVFXEvent targetEvent)
        {
            var library = MotionEventPresetLibraryUtility.Load();
            var menu = new GenericMenu();
            var entries = library?.presets?
                .Where(entry => entry?.events != null && entry.events.Any(evt => evt is SlashVFXEvent))
                .ToList();

            if (entries == null || entries.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("저장된 SlashVFX 프리셋 없음"));
            }
            else
            {
                foreach (var entry in entries)
                {
                    string label = string.IsNullOrEmpty(entry.displayName) ? entry.id : entry.displayName;
                    menu.AddItem(new GUIContent(label), false, () => ApplySlashVfxPreset(targetEvent, entry));
                }
            }

            menu.ShowAsContext();
        }

        void ApplySlashVfxPreset(SlashVFXEvent targetEvent, MotionEventPresetEntry entry)
        {
            SlashVFXEvent source = entry?.events?.OfType<SlashVFXEvent>().FirstOrDefault();
            if (source == null)
                return;

            float start = targetEvent.startTime;
            float end = targetEvent.endTime;

            RecordSelectedSlashVfxUndo("Apply SlashVFX Preset");
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), targetEvent);
            targetEvent.startTime = start;
            targetEvent.endTime = end;
            MarkSelectedSlashVfxDirty();

            _slashVfxSceneTuneStatus = $"프리셋 적용: {entry.displayName}";
            SceneView.RepaintAll();
        }

        void RecordSelectedSlashVfxUndo(string name)
        {
            UnityEngine.Object undoTarget = _asset != null
                ? _asset
                : _actorAnimationSet != null
                    ? _actorAnimationSet
                    : _playerActorAnimationSet != null
                        ? _playerActorAnimationSet
                        : null;

            if (undoTarget != null)
                Undo.RecordObject(undoTarget, name);
        }

        void MarkSelectedSlashVfxDirty()
        {
            if (_asset != null)
                EditorUtility.SetDirty(_asset);
            if (_actorAnimationSet != null)
                EditorUtility.SetDirty(_actorAnimationSet);
            if (_playerActorAnimationSet != null)
                EditorUtility.SetDirty(_playerActorAnimationSet);

            Repaint();
            SceneView.RepaintAll();
        }

        static Vector3 NormalizeEuler(Vector3 euler)
        {
            return new Vector3(
                MotionEventOffsetFieldUtil.NormalizeAngle(euler.x),
                MotionEventOffsetFieldUtil.NormalizeAngle(euler.y),
                MotionEventOffsetFieldUtil.NormalizeAngle(euler.z));
        }

        static Vector3 ResolveSlashVfxSpawnPosition(
            Vector3 center,
            Quaternion bladeRotation,
            Quaternion actorRootRotation,
            SlashVFXEvent slashEvent)
        {
            if (slashEvent != null && slashEvent.positionSpace == SlashVFXPositionSpace.World)
                return center + actorRootRotation * slashEvent.positionOffset;

            return center + bladeRotation * slashEvent.positionOffset;
        }

        static string GetRotationBaseLabel(SlashVFXEvent slashEvent)
        {
            return slashEvent.rotationSpace == SlashVFXRotationSpace.World
                ? "Actor Root Rotation"
                : "Blade Rotation";
        }

        static string GetPositionBaseLabel(SlashVFXEvent slashEvent)
        {
            return slashEvent.positionSpace == SlashVFXPositionSpace.World
                ? "Actor Root Offset"
                : "Blade Offset";
        }
    }
}
