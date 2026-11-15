using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;

namespace UPlayGround.EditorTools
{
    /// <summary>구매·이식 원본을 보존하고 시그니처 전용 파생 클립의 추출 루트만 보정한다.</summary>
    public static class WeaponSignatureRootMotionAuthoring
    {
        private const string ClipRoot = "Assets/07.Animations/Player/Signature/";
        private const string OutputRoot = ClipRoot + "DerivedRoot/";
        private const string MotionRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Signature/";
        private const float DashStart = 15f / 30f;
        private const float DashEnd = 21f / 30f;
        private const float DashDistance = 8f;

        [Serializable]
        private sealed class SourceRecord
        {
            public string sourceGuid;
            public long sourceLocalId;
        }

        private readonly struct Plan
        {
            public readonly MotionSetAsset Motion;
            public readonly AnimationClip Source;
            public readonly string Output;
            public readonly float HumanScale;
            public readonly bool IsDash;
            public Plan(MotionSetAsset motion, AnimationClip source, string output, float humanScale, bool dash)
            { Motion = motion; Source = source; Output = output; HumanScale = humanScale; IsDash = dash; }
        }

        /// <summary>편집 모드에서 4개 MotionSet의 클립 참조만 원본 기반 파생 클립으로 바꾼다.</summary>
        public static string Apply()
        {
            CheckEditor();
            var plans = new[]
            {
                PrepareDash("Katana", "Arin", ImportedClip(ClipRoot + "Katana/Katana_Signature.fbx")),
                PrepareDash("Katana_Raon", "Raon", Require<AnimationClip>(ClipRoot + "Katana/Katana_Raon_Signature.anim")),
                PrepareSpin("DoubleAxe"),
                PrepareSpin("DualBlade")
            };
            var touched = new List<UnityEngine.Object>();
            var created = new List<string>();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("시그니처 추출 루트 보정");
            try
            {
                EnsureFolder(OutputRoot.TrimEnd('/'));
                var report = new StringBuilder();
                foreach (Plan plan in plans)
                {
                    var output = AssetDatabase.LoadAssetAtPath<AnimationClip>(plan.Output);
                    if (output == null)
                    {
                        if (AssetDatabase.LoadMainAssetAtPath(plan.Output) != null)
                            throw new InvalidOperationException("기존 출력의 타입 불일치: " + plan.Output);
                        output = UnityEngine.Object.Instantiate(plan.Source);
                        AssetDatabase.CreateAsset(output, plan.Output);
                        created.Add(plan.Output);
                        Undo.RegisterCreatedObjectUndo(output, "시그니처 파생 클립 생성");
                    }
                    else
                    {
                        Undo.RegisterCompleteObjectUndo(output, "시그니처 파생 클립 재저작");
                        EditorUtility.CopySerialized(plan.Source, output);
                    }
                    touched.Add(output);
                    output.name = Path.GetFileNameWithoutExtension(plan.Output);
                    if (plan.IsDash) WriteDash(output, plan.Source, plan.HumanScale, report);
                    else WriteStationarySpin(output, plan.Source, report);
                    ValidateDerived(plan, output);
                    Undo.RegisterCompleteObjectUndo(plan.Motion, "시그니처 파생 클립 연결");
                    touched.Add(plan.Motion);
                    foreach (var motion in plan.Motion.motionSet.motions) motion.motionClip = output;
                    EditorUtility.SetDirty(output);
                    EditorUtility.SetDirty(plan.Motion);
                }
                foreach (UnityEngine.Object asset in touched) AssetDatabase.SaveAssetIfDirty(asset);
                foreach (Plan plan in plans) StoreSourceRecord(plan);
                Undo.CollapseUndoOperations(group);
                report.AppendLine("Spear는 공통 전신 상승의 RootT.y 상관관계 실측 전이라 변경하지 않았습니다.");
                report.AppendLine("실제 Animator.deltaPosition·yaw 및 타격 구간은 120Hz 재측정해야 합니다.");
                return report.ToString();
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (string path in created)
                    if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
                foreach (UnityEngine.Object asset in touched)
                    if (asset != null) AssetDatabase.SaveAssetIfDirty(asset);
                foreach (Plan plan in plans)
                    if (AssetImporter.GetAtPath(plan.Output) != null) AssetDatabase.WriteImportSettingsIfDirty(plan.Output);
                throw;
            }
        }

