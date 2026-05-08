using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace Game.Editor.P09Builder
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
        public ScriptableObject EyeColorSo;
        public ScriptableObject SkinColorSo;

        // ---------- Weapons ----------
        public bool UseWeaponGroup = false;
        public ScriptableObject WeaponGroupSo;
        public ScriptableObject SwordSo;
        public ScriptableObject ShieldSo;
        public ScriptableObject BowSo;
        public ScriptableObject StaffSo;
        public bool ShowArrows = false;

        // ---------- Physics ----------
        public bool UseMagicaCloth = true;

        // ---------- Stats ----------
        public StatsAssignment Stats = new StatsAssignment();

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
            EyeColorSo = null;
            SkinColorSo = null;
            UseWeaponGroup = false;
            WeaponGroupSo = null;
            SwordSo = null;
            ShieldSo = null;
            BowSo = null;
            StaffSo = null;
            ShowArrows = false;
            UseMagicaCloth = true;
            Stats = new StatsAssignment();
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
                if (!Stats.createNewStats && Stats.existingStatsSo == null)
                    yield return "기존 Stats SO를 선택해야 합니다.";
                if (!Stats.createNewBehavior && Stats.existingBehaviorSo == null)
                    yield return "기존 Behavior SO를 선택해야 합니다.";
                if (Stats.recruitableOnDefeat && Stats.recruitableAs == CharacterActorType.None)
                    yield return "회유 대상 캐릭터 타입이 None입니다.";
            }
        }
    }
}
