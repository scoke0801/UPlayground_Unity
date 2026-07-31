using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Ability;

namespace UPlayGround.Data.Editor.Ability.Production
{
    public sealed class AbilityProductionDashboardWindow : EditorWindow
    {
        [SerializeField] private GameplayAbilitySO _ability;
        [SerializeField] private Object _dependencyTarget;
        [SerializeField] private string _cloneAbilityId =
            "Actor.Clone";
        [SerializeField] private string _cloneAssetName = "Clone";
        [SerializeField] private string _cloneRoot =
            "Assets/10.Datas/Ability/Actor";
        [SerializeField] private float _measuredDamage;
        [SerializeField] private float _measuredDuration;
        [SerializeField] private float _measuredHitCount;
        [SerializeField] private float _measuredCooldown;
        [SerializeField] private int _remainingTasks;
        [SerializeField] private int _remainingEffects;
        [SerializeField] private int _remainingTags;

        private Vector2 _scroll;
        private AbilityMotionReport _motion;
        private AbilityDependencyReport _dependencies;
        private AbilityClonePlan _clonePlan;
        private AbilityStaticBalanceSummary _balance;
        private List<AbilityProductionIssue> _productionIssues = new();
        private List<AbilityValidationIssue> _projectIssues = new();
        private AbilityReplayComparison _replay;
        private AbilityBalanceComparison _measuredComparison;
        private List<string> _snapshotFindings = new();
        private string _message;
        private VisualElement _motionResult;
        private VisualElement _cloneResult;
        private VisualElement _dependencyResult;
        private VisualElement _validationResult;
        private VisualElement _balanceResult;
        private VisualElement _replayResult;
        private HelpBox _messageBox;

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/Ability Production Dashboard")]
        public static void Open()
        {
            var window = GetWindow<AbilityProductionDashboardWindow>();
            window.titleContent = new GUIContent("Ability Dashboard");
            window.minSize = new Vector2(720f, 680f);
            window.Show();
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

            var title = new Label("Ability 제작 검증 대시보드");
            title.style.fontSize = 16f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            scroll.Add(title);
            var abilityField = new ObjectField("검사 Ability")
            {
                objectType = typeof(GameplayAbilitySO),
                allowSceneObjects = false,
                value = _ability,
            };
            abilityField.RegisterValueChangedCallback(evt =>
            {
                _ability = evt.newValue as GameplayAbilitySO;
                ClearAnalysisResults();
            });
            scroll.Add(abilityField);

            Foldout motion = CreateFoldout(
                "Motion / HitPhase",
                "MotionEvent와 Payload HitPhase 수를 비교하고 부족한 페이즈만 추가합니다.");
            motion.Add(new Button(() =>
            {
                _motion = AbilityMotionAnalyzer.Analyze(FindPayload(_ability));
                RefreshMotionResult();
            }) { text = "선택 Ability의 Motion 분석" });
            _motionResult = new VisualElement();
            motion.Add(_motionResult);
            scroll.Add(motion);

            Foldout clone = CreateFoldout(
                "안전 Fork",
                "Ability와 Payload만 독립 복제하고 Motion Key 매핑·TaskGraph·Effect는 공유합니다.");
            clone.Add(CreateTextField(
                "새 Ability ID",
                _cloneAbilityId,
                value => _cloneAbilityId = value));
            clone.Add(CreateTextField(
                "새 에셋 이름",
                _cloneAssetName,
                value => _cloneAssetName = value));
            clone.Add(CreateTextField(
                "저장 루트",
                _cloneRoot,
                value => _cloneRoot = value));
            var cloneActions = CreateActionRow();
            cloneActions.Add(new Button(() =>
            {
                _clonePlan = AbilityCloneService.Build(
                    _ability,
                    _cloneAbilityId,
                    _cloneAssetName,
                    _cloneRoot);
                RefreshCloneResult();
            }) { text = "복제 Preview" });
            cloneActions.Add(new Button(() =>
            {
                AbilityProductionResult result =
                    AbilityCloneService.Apply(_clonePlan);
                _message = result.Message;
                if (result.Success)
                {
                    _ability = result.Ability;
                    abilityField.SetValueWithoutNotify(_ability);
                    _clonePlan = null;
                }
                RefreshCloneResult();
                RefreshMessage();
            }) { text = "Ability Fork 적용", name = "apply-clone" });
            clone.Add(cloneActions);
            _cloneResult = new VisualElement();
            clone.Add(_cloneResult);
            scroll.Add(clone);

            Foldout dependencies = CreateFoldout(
                "공유 영향 분석",
                "수정 후보 에셋을 참조하는 Ability·Set 소비자를 찾아 공유 변경 범위를 확인합니다.");
            var dependencyField = new ObjectField("수정 후보 에셋")
            {
                objectType = typeof(Object),
                allowSceneObjects = false,
                value = _dependencyTarget,
            };
            dependencyField.RegisterValueChangedCallback(
                evt => _dependencyTarget = evt.newValue);
            dependencies.Add(dependencyField);
            dependencies.Add(new Button(() =>
            {
                _dependencies =
                    AbilityDependencyAnalyzer.FindReferencers(_dependencyTarget);
                RefreshDependencyResult();
            }) { text = "역참조 Preview" });
            _dependencyResult = new VisualElement();
            dependencies.Add(_dependencyResult);
            scroll.Add(dependencies);

            Foldout validation = CreateFoldout(
                "교차 검증",
                "TaskGraph·Motion·Payload 교차 검증과 프로젝트 전체 Ability 검증을 실행합니다.");
            var validationActions = CreateActionRow();
            validationActions.Add(new Button(() =>
            {
                _productionIssues.Clear();
                if (_ability == null)
                {
                    _productionIssues.Add(new AbilityProductionIssue(
                        "VALIDATION.ABILITY",
                        AbilityProductionSeverity.Error,
                        "Ability를 선택하세요."));
                }
                else
                {
                    _productionIssues.AddRange(
                        AbilityTaskGraphValidator.Validate(_ability.taskGraph));
                    _productionIssues.AddRange(
                        AbilityMotionAnalyzer.Analyze(FindPayload(_ability)).Issues);
                }
                RefreshValidationResult();
            }) { text = "선택 Ability 교차 검증" });
            validationActions.Add(new Button(() =>
            {
                _projectIssues = AbilityDataValidator.ValidateAll();
                RefreshValidationResult();
            }) { text = "프로젝트 전체 검증" });
            validation.Add(validationActions);
            _validationResult = new VisualElement();
            validation.Add(_validationResult);
            scroll.Add(validation);

            Foldout balance = CreateFoldout(
                "밸런스 / Snapshot",
                "정적 예상값과 샌드박스 실측을 비교하고 프로젝트 Snapshot 회귀를 확인합니다.");
            balance.Add(new Button(() =>
            {
                _balance = AbilityBalanceAnalyzer.Summarize(_ability);
                RefreshBalanceResult();
            }) { text = "정적 예상값 계산" });
            balance.Add(CreateFloatField(
                "평균 피해", _measuredDamage, value => _measuredDamage = value));
            balance.Add(CreateFloatField(
                "평균 실행시간", _measuredDuration, value => _measuredDuration = value));
            balance.Add(CreateFloatField(
                "평균 적중 수", _measuredHitCount, value => _measuredHitCount = value));
            balance.Add(CreateFloatField(
                "쿨다운 Ready", _measuredCooldown, value => _measuredCooldown = value));
            balance.Add(CreateIntegerField(
                "종료 후 Task", _remainingTasks, value => _remainingTasks = value));
            balance.Add(CreateIntegerField(
                "종료 후 Effect", _remainingEffects, value => _remainingEffects = value));
            balance.Add(CreateIntegerField(
                "종료 후 Tag", _remainingTags, value => _remainingTags = value));
            balance.Add(new Button(() =>
            {
                _measuredComparison = AbilityBalanceAnalyzer.Compare(
                    _ability,
                    new AbilityMeasuredResult
                    {
                        AbilityId = _ability?.abilityId,
                        AverageDamage = _measuredDamage,
                        AverageDuration = _measuredDuration,
                        AverageHitCount = _measuredHitCount,
                        CooldownReadySeconds = _measuredCooldown,
                        RemainingTaskCount = _remainingTasks,
                        RemainingEffectCount = _remainingEffects,
                        RemainingTagCount = _remainingTags,
                    });
                RefreshBalanceResult();
            }) { text = "정적 예상값과 실측 비교" });
            var snapshotActions = CreateActionRow();
            snapshotActions.Add(new Button(SaveSnapshot) { text = "현재 Snapshot 저장" });
            snapshotActions.Add(new Button(CompareSnapshot) { text = "기준 Snapshot과 비교" });
            balance.Add(snapshotActions);
            _balanceResult = new VisualElement();
            balance.Add(_balanceResult);
            scroll.Add(balance);

            Foldout replay = CreateFoldout(
                "Encounter Replay",
                "Replay JSON의 공격 후보 프레임과 거리·활성화 실패를 선택 Ability와 비교합니다.");
            replay.Add(new Button(LoadReplay) { text = "Replay JSON 열기 및 비교" });
            _replayResult = new VisualElement();
            replay.Add(_replayResult);
            scroll.Add(replay);

            _messageBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            _messageBox.style.marginTop = 10f;
            scroll.Add(_messageBox);
            ClearAnalysisResults();
            RefreshMessage();
        }

