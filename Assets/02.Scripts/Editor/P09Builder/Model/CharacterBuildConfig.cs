using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor.P09Builder
{
    [Serializable]
    public class CharacterBuildConfig
    {
        // ---------- Actor ----------
        public BuilderActorKind ActorKind = BuilderActorKind.Enemy;
        public CharacterActorType PlayerCharacterType = CharacterActorType.None;

        // ---------- Body ----------
        public BuilderSex Sex = BuilderSex.Male;
        public ScriptableObject BustSizeSo;

        // ---------- Armor ----------
        public ArmorSelectionMap ArmorSelections = new ArmorSelectionMap();

        // ---------- Appearance ----------
        public ScriptableObject HairStyleSo;
        public ScriptableObject HairColorSo;
        public ScriptableObject FaceTypeSo;
        public ScriptableObject EmotionSo;
        public ScriptableObject FacialHairSo;
        public int FacialHairId = 0;
        public ScriptableObject EyeColorSo;
        public ScriptableObject SkinColorSo;
        public bool IsRandomAppearance = false;

        // ---------- Weapons ----------
        public bool UseWeaponGroup = false;
        public ScriptableObject WeaponGroupSo;
        public ScriptableObject SwordSo;
        public ScriptableObject SubSwordSo;
        public ScriptableObject GreatSwordSo;
        public ScriptableObject ShieldSo;
        public ScriptableObject BowSo;
        public ScriptableObject StaffSo;
        public ScriptableObject SpearSo;
        public ScriptableObject DualAxeSo;
        public ScriptableObject WhipSo;
        public bool ShowArrows = false;

        // ---------- Physics ----------
        public bool UseMagicaCloth = true;

        // ---------- Stats ----------
        public StatsAssignment Stats = new StatsAssignment();

        // ---------- Cycle ----------
        public CycleBuildSettings Cycle = new CycleBuildSettings();

        // ---------- Naming / Save ----------
        public bool UseManualName = false;
        public string ManualName = string.Empty;
        public string SaveBaseFolder = "Assets/03.Prefabs/Characters";

        public void Reset()
        {
            ActorKind = BuilderActorKind.Enemy;
            PlayerCharacterType = CharacterActorType.None;
            Sex = BuilderSex.Male;
            BustSizeSo = null;
            ArmorSelections = new ArmorSelectionMap();
            HairStyleSo = null;
            HairColorSo = null;
            FaceTypeSo = null;
            EmotionSo = null;
            FacialHairSo = null;
            FacialHairId = 0;
            EyeColorSo = null;
            SkinColorSo = null;
            IsRandomAppearance = false;
            UseWeaponGroup = false;
            WeaponGroupSo = null;
            SwordSo = null;
            SubSwordSo = null;
            GreatSwordSo = null;
            ShieldSo = null;
            BowSo = null;
            StaffSo = null;
            SpearSo = null;
            DualAxeSo = null;
            WhipSo = null;
            ShowArrows = false;
            UseMagicaCloth = true;
            Stats = new StatsAssignment();
            Cycle = new CycleBuildSettings();
            UseManualName = false;
            ManualName = string.Empty;
            SaveBaseFolder = "Assets/03.Prefabs/Characters";
        }

        public IEnumerable<string> Validate()
        {
            if (UseManualName && string.IsNullOrWhiteSpace(ManualName))
                yield return "수동 이름이 비어있습니다.";

            if (string.IsNullOrWhiteSpace(SaveBaseFolder))
                yield return "저장 폴더가 비어있습니다.";

            if (ActorKind == BuilderActorKind.Player &&
                PlayerCharacterType == CharacterActorType.None)
                yield return "플레이어 캐릭터 타입이 None입니다.";

            if (ActorKind == BuilderActorKind.Enemy)
            {
                if (Stats == null)
                {
                    yield return "Stats 설정이 비어있습니다.";
                    yield break;
                }
                if (!Stats.createNewPoise && Stats.existingPoiseSo == null)
                    yield return "기존 Poise SO를 선택해야 합니다.";
                if (!Stats.createNewBehavior && Stats.existingBehaviorSo == null)
                    yield return "기존 Behavior SO를 선택해야 합니다.";
                if (Stats.recruitableOnDefeat && Stats.recruitableAs == CharacterActorType.None)
                    yield return "회유 대상 캐릭터 타입이 None입니다.";

                if (Cycle != null && Cycle.isCycleBoss)
                {
                    if (Cycle.worldConfig == null)
                        yield return "Cycle 보스의 World Config가 비어있습니다.";
                    if (!Cycle.registerAsOuterBoss && !Cycle.registerAsCentralBoss)
                        yield return "Cycle 보스 풀(외곽/중앙)을 하나 이상 선택해야 합니다.";
                    if (Stats.recruitableOnDefeat)
                        yield return "Cycle 보스 영입은 BossAssist 경로입니다. '처치 시 파티 합류'를 해제하세요.";
                    if (Cycle.createOrUpdateBossAssist && Cycle.assistDatabase == null)
                        yield return "BossAssist 생성/갱신용 Database가 비어있습니다.";
                    if (Cycle.createOrUpdateBossAssist && Cycle.assistPrefab == null && Cycle.healAmount <= 0f)
                        yield return "효과 실행기가 필요한 BossAssist에는 전용 Assist Prefab을 지정해야 합니다.";
                    if (Cycle.createOrUpdateBossAssist && Cycle.assistPrefab != null &&
                        Cycle.assistPrefab.GetComponentInChildren<UPlayGround.MonsterActor>(true) != null)
                        yield return "Assist Prefab에 MonsterActor가 남아있습니다. 비이동·비어그로 전용 프리팹을 사용하세요.";
                }
            }

            if (ActorKind == BuilderActorKind.Npc)
            {
                if (Stats == null)
                {
                    yield return "NPC 데이터 설정이 비어있습니다.";
                    yield break;
                }

                if (Stats.createNewNpcData)
                {
                    if (string.IsNullOrWhiteSpace(Stats.npcDisplayName))
                        yield return "NPC 표시 이름이 비어있습니다.";
                    if (Stats.npcHp < 0)
                        yield return "NPC HP는 0 이상이어야 합니다.";
                    if (Stats.npcInteractionCompleteDuration < 0f)
                        yield return "NPC 상호작용 유지 시간은 0 이상이어야 합니다.";
                }
                else if (Stats.existingNpcData == null)
                {
                    yield return "기존 NpcActorSO를 선택해야 합니다.";
                }

                if (Stats.wanderRadius < 0f)
                    yield return "NPC 배회 반경은 0 이상이어야 합니다.";
                if (Stats.npcWanderWaitTime < 0f)
                    yield return "NPC 배회 대기 시간은 0 이상이어야 합니다.";
            }

            if (ActorKind != BuilderActorKind.Enemy && Cycle != null && Cycle.isCycleBoss)
                yield return "Cycle 보스는 Enemy Actor만 지원합니다.";
        }
    }
}
