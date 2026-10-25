using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>실제 모델 측정과 시간·보존 경계의 EditMode 검증. 원본 에셋은 수정하지 않는다.</summary>
    public static class CombatMotionAnalysisValidation
    {
        [Serializable] private sealed class Report { public string generatedUtc; public List<string> passed = new(); public List<string> failed = new(); }

        /// <summary>실제 프리팹을 사용하는 의미 있는 경계를 검사하고 실패는 예외로 반환한다.</summary>
        public static string Run(CombatMotionCatalog catalog)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("경계 검증은 Edit Mode에서 실행하세요.");
            var report = new Report { generatedUtc = DateTime.UtcNow.ToString("O") };
            var sourcePaths = new List<string> { AssetDatabase.GetAssetPath(catalog.modelPrefab) };
            foreach (var entry in catalog.entries)
            {
                sourcePaths.Add(AssetDatabase.GetAssetPath(entry.ability));
                sourcePaths.Add(AssetDatabase.GetAssetPath(CombatMotionAnalysis.Resolve(catalog, entry, out _, out _, out _)));
            }
            var sourceHashes = CaptureSourceHashes(sourcePaths.ToArray());
            Check(report, "생성된 대표 데이터·유한수·타격 범위", () => {
                Require(catalog.entries.Count >= 5, "대표 세트가 부족합니다.");
                foreach (var entry in catalog.entries)
                {
                    var result = entry.measurement;
                    Require(result != null && result.samples.Count > 2 && result.fingerprint == CombatMotionAnalysis.Fingerprint(catalog, entry), "결과가 없거나 오래되었습니다.");
                    Require(result.windows.Where(w => w.kind == "Hit").All(w => w.hasBounds), "타격 범위 미측정");
                    Require(result.samples.All(s => IsFinite(s.bladeTip) && IsFinite(s.rootPosition) && !float.IsNaN(s.tipSpeed)), "비정상 측정값");
                    Require(result.clips.All(c => long.TryParse(c.source.localFileId, out _) && c.originalClipSamples.Count > 2), "원본 Clip/ID 누락");
                }
            });
            Check(report, "원본 포즈·저장 궤적 일치 및 이벤트 부수효과 차단", () => ValidatePose(catalog));
            Check(report, "재생 구간 20~80%·2배속의 클립 시간 및 경계 포즈", () => ValidateTrim(catalog));
            Check(report, "다단 HitPhase·타격 간격·행동별 캔슬 폴백", ValidateTiming);
            Check(report, "비율 이벤트·레이어 이벤트 발화 계약", ValidateLinkedTiming);
            Check(report, "AvatarMask 레이어 적용과 비활성화 복원", () => ValidateLayer(catalog));
            Check(report, "누락된 무기 측정 지점은 실패하고 프리팹 씬을 해제", () => {
                var copy = UnityEngine.Object.Instantiate(catalog);
                int count = UnityEngine.SceneManagement.SceneManager.sceneCount;
                try {
                    copy.bladeTipPath = "Missing/BladeTip";
                    bool failed = false;
                    try { using var sampler = new CombatMotionSampler(copy); } catch (InvalidOperationException) { failed = true; }
                    Require(failed, "누락 지점을 조용히 허용했습니다.");
                    Require(count == UnityEngine.SceneManagement.SceneManager.sceneCount, "임시 씬 누수");
                } finally { UnityEngine.Object.DestroyImmediate(copy); }
            });
            Check(report, "재분석은 수동 태그·검수 결과를 보존", () => {
                var copy = UnityEngine.Object.Instantiate(catalog);
                try {
                    copy.entries = new List<CombatMotionEntry> { copy.entries[0] };
                    var entry = copy.entries[0]; entry.manualTags = "수동 보존"; entry.isReviewed = true; entry.reviewNotes = "검수 기록";
                    CombatMotionAnalysis.Regenerate(copy);
                    Require(entry.manualTags == "수동 보존" && entry.isReviewed && entry.reviewNotes == "검수 기록", "수동 검수 유실");
                } finally { UnityEngine.Object.DestroyImmediate(copy); }
            });
            Check(report, "샘플 설정 변경 감지 및 검수 메모 독립성", () => {
                var copy = UnityEngine.Object.Instantiate(catalog);
                try {
                    string original = CombatMotionAnalysis.Fingerprint(copy, copy.entries[0]);
                    copy.entries[0].manualTags = "검수만 변경";
                    Require(original == CombatMotionAnalysis.Fingerprint(copy, copy.entries[0]), "검수 변경이 측정 지문에 포함됨");
                    copy.sampleRate = copy.sampleRate == 120 ? 60 : 120;
                    Require(original != CombatMotionAnalysis.Fingerprint(copy, copy.entries[0]), "측정 설정 변경 미감지");
                } finally { UnityEngine.Object.DestroyImmediate(copy); }
            });
            Check(report, "측정 전후 원본 SO·프리팹·GUID 파일 바이트 보존", () => {
                var after = CaptureSourceHashes(sourcePaths.ToArray());
                Require(sourceHashes.Count == after.Count && sourceHashes.All(p => after.TryGetValue(p.Key, out string hash) && hash == p.Value), "측정 도중 원본 에셋이 변경되었습니다.");
            });
            Directory.CreateDirectory("Logs/CombatMotionAnalysis");
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText("Logs/CombatMotionAnalysis/validation.json", json);
            File.WriteAllText("Logs/CombatMotionAnalysis/validated-source-hashes.txt", string.Join("\n", sourceHashes.Select(p => p.Key + " " + p.Value)));
            if (report.failed.Count > 0) throw new InvalidOperationException(json);
            return json;
        }

        private static void ValidatePose(CombatMotionCatalog catalog)
        {
            float timeScale = Time.timeScale;
            foreach (var entry in catalog.entries)
            {
                var asset = CombatMotionAnalysis.Resolve(catalog, entry, out _, out _, out _);
                using var sampler = new CombatMotionSampler(catalog);
                Require(!sampler.Animator.fireEvents, "Clip 이벤트가 활성화되어 있습니다.");
                Require(sampler.Root.GetComponentsInChildren<MonoBehaviour>(true).All(b => b == null || !b.enabled || b is Animancer.AnimancerComponent), "게임플레이 스크립트 활성화");
                foreach (var boundary in entry.measurement.boundaries)
                {
                    var sample = entry.measurement.samples.OrderBy(s => Mathf.Abs(s.time - boundary.time)).First();
                    sampler.ShowPose(asset.motionSet, sample);
                    Require(sampler.GetTipError(sample) < 0.001f, $"{entry.ability.name} {sample.time:F3}s 포즈/궤적 불일치");
                }
            }
            Require(Mathf.Approximately(timeScale, Time.timeScale), "전역 시간 변경");
        }

        private static void ValidateTrim(CombatMotionCatalog catalog)
        {
            var asset = CombatMotionAnalysis.Resolve(catalog, catalog.entries[0], out _, out _, out _);
            var clip = asset.motionSet.motions[0].motionClip;
            var set = MotionSet.CreateAuthored("검증용 잘라낸 클립");
            var motion = new Motion { motionClip = clip, clipStartTime = clip.length * 0.2f, clipEndTime = clip.length * 0.8f, playbackSpeed = 2 };
            set.motions.Add(motion);
            var samples = new List<MotionPoseSample>();
            using var sampler = new CombatMotionSampler(catalog);
            sampler.Sample(set, samples, null, null);
            Require(Mathf.Abs(samples.Last().time - clip.length * 0.3f) < 0.00001f, "속도 적용 길이 오류");
            Require(Mathf.Abs(samples[0].clipTime - clip.length * 0.2f) < 0.00001f, "원본 시작 시간 오류");
            Require(Mathf.Abs(samples.Last().clipTime - clip.length * 0.8f) < 0.00001f, "원본 종료 시간 오류");
            sampler.ShowPose(set, samples.Last());
            Require(sampler.GetTipError(samples.Last()) < 0.001f, "구간/속도 변경 후 포즈 불일치");
        }

        private static void ValidateTiming()
        {
            var clip = new AnimationClip();
            try
            {
                clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
                var set = MotionSet.CreateAuthored("다단 검증");
                set.motions.Add(new Motion { motionClip = clip, events = new List<MotionEventBase> {
                    new BeginCollisionEvent { startTime = 0.2f, endTime = 0.3f, hitPhaseIndex = 0 },
                    new BeginCollisionEvent { startTime = 0.6f, endTime = 0.7f, hitPhaseIndex = 1 },
                    new CancelWindowEvent { startTime = 0.25f, endTime = 0.27f, maskOverride = PlayerInterruptAction.Dodge } } });
                var attack = new AbilityAttackInfo();
                attack.baseInfo = new AttackInfoBase();
                attack.baseInfo.hitPhases = new List<HitPhaseData> { new(), new() };
                attack.interruptActions = PlayerInterruptAction.Dodge | PlayerInterruptAction.Guard | PlayerInterruptAction.Move;
                var result = new CombatMotionMeasurement();
                CombatMotionAnalysis.CollectTiming(set, attack, "LightCombo[0]", result);
                var hits = result.windows.Where(w => w.kind == "Hit").ToArray();
                Require(hits.Length == 2 && hits[1].hitPhaseIndex == 1 && Mathf.Abs(hits[1].gapFromPrevious - 0.3f) < 0.00001f, "다단 간격 오류");
                Require(Mathf.Abs(result.beforeFirstHit - 0.2f) < 0.00001f && Mathf.Abs(result.afterLastHit - 0.3f) < 0.00001f, "전후 길이 오류");
                Require(CombatMotionAnalysis.ResolveCancelMask(attack.interruptActions, false, Array.Empty<PlayerInterruptAction>()) == attack.interruptActions, "OFF 폴백 오류");
                Require(CombatMotionAnalysis.ResolveCancelMask(attack.interruptActions, true, new[] { PlayerInterruptAction.Dodge }) == PlayerInterruptAction.Dodge, "활성 마스크 오류");
                set.sections[0].endPolicy = MotionSectionEndPolicy.LoopSelf;
                result = new CombatMotionMeasurement(); CombatMotionAnalysis.CollectTiming(set, attack, "LightCombo[0]", result);
                Require(!result.hasSinglePassTiming && result.beforeFirstHit < 0 && result.afterLastHit < 0, "반복 구간을 단일 실행으로 축약함");
            }
            finally { UnityEngine.Object.DestroyImmediate(clip); }
        }

        private static void ValidateLinkedTiming()
        {
            var clip = new AnimationClip();
            try
            {
                clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 2, 1));
                var set = MotionSet.CreateAuthored("비율 이벤트 검증");
                var hit = new BeginCollisionEvent { timeLink = new MotionEventTimeLink { enabled = true,
                    mode = MotionEventLinkMode.Proportional, linkedMotionId = "main", startValue = 0.25f, endValue = 0.5f } };
                var motion = new Motion { id = "main", motionClip = clip, clipStartTime = 0.5f, clipEndTime = 1.5f,
                    playbackSpeed = 2, events = new List<MotionEventBase> { hit } };
                set.motions.Add(motion);
                Require(MotionTimelineResolver.TryGetEventGlobalRange(set, hit, out float start, out float end) &&
                    Mathf.Abs(start - 0.125f) < 0.00001f && Mathf.Abs(end - 0.25f) < 0.00001f, "비율 이벤트의 재생 속도 해석 오류");
                var followerHit = new BeginCollisionEvent { startTime = 0.1f, endTime = 0.2f };
                var layer = new MotionLayer { sync = new MotionSyncSettings { role = MotionSyncRole.Follower, triggerFollowerEvents = false },
                    motions = new List<Motion> { new() { motionClip = clip, events = new List<MotionEventBase> { followerHit } } } };
                set.layers.Add(layer);
                var events = new List<MotionEventBase>();
                MotionTimelineResolver.CollectEventsInRange(set, -1, 10, events);
                Require(events.Count == 1 && events[0] == hit, "Follower의 비발화 이벤트를 타격으로 수집함");
            }
            finally { UnityEngine.Object.DestroyImmediate(clip); }
        }

        private static void ValidateLayer(CombatMotionCatalog catalog)
        {
            var asset = CombatMotionAnalysis.Resolve(catalog, catalog.entries[0], out _, out _, out _);
            var other = CombatMotionAnalysis.Resolve(catalog, catalog.entries[2], out _, out _, out _);
            var set = MotionSet.CreateAuthored("레이어 검증");
            set.motions.Add(new Motion { motionClip = asset.motionSet.motions[0].motionClip });
            var mask = new AvatarMask();
            try
            {
                for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
                var layer = new MotionLayer { avatarMask = mask, weight = 1, motions = new List<Motion> { new() { motionClip = other.motionSet.motions[0].motionClip } } };
                using var sampler = new CombatMotionSampler(catalog);
                var baseSamples = new List<MotionPoseSample>(); sampler.Sample(set, baseSamples, null, null);
                set.layers.Add(layer);
                var layered = new List<MotionPoseSample>(); sampler.Sample(set, layered, null, null);
                float time = 0.3f;
                var before = baseSamples.OrderBy(s => Mathf.Abs(s.time - time)).First();
                var after = layered.OrderBy(s => Mathf.Abs(s.time - time)).First();
                Require(Vector3.Distance(before.bladeTip, after.bladeTip) > 0.01f, "오른팔 레이어가 포즈에 적용되지 않음");
                Require(Vector3.Distance(before.rootPosition, after.rootPosition) < 0.001f, "상체 마스크가 루트 이동을 바꿈");
                layer.enabled = false;
                var restored = new List<MotionPoseSample>(); sampler.Sample(set, restored, null, null);
                var final = restored.OrderBy(s => Mathf.Abs(s.time - time)).First();
                Require(Vector3.Distance(before.bladeTip, final.bladeTip) < 0.001f, "비활성 레이어의 포즈가 남아 있음");
            }
            finally { UnityEngine.Object.DestroyImmediate(mask); }
        }

        private static bool IsFinite(Vector3 value) => !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Check(Report report, string name, Action action)
        {
            try { action(); report.passed.Add(name); }
            catch (Exception exception) { report.failed.Add(name + ": " + exception.Message); }
        }

        private static Dictionary<string, string> CaptureSourceHashes(string[] roots)
        {
            var result = new Dictionary<string, string>();
            using var sha = SHA256.Create();
            foreach (string path in AssetDatabase.GetDependencies(roots, true).Where(p => p.EndsWith(".asset") || p.EndsWith(".prefab")))
                foreach (string file in new[] { path, path + ".meta" })
                    if (File.Exists(file)) result[file] = Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(file)));
            return result;
        }
    }
}
