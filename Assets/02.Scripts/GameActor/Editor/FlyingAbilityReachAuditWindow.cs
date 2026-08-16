using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 비행 몬스터의 공중 Ability가 실제 선회 거리에 닿는지 감사하고 고친다.
    ///
    /// ■ 왜 필요한가
    /// AirCircle은 타겟 주위를 반경 r, 고도 h로 돈다. EnemyDetection.DistanceToTarget은
    /// 3D 거리이므로 이때 타겟까지의 거리는 sqrt(r² + h²)이며 지상 교전 거리보다 훨씬 멀다.
    /// 그런데 공중 Ability의 activation.maxDistance가 지상 기준(보통 3m 안팎)으로
    /// 저작되어 있으면 ActorAbilitySystem.MatchesDistance에서 항상 탈락한다.
    /// (Melee는 EnemyAttackRangePolicy의 근접 사거리 정책까지 한 번 더 통과해야 한다.)
    /// 그 결과
    ///   · AirCircle이 한 번도 공격하지 못하고 체류 시간만 소진하고
    ///   · HasDiveSkillAvailable이 늘 false여서 급강하가 영영 발동하지 않는다.
    /// 데이터만 보면 멀쩡해 보여서 눈으로는 잡기 어려운 종류의 결함이다.
    ///
    /// ■ 또 하나의 함정
    /// EnemyAbilitySelectionPolicy는 aerialOnly + diveOnly=false 요청에서 급강하를
    /// 명시적으로 제외한다. 따라서 급강하만 가진 몬스터는 거리와 무관하게 공중 견제
    /// 후보가 0이다. 이 창은 그 경우도 함께 보고한다.
    ///
    /// 특정 몬스터에 묶이지 않는다. 대상 AbilitySet과 선회 파라미터를 지정해 사용한다.
    /// </summary>
    public sealed class FlyingAbilityReachAuditWindow : EditorWindow
    {
        [SerializeField] private AbilitySetSO _abilitySet;

        [Tooltip("EnemyFlyingAIController를 가진 프리팹. 지정하면 선회 반경·고도를 자동으로 읽는다.")]
        [SerializeField] private GameObject _flyingPrefab;

        [SerializeField] private float _airCircleRadius = 6f;
        [SerializeField] private float _airHoverHeight = 4f;

        [Tooltip("선회 거리에 더할 여유 비율. 접근·이탈 중에도 후보로 남게 한다.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _margin = 0.6f;

        private SerializedObject _serialized;
        private Vector2 _scroll;
        private string _report = "‘감사’를 눌러 공중 사거리 문제를 확인하세요.";
        private readonly List<GameplayAbilitySO> _fixTargets = new();
        private float _requiredDistance;

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/툴 런처/게임플레이 · 전투/비행 공중 사거리 감사",
            false,
            322)]
        public static void Open()
        {
            var window = GetWindow<FlyingAbilityReachAuditWindow>(true, "비행 공중 사거리 감사");
            window.minSize = new Vector2(600f, 520f);
            window.Show();
        }

        private void OnEnable() => _serialized = new SerializedObject(this);

        private void OnGUI()
        {
            _serialized ??= new SerializedObject(this);
            _serialized.Update();

            EditorGUILayout.HelpBox(
                "공중 선회 거리 = sqrt(반경² + 고도²).\n"
                + "공중 Ability의 activation.maxDistance가 이보다 짧으면 "
                + "공중 공격과 급강하가 영영 발동하지 않습니다.",
                MessageType.Info);

            SerializedProperty property = _serialized.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
                EditorGUILayout.PropertyField(property, true);
            _serialized.ApplyModifiedProperties();

            if (_flyingPrefab != null && GUILayout.Button("프리팹에서 선회 파라미터 읽기"))
                ReadFromPrefab();

            float orbit = Mathf.Sqrt(
                _airCircleRadius * _airCircleRadius + _airHoverHeight * _airHoverHeight);
            EditorGUILayout.LabelField(
                $"선회 거리 {orbit:0.00}m → 권장 최소 maxDistance "
                + $"{orbit * (1f + _margin):0.0}m",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(_abilitySet == null))
            {
                if (GUILayout.Button("감사", GUILayout.Height(26f)))
                    Audit();

                using (new EditorGUI.DisabledScope(_fixTargets.Count == 0))
                {
                    if (GUILayout.Button(
                            $"사거리 자동 수정 ({_fixTargets.Count}건)",
                            GUILayout.Height(26f)))
                        ApplyFix();
                }
            }

            EditorGUILayout.Space(6f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void ReadFromPrefab()
        {
            var controller = _flyingPrefab.GetComponentInChildren<EnemyFlyingAIContext>(true);
            if (controller == null)
            {
                _report = "프리팹에서 EnemyFlyingAIContext를 찾지 못했습니다.";
                return;
            }

            _airCircleRadius = controller.AirCircleRadius;
            _airHoverHeight = controller.AirHoverHeight;
            _report = $"프리팹에서 읽음 — 반경 {_airCircleRadius}, 고도 {_airHoverHeight}";
        }

        private void Audit()
        {
            _fixTargets.Clear();
            float orbit = Mathf.Sqrt(
                _airCircleRadius * _airCircleRadius + _airHoverHeight * _airHoverHeight);
            _requiredDistance = Mathf.Ceil(orbit * (1f + _margin));

            var log = new StringBuilder();
            log.AppendLine($"AbilitySet: {_abilitySet.name}");
            log.AppendLine($"선회 거리 {orbit:0.00}m / 권장 maxDistance {_requiredDistance:0}m");
            log.AppendLine();

            var diveAbilities = new List<GameplayAbilitySO>();
            var harassAbilities = new List<GameplayAbilitySO>();

            foreach (GameplayAbilitySO ability in _abilitySet.GetRuntimeAbilities())
            {
                AbilityAttackInfo info = ResolveAttackInfo(ability);
                if (info == null || !info.isAerialSkill)
                    continue;

                if (info.isDiveAttack)
                    diveAbilities.Add(ability);
                else
                    harassAbilities.Add(ability);

                float max = ability.activation.maxDistance;
                bool isMelee = info.baseInfo?.attackType == AttackType.Melee;

                // max <= 0은 MatchesDistance 기준으로는 무제한이다. 하지만 Melee는
                // EnemyAttackRangePolicy가 한 번 더 걸리고, 거기서 authoredMax가 0이면
                // targetingRange + personalSpace로 폴백한다(보통 2~3m). 즉 Melee에서
                // max 0은 무제한이 아니라 '히트박스 크기만큼'이라 선회 거리에 못 닿는다.
                bool meleeFallbackTooShort = isMelee && max <= 0f && ResolveThreatRange(info) < orbit;
                bool unreachable = (max > 0f && max < orbit) || meleeFallbackTooShort;

                string kind = info.isDiveAttack ? "급강하" : "공중 견제";
                string maxLabel = max > 0f ? $"{max:0.##}" : "0(무제한)";
                log.AppendLine(
                    $"{(unreachable ? "✗" : "·")} [{kind}] {ability.name} "
                    + $"— min {ability.activation.minDistance:0.##} / max {maxLabel}"
                    + (unreachable ? "  ← 선회 거리에 닿지 않음" : string.Empty));

                if (meleeFallbackTooShort)
                    log.AppendLine(
                        "    ⚠ Melee + maxDistance 0은 무제한이 아닙니다. "
                        + "근접 사거리 정책이 targetingRange로 폴백합니다.");

                if (unreachable)
                    _fixTargets.Add(ability);

                if (isMelee && !info.isDiveAttack)
                    log.AppendLine(
                        "    ⚠ Melee 공중 견제기는 근접 사거리 정책까지 통과해야 합니다. "
                        + "원거리 견제라면 attackType을 Ranged로 두세요.");
            }

            log.AppendLine();
            if (diveAbilities.Count == 0)
                log.AppendLine("✗ 급강하(isDiveAttack) Ability가 없습니다 — Dive 상태에 진입할 수 없습니다.");
            if (harassAbilities.Count == 0)
                log.AppendLine(
                    "✗ 비-급강하 공중 Ability가 없습니다 — AirCircle의 공중 공격은 "
                    + "거리와 무관하게 후보가 0입니다.\n"
                    + "   (EnemyAbilitySelectionPolicy가 diveOnly=false 요청에서 "
                    + "급강하를 명시적으로 제외합니다.)");
            if (diveAbilities.Count > 0 && harassAbilities.Count > 0 && _fixTargets.Count == 0)
                log.AppendLine("문제 없음.");

            _report = log.ToString();
        }

        private void ApplyFix()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("공중 사거리 수정");

            var log = new StringBuilder();
            foreach (GameplayAbilitySO ability in _fixTargets)
            {
                if (ability == null)
                    continue;
                Undo.RecordObject(ability, "공중 사거리 수정");
                log.AppendLine(
                    $"· {ability.name}: maxDistance "
                    + $"{ability.activation.maxDistance:0.##} → {_requiredDistance:0}");
                ability.activation.maxDistance = _requiredDistance;
                EditorUtility.SetDirty(ability);
            }

            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            _fixTargets.Clear();

            log.AppendLine();
            log.AppendLine("수정 완료. 되돌리려면 Ctrl+Z. 다시 ‘감사’를 눌러 확인하세요.");
            _report = log.ToString();
        }

        /// <summary>
        /// authoredMax가 없을 때 근접 사거리 정책이 폴백으로 쓰는 위협 반경.
        /// EnemyAttackRangePolicy.ResolveEffectiveMaxDistance와 같은 기준이다.
        /// </summary>
        private static float ResolveThreatRange(AbilityAttackInfo info)
        {
            List<HitPhaseData> phases = info.baseInfo?.hitPhases;
            if (phases == null)
                return 0f;

            float range = 0f;
            for (var i = 0; i < phases.Count; i++)
            {
                if (phases[i] != null)
                    range = Mathf.Max(range, phases[i].targetingRange);
            }
            return range;
        }

        private static AbilityAttackInfo ResolveAttackInfo(GameplayAbilitySO ability)
        {
            if (ability?.variants == null)
                return null;
            for (var i = 0; i < ability.variants.Count; i++)
            {
                if (ability.variants[i]?.executionPayload
                    is UPlayGroundMotionAbilityPayloadSO payload)
                    return payload.attackInfo;
            }
            return null;
        }
    }
}
