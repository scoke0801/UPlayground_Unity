using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor
{
    /// <summary>
    /// MonsterData.json → EnemyStatsSO / PoiseSO / EnemyBehaviorSO / EnemyAttackDataSO 일괄 생성
    /// Tools > UPlayGround > Import Monster Data
    /// </summary>
    public class MonsterDataImporter : EditorWindow
    {
        // ── JSON 매핑 구조 ──────────────────────────────────────────

        [Serializable] private class Root { public List<MonsterJson> monsters; }

        [Serializable] private class MonsterJson
        {
            public string _id;
            public StatsJson    stats;
            public PoiseJson    poise;
            public BrainJson    brain;
            public AttackDataJson attackData;
        }

        [Serializable] private class StatsJson
        {
            public float maxHealth = 100;
            public float walkSpeed = 2; public float runSpeed = 4;
            public float chaseSpeedMultiplier = 1.2f;
            public float detectionRadius = 10; public float lostTargetRadius = 15;
            public float fieldOfView = 120;
            public float attackRange = 2.5f; public float attackCooldown = 1.5f;
            public bool  enablePatrol = true;
            public float patrolRadius = 5; public float patrolWaitTime = 2;
        }

        [Serializable] private class PoiseJson
        {
            public float maxPoise = 100; public float recoveryDelay = 2;
            public float recoveryRate = 40; public bool hasHyperArmor;
        }

        [Serializable] private class BrainJson
        {
            public float optimalCombatDistance = 2.5f; public float minCombatDistance = 1.5f;
            public bool  maintainDistance = true;
            public float continueAttackChance = 0.3f; public float guardChance = 0.25f;
            public float retreatChance = 0.2f; public float chaseSpeedMultiplier = 1.2f;
            public float circleDuration = 2.5f; public float guardDuration = 1.5f;
            public float retreatDistance = 3f;
            public bool  enablePatrol = true;
            public float patrolRadius = 5f; public float patrolWaitTime = 2f;
            public List<PhaseJson> phases = new();
        }

        [Serializable] private class PhaseJson
        {
            public string phaseName = "Phase";
            public float hpThreshold;
            public float continueAttackChance; public float guardChance;
            public float retreatChance; public float chargeChance; public float flankChance;
            public float chaseSpeedMultiplier = 1.2f;
            public bool  allowCharge; public bool allowFlank;
            public int   maxConsecutiveAttacks = 3;
        }

        [Serializable] private class AttackDataJson
        {
            public float globalCooldown = 1;
            public List<SkillJson> skills = new();
        }

        [Serializable] private class SkillJson
        {
            public int    skillType; public float selectionWeight;
            public float  minRange;  public float maxRange;
            public float  cooldown;
            public ConditionGroupJson conditionGroup;
            public BaseInfoJson baseInfo;
        }

        [Serializable] private class BaseInfoJson
        {
            public int    animKey; public int attackType; public int reactionType;
            public float  damage;  public float poiseDamage;
            public Vec3Json attackOffset; public float attackRadius;
            public string hitParticleName;
            public float  knockbackForce;
            public float  knockbackDrag;
            public float  airborneForce;
            public float  pullForce;
        }

        [Serializable] private class Vec3Json { public float x, y, z; }

        [Serializable] private class ConditionGroupJson
        {
            public int conditionOperator;
            public List<ConditionJson> conditions = new();
        }

        [Serializable] private class ConditionJson
        {
            public int   type;
            public float minHealthPercent; public float maxHealthPercent = 1;
            public float minRange;         public float maxRange = 10;
            public int   minAllyCount;     public int   maxAllyCount = 99;
            public int   checkSpawnCount;
        }

        // ── EditorWindow ────────────────────────────────────────────

        private TextAsset _jsonAsset;
        private string _attackDataPath = "Assets/10.Datas/Actor/Enemy/AttackData";
        private string _statDataPath   = "Assets/10.Datas/Actor/Enemy/StatData";
        private string _poiseDataPath  = "Assets/10.Datas/Actor/Enemy/PoiseData";
        private string _behaviorPath   = "Assets/10.Datas/Actor/Enemy/BehaviorData";
        private Vector2 _scroll;
        private string _log;

        [MenuItem("UPlayGround/Actor/Data/Import Monster Data")]
        public static void Open() => GetWindow<MonsterDataImporter>("Monster Data Importer");

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Monster Data Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _jsonAsset      = (TextAsset)EditorGUILayout.ObjectField("JSON 파일", _jsonAsset, typeof(TextAsset), false);
            _statDataPath   = EditorGUILayout.TextField("StatData 경로",   _statDataPath);
            _poiseDataPath  = EditorGUILayout.TextField("PoiseData 경로",  _poiseDataPath);
            _behaviorPath   = EditorGUILayout.TextField("BehaviorData 경로", _behaviorPath);
            _attackDataPath = EditorGUILayout.TextField("AttackData 경로", _attackDataPath);

            EditorGUILayout.Space(8);
            GUI.enabled = _jsonAsset != null;
            if (GUILayout.Button("SO 생성 / 덮어쓰기", GUILayout.Height(36)))
                Import();
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(_log))
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("결과", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_log, MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Import ──────────────────────────────────────────────────

        private void Import()
        {
            Root root;
            try { root = JsonUtility.FromJson<Root>(_jsonAsset.text); }
            catch (Exception e) { _log = $"JSON 파싱 실패:\n{e.Message}"; return; }

            if (root?.monsters == null || root.monsters.Count == 0)
            { _log = "monsters 배열이 비어 있습니다."; return; }

            EnsureDirectory(_statDataPath);
            EnsureDirectory(_poiseDataPath);
            EnsureDirectory(_behaviorPath);
            EnsureDirectory(_attackDataPath);

            var sb = new System.Text.StringBuilder();
            int count = 0;

            foreach (var m in root.monsters)
            {
                if (string.IsNullOrEmpty(m._id)) continue;
                WriteStatSO(m, sb);
                WritePoiseSO(m, sb);
                WriteBehaviorSO(m, sb);
                WriteAttackSO(m, sb);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _log = $"{count}개 몬스터 처리 완료.\n\n{sb}";
            Debug.Log(_log);
        }

        // ── StatData ────────────────────────────────────────────────

        private void WriteStatSO(MonsterJson m, System.Text.StringBuilder sb)
        {
            string path = $"{_statDataPath}/StatData_{m._id}.asset";
            var so = LoadOrCreate<EnemyStatsSO>(path);
            var s  = m.stats ?? new StatsJson();

            so.maxHealth            = s.maxHealth;
            so.walkSpeed            = s.walkSpeed;
            so.runSpeed             = s.runSpeed;
            so.chaseSpeedMultiplier = s.chaseSpeedMultiplier;
            so.detectionRadius      = s.detectionRadius;
            so.lostTargetRadius     = s.lostTargetRadius;
            so.fieldOfView          = s.fieldOfView;
            so.attackRange          = s.attackRange;
            so.attackCooldown       = s.attackCooldown;
            so.enablePatrol         = s.enablePatrol;
            so.patrolRadius         = s.patrolRadius;
            so.patrolWaitTime       = s.patrolWaitTime;

            EditorUtility.SetDirty(so);
            sb.AppendLine($"[Stat]     {m._id} → {path}");
        }

        // ── PoiseData ────────────────────────────────────────────────

        private void WritePoiseSO(MonsterJson m, System.Text.StringBuilder sb)
        {
            string path = $"{_poiseDataPath}/PoiseData_{m._id}.asset";
            var so = LoadOrCreate<PoiseSO>(path);
            var p  = m.poise ?? new PoiseJson();

            so.maxPoise      = p.maxPoise;
            so.recoveryDelay = p.recoveryDelay;
            so.recoveryRate  = p.recoveryRate;
            so.hasHyperArmor = p.hasHyperArmor;

            EditorUtility.SetDirty(so);
            sb.AppendLine($"[Poise]    {m._id} → {path}");
        }

        // ── BehaviorData ─────────────────────────────────────────────

        private void WriteBehaviorSO(MonsterJson m, System.Text.StringBuilder sb)
        {
            string path = $"{_behaviorPath}/BehaviorData_{m._id}.asset";
            var so = LoadOrCreate<EnemyBehaviorSO>(path);
            var b  = m.brain ?? new BrainJson();

            so.optimalCombatDistance = b.optimalCombatDistance;
            so.minCombatDistance     = b.minCombatDistance;
            so.maintainDistance      = b.maintainDistance;
            so.continueAttackChance  = b.continueAttackChance;
            so.guardChance           = b.guardChance;
            so.retreatChance         = b.retreatChance;
            so.chaseSpeedMultiplier  = b.chaseSpeedMultiplier;
            so.circleDuration        = b.circleDuration;
            so.guardDuration         = b.guardDuration;
            so.retreatDistance       = b.retreatDistance;
            so.enablePatrol          = b.enablePatrol;
            so.patrolRadius          = b.patrolRadius;
            so.patrolWaitTime        = b.patrolWaitTime;

            // 페이즈 변환
            so.phases = new BehaviorPhase[b.phases?.Count ?? 0];
            for (int i = 0; i < so.phases.Length; i++)
            {
                var pj = b.phases[i];
                so.phases[i] = new BehaviorPhase
                {
                    phaseName             = pj.phaseName,
                    hpThreshold           = pj.hpThreshold,
                    continueAttackChance  = pj.continueAttackChance,
                    guardChance           = pj.guardChance,
                    retreatChance         = pj.retreatChance,
                    chargeChance          = pj.chargeChance,
                    flankChance           = pj.flankChance,
                    chaseSpeedMultiplier  = pj.chaseSpeedMultiplier,
                    allowCharge           = pj.allowCharge,
                    allowFlank            = pj.allowFlank,
                    maxConsecutiveAttacks = pj.maxConsecutiveAttacks,
                };
            }

            EditorUtility.SetDirty(so);
            sb.AppendLine($"[Behavior] {m._id} → {path} ({so.phases.Length}페이즈)");
        }

        // ── AttackData ───────────────────────────────────────────────

        private void WriteAttackSO(MonsterJson m, System.Text.StringBuilder sb)
        {
            string path = $"{_attackDataPath}/{m._id}.asset";
            var so = LoadOrCreate<EnemyAttackDataSO>(path);

            so.globalCooldown = m.attackData?.globalCooldown ?? 1f;
            so.skills = new List<EnemyAttackInfo>();

            foreach (var sj in m.attackData?.skills ?? new List<SkillJson>())
                so.skills.Add(ConvertSkill(sj));

            EditorUtility.SetDirty(so);
            sb.AppendLine($"[Attack]   {m._id} → {path} ({so.skills.Count}스킬)");
        }

        private EnemyAttackInfo ConvertSkill(SkillJson sj) => new()
        {
            skillType       = (SkillType)sj.skillType,
            selectionWeight = sj.selectionWeight,
            minRange        = sj.minRange,
            maxRange        = sj.maxRange,
            cooldown        = sj.cooldown,
            conditionGroup  = ConvertConditionGroup(sj.conditionGroup),
            baseInfo        = ConvertBaseInfo(sj.baseInfo),
        };

        private AttackInfoBase ConvertBaseInfo(BaseInfoJson bj)
        {
            if (bj == null) return new AttackInfoBase();

            var info = new AttackInfoBase
            {
                animKey    = (AnimKey)bj.animKey,
                attackType = (AttackType)bj.attackType,
            };

            // 하위 호환 프로퍼티는 getter-only → hitPhases[0]에 직접 기입
            var phase = info.hitPhases[0];
            phase.reactionType    = (AttackReactionType)bj.reactionType;
            phase.damage          = bj.damage;
            phase.poiseDamage     = bj.poiseDamage;
            phase.attackOffset    = bj.attackOffset != null
                                        ? new Vector3(bj.attackOffset.x, bj.attackOffset.y, bj.attackOffset.z)
                                        : Vector3.zero;
            phase.attackRadius    = bj.attackRadius;
            phase.hitParticleName = bj.hitParticleName ?? "";
            phase.knockBackForce  = bj.knockbackForce;
            phase.knockBackDrag   = bj.knockbackDrag > 0f ? bj.knockbackDrag : 20f; // 기존 데이터 호환: 없으면 기본값
            phase.airborneForce   = bj.airborneForce;
            phase.pullForce       = bj.pullForce;

            return info;
        }

        private SkillConditionGroup ConvertConditionGroup(ConditionGroupJson cg)
        {
            var group = new SkillConditionGroup
            {
                conditionOperator = cg == null ? ConditionOperator.And : (ConditionOperator)cg.conditionOperator,
                conditions        = new List<SkillCondition>(),
            };
            if (cg?.conditions == null) return group;
            foreach (var c in cg.conditions)
            {
                group.conditions.Add(new SkillCondition
                {
                    type             = (ConditionType)c.type,
                    minHealthPercent = c.minHealthPercent,
                    maxHealthPercent = c.maxHealthPercent,
                    minRange         = c.minRange,
                    maxRange         = c.maxRange,
                    minAllyCount     = c.minAllyCount,
                    maxAllyCount     = c.maxAllyCount,
                    checkSpawnCount  = c.checkSpawnCount,
                });
            }
            return group;
        }

        // ── 유틸 ────────────────────────────────────────────────────

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var so = CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
