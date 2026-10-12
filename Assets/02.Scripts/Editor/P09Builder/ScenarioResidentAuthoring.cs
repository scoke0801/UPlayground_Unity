using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>주민 제작에 쓸 실제 P09 얼굴과 머리를 저장 없이 비교 촬영한다.</summary>
    public static partial class ScenarioResidentAuthoring
    {
        private const string Output = "output/ScenarioGameplay/P03";
        /// <summary>외형 카탈로그를 촬영하고 선택 근거를 남긴다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/시나리오 주민 외형 조사")]
        public static void Inspect()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("편집 모드 컴파일 완료 후 실행하세요.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("미저장 씬이 있어 조사를 중단합니다.");
            Directory.CreateDirectory(Output + "/Catalog");
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            foreach (BuilderSex sex in Enum.GetValues(typeof(BuilderSex)))
            {
                var config = new CharacterBuildConfig { Sex = sex, UseMagicaCloth = false };
                config.HairStyleSo = catalog.HairStyles.First();
                config.HairColorSo = catalog.HairColors.First();
                config.SkinColorSo = (sex == BuilderSex.Male ? catalog.SkinColorsMale : catalog.SkinColorsFemale).First();
                config.EyeColorSo = catalog.EyeColors.First();
                ArmorIndexPresetUtility.Apply(config.ArmorSelections, ArmorIndexPresetUtility.Build(catalog).First(x => x.Index == 2));
                config.ArmorSelections.Set(BuilderArmorSlot.Head, null);
                foreach (var face in catalog.FaceTypes)
                {
                    config.FaceTypeSo = face;
                    CaptureConfig(config, catalog, sex + "_" + face.name);
                }
                if (sex == BuilderSex.Female)
                    foreach (var hair in catalog.HairStyles)
                    {
                        config.HairStyleSo = hair;
                        CaptureConfig(config, catalog, "Hair_" + hair.name);
                    }
            }
            File.WriteAllText(Output + "/Catalog/complete.txt", "얼굴 6종과 머리 14종 촬영 완료. 에셋·씬 저장 없음.");
        }

        private static void CaptureConfig(CharacterBuildConfig config, P09AssetCatalog catalog, string name)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PathConfig.GetBasePrefabPath(config.Sex, false)));
            try
            {
                AppearanceApplier.Apply(root, config, catalog);
                ApplyWeaponStep.Apply(root, config, catalog);
                Capture(root, Output + "/Catalog/" + name + ".png", true, 0);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void Capture(GameObject root, string path, bool face, float yaw, bool sideLight = false, bool warmup = false)
        {
            var preview = new PreviewRenderUtility();
            try
            {
                preview.AddSingleGO(root);
                var renderers = root.GetComponentsInChildren<Renderer>().Where(x => x.enabled).ToArray();
                if (renderers.Length == 0) throw new InvalidOperationException("표시할 메시가 없습니다.");
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                var target = bounds.center;
                float distance = bounds.size.y * 2.1f;
                if (face)
                {
                    var animator = root.GetComponentInChildren<Animator>();
                    var head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
                    target = head != null ? head.position + Vector3.up * 0.08f : bounds.center + Vector3.up * bounds.extents.y * 0.75f;
                    distance = 0.95f;
                }
                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.22f, 0.23f, 0.25f);
                camera.fieldOfView = 30;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100;
                camera.transform.position = target + Quaternion.Euler(0, yaw, 0) * root.transform.forward * distance;
                camera.transform.LookAt(target);
                preview.lights[0].intensity = 1.3f;
                preview.lights[0].transform.rotation = camera.transform.rotation * Quaternion.Euler(25, sideLight ? -80 : -25, 0);
                preview.lights[1].intensity = sideLight ? 0.2f : 0.8f;
                preview.ambientColor = sideLight ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.55f, 0.55f, 0.55f);
                if (warmup)
                {
                    // 첫 GPU 업로드의 임시 눈 텍스처가 검수 이미지에 남지 않게 한다.
                    preview.BeginStaticPreview(new Rect(0, 0, 512, 640));
                    preview.Render(true);
                    UnityEngine.Object.DestroyImmediate(preview.EndStaticPreview());
                }
                preview.BeginStaticPreview(new Rect(0, 0, 512, 640));
                preview.Render(true);
                var texture = preview.EndStaticPreview();
                try { File.WriteAllBytes(path, texture.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            finally { preview.Cleanup(); }
        }
    }
}
