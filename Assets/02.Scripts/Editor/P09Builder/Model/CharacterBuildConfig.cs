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

        public GameObject ExternalModelPrefab;
        public UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet ExternalMotionSet;

        // ---------- Body ----------
        public BuilderSex Sex = BuilderSex.Male;
        public ScriptableObject BustSizeSo;

        // ---------- Armor ----------
        public ArmorSelectionMap ArmorSelections = new ArmorSelectionMap();

        // ---------- Appearance ----------
        public ScriptableObject HairStyleSo;
        public ScriptableObject HairColorSo;
        public ScriptableObject FaceTypeSo;
        public P09FaceMeshProfileSO FaceMeshProfile;
        public ScriptableObject EmotionSo;
        public ScriptableObject FacialHairSo;
        public int FacialHairId = 0;
        public ScriptableObject EyeColorSo;
        public ScriptableObject SkinColorSo;
        public bool IsRandomAppearance = false;
        public P09ToonLookProfileSO ToonLookProfile;

        // ---------- Weapons ----------
        public bool UseWeaponGroup = false;
        public ScriptableObject WeaponGroupSo;
        public ScriptableObject SwordSo;
        public ScriptableObject SubSwordSo;
        public ScriptableObject GreatSwordSo;
        public ScriptableObject ShieldSo;
        public ScriptableObject BowSo;
        public ScriptableObject StaffSo;
        public bool OverrideStaffGrip;
        public Vector3 StaffGripPosition;
        public Vector3 StaffGripRotation;
        public ScriptableObject SpearSo;
        public ScriptableObject DualAxeSo;
        public ScriptableObject WhipSo;
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
            ExternalModelPrefab = null;
            ExternalMotionSet = null;
            PlayerCharacterType = CharacterActorType.None;
            Sex = BuilderSex.Male;
            BustSizeSo = null;
            ArmorSelections = new ArmorSelectionMap();
            HairStyleSo = null;
            HairColorSo = null;
            FaceTypeSo = null;
            FaceMeshProfile = null;
            EmotionSo = null;
            FacialHairSo = null;
            FacialHairId = 0;
            EyeColorSo = null;
            SkinColorSo = null;
            IsRandomAppearance = false;
            ToonLookProfile = null;
            UseWeaponGroup = false;
            WeaponGroupSo = null;
            SwordSo = null;
            SubSwordSo = null;
            GreatSwordSo = null;
            ShieldSo = null;
            BowSo = null;
            StaffSo = null;
            OverrideStaffGrip = false;
            StaffGripPosition = Vector3.zero;
            StaffGripRotation = Vector3.zero;
            SpearSo = null;
            DualAxeSo = null;
            WhipSo = null;
            ShowArrows = false;
            UseMagicaCloth = true;
            Stats = new StatsAssignment();
            UseManualName = false;
            ManualName = string.Empty;
            SaveBaseFolder = "Assets/03.Prefabs/Characters";
        }

        public IEnumerable<string> Validate()
        {
            if (FaceMeshProfile != null)
            {
                string faceError = null;
                try { FaceMeshProfile.ValidateProfile(); }
                catch (InvalidOperationException exception) { faceError = exception.Message; }
                if (faceError != null) yield return faceError;
                if (Sex != FaceMeshProfile.sex || ExternalModelPrefab != null)
                    yield return "교체 얼굴은 프로파일과 같은 성별의 P09 베이스에서 사용하세요.";
            }
            if (ExternalModelPrefab != null)
            {
                string sourceError = null;
                try { ExternalCharacterAssembler.ValidateSource(ExternalModelPrefab); }
                catch (InvalidOperationException exception) { sourceError = exception.Message; }
                if (sourceError != null) yield return sourceError;
                if (ExternalMotionSet == null) yield return "외부 모델의 NPC 모션 세트를 선택하세요.";
                if (ActorKind != BuilderActorKind.Npc)
                    yield return "외부 모델의 Actor 생성은 현재 NPC를 지원합니다. 전투 역할은 무기 소켓·GAS 연결 저작이 필요합니다.";
                if (UseWeaponGroup || SwordSo != null || SubSwordSo != null || GreatSwordSo != null ||
                    ShieldSo != null || BowSo != null || StaffSo != null || SpearSo != null || DualAxeSo != null || WhipSo != null)
                    yield return "외부 모델은 P09 전용 무기 부품을 사용할 수 없습니다. 외부 모델 장비 프로필에서 구성하세요.";
            }

            // 에셋 생성·교체가 시작되기 전에 잘못된 톤 연결을 거부한다.
            if (ToonLookProfile != null)
            {
                string error = null;
                try { ToonLookProfile.ValidateBindings(); }
                catch (InvalidOperationException exception) { error = exception.Message; }
                if (error != null) yield return error;
            }

            if (!UseWeaponGroup && StaffSo != null &&
                (SwordSo != null || SubSwordSo != null || GreatSwordSo != null || ShieldSo != null ||
                 BowSo != null || SpearSo != null || DualAxeSo != null || WhipSo != null))
                yield return "스태프와 다른 무기를 동시에 선택할 수 없습니다.";

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
        }
    }
}
