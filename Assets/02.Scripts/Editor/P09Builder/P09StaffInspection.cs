using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>남녀·물리 옵션·스태프별 손 연결을 검사하고 기존 클립 자세를 촬영한다.</summary>
    public static class P09StaffInspection
    {
        public const string ToolId = "UPlayGround/캐릭터/P09/스태프 손 연결 검증";
        private const string Output = "Logs/P09Staff";
        private static readonly string[] MotionNames = { "Idle", "Attack_1", "Attack_2", "Attack_3", "HeavyAttack_1", "Guard", "Skill_1", "Skill_2", "Skill_3", "Skill_4" };

        /// <summary>원본 에셋을 저장하지 않고 연결·보정 보존과 Idle·전투 자세를 검사한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool(ToolId, priority = 1103)]
        public static void Inspect()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("편집 모드에서 실행하세요.");
            Directory.CreateDirectory(Output);
            var report = new StringBuilder();
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            Require(catalog.Staves.Count == 4, "스태프 4종 카탈로그 누락");
            Require(AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(P09StaffAttachment.MotionSetPath) != null, "스태프 모션 세트 누락");
            int cases = 0;
            foreach (var sex in new[] { BuilderSex.Male, BuilderSex.Female })
            foreach (bool cloth in new[] { false, true })
            foreach (var staffData in catalog.Staves)
            {
                var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PathConfig.GetBasePrefabPath(sex, cloth)));
                try
                {
                    var config = new CharacterBuildConfig { Sex = sex, UseMagicaCloth = cloth, StaffSo = staffData };
                    AppearanceApplier.Apply(root, config, catalog);
                    ApplyWeaponStep.Apply(root, config, catalog);
                    var staff = P09StaffAttachment.FindActive(root);
                    Require(staff != null, "스태프 슬롯 누락");
                    Require(staff.GetComponentsInChildren<Renderer>().Count(r=>r.enabled) == 1, "스태프 중복 활성화");
                    var binding = staff.GetComponent<WeaponSocketBinding>();
                    Require(binding != null && binding.weaponType == WeaponType.Staff && binding.Constraint == staff, "명시적 바인딩 누락");
                    var hand = root.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
                    Require(staff.GetSource(0).sourceTransform.IsChildOf(hand), "오른손 본 연결 오류");
                    var backPosition = staff.GetTranslationOffset(1);
                    var backRotation = staff.GetRotationOffset(1);
                    var handPosition = staff.GetSource(0).sourceTransform.localPosition;
                    config.OverrideStaffGrip = true;
                    config.StaffGripPosition = new Vector3(.01f,.02f,.03f);
                    config.StaffGripRotation = new Vector3(5,10,15);
                    ApplyWeaponStep.Apply(root, config, catalog);
                    ApplyWeaponStep.Apply(root, config, catalog);
                    Require(staff.GetTranslationOffset(0) == config.StaffGripPosition && staff.GetRotationOffset(0) == config.StaffGripRotation, "보정 재적용 누적 오류");
                    Require(staff.GetTranslationOffset(1) == backPosition && staff.GetRotationOffset(1) == backRotation, "등 보정 변경");
                    Require(staff.GetSource(0).sourceTransform.localPosition == handPosition, "공용 손 소켓 변경");
                    Require(staff.GetComponents<WeaponSocketBinding>().Length == 1, "바인딩 중복 생성");
                    if (!cloth && staffData == catalog.Staves[0])
                        VerifySerialization(root, config);
                    config.StaffGripPosition = Vector3.zero;
                    config.StaffGripRotation = Vector3.zero;
                    ApplyWeaponStep.Apply(root, config, catalog);
                    report.AppendLine($"PASS {sex}, cloth={cloth}, {staffData.name}, hand={hand.name}");
                    cases++;
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            foreach (var sex in new[] { BuilderSex.Male, BuilderSex.Female })
                Capture(sex, catalog, report);
            report.AppendLine($"PASS {cases} attachment cases; preview frames are sampled poses, not Play Mode.");
            File.WriteAllText(Output+"/inspection.txt",report.ToString());
            Debug.Log(report.ToString());
        }

        private static void Capture(BuilderSex sex, P09AssetCatalog catalog, StringBuilder report)
        {
            var preview = new PreviewRenderUtility();
            var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PathConfig.GetBasePrefabPath(sex, false)));
            preview.AddSingleGO(instance);
            var config = new CharacterBuildConfig { Sex = sex, UseMagicaCloth = false, StaffSo = catalog.Staves[0] };
            AppearanceApplier.Apply(instance, config, catalog);
            ApplyWeaponStep.Apply(instance, config, catalog);
            var staff = P09StaffAttachment.FindActive(instance);
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = 1.15f;
            preview.camera.nearClipPlane = .01f;
            preview.camera.farClipPlane = 30;
            preview.camera.backgroundColor = new Color(.13f,.15f,.18f);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.lights[0].intensity = 1.3f;
            preview.lights[0].transform.eulerAngles = new Vector3(30,30,0);
            preview.lights[1].intensity = .8f;
            var sheet = new Texture2D(5*360, MotionNames.Length*400, TextureFormat.RGB24, false);
            var pose = new P09PosePreview(instance);
            try
            {
                for (int row=0; row<MotionNames.Length; row++)
                {
                    var motion = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(P09StaffAttachment.MotionRoot+MotionNames[row]+".asset");
                    Require(motion != null && !SerializationUtility.HasManagedReferencesWithMissingTypes(motion), "모션/이벤트 타입 누락: "+MotionNames[row]);
                    var clip = motion.motionSet.motions[0].motionClip;
                    Require(clip != null, "클립 누락: "+MotionNames[row]);
                    report.AppendLine($"{sex} {MotionNames[row]}: {clip.name} {clip.length}");
                    for (int col=0; col<5; col++)
                    {
                        pose.Sample(motion, col/5f, staff);
                        preview.camera.transform.position = new Vector3(2.4f,1.8f,3.5f);
                        preview.camera.transform.LookAt(new Vector3(0,1.1f,0));
                        preview.BeginStaticPreview(new Rect(0,0,360,400));
                        preview.Render(true);
                        var frame = preview.EndStaticPreview();
                        sheet.SetPixels(col*360,(MotionNames.Length-1-row)*400,360,400,frame.GetPixels());
                        UnityEngine.Object.DestroyImmediate(frame);
                    }
                }
                sheet.Apply();
                File.WriteAllBytes($"{Output}/{sex}.png",sheet.EncodeToPNG());
                CaptureGrip(preview, pose, staff, sex);
            }
            finally { pose.Dispose(); preview.Cleanup(); UnityEngine.Object.DestroyImmediate(sheet); }
        }

        private static void VerifySerialization(GameObject root, CharacterBuildConfig config)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/P09StaffSerializationCheck.prefab");
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                Require(success, "임시 프리팹 저장 실패");
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var binding = saved.GetComponentsInChildren<WeaponSocketBinding>(true).Single(b => b.weaponType == WeaponType.Staff);
                Require(binding.Constraint.GetTranslationOffset(0) == config.StaffGripPosition, "프리팹 손 위치 보정 유실");
                Require(binding.Constraint.GetRotationOffset(0) == config.StaffGripRotation, "프리팹 손 회전 보정 유실");
                var restored = JsonUtility.FromJson<CharacterBuildConfig>(JsonUtility.ToJson(config));
                Require(restored.OverrideStaffGrip && restored.StaffGripPosition == config.StaffGripPosition &&
                    restored.StaffGripRotation == config.StaffGripRotation, "프리셋 손 보정 직렬화 유실");
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        private static void CaptureGrip(PreviewRenderUtility preview, P09PosePreview pose, ParentConstraint staff, BuilderSex sex)
        {
            var names = new[] { "Idle", "Attack_1", "HeavyAttack_1", "Guard", "Skill_1" };
            var sheet = new Texture2D(6 * 300, names.Length * 300, TextureFormat.RGB24, false);
            try
            {
                preview.camera.orthographicSize = .23f;
                for (int row = 0; row < names.Length; row++)
                {
                    var motion = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(P09StaffAttachment.MotionRoot + names[row] + ".asset");
                    for (int col = 0; col < 6; col++)
                    {
                        pose.Sample(motion, (col % 3) * .4f, staff);
                        var target = staff.GetSource(0).sourceTransform.position;
                        preview.camera.transform.position = target + (col < 3 ? Vector3.forward : Vector3.right) * 3f;
                        preview.camera.transform.LookAt(target);
                        preview.BeginStaticPreview(new Rect(0, 0, 300, 300));
                        preview.Render(true);
                        var frame = preview.EndStaticPreview();
                        sheet.SetPixels(col * 300, (names.Length - 1 - row) * 300, 300, 300, frame.GetPixels());
                        UnityEngine.Object.DestroyImmediate(frame);
                    }
                }
                sheet.Apply();
                File.WriteAllBytes($"{Output}/{sex}-grip.png", sheet.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(sheet); }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
