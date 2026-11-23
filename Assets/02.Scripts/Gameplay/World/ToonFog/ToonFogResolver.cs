using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Rendering;

namespace UPlayGround.Gameplay.World.ToonFog
{
    internal sealed class ToonFogResolver : IDisposable
    {
        internal const int MaxLocals = 8;
        private const int PaletteSize = 256;
        private const int MaxRegions = 4;

        internal struct Local
        {
            internal Matrix4x4 Inverse;
            internal Vector4 Settings, Tint, Noise;
        }

        private struct ProfileValues
        {
            internal Vector4 Distance, Height, Colors;
            internal Color HeightTint;

            internal static ProfileValues Read(ToonFogProfile profile) => new()
            {
                Distance = new Vector4(profile.fogEnabled ? profile.distanceDensity : 0, profile.nearClearDistance,
                    profile.maxEvaluationDistance, profile.maxOpacity),
                Height = new Vector4(profile.fogEnabled && profile.heightEnabled ? profile.heightDensity : 0,
                    profile.baseHeight, profile.heightScale, profile.fogEnabled ? profile.localDensityMultiplier : 0),
                Colors = new Vector4(profile.colorNear, profile.colorFar, 0, 0),
                HeightTint = QualitySettings.activeColorSpace == ColorSpace.Linear ? profile.heightTint.linear : profile.heightTint
            };

            internal static ProfileValues Blend(ProfileValues a, ProfileValues b, float t) => new()
            {
                Distance = Vector4.Lerp(a.Distance, b.Distance, t),
                Height = Vector4.Lerp(a.Height, b.Height, t),
                Colors = Vector4.Lerp(a.Colors, b.Colors, t),
                HeightTint = Color.Lerp(a.HeightTint, b.HeightTint, t)
            };
        }

        private sealed class Palette
        {
            internal readonly Color[] Colors = new Color[PaletteSize];
            internal int Revision;
        }

        private struct Region
        {
            internal ToonFogRegion Component;
            internal float Weight;
        }

        private struct Candidate
        {
            internal int Key, Id, Priority;
            internal float Score;
            internal Local Value;
        }

        private struct Slot
        {
            internal Candidate Candidate;
            internal float Weight;
        }

        private static readonly Comparison<Region> CompareRegions = (a, b) =>
        {
            int order = b.Component.priority.CompareTo(a.Component.priority);
            if (order != 0) return order;
            order = a.Component.Id.CompareTo(b.Component.Id);
            return order != 0 ? order : a.Component.GetInstanceID().CompareTo(b.Component.GetInstanceID());
        };

        private static readonly Comparison<Candidate> CompareLocals = (a, b) =>
        {
            int order = b.Priority.CompareTo(a.Priority);
            if (order != 0) return order;
            order = b.Score.CompareTo(a.Score);
            if (order != 0) return order;
            order = a.Id.CompareTo(b.Id);
            return order != 0 ? order : a.Key.CompareTo(b.Key);
        };

        private readonly Dictionary<ToonFogProfile, Palette> _palettes = new();
        private readonly List<Region> _regions = new();
        private readonly List<Candidate> _candidates = new();
        private readonly Slot[] _slots = new Slot[MaxLocals];
        private readonly Plane[] _frustum = new Plane[6];
        private readonly Color[] _targetPalette = new Color[PaletteSize];
        private readonly Color[] _currentPalette = new Color[PaletteSize];

        private ToonFogWorld _world;
        private Texture2D _lut;
        private ProfileValues _current;
        private Vector3 _lastAnchor, _lastCameraPosition;
        private uint _transitionRevision;
        private double _lastTime;
        private float _combat;
        private float _environmentDensity;
        private Color _environmentTint;
        private Vector2 _environmentWind;
        private int _lastLocalLimit;

        internal Vector4 Distance, Height, Colors;
        internal Color HeightTint, EnvironmentTint;
        internal Texture2D Lut => _lut;
        internal Texture Noise;
        internal int LocalCount, Samples, RegisteredCount, CandidateCount, RejectedCount, RegionCount;
        internal readonly Local[] Locals = new Local[MaxLocals];

