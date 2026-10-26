using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;

namespace UPlayGround.Animation.Editor
{
    /// <summary>에디터 측정에서 런타임이 소비할 운동 정보만 추출하고 GAS 반응 규칙을 연결한다.</summary>
    public static class CombatMotionImpactBaker
    {
        /// <summary>현재 원본을 재측정한 뒤 모든 결과가 유효할 때만 한 Undo 그룹으로 적용한다.</summary>
        public static void Bake(CombatMotionCatalog source)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Edit Mode에서 컴파일 완료 후 베이크하세요.");
            if (source == null || source.runtimeCatalog == null || source.motionResponse == null)
                throw new InvalidOperationException("런타임 운동 데이터와 타격 반응 규칙을 지정하세요.");

            var windows = new List<MotionImpactWindow>();
            var measurements = new List<CombatMotionMeasurement>();
            var payloads = new HashSet<UPlayGroundMotionAbilityPayloadSO>();
            ActorAnimationMotionSet owner = null;
            foreach (var entry in source.entries)
            {
                CombatMotionAnalysis.Resolve(source, entry, out var payload, out var entryOwner, out _);
                if (owner != null && owner != entryOwner)
                    throw new InvalidOperationException("측정 묶음은 동일 무기 모션 세트만 포함해야 합니다.");
                owner = entryOwner;
                payloads.Add(payload);
                var measurement = CombatMotionAnalysis.Measure(source, entry);
                measurements.Add(measurement);
                if (!measurement.hasSinglePassTiming)
                    throw new InvalidOperationException($"반복·분기 타임라인은 아직 베이크할 수 없습니다: {entry.ability.abilityId}");
                var extracted = Extract(measurement);
                if (extracted.Count == 0)
                    throw new InvalidOperationException($"부착형 타격 구간이 없습니다: {entry.ability.abilityId}");
                foreach (var window in extracted)
                {
                    if (windows.Any(w => w.motionKey == window.motionKey && w.hitPhaseIndex == window.hitPhaseIndex
                        && w.start < window.end && window.start < w.end))
                        throw new InvalidOperationException("같은 MotionKey/HitPhase에 중복 타격 구간이 있습니다.");
                    windows.Add(window);
                }
            }
            if (owner == null) throw new InvalidOperationException("베이크할 공격이 없습니다.");
            if (owner.motionImpactCatalog != null && owner.motionImpactCatalog != source.runtimeCatalog)
                throw new InvalidOperationException("모션 세트에 다른 런타임 운동 데이터가 연결되어 있습니다.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("모션 운동 데이터 런타임 베이크");
            var objects = new List<UnityEngine.Object> { source.runtimeCatalog, owner, source };
            objects.AddRange(payloads);
            try
            {
                Undo.RecordObjects(objects.ToArray(), "모션 운동 데이터 런타임 베이크");
                source.runtimeCatalog.windows = windows;
                owner.motionImpactCatalog = source.runtimeCatalog;
                foreach (var payload in payloads)
                {
                    foreach (var phase in payload.attackInfo.baseInfo.hitPhases)
                    {
                        phase.reactionProfile ??= new UPlayGround.Data.AttackReactionProfile();
                        phase.reactionProfile.motionResponse = source.motionResponse;
                    }
                }
                foreach (var obj in objects) { EditorUtility.SetDirty(obj); AssetDatabase.SaveAssetIfDirty(obj); }
                // 최초 연결로 바뀐 원본 참조를 반영한다. 운동 표본 자체는 바꾸지 않는다.
                for (int i = 0; i < source.entries.Count; i++)
                {
                    var entry = source.entries[i];
                    string fingerprint = Fingerprint(source, entry);
                    var key = GetKey(source, entry);
                    foreach (var window in windows)
                        if (window.motionKey == key) window.sourceFingerprint = fingerprint;
                    entry.measurement = measurements[i];
                }
                EditorUtility.SetDirty(source.runtimeCatalog);
                AssetDatabase.SaveAssetIfDirty(source.runtimeCatalog);
                foreach (var entry in source.entries)
                {
                    entry.measurement.fingerprint = CombatMotionAnalysis.Fingerprint(source, entry);
                    entry.measurement.prefab = CombatMotionAnalysis.Identify(source.modelPrefab);
                    entry.measurement.ability = CombatMotionAnalysis.Identify(entry.ability);
                    CombatMotionAnalysis.Resolve(source, entry, out var payload, out _, out _);
                    entry.measurement.payload = CombatMotionAnalysis.Identify(payload);
                    foreach (var dependency in entry.measurement.dependencies)
                        dependency.dependencyHash = AssetDatabase.GetAssetDependencyHash(dependency.path).ToString();
                }
                EditorUtility.SetDirty(source);
                AssetDatabase.SaveAssetIfDirty(source);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var obj in objects) { EditorUtility.SetDirty(obj); AssetDatabase.SaveAssetIfDirty(obj); }
                throw;
            }
        }