        private static Foldout CreateFoldout(string title, string help)
        {
            var foldout = new Foldout { text = title, value = true };
            foldout.style.marginTop = 10f;
            var description = new Label(help);
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.opacity = 0.72f;
            description.style.marginBottom = 5f;
            foldout.Add(description);
            return foldout;
        }

        private static VisualElement CreateActionRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 4f;
            return row;
        }

        private static TextField CreateTextField(
            string label,
            string value,
            System.Action<string> changed)
        {
            var field = new TextField(label) { value = value };
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            return field;
        }

        private static FloatField CreateFloatField(
            string label,
            float value,
            System.Action<float> changed)
        {
            var field = new FloatField(label) { value = value };
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            return field;
        }

        private static IntegerField CreateIntegerField(
            string label,
            int value,
            System.Action<int> changed)
        {
            var field = new IntegerField(label) { value = value };
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            return field;
        }

        private void ClearAnalysisResults()
        {
            _motion = null;
            _clonePlan = null;
            _dependencies = null;
            _balance = null;
            _replay = null;
            _productionIssues.Clear();
            RefreshMotionResult();
            RefreshCloneResult();
            RefreshDependencyResult();
            RefreshValidationResult();
            RefreshBalanceResult();
            RefreshReplayResult();
        }

        private void RefreshMotionResult()
        {
            if (_motionResult == null) return;
            _motionResult.Clear();
            if (_motion == null) return;
            _motionResult.Add(ResultLabel("Motion", ObjectLabel(_motion.Motion)));
            _motionResult.Add(ResultLabel("총 길이", $"{_motion.Duration:0.###}초"));
            _motionResult.Add(ResultLabel(
                "이벤트 / 요구 HitPhase",
                $"{_motion.EventCount} / {_motion.RequiredHitPhaseCount}"));
            _motionResult.Add(ResultLabel(
                "분류",
                $"Projectile={_motion.HasProjectileEvent}, Telegraph={_motion.HasTelegraphEvent}"));
            AddIssues(_motionResult, _motion.Issues);
            UPlayGroundMotionAbilityPayloadSO payload = FindPayload(_ability);
            int phaseCount = payload?.attackInfo?.baseInfo?.hitPhases?.Count ?? 0;
            var apply = new Button(() =>
            {
                AbilityMotionAnalyzer.ExpandHitPhasesToMatch(payload, _motion);
                _message = "기존 HitPhase 값은 유지하고 부족한 항목만 추가했습니다.";
                RefreshMessage();
                RefreshMotionResult();
            }) { text = "부족한 HitPhase만 추가" };
            apply.SetEnabled(
                payload != null && _motion.RequiredHitPhaseCount > phaseCount);
            _motionResult.Add(apply);
        }

