using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Components;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Editor.Ability.Production;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Animation.Editor
{
    /// <summary>기존 Ability 검사·Motion 해석·재생 커널을 묶는 오프라인 측정 진입점.</summary>
    public static partial class CombatMotionAnalysis
    {
        public const int Version = 2;

        /// <summary>참조와 sub-asset 식별자를 현재 AssetDatabase에서 읽는다.</summary>
        public static MotionSourceIdentity Identify(UnityEngine.Object source)
        {
            string path = AssetDatabase.GetAssetPath(source);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
            return new MotionSourceIdentity { path = path, guid = guid, localFileId = localId.ToString(),
                dependencyHash = string.IsNullOrEmpty(path) ? "" : AssetDatabase.GetAssetDependencyHash(path).ToString() };
        }

        /// <summary>실제 모델 AbilitySet의 멤버십과 무기 우선/NoWeapon 폴백을 검증한다.</summary>
        public static MotionSetAsset Resolve(CombatMotionCatalog catalog, CombatMotionEntry entry,
            out UPlayGroundMotionAbilityPayloadSO payload, out ActorAnimationMotionSet owner, out bool fallback)
        {
            if (catalog == null || catalog.modelPrefab == null || entry?.ability == null)
                throw new InvalidOperationException("모델 프리팹과 Ability가 필요합니다.");
            var model = catalog.modelPrefab.GetComponent<CharacterModelData>();
            var animator = catalog.modelPrefab.GetComponent<PlayerActorAnimator>();
            if (model?.abilitySet == null || !model.abilitySet.EnumerateAll().Contains(entry.ability))
                throw new InvalidOperationException("대상 모델의 실제 AbilitySet에 없는 Ability입니다.");
            var variant = entry.ability.variants?.FirstOrDefault(v => v != null && v.variantId == entry.variantId);
            payload = variant?.executionPayload as UPlayGroundMotionAbilityPayloadSO;
            if (payload?.attackInfo?.baseInfo == null || animator?.PlayerMotionSet == null)
                throw new InvalidOperationException("Variant의 공격 Payload 또는 플레이어 모션 세트가 없습니다.");
            var key = payload.attackInfo.motionKey;
            owner = animator.PlayerMotionSet.GetActorAnimationMotionSet(catalog.weapon);
            fallback = owner?.GetAbilityMotionAsset(key) == null;
            if (fallback) owner = animator.PlayerMotionSet.GetActorAnimationMotionSet(WeaponType.NoWeapon);
            var motion = animator.PlayerMotionSet.GetAbilityMotionAsset(catalog.weapon, key);
            if (motion == null) throw new InvalidOperationException($"MotionKey를 해석하지 못했습니다: {key}");
            return motion;
        }

        /// <summary>설정·모델 의존 에셋·클립 importer·전투 원본과 분석 버전의 지문.</summary>
        public static string Fingerprint(CombatMotionCatalog catalog, CombatMotionEntry entry)
        {
            var motion = Resolve(catalog, entry, out var payload, out _, out _);
            string settings = $"{Version}|{catalog.weapon}|{catalog.sampleRate}|{catalog.weaponPath}|" +
                $"{catalog.drawnConstraintSource}|{catalog.bladeBasePath}|{catalog.bladeTipPath}|" +
                $"{catalog.directionMinimumTravel:R}|{catalog.directionDominance:R}|{catalog.movementThreshold:R}|{entry.variantId}";
            foreach (var asset in new UnityEngine.Object[] { catalog.modelPrefab, entry.ability, payload, motion })
                settings += "|" + JsonUtility.ToJson(Identify(asset));
            return Hash128.Compute(settings).ToString();
        }

        /// <summary>수동 태그·검수 기록을 건드리지 않고 모든 측정 성공 후 결과만 교체한다.</summary>
        public static void Regenerate(CombatMotionCatalog catalog)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("측정은 Edit Mode에서 실행하세요.");
            if (catalog.sampleRate < 15 || catalog.sampleRate > 240)
                throw new InvalidOperationException("샘플률은 15~240Hz 범위여야 합니다.");
            var results = new List<CombatMotionMeasurement>();
            foreach (var entry in catalog.entries) results.Add(Measure(catalog, entry));
            Undo.RecordObject(catalog, "전투 모션 분석 재생성");
            for (int i = 0; i < results.Count; i++) catalog.entries[i].measurement = results[i];
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        /// <summary>원본 전투 에셋을 수정하지 않고 사용처 하나를 측정한다.</summary>
        public static CombatMotionMeasurement Measure(CombatMotionCatalog catalog, CombatMotionEntry entry)
        {
            var asset = Resolve(catalog, entry, out var payload, out var owner, out bool fallback);
            if (!MotionTimelineResolver.TryValidateSectionLayout(asset.motionSet, out string error))
                throw new InvalidOperationException(error);
            var result = new CombatMotionMeasurement {
                version = Version, generatedUtc = DateTime.UtcNow.ToString("O"), fingerprint = Fingerprint(catalog, entry),
                motionKey = payload.attackInfo.motionKey.value, motionSet = Identify(asset), prefab = Identify(catalog.modelPrefab),
                ability = Identify(entry.ability), payload = Identify(payload), resolvedOwner = AssetDatabase.GetAssetPath(owner),
                usedNoWeaponFallback = fallback, duration = asset.motionSet.TotalDuration };
            var report = AbilityMotionAnalyzer.Analyze(payload, owner);
            result.diagnostics.Add($"기존 AbilityMotionAnalyzer: 이벤트 {report.EventCount}, 필요 HitPhase {report.RequiredHitPhaseCount}, 이슈 {report.Issues.Count}");
            if (report.RequiredHitPhaseCount > payload.attackInfo.baseInfo.hitPhases.Count)
                throw new InvalidOperationException("Motion의 HitPhase 인덱스가 Payload 범위를 벗어났습니다.");
            CollectSources(catalog, asset, result);
            CollectTiming(asset.motionSet, payload.attackInfo, entry.usage, result);
            using (var sampler = new CombatMotionSampler(catalog))
            {
                result.avatar = JsonUtility.ToJson(Identify(sampler.Animator.avatar));
                sampler.Sample(asset.motionSet, result.samples, result.boundaries, result.windows);
                foreach (var clip in result.clips)
                {
                    var original = AssetDatabase.LoadAllAssetsAtPath(clip.source.path).OfType<AnimationClip>()
                        .First(c => Identify(c).localFileId == clip.source.localFileId);
                    var rawSet = MotionSet.CreateAuthored(original.name);
                    rawSet.motions.Add(new Motion { motionClip = original });
                    sampler.Sample(rawSet, clip.originalClipSamples, null, null);
                }
            }
            Summarize(catalog, result);
            return result;
        }

        private static void CollectSources(CombatMotionCatalog catalog, MotionSetAsset asset, CombatMotionMeasurement result)
        {
            CollectClips(asset.motionSet.motions, $"Base:{asset.motionSet.baseLayerIndex}", null, 1f, result.clips);
            foreach (var layer in asset.motionSet.layers.Where(l => l != null && l.enabled))
                CollectClips(layer.motions, $"{layer.layerName}:{layer.animancerLayerIndex}/{layer.blendMode}", layer.avatarMask, layer.weight, result.clips);
            // 각 루트의 dependencyHash가 전이 의존성을 포함하므로 수천 개의 무관한 FX 참조를 중복 저장하지 않는다.
            foreach (string path in new[] { AssetDatabase.GetAssetPath(catalog.modelPrefab), result.motionSet.path, result.ability.path, result.payload.path, result.resolvedOwner }
                .Concat(result.clips.Select(c => c.source.path)).Distinct())
                result.dependencies.Add(Identify(AssetDatabase.LoadMainAssetAtPath(path)));
            foreach (string guid in AssetDatabase.FindAssets("t:ActorAnimationMotionSet"))
            {
                var owner = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(AssetDatabase.GUIDToAssetPath(guid));
                if (owner?.abilityMotions == null) continue;
                foreach (var pair in owner.abilityMotions.Where(p => p.Value == asset))
                    result.sharedUsages.Add($"{AssetDatabase.GetAssetPath(owner)} :: {pair.Key.value}");
            }
            result.limitations.Add("원본 클립도 이 모델 Avatar로 리타기팅한 전체 구간 1배속 결과이며 DCC 원본 좌표가 아니다.");
            result.limitations.Add("루트는 imported Animator deltaPosition/deltaRotation. KCC·중력·지형·호밍·워프·IK·물리·히트스톱·동적 속도·입력은 제외.");
            result.limitations.Add("무기는 지정된 발도 ParentConstraint source/offset을 고정 평가한다. 장착 전환·무기 조준·다중 가중치 블렌드는 제외.");
            result.limitations.Add("크로스페이드와 외부 하체 포즈를 제외한 타임라인 포즈 측정. 병렬 레이어는 재생 커널의 mask/weight/blend 설정을 적용한다.");
            result.limitations.Add("측정 AABB는 표본 형상의 공간 외피이며 런타임 sweep·적중 보장·최종 사거리가 아니다. 최고 무기 속도는 접촉 시점이 아니다.");
        }

        private static void CollectClips(List<Motion> motions, string layer, AvatarMask mask, float weight, List<MotionClipMeasurement> results)
        {
            float offset = 0;
            foreach (var motion in motions ?? new List<Motion>())
            {
                if (motion?.motionClip == null) continue;
                var clip = motion.motionClip;
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                results.Add(new MotionClipMeasurement { source = Identify(clip), name = clip.name, layer = layer,
                    mask = AssetDatabase.GetAssetPath(mask), layerWeight = weight, isHumanoid = clip.isHumanMotion,
                    clipStart = motion.ClipStartTime, clipEnd = motion.ClipEndTime,
                    speed = motion.playbackSpeed > 0 ? motion.playbackSpeed : 1, timelineStart = offset, duration = motion.Duration,
                    importSettings = JsonUtility.ToJson(settings) });
                offset += motion.Duration;
            }
        }

        /// <summary>외부 조회용 JSON을 내보낸다. 생성 에셋의 GUID는 AssetDatabase가 소유한다.</summary>
        public static void Export(CombatMotionCatalog catalog, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(catalog));
        }

        private static void Summarize(CombatMotionCatalog catalog, CombatMotionMeasurement result)
        {
            var samples = result.samples;
            for (int i = 1; i < samples.Count; i++)
            {
                result.rootPathLength += Vector3.Distance(samples[i - 1].rootPosition, samples[i].rootPosition);
                if (samples[i].tipSpeed <= result.peakTipSpeed) continue;
                result.peakTipSpeed = samples[i].tipSpeed;
                result.peakTipTime = samples[i].time;
            }
            result.rootDisplacement = samples.Last().rootPosition - samples[0].rootPosition;
            result.rootYaw = samples.Last().rootYaw;
            var movement = result.rootDisplacement;
            result.suggestedMovement = result.rootPathLength < catalog.movementThreshold ? "제자리" :
                movement.magnitude < catalog.movementThreshold ? "왕복 이동" :
                Mathf.Abs(movement.z) > Mathf.Abs(movement.x) * catalog.directionDominance ? (movement.z > 0 ? "전진" : "후퇴") :
                Mathf.Abs(movement.x) > Mathf.Abs(movement.z) * catalog.directionDominance ? "횡이동" : "미분류";
            foreach (var window in result.windows.Where(w => w.kind == "Hit"))
            {
                var active = samples.Where(s => s.time >= window.start - 0.00001f && s.time <= window.end + 0.00001f).ToList();
                if (active.Count > 1) window.direction = Classify(catalog, active);
                foreach (var hitbox in active.SelectMany(s => s.hitboxes).Where(h => window.groups.Contains(h.group)))
                {
                    Bounds bounds = CombatMotionSampler.GetBounds(hitbox);
                    if (!window.hasBounds) { window.boundsMin = bounds.min; window.boundsMax = bounds.max; window.hasBounds = true; }
                    else { window.boundsMin = Vector3.Min(window.boundsMin, bounds.min); window.boundsMax = Vector3.Max(window.boundsMax, bounds.max); }
                }
                if (!window.hasBounds) result.diagnostics.Add($"P{window.hitPhaseIndex}: 일치하는 부착 HitBox 표본이 없습니다.");
            }
            var directions = result.windows.Where(w => w.kind == "Hit").Select(w => w.direction).Distinct().ToArray();
            if (directions.Length == 1) result.suggestedDirection = directions[0];
            if (!result.windows.Any(w => w.kind == "Hit" && result.peakTipTime >= w.start && result.peakTipTime <= w.end))
                result.diagnostics.Add($"최대 칼끝 속도 {result.peakTipTime:F3}s가 저작 판정 밖: 시각 검토 후보이며 접촉 오류 확정 아님.");
        }

        private static string Classify(CombatMotionCatalog catalog, List<MotionPoseSample> samples)
        {
            Vector3 delta = samples.Last().bladeTip - samples[0].bladeTip;
            if (delta.magnitude < catalog.directionMinimumTravel) return "미분류";
            float x = Mathf.Abs(delta.x), y = Mathf.Abs(delta.y), z = Mathf.Abs(delta.z);
            if (y > Mathf.Max(x, z) * catalog.directionDominance) return delta.y > 0 ? "올려베기" : "내려찍기";
            if (x > Mathf.Max(y, z) * catalog.directionDominance) return delta.x > 0 ? "좌→우" : "우→좌";
            // 찌르기·회전은 끝점 차이만으로 확정할 수 없어 이번 분석에서는 미분류로 남긴다.
            return "미분류";
        }
    }
}
