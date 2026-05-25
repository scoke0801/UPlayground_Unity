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

            config.Stats.defaultHp = EditorGUILayout.FloatField("체력", config.Stats.defaultHp);
            config.Stats.grade     = (MonsterActorGrade)EditorGUILayout.EnumPopup("등급", config.Stats.grade);
            config.Stats.level     = Mathf.Max(1, EditorGUILayout.IntField("레벨", config.Stats.level));

            EditorGUILayout.Space(4);
            config.Stats.applyLevelScaling = EditorGUILayout.Toggle("레벨 스케일링", config.Stats.applyLevelScaling);
            if (config.Stats.applyLevelScaling)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    config.Stats.healthPerLevel = EditorGUILayout.FloatField("레벨당 체력 증가율", config.Stats.healthPerLevel);
                    config.Stats.attackPerLevel = EditorGUILayout.FloatField("레벨당 공격 증가율", config.Stats.attackPerLevel);
                }
            }

            config.Stats.applyArmorStatBonus = EditorGUILayout.Toggle("갑옷 프리셋 스탯 강화", config.Stats.applyArmorStatBonus);
            if (config.Stats.applyArmorStatBonus)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    config.Stats.armorHealthPerTier = EditorGUILayout.FloatField("갑옷 티어당 체력 증가율", config.Stats.armorHealthPerTier);
                    config.Stats.armorMoveSpeedPerTier = EditorGUILayout.FloatField("갑옷 티어당 이동 증가율", config.Stats.armorMoveSpeedPerTier);
                }
            }

            config.Stats.randomizeStatsOnBuild = EditorGUILayout.Toggle("빌드 시 스탯 랜덤 발급", config.Stats.randomizeStatsOnBuild);
            using (new EditorGUI.DisabledScope(!config.Stats.randomizeStatsOnBuild))
            using (new EditorGUI.IndentLevelScope())
            {
                config.Stats.randomStatMin = EditorGUILayout.FloatField("랜덤 최소 배율", config.Stats.randomStatMin);
                config.Stats.randomStatMax = EditorGUILayout.FloatField("랜덤 최대 배율", config.Stats.randomStatMax);
            }

            if (GUILayout.Button("현재 스탯 랜덤 발급"))
                RandomizeEnemyStats(config);

            EditorGUILayout.Space();

            // Poise
            config.Stats.createNewPoise = EditorGUILayout.Toggle("새 Poise SO 생성", config.Stats.createNewPoise);
            if (config.Stats.createNewPoise)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    config.Stats.defaultMaxPoise = EditorGUILayout.FloatField("최대 Poise", config.Stats.defaultMaxPoise);
                    config.Stats.defaultPoiseRecoveryDelay = EditorGUILayout.FloatField("Poise 회복 지연", config.Stats.defaultPoiseRecoveryDelay);
                    config.Stats.defaultPoiseRecoveryRate = EditorGUILayout.FloatField("Poise 회복량/초", config.Stats.defaultPoiseRecoveryRate);
                    config.Stats.defaultHasHyperArmor = EditorGUILayout.Toggle("Hyper Armor 사용", config.Stats.defaultHasHyperArmor);
                }
            }
            else
            {
                config.Stats.existingPoiseSo = EditorGUILayout.ObjectField(
                    "기존 PoiseSO", config.Stats.existingPoiseSo, typeof(PoiseSO), false) as ScriptableObject;
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
            config.Stats.defaultAttackDamage = EditorGUILayout.FloatField(
                "기본 공격 데미지", config.Stats.defaultAttackDamage);
            config.Stats.applyWeaponAttackBonus = EditorGUILayout.Toggle(
                "무기 등급 공격 강화", config.Stats.applyWeaponAttackBonus);
            if (config.Stats.applyWeaponAttackBonus)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    config.Stats.weaponAttackPerTier = EditorGUILayout.FloatField(
                        "무기 티어당 공격 증가율", config.Stats.weaponAttackPerTier);
                }
            }
            if (config.Stats.attackDataSo != null)
                EditorGUILayout.HelpBox("기존 AttackData SO를 지정하면 공유 자산 보호를 위해 새 공격 데이터 보정은 적용하지 않습니다.", MessageType.Info);

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

        private static void RandomizeEnemyStats(CharacterBuildConfig config)
        {
            if (config?.Stats == null) return;

            config.Stats.level = UnityEngine.Random.Range(1, 31);
            config.Stats.defaultHp = Mathf.Round(UnityEngine.Random.Range(80f, 220f));
            config.Stats.defaultAttackDamage = Mathf.Round(UnityEngine.Random.Range(8f, 28f));
            config.Stats.grade = (MonsterActorGrade)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(MonsterActorGrade)).Length);
            GUI.changed = true;
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