        private void RefreshCloneResult()
        {
            if (_cloneResult == null) return;
            _cloneResult.Clear();
            Button apply = rootVisualElement.Q<Button>("apply-clone");
            apply?.SetEnabled(_clonePlan?.CanApply == true);
            if (_clonePlan == null) return;
            _cloneResult.Add(ResultLabel("Ability", _clonePlan.AbilityPath));
            _cloneResult.Add(ResultLabel("Payload", _clonePlan.PayloadPath));
            AddIssues(_cloneResult, _clonePlan.Issues);
        }

        private void RefreshDependencyResult()
        {
            if (_dependencyResult == null) return;
            _dependencyResult.Clear();
            if (_dependencies == null) return;
            _dependencyResult.Add(ResultLabel(
                "소비자 수",
                _dependencies.Referencers.Count.ToString()));
            for (int i = 0; i < _dependencies.Referencers.Count; i++)
            {
                Object item = _dependencies.Referencers[i];
                var captured = item;
                _dependencyResult.Add(new Button(() =>
                {
                    Selection.activeObject = captured;
                    EditorGUIUtility.PingObject(captured);
                })
                {
                    text = $"{item.name} · {AssetDatabase.GetAssetPath(item)}",
                });
            }
        }

        private void RefreshValidationResult()
        {
            if (_validationResult == null) return;
            _validationResult.Clear();
            AddIssues(_validationResult, _productionIssues);
            for (int i = 0; i < _projectIssues.Count; i++)
            {
                AbilityValidationIssue issue = _projectIssues[i];
                var captured = issue;
                var button = new Button(() =>
                {
                    if (captured.Context == null) return;
                    Selection.activeObject = captured.Context;
                    EditorGUIUtility.PingObject(captured.Context);
                }) { text = $"[{issue.Severity}] {issue.Message}" };
                button.style.whiteSpace = WhiteSpace.Normal;
                _validationResult.Add(button);
            }
        }

