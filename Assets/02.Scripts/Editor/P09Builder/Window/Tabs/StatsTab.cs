using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;

namespace Game.Editor.P09Builder
{
    internal sealed class StatsTab : IBuilderTab
    {
        public string Title => "스탯";

        public void Initialize(P09CharacterPrefabBuilderWindow window, P09AssetCatalog catalog) { }

        public void OnGUI(CharacterBuildConfig config, P09AssetCatalog catalog, IconResolver iconResolver)
        {
            if (config == null) return;

            switch (config.ActorKind)
            {
                case BuilderActorKind.Enemy:  DrawEnemyStats(config); break;
                case BuilderActorKind.Player: DrawPlayerStats(config); break;
                case BuilderActorKind.Npc:    DrawNpcStats(config); break;
            }
        }

        public IEnumerable<string> Validate(CharacterBuildConfig config)
        {
            yield break;
        }

        // ---------- Enemy ----------
        private static void DrawEnemyStats(CharacterBuildConfig config)
        {
            EditorGUILayout.LabelField("Enemy Stats", EditorStyles.boldLabel);

            config.Stats.createNewStats = EditorGUILayout.Toggle("새 Stats SO 생성", config.Stats.createNewStats);
            if (config.Stats.createNewStats)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    config.Stats.defaultHp              = EditorGUILayout.FloatField("체력", config.Stats.defaultHp);
                    config.Stats.defaultWalkSpeed       = EditorGUILayout.FloatField("이동속도(걷기)", config.Stats.defaultWalkSpeed);
                    config.Stats.defaultRunSpeed        = EditorGUILayout.FloatField("이동속도(달리기)", config.Stats.defaultRunSpeed);
                    config.Stats.defaultDetectionRadius = EditorGUILayout.FloatField("탐지 반경", config.Stats.defaultDetectionRadius);
                    config.Stats.grade                  = (MonsterActorGrade)EditorGUILayout.EnumPopup("등급", config.Stats.grade);
                }
            }
            else
            {
                config.Stats.existingStatsSo = EditorGUILayout.ObjectField(
                    "기존 StatsSO", config.Stats.existingStatsSo, typeof(EnemyStatsSO), false) as ScriptableObject;
            }

            EditorGUILayout.Space();

            // Behavior
            config.Stats.createNewBehavior = EditorGUILayout.Toggle("새 Behavior SO 생성", config.Stats.createNewBehavior);
            if (config.Stats.createNewBehavior)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    config.Stats.optimalCombatDistance = EditorGUILayout.FloatField(
                        "최적 전투거리", config.Stats.optimalCombatDistance);
                }
            }
            else
            {
                config.Stats.existingBehaviorSo = EditorGUILayout.ObjectField(
                    "기존 BehaviorSO", config.Stats.existingBehaviorSo, typeof(EnemyBehaviorSO), false) as ScriptableObject;
            }

            EditorGUILayout.Space();

            // AttackData
            EditorGUILayout.LabelField("공격 데이터", EditorStyles.boldLabel);
            config.Stats.attackDataSo = EditorGUILayout.ObjectField(
                "AttackData SO", config.Stats.attackDataSo, typeof(EnemyAttackDataSO), false) as ScriptableObject;
            config.Stats.combatStyle = (EnemyCombatStyle)EditorGUILayout.EnumPopup(
                "전투 스타일", config.Stats.combatStyle);

            EditorGUILayout.Space();

            // 회유
            config.Stats.recruitableOnDefeat = EditorGUILayout.Toggle(
                "처치 시 파티 합류", config.Stats.recruitableOnDefeat);
            if (config.Stats.recruitableOnDefeat)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    config.Stats.recruitableAs = (CharacterActorType)EditorGUILayout.EnumPopup(
                        "합류 캐릭터", config.Stats.recruitableAs);
                }
            }
        }

        // ---------- Player ----------
        private static void DrawPlayerStats(CharacterBuildConfig config)
        {
            EditorGUILayout.HelpBox("Player Stats (Phase 2 예정)", MessageType.Info);

            config.Stats.playerAttackDataSo = EditorGUILayout.ObjectField(
                "Player AttackData SO",
                config.Stats.playerAttackDataSo,
                typeof(ScriptableObject), false) as ScriptableObject;

            config.Stats.addToStartingParty = EditorGUILayout.Toggle(
                "시작 파티 포함", config.Stats.addToStartingParty);
            if (config.Stats.addToStartingParty)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    config.Stats.partyOrder = EditorGUILayout.IntField("파티 순서", config.Stats.partyOrder);
                }
            }
        }

        // ---------- NPC ----------
        private static void DrawNpcStats(CharacterBuildConfig config)
        {
            EditorGUILayout.HelpBox("NPC Stats (Phase 2 예정)", MessageType.Info);

            config.Stats.dialogueSo = EditorGUILayout.ObjectField(
                "대화 SO", config.Stats.dialogueSo, typeof(ScriptableObject), false) as ScriptableObject;

            config.Stats.wanderRadius = EditorGUILayout.FloatField("배회 반경", config.Stats.wanderRadius);
        }
    }
}
