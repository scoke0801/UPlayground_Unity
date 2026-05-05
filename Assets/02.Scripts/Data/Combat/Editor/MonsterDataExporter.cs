using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor
{
    /// <summary>
    /// EnemyStatsSO / PoiseSO / EnemyBehaviorSO / EnemyAttackDataSO → MonsterData.json 역변환
    /// Tools > UPlayGround > Export Monster Data
    ///
    /// ID 추출 규칙:
    ///   StatData_{id}.asset / PoiseData_{id}.asset / BehaviorData_{id}.asset / {id}.asset
    /// 4종 중 하나라도 있으면 monster 블록이 생성된다.
    /// </summary>
    public class MonsterDataExporter : EditorWindow
    {
        private string _statDataPath    = "Assets/10.Datas/Actor/Enemy/StatData";
        private string _poiseDataPath   = "Assets/10.Datas/Actor/Enemy/PoiseData";
        private string _behaviorPath    = "Assets/10.Datas/Actor/Enemy/BehaviorData";
        private string _attackDataPath  = "Assets/10.Datas/Actor/Enemy/AttackData";
        private string _outputPath      = "Assets/10.Datas/Actor/Enemy/MonsterData_Export.json";

        private Vector2 _scroll;
        private string  _log;

        [MenuItem("UPlayGround/Character/Actor/Data/Export Monster Data")]
        public static void Open() => GetWindow<MonsterDataExporter>("Monster Data Exporter");

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Monster Data Exporter", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _statDataPath   = EditorGUILayout.TextField("StatData 경로",    _statDataPath);
            _poiseDataPath  = EditorGUILayout.TextField("PoiseData 경로",   _poiseDataPath);
            _behaviorPath   = EditorGUILayout.TextField("BehaviorData 경로",_behaviorPath);
            _attackDataPath = EditorGUILayout.TextField("AttackData 경로",  _attackDataPath);

            EditorGUILayout.Space(4);
            _outputPath = EditorGUILayout.TextField("출력 JSON 경로", _outputPath);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("JSON 내보내기", GUILayout.Height(36)))
                Export();

            if (!string.IsNullOrEmpty(_log))
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("결과", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_log, MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Export ──────────────────────────────────────────────────

        private void Export()
        {
            // 각 폴더에서 SO 로드 후 id → SO 딕셔너리 구성
            var stats     = LoadSODict<EnemyStatsSO>   (_statDataPath,   "StatData_");
            var poises    = LoadSODict<PoiseSO>         (_poiseDataPath,  "PoiseData_");
            var behaviors = LoadSODict<EnemyBehaviorSO> (_behaviorPath,   "BehaviorData_");
            var attacks   = LoadSODict<EnemyAttackDataSO>(_attackDataPath, "");

            // 전체 id 수집 (합집합)
            var ids = new HashSet<string>();
            foreach (var k in stats.Keys)     ids.Add(k);
            foreach (var k in poises.Keys)    ids.Add(k);
            foreach (var k in behaviors.Keys) ids.Add(k);
            foreach (var k in attacks.Keys)   ids.Add(k);

            if (ids.Count == 0)
            {
                _log = "변환할 SO가 없습니다. 경로를 확인하세요.";
                return;
            }

            var sortedIds = new List<string>(ids);
            sortedIds.Sort();

            // JSON 직렬화
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine(I(1) + "\"_comment\": \"EnemyAttackDataSO + EnemyStatsSO + PoiseSO + EnemyBehaviorSO 역변환 파일\",");
            sb.AppendLine(I(1) + "\"monsters\": [");

            for (int i = 0; i < sortedIds.Count; i++)
            {
                string id = sortedIds[i];
                stats.TryGetValue(id,     out var stat);
                poises.TryGetValue(id,    out var poise);
                behaviors.TryGetValue(id, out var behavior);
                attacks.TryGetValue(id,   out var attack);

                WriteMonster(sb, id, stat, poise, behavior, attack);

                if (i < sortedIds.Count - 1) sb.AppendLine(I(2) + ",");
                else sb.AppendLine();
            }

            sb.AppendLine(I(1) + "]");
            sb.AppendLine("}");

            // 파일 저장
            string absPath = Path.GetFullPath(_outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);
            File.WriteAllText(absPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            _log = $"{sortedIds.Count}개 몬스터 → {_outputPath}";
            Debug.Log($"[MonsterDataExporter] {_log}");
        }

        // ── 몬스터 블록 직렬화 ──────────────────────────────────────

        private void WriteMonster(
            StringBuilder sb, string id,
            EnemyStatsSO stat, PoiseSO poise,
            EnemyBehaviorSO behavior, EnemyAttackDataSO attack)
        {
            sb.AppendLine(I(2) + "{");
            sb.AppendLine(I(3) + $"\"_id\": \"{id}\",");

            WriteStats(sb, stat);
            WritePoise(sb, poise);
            WriteBehavior(sb, behavior);
            WriteAttack(sb, attack);

            sb.Append(I(2) + "}");
        }

        // ── StatData ────────────────────────────────────────────────

        private void WriteStats(StringBuilder sb, EnemyStatsSO s)
        {
            sb.AppendLine(I(3) + "\"stats\": {");
            if (s == null) { sb.AppendLine(I(3) + "},"); return; }

            sb.AppendLine(F("maxHealth",            s.maxHealth,            4, true));
            sb.AppendLine(F("walkSpeed",            s.walkSpeed,            4, true));
            sb.AppendLine(F("runSpeed",             s.runSpeed,             4, true));
            sb.AppendLine(F("chaseSpeedMultiplier", s.chaseSpeedMultiplier, 4, true));
            sb.AppendLine(F("detectionRadius",      s.detectionRadius,      4, true));
            sb.AppendLine(F("lostTargetRadius",     s.lostTargetRadius,     4, true));
            sb.AppendLine(F("fieldOfView",          s.fieldOfView,          4, true));
            sb.AppendLine(F("attackRange",          s.attackRange,          4, true));
            sb.AppendLine(F("attackCooldown",       s.attackCooldown,       4, true));
            sb.AppendLine(B("enablePatrol",         s.enablePatrol,         4, true));
            sb.AppendLine(F("patrolRadius",         s.patrolRadius,         4, true));
            sb.AppendLine(F("patrolWaitTime",       s.patrolWaitTime,       4, false));

            sb.AppendLine(I(3) + "},");
        }

        // ── PoiseData ────────────────────────────────────────────────

        private void WritePoise(StringBuilder sb, PoiseSO p)
        {
            sb.AppendLine(I(3) + "\"poise\": {");
            if (p == null) { sb.AppendLine(I(3) + "},"); return; }

            sb.AppendLine(F("maxPoise",      p.maxPoise,      4, true));
            sb.AppendLine(F("recoveryDelay", p.recoveryDelay, 4, true));
            sb.AppendLine(F("recoveryRate",  p.recoveryRate,  4, true));
            sb.AppendLine(B("hasHyperArmor", p.hasHyperArmor, 4, false));

            sb.AppendLine(I(3) + "},");
        }

        // ── BehaviorData ─────────────────────────────────────────────

        private void WriteBehavior(StringBuilder sb, EnemyBehaviorSO bh)
        {
            sb.AppendLine(I(3) + "\"brain\": {");
            if (bh == null) { sb.AppendLine(I(3) + "},"); return; }

            sb.AppendLine(F("optimalCombatDistance", bh.optimalCombatDistance, 4, true));
            sb.AppendLine(F("minCombatDistance",     bh.minCombatDistance,     4, true));
            sb.AppendLine(B("maintainDistance",      bh.maintainDistance,      4, true));
            sb.AppendLine(F("continueAttackChance",  bh.continueAttackChance,  4, true));
            sb.AppendLine(F("guardChance",           bh.guardChance,           4, true));
            sb.AppendLine(F("retreatChance",         bh.retreatChance,         4, true));
            sb.AppendLine(F("chaseSpeedMultiplier",  bh.chaseSpeedMultiplier,  4, true));
            sb.AppendLine(F("circleDuration",        bh.circleDuration,        4, true));
            sb.AppendLine(F("guardDuration",         bh.guardDuration,         4, true));
            sb.AppendLine(F("retreatDistance",       bh.retreatDistance,       4, true));
            sb.AppendLine(B("enablePatrol",          bh.enablePatrol,          4, true));
            sb.AppendLine(F("patrolRadius",          bh.patrolRadius,          4, true));
            sb.AppendLine(F("patrolWaitTime",        bh.patrolWaitTime,        4, true));

            // phases
            sb.AppendLine(I(4) + "\"phases\": [");
            var phases = bh.phases ?? System.Array.Empty<BehaviorPhase>();
            for (int i = 0; i < phases.Length; i++)
            {
                WritePhase(sb, phases[i]);
                sb.AppendLine(i < phases.Length - 1 ? "," : "");
            }
            sb.AppendLine(I(4) + "]");

            sb.AppendLine(I(3) + "},");
        }

        private void WritePhase(StringBuilder sb, BehaviorPhase p)
        {
            sb.Append(I(5) + "{");
            sb.Append($" \"phaseName\": \"{p.phaseName}\",");
            sb.Append($" \"hpThreshold\": {p.hpThreshold},");
            sb.Append($" \"continueAttackChance\": {p.continueAttackChance},");
            sb.Append($" \"guardChance\": {p.guardChance},");
            sb.Append($" \"retreatChance\": {p.retreatChance},");
            sb.Append($" \"chargeChance\": {p.chargeChance},");
            sb.Append($" \"flankChance\": {p.flankChance},");
            sb.Append($" \"chaseSpeedMultiplier\": {p.chaseSpeedMultiplier},");
            sb.Append($" \"allowCharge\": {p.allowCharge.ToString().ToLower()},");
            sb.Append($" \"allowFlank\": {p.allowFlank.ToString().ToLower()},");
            sb.Append($" \"maxConsecutiveAttacks\": {p.maxConsecutiveAttacks}");
            sb.Append(" }");
        }

        // ── AttackData ───────────────────────────────────────────────

        private void WriteAttack(StringBuilder sb, EnemyAttackDataSO atk)
        {
            sb.AppendLine(I(3) + "\"attackData\": {");
            if (atk == null) { sb.AppendLine(I(3) + "}"); return; }

            sb.AppendLine(F("globalCooldown", atk.globalCooldown, 4, true));
            sb.AppendLine(I(4) + "\"skills\": [");

            var skills = atk.skills ?? new System.Collections.Generic.List<EnemyAttackInfo>();
            for (int i = 0; i < skills.Count; i++)
            {
                WriteSkill(sb, skills[i]);
                sb.AppendLine(i < skills.Count - 1 ? "," : "");
            }

            sb.AppendLine(I(4) + "]");
            sb.AppendLine(I(3) + "}");
        }

        private void WriteSkill(StringBuilder sb, EnemyAttackInfo sk)
        {
            sb.AppendLine(I(5) + "{");
            sb.AppendLine(I(6) + $"\"skillType\": {(int)sk.skillType},");
            sb.AppendLine(I(6) + $"\"selectionWeight\": {sk.selectionWeight},");
            sb.AppendLine(I(6) + $"\"minRange\": {sk.minRange},");
            sb.AppendLine(I(6) + $"\"maxRange\": {sk.maxRange},");
            sb.AppendLine(I(6) + $"\"cooldown\": {sk.cooldown},");

            WriteConditionGroup(sb, sk.conditionGroup);
            WriteBaseInfo(sb, sk.baseInfo);

            sb.Append(I(5) + "}");
        }

        private void WriteConditionGroup(StringBuilder sb, SkillConditionGroup cg)
        {
            sb.AppendLine(I(6) + "\"conditionGroup\": {");
            sb.AppendLine(I(7) + $"\"conditionOperator\": {(int)cg.conditionOperator},");
            sb.AppendLine(I(7) + "\"conditions\": [");

            var conds = cg.conditions ?? new System.Collections.Generic.List<SkillCondition>();
            for (int i = 0; i < conds.Count; i++)
            {
                var c = conds[i];
                sb.Append(I(8) + "{");
                sb.Append($" \"type\": {(int)c.type},");
                sb.Append($" \"minHealthPercent\": {c.minHealthPercent},");
                sb.Append($" \"maxHealthPercent\": {c.maxHealthPercent},");
                sb.Append($" \"minRange\": {c.minRange},");
                sb.Append($" \"maxRange\": {c.maxRange},");
                sb.Append($" \"minAllyCount\": {c.minAllyCount},");
                sb.Append($" \"maxAllyCount\": {c.maxAllyCount},");
                sb.Append($" \"checkSpawnCount\": {c.checkSpawnCount}");
                sb.Append(" }");
                sb.AppendLine(i < conds.Count - 1 ? "," : "");
            }

            sb.AppendLine(I(7) + "]");
            sb.AppendLine(I(6) + "},");
        }

        private void WriteBaseInfo(StringBuilder sb, AttackInfoBase bi)
        {
            var phase = bi.GetHitPhase(0);
            
            sb.AppendLine(I(6) + "\"baseInfo\": {");
            sb.AppendLine(I(7) + $"\"animKey\": {(int)bi.animKey},");
            sb.AppendLine(I(7) + $"\"attackType\": {(int)bi.attackType},");
            sb.AppendLine(I(7) + $"\"reactionType\": {(int)phase.reactionType},");
            sb.AppendLine(I(7) + $"\"damage\": {phase.damage},");
            sb.AppendLine(I(7) + $"\"poiseDamage\": {phase.poiseDamage},");
            sb.AppendLine(I(7) + $"\"attackOffset\": {{ \"x\": {phase.attackOffset.x}, \"y\": {phase.attackOffset.y}, \"z\": {phase.attackOffset.z} }},");
            sb.AppendLine(I(7) + $"\"attackRadius\": {phase.attackRadius},");
            sb.AppendLine(I(7) + $"\"hitParticleName\": \"{phase.hitParticleName}\",");
            // Reaction Forces — reactionType이 KnockBack/Airborne/Pull일 때만 의미 있지만 항상 내보낸다
            sb.AppendLine(I(7) + $"\"knockbackForce\": {phase.knockBackForce},");
            sb.AppendLine(I(7) + $"\"knockbackDrag\": {phase.knockBackDrag},");
            sb.AppendLine(I(7) + $"\"airborneForce\": {phase.airborneForce},");
            sb.AppendLine(I(7) + $"\"pullForce\": {phase.pullForce}");
            sb.AppendLine(I(6) + "}");
        }

        // ── SO 폴더 스캔 ─────────────────────────────────────────────

        /// <summary>
        /// 지정 폴더에서 T 타입 SO를 모두 로드해 id → SO 딕셔너리로 반환.
        /// prefix: 파일명 앞에 붙는 접두사 ("StatData_" 등). 비어 있으면 파일명 그대로 id.
        /// </summary>
        private static Dictionary<string, T> LoadSODict<T>(string folderPath, string prefix)
            where T : ScriptableObject
        {
            var dict = new Dictionary<string, T>();
            if (!AssetDatabase.IsValidFolder(folderPath)) return dict;

            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<T>(path);
                if (so == null) continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                string id = !string.IsNullOrEmpty(prefix) && fileName.StartsWith(prefix)
                    ? fileName.Substring(prefix.Length)
                    : fileName;

                dict[id] = so;
            }
            return dict;
        }

        // ── JSON 헬퍼 ────────────────────────────────────────────────

        // 들여쓰기 (탭 2스페이스 × depth)
        private static string I(int depth) => new string(' ', depth * 2);

        // float 필드 한 줄
        private static string F(string key, float val, int depth, bool comma)
            => $"{I(depth)}\"{key}\": {val}{(comma ? "," : "")}";

        // bool 필드 한 줄
        private static string B(string key, bool val, int depth, bool comma)
            => $"{I(depth)}\"{key}\": {val.ToString().ToLower()}{(comma ? "," : "")}";
    }
}