        /// <summary>현재 카메라의 프로필과 국소 슬롯을 렌더 패스 기록 전에 확정한다.</summary>
        internal bool Resolve(Camera camera, ToonFogWorld world, Transform anchorOverride)
        {
            var quality = world.quality;
            if (world.profile == null || !world.profile.IsValid || quality == null)
            {
                Invalidate();
                return false;
            }

            Vector3 cameraPosition = camera.transform.position;
            var input = world.GetInput(camera);
            Transform anchorTransform = anchorOverride != null ? anchorOverride : world.playerAnchor;
            if (anchorTransform == null) anchorTransform = input.PlayerAnchor;
            Vector3 anchor = camera.cameraType != CameraType.SceneView && anchorTransform != null ? anchorTransform.position : cameraPosition;
            double now = Time.realtimeSinceStartupAsDouble;
            float delta = (float)Math.Max(0, now - _lastTime);
            _lastTime = now;
            float cutSquared = quality.teleportDistance * quality.teleportDistance;
            bool reset = _world != world || _transitionRevision != world.TransitionRevision || input.ResetTransition
                || (cutSquared > 0 && ((anchor - _lastAnchor).sqrMagnitude > cutSquared
                    || (cameraPosition - _lastCameraPosition).sqrMagnitude > cutSquared));
            if (_world != world) _palettes.Clear();
            _world = world;
            _transitionRevision = world.TransitionRevision;
            _lastAnchor = anchor;
            _lastCameraPosition = cameraPosition;

            ProfileValues target = ResolveRegions(anchor, world, out float seconds);
            float combatTarget = Mathf.Clamp01(input.CombatWeight);
            _combat = Mathf.Lerp(_combat, combatTarget, BlendFactor(delta,
                combatTarget > _combat ? quality.combatEnterSeconds : quality.combatExitSeconds, reset));
            float blend = BlendFactor(delta, seconds, reset);
            _current = ProfileValues.Blend(_current, target, blend);
            UpdatePalette(blend, reset);

            _environmentDensity = input.DensityMultiplier <= 0
                ? 0 : Mathf.Lerp(_environmentDensity, input.DensityMultiplier, blend);
            _environmentTint = Color.Lerp(_environmentTint, input.TintMultiplier, blend);
            _environmentWind = Vector2.Lerp(_environmentWind, input.WindVelocityXZ, blend);
            float density = _environmentDensity * Mathf.Lerp(1, quality.combatDensityMultiplier, _combat);
            Distance = _current.Distance;
            Distance.x *= density;
            Distance.y = Mathf.Lerp(Distance.y, Mathf.Max(Distance.y, quality.combatNearClearDistance), _combat);
            Distance.w = Mathf.Lerp(Distance.w, Mathf.Min(Distance.w, quality.combatMaxOpacity), _combat);
            Height = _current.Height;
            Height.x *= density;
            Colors = _current.Colors;
            HeightTint = _current.HeightTint;
            EnvironmentTint = _environmentTint;
            ResolveLocals(camera, quality, _environmentWind, Height.w * density, delta, reset);
            return true;
        }

        private ProfileValues ResolveRegions(Vector3 anchor, ToonFogWorld world, out float seconds)
        {
            var baseProfile = world.profile;
            var values = ProfileValues.Read(baseProfile);
            seconds = baseProfile.regionTransitionSeconds;
            Array.Copy(GetPalette(baseProfile), _targetPalette, PaletteSize);
            _regions.Clear();
            foreach (var region in ToonFogWorld.Regions)
            {
                if (region == null || !region.isActiveAndEnabled) continue;
                float weight = region.GetWeight(anchor);
                if (weight > 0) _regions.Add(new Region { Component = region, Weight = weight });
            }
            _regions.Sort(CompareRegions);
            RegionCount = _regions.Count;
            for (int end = Mathf.Min(_regions.Count, MaxRegions); end > 0;)
            {
                int first = end - 1;
                int priority = _regions[first].Component.priority;
                while (first > 0 && _regions[first - 1].Component.priority == priority) first--;

                float sum = 0, groupSeconds = 0;
                ProfileValues group = default;
                for (int i = first; i < end; i++)
                {
                    var region = _regions[i];
                    sum += region.Weight;
                    float fraction = region.Weight / sum;
                    group = ProfileValues.Blend(group, ProfileValues.Read(region.Component.profile), fraction);
                    groupSeconds = Mathf.Lerp(groupSeconds, region.Component.profile.regionTransitionSeconds, fraction);
                }
                float strength = Mathf.Min(1, sum);
                values = ProfileValues.Blend(values, group, strength);
                seconds = Mathf.Lerp(seconds, groupSeconds, strength);
                for (int i = 0; i < PaletteSize; i++) _targetPalette[i] *= 1 - strength;
                for (int i = first; i < end; i++)
                {
                    var region = _regions[i];
                    var palette = GetPalette(region.Component.profile);
                    float weight = strength * region.Weight / sum;
                    for (int j = 0; j < PaletteSize; j++) _targetPalette[j] += palette[j] * weight;
                }
                end = first;
            }
            return values;
        }

