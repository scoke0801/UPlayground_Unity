using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Ability.Core;
using UPlayGround.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Editor.Ability.Production
{
    public enum AbilityProductionWorkflow
    {
        ComposeAbilitySet,
        CreateAbilityFromRecipe,
    }

    public sealed class GameplayAbilityProductionWizardWindow : EditorWindow
    {
        [SerializeField] private AbilityProductionWorkflow _workflow =
            AbilityProductionWorkflow.ComposeAbilitySet;
        [SerializeField] private AbilitySetSO _compositionBaseSet;
        [SerializeField] private List<GameplayAbilitySO> _compositionAbilities =
            new();
        [SerializeField] private List<AbilitySetSO.AbilityOverrideEntry>
            _compositionOverrides = new();
        [SerializeField] private string _compositionAssetName =
            "AbilitySet_Common";
        [SerializeField] private string _compositionSaveRoot =
            "Assets/10.Datas/Ability/Sets";
        [SerializeField] private MonsterActorProfileSO
            _compositionTargetProfile;
        [SerializeField] private ActorDefinitionSO
            _compositionTargetDefinition;
        [SerializeField] private AbilitySetSO _targetSet;
        [SerializeField] private ActorAnimationMotionSet _motionOwner;
        [SerializeField] private MotionSetAsset _motion;
        [SerializeField] private AbilityTaskGraphSO _taskGraph;
        [SerializeField] private GameplayEffectSO _commitEffect;
        [SerializeField] private GameplayEffectSO _endEffect;
        [SerializeField] private bool _createCommitEffect;
        [SerializeField] private string _effectId = "Effect.Actor.New";
        [SerializeField] private string _effectAssetName = "NewEffect";
        [SerializeField] private GameplayEffectPolarity _effectPolarity;
        [SerializeField] private GameplayEffectDurationType _effectDurationType;
        [SerializeField] private float _effectDurationSeconds;
        [SerializeField] private string _effectAttributeId;
        [SerializeField] private ModifierType _effectModifierType =
            ModifierType.Flat;
        [SerializeField] private float _effectModifierValue;
        [SerializeField] private int _recipeIndex = 2;
        [SerializeField] private AbilitySetBindingMode _bindingMode =
            AbilitySetBindingMode.AdditionalAbilities;
        [SerializeField] private PlayerSkillSlot _playerSkillSlot;
        [SerializeField] private PlayerCombatAbilitySlot _playerCombatSlot;
        [SerializeField] private bool _replaceExistingBinding;
        [SerializeField] private string _displayName = "새 몬스터 공격";
        [SerializeField] private string _abilityId = "Monster.Actor.Attack";
        [SerializeField] private string _assetName = "MonsterAttack";
        [SerializeField] private string _saveRoot =
            "Assets/10.Datas/Ability/Actor";
        [SerializeField] private int _requiredLevel = 1;
        [SerializeField] private float _selectionWeight = 10f;
        [SerializeField] private float _minDistance;
        [SerializeField] private float _maxDistance = 3f;

        private Vector2 _scroll;
        private AbilityCreationPlan _preview;
        private string _resultMessage;
        private MessageType _resultType = MessageType.Info;
        private VisualElement _previewRoot;
        private HelpBox _resultBox;
        private Button _applyButton;
        private AbilitySetCompositionPlan _compositionPreview;
        private VisualElement _compositionPreviewRoot;
        private Button _compositionApplyButton;
        private HelpBox _compositionResultBox;

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/Ability Production Wizard")]
        public static void Open()
        {
            GameplayAbilityProductionWizardWindow window =
                GetWindow<GameplayAbilityProductionWizardWindow>();
            window.titleContent = new GUIContent("Ability Production");
            window.minSize = new Vector2(620f, 680f);
            window.Show();
        }

        public static void OpenForSelection(
            IEnumerable<UnityEngine.Object> selection)
        {
            Open();
            GameplayAbilityProductionWizardWindow window =
                GetWindow<GameplayAbilityProductionWizardWindow>();
            window._workflow = AbilityProductionWorkflow.ComposeAbilitySet;
            window._compositionAbilities.Clear();
            foreach (UnityEngine.Object item in selection
                     ?? System.Array.Empty<UnityEngine.Object>())
            {
                switch (item)
                {
                    case GameplayAbilitySO ability:
                        if (!window._compositionAbilities.Contains(ability))
                            window._compositionAbilities.Add(ability);
                        break;
                    case AbilitySetSO set:
                        window._compositionBaseSet = set;
                        break;
                    case MonsterActorProfileSO profile:
                        window._compositionTargetProfile = profile;
                        window._compositionBaseSet = profile.abilitySet;
                        break;
                    case ActorDefinitionSO definition:
                        window._compositionTargetDefinition = definition;
                        if (definition.monsterProfile != null)
                        {
                            window._compositionTargetProfile =
                                definition.monsterProfile;
                            window._compositionBaseSet =
                                definition.monsterProfile.abilitySet;
                        }
                        break;
                }
            }
            window._compositionPreview = null;
            window.rootVisualElement.schedule.Execute(window.CreateGUI);
        }

        private void OnEnable()
        {
            if (_taskGraph == null)
            {
                _taskGraph = AssetDatabase.LoadAssetAtPath<AbilityTaskGraphSO>(
                    AbilityRecipeCatalog.SharedMotionTaskGraphPath);
            }
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 12f;
            rootVisualElement.style.paddingRight = 12f;
            rootVisualElement.style.paddingTop = 10f;
            rootVisualElement.style.paddingBottom = 10f;

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            rootVisualElement.Add(scroll);

            var title = new Label("Ability 양산화 Wizard");
            title.style.fontSize = 16f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 8f;
            scroll.Add(title);
            scroll.Add(new HelpBox(
                "기본 흐름은 선택 Ability를 공용 AbilitySet으로 묶거나 Base Set에서 "
                + "파생된 특수 Set을 만드는 것입니다. 새 Ability가 필요한 경우에만 "
                + "레시피 생성 워크플로를 사용합니다.",
                HelpBoxMessageType.Info));

            SerializedObject serialized = new(this);
            var workflowField = new EnumField("작업 흐름", _workflow);
            scroll.Add(workflowField);

            VisualElement compositionRoot =
                BuildCompositionUI(serialized);
            scroll.Add(compositionRoot);

            var legacyRoot = new VisualElement();
            var legacyHeader = new HelpBox(
                "신규 Ability와 Payload가 필요한 경우 사용하는 보조 제작 흐름입니다.",
                HelpBoxMessageType.Info);
            legacyRoot.Add(legacyHeader);
            VisualElement recipe = CreateSection(
                "1. 제작 기준",
                "레시피는 기본 구조만 제공하며 기존 데이터의 의미를 추측하지 않습니다.");
            var recipeChoices = new System.Collections.Generic.List<string>();
            for (int i = 0; i < AbilityRecipeCatalog.All.Count; i++)
            {
                AbilityRecipeDefinition item = AbilityRecipeCatalog.All[i];
                recipeChoices.Add(
                    $"{item.DisplayName} · {item.RecipeId} · v{item.Version}");
            }
            _recipeIndex = Mathf.Clamp(
                _recipeIndex,
                0,
                Mathf.Max(0, recipeChoices.Count - 1));
            var recipeField = new DropdownField(
                "표준 레시피",
                recipeChoices,
                _recipeIndex);
            recipeField.RegisterValueChangedCallback(evt =>
            {
                int index = recipeChoices.IndexOf(evt.newValue);
                if (index < 0)
                    return;
                _recipeIndex = index;
                _bindingMode = AbilityRecipeCatalog.All[index].BindingMode;
                serialized.Update();
                serialized.FindProperty("_recipeIndex").intValue = index;
                serialized.FindProperty("_bindingMode").enumValueIndex =
                    (int)_bindingMode;
                serialized.ApplyModifiedProperties();
                _preview = null;
                RefreshPreview();
            });
            recipe.Add(recipeField);
            legacyRoot.Add(recipe);

            VisualElement target = CreateSection(
                "2. 대상 Set과 실행 데이터",
                "생성된 Ability가 연결될 Set과 Motion/TaskGraph를 명시합니다.");
            AddProperty(target, serialized, "_targetSet", "대상 AbilitySet");
            AddProperty(target, serialized, "_motionOwner", "Actor MotionSet");
            AddProperty(target, serialized, "_motion", "Motion Asset");
            AddProperty(target, serialized, "_taskGraph", "Task Graph");
            AddProperty(target, serialized, "_bindingMode", "AbilitySet 연결");
            AddProperty(target, serialized, "_playerSkillSlot", "플레이어 입력 슬롯");
            AddProperty(target, serialized, "_playerCombatSlot", "전투 슬롯");
            AddProperty(target, serialized, "_replaceExistingBinding", "기존 바인딩 교체");
            legacyRoot.Add(target);

            VisualElement identity = CreateSection(
                "3. 생성 에셋",
                "ID는 런타임 고유값이며, 저장 루트의 기존 경로와 충돌하면 적용이 차단됩니다.");
            AddProperty(identity, serialized, "_displayName", "표시 이름");
            AddProperty(identity, serialized, "_abilityId", "Ability ID");
            AddProperty(identity, serialized, "_assetName", "에셋 이름");
            AddProperty(identity, serialized, "_saveRoot", "저장 루트");
            AddProperty(identity, serialized, "_requiredLevel", "요구 레벨");
            AddProperty(identity, serialized, "_selectionWeight", "AI 선택 가중치");
            AddProperty(identity, serialized, "_minDistance", "최소 거리");
            AddProperty(identity, serialized, "_maxDistance", "최대 거리");
            legacyRoot.Add(identity);

            VisualElement effects = CreateSection(
                "4. Effect 연결",
                "공유 Effect는 참조만 연결합니다. 신규 생성은 ID와 Modifier 의미를 "
                + "직접 지정한 경우에만 사용하세요.");
            AddProperty(effects, serialized, "_createCommitEffect", "Commit Effect 신규 생성");
            AddProperty(effects, serialized, "_commitEffect", "Commit Effect 공유");
            AddProperty(effects, serialized, "_endEffect", "End Effect");
            AddProperty(effects, serialized, "_effectId", "Effect ID");
            AddProperty(effects, serialized, "_effectAssetName", "Effect 에셋 이름");
            AddProperty(effects, serialized, "_effectPolarity", "Effect 극성");
            AddProperty(effects, serialized, "_effectDurationType", "Effect 수명");
            AddProperty(effects, serialized, "_effectDurationSeconds", "지속시간");
            AddProperty(effects, serialized, "_effectAttributeId", "Modifier Attribute ID");
            AddProperty(effects, serialized, "_effectModifierType", "Modifier 방식");
            AddProperty(effects, serialized, "_effectModifierValue", "Modifier 값");
            legacyRoot.Add(effects);

            VisualElement actions = CreateSection(
                "5. Preview와 적용",
                "Preview의 오류가 0개일 때만 적용할 수 있습니다.");
            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            var previewButton = new Button(() =>
            {
                serialized.ApplyModifiedProperties();
                _preview = AbilityCreationPlanner.Build(BuildRequest());
                _resultMessage = null;
                RefreshPreview();
            }) { text = "생성 계획 Preview" };
            previewButton.style.flexGrow = 1f;
            actionRow.Add(previewButton);
            _applyButton = new Button(() =>
            {
                AbilityProductionResult result =
                    AbilityAssetFactory.Apply(_preview);
                _resultMessage = result.Message;
                _resultType = result.Success
                    ? MessageType.Info
                    : MessageType.Error;
                if (result.Success)
                    _preview = null;
                RefreshPreview();
            }) { text = "계획 적용" };
            _applyButton.style.flexGrow = 1f;
            _applyButton.style.marginLeft = 6f;
            actionRow.Add(_applyButton);
            actions.Add(actionRow);
            _previewRoot = new VisualElement();
            _previewRoot.style.marginTop = 8f;
            actions.Add(_previewRoot);
            _resultBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            _resultBox.style.marginTop = 8f;
            actions.Add(_resultBox);
            legacyRoot.Add(actions);
            scroll.Add(legacyRoot);

            void RefreshWorkflow()
            {
                compositionRoot.style.display =
                    _workflow == AbilityProductionWorkflow.ComposeAbilitySet
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                legacyRoot.style.display =
                    _workflow
                    == AbilityProductionWorkflow.CreateAbilityFromRecipe
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }
            workflowField.RegisterValueChangedCallback(evt =>
            {
                _workflow = (AbilityProductionWorkflow)evt.newValue;
                serialized.Update();
                serialized.FindProperty("_workflow").enumValueIndex =
                    (int)_workflow;
                serialized.ApplyModifiedProperties();
                RefreshWorkflow();
            });

            scroll.Bind(serialized);
            RefreshWorkflow();
            RefreshCompositionPreview();
            RefreshPreview();
        }

        private VisualElement BuildCompositionUI(SerializedObject serialized)
        {
            var root = new VisualElement();
            VisualElement source = CreateSection(
                "1. 공용/파생 Set 입력",
                "Base Set이 비어 있으면 선택 Ability로 독립 공용 Set을 만듭니다. "
                + "Base Set이 있으면 추가 Ability와 Replace/Remove만 저장하는 파생 Set을 만듭니다.");
            AddProperty(source, serialized, "_compositionBaseSet", "공용 Base Set");
            AddProperty(source, serialized, "_compositionAbilities", "추가할 Ability");
            AddProperty(source, serialized, "_compositionOverrides", "Replace / Remove");
            root.Add(source);

            VisualElement output = CreateSection(
                "2. 저장과 연결",
                "일반 타입 공용 Set은 MonsterProfile에, 특수 파생 Set은 ActorDefinition에 연결합니다.");
            AddProperty(output, serialized, "_compositionAssetName", "Set 에셋 이름");
            AddProperty(output, serialized, "_compositionSaveRoot", "저장 루트");
            AddProperty(output, serialized, "_compositionTargetProfile", "연결할 MonsterProfile");
            AddProperty(output, serialized, "_compositionTargetDefinition", "연결할 특수 ActorDefinition");
            root.Add(output);

            VisualElement actions = CreateSection(
                "3. Preview와 적용",
                "경로·Base 포함 여부·Override 중복·Profile 연결 조건을 검사한 뒤 적용합니다.");
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row },
            };
            var preview = new Button(() =>
            {
                serialized.ApplyModifiedProperties();
                _compositionPreview =
                    AbilitySetCompositionService.Build(
                        BuildCompositionRequest());
                _resultMessage = null;
                RefreshCompositionPreview();
            }) { text = "Set 구성 Preview" };
            preview.style.flexGrow = 1f;
            row.Add(preview);
            _compositionApplyButton = new Button(() =>
            {
                AbilitySetCompositionResult result =
                    AbilitySetCompositionService.Apply(
                        _compositionPreview);
                _resultMessage = result.Message;
                _resultType = result.Success
                    ? MessageType.Info
                    : MessageType.Error;
                if (result.Success)
                    _compositionPreview = null;
                RefreshCompositionPreview();
            }) { text = "공용/파생 Set 적용" };
            _compositionApplyButton.style.flexGrow = 1f;
            _compositionApplyButton.style.marginLeft = 6f;
            row.Add(_compositionApplyButton);
            actions.Add(row);
            _compositionPreviewRoot = new VisualElement();
            actions.Add(_compositionPreviewRoot);
            _compositionResultBox =
                new HelpBox(string.Empty, HelpBoxMessageType.Info);
            actions.Add(_compositionResultBox);
            root.Add(actions);
            return root;
        }

        private AbilitySetCompositionRequest BuildCompositionRequest() =>
            new()
            {
                BaseSet = _compositionBaseSet,
                AddedAbilities = new List<GameplayAbilitySO>(
                    _compositionAbilities
                    ?? new List<GameplayAbilitySO>()),
                Overrides = new List<AbilitySetSO.AbilityOverrideEntry>(
                    _compositionOverrides
                    ?? new List<AbilitySetSO.AbilityOverrideEntry>()),
                AssetName = _compositionAssetName,
                SaveRoot = _compositionSaveRoot,
                TargetMonsterProfile = _compositionTargetProfile,
                TargetActorDefinition = _compositionTargetDefinition,
            };

        private void RefreshCompositionPreview()
        {
            if (_compositionPreviewRoot == null)
                return;
            _compositionPreviewRoot.Clear();
            _compositionApplyButton?.SetEnabled(
                _compositionPreview?.CanApply == true);
            if (_compositionPreview == null)
            {
                _compositionPreviewRoot.Add(new HelpBox(
                    "아직 Set 구성 계획이 없습니다.",
                    HelpBoxMessageType.None));
            }
            else
            {
                _compositionPreviewRoot.Add(new Label(
                    $"{(_compositionPreview.IsDerived ? "파생 Set" : "공용 Set")}"
                    + $"\n{_compositionPreview.AssetPath}"));
                for (int i = 0;
                     i < _compositionPreview.Issues.Count;
                     i++)
                {
                    AbilityProductionIssue issue =
                        _compositionPreview.Issues[i];
                    _compositionPreviewRoot.Add(new HelpBox(
                        $"[{issue.Code}] {issue.Message}",
                        issue.Severity switch
                        {
                            AbilityProductionSeverity.Error =>
                                HelpBoxMessageType.Error,
                            AbilityProductionSeverity.Warning =>
                                HelpBoxMessageType.Warning,
                            _ => HelpBoxMessageType.Info,
                        }));
                }
            }
            if (_compositionResultBox != null)
            {
                _compositionResultBox.text = _resultMessage ?? string.Empty;
                _compositionResultBox.messageType = _resultType switch
                {
                    MessageType.Error => HelpBoxMessageType.Error,
                    MessageType.Warning => HelpBoxMessageType.Warning,
                    _ => HelpBoxMessageType.Info,
                };
                _compositionResultBox.style.display =
                    string.IsNullOrWhiteSpace(_resultMessage)
                        ? DisplayStyle.None
                        : DisplayStyle.Flex;
            }
        }

        private static VisualElement CreateSection(string title, string help)
        {
            var section = new VisualElement();
            section.style.marginTop = 12f;
            section.style.paddingLeft = 10f;
            section.style.paddingRight = 10f;
            section.style.paddingTop = 8f;
            section.style.paddingBottom = 10f;
            section.style.borderLeftWidth = 1f;
            section.style.borderRightWidth = 1f;
            section.style.borderTopWidth = 1f;
            section.style.borderBottomWidth = 1f;
            var heading = new Label(title);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.Add(heading);
            var description = new Label(help);
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.opacity = 0.72f;
            description.style.marginBottom = 6f;
            section.Add(description);
            return section;
        }

        private static void AddProperty(
            VisualElement parent,
            SerializedObject serialized,
            string propertyName,
            string label)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                parent.Add(new PropertyField(property, label));
        }

        private void RefreshPreview()
        {
            if (_previewRoot == null)
                return;
            _previewRoot.Clear();
            _applyButton?.SetEnabled(_preview?.CanApply == true);

            if (_preview == null)
            {
                _previewRoot.Add(new HelpBox(
                    "아직 생성 계획이 없습니다.",
                    HelpBoxMessageType.None));
            }
            else
            {
                for (int i = 0; i < _preview.Items.Count; i++)
                {
                    AbilityPlanItem item = _preview.Items[i];
                    var row = new Label(
                        $"{item.Operation} · {item.AssetKind}\n{item.TargetPath}");
                    row.style.whiteSpace = WhiteSpace.Normal;
                    row.style.marginBottom = 5f;
                    _previewRoot.Add(row);
                }
                for (int i = 0; i < _preview.Issues.Count; i++)
                {
                    AbilityProductionIssue issue = _preview.Issues[i];
                    _previewRoot.Add(new HelpBox(
                        $"[{issue.Code}] {issue.Message}",
                        issue.Severity switch
                        {
                            AbilityProductionSeverity.Error =>
                                HelpBoxMessageType.Error,
                            AbilityProductionSeverity.Warning =>
                                HelpBoxMessageType.Warning,
                            _ => HelpBoxMessageType.Info,
                        }));
                }
            }

            if (_resultBox != null)
            {
                _resultBox.text = _resultMessage ?? string.Empty;
                _resultBox.messageType = _resultType switch
                {
                    MessageType.Error => HelpBoxMessageType.Error,
                    MessageType.Warning => HelpBoxMessageType.Warning,
                    _ => HelpBoxMessageType.Info,
                };
                _resultBox.style.display =
                    string.IsNullOrWhiteSpace(_resultMessage)
                        ? DisplayStyle.None
                        : DisplayStyle.Flex;
            }
        }

        private void DrawLegacyGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "스킬 양산화 — Phase 2",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "6종 표준 레시피와 AbilitySet의 추가 Ability·플레이어 입력 슬롯·"
                + "전투 시퀀스 연결을 지원합니다. 기존 바인딩 교체는 명시적으로 "
                + "선택해야 하며 에셋 경로 충돌은 항상 차단합니다.",
                MessageType.Info);

            DrawRecipe();
            EditorGUILayout.Space(8f);
            DrawInputs();
            EditorGUILayout.Space(12f);
            DrawPreviewControls();
            EditorGUILayout.Space(8f);
            DrawPreview();

            if (!string.IsNullOrWhiteSpace(_resultMessage))
                EditorGUILayout.HelpBox(_resultMessage, _resultType);
            EditorGUILayout.EndScrollView();
        }

        private void DrawRecipe()
        {
            var recipes = AbilityRecipeCatalog.All;
            string[] labels = new string[recipes.Count];
            for (int i = 0; i < recipes.Count; i++)
                labels[i] = $"{recipes[i].DisplayName} ({recipes[i].RecipeId})";
            _recipeIndex = Mathf.Clamp(_recipeIndex, 0, recipes.Count - 1);
            int next = EditorGUILayout.Popup("표준 레시피", _recipeIndex, labels);
            if (next != _recipeIndex)
            {
                _recipeIndex = next;
                _bindingMode = recipes[_recipeIndex].BindingMode;
                _preview = null;
            }
            AbilityRecipeDefinition recipe = recipes[_recipeIndex];
            EditorGUILayout.LabelField("레시피", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Recipe ID", recipe.RecipeId);
                EditorGUILayout.TextField("버전", recipe.Version.ToString());
            }
        }

        private void DrawInputs()
        {
            EditorGUILayout.LabelField("생성 입력", EditorStyles.boldLabel);
            _targetSet = (AbilitySetSO)EditorGUILayout.ObjectField(
                "대상 AbilitySet",
                _targetSet,
                typeof(AbilitySetSO),
                false);
            _motionOwner =
                (ActorAnimationMotionSet)EditorGUILayout.ObjectField(
                    "Actor MotionSet",
                    _motionOwner,
                    typeof(ActorAnimationMotionSet),
                    false);
            _motion =
                (MotionSetAsset)EditorGUILayout.ObjectField(
                    "Motion Asset",
                    _motion,
                    typeof(MotionSetAsset),
                    false);
            _taskGraph = (AbilityTaskGraphSO)EditorGUILayout.ObjectField(
                "Task Graph",
                _taskGraph,
                typeof(AbilityTaskGraphSO),
                false);
            _bindingMode = (AbilitySetBindingMode)EditorGUILayout.EnumPopup(
                "AbilitySet 연결",
                _bindingMode);
            if (_bindingMode == AbilitySetBindingMode.PlayerSkillSlot)
            {
                _playerSkillSlot =
                    (PlayerSkillSlot)EditorGUILayout.EnumPopup(
                        "입력 슬롯",
                        _playerSkillSlot);
            }
            else if (_bindingMode
                     == AbilitySetBindingMode.PlayerCombatSequence)
            {
                _playerCombatSlot =
                    (PlayerCombatAbilitySlot)EditorGUILayout.EnumPopup(
                        "전투 슬롯",
                        _playerCombatSlot);
            }
            if (_bindingMode != AbilitySetBindingMode.AdditionalAbilities)
            {
                _replaceExistingBinding = EditorGUILayout.Toggle(
                    "기존 바인딩 교체",
                    _replaceExistingBinding);
            }
            AbilityRecipeDefinition recipe =
                AbilityRecipeCatalog.All[_recipeIndex];
            if (recipe.SupportsEffect)
                EditorGUILayout.HelpBox(
                    "Effect는 새 의미를 추측해 자동 생성하지 않습니다. 검증된 기존 "
                    + "Effect를 Commit 또는 End 시점에 명시적으로 연결하세요.",
                    MessageType.Info);
            _createCommitEffect = EditorGUILayout.Toggle(
                "Commit Effect 신규 생성",
                _createCommitEffect);
            using (new EditorGUI.DisabledScope(_createCommitEffect))
            {
                _commitEffect = (GameplayEffectSO)EditorGUILayout.ObjectField(
                    "Commit Effect 공유",
                    _commitEffect,
                    typeof(GameplayEffectSO),
                    false);
            }
            _endEffect = (GameplayEffectSO)EditorGUILayout.ObjectField(
                "End Effect",
                _endEffect,
                typeof(GameplayEffectSO),
                false);
            if (_createCommitEffect)
            {
                _effectId = EditorGUILayout.TextField(
                    "Effect ID",
                    _effectId);
                _effectAssetName = EditorGUILayout.TextField(
                    "Effect 에셋 이름",
                    _effectAssetName);
                _effectPolarity =
                    (GameplayEffectPolarity)EditorGUILayout.EnumPopup(
                        "Effect 극성",
                        _effectPolarity);
                _effectDurationType =
                    (GameplayEffectDurationType)EditorGUILayout.EnumPopup(
                        "Effect 수명",
                        _effectDurationType);
                if (_effectDurationType
                    == GameplayEffectDurationType.Duration)
                {
                    _effectDurationSeconds = EditorGUILayout.FloatField(
                        "지속시간",
                        _effectDurationSeconds);
                }
                _effectAttributeId = EditorGUILayout.TextField(
                    "Modifier Attribute ID",
                    _effectAttributeId);
                _effectModifierType =
                    (ModifierType)EditorGUILayout.EnumPopup(
                        "Modifier 방식",
                        _effectModifierType);
                _effectModifierValue = EditorGUILayout.FloatField(
                    "Modifier 값",
                    _effectModifierValue);
            }
            _displayName = EditorGUILayout.TextField("표시 이름", _displayName);
            _abilityId = EditorGUILayout.TextField("Ability ID", _abilityId);
            _assetName = EditorGUILayout.TextField("에셋 이름", _assetName);
            _saveRoot = EditorGUILayout.TextField("저장 루트", _saveRoot);
            _requiredLevel = EditorGUILayout.IntField(
                "요구 레벨",
                _requiredLevel);
            _selectionWeight = EditorGUILayout.FloatField(
                "AI 선택 가중치",
                _selectionWeight);
            _minDistance = EditorGUILayout.FloatField(
                "최소 거리",
                _minDistance);
            _maxDistance = EditorGUILayout.FloatField(
                "최대 거리",
                _maxDistance);
        }

        private void DrawPreviewControls()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("생성 계획 Preview", GUILayout.Height(30f)))
            {
                _preview = AbilityCreationPlanner.Build(BuildRequest());
                _resultMessage = null;
            }

            using (new EditorGUI.DisabledScope(_preview?.CanApply != true))
            {
                if (GUILayout.Button("계획 적용", GUILayout.Height(30f)))
                {
                    AbilityProductionResult result =
                        AbilityAssetFactory.Apply(_preview);
                    _resultMessage = result.Message;
                    _resultType = result.Success
                        ? MessageType.Info
                        : MessageType.Error;
                    if (result.Success)
                        _preview = null;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreview()
        {
            if (_preview == null)
                return;

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            for (int i = 0; i < _preview.Items.Count; i++)
            {
                AbilityPlanItem item = _preview.Items[i];
                EditorGUILayout.LabelField(
                    $"{item.Operation} · {item.AssetKind}",
                    item.TargetPath);
            }

            for (int i = 0; i < _preview.Issues.Count; i++)
            {
                AbilityProductionIssue issue = _preview.Issues[i];
                EditorGUILayout.HelpBox(
                    $"[{issue.Code}] {issue.Message}",
                    ToMessageType(issue.Severity));
            }
        }

        private AbilityCreationRequest BuildRequest() =>
            new()
            {
                Recipe = AbilityRecipeCatalog.All[_recipeIndex],
                DisplayName = _displayName,
                AbilityId = _abilityId,
                AssetName = _assetName,
                SaveRoot = _saveRoot,
                TargetSet = _targetSet,
                MotionOwner = _motionOwner,
                Motion = _motion,
                TaskGraph = _taskGraph,
                CommitEffect = _commitEffect,
                EndEffect = _endEffect,
                CreateCommitEffect = _createCommitEffect,
                EffectId = _effectId,
                EffectAssetName = _effectAssetName,
                EffectPolarity = _effectPolarity,
                EffectDurationType = _effectDurationType,
                EffectDurationSeconds = _effectDurationSeconds,
                EffectAttributeId = _effectAttributeId,
                EffectModifierType = _effectModifierType,
                EffectModifierValue = _effectModifierValue,
                BindingMode = _bindingMode,
                PlayerSkillSlot = _playerSkillSlot,
                PlayerCombatSlot = _playerCombatSlot,
                ReplaceExistingBinding = _replaceExistingBinding,
                RequiredLevel = _requiredLevel,
                SelectionWeight = _selectionWeight,
                MinDistance = _minDistance,
                MaxDistance = _maxDistance,
            };

        private static MessageType ToMessageType(
            AbilityProductionSeverity severity) =>
            severity switch
            {
                AbilityProductionSeverity.Error => MessageType.Error,
                AbilityProductionSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info,
            };
    }
}
