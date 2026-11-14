#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Cinematic;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Editor
{
    /// <summary>최초 저작용 기본값을 SO에 저장한다. 기존 에셋은 덮어쓰지 않는다.</summary>
    public static class WeaponPresentationAssetBuilder
    {
        public const string Root = "Assets/10.Datas/Combat/WeaponPresentation";

        /// <summary>Sword/GreatSword의 생성 파이프라인을 먼저 준비한다.</summary>
        public static void CreateVerticalSlice()
        {
            Create(WeaponType.Sword, CinematicStageTier.CameraOnly, 0.72f, 0.045f, 0.8f, 0.12f, new Color(0.55f, 0.78f, 0.88f));
            Create(WeaponType.GreatSword, CinematicStageTier.CasterClone, 0.78f, 0.09f, 1.5f, 0.22f, new Color(0.85f, 0.64f, 0.36f));
        }

        /// <summary>나머지 8무기는 확정 모션을 발명하지 않고 표현 템플릿만 만든다.</summary>
        public static void CreateRemainingProfiles()
        {
            Create(WeaponType.SwordShield, CinematicStageTier.CameraOnly, 0.72f, 0.06f, 1f, 0.16f, new Color(0.48f, 0.7f, 0.8f));
            Create(WeaponType.Katana, CinematicStageTier.CasterClone, 0.8f, 0.025f, 0.45f, 0.08f, new Color(0.65f, 0.85f, 0.7f));
            Create(WeaponType.DualBlade, CinematicStageTier.CasterClone, 0.74f, 0.035f, 0.65f, 0.1f, new Color(0.55f, 0.8f, 0.83f));
            Create(WeaponType.DoubleAxe, CinematicStageTier.CasterClone, 0.78f, 0.1f, 1.6f, 0.24f, new Color(0.9f, 0.38f, 0.28f));
            Create(WeaponType.Spear, CinematicStageTier.CameraOnly, 0.7f, 0.03f, 0.5f, 0.09f, new Color(0.65f, 0.8f, 0.86f));
            Create(WeaponType.Bow, CinematicStageTier.CasterClone, 0.76f, 0.035f, 0.6f, 0.1f, new Color(0.58f, 0.83f, 0.5f));
            Create(WeaponType.Staff, CinematicStageTier.CasterClone, 0.78f, 0.07f, 1.1f, 0.18f, new Color(0.6f, 0.7f, 0.95f));
            Create(WeaponType.Whip, CinematicStageTier.CasterClone, 0.76f, 0.045f, 0.8f, 0.13f, new Color(0.85f, 0.5f, 0.65f));
        }

        private static void Create(WeaponType weapon, CinematicStageTier tier, float finish,
            float freeze, float strength, float shakeDuration, Color accent)
        {
            Directory.CreateDirectory(Root);
            string path = $"{Root}/WeaponPresentation_{weapon}.asset";
            if (AssetDatabase.LoadAssetAtPath<WeaponPresentationProfileSO>(path) != null)
                return;
            if (File.Exists(path) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                throw new InvalidOperationException($"다른 기존 파일을 덮어쓸 수 없습니다: {path}");
            var profile = ScriptableObject.CreateInstance<WeaponPresentationProfileSO>();
            profile.weaponType = weapon;
            profile.defaultTier = tier;
            profile.accentColor = accent;
            profile.suggestedFreezeDuration = freeze;
            profile.motionReview = "모션은 기존 Ability MotionKey가 소유합니다. 신규 동작 연결은 후보 클립 검수 후 저작합니다.";
            if (weapon == WeaponType.Whip)
                profile.motionReview = "비접촉 궤적형 T2 초안. 속박·던지기 접촉/T3는 사용자 결정 전 보류합니다.";
            float[] positions = { 0f, 0.08f, 0.24f, finish, 0.95f };
            for (int i = 0; i < positions.Length; i++)
                profile.beats.Add(new WeaponPresentationBeat { beat = (PresentationBeat)i, normalizedTime = positions[i] });
            profile.beats[0].eventTemplates.Add(new UltimateFaceStripEvent());
            var shake = ScriptableObject.CreateInstance<CameraShakeData>();
            shake.name = $"FinishShake_{weapon}";
            shake.key = $"Ultimate.{weapon}.Finish";
            shake.Duration = shakeDuration;
            shake.Noise = CameraShakeData.NoiseType.Wave;
            shake.PitchAmplitude = strength;
            shake.YawAmplitude = strength * 0.6f;
            shake.RollAmplitude = 0f;
            profile.beats[3].eventTemplates.Add(new UltimateCameraShakeEvent
                { shake = shake, strength = 1f, anchor = UltimateTimelineAnchor.Caster });
            if (!profile.IsValid(out string error))
                throw new InvalidOperationException(error);
            try
            {
                AssetDatabase.CreateAsset(profile, path);
                AssetDatabase.AddObjectToAsset(shake, profile);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }
            catch
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == profile)
                    AssetDatabase.DeleteAsset(path);
                else if (!AssetDatabase.Contains(profile))
                    UnityEngine.Object.DestroyImmediate(profile);
                if (shake != null && !AssetDatabase.Contains(shake))
                    UnityEngine.Object.DestroyImmediate(shake);
                throw;
            }
        }
    }
}
#endif
