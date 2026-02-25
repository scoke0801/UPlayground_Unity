using UPlayGround.Data;
using UPlayGround.Manager;
using UnityEngine;

namespace UPlayGround.CameraEffects
{
    public static class CameraEffectPresetRunner
    {
        public static void Play(CameraManager cameraManager, CameraImpactPreset preset, string effectGroupId)
        {
            if (cameraManager == null || string.IsNullOrEmpty(effectGroupId))
            {
                return;
            }

            CameraImpactPresetDatabase database = cameraManager.GetImpactPresetDatabase();
            if (database != null && database.TryGet(preset, out CameraImpactPresetEntry entry))
            {
                ApplyEntry(cameraManager, entry, effectGroupId);
                return;
            }

            ApplyDefault(cameraManager, preset, effectGroupId);
        }

        public static void Stop(CameraManager cameraManager, string effectGroupId)
        {
            if (cameraManager == null || string.IsNullOrEmpty(effectGroupId))
            {
                return;
            }

            cameraManager.StopEffect(MakeId(effectGroupId, "shake"));
            cameraManager.StopEffect(MakeId(effectGroupId, "spring"));
            cameraManager.StopEffect(MakeId(effectGroupId, "smooth"));
            cameraManager.StopEffect(MakeId(effectGroupId, "rot"));
            cameraManager.StopEffect(MakeId(effectGroupId, "zoom"));
            cameraManager.StopEffect(MakeId(effectGroupId, "fov"));
            cameraManager.StopEffect(MakeId(effectGroupId, "time"));
        }

        private static void ApplyEntry(CameraManager cameraManager, CameraImpactPresetEntry entry, string effectGroupId)
        {
            if (entry.useShake)
            {
                cameraManager.PlayProceduralShakeEffect(
                    MakeId(effectGroupId, "shake"),
                    entry.shakeAmplitude,
                    entry.shakeFrequency,
                    entry.shakeHold,
                    entry.shakeBlendIn,
                    entry.shakeBlendOut);
            }

            if (entry.useSpring)
            {
                cameraManager.PlaySpringDampEffect(
                    MakeId(effectGroupId, "spring"),
                    entry.springLocalOffset,
                    entry.springHold,
                    entry.springStiffness,
                    entry.springDamping,
                    entry.springBlendIn,
                    entry.springBlendOut);
            }

            if (entry.useSmooth)
            {
                cameraManager.PlaySmoothDampEffect(
                    MakeId(effectGroupId, "smooth"),
                    entry.smoothLocalOffset,
                    entry.smoothHold,
                    entry.smoothTime,
                    entry.smoothBlendIn,
                    entry.smoothBlendOut);
            }

            if (entry.useRotation)
            {
                cameraManager.PlayRotationEffect(
                    MakeId(effectGroupId, "rot"),
                    entry.rotationEuler,
                    entry.rotationHold,
                    entry.rotationBlendIn,
                    entry.rotationBlendOut);
            }

            if (entry.useZoom)
            {
                cameraManager.PlayZoomEffect(
                    MakeId(effectGroupId, "zoom"),
                    entry.zoomDistanceOffset,
                    entry.zoomHold,
                    entry.zoomBlendIn,
                    entry.zoomBlendOut);
            }

            if (entry.useFov)
            {
                cameraManager.PlayFovEffect(
                    MakeId(effectGroupId, "fov"),
                    entry.fovOffset,
                    entry.fovHold,
                    entry.fovBlendIn,
                    entry.fovBlendOut);
            }

            if (entry.useTimeScale)
            {
                cameraManager.PlayTimeScaleEffect(
                    MakeId(effectGroupId, "time"),
                    entry.timeScale,
                    entry.timeScaleHold,
                    entry.timeScaleBlendIn,
                    entry.timeScaleBlendOut);
            }
        }

