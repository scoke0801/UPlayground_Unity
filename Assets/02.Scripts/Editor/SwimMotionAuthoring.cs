using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 수영 모션 슬롯(Idle/Move/Fast)을 공용 인간형 플레이어 모션셋에 연결한다.
    /// 무기별 세트는 NoWeapon 세트로 폴백하므로 공용 세트 한 곳에 넣으면 모든 플레이어블 캐릭터가 같은 모션을 쓴다.
    /// </summary>
    public static class SwimMotionAuthoring
    {
        private const string OutputFolder = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Swim";
        private const string CommonSetPath = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Humanoid_Player_CommonAnimationSet.asset";
        private const string DerivedClipFolder = "Assets/10.Datas/Actor/Animation/Clips/Swim";
        private const float DiveMovePitch = 80f;
        private const float DiveDescendPitch = 150f;
        private const float ResampleFramesPerSecond = 30f;
        private const string ClipFolder ="Assets/ExternalAssets/AnimationOnly/Platform_Animation/Animation/50_Adventure/";

        private readonly struct SlotSource
        {
            public readonly GameplayTag Slot;
            public readonly string AssetName;
            public readonly string FbxName;
            /// <summary>NaN이 아니면 원본 클립의 몸통 기울기를 이 각도(전방 숙임, 도)로 바꾼 파생 클립을 쓴다.</summary>
            public readonly float DerivedPitchDegrees;

            public SlotSource(GameplayTag slot, string assetName, string fbxName, float derivedPitchDegrees = float.NaN)
            {
                Slot = slot;
                AssetName = assetName;
                FbxName = fbxName;
                DerivedPitchDegrees = derivedPitchDegrees;
            }
        }

        [UPlaygroundTool("Animation.SwimMotion.Author")]
        public static void Author()
        {
            var sources = new[]
            {
                new SlotSource(MotionTags.SwimIdle, "Player_Swim_Idle", "Stander@Swim_Idle.FBX"),
                new SlotSource(MotionTags.SwimMove, "Player_Swim_Move", "Stander@Swim_Walk_F.FBX"),
                new SlotSource(MotionTags.SwimFast, "Player_Swim_Fast", "Stander@Swim_Run.FBX"),
                // 수중 자세: 지상형 클립은 몸이 서 있으므로 몸통 기울기만 바꿔 엎드린/머리 아래 자세를 만든다.
                new SlotSource(MotionTags.SwimDiveMove, "Player_Swim_DiveMove", "Stander@Swim_Run.FBX", DiveMovePitch),
                new SlotSource(MotionTags.SwimDiveDescend, "Player_Swim_DiveDescend", "Stander@Swim_Run.FBX", DiveDescendPitch),
            };

            var commonSet = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(CommonSetPath);
            if (commonSet == null)
            {
                Debug.LogError($"[SwimMotion] 공용 모션셋을 찾을 수 없습니다: {CommonSetPath}");
                return;
            }

            EnsureTagsRegistered();
            EnsureFolder(OutputFolder);
            commonSet.motionSlots ??= new AYellowpaper.SerializedCollections.SerializedDictionary<GameplayTag, MotionSetAsset>();
            foreach (SlotSource source in sources)
            {
                MotionSetAsset asset = LoadOrCreateAsset(source);
                if (asset == null) return;
                commonSet.motionSlots[source.Slot] = asset;
            }

            EditorUtility.SetDirty(commonSet);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SwimMotion] 수영 모션 {sources.Length}개를 공용 모션셋에 연결했습니다.");
        }

        /// <summary>모든 플레이어 모션셋이 무기와 무관하게 수영 모션을 해석하는지 확인하고 결과 문자열을 돌려준다.</summary>
        [UPlaygroundTool("Animation.SwimMotion.Verify")]
        public static void Verify()
        {
            var tags = new[] { MotionTags.SwimIdle, MotionTags.SwimMove, MotionTags.SwimFast, MotionTags.SwimDiveMove, MotionTags.SwimDiveDescend };
            var missing = new System.Text.StringBuilder();
            int checkedCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:PlayerActorAnimationMotionSet"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var set = AssetDatabase.LoadAssetAtPath<PlayerActorAnimationMotionSet>(path);
                if (set == null) continue;
                checkedCount++;
                foreach (WeaponType weapon in set.motionSets.Keys.ToList())
                foreach (GameplayTag tag in tags)
                    if (set.GetMotionSetAsset(weapon, tag) == null)
                        missing.AppendLine($"  {path} [{weapon}] {tag.TagName}");
            }

            if (missing.Length == 0)
                Debug.Log($"[SwimMotion] 검증 통과: 플레이어 모션셋 {checkedCount}개 모두 수영 모션 해석 가능");
            else
                Debug.LogWarning($"[SwimMotion] 수영 모션을 해석하지 못하는 조합:\n{missing}");
        }

        /// <summary>코드 정의 태그는 레지스트리에 있어야 유효하다. 없으면 모션 해석이 조용히 실패한다.</summary>
        private static void EnsureTagsRegistered()
        {
            var registry = Resources.Load<GameplayTagRegistrySO>("GameplayTagRegistry");
            if (registry == null)
            {
                Debug.LogError("[SwimMotion] Resources/GameplayTagRegistry.asset을 찾을 수 없습니다.");
                return;
            }

            var definitions = new (string name, string description)[]
            {
                (GameplayTags.State_Swim.TagName, "수영 중"),
                (MotionTags.SwimIdle.TagName, "수영 제자리"),
                (MotionTags.SwimMove.TagName, "수영 이동"),
                (MotionTags.SwimFast.TagName, "빠른 수영 이동"),
                (MotionTags.SwimDiveMove.TagName, "잠수 중 엎드려 이동"),
                (MotionTags.SwimDiveDescend.TagName, "잠수 중 머리를 아래로 하강"),
            };
            foreach ((string name, string description) in definitions)
            {
                if (registry.TryGetDefinition(name, out _)) continue;
                registry.tags.Add(new GameplayTagDefinition { tagName = name, description = description, color = new Color(0.25f, 0.6f, 0.9f) });
            }

            registry.RebuildLookup();
            EditorUtility.SetDirty(registry);
        }

        private static MotionSetAsset LoadOrCreateAsset(SlotSource source)
        {
            AnimationClip clip = LoadClip(ClipFolder + source.FbxName);
            if (clip != null && !float.IsNaN(source.DerivedPitchDegrees))
                clip = LoadOrCreateDerivedClip(clip, source.AssetName, source.DerivedPitchDegrees);
            if (clip == null)
            {
                Debug.LogError($"[SwimMotion] 클립을 찾을 수 없습니다: {ClipFolder + source.FbxName}");
                return null;
            }

            string path = $"{OutputFolder}/{source.AssetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MotionSetAsset>();
                asset.motionSet = MotionSet.CreateAuthored(source.AssetName, MotionSectionEndPolicy.LoopSelf);
                AssetDatabase.CreateAsset(asset, path);
            }

            if (asset.motionSet.motions.Count == 0)
                asset.motionSet.motions.Add(new UPlayGround.Animation.Motion());

            UPlayGround.Animation.Motion motion = asset.motionSet.motions[0];
            motion.motionClip = clip;
            motion.motionName = clip.name;
            motion.playbackSpeed = 1f;
            motion.clipStartTime = -1f;
            motion.clipEndTime = -1f;
            asset.motionSet.internalBlendDuration = 0.1f;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>
        /// 원본 휴머노이드 클립을 복사해 몸통(RootQ)의 앞뒤 기울기만 목표 각도로 바꾼 클립을 만든다.
        /// 방향(요)은 유지하고 기울기를 걸어 두 축을 분리하므로 이동 방향이 틀어지지 않는다.
        /// </summary>
        private static AnimationClip LoadOrCreateDerivedClip(AnimationClip source, string name, float targetPitch)
        {
            EnsureFolder(DerivedClipFolder);
            string path = $"{DerivedClipFolder}/{name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = Object.Instantiate(source);
                AssetDatabase.CreateAsset(clip, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, clip);
            }

            clip.name = name;
            var bindings = AnimationUtility.GetCurveBindings(source);
            EditorCurveBinding qx = FindBinding(bindings, "RootQ.x"), qy = FindBinding(bindings, "RootQ.y"),
                qz = FindBinding(bindings, "RootQ.z"), qw = FindBinding(bindings, "RootQ.w");
            AnimationCurve cx = AnimationUtility.GetEditorCurve(source, qx), cy = AnimationUtility.GetEditorCurve(source, qy),
                cz = AnimationUtility.GetEditorCurve(source, qz), cw = AnimationUtility.GetEditorCurve(source, qw);
            if (cx == null || cy == null || cz == null || cw == null)
            {
                Debug.LogError($"[SwimMotion] RootQ 곡선이 없어 파생 클립을 만들 수 없습니다: {source.name}");
                return null;
            }

            int frameCount = Mathf.Max(2, Mathf.CeilToInt(source.length * ResampleFramesPerSecond) + 1);
            var samples = new Quaternion[frameCount];
            var times = new float[frameCount];
            float basePitch = 0f;
            for (int i = 0; i < frameCount; i++)
            {
                times[i] = Mathf.Min(source.length, i / ResampleFramesPerSecond);
                samples[i] = new Quaternion(cx.Evaluate(times[i]), cy.Evaluate(times[i]), cz.Evaluate(times[i]), cw.Evaluate(times[i])).normalized;
                SplitYaw(samples[i], out _, out Quaternion tilt);
                basePitch += Mathf.DeltaAngle(0f, tilt.eulerAngles.x) / frameCount;
            }

            Quaternion pitchOffset = Quaternion.AngleAxis(targetPitch - basePitch, Vector3.right);
            var outX = new AnimationCurve(); var outY = new AnimationCurve(); var outZ = new AnimationCurve(); var outW = new AnimationCurve();
            Quaternion previous = Quaternion.identity;
            for (int i = 0; i < frameCount; i++)
            {
                SplitYaw(samples[i], out Quaternion yaw, out Quaternion tilt);
                Quaternion result = yaw * pitchOffset * tilt;
                // 이웃 키가 반구를 넘나들면 보간이 뒤집히므로 부호를 맞춘다.
                if (i > 0 && Quaternion.Dot(previous, result) < 0f)
                    result = new Quaternion(-result.x, -result.y, -result.z, -result.w);
                previous = result;
                outX.AddKey(times[i], result.x); outY.AddKey(times[i], result.y);
                outZ.AddKey(times[i], result.z); outW.AddKey(times[i], result.w);
            }

            foreach (AnimationCurve curve in new[] { outX, outY, outZ, outW })
                for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);

            AnimationUtility.SetEditorCurve(clip, qx, outX);
            AnimationUtility.SetEditorCurve(clip, qy, outY);
            AnimationUtility.SetEditorCurve(clip, qz, outZ);
            AnimationUtility.SetEditorCurve(clip, qw, outW);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.wrapMode = WrapMode.Loop;
            EditorUtility.SetDirty(clip);
            return clip;
        }

        /// <summary>회전을 위쪽 축 비틀림(요)과 나머지 기울기로 나눈다. 회전 = yaw * tilt.</summary>
        private static void SplitYaw(Quaternion rotation, out Quaternion yaw, out Quaternion tilt)
        {
            var twist = new Quaternion(0f, rotation.y, 0f, rotation.w);
            yaw = twist.x * twist.x + twist.y * twist.y + twist.z * twist.z + twist.w * twist.w < 1e-8f
                ? Quaternion.identity
                : Quaternion.Normalize(twist);
            tilt = Quaternion.Inverse(yaw) * rotation;
        }

        private static EditorCurveBinding FindBinding(EditorCurveBinding[] bindings, string propertyName) =>
            bindings.FirstOrDefault(binding => binding.propertyName == propertyName);

        private static AnimationClip LoadClip(string fbxPath) =>
            AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview"));

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
