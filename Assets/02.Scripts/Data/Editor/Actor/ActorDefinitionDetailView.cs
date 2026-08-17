#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Editor.Authoring;

namespace UPlayGround.Data.Editor.Actor
{
    /// <summary>ActorDefinitionSO 상세 뷰의 표시 옵션.</summary>
    public sealed class ActorDefinitionDetailOptions
    {
        /// <summary>상단에 "데이터 저작 허브에서 열기" 버튼을 표시한다. (Inspector용)</summary>
        public bool ShowOpenHubButton;

        /// <summary>상단에 에셋 이름 + "Project에서 열기" 헤더를 표시한다. (저작 허브용)</summary>
        public bool ShowAssetHeader;

        /// <summary>연결된 SO 필드 옆에 "허브에서 열기" 버튼을 표시한다.</summary>
        public bool ShowHubLinks = true;
    }

    /// <summary>
    /// ActorDefinitionSO 상세 편집 UI를 만드는 공용 UI Toolkit 팩토리.
    ///
    /// Inspector(ActorDefinitionSOEditor), 데이터 저작 허브(ActorDomainPanel),
    /// 액터 데이터베이스 에디터가 모두 이 뷰를 사용해 동일한 섹션 구성과 디자인을 공유한다.
    /// 섹션 제목의 단일 소스는 이 파일이며, ActorDefinitionSO에는 [Header]를 두지 않는다.
    /// (두 곳에 두면 제목이 두 번 그려진다)
    /// </summary>
    public static class ActorDefinitionDetailView
    {
        // ── 카드 스타일 ───────────────────────────────────────────────
        private static Color SectionBackground => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.035f)
            : new Color(0f, 0f, 0f, 0.035f);