        private void RefreshBalanceResult()
        {
            if (_balanceResult == null) return;
            _balanceResult.Clear();
            if (_balance != null)
            {
                _balanceResult.Add(ResultLabel("Ability ID", _balance.AbilityId));
                _balanceResult.Add(ResultLabel(
                    "HitPhase / 피해 / Poise / Break",
                    $"{_balance.HitPhaseCount} / {_balance.TotalDamage:0.###} / "
                    + $"{_balance.TotalPoiseDamage:0.###} / {_balance.TotalBreakDamage:0.###}"));
                _balanceResult.Add(ResultLabel(
                    "Motion / 기대 지속 / 쿨다운 / 주기",
                    $"{_balance.MotionDuration:0.###} / {_balance.ExpectedDuration:0.###} / "
                    + $"{_balance.Cooldown:0.###} / {_balance.CycleDuration:0.###}"));
                _balanceResult.Add(ResultLabel(
                    "이론 DPS",
                    _balance.DamagePerSecond.ToString("0.###")));
            }
            if (_measuredComparison != null)
                for (int i = 0; i < _measuredComparison.Findings.Count; i++)
                    _balanceResult.Add(new HelpBox(
                        _measuredComparison.Findings[i],
                        HelpBoxMessageType.Info));
            for (int i = 0; i < _snapshotFindings.Count; i++)
                _balanceResult.Add(new HelpBox(
                    _snapshotFindings[i],
                    HelpBoxMessageType.Warning));
        }

        private void RefreshReplayResult()
        {
            if (_replayResult == null) return;
            _replayResult.Clear();
            if (_replay == null) return;
            _replayResult.Add(ResultLabel(
                "프레임 / 공격 후보 / 후보 비율",
                $"{_replay.FrameCount} / {_replay.AttackCandidateFrames} / "
                + $"{_replay.AttackCandidateRatio:P1}"));
            _replayResult.Add(ResultLabel(
                "평균 거리 / 실패",
                $"{_replay.AverageDistance:0.###} / {_replay.ActivationFailureCount}"));
            for (int i = 0; i < _replay.Findings.Count; i++)
                _replayResult.Add(new HelpBox(
                    _replay.Findings[i],
                    HelpBoxMessageType.Warning));
            _replayResult.Add(new Button(SaveReplayCsv)
            {
                text = "비교 결과 CSV 저장",
            });
        }