        private static void ApplyDefault(CameraManager cameraManager, CameraImpactPreset preset, string effectGroupId)
        {
            switch (preset)
            {
                case CameraImpactPreset.LightHit:
                    cameraManager.PlayProceduralShakeEffect(MakeId(effectGroupId, "shake"), new Vector3(0.05f, 0.05f, 0.05f), 18f, 0.08f, 0.01f, 0.1f);
                    cameraManager.PlayFovEffect(MakeId(effectGroupId, "fov"), -2.5f, 0.07f, 0.03f, 0.1f);
                    break;

                case CameraImpactPreset.MediumHit:
                    cameraManager.PlayProceduralShakeEffect(MakeId(effectGroupId, "shake"), new Vector3(0.08f, 0.08f, 0.08f), 20f, 0.11f, 0.01f, 0.12f);
                    cameraManager.PlaySpringDampEffect(MakeId(effectGroupId, "spring"), new Vector3(0f, 0.16f, -0.05f), 0.09f, 90f, 18f, 0.02f, 0.14f);
                    cameraManager.PlayFovEffect(MakeId(effectGroupId, "fov"), -4f, 0.1f, 0.03f, 0.12f);
                    break;

                case CameraImpactPreset.HeavyHit:
                    cameraManager.PlayProceduralShakeEffect(MakeId(effectGroupId, "shake"), new Vector3(0.12f, 0.11f, 0.1f), 24f, 0.14f, 0.005f, 0.16f);
                    cameraManager.PlaySpringDampEffect(MakeId(effectGroupId, "spring"), new Vector3(0f, 0.24f, -0.12f), 0.12f, 110f, 16f, 0.02f, 0.16f);
                    cameraManager.PlayRotationEffect(MakeId(effectGroupId, "rot"), new Vector3(2f, 0f, 0f), 0.1f, 0.02f, 0.12f);
                    cameraManager.PlayFovEffect(MakeId(effectGroupId, "fov"), -6f, 0.12f, 0.02f, 0.16f);
                    cameraManager.PlayTimeScaleEffect(MakeId(effectGroupId, "time"), 0.15f, 0.06f, 0f, 0.08f);
                    break;

                case CameraImpactPreset.Finisher:
                    cameraManager.PlayProceduralShakeEffect(MakeId(effectGroupId, "shake"), new Vector3(0.18f, 0.14f, 0.12f), 28f, 0.2f, 0.005f, 0.22f);
                    cameraManager.PlaySpringDampEffect(MakeId(effectGroupId, "spring"), new Vector3(0f, 0.34f, -0.2f), 0.2f, 120f, 14f, 0.02f, 0.2f);
                    cameraManager.PlayRotationEffect(MakeId(effectGroupId, "rot"), new Vector3(4f, 0f, -1.2f), 0.18f, 0.02f, 0.18f);
                    cameraManager.PlayFovEffect(MakeId(effectGroupId, "fov"), -10f, 0.2f, 0.02f, 0.22f);
                    cameraManager.PlayTimeScaleEffect(MakeId(effectGroupId, "time"), 0.08f, 0.12f, 0f, 0.12f);
                    break;

                case CameraImpactPreset.DashStart:
                    cameraManager.PlayFovEffect(MakeId(effectGroupId, "fov"), 5.5f, 0.16f, 0.03f, 0.1f);
                    cameraManager.PlaySmoothDampEffect(MakeId(effectGroupId, "smooth"), new Vector3(0f, -0.05f, 0.18f), 0.1f, 0.08f, 0.02f, 0.1f);
                    break;

                case CameraImpactPreset.GuardImpact:
                    cameraManager.PlayProceduralShakeEffect(MakeId(effectGroupId, "shake"), new Vector3(0.07f, 0.06f, 0.05f), 17f, 0.08f, 0.01f, 0.1f);
                    cameraManager.PlayRotationEffect(MakeId(effectGroupId, "rot"), new Vector3(0f, 0f, 1.8f), 0.07f, 0.02f, 0.1f);
                    cameraManager.PlayFovEffect(MakeId(effectGroupId, "fov"), -3f, 0.08f, 0.02f, 0.1f);
                    cameraManager.PlayTimeScaleEffect(MakeId(effectGroupId, "time"), 0.2f, 0.04f, 0f, 0.08f);
                    break;
            }
        }

        private static string MakeId(string group, string suffix)
        {
            return $"{group}_{suffix}";
        }
    }
}
