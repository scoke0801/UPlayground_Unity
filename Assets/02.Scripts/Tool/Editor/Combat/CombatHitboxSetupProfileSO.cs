#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;

namespace UPlayGround.Tool.Editor.Combat
{
    public enum CombatHitboxSetupTargetKind
    {
        Weapon,
        Humanoid,
        Generic,
        Chain,
    }

    public enum CombatHitboxPreferredShape
    {
        Auto,
        Box,
        Capsule,
    }

    [Serializable]
    public sealed class CombatHitboxBoneRule
    {
        public string groupId = CombatHitbox.DefaultGroupId;
        public HumanBodyBones humanoidBone = HumanBodyBones.RightHand;
        public CombatHitboxPreferredShape shape = CombatHitboxPreferredShape.Box;
        public Vector3 center;
        public Vector3 size = new(0.25f, 0.25f, 0.35f);
    }

    [CreateAssetMenu(
        fileName = "CombatHitboxSetupProfile",
        menuName = "UPlayGround/전투/HitBox Setup Profile")]
    public sealed class CombatHitboxSetupProfileSO : ScriptableObject
    {
        [SerializeField] private string _profileId = "Default";
        [SerializeField] private CombatHitboxSetupTargetKind _targetKind = CombatHitboxSetupTargetKind.Weapon;
        [SerializeField] private string _defaultGroupId = "MainWeapon";
        [SerializeField] private CombatHitboxPreferredShape _preferredShape = CombatHitboxPreferredShape.Auto;
        [SerializeField] private List<string> _includeNamePatterns = new();
        [SerializeField] private List<string> _excludeNamePatterns =
            new() { "Sheath", "Scabbard", "Effect", "Trail", "VFX", "FX" };
        [SerializeField, Min(0f)] private float _padding = 0.02f;
        [SerializeField, Min(0.001f)] private float _minimumThickness = 0.04f;
        [SerializeField, Range(0f, 0.45f)] private float _axisTrimStart;
        [SerializeField, Range(0f, 0.45f)] private float _axisTrimEnd;
        [SerializeField] private bool _useSweep = true;
        [SerializeField, Min(0.01f)] private float _sweepStepDistance = 0.15f;
        [SerializeField, Range(1, 32)] private int _maxSweepSteps = 8;
        [Header("Chain (채찍/세그먼트 무기)")]
        [Tooltip("체인 모드에서 N개 노드마다 캡슐 1개를 만든다. 1이면 모든 링크에 생성.")]
        [SerializeField, Min(1)] private int _chainSegmentStride = 1;
        [Tooltip("체인 캡슐의 반경(월드 기준 m). 본 lossyScale로 자동 보정된다.")]
        [SerializeField, Min(0.001f)] private float _chainRadius = 0.08f;
        [SerializeField] private List<CombatHitboxBoneRule> _boneRules = new();

        public string ProfileId => _profileId;
        public CombatHitboxSetupTargetKind TargetKind => _targetKind;
        public string DefaultGroupId =>
            string.IsNullOrWhiteSpace(_defaultGroupId) ? CombatHitbox.DefaultGroupId : _defaultGroupId.Trim();
        public CombatHitboxPreferredShape PreferredShape => _preferredShape;
        public IReadOnlyList<string> IncludeNamePatterns => _includeNamePatterns;
        public IReadOnlyList<string> ExcludeNamePatterns => _excludeNamePatterns;
        public float Padding => Mathf.Max(0f, _padding);
        public float MinimumThickness => Mathf.Max(0.001f, _minimumThickness);
        public float AxisTrimStart => Mathf.Clamp(_axisTrimStart, 0f, 0.45f);
        public float AxisTrimEnd => Mathf.Clamp(_axisTrimEnd, 0f, 0.45f);
        public bool UseSweep => _useSweep;
        public float SweepStepDistance => Mathf.Max(0.01f, _sweepStepDistance);
        public int MaxSweepSteps => Mathf.Clamp(_maxSweepSteps, 1, 32);
        public int ChainSegmentStride => Mathf.Max(1, _chainSegmentStride);
        public float ChainRadius => Mathf.Max(0.001f, _chainRadius);
        public IReadOnlyList<CombatHitboxBoneRule> BoneRules => _boneRules;
    }
}
#endif
