using UnityEditor;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 기획서 §4.1 프리셋 9종을 한 번에 생성하는 에디터 툴.
    /// Menu: UPlayGround > Camera > Generate Shake Presets
    /// </summary>
    public static class CameraShakePresetGenerator
    {
        private struct PresetDef
        {
            public string key;
            public float pitch, yaw, freqHz, duration;
            public CameraShakeData.DampeningType dampening;
        }

        // 현재 회전 전용 프리셋 기준값
        private static readonly PresetDef[] Presets =
        {
            new() { key = "LiteHit",         pitch = 0.5f, yaw = 0.4f, freqHz = 25f, duration = 0.08f, dampening = CameraShakeData.DampeningType.EaseOut   },
            new() { key = "MediumHit",       pitch = 1.0f, yaw = 0.7f, freqHz = 22f, duration = 0.12f, dampening = CameraShakeData.DampeningType.EaseOut   },
            new() { key = "HeavyHit",        pitch = 1.8f, yaw = 1.2f, freqHz = 18f, duration = 0.18f, dampening = CameraShakeData.DampeningType.EaseOut   },
            new() { key = "CriticalHit",     pitch = 1.2f, yaw = 0.8f, freqHz = 15f, duration = 0.22f, dampening = CameraShakeData.DampeningType.EaseOut   },
            new() { key = "PlayerHit_Light", pitch = 0.9f, yaw = 0.7f, freqHz = 20f, duration = 0.15f, dampening = CameraShakeData.DampeningType.Linear    },
            new() { key = "PlayerHit_Heavy", pitch = 2.0f, yaw = 1.4f, freqHz = 16f, duration = 0.25f, dampening = CameraShakeData.DampeningType.Linear    },
            new() { key = "PoiseBreak",      pitch = 2.0f, yaw = 1.0f, freqHz = 20f, duration = 0.18f, dampening = CameraShakeData.DampeningType.EaseOut   },
            new() { key = "KillCam",         pitch = 1.4f, yaw = 0.9f, freqHz = 10f, duration = 0f,    dampening = CameraShakeData.DampeningType.Constant  },
            new() { key = "Explosion",       pitch = 3.0f, yaw = 2.4f, freqHz = 12f, duration = 0.40f, dampening = CameraShakeData.DampeningType.EaseOut   },
        };

        public static void Generate()
        {
            string savePath = EditorUtility.OpenFolderPanel("프리셋 저장 폴더 선택", "Assets", "");
            if (string.IsNullOrEmpty(savePath)) return;

            // 절대 경로 → 프로젝트 상대 경로
            if (!savePath.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("오류", "Assets 폴더 안을 선택하세요.", "확인");
                return;
            }
            string relativePath = "Assets" + savePath.Substring(Application.dataPath.Length);

            int created = 0, skipped = 0;

            foreach (var def in Presets)
            {
                string assetPath = $"{relativePath}/{def.key}.asset";

                // 이미 존재하면 덮어쓰지 않음
                if (AssetDatabase.LoadAssetAtPath<CameraShakeData>(assetPath) != null)
                {
                    Debug.Log($"[ShakePreset] 스킵 (이미 존재): {def.key}");
                    skipped++;
                    continue;
                }

                var so = ScriptableObject.CreateInstance<CameraShakeData>();
                so.key        = def.key;
                so.PitchAmplitude = def.pitch;
                so.YawAmplitude = def.yaw;
                so.Frequency  = def.freqHz;
                so.Duration   = def.duration;
                so.Dampening  = def.dampening;
                so.UseMainCamera = true;

                AssetDatabase.CreateAsset(so, assetPath);
                created++;
                Debug.Log($"[ShakePreset] 생성: {assetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "완료",
                $"생성: {created}개 / 스킵(이미 존재): {skipped}개\n\n" +
                "생성된 SO를 CameraShakeDatabase에 등록하세요.",
                "확인"
            );
        }
    }
}
