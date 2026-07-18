using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Debugging;

namespace UPlayGround.Animation.Editor
{
    public partial class MotionSetEditorWindow
    {
        VisualElement _controlPanelBody;
        readonly List<Button> _controlPanelButtons = new();

        VisualElement BuildControlPanelsUIToolkit()
        {
            var root = new VisualElement();
            root.AddToClassList("up-control-panels");

            var toolbar = new Toolbar();
            toolbar.AddToClassList("up-control-panel-tabs");
            _controlPanelButtons.Clear();

            int selected = EditorPrefs.GetInt(PREFS_PANEL_TAB, -1);
            for (int i = 0; i < _panelTabTitles.Length; i++)
            {
                int index = i;
                var button = new Button(() =>
                {
                    int current = EditorPrefs.GetInt(PREFS_PANEL_TAB, -1);
                    EditorPrefs.SetInt(PREFS_PANEL_TAB, current == index ? -1 : index);
                    RebuildControlPanelBody();
                }) { text = _panelTabTitles[i] };
                button.AddToClassList("up-control-panel-tab");
                _controlPanelButtons.Add(button);
                toolbar.Add(button);
            }

            var spacer = new VisualElement();
            spacer.AddToClassList("up-flex-spacer");
            toolbar.Add(spacer);

            var help = new Toggle("ⓘ 도움말") { value = ShowPanelHelp };
            help.AddToClassList("up-control-panel-help");
            help.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetBool(PREFS_PANEL_HELP, evt.newValue);
                RebuildControlPanelBody();
            });
            toolbar.Add(help);
            root.Add(toolbar);

