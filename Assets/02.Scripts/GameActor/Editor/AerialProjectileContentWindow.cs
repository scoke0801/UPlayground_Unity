using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Event;
using UPlayGround.Data.Projectile;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 투사체 공격 Ability를 실제로 발사되게 만드는 3종 세트를 한 번에 채운다.
    ///  1. <see cref="ProjectileDefinitionSO"/> 생성 (없으면)
    ///  2. Ability Payload의 HitPhase에 그 정의를 연결
    ///  3. 실행 MotionSet에 <see cref="SpawnProjectileEvent"/> 추가
    ///
    /// Ability Editor는 수치를, 애니메이션 에디터는 이벤트를 따로 다루므로 셋을 손으로
    /// 맞추면 하나를 빠뜨리기 쉽다(정의 없음 / 이벤트 없음 / 인덱스 불일치). 이 도구는
    /// 셋을 같은 hitPhaseIndex로 묶어 한 Undo 그룹에 적용한다.
    ///
    /// 특정 몬스터에 묶이지 않는다. 대상은 전부 필드로 지정한다.
    /// </summary>
    public sealed class AerialProjectileContentWindow : EditorWindow
    {
        [Header("대상")]
        [SerializeField] private GameplayAbilitySO _ability;
        [Tooltip("투사체 Payload를 수정할 Ability Variant 인덱스")]
        [Min(0)]
        [SerializeField] private int _variantIndex;
        [SerializeField] private MotionSetAsset _motion;
        [Tooltip("SpawnProjectileEvent를 추가할 MotionSet 내부 모션 인덱스")]
        [Min(0)]
        [SerializeField] private int _motionIndex;
        [SerializeField] private int _hitPhaseIndex;

        [Header("투사체 정의")]
        [SerializeField] private ProjectileDefinitionSO _existingDefinition;
        [SerializeField] private string _definitionFolder = "Assets/10.Datas/Projectile";
        [SerializeField] private string _definitionName = "ProjectileDefinition_";
        [SerializeField] private GameObject _visualPrefab;
        [SerializeField] private string _hitEffectKey = "LiteHit";
        [SerializeField] private float _speed = 18f;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private float _collisionRadius = 0.35f;

        [Header("발사 이벤트")]
        [SerializeField] private ProjectileTargetMode _targetMode =
            ProjectileTargetMode.EnemySkillTarget;
        [SerializeField] private Vector3 _spawnOffset = new(0f, 1.2f, 0.8f);
        [SerializeField] private string _spawnPointName = string.Empty;
        [Range(0f, 1f)]
        [SerializeField] private float _fireTimeRatio = 0.45f;
        [SerializeField] private float _eventWindow = 0.5f;

        private SerializedObject _serialized;
        private Vector2 _scroll;
        private string _report = "대상을 지정하고 ‘적용’을 누르세요.";

        [MenuItem("UPlayGround/툴 런처/게임플레이 · 전투/투사체 공격 콘텐츠 셋업", priority = 321)]
        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/툴 런처/게임플레이 · 전투/투사체 공격 콘텐츠 셋업",
            false,
            321)]
        public static void Open()
        {
            var window = GetWindow<AerialProjectileContentWindow>(true, "투사체 공격 콘텐츠 셋업");
            window.minSize = new Vector2(560f, 620f);
            window.Show();
        }

        private void OnEnable() => _serialized = new SerializedObject(this);

        private void OnGUI()
        {
            _serialized ??= new SerializedObject(this);
            _serialized.Update();

            EditorGUILayout.HelpBox(
                "투사체 정의 · Payload 연결 · 발사 MotionEvent를 같은 hitPhaseIndex로 묶어 "
                + "한 번에 적용합니다.",
                MessageType.Info);

            SerializedProperty property = _serialized.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
                EditorGUILayout.PropertyField(property, true);
            _serialized.ApplyModifiedProperties();

            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(_ability == null || _motion == null))
            {
                if (GUILayout.Button("적용", GUILayout.Height(28f)))
                    Apply();
            }

            EditorGUILayout.Space(6f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void Apply()
        {
            var log = new StringBuilder();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("투사체 공격 콘텐츠 셋업");

            try
            {
                ProjectileDefinitionSO definition = ResolveOrCreateDefinition(log);
                LinkPayload(definition, log);
                EnsureSpawnEvent(log);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Undo.CollapseUndoOperations(undoGroup);
                log.AppendLine();
                log.AppendLine("완료. 되돌리려면 Ctrl+Z.");
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.Refresh();
                log.AppendLine();
                log.AppendLine("실패 — 전체 롤백했습니다.");
                log.AppendLine(exception.Message);
                Debug.LogException(exception);
            }

            _report = log.ToString();
        }

        private ProjectileDefinitionSO ResolveOrCreateDefinition(StringBuilder log)
        {
            if (_existingDefinition != null)
            {
                log.AppendLine($"■ 투사체 정의: 기존 사용 — {_existingDefinition.name}");
                return _existingDefinition;
            }

            string path = $"{NormalizeFolder(_definitionFolder)}/{_definitionName.Trim()}.asset";
            var found = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(path);
            if (found != null)
            {
                log.AppendLine($"■ 투사체 정의: 이미 존재 — {path}");
                return found;
            }

            if (_visualPrefab == null)
                throw new InvalidOperationException(
                    "새 투사체 정의를 만들려면 비주얼 프리팹이 필요합니다. "
                    + "(ProjectileDefinitionSO는 visualPrefab이 없으면 검증에 실패합니다.)");

            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            definition.visualPrefab = _visualPrefab;
            definition.hitEffectKey = _hitEffectKey;
            definition.detachTrailOnReturn = true;
            definition.motion = new LinearProjectileMotion
            {
                speed = _speed,
                acceleration = 0f,
                maxSpeed = _speed,
            };
            definition.behaviors = new List<ProjectileBehaviorData>();
            definition.lifetime = _lifetime;
            definition.collisionRadius = _collisionRadius;
            definition.destroyOnHit = true;
            definition.inheritOwnerTimeScale = true;
            definition.prewarmCount = 4;
            definition.maxPoolSize = 32;

            // 디스크에 쓰기 전에 검증한다. 생성 후 실패하면 롤백해도 파일이 남을 수 있다.
            var errors = new List<string>();
            definition.CollectValidationErrors(errors);
            if (errors.Count > 0)
            {
                DestroyImmediate(definition);
                throw new InvalidOperationException(
                    "투사체 정의 검증 실패:\n" + string.Join("\n", errors));
            }

            EnsureFolder(NormalizeFolder(_definitionFolder));
            Undo.RegisterCreatedObjectUndo(definition, "투사체 정의 생성");
            AssetDatabase.CreateAsset(definition, path);

            log.AppendLine($"■ 투사체 정의 생성 — {path}");
            log.AppendLine($"  · Linear / speed {_speed} / lifetime {_lifetime} "
                           + $"/ radius {_collisionRadius}");
            return definition;
        }

        private void LinkPayload(ProjectileDefinitionSO definition, StringBuilder log)
        {
            UPlayGroundMotionAbilityPayloadSO payload = ResolvePayload(_ability, _variantIndex);
            if (payload?.attackInfo?.baseInfo == null
                || !payload.attackInfo.baseInfo.HasHitPhases)
                throw new InvalidOperationException(
                    $"{_ability.name}의 Payload 히트 페이즈를 찾을 수 없습니다.");

            List<HitPhaseData> phases = payload.attackInfo.baseInfo.hitPhases;
            if (_hitPhaseIndex < 0 || _hitPhaseIndex >= phases.Count)
                throw new InvalidOperationException(
                    $"hitPhaseIndex {_hitPhaseIndex}가 범위를 벗어났습니다. "
                    + $"({phases.Count}개 페이즈)");

            HitPhaseData phase = phases[_hitPhaseIndex];
            if (phase.projectileDefinition == definition)
            {
                log.AppendLine("■ Payload 연결: 이미 연결됨");
                return;
            }

            Undo.RecordObject(payload, "Payload 투사체 연결");
            phase.projectileDefinition = definition;
            EditorUtility.SetDirty(payload);
            log.AppendLine($"■ Payload 연결 — hitPhases[{_hitPhaseIndex}].projectileDefinition");
        }

        private static UPlayGroundMotionAbilityPayloadSO ResolvePayload(
            GameplayAbilitySO ability,
            int variantIndex)
        {
            if (ability?.variants == null || ability.variants.Count == 0)
                throw new InvalidOperationException("Ability에 Variant가 없습니다.");
            if (variantIndex < 0 || variantIndex >= ability.variants.Count)
                throw new InvalidOperationException(
                    $"Variant 인덱스 {variantIndex}가 범위를 벗어났습니다. "
                    + $"({ability.variants.Count}개 Variant)");
            if (ability.variants[variantIndex]?.executionPayload
                is not UPlayGroundMotionAbilityPayloadSO payload)
                throw new InvalidOperationException(
                    $"Variant[{variantIndex}]에 Motion Ability Payload가 없습니다.");

            return payload;
        }

        private void EnsureSpawnEvent(StringBuilder log)
        {
            if (_motion.motionSet?.motions == null || _motion.motionSet.motions.Count == 0)
                throw new InvalidOperationException($"{_motion.name}에 모션이 없습니다.");
            if (_motionIndex < 0 || _motionIndex >= _motion.motionSet.motions.Count)
                throw new InvalidOperationException(
                    $"Motion 인덱스 {_motionIndex}가 범위를 벗어났습니다. "
                    + $"({_motion.motionSet.motions.Count}개 Motion)");

            UPlayGround.Animation.Motion motion = _motion.motionSet.motions[_motionIndex];
            motion.events ??= new List<MotionEventBase>();

            int otherPhaseEventCount = 0;
            for (var i = 0; i < motion.events.Count; i++)
            {
                if (motion.events[i] is SpawnProjectileEvent existing)
                {
                    if (existing.hitPhaseIndex == _hitPhaseIndex)
                    {
                        log.AppendLine(
                            $"■ 발사 이벤트: 이미 존재 — hitPhaseIndex {existing.hitPhaseIndex}");
                        return;
                    }

                    otherPhaseEventCount++;
                }
            }

            if (otherPhaseEventCount > 0)
                log.AppendLine(
                    $"■ 다른 HitPhase의 발사 이벤트 {otherPhaseEventCount}건은 보존합니다.");

            if (motion.motionClip == null)
                throw new InvalidOperationException(
                    $"{_motion.name}에 AnimationClip이 없어 발사 시점을 계산할 수 없습니다.");

            float duration = motion.Duration > 0f ? motion.Duration : motion.motionClip.length;
            float start = Mathf.Clamp(
                duration * _fireTimeRatio,
                0f,
                Mathf.Max(0f, duration - 0.05f));
            float end = Mathf.Min(duration, start + Mathf.Max(0.05f, _eventWindow));

            var spawnEvent = new SpawnProjectileEvent
            {
                startTime = start,
                endTime = end,
                // 정의를 비워 두면 hitPhaseIndex로 Combat의 현재 스킬 HitPhase에서 해석한다.
                // 수치 단일 소스를 Payload에 두기 위해 이벤트에는 직접 물리지 않는다.
                projectileDefinition = null,
                projectilePrefab = null,
                hitPhaseIndex = _hitPhaseIndex,
                spawnPointName = _spawnPointName,
                spawnOffset = _spawnOffset,
                useSpawnRotation = !string.IsNullOrWhiteSpace(_spawnPointName),
                targetMode = _targetMode,
                hitParticleName = _hitEffectKey,
            };

            Undo.RecordObject(_motion, "발사 이벤트 추가");
            motion.events.Add(spawnEvent);
            EditorUtility.SetDirty(_motion);

            log.AppendLine(
                $"■ 발사 이벤트 추가 — {_motion.name}.motions[{_motionIndex}] "
                + $"(클립 {duration:0.00}초)");
            log.AppendLine($"  · startTime {start:0.00} / endTime {end:0.00} "
                           + $"/ targetMode {_targetMode}");
            log.AppendLine("  · ⚠ 발사 타이밍과 spawnOffset은 비율 기반 추정값입니다. "
                           + "애니메이션 에디터에서 모션을 보고 맞추세요.");
        }

        private static string NormalizeFolder(string folder)
            => string.IsNullOrWhiteSpace(folder)
                ? "Assets"
                : folder.Trim().Replace('\\', '/').TrimEnd('/');

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folder);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(leaf))
                throw new InvalidOperationException($"폴더 경로가 올바르지 않습니다: {folder}");

            EnsureFolder(parent);
            if (string.IsNullOrWhiteSpace(AssetDatabase.CreateFolder(parent, leaf)))
                throw new InvalidOperationException($"폴더를 생성하지 못했습니다: {folder}");
        }
    }
}