        private static Plan PrepareDash(string key, string character, AnimationClip source)
        {
            var motion = Require<MotionSetAsset>(MotionRoot + key + "_Signature.asset");
            string output = OutputRoot + key + "_Signature_ForwardRoot.anim";
            CheckCurrentSources(motion, source, output);
            if (motion.motionSet.motions.Count != 1 || Mathf.Abs(motion.motionSet.TotalDuration - 51f / 30f) > .001f
                || Mathf.Abs(source.length - 51f / 30f) > .001f)
                throw new InvalidOperationException(key + "의 51프레임 입력 계약이 달라졌습니다.");
            RequireCurve(source, "RootT.x"); RequireCurve(source, "RootT.z");
            ReadRotation(source, 0f);
            return new Plan(motion, source, output, HumanScale(character), true);
        }

        private static Plan PrepareSpin(string weapon)
        {
            var motion = Require<MotionSetAsset>(MotionRoot + weapon + "_Signature.asset");
            string output = OutputRoot + weapon + "_Signature_StationaryRoot.anim";
            if (motion.motionSet?.motions == null || motion.motionSet.motions.Count == 0)
                throw new InvalidOperationException(weapon + ": 입력 모션이 없습니다.");
            AnimationClip source = ResolveOriginal(motion.motionSet.motions[0].motionClip, output);
            CheckCurrentSources(motion, source, output);
            RequireCurve(source, "RootT.x"); RequireCurve(source, "RootT.z");
            return new Plan(motion, source, output, 1f, false);
        }

        private static void CheckCurrentSources(MotionSetAsset asset, AnimationClip source, string output)
        {
            if (asset.motionSet == null || asset.motionSet.motions.Count == 0)
                throw new InvalidOperationException("연결할 모션이 없습니다: " + asset.name);
            foreach (var motion in asset.motionSet.motions)
                if (motion.motionClip != source && AssetDatabase.GetAssetPath(motion.motionClip) != output)
                    throw new InvalidOperationException(asset.name + "에 다른 저작 클립이 연결되어 있습니다. 자동 교체하지 않습니다.");
        }

        private static void WriteDash(AnimationClip output, AnimationClip source, float humanScale, StringBuilder report)
        {
            Quaternion yaw = Yaw(ReadRotation(source, 0f));
            // RootT는 Body 좌표계다. 초기 Body yaw를 보상해야 추출된 월드 이동이 정면이 된다.
            Vector3 authoredDelta = yaw * Vector3.forward * (DashDistance / humanScale);
            WriteDashAxis(output, source, "RootT.x", authoredDelta.x);
            WriteDashAxis(output, source, "RootT.z", authoredDelta.z);
            var settings = AnimationUtility.GetAnimationClipSettings(output);
            settings.loopBlendPositionXZ = false;
            settings.keepOriginalPositionXZ = true;
            settings.loopBlendOrientation = true;
            settings.keepOriginalOrientation = false;
            AnimationUtility.SetAnimationClipSettings(output, settings);
            report.AppendLine(output.name + " | humanScale=" + humanScale.ToString("R", CultureInfo.InvariantCulture)
                + " | initialBodyYaw=" + yaw.eulerAngles.y.ToString("R", CultureInfo.InvariantCulture)
                + " | RootT delta=" + authoredDelta.ToString("F6") + " | 예상 월드 delta=(0,0,8), F15~21");
        }

        private static void WriteDashAxis(AnimationClip output, AnimationClip source, string property, float displacement)
        {
            EditorCurveBinding binding = Binding(source, property);
            float initial = AnimationUtility.GetEditorCurve(source, binding).Evaluate(0f);
            float velocity = displacement / (DashEnd - DashStart);
            var curve = new AnimationCurve(
                new Keyframe(0f, initial, 0f, 0f),
                new Keyframe(DashStart, initial, 0f, velocity),
                new Keyframe(DashEnd, initial + displacement, velocity, 0f),
                new Keyframe(source.length, initial + displacement, 0f, 0f));
            AnimationUtility.SetEditorCurve(output, binding, curve);
        }

        private static void WriteStationarySpin(AnimationClip output, AnimationClip source, StringBuilder report)
        {
            foreach (string property in new[] { "RootT.x", "RootT.z" })
            {
                EditorCurveBinding binding = Binding(source, property);
                float initial = AnimationUtility.GetEditorCurve(source, binding).Evaluate(0f);
                AnimationUtility.SetEditorCurve(output, binding, AnimationCurve.Constant(0f, source.length, initial));
            }
            // RootQ·근육은 원본 그대로 두고 회전 추출만 막아 몸의 회전 포즈를 보존한다.
            var settings = AnimationUtility.GetAnimationClipSettings(output);
            settings.loopBlendPositionXZ = true;
            settings.keepOriginalPositionXZ = true;
            settings.loopBlendOrientation = true;
            settings.keepOriginalOrientation = true;
            AnimationUtility.SetAnimationClipSettings(output, settings);
            report.AppendLine(output.name + " | 평면 RootT 상수, yaw 추출 차단, RootQ·근육·클립 체인 구간 보존");
        }