        private Color[] GetPalette(ToonFogProfile profile)
        {
            if (_palettes.TryGetValue(profile, out var palette) && palette.Revision == profile.Revision)
                return palette.Colors;
            palette ??= new Palette();
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            for (int i = 0; i < PaletteSize; i++)
            {
                Color color = profile.distanceGradient.Evaluate(i / (float)(PaletteSize - 1));
                if (linear && profile.distanceGradient.colorSpace != ColorSpace.Linear) color = color.linear;
                if (!float.IsFinite(color.r) || !float.IsFinite(color.g) || !float.IsFinite(color.b)) color = Color.black;
                color.a = 1;
                palette.Colors[i] = color;
            }
            palette.Revision = profile.Revision;
            _palettes[profile] = palette;
            return palette.Colors;
        }

        private void UpdatePalette(float blend, bool reset)
        {
            bool changed = reset || _lut == null;
            for (int i = 0; i < PaletteSize; i++)
            {
                Color current = _currentPalette[i];
                Color next = Color.Lerp(current, _targetPalette[i], blend);
                if (!changed && (Vector4)(next - current) == Vector4.zero) continue;
                _currentPalette[i] = next;
                changed = true;
            }
            if (!changed) return;
            if (_lut == null)
            {
                _lut = new Texture2D(PaletteSize, 1, TextureFormat.RGBAFloat, false, true)
                {
                    name = "Toon Fog Camera Palette", wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave
                };
            }
            _lut.SetPixelData(_currentPalette, 0);
            _lut.Apply(false);
        }

