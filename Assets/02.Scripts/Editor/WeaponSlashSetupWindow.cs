using System.Collections.Generic;
using System.Linq;
using FX;
using UPlayGround.Animation;
using UPlayGround.Animation.Editor;
using UPlayGround.Data.Event;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.VFX
{
    public sealed class WeaponSlashSetupWindow : EditorWindow
    {
        private const string SpawnFunctionName = "SpawnSlash";
        private const string BladeBaseName = "Blade_Base";
        private const string BladeTipName = "Blade_Tip";
        private const string PreviewName = "[Preview] Weapon Slash VFX";

        private GameObject targetObject;
        private GameObject weaponRootObject;
        private WeaponSlashVfxSpawner spawner;
        private Transform bladeBase;
        private Transform bladeTip;
        private GameObject slashVfxPrefab;
        private AnimationClip animationClip;
        private MotionSetAsset motionSetAsset;
        private SlashVFXPresetSO preset;
        private int spawnFrame = 12;
        private float scale = 1f;
        private float destroyDelay = 2f;
        private float minimumEventDuration = 0.05f;
        private bool updateExistingSlashEvents = true;
        private bool includeGlobalCollisionEvents;
        private bool overrideSpawnerTransform;
        private SlashVFXPositionSpace positionSpace = SlashVFXPositionSpace.Blade;
        private SlashVFXRotationSpace rotationSpace = SlashVFXRotationSpace.BladeOffset;
        private Vector3 positionOffset;
        private Vector3 rotationOffsetEuler;
        private Vector2 scroll;
        private string statusMessage = "";

        [MenuItem("UPlayGround/VFX/Weapon Slash Setup")]
        public static void Open()
        {
            var window = GetWindow<WeaponSlashSetupWindow>("Weapon Slash Setup");
            window.minSize = new Vector2(420f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            if (Selection.activeGameObject != null)
            {
                targetObject = Selection.activeGameObject;
                FindSpawner();
            }
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Weapon Slash VFX Setup", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Blade_Base / Blade_Tip의 현재 월드 자세를 샘플링해 Slash VFX를 1회 생성하는 세팅 도구입니다.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8);

            DrawTargetSection();
            EditorGUILayout.Space(6);
            DrawBladeSection();
            EditorGUILayout.Space(6);
            DrawVfxSection();
            EditorGUILayout.Space(6);
            DrawAnimationSection();
            EditorGUILayout.Space(6);
            DrawMotionSetAutomationSection();
            EditorGUILayout.Space(6);
            DrawPreviewSection();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTargetSection()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
                FindSpawner();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("WeaponSlashVfxSpawner", spawner, typeof(WeaponSlashVfxSpawner), true);
            EditorGUI.EndDisabledGroup();
            weaponRootObject = (GameObject)EditorGUILayout.ObjectField("Weapon Root Object", weaponRootObject, typeof(GameObject), true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add / Find Spawner", GUILayout.Height(24)))
                    AddOrFindSpawner();

                if (GUILayout.Button("Read From Spawner", GUILayout.Height(24)))
                    ReadFromSpawner();
            }
        }

        private void DrawBladeSection()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Blade", EditorStyles.boldLabel);

            bladeBase = (Transform)EditorGUILayout.ObjectField("Blade Base", bladeBase, typeof(Transform), true);
            bladeTip = (Transform)EditorGUILayout.ObjectField("Blade Tip", bladeTip, typeof(Transform), true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Blade Points", GUILayout.Height(24)))
                    CreateBladePoints();

                if (GUILayout.Button("Apply Blade Points", GUILayout.Height(24)))
                    ApplyBladePoints();
            }
        }

        private void DrawVfxSection()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("VFX", EditorStyles.boldLabel);

            preset = (SlashVFXPresetSO)EditorGUILayout.ObjectField("Preset", preset, typeof(SlashVFXPresetSO), false);

            if (GUILayout.Button("Apply Preset", GUILayout.Height(24)))
                ApplyPreset();

            slashVfxPrefab = (GameObject)EditorGUILayout.ObjectField("Slash VFX Prefab", slashVfxPrefab, typeof(GameObject), false);
            scale = EditorGUILayout.FloatField("Scale", scale);
            destroyDelay = EditorGUILayout.FloatField("Destroy Delay", destroyDelay);
            positionOffset = DrawPositionOffsetField("Position Offset", positionOffset, positionSpace);
            rotationOffsetEuler = DrawRotationOffsetField(rotationOffsetEuler, rotationSpace);

            if (GUILayout.Button("Apply VFX Settings", GUILayout.Height(24)))
                ApplyVfxSettings();
        }

        private void DrawAnimationSection()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Animation Clip Event", EditorStyles.boldLabel);

            animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false);
            spawnFrame = EditorGUILayout.IntField("Spawn Frame", Mathf.Max(0, spawnFrame));

            using (new EditorGUI.DisabledScope(animationClip == null))
            {
                float frameRate = animationClip != null ? animationClip.frameRate : 0f;
                float time = animationClip != null && frameRate > 0f ? spawnFrame / frameRate : 0f;
                EditorGUILayout.LabelField("Frame Rate", frameRate.ToString("0.###"));
                EditorGUILayout.LabelField("Event Time", $"{time:0.###} sec");

                if (GUILayout.Button("Add Animation Event", GUILayout.Height(26)))
                    AddAnimationEvent();
            }
        }

        private Vector3 DrawPositionOffsetField(string label, Vector3 value, SlashVFXPositionSpace space)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (space == SlashVFXPositionSpace.World)
            {
                value.x = EditorGUILayout.FloatField("World X", value.x);
                value.y = EditorGUILayout.FloatField("World Y", value.y);
                value.z = EditorGUILayout.FloatField("World Z", value.z);
            }
            else
            {
                value.x = EditorGUILayout.FloatField("Blade Right / X", value.x);
                value.y = EditorGUILayout.FloatField("Blade Up / Y", value.y);
                value.z = EditorGUILayout.FloatField("Blade Forward / Z", value.z);
            }
            EditorGUI.indentLevel--;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Offset", GUILayout.Height(22)))
                    value = Vector3.zero;

                if (GUILayout.Button("Preview After Apply", GUILayout.Height(22)))
                {
                    if (!overrideSpawnerTransform)
                        ApplyVfxSettings();
                    PreviewSpawn();
                }
            }

            return value;
        }

        private Vector3 DrawRotationOffsetField(Vector3 value, SlashVFXRotationSpace space)
        {
            EditorGUILayout.LabelField(space == SlashVFXRotationSpace.World ? "Rotation (World Euler)" : "Rotation Offset", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            value.x = EditorGUILayout.FloatField("Pitch / X", value.x);
            value.y = EditorGUILayout.FloatField("Yaw / Y", value.y);
            value.z = EditorGUILayout.FloatField("Roll / Z", value.z);
            EditorGUI.indentLevel--;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Rotation", GUILayout.Height(22)))
                    value = Vector3.zero;

                if (GUILayout.Button(space == SlashVFXRotationSpace.World ? "Yaw +180" : "Flip Forward", GUILayout.Height(22)))
                    value.y = MotionEventOffsetFieldUtil.NormalizeAngle(value.y + 180f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Roll +90", GUILayout.Height(22)))
                    value.z = MotionEventOffsetFieldUtil.NormalizeAngle(value.z + 90f);

                if (GUILayout.Button("Roll -90", GUILayout.Height(22)))
                    value.z = MotionEventOffsetFieldUtil.NormalizeAngle(value.z - 90f);
            }

            return value;
        }

        private void DrawMotionEventOverrideSection()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("MotionEvent Override", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Collision 기준 SlashVFX 이벤트를 만들 때 Spawner 값 대신 이벤트 값을 사용할지 설정합니다.", EditorStyles.wordWrappedMiniLabel);

            overrideSpawnerTransform = EditorGUILayout.Toggle("Override Spawner Transform", overrideSpawnerTransform);

            using (new EditorGUI.DisabledScope(!overrideSpawnerTransform))
            {
                positionSpace = (SlashVFXPositionSpace)EditorGUILayout.EnumPopup("Position Space", positionSpace);
                rotationSpace = (SlashVFXRotationSpace)EditorGUILayout.EnumPopup("Rotation Space", rotationSpace);
            }

            if (!overrideSpawnerTransform)
            {
                EditorGUILayout.HelpBox("꺼져 있으면 자동 생성된 SlashVFX 이벤트는 Spawner의 Position/Rotation/Scale/DestroyDelay를 사용합니다.", MessageType.None);
            }
            else if (positionSpace == SlashVFXPositionSpace.World || rotationSpace == SlashVFXRotationSpace.World)
            {
                EditorGUILayout.HelpBox("World 모드에서는 이벤트의 X/Y/Z 또는 Euler 값이 월드 기준으로 그대로 적용됩니다.", MessageType.None);
            }
        }

        private void DrawMotionSetAutomationSection()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("MotionEvent Automation", EditorStyles.boldLabel);

            DrawMotionEventOverrideSection();
            EditorGUILayout.Space(4);

            motionSetAsset = (MotionSetAsset)EditorGUILayout.ObjectField("MotionSet Asset", motionSetAsset, typeof(MotionSetAsset), false);
            minimumEventDuration = Mathf.Max(0.01f, EditorGUILayout.FloatField("Minimum Duration", minimumEventDuration));
            updateExistingSlashEvents = EditorGUILayout.Toggle("Update Existing SlashVFX", updateExistingSlashEvents);
            includeGlobalCollisionEvents = EditorGUILayout.Toggle("Include Global Events", includeGlobalCollisionEvents);

            float prefabDuration = CalculatePrefabPlaybackDuration(slashVfxPrefab);
            EditorGUILayout.LabelField("Particle Duration", prefabDuration > 0f ? $"{prefabDuration:0.###} sec" : "Prefab 없음");

            using (new EditorGUI.DisabledScope(motionSetAsset == null || slashVfxPrefab == null))
            {
                if (GUILayout.Button("Create SlashVFX From Collision Events", GUILayout.Height(26)))
                    CreateSlashEventsFromCollisionEvents();
            }
        }

        private void DrawPreviewSection()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Spawn", GUILayout.Height(26)))
                    PreviewSpawn();

                if (GUILayout.Button("Clear Preview", GUILayout.Height(26)))
                    ClearPreviewObjects();
            }
        }

        private void AddOrFindSpawner()
        {
            if (targetObject == null)
            {
                statusMessage = "Target Object를 먼저 지정하세요.";
                return;
            }

            GameObject ownerObject = weaponRootObject != null ? weaponRootObject : targetObject;
            FindSpawner();
            if (spawner == null)
            {
                spawner = Undo.AddComponent<WeaponSlashVfxSpawner>(ownerObject);
                statusMessage = $"{ownerObject.name}에 WeaponSlashVfxSpawner를 추가했습니다.";
            }
            else
            {
                statusMessage = "기존 WeaponSlashVfxSpawner를 찾았습니다.";
            }

            ReadFromSpawner();
        }

        private void FindSpawner()
        {
            GameObject searchObject = weaponRootObject != null ? weaponRootObject : targetObject;
            spawner = searchObject != null ? searchObject.GetComponentInChildren<WeaponSlashVfxSpawner>(true) : null;
            ReadFromSpawner();
        }

        private void ReadFromSpawner()
        {
            if (spawner == null)
                return;

            bladeBase = spawner.BladeBase;
            bladeTip = spawner.BladeTip;
            slashVfxPrefab = spawner.SlashVfxPrefab;
            if (weaponRootObject == null)
                weaponRootObject = spawner.gameObject;
            scale = spawner.Scale;
            destroyDelay = spawner.DestroyDelay;
            positionOffset = spawner.PositionOffset;
            rotationOffsetEuler = spawner.RotationOffsetEuler;
            Repaint();
        }

        private void CreateBladePoints()
        {
            if (targetObject == null)
            {
                statusMessage = "Target Object를 먼저 지정하세요.";
                return;
            }

            Transform parent = weaponRootObject != null ? weaponRootObject.transform : targetObject.transform;
            bladeBase = FindOrCreateChild(parent, BladeBaseName);
            bladeTip = FindOrCreateChild(parent, BladeTipName);

            Bounds localBounds = CalculateLocalRendererBounds(parent);
            Undo.RecordObject(bladeBase, "Set Blade Base");
            Undo.RecordObject(bladeTip, "Set Blade Tip");

            if (localBounds.size.sqrMagnitude > 0f)
            {
                Vector3 baseLocal = localBounds.center;
                Vector3 tipLocal = localBounds.center;
                baseLocal.z = localBounds.min.z;
                tipLocal.z = localBounds.max.z;
                bladeBase.localPosition = baseLocal;
                bladeTip.localPosition = tipLocal;
            }
            else
            {
                bladeBase.localPosition = Vector3.zero;
                bladeTip.localPosition = Vector3.forward;
            }

            bladeBase.localRotation = Quaternion.identity;
            bladeTip.localRotation = Quaternion.identity;
            bladeBase.localScale = Vector3.one;
            bladeTip.localScale = Vector3.one;

            ApplyBladePoints();
            statusMessage = $"Blade_Base / Blade_Tip을 {parent.name} 하위에 생성하거나 재사용했습니다. 실제 무기 오브젝트 하위인지 확인하고 위치를 수동 보정하세요.";
        }

        private Transform FindOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                return child;

            var go = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(go, $"Create {childName}");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private Bounds CalculateLocalRendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            bool hasBounds = false;
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (Renderer renderer in renderers)
            {
                Vector3[] corners =
                {
                    new(renderer.bounds.min.x, renderer.bounds.min.y, renderer.bounds.min.z),
                    new(renderer.bounds.min.x, renderer.bounds.min.y, renderer.bounds.max.z),
                    new(renderer.bounds.min.x, renderer.bounds.max.y, renderer.bounds.min.z),
                    new(renderer.bounds.min.x, renderer.bounds.max.y, renderer.bounds.max.z),
                    new(renderer.bounds.max.x, renderer.bounds.min.y, renderer.bounds.min.z),
                    new(renderer.bounds.max.x, renderer.bounds.min.y, renderer.bounds.max.z),
                    new(renderer.bounds.max.x, renderer.bounds.max.y, renderer.bounds.min.z),
                    new(renderer.bounds.max.x, renderer.bounds.max.y, renderer.bounds.max.z),
                };

                foreach (Vector3 corner in corners)
                {
                    Vector3 localCorner = root.InverseTransformPoint(corner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            return localBounds;
        }

        private void ApplyBladePoints()
        {
            if (!EnsureSpawner())
                return;

            Undo.RecordObject(spawner, "Apply Weapon Slash Blade Points");
            spawner.SetBladePoints(bladeBase, bladeTip);
            EditorUtility.SetDirty(spawner);
            statusMessage = "Blade Point 참조를 Spawner에 적용했습니다.";
        }

        private void ApplyPreset()
        {
            if (preset == null)
            {
                statusMessage = "Preset을 먼저 지정하세요.";
                return;
            }

            slashVfxPrefab = preset.vfxPrefab;
            // Spawner는 균일 스케일(float)을 사용하므로 scaleMultiplier의 X 성분을 대표값으로 사용한다.
            scale = preset.scaleMultiplier.x;
            destroyDelay = preset.destroyDelay;
            positionOffset = preset.positionOffset;
            rotationOffsetEuler = preset.rotationOffset;
            ApplyVfxSettings();
        }

        private void ApplyVfxSettings()
        {
            if (!EnsureSpawner())
                return;

            Undo.RecordObject(spawner, "Apply Weapon Slash VFX Settings");
            spawner.ApplySettings(slashVfxPrefab, scale, destroyDelay, positionOffset, rotationOffsetEuler);
            EditorUtility.SetDirty(spawner);
            statusMessage = "VFX 설정을 Spawner에 적용했습니다.";
        }

        private bool EnsureSpawner()
        {
            if (spawner != null)
                return true;

            AddOrFindSpawner();
            return spawner != null;
        }

        private void AddAnimationEvent()
        {
            if (animationClip == null)
            {
                statusMessage = "Animation Clip을 먼저 지정하세요.";
                return;
            }

            float frameRate = animationClip.frameRate;
            if (frameRate <= 0f)
            {
                statusMessage = "Animation Clip의 frameRate가 유효하지 않습니다.";
                return;
            }

            float time = spawnFrame / frameRate;
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(animationClip);
            float tolerance = 0.5f / frameRate;
            bool hasDuplicate = events.Any(e => e.functionName == SpawnFunctionName && Mathf.Abs(e.time - time) <= tolerance);

            if (hasDuplicate)
            {
                statusMessage = $"이미 {SpawnFunctionName} 이벤트가 같은 시간대에 있습니다. ({time:0.###} sec)";
                return;
            }

            Undo.RegisterCompleteObjectUndo(animationClip, "Add Weapon Slash Animation Event");
            var newEvent = new AnimationEvent
            {
                time = time,
                functionName = SpawnFunctionName,
            };

            AnimationEvent[] updatedEvents = events.Concat(new[] { newEvent }).OrderBy(e => e.time).ToArray();
            AnimationUtility.SetAnimationEvents(animationClip, updatedEvents);
            EditorUtility.SetDirty(animationClip);
            AssetDatabase.SaveAssets();

            statusMessage = $"{animationClip.name} {spawnFrame}프레임에 {SpawnFunctionName} 이벤트를 추가했습니다.";
        }

        private void CreateSlashEventsFromCollisionEvents()
        {
            if (motionSetAsset == null || motionSetAsset.motionSet == null)
            {
                statusMessage = "MotionSet Asset을 먼저 지정하세요.";
                return;
            }

            if (slashVfxPrefab == null)
            {
                statusMessage = "Slash VFX Prefab을 먼저 지정하세요.";
                return;
            }

            float eventDuration = Mathf.Max(minimumEventDuration, CalculatePrefabPlaybackDuration(slashVfxPrefab));
            int created = 0;
            int updated = 0;
            int skipped = 0;

            Undo.RecordObject(motionSetAsset, "Create SlashVFX MotionEvents From Collision");

            MotionSet motionSet = motionSetAsset.motionSet;
            if (motionSet.motions != null)
            {
                foreach (UPlayGround.Animation.Motion motion in motionSet.motions)
                {
                    if (motion == null)
                        continue;

                    SyncSlashEventsFromCollisionList(motion.events, eventDuration, ref created, ref updated, ref skipped);
                }
            }

            if (includeGlobalCollisionEvents)
                SyncSlashEventsFromCollisionList(motionSet.globalEvents, eventDuration, ref created, ref updated, ref skipped);

            EditorUtility.SetDirty(motionSetAsset);
            AssetDatabase.SaveAssets();

            statusMessage = $"Collision 기준 SlashVFX 동기화 완료 - 생성 {created}, 갱신 {updated}, 유지 {skipped}, duration {eventDuration:0.###}s";
        }

        private void SyncSlashEventsFromCollisionList(List<MotionEventBase> events, float eventDuration, ref int created, ref int updated, ref int skipped)
        {
            if (events == null)
                return;

            const float timeTolerance = 0.001f;
            var collisions = events
                .OfType<BeginCollisionEvent>()
                .OrderBy(evt => evt.startTime)
                .ToArray();

            foreach (BeginCollisionEvent collision in collisions)
            {
                SlashVFXEvent slashEvent = events
                    .OfType<SlashVFXEvent>()
                    .FirstOrDefault(evt => Mathf.Abs(evt.startTime - collision.startTime) <= timeTolerance);

                if (slashEvent == null)
                {
                    slashEvent = new SlashVFXEvent();
                    ApplySlashEventSettings(slashEvent, collision.startTime, eventDuration);
                    events.Add(slashEvent);
                    created++;
                    continue;
                }

                if (updateExistingSlashEvents)
                {
                    ApplySlashEventSettings(slashEvent, collision.startTime, eventDuration);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }

            events.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                return a.startTime.CompareTo(b.startTime);
            });
        }

        private void ApplySlashEventSettings(SlashVFXEvent slashEvent, float startTime, float eventDuration)
        {
            slashEvent.startTime = startTime;
            slashEvent.endTime = startTime + eventDuration;
            slashEvent.useSpawnerSettings = true;
            slashEvent.overrideSpawnerTransform = overrideSpawnerTransform;
            slashEvent.vfxPrefab = slashVfxPrefab;
            slashEvent.spawnerObjectName = spawner != null ? spawner.gameObject.name : "";
            slashEvent.weaponRootName = weaponRootObject != null ? weaponRootObject.name : "";
            slashEvent.basePointName = BladeBaseName;
            slashEvent.tipPointName = BladeTipName;
            slashEvent.positionSpace = positionSpace;
            slashEvent.positionOffset = positionOffset;
            slashEvent.rotationSpace = rotationSpace;
            slashEvent.rotationOffset = rotationOffsetEuler;
            slashEvent.scale = scale;
            slashEvent.destroyDelay = Mathf.Max(destroyDelay, eventDuration);
        }

        private float CalculatePrefabPlaybackDuration(GameObject prefab)
        {
            if (prefab == null)
                return 0f;

            float maxDuration = 0f;
            ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem == null)
                    continue;

                ParticleSystem.MainModule main = particleSystem.main;
                float duration = main.duration + GetMaxValue(main.startDelay) + GetMaxValue(main.startLifetime);
                maxDuration = Mathf.Max(maxDuration, duration);
            }

            return maxDuration;
        }

        private float GetMaxValue(ParticleSystem.MinMaxCurve curve)
        {
            return curve.mode switch
            {
                ParticleSystemCurveMode.Constant => curve.constant,
                ParticleSystemCurveMode.TwoConstants => curve.constantMax,
                ParticleSystemCurveMode.Curve => GetMaxCurveValue(curve.curve) * curve.curveMultiplier,
                ParticleSystemCurveMode.TwoCurves => Mathf.Max(
                    GetMaxCurveValue(curve.curveMin) * curve.curveMultiplier,
                    GetMaxCurveValue(curve.curveMax) * curve.curveMultiplier),
                _ => 0f,
            };
        }

        private float GetMaxCurveValue(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                return 0f;

            float max = 0f;
            foreach (Keyframe key in curve.keys)
                max = Mathf.Max(max, key.value);

            return max;
        }

        private void PreviewSpawn()
        {
            if (!EnsureSpawner())
                return;

            ApplyBladePoints();
            if (!overrideSpawnerTransform)
                ApplyVfxSettings();

            Vector3 spawnPosition;
            Quaternion rotation;
            bool hasSpawnPose = overrideSpawnerTransform
                ? spawner.TryGetSpawnPose(
                    slashVfxPrefab,
                    positionOffset,
                    positionSpace == SlashVFXPositionSpace.World,
                    rotationOffsetEuler,
                    rotationSpace == SlashVFXRotationSpace.World,
                    out spawnPosition,
                    out rotation)
                : spawner.TryGetSpawnPose(out spawnPosition, out rotation);

            if (!hasSpawnPose)
            {
                statusMessage = "Preview Spawn 실패: Spawner 참조와 Blade Point를 확인하세요.";
                return;
            }

            if (slashVfxPrefab == null)
            {
                statusMessage = "Slash VFX Prefab을 먼저 지정하세요.";
                return;
            }

            GameObject previewObject = PrefabUtility.InstantiatePrefab(slashVfxPrefab) as GameObject;
            if (previewObject == null)
                previewObject = Instantiate(slashVfxPrefab);

            Undo.RegisterCreatedObjectUndo(previewObject, "Preview Weapon Slash VFX");
            previewObject.name = PreviewName;
            previewObject.transform.SetPositionAndRotation(spawnPosition, rotation);
            previewObject.transform.localScale *= scale;
            previewObject.transform.SetParent(null, true);
            Selection.activeGameObject = previewObject;

            statusMessage = "Preview Slash VFX를 씬에 생성했습니다. Clear Preview로 정리할 수 있습니다.";
        }

        private void ClearPreviewObjects()
        {
            GameObject[] previewObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.name == PreviewName)
                .ToArray();

            foreach (GameObject previewObject in previewObjects)
                Undo.DestroyObjectImmediate(previewObject);

            statusMessage = $"Preview 오브젝트 {previewObjects.Length}개를 정리했습니다.";
        }
    }
}