        private static Color SectionBorder => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.08f)
            : new Color(0f, 0f, 0f, 0.10f);

        public static VisualElement Build(SerializedObject serializedObject, ActorDefinitionDetailOptions options = null)
        {
            options ??= new ActorDefinitionDetailOptions();

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

            var asset = serializedObject.targetObject as ActorDefinitionSO;

            if (options.ShowAssetHeader && asset != null)
                root.Add(BuildAssetHeader(asset));

            if (options.ShowOpenHubButton && asset != null)
            {
                var openHub = new Button(() => DataAuthoringHubWindow.Open(ActorDomainPanel.DomainKey, asset))
                {
                    text = "데이터 저작 허브에서 열기",
                };
                openHub.style.height = 26f;
                openHub.style.marginTop = 2f;
                openHub.style.marginBottom = 2f;
                root.Add(openHub);
            }

            // ── 식별 ─────────────────────────────────────────────────
            var identity = AddSection(root, "식별");
            AddProperty(identity, serializedObject, "actorId", "Actor ID");
            AddProperty(identity, serializedObject, "displayName", "표시 이름");
            AddProperty(identity, serializedObject, "description", "설명");

            // ── Actor 기본 정보 ───────────────────────────────────────
            var basics = AddSection(root, "Actor 기본 정보");
            AddProperty(basics, serializedObject, "actorType", "Actor 타입");
            AddProperty(basics, serializedObject, "characterType", "캐릭터 타입");
            AddProperty(basics, serializedObject, "combatFaction", "전투 진영");
            AddProperty(basics, serializedObject, "targetLayerMask", "공격 대상 레이어");

            // ── 프리팹 ────────────────────────────────────────────────
            var prefab = AddSection(root, "프리팹");
            AddProperty(prefab, serializedObject, "prefab", "런타임 프리팹");

            // ── 스탯 데이터 ───────────────────────────────────────────
            var stats = AddSection(root, "스탯 데이터", out Label statsHeading);

            var playerStatHint = new HelpBox(
                "PlayerActor는 PartyConfigSO의 성장/파티 데이터와 Attribute Profile을 GAS Attribute 기본값으로 사용합니다.",
                HelpBoxMessageType.Info);
            stats.Add(playerStatHint);

            AddProperty(stats, serializedObject, "attributeProfile", "Attribute Profile");
            var attributeHint = new HelpBox("Attribute Profile이 필요합니다.", HelpBoxMessageType.Error);
            stats.Add(attributeHint);
            AddProperty(stats, serializedObject, "poiseData", "Poise Data");

            // ── 몬스터 ────────────────────────────────────────────────
            var monsterProfile = AddSection(root, "몬스터 프로필");
            AddProperty(monsterProfile, serializedObject, "monsterProfile", "몬스터 프로필");

            var monsterLegacy = AddSection(root, "몬스터 데이터 (레거시 호환)");
            AddProperty(monsterLegacy, serializedObject, "breakGaugeData", "브레이크 게이지");
            AddProperty(monsterLegacy, serializedObject, "monsterScaling", "스케일링");

            var monsterMeta = AddSection(root, "몬스터 메타");
            AddProperty(monsterMeta, serializedObject, "grade", "등급");
            AddProperty(monsterMeta, serializedObject, "level", "레벨");

            var combat = AddSection(root, "전투/AI 데이터");
            AddProperty(combat, serializedObject, "combatElement", "전투 속성");
            AddProperty(combat, serializedObject, "elementAssignmentMode", "속성 할당 방식");
            AddProperty(combat, serializedObject, "elementalAdvantageMultiplier", "속성 우위 배율");
            AddProperty(combat, serializedObject, "abilitySet", "Ability Set");
            AddProperty(combat, serializedObject, "combatDefensePolicy", "방어 정책");
            AddProperty(combat, serializedObject, "combatReactionPolicy", "리액션 정책");
            AddProperty(combat, serializedObject, "behaviorData", "AI 행동 데이터");

            var drop = AddSection(root, "드랍 데이터");
            AddLinkedProperty(drop, serializedObject, "dropTable", "드랍 테이블",
                DropDomainPanel.DomainKey, options.ShowHubLinks);

            var recruit = AddSection(root, "합류");
            AddProperty(recruit, serializedObject, "recruitableAs", "처치 시 해금 캐릭터");

            var reward = AddSection(root, "성장 보상");
            AddProperty(reward, serializedObject, "expReward", "경험치 보상");
            AddProperty(reward, serializedObject, "goldReward", "골드 보상");

            // ── NPC ──────────────────────────────────────────────────
            var npc = AddSection(root, "NPC 데이터");
            AddLinkedProperty(npc, serializedObject, "npcData", "NPC Data",
                NpcDomainPanel.DomainKey, options.ShowHubLinks);

            // ── ActorType/값에 따른 조건부 표시 ───────────────────────
            SerializedProperty actorTypeProp = serializedObject.FindProperty("actorType");
            SerializedProperty attributeProfileProp = serializedObject.FindProperty("attributeProfile");

            void UpdateConditionalSections()
            {
                var type = actorTypeProp != null ? (ActorType)actorTypeProp.intValue : ActorType.None;
                bool isPlayer  = type.HasFlag(ActorType.Player);
                bool isMonster = type.HasFlag(ActorType.Monster);
                bool isNpc     = type.HasFlag(ActorType.NPC);

                SetVisible(monsterProfile, isMonster);
                SetVisible(monsterLegacy, isMonster);
                SetVisible(monsterMeta, isMonster);
                SetVisible(combat, isMonster);
                SetVisible(drop, isMonster);
                SetVisible(recruit, isMonster);
                SetVisible(reward, isMonster);
                SetVisible(npc, isNpc);

                statsHeading.text = isMonster ? "스탯 데이터" : "스탯 데이터 (선택)";
                SetVisible(playerStatHint, isPlayer);

                bool missingProfile = attributeProfileProp != null && attributeProfileProp.objectReferenceValue == null;
                SetVisible(attributeHint, missingProfile);
                attributeHint.messageType = isMonster ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning;
            }

            UpdateConditionalSections();
            if (actorTypeProp != null)
                root.TrackPropertyValue(actorTypeProp, _ => UpdateConditionalSections());
            if (attributeProfileProp != null)
                root.TrackPropertyValue(attributeProfileProp, _ => UpdateConditionalSections());

            root.Bind(serializedObject);
            return root;
        }

        // ── 구성 헬퍼 ─────────────────────────────────────────────────
        private static VisualElement BuildAssetHeader(ActorDefinitionSO asset)
        {
            var header = new Toolbar();
            var title = new Label(string.IsNullOrWhiteSpace(asset.displayName) ? asset.name : asset.displayName);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            header.Add(spacer);

            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(asset)) { text = "Project에서 열기" });
            return header;
        }

        private static VisualElement AddSection(VisualElement parent, string title)
            => AddSection(parent, title, out _);

        private static VisualElement AddSection(VisualElement parent, string title, out Label heading)
        {
            var section = new VisualElement();
            section.style.marginTop = 8f;
            section.style.paddingLeft = 8f;
            section.style.paddingRight = 8f;
            section.style.paddingTop = 6f;
            section.style.paddingBottom = 6f;
            section.style.backgroundColor = SectionBackground;

            section.style.borderLeftWidth = 1f;
            section.style.borderRightWidth = 1f;
            section.style.borderTopWidth = 1f;
            section.style.borderBottomWidth = 1f;
            section.style.borderLeftColor = SectionBorder;
            section.style.borderRightColor = SectionBorder;
            section.style.borderTopColor = SectionBorder;
            section.style.borderBottomColor = SectionBorder;

            section.style.borderTopLeftRadius = 4f;
            section.style.borderTopRightRadius = 4f;
            section.style.borderBottomLeftRadius = 4f;
            section.style.borderBottomRightRadius = 4f;

            heading = new Label(title);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 4f;
            section.Add(heading);

            parent.Add(section);
            return section;
        }

        private static void AddProperty(
            VisualElement parent, SerializedObject serializedObject, string path, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(path);
            if (property != null)
                parent.Add(new PropertyField(property, label));
        }

        private static void AddLinkedProperty(
            VisualElement parent,
            SerializedObject serializedObject,
            string path,
            string label,
            string domainId,
            bool showHubLink)
        {
            SerializedProperty property = serializedObject.FindProperty(path);
            if (property == null)
                return;

            if (!showHubLink)
            {
                parent.Add(new PropertyField(property, label));
                return;
            }

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var field = new PropertyField(property, label);
            field.style.flexGrow = 1f;
            row.Add(field);

            var open = new Button(() =>
            {
                serializedObject.Update();
                DataAuthoringHubWindow.Open(domainId, serializedObject.FindProperty(path).objectReferenceValue);
            })
            { text = "허브에서 열기" };
            open.style.width = 92f;
            row.Add(open);

            parent.Add(row);
        }

        private static void SetVisible(VisualElement element, bool visible)
            => element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
#endif