        /// <summary>루트 병진을 제거한 칼끝 표본과 시간 가중 평균 속도를 추출한다.</summary>
        public static List<MotionImpactWindow> Extract(CombatMotionMeasurement measurement)
        {
            var result = new List<MotionImpactWindow>();
            foreach (var timing in measurement.windows.Where(w => w.kind == "Hit"))
            {
                if (!timing.hasBounds || timing.groups.Count == 0 || timing.end <= timing.start)
                    throw new InvalidOperationException("부착형 HitBox 공간 표본이 없는 타격은 운동 데이터로 베이크할 수 없습니다.");
                var window = new MotionImpactWindow { motionKey = new MotionKey(measurement.motionKey),
                    hitPhaseIndex = timing.hitPhaseIndex, start = timing.start, end = timing.end };
                var samples = measurement.samples;
                for (int i = 0; i < samples.Count; i++)
                {
                    var sample = samples[i];
                    if (sample.time < timing.start - 0.00001f || sample.time > timing.end + 0.00001f) continue;
                    int beforeIndex = Math.Max(0, i - 1), afterIndex = Math.Max(1, i);
                    var before = samples[beforeIndex];
                    var after = samples[afterIndex];
                    float delta = after.time - before.time;
                    if (delta <= 0f) throw new InvalidOperationException("측정 시각이 오름차순이 아닙니다.");
                    Vector3 velocity = ((after.bladeTip - before.bladeTip) - (after.rootPosition - before.rootPosition)) / delta;
                    Vector3 local = Quaternion.Inverse(sample.rootRotation) * velocity;
                    if (float.IsNaN(local.sqrMagnitude) || float.IsInfinity(local.sqrMagnitude))
                        throw new InvalidOperationException("무기 속도에 유효하지 않은 값이 있습니다.");
                    window.samples.Add(new MotionImpactSample { time = Mathf.Clamp(sample.time, timing.start, timing.end), localVelocity = local });
                    window.peakSpeed = Mathf.Max(window.peakSpeed, local.magnitude);
                }
                if (window.samples.Count < 2 || Mathf.Abs(window.samples[0].time - timing.start) > 0.00001f
                    || Mathf.Abs(window.samples[window.samples.Count - 1].time - timing.end) > 0.00001f)
                    throw new InvalidOperationException("타격 시작·종료 경계 표본이 부족합니다.");
                float integral = 0f;
                for (int i = 1; i < window.samples.Count; i++)
                {
                    var before = window.samples[i - 1];
                    var after = window.samples[i];
                    integral += (before.localVelocity.magnitude + after.localVelocity.magnitude) * 0.5f * (after.time - before.time);
                }
                window.meanSpeed = integral / (timing.end - timing.start);
                result.Add(window);
            }
            return result;
        }

        /// <summary>파생 출력 자신을 의존성 해시에 포함하지 않아 재베이크가 스스로 오래됨을 만들지 않는다.</summary>
        public static string Fingerprint(CombatMotionCatalog source, CombatMotionEntry entry)
        {
            var motion = CombatMotionAnalysis.Resolve(source, entry, out var payload, out var owner, out _);
            string value = $"1|{source.weapon}|{source.sampleRate}|{source.weaponPath}|{source.drawnConstraintSource}|{source.bladeBasePath}|{source.bladeTipPath}|";
            value += AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(motion));
            value += File.ReadAllText(AssetDatabase.GetAssetPath(owner));
            foreach (string path in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(source.modelPrefab), true).OrderBy(p => p))
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".prefab") value += File.ReadAllText(path);
                else if (extension == ".fbx" || extension == ".anim" || extension == ".mask")
                    value += AssetDatabase.GetAssetDependencyHash(path);
            }
            foreach (var phase in payload.attackInfo.baseInfo.hitPhases) value += "|" + phase.hitboxGroupId;
            return Hash128.Compute(value).ToString();
        }

        private static MotionKey GetKey(CombatMotionCatalog source, CombatMotionEntry entry)
        {
            CombatMotionAnalysis.Resolve(source, entry, out var payload, out _, out _);
            return payload.attackInfo.motionKey;
        }
    }
}