        private void ResolveLocals(Camera camera, ToonFogQualitySettings quality, Vector2 wind, float multiplier, float delta, bool reset)
        {
            int limit = quality.quality switch
            {
                ToonFogQualitySettings.Tier.Low => 2,
                ToonFogQualitySettings.Tier.High => 8,
                _ => 4
            };
            Samples = limit;
            Noise = quality.quality != ToonFogQualitySettings.Tier.Low && quality.noise != null
                ? quality.noise : Texture2D.grayTexture;
            if (reset || limit != _lastLocalLimit) Array.Clear(_slots, 0, _slots.Length);
            _lastLocalLimit = limit;

            Vector3 eye = camera.transform.position;
            GeometryUtility.CalculateFrustumPlanes(camera, _frustum);
            // 하늘 앞 부피는 카메라 Far Clip이 짧아도 평가 상한까지 보인다.
            Vector3 forward = camera.transform.forward;
            _frustum[5] = new Plane(-forward, eye + forward * Mathf.Max(camera.farClipPlane, Distance.z));
            RegisteredCount = ToonFogWorld.Volumes.Count;
            _candidates.Clear();
            foreach (var volume in ToonFogWorld.Volumes)
            {
                if (volume == null || !volume.isActiveAndEnabled || !float.IsFinite(volume.density) || volume.density <= 0
                    || !float.IsFinite(volume.weight) || volume.weight <= 0 || !volume.TryGetGeometry(out Bounds bounds, out Matrix4x4 inverse)) continue;
                float distanceSquared = bounds.SqrDistance(eye);
                float max = volume.maxVisibleDistance > 0 ? Mathf.Min(Distance.z, volume.maxVisibleDistance) : Distance.z;
                if (distanceSquared > max * max || (!bounds.Contains(eye) && !GeometryUtility.TestPlanesAABB(_frustum, bounds))) continue;

                float feather = float.IsFinite(volume.edgeFeather01) ? Mathf.Clamp(volume.edgeFeather01, 0, 0.49f) : 0;
                float noiseAmount = Noise == Texture2D.grayTexture || !float.IsFinite(volume.noiseAmount) ? 0 : Mathf.Clamp01(volume.noiseAmount);
                float frequency = float.IsFinite(volume.noiseFrequency) ? Mathf.Max(0, volume.noiseFrequency) : 0;
                Vector2 velocity = volume.windVelocity + wind;
                if (!float.IsFinite(velocity.sqrMagnitude)) velocity = Vector2.zero;
                double time = Application.isPlaying ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble;
                Color tint = volume.useRegionTint ? HeightTint : QualitySettings.activeColorSpace == ColorSpace.Linear ? volume.tint.linear : volume.tint;
                if (!float.IsFinite(tint.r) || !float.IsFinite(tint.g) || !float.IsFinite(tint.b)) continue;
                int key = volume.GetInstanceID();
                float score = bounds.extents.sqrMagnitude / (1 + distanceSquared);
                for (int i = 0; i < limit; i++) if (_slots[i].Candidate.Key == key) { score *= 1.1f; break; }

                _candidates.Add(new Candidate
                {
                    Key = key, Id = volume.Id, Priority = volume.priority, Score = score,
                    Value = new Local
                    {
                        Inverse = inverse,
                        Settings = new Vector4(volume.density * Mathf.Clamp01(volume.weight), (int)volume.shape, feather, noiseAmount),
                        Tint = new Vector4(tint.r, tint.g, tint.b, volume.Id),
                        Noise = new Vector4(frequency, (float)((velocity.x * frequency * time) % 1), (float)((velocity.y * frequency * time) % 1), 0)
                    }
                });
            }

            _candidates.Sort(CompareLocals);
            CandidateCount = _candidates.Count;
            RejectedCount = Mathf.Max(0, CandidateCount - limit);
            int wantedCount = Mathf.Min(limit, CandidateCount);
            float step = reset || quality.localFadeSeconds <= 0 ? 1 : delta / quality.localFadeSeconds;
            for (int i = 0; i < limit; i++)
            {
                ref Slot slot = ref _slots[i];
                if (slot.Candidate.Key == 0) continue;
                int index = FindCandidate(slot.Candidate.Key, wantedCount);
                if (index >= 0) slot.Candidate = _candidates[index];
                slot.Weight = Mathf.MoveTowards(slot.Weight, index >= 0 ? 1 : 0, step);
                if (slot.Weight <= 0) slot = default;
            }
            for (int i = 0; i < wantedCount; i++)
            {
                var candidate = _candidates[i];
                int free = -1;
                bool present = false;
                for (int j = 0; j < limit; j++)
                {
                    if (_slots[j].Candidate.Key == candidate.Key) { present = true; break; }
                    if (free < 0 && _slots[j].Candidate.Key == 0) free = j;
                }
                if (!present && free >= 0) _slots[free] = new Slot { Candidate = candidate, Weight = Mathf.Clamp01(step) };
            }

            LocalCount = 0;
            for (int i = 0; i < limit; i++)
            {
                ref Slot slot = ref _slots[i];
                if (slot.Candidate.Key == 0 || slot.Weight <= 0 || multiplier <= 0) continue;
                Local local = slot.Candidate.Value;
                local.Settings.x *= multiplier * slot.Weight;
                Locals[LocalCount++] = local;
            }
        }

        private int FindCandidate(int key, int count)
        {
            for (int i = 0; i < count; i++) if (_candidates[i].Key == key) return i;
            return -1;
        }

        private static float BlendFactor(float delta, float seconds, bool reset) =>
            reset || seconds <= 0 ? 1 : 1 - Mathf.Exp(-delta / seconds);

        internal void Invalidate() => _world = null;

        public void Dispose()
        {
            if (_lut == null) return;
            UnityEngine.Rendering.CoreUtils.Destroy(_lut);
            _lut = null;
        }
    }
}