        private static void ValidateDerived(Plan plan, AnimationClip output)
        {
            if (!output.humanMotion || Mathf.Abs(output.length - plan.Source.length) > .001f)
                throw new InvalidOperationException("파생 클립의 Humanoid·길이 계약 불일치: " + output.name);
            EditorCurveBinding[] originalBindings = AnimationUtility.GetCurveBindings(plan.Source);
            foreach (EditorCurveBinding binding in originalBindings)
            {
                if (binding.type == typeof(Animator) && (binding.propertyName == "RootT.x" || binding.propertyName == "RootT.z")) continue;
                AnimationCurve source = AnimationUtility.GetEditorCurve(plan.Source, binding);
                AnimationCurve derived = AnimationUtility.GetEditorCurve(output, binding);
                if (!SameCurve(source, derived))
                    throw new InvalidOperationException("루트 이외 커브가 변했습니다: " + binding.path + " / " + binding.propertyName);
            }
            if (plan.IsDash)
            {
                Quaternion initialYaw = Yaw(ReadRotation(plan.Source, 0f));
                Vector3 before = RootPosition(output, DashStart), after = RootPosition(output, DashEnd);
                Vector3 expectedWorld = Quaternion.Inverse(initialYaw) * (after - before) * plan.HumanScale;
                expectedWorld.y = 0f;
                if (Vector3.Distance(expectedWorld, Vector3.forward * DashDistance) > .001f)
                    throw new InvalidOperationException("대시 좌표계·길이 검증 실패: " + output.name);
                if (Vector3.ProjectOnPlane(RootPosition(output, 0f) - before, Vector3.up).magnitude > .001f
                    || Vector3.ProjectOnPlane(RootPosition(output, output.length) - after, Vector3.up).magnitude > .001f)
                    throw new InvalidOperationException("대시 구간 밖 RootT가 이동합니다: " + output.name);
            }
            else
                foreach (string property in new[] { "RootT.x", "RootT.z" })
                {
                    AnimationCurve curve = RequireCurve(output, property);
                    float initial = curve.Evaluate(0f);
                    if (curve.keys.Any(key => Mathf.Abs(key.value - initial) > .00001f))
                        throw new InvalidOperationException("회전 공격의 평면 루트가 고정되지 않았습니다.");
                }
        }

        /// <summary>창의 Body 높이 커브와 기존 실제 모델 CSV를 대조한다. 클립과 연결은 변경하지 않는다.</summary>
        public static string InspectSpearBodyHeight()
        {
            CheckEditor();
            AnimationClip clip = ImportedClip(ClipRoot + "Spear/Spear_Signature.fbx");
            AnimationCurve curve = RequireCurve(clip, "RootT.y");
            float scale = HumanScale("Reine"), initial = curve.Evaluate(0f);
            var report = new StringBuilder("Spear RootT.y 공통 Body 이동 대조(미적용)\n");
            string csvPath = "docs/work-status/weapon-signature/sampling/Reine.csv";
            string[][] rows = File.Exists(csvPath) ? File.ReadLines(csvPath).Skip(1).Select(line => line.Split(',')).ToArray() : Array.Empty<string[]>();
            foreach (float time in new[] { 0f, .3f, .9f, 1.5f, 2f })
            {
                report.Append("t=").Append(time.ToString("F3", CultureInfo.InvariantCulture))
                    .Append(" | RootTy delta(m)=").Append(((curve.Evaluate(time) - initial) * scale).ToString("R", CultureInfo.InvariantCulture));
                string[] row = rows.OrderBy(value => Mathf.Abs(Parse(value[0]) - time)).FirstOrDefault();
                if (row != null)
                    report.Append(" | CSVhips=").Append(row[4]).Append(" | feet=").Append(row[5]).Append('/').Append(row[6]);
                report.AppendLine();
            }
            report.AppendLine("양발·골반에 같은 이동량이 남는지 확인한 뒤 BodyY 보정 여부를 결정합니다. 다리·손 근육은 변경하지 않았습니다.");
            return report.ToString();
        }