            _controlPanelBody = new VisualElement();
            _controlPanelBody.AddToClassList("up-control-panel-body");
            root.Add(_controlPanelBody);
            RebuildControlPanelBody();
            return root;
        }

        void RebuildControlPanelBody()
        {
            if (_controlPanelBody == null)
                return;

            _controlPanelBody.Clear();
            int selected = EditorPrefs.GetInt(PREFS_PANEL_TAB, -1);
            for (int i = 0; i < _controlPanelButtons.Count; i++)
                _controlPanelButtons[i].EnableInClassList("up-tab-selected", i == selected);

            VisualElement panel = selected switch
            {
                0 => BuildRootMotionPanel(),
                1 => BuildWarpPanel(),
                2 => BuildEventDebugPanel(),
                3 => BuildCombatOverlayPanel(),
                4 => BuildCaptureBridgePanel(),
                _ => null,
            };
            _controlPanelBody.EnableInClassList("up-hidden", panel == null);
            if (panel != null)
                _controlPanelBody.Add(panel);
        }

        VisualElement BuildRootMotionPanel()
        {
            var panel = CreatePanel("루트 모션 프리뷰");
            var enabled = new Toggle("루트 모션 적용") { value = _rootMotionEnabled };
            panel.Add(enabled);

            var settings = new VisualElement();
            settings.AddToClassList("up-panel-fields");
            panel.Add(settings);

            var scale = new Slider("스케일", 0f, 3f) { value = _rootMotionUniformScale, showInputField = true };
            scale.RegisterValueChangedCallback(evt =>
            {
                _rootMotionUniformScale = evt.newValue;
                SceneView.RepaintAll();
            });
            settings.Add(scale);

            var presets = CreateButtonRow();
            AddButton(presets, "0×", () => SetRootMotionScale(scale, 0f));
            AddButton(presets, "0.5×", () => SetRootMotionScale(scale, 0.5f));
            AddButton(presets, "1×", () => SetRootMotionScale(scale, 1f));
            AddButton(presets, "1.5×", () => SetRootMotionScale(scale, 1.5f));
            AddButton(presets, "2×", () => SetRootMotionScale(scale, 2f));
            settings.Add(presets);

            var axisAdvanced = new Toggle("축별 스케일") { value = _rootMotionAxisAdvanced };
            var axisScale = new Vector3Field("XYZ 배율") { value = _rootMotionAxisScale };
            axisScale.EnableInClassList("up-hidden", !_rootMotionAxisAdvanced);
            axisAdvanced.RegisterValueChangedCallback(evt =>
            {
                _rootMotionAxisAdvanced = evt.newValue;
                axisScale.EnableInClassList("up-hidden", !evt.newValue);
                SceneView.RepaintAll();
            });
            axisScale.RegisterValueChangedCallback(evt =>
            {
                _rootMotionAxisScale = evt.newValue;
                SceneView.RepaintAll();
            });
            settings.Add(axisAdvanced);
            settings.Add(axisScale);

            var rotation = new Toggle("회전 루트 모션 적용") { value = _rootMotionApplyRotation };
            rotation.RegisterValueChangedCallback(evt => _rootMotionApplyRotation = evt.newValue);
            settings.Add(rotation);
            var trail = new Toggle("궤적 표시") { value = _rootMotionDrawTrail };
            trail.RegisterValueChangedCallback(evt =>
            {
                _rootMotionDrawTrail = evt.newValue;
                SceneView.RepaintAll();
            });
            settings.Add(trail);
            enabled.RegisterValueChangedCallback(evt =>
            {
                _rootMotionEnabled = evt.newValue;
                settings.SetEnabled(evt.newValue);
                SceneView.RepaintAll();
            });
            settings.SetEnabled(_rootMotionEnabled);

            var reset = new Button(ResetRootMotionPreviewPose) { text = "위치 리셋" };
            reset.SetEnabled(_rootMotionPreviewActive);
            panel.Add(reset);
            if (ShowPanelHelp)
                panel.Add(CreateHelp("스케일 1×가 클립 의도보다 길게 이동하면 비플레이어 테스트 액터로 이중 소비 여부를 확인하세요."));
            return panel;
        }

        void SetRootMotionScale(Slider slider, float value)
        {
            _rootMotionUniformScale = value;
            slider.SetValueWithoutNotify(value);
            SceneView.RepaintAll();
        }

        VisualElement BuildWarpPanel()
        {
            var container = new VisualElement();
            container.AddToClassList("up-control-panel-stack");

            var bake = CreatePanel("Warp 루트모션 베이크");
            var bakeButton = new Button(StartWarpBake)
            {
                text = _warpBakeActive ? "베이크 중..." : "Bake Warp Root Motion",
            };
            bakeButton.SetEnabled(!_warpBakeActive && Application.isPlaying && _targetActor != null);
            bake.Add(bakeButton);
            if (!string.IsNullOrEmpty(_warpBakeSummary))
                bake.Add(new TextField("최근 베이크 결과") { value = _warpBakeSummary, multiline = true, isReadOnly = true });
            if (ShowPanelHelp)
                bake.Add(CreateHelp("Play Mode에서 MotionWarp 이벤트 구간의 루트모션 총량을 결정적 고정 스텝으로 베이크합니다."));
            container.Add(bake);

            var target = CreatePanel("워프 타깃");
            var enabled = new Toggle("활성") { value = _warpTargetEnabled };
            enabled.SetEnabled(WarpTargetGuiAllowed);
            target.Add(enabled);

            var fields = new VisualElement();
            fields.AddToClassList("up-panel-fields");
            var distance = new Slider("거리 (m)", 0f, 10f) { value = _warpTargetDistance, showInputField = true };
            var angle = new Slider("각도 (°)", -180f, 180f) { value = _warpTargetAngle, showInputField = true };
            var height = new Slider("높이 (m)", -2f, 3f) { value = _warpTargetHeight, showInputField = true };
            var snapshot = new Toggle("Snapshot 모드") { value = _warpTargetUseSnapshot };
            fields.Add(distance);
            fields.Add(angle);
            fields.Add(height);
            fields.Add(snapshot);
            fields.SetEnabled(_warpTargetEnabled && WarpTargetGuiAllowed);
            target.Add(fields);

            enabled.RegisterValueChangedCallback(evt =>
            {
                _warpTargetEnabled = evt.newValue;
                fields.SetEnabled(evt.newValue && WarpTargetGuiAllowed);
                if (evt.newValue) TryEnsureWarpTargetSpawned();
                else DestroyWarpTarget();
            });
            distance.RegisterValueChangedCallback(evt => { _warpTargetDistance = evt.newValue; UpdateWarpTargetTransform(); });
            angle.RegisterValueChangedCallback(evt => { _warpTargetAngle = evt.newValue; UpdateWarpTargetTransform(); });
            height.RegisterValueChangedCallback(evt => { _warpTargetHeight = evt.newValue; UpdateWarpTargetTransform(); });
            snapshot.RegisterValueChangedCallback(evt => { _warpTargetUseSnapshot = evt.newValue; UpdateWarpTargetTransform(); });

            var presets = CreateButtonRow();
            AddButton(presets, "정면", () => SetWarpPreset(angle, height, 0f, 0f));
            AddButton(presets, "좌45°", () => SetWarpPreset(angle, height, -45f, _warpTargetHeight));
            AddButton(presets, "우45°", () => SetWarpPreset(angle, height, 45f, _warpTargetHeight));
            AddButton(presets, "뒤(180°)", () => SetWarpPreset(angle, height, 180f, _warpTargetHeight));
            fields.Add(presets);

            if (ShowPanelHelp)
                target.Add(CreateHelp("UseExisting resolver는 이 더미를 사용하며 Snapshot은 워프 시작 시점 위치를 고정합니다."));
            container.Add(target);
            return container;
        }

        void SetWarpPreset(Slider angleField, Slider heightField, float angle, float height)
        {
            _warpTargetAngle = angle;
            _warpTargetHeight = height;
            angleField.SetValueWithoutNotify(angle);
            heightField.SetValueWithoutNotify(height);
            UpdateWarpTargetTransform();
        }

        VisualElement BuildEventDebugPanel()
        {
            var panel = CreatePanel("이벤트 디버그");
            var sceneLabels = new Toggle("Scene 라벨") { value = _showSceneEventOverlay };
            sceneLabels.RegisterValueChangedCallback(evt => _showSceneEventOverlay = evt.newValue);
            panel.Add(sceneLabels);
            var autoAttach = new Toggle("Game 오버레이 자동 부착") { value = _autoAttachDebugOverlay };
            autoAttach.RegisterValueChangedCallback(evt => _autoAttachDebugOverlay = evt.newValue);
            panel.Add(autoAttach);

            var buttons = CreateButtonRow();
            var attach = new Button(() => EnsureDebugOverlay(true)) { text = "오버레이 부착" };
            attach.SetEnabled(_targetActor != null);
            buttons.Add(attach);
            AddButton(buttons, "로그 지우기", () =>
            {
                _eventLog.Clear();
                MotionSetEventDebugOverlay.Clear();
                RebuildControlPanelBody();
            });
            panel.Add(buttons);
            panel.Add(new Label(BuildWarpDebugText()));
            panel.Add(new Label(_activeEvents != null && _activeEvents.Count > 0
                ? $"Active: {string.Join(", ", GetEventLabels(_activeEvents))}"
                : "Active: -"));
            int count = Mathf.Min(5, _eventLog.Count);
            for (int i = 0; i < count; i++)
                panel.Add(new Label(_eventLog[i]));
            return panel;
        }

        VisualElement BuildCombatOverlayPanel()
        {
            LoadCombatPrefsOnce();
            var panel = CreatePanel("전투 오버레이");
            var show = new Toggle("표시") { value = _showCombatOverlay };
            show.RegisterValueChangedCallback(evt =>
            {
                _showCombatOverlay = evt.newValue;
                EditorPrefs.SetBool(PREFS_COMBAT_SHOW, evt.newValue);
                SceneView.RepaintAll();
            });
            panel.Add(show);

            var data = new ObjectField("AbilitySet")
            {
                objectType = typeof(AbilitySetSO),
                allowSceneObjects = false,
                value = _combatAttackData,
            };
            data.RegisterValueChangedCallback(evt =>
            {
                _combatAttackData = evt.newValue as AbilitySetSO;
                SaveCombatPairing();
            });
            panel.Add(data);

            var buttons = CreateButtonRow();
            AddButton(buttons, "자동 연결", () =>
            {
                AutoConnectCombatData();
                RebuildControlPanelBody();
            });
            var edit = new Toggle("씬 핸들 편집") { value = _combatEditHitbox };
            edit.RegisterValueChangedCallback(evt =>
            {
                _combatEditHitbox = evt.newValue;
                EditorPrefs.SetBool(PREFS_COMBAT_EDIT, evt.newValue);
                SceneView.RepaintAll();
            });
            buttons.Add(edit);
            panel.Add(buttons);

            if (_selectedActorMotionKey == AnimKey.None)
            {
                var key = new EnumField("AnimKey", _combatManualKey);
                key.RegisterValueChangedCallback(evt =>
                {
                    _combatManualKey = (AnimKey)evt.newValue;
                    RebuildControlPanelBody();
                });
                panel.Add(key);
            }

            ResolveCombatAttacks(GetCombatAnimKey());
            panel.Add(new Label(BuildCombatStatusText()));
            return panel;
        }

        string BuildCombatStatusText()
        {
            AnimKey key = GetCombatAnimKey();
            if (_combatAttackData == null)
                return "공격 데이터 없음 — AbilitySet을 지정하거나 자동 연결하세요.";
            if (key == AnimKey.None)
                return "모션 키 미선택";
            if (_combatResolved.Count == 0)
                return $"'{key}'를 사용하는 공격 Ability가 없습니다.";
            var attack = GetCurrentCombatAttack();
            int phaseCount = attack?.HitPhases?.Count ?? 0;
            return $"{attack?.SourceName ?? "-"} · Hit Phase {phaseCount}개";
        }

        VisualElement BuildCaptureBridgePanel()
        {
            var panel = CreatePanel("카메라 동기 촬영");
            MotionSet set = GetCurrentMotionSet();
            float total = set?.TotalDuration ?? 0f;
            float end = _endTime > 0f ? Mathf.Min(_endTime, total) : total;
            panel.Add(new Label($"MotionSet: {(_asset != null ? _asset.name : "-")}"));
            panel.Add(new Label($"대상: {(_targetActor != null ? _targetActor.name : "-")} · 구간 {_startTime:0.000}s → {end:0.000}s"));
            var open = new Button(() =>
            {
                Transform anchor = _targetActor != null ? _targetActor.transform : null;
                UPlayGround.Data.Editor.DialogueCameraRecorderWindow.OpenForMotion(_asset, anchor);
            }) { text = "현재 모션으로 카메라 녹화 열기" };
            open.SetEnabled(_asset != null);
            panel.Add(open);
            if (ShowPanelHelp)
                panel.Add(CreateHelp("카메라 녹화 창의 동기 촬영이 현재 MotionSet 재생 구간과 타임코드를 공유합니다."));
            return panel;
        }

        static VisualElement CreatePanel(string title)
        {
            var panel = new VisualElement();
            panel.AddToClassList("up-control-panel");
            var label = new Label(title);
            label.AddToClassList("up-control-panel-title");
            panel.Add(label);
            return panel;
        }

        static VisualElement CreateButtonRow()
        {
            var row = new VisualElement();
            row.AddToClassList("up-button-row");
            return row;
        }

        static void AddButton(VisualElement parent, string text, System.Action action)
        {
            parent.Add(new Button(action) { text = text });
        }

        static Label CreateHelp(string text)
        {
            var label = new Label(text);
            label.AddToClassList("up-control-panel-help-text");
            return label;
        }
    }
}
