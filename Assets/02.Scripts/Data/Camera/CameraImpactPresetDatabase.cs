using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.CameraEffects;

namespace UPlayGround.Data
{
    [Serializable]
    public class CameraImpactPresetEntry
    {
        public CameraImpactPreset preset;

        [Header("Shake")]
        public bool useShake;
        public Vector3 shakeAmplitude = Vector3.zero;
        public float shakeFrequency = 20f;
        public float shakeHold = 0.1f;
        public float shakeBlendIn = 0.02f;
        public float shakeBlendOut = 0.15f;

        [Header("Spring")]
        public bool useSpring;
        public Vector3 springLocalOffset = Vector3.zero;
        public float springHold = 0.1f;
        public float springStiffness = 90f;
        public float springDamping = 16f;
        public float springBlendIn = 0.05f;
        public float springBlendOut = 0.15f;

        [Header("Smooth")]
        public bool useSmooth;
        public Vector3 smoothLocalOffset = Vector3.zero;
        public float smoothHold = 0.1f;
        public float smoothTime = 0.12f;
        public float smoothBlendIn = 0.08f;
        public float smoothBlendOut = 0.12f;

        [Header("Rotation")]
        public bool useRotation;
        public Vector3 rotationEuler = Vector3.zero;
        public float rotationHold = 0.1f;
        public float rotationBlendIn = 0.08f;
        public float rotationBlendOut = 0.12f;

        [Header("Zoom")]
        public bool useZoom;
        public float zoomDistanceOffset = 0f;
        public float zoomHold = 0.1f;
        public float zoomBlendIn = 0.08f;
        public float zoomBlendOut = 0.12f;

        [Header("FOV")]
        public bool useFov;
        public float fovOffset = 0f;
        public float fovHold = 0.1f;
        public float fovBlendIn = 0.08f;
        public float fovBlendOut = 0.15f;

        [Header("TimeScale")]
        public bool useTimeScale;
        public float timeScale = 1f;
        public float timeScaleHold = 0.08f;
        public float timeScaleBlendIn = 0f;
        public float timeScaleBlendOut = 0.08f;
    }

    [CreateAssetMenu(fileName = "CameraImpactPresetDatabase", menuName = "UPlayGround/SO/Camera/ImpactPresetDatabase")]
    public class CameraImpactPresetDatabase : ScriptableObject
    {
        [SerializeField] private List<CameraImpactPresetEntry> presets = new List<CameraImpactPresetEntry>();

        private Dictionary<CameraImpactPreset, CameraImpactPresetEntry> _presetMap;

        public void Initialize()
        {
            _presetMap = new Dictionary<CameraImpactPreset, CameraImpactPresetEntry>();
            for (int i = 0; i < presets.Count; i++)
            {
                CameraImpactPresetEntry entry = presets[i];
                if (entry == null)
                {
                    continue;
                }

                if (_presetMap.ContainsKey(entry.preset))
                {
                    _presetMap[entry.preset] = entry;
                }
                else
                {
                    _presetMap.Add(entry.preset, entry);
                }
            }
        }

        public bool TryGet(CameraImpactPreset preset, out CameraImpactPresetEntry entry)
        {
            if (_presetMap == null)
            {
                Initialize();
            }

            return _presetMap.TryGetValue(preset, out entry);
        }
    }
}