        private static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
        private static Vector3 RootPosition(AnimationClip clip, float time)
            => new(RequireCurve(clip, "RootT.x").Evaluate(time), RequireCurve(clip, "RootT.y").Evaluate(time), RequireCurve(clip, "RootT.z").Evaluate(time));
        private static Quaternion ReadRotation(AnimationClip clip, float time)
            => new Quaternion(RequireCurve(clip, "RootQ.x").Evaluate(time), RequireCurve(clip, "RootQ.y").Evaluate(time),
                RequireCurve(clip, "RootQ.z").Evaluate(time), RequireCurve(clip, "RootQ.w").Evaluate(time)).normalized;
        private static Quaternion Yaw(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            if (forward.x * forward.x + forward.z * forward.z < .000001f) throw new InvalidOperationException("초기 Body 방향이 수직이라 이동 좌표계를 확정할 수 없습니다.");
            return Quaternion.Euler(0f, Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 0f);
        }
        private static AnimationCurve RequireCurve(AnimationClip clip, string property)
            => AnimationUtility.GetEditorCurve(clip, Binding(clip, property)) ?? throw new InvalidOperationException(clip.name + ": 커브 없음 " + property);
        private static EditorCurveBinding Binding(AnimationClip clip, string property)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Animator) && binding.path == "" && binding.propertyName == property).ToArray();
            if (bindings.Length != 1) throw new InvalidOperationException(clip.name + ": Animator 커브 모호성 " + property);
            return bindings[0];
        }
        private static bool SameCurve(AnimationCurve source, AnimationCurve output)
        {
            if (source == null || output == null || source.length != output.length) return false;
            if (source.preWrapMode != output.preWrapMode || source.postWrapMode != output.postWrapMode) return false;
            return source.keys.SequenceEqual(output.keys);
        }

        private static AnimationClip ResolveOriginal(AnimationClip current, string outputPath)
        {
            if (current == null) throw new InvalidOperationException("원본 클립이 연결되지 않았습니다.");
            if (AssetDatabase.GetAssetPath(current) != outputPath) return current;
            AssetImporter importer = AssetImporter.GetAtPath(outputPath);
            SourceRecord record = !string.IsNullOrEmpty(importer?.userData) ? JsonUtility.FromJson<SourceRecord>(importer.userData) : null;
            if (record == null || string.IsNullOrEmpty(record.sourceGuid))
                throw new InvalidOperationException(outputPath + ": 파생 전 원본 GUID 기록이 없습니다.");
            string sourcePath = AssetDatabase.GUIDToAssetPath(record.sourceGuid);
            foreach (AnimationClip candidate in AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>())
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out string guid, out long localId)
                    && guid == record.sourceGuid && localId == record.sourceLocalId) return candidate;
            throw new InvalidOperationException(outputPath + ": 기록된 원본 GUID/localId를 해석할 수 없습니다.");
        }

        private static void StoreSourceRecord(Plan plan)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(plan.Source, out string guid, out long localId))
                throw new InvalidOperationException("원본 GUID/localId를 기록할 수 없습니다: " + plan.Source.name);
            AssetImporter importer = AssetImporter.GetAtPath(plan.Output);
            if (importer == null) throw new InvalidOperationException("파생 클립 importer 없음: " + plan.Output);
            string record = JsonUtility.ToJson(new SourceRecord { sourceGuid = guid, sourceLocalId = localId });
            Undo.RegisterCompleteObjectUndo(importer, "파생 클립 원본 GUID 기록");
            importer.userData = record;
            AssetDatabase.WriteImportSettingsIfDirty(plan.Output);
        }
        private static float HumanScale(string character)
        {
            string path = "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + character + ".prefab";
            Require<GameObject>(path);
            GameObject preview = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var animator = preview.GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid
                    || !float.IsFinite(animator.humanScale) || animator.humanScale <= 0f)
                    throw new InvalidOperationException(character + ": 유효한 프리뷰 Humanoid humanScale 없음");
                return animator.humanScale;
            }
            finally { PrefabUtility.UnloadPrefabContents(preview); }
        }
        private static AnimationClip ImportedClip(string path)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__", StringComparison.Ordinal)).ToArray();
            if (clips.Length != 1) throw new InvalidOperationException(path + ": 단일 입력 클립이 필요합니다.");
            return clips[0];
        }
        private static T Require<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("필수 에셋 없음: " + path);
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void CheckEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일을 마친 Edit Mode에서 실행합니다.");
        }
    }
}
