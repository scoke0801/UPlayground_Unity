using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.EditorTools
{
    /// <summary>Blender 출력만 임포트하고 구매 원본·Avatar·기존 실행 매핑은 보존한다.</summary>
    public static class WeaponSignatureClipImport
    {
        private const string Root = "Assets/07.Animations/Player/Signature";
        private const string RaonSource = "Assets/07.Animations/Player/Raon/FrankWeapon/Raon_Frank_RPG_Katana_S3_Skill02.anim";

        [Serializable] private sealed class Card { public string weapon; public string source; public float duration; }

        public static string Import()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("정상 편집 모드의 컴파일 완료 후 임포트합니다.");
            var report = new List<string>();
            foreach (string weapon in new[] { "Bow", "Staff", "Spear", "SwordShield", "Katana" })
            {
                string sourceFolder = "ArtSource/Animation/Signature/" + weapon;
                Card card = JsonUtility.FromJson<Card>(File.ReadAllText(sourceFolder + "/production-card.json"));
                string source = sourceFolder + "/" + weapon + "_Signature.fbx";
                string folder = Root + "/" + weapon;
                Directory.CreateDirectory(folder);
                string path = folder + "/" + weapon + "_Signature.fbx";
                if (File.Exists(path))
                {
                    if (!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(path)))
                        throw new InvalidOperationException("기존 임포트와 제작본이 다릅니다. 변경 검토 필요: " + path);
                }
                else File.Copy(source, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var original = AssetImporter.GetAtPath(card.source) as ModelImporter;
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                Avatar avatar = original.sourceAvatar != null ? original.sourceAvatar
                    : AssetDatabase.LoadAllAssetsAtPath(card.source).OfType<Avatar>().FirstOrDefault();
                if (avatar == null || !avatar.isHuman || !avatar.isValid)
                    throw new InvalidOperationException("원본 Avatar를 확인할 수 없습니다: " + card.source);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                if (weapon == "Spear" || weapon == "SwordShield" || weapon == "Katana")
                    ConfigureFrankAvatar(importer, original, path, card.source);
                importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation = true;
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.optimizeGameObjects = false;
                var clip = importer.defaultClipAnimations.Single();
                clip.name = weapon + "_Signature";
                clip.lastFrame = clip.firstFrame + card.duration * 30f;
                clip.loopTime = false;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = weapon != "Katana";
                importer.clipAnimations = new[] { clip };
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                AnimationClip imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__"));
                if (!imported.humanMotion || Mathf.Abs(imported.length - card.duration) > 1f / 30f)
                    throw new InvalidOperationException("Humanoid/길이 불일치: " + path);
                report.Add(weapon + " | " + imported.length + "s | " + imported.frameRate + "fps | " + AssetDatabase.GetAssetPath(avatar));
            }
            CreateRaonClip();
            Directory.CreateDirectory("docs/work-status/weapon-signature");
            File.WriteAllLines("docs/work-status/weapon-signature/CLIP_IMPORT.txt", report);
            return string.Join("\n", report);
        }

        private static void ConfigureFrankAvatar(ModelImporter importer, ModelImporter original, string path, string sourcePath)
        {
            // Blender FBX의 추가 Armature 노드는 공유 Avatar의 경로 계약과 다르다.
            // 원본의 명시적 Human 매핑과 T포즈 보정을 새 bind 좌표계로 옮긴다.
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.sourceAvatar = null;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.clipAnimations = Array.Empty<ModelImporterClipAnimation>();
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var outputRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var sourceRoot = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (outputRoot == null || sourceRoot == null) throw new InvalidOperationException("새 FBX bind 계층을 읽지 못했습니다: " + path);
            var originalPose = original.humanDescription.skeleton.ToDictionary(b => b.name, b => b);
            var originalBind = sourceRoot.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
            var transforms = outputRoot.GetComponentsInChildren<Transform>(true);
            HumanDescription description = original.humanDescription;
            description.skeleton = transforms.Select(t =>
            {
                Matrix4x4 pose = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
                if (originalPose.TryGetValue(t.name, out SkeletonBone targetPose) && originalBind.TryGetValue(t.name, out Transform bind))
                {
                    Matrix4x4 bindMatrix = Matrix4x4.TRS(bind.localPosition, bind.localRotation, bind.localScale);
                    pose = pose * bindMatrix.inverse * Matrix4x4.TRS(targetPose.position, targetPose.rotation, targetPose.scale);
                }
                return new SkeletonBone { name = t.name, position = t.localPosition, rotation = pose.rotation, scale = t.localScale };
            }).ToArray();
            // Generic 임포트는 Blender Armature 래퍼를 생략하지만 Humanoid는 이 노드를 복원한다.
            if (!description.skeleton.Any(b => b.name == "Armature"))
            {
                var skeleton = description.skeleton.ToList();
                skeleton.Insert(1, new SkeletonBone { name = "Armature", rotation = Quaternion.identity, scale = Vector3.one });
                description.skeleton = skeleton.ToArray();
            }
            foreach (HumanBone human in description.human)
                if (!transforms.Any(t => t.name == human.boneName))
                    throw new InvalidOperationException("Human 본 누락: " + human.boneName);
            importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.humanDescription = description;
            importer.importAnimation = true;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var generated = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (generated == null || !generated.isHuman || !generated.isValid)
                throw new InvalidOperationException("독립 Avatar 검증 실패: " + path);
        }

        private static void CreateRaonClip()
        {
            string path = Root + "/Katana/Katana_Raon_Signature.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) return;
            var body = AssetDatabase.LoadAllAssetsAtPath(Root + "/Katana/Katana_Signature.fbx")
                .OfType<AnimationClip>().Single(c => !c.name.StartsWith("__"));
            var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(RaonSource);
            if (source == null) throw new InvalidOperationException("라온 검·칼집 이식 원본 누락");
            var result = UnityEngine.Object.Instantiate(body);
            result.name = "Katana_Raon_Signature";
            foreach (var binding in AnimationUtility.GetCurveBindings(result).Where(b => b.type == typeof(Transform)))
                AnimationUtility.SetEditorCurve(result, binding, null);
            var bindings = AnimationUtility.GetCurveBindings(source).Where(b => b.type == typeof(Transform)
                && (b.path.StartsWith("Armature/") || b.path.StartsWith("Weapon/"))).ToArray();
            if (!bindings.Any(b => b.path.EndsWith("/Weapon_Blade")))
                throw new InvalidOperationException("라온 Weapon_Blade 이식 커브 누락");
            foreach (var binding in bindings)
            {
                AnimationCurve input = AnimationUtility.GetEditorCurve(source, binding);
                var output = new AnimationCurve();
                for (int frame = 0; frame <= 51; frame++)
                {
                    float time = SourceTime(frame);
                    float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01((frame - 30f) / 21f));
                    float value;
                    if (binding.propertyName.StartsWith("m_LocalRotation."))
                    {
                        Quaternion from = Rotation(source, binding.path, time);
                        Quaternion to = Rotation(source, binding.path, 207f / 60f);
                        Quaternion rotation = Quaternion.Slerp(from, to, progress);
                        value = binding.propertyName.EndsWith(".x") ? rotation.x
                            : binding.propertyName.EndsWith(".y") ? rotation.y
                            : binding.propertyName.EndsWith(".z") ? rotation.z : rotation.w;
                    }
                    else value = Mathf.Lerp(input.Evaluate(time), input.Evaluate(207f / 60f), progress);
                    output.AddKey(frame / 30f, value);
                }
                AnimationUtility.SetEditorCurve(result, binding, output);
            }
            result.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(result, path);
        }

        private static float SourceTime(float frame)
        {
            float[] output = { 0, 12, 15, 21, 24 };
            float[] source = { 1, 65, 75, 82, 86 };
            for (int i = 1; i < output.Length; i++)
                if (frame <= output[i])
                    return (Mathf.Lerp(source[i - 1], source[i], Mathf.InverseLerp(output[i - 1], output[i], frame)) - 1) / 60f;
            return 85f / 60f;
        }

        private static Quaternion Rotation(AnimationClip source, string path, float time)
        {
            float Value(string axis, float fallback)
            {
                var curve = AnimationUtility.GetEditorCurve(source, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + axis));
                return curve != null ? curve.Evaluate(time) : fallback;
            }
            return new Quaternion(Value("x", 0), Value("y", 0), Value("z", 0), Value("w", 1)).normalized;
        }
    }
}