        private static Label ResultLabel(string key, string value)
        {
            var label = new Label($"{key}: {value}");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 3f;
            return label;
        }

        private static void AddIssues(
            VisualElement parent,
            IReadOnlyList<AbilityProductionIssue> issues)
        {
            if (issues == null) return;
            for (int i = 0; i < issues.Count; i++)
            {
                AbilityProductionIssue issue = issues[i];
                var captured = issue;
                var button = new Button(() =>
                {
                    if (captured.Context == null) return;
                    Selection.activeObject = captured.Context;
                    EditorGUIUtility.PingObject(captured.Context);
                }) { text = $"[{issue.Code}] {issue.Message}" };
                button.style.whiteSpace = WhiteSpace.Normal;
                parent.Add(button);
            }
        }

        private void SaveSnapshot()
        {
            string path = EditorUtility.SaveFilePanel(
                "Ability Balance Snapshot",
                System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(
                        Application.dataPath,
                        "..",
                        "BalanceSnapshots")),
                "ability-production.json",
                "json");
            if (string.IsNullOrWhiteSpace(path)) return;
            AbilityProductionSnapshotService.Save(
                path,
                AbilityProductionSnapshotService.Capture());
            _message = $"Snapshot 저장 완료: {path}";
            RefreshMessage();
        }

        private void CompareSnapshot()
        {
            string path = EditorUtility.OpenFilePanel(
                "Ability Balance Snapshot",
                System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(
                        Application.dataPath,
                        "..",
                        "BalanceSnapshots")),
                "json");
            if (string.IsNullOrWhiteSpace(path)) return;
            _snapshotFindings = AbilityProductionSnapshotService.Compare(
                AbilityProductionSnapshotService.Load(path),
                AbilityProductionSnapshotService.Capture());
            RefreshBalanceResult();
        }

        private void LoadReplay()
        {
            string path = EditorUtility.OpenFilePanel(
                "Encounter Replay JSON",
                Application.persistentDataPath,
                "json");
            if (string.IsNullOrWhiteSpace(path)) return;
            AbilityReplayData replay = BalanceReplayComparator.LoadJson(path);
            _replay = BalanceReplayComparator.Compare(
                _ability,
                FindPayload(_ability),
                replay);
            RefreshReplayResult();
        }

        private void SaveReplayCsv()
        {
            string path = EditorUtility.SaveFilePanel(
                "Replay 비교 CSV",
                string.Empty,
                "AbilityReplayComparison.csv",
                "csv");
            if (string.IsNullOrWhiteSpace(path)) return;
            File.WriteAllText(path, BalanceReplayComparator.ToCsv(_replay));
            _message = $"CSV 저장 완료: {path}";
            RefreshMessage();
        }

        private void RefreshMessage()
        {
            if (_messageBox == null) return;
            _messageBox.text = _message ?? string.Empty;
            _messageBox.style.display = string.IsNullOrWhiteSpace(_message)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        private void DrawLegacyGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField(
                "Ability 제작 검증 대시보드 — Phase 3~5",
                EditorStyles.boldLabel);
            _ability = (GameplayAbilitySO)EditorGUILayout.ObjectField(
                "Ability",
                _ability,
                typeof(GameplayAbilitySO),
                false);
            EditorGUILayout.Space(8f);
            DrawMotion();
            EditorGUILayout.Space(8f);
            DrawClone();
            EditorGUILayout.Space(8f);
            DrawDependencies();
            EditorGUILayout.Space(8f);
            DrawValidation();
            EditorGUILayout.Space(8f);
            DrawBalance();
            EditorGUILayout.Space(8f);
            DrawReplay();
            if (!string.IsNullOrWhiteSpace(_message))
                EditorGUILayout.HelpBox(_message, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void DrawMotion()
        {
            EditorGUILayout.LabelField(
                "Motion Compare / Apply Selected",
                EditorStyles.boldLabel);
            if (GUILayout.Button("선택 Ability의 Motion 분석"))
            {
                _motion = AbilityMotionAnalyzer.Analyze(FindPayload(_ability));
            }
            if (_motion == null)
                return;
            EditorGUILayout.LabelField("Motion", ObjectLabel(_motion.Motion));
            EditorGUILayout.LabelField("총 길이", $"{_motion.Duration:0.###}초");
            EditorGUILayout.LabelField(
                "이벤트 / 요구 HitPhase",
                $"{_motion.EventCount} / {_motion.RequiredHitPhaseCount}");
            EditorGUILayout.LabelField(
                "분류",
                $"Projectile={_motion.HasProjectileEvent}, "
                + $"Telegraph={_motion.HasTelegraphEvent}");
            EditorGUILayout.LabelField(
                "이벤트 타입",
                string.Join(", ", _motion.EventTypes));
            DrawIssues(_motion.Issues);
            UPlayGroundMotionAbilityPayloadSO payload = FindPayload(_ability);
            int phaseCount =
                payload?.attackInfo?.baseInfo?.hitPhases?.Count ?? 0;
            using (new EditorGUI.DisabledScope(
                       payload == null
                       || _motion.RequiredHitPhaseCount <= phaseCount))
            {
                if (GUILayout.Button(
                        "Apply Selected: 부족한 HitPhase만 추가"))
                {
                    AbilityMotionAnalyzer.ExpandHitPhasesToMatch(
                        payload,
                        _motion);
                    _message =
                        "기존 HitPhase 값은 유지하고 부족한 항목만 추가했습니다.";
                }
            }
        }

        private void DrawClone()
        {
            EditorGUILayout.LabelField("안전 복제", EditorStyles.boldLabel);
            _cloneAbilityId = EditorGUILayout.TextField(
                "새 Ability ID",
                _cloneAbilityId);
            _cloneAssetName = EditorGUILayout.TextField(
                "새 에셋 이름",
                _cloneAssetName);
            _cloneRoot = EditorGUILayout.TextField("저장 루트", _cloneRoot);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("복제 Preview"))
            {
                _clonePlan = AbilityCloneService.Build(
                    _ability,
                    _cloneAbilityId,
                    _cloneAssetName,
                    _cloneRoot);
            }
            using (new EditorGUI.DisabledScope(
                       _clonePlan?.CanApply != true))
            {
                if (GUILayout.Button("Ability Fork 적용"))
                {
                    AbilityProductionResult result =
                        AbilityCloneService.Apply(_clonePlan);
                    _message = result.Message;
                    if (result.Success)
                    {
                        _ability = result.Ability;
                        _clonePlan = null;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            if (_clonePlan == null)
                return;
            EditorGUILayout.LabelField(
                "Ability",
                _clonePlan.AbilityPath);
            EditorGUILayout.LabelField(
                "Payload",
                _clonePlan.PayloadPath);
            EditorGUILayout.HelpBox(
                "TaskGraph와 Effect는 공유하고 Ability와 Payload는 독립 복제합니다. "
                + "액터 Motion Key 매핑은 새 키로 함께 복제하며 원본은 변경하지 않습니다.",
                MessageType.Info);
            DrawIssues(_clonePlan.Issues);
        }

        private void DrawDependencies()
        {
            EditorGUILayout.LabelField("공유 영향 분석", EditorStyles.boldLabel);
            _dependencyTarget = EditorGUILayout.ObjectField(
                "수정 후보 에셋",
                _dependencyTarget,
                typeof(Object),
                false);
            if (GUILayout.Button("역참조 Preview"))
                _dependencies =
                    AbilityDependencyAnalyzer.FindReferencers(
                        _dependencyTarget);
            if (_dependencies == null)
                return;
            EditorGUILayout.LabelField(
                "소비자 수",
                _dependencies.Referencers.Count.ToString());
            for (int i = 0; i < _dependencies.Referencers.Count; i++)
            {
                Object item = _dependencies.Referencers[i];
                if (GUILayout.Button(
                        $"{item.name}  ·  {AssetDatabase.GetAssetPath(item)}",
                        EditorStyles.linkLabel))
                {
                    Selection.activeObject = item;
                    EditorGUIUtility.PingObject(item);
                }
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField(
                "검증과 Issue 이동",
                EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("선택 Ability 교차 검증"))
            {
                _productionIssues.Clear();
                if (_ability == null)
                {
                    _productionIssues.Add(new AbilityProductionIssue(
                        "VALIDATION.ABILITY",
                        AbilityProductionSeverity.Error,
                        "Ability를 선택하세요."));
                }
                else
                {
                    _productionIssues.AddRange(
                        AbilityTaskGraphValidator.Validate(
                            _ability.taskGraph));
                    _productionIssues.AddRange(
                        AbilityMotionAnalyzer.Analyze(
                            FindPayload(_ability)).Issues);
                }
            }
            if (GUILayout.Button("프로젝트 전체 검증"))
                _projectIssues = AbilityDataValidator.ValidateAll();
            EditorGUILayout.EndHorizontal();
            DrawIssues(_productionIssues);
            for (int i = 0; i < _projectIssues.Count; i++)
            {
                AbilityValidationIssue issue = _projectIssues[i];
                if (GUILayout.Button(
                        $"[{issue.Severity}] {issue.Message}",
                        EditorStyles.wordWrappedLabel)
                    && issue.Context != null)
                {
                    Selection.activeObject = issue.Context;
                    EditorGUIUtility.PingObject(issue.Context);
                }
            }
        }

        private void DrawBalance()
        {
            EditorGUILayout.LabelField("정적 밸런스 요약", EditorStyles.boldLabel);
            if (GUILayout.Button("정적 예상값 계산"))
                _balance = AbilityBalanceAnalyzer.Summarize(_ability);
            if (_balance == null)
                return;
            EditorGUILayout.LabelField("Ability ID", _balance.AbilityId);
            EditorGUILayout.LabelField(
                "HitPhase / 총 피해 / Poise / Break",
                $"{_balance.HitPhaseCount} / {_balance.TotalDamage:0.###} / "
                + $"{_balance.TotalPoiseDamage:0.###} / "
                + $"{_balance.TotalBreakDamage:0.###}");
            EditorGUILayout.LabelField(
                "Motion / 예상 지속 / 쿨다운 / 주기",
                $"{_balance.MotionDuration:0.###} / "
                + $"{_balance.ExpectedDuration:0.###} / "
                + $"{_balance.Cooldown:0.###} / "
                + $"{_balance.CycleDuration:0.###}");
            EditorGUILayout.LabelField(
                "이론 DPS",
                _balance.DamagePerSecond.ToString("0.###"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "샌드박스/로그 실측 입력",
                EditorStyles.miniBoldLabel);
            _measuredDamage = EditorGUILayout.FloatField(
                "평균 피해",
                _measuredDamage);
            _measuredDuration = EditorGUILayout.FloatField(
                "평균 실행시간",
                _measuredDuration);
            _measuredHitCount = EditorGUILayout.FloatField(
                "평균 적중 수",
                _measuredHitCount);
            _measuredCooldown = EditorGUILayout.FloatField(
                "쿨다운 Ready",
                _measuredCooldown);
            _remainingTasks = EditorGUILayout.IntField(
                "종료 후 Task",
                _remainingTasks);
            _remainingEffects = EditorGUILayout.IntField(
                "종료 후 Effect",
                _remainingEffects);
            _remainingTags = EditorGUILayout.IntField(
                "종료 후 Tag",
                _remainingTags);
            if (GUILayout.Button("정적 예상값과 실측 비교"))
            {
                _measuredComparison = AbilityBalanceAnalyzer.Compare(
                    _ability,
                    new AbilityMeasuredResult
                    {
                        AbilityId = _ability?.abilityId,
                        AverageDamage = _measuredDamage,
                        AverageDuration = _measuredDuration,
                        AverageHitCount = _measuredHitCount,
                        CooldownReadySeconds = _measuredCooldown,
                        RemainingTaskCount = _remainingTasks,
                        RemainingEffectCount = _remainingEffects,
                        RemainingTagCount = _remainingTags,
                    });
            }
            if (_measuredComparison != null)
                for (int i = 0;
                     i < _measuredComparison.Findings.Count;
                     i++)
                    EditorGUILayout.HelpBox(
                        _measuredComparison.Findings[i],
                        MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("현재 전체 Snapshot 저장"))
            {
                string path = EditorUtility.SaveFilePanel(
                    "Ability Balance Snapshot",
                    System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(
                            Application.dataPath,
                            "..",
                            "BalanceSnapshots")),
                    "ability-production.json",
                    "json");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    AbilityProductionSnapshotService.Save(
                        path,
                        AbilityProductionSnapshotService.Capture());
                    _message = $"Snapshot 저장 완료: {path}";
                }
            }
            if (GUILayout.Button("기준 Snapshot과 현재 비교"))
            {
                string path = EditorUtility.OpenFilePanel(
                    "Ability Balance Snapshot",
                    System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(
                            Application.dataPath,
                            "..",
                            "BalanceSnapshots")),
                    "json");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    _snapshotFindings =
                        AbilityProductionSnapshotService.Compare(
                            AbilityProductionSnapshotService.Load(path),
                            AbilityProductionSnapshotService.Capture());
                }
            }
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < _snapshotFindings.Count; i++)
                EditorGUILayout.HelpBox(
                    _snapshotFindings[i],
                    MessageType.Warning);
        }

        private void DrawReplay()
        {
            EditorGUILayout.LabelField("Encounter Replay 비교", EditorStyles.boldLabel);
            if (GUILayout.Button("Replay JSON 열기 및 비교"))
            {
                string path = EditorUtility.OpenFilePanel(
                    "Encounter Replay JSON",
                    Application.persistentDataPath,
                    "json");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    AbilityReplayData replay =
                        BalanceReplayComparator.LoadJson(path);
                    _replay = BalanceReplayComparator.Compare(
                        _ability,
                        FindPayload(_ability),
                        replay);
                }
            }
            if (_replay == null)
                return;
            EditorGUILayout.LabelField(
                "프레임 / 공격 후보 / 후보 비율",
                $"{_replay.FrameCount} / {_replay.AttackCandidateFrames} / "
                + $"{_replay.AttackCandidateRatio:P1}");
            EditorGUILayout.LabelField(
                "평균 거리 / 실패",
                $"{_replay.AverageDistance:0.###} / "
                + $"{_replay.ActivationFailureCount}");
            for (int i = 0; i < _replay.Findings.Count; i++)
                EditorGUILayout.HelpBox(
                    _replay.Findings[i],
                    MessageType.Warning);
            if (GUILayout.Button("비교 결과 CSV 저장"))
            {
                string path = EditorUtility.SaveFilePanel(
                    "Replay 비교 CSV",
                    string.Empty,
                    "AbilityReplayComparison.csv",
                    "csv");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    File.WriteAllText(
                        path,
                        BalanceReplayComparator.ToCsv(_replay));
                    _message = $"CSV 저장 완료: {path}";
                }
            }
        }

        private static void DrawIssues(
            IReadOnlyList<AbilityProductionIssue> issues)
        {
            if (issues == null)
                return;
            for (int i = 0; i < issues.Count; i++)
            {
                AbilityProductionIssue issue = issues[i];
                if (GUILayout.Button(
                        $"[{issue.Code}] {issue.Message}",
                        EditorStyles.wordWrappedLabel)
                    && issue.Context != null)
                {
                    Selection.activeObject = issue.Context;
                    EditorGUIUtility.PingObject(issue.Context);
                }
            }
        }

        private static UPlayGroundMotionAbilityPayloadSO FindPayload(
            GameplayAbilitySO ability)
        {
            if (ability?.variants == null)
                return null;
            for (int i = 0; i < ability.variants.Count; i++)
                if (ability.variants[i]?.executionPayload
                    is UPlayGroundMotionAbilityPayloadSO payload)
                    return payload;
            return null;
        }

        private static string ObjectLabel(Object value) =>
            value != null ? value.name : "(없음)";
    }
}
