#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;

namespace UPlayGround.Gameplay.Ability.Editor
{
    public sealed class GasRuntimeDebuggerWindow : EditorWindow
    {
        private readonly List<IAbilitySystemDebugSource> _sources = new();
        private readonly string[] _tabs =
            { "Overview", "Tasks", "Effects", "Tags", "Attributes", "Events", "Trace" };
        private int _selectedSource;
        private int _selectedTab;
        private Vector2 _scroll;
        private double _nextRefresh;
        private AbilitySystemDebugSnapshot _snapshot;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/Ability/GAS Runtime Debugger")]
        private static void Open() => GetWindow<GasRuntimeDebuggerWindow>("GAS Debugger");

        private void OnEnable() => EditorApplication.update += OnEditorUpdate;
        private void OnDisable() => EditorApplication.update -= OnEditorUpdate;

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextRefresh) return;
            _nextRefresh = EditorApplication.timeSinceStartup + 0.25d;
            RefreshSnapshot();
            Repaint();
        }

        private void RefreshSnapshot()
        {
            AbilitySystemDebugRegistry.CopyAlive(_sources);
            _selectedSource = Mathf.Clamp(_selectedSource, 0, Mathf.Max(0, _sources.Count - 1));
            _snapshot = _sources.Count == 0
                ? null
                : _sources[_selectedSource].CaptureDebugSnapshot(AbilityDebugCaptureOptions.All);
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode에서 활성 Actor의 AbilitySystem을 표시합니다.", MessageType.Info);
                return;
            }
            if (_sources.Count == 0 || _snapshot == null)
            {
                EditorGUILayout.HelpBox("등록된 AbilitySystemComponent가 없습니다.", MessageType.Warning);
                return;
            }

            string[] owners = new string[_sources.Count];
            for (int i = 0; i < _sources.Count; i++)
            {
                AbilitySystemDebugSnapshot item = _sources[i].CaptureDebugSnapshot(AbilityDebugCaptureOptions.None);
                owners[i] = $"{item.OwnerId} [{item.AbilitySystemHandle.Value}]";
            }
            int selected = EditorGUILayout.Popup("Actor", _selectedSource, owners);
            if (selected != _selectedSource)
            {
                _selectedSource = selected;
                RefreshSnapshot();
            }
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            EditorGUILayout.LabelField(
                $"Frame {_snapshot.Frame} / Time {_snapshot.Time:F2}", EditorStyles.miniLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_selectedTab)
            {
                case 0: DrawOverview(); break;
                case 1: DrawTasks(); break;
                case 2: DrawEffects(); break;
                case 3: DrawTags(); break;
                case 4: DrawAttributes(); break;
                case 5: DrawEvents(false); break;
                case 6: DrawEvents(true); break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawOverview()
        {
            EditorGUILayout.LabelField("ASC", _snapshot.AbilitySystemHandle.Value.ToString());
            if (_sources[_selectedSource] is AbilitySystemComponent component)
            EditorGUILayout.LabelField("Authority", "GasOnly");
            EditorGUILayout.LabelField("Attributes", _snapshot.Attributes.Count.ToString());
            EditorGUILayout.LabelField("Active Effects", _snapshot.Effects.Count.ToString());
            EditorGUILayout.LabelField("Active Tasks", _snapshot.Tasks.Count.ToString());
            EditorGUILayout.LabelField("Owned Tags", _snapshot.Tags.Count.ToString());
            EditorGUILayout.LabelField("Recorded Events", _snapshot.Events.Count.ToString());
            DrawVital(AttributeIds.Vital.Health, AttributeIds.Vital.MaxHealth);
            DrawVital(AttributeIds.Vital.Poise, AttributeIds.Vital.MaxPoise);
            DrawVital(AttributeIds.Resource.UltimateEnergy, AttributeIds.Resource.MaxUltimateEnergy);
        }

        private void DrawTasks()
        {
            for (int i = 0; i < _snapshot.Tasks.Count; i++)
            {
                AbilityTaskDebugState task = _snapshot.Tasks[i];
                EditorGUILayout.LabelField(
                    $"{task.TaskType} [{task.State}]",
                    $"Task {task.TaskHandle} / Parent {task.ParentAbilityHandle} {task.EndReason}");
            }
        }

        private void DrawVital(AttributeId currentId, AttributeId maximumId)
        {
            if (!_snapshot.Attributes.TryGetValue(currentId, out GameplayAttributeValue current)) return;
            float maximum = _snapshot.Attributes.TryGetValue(maximumId, out GameplayAttributeValue max)
                ? max.CurrentValue
                : 0f;
            EditorGUILayout.LabelField(currentId.Value, $"{current.CurrentValue:F2} / {maximum:F2}");
        }

        private void DrawEffects()
        {
            for (int i = 0; i < _snapshot.Effects.Count; i++)
            {
                ActiveGameplayEffectDebugState effect = _snapshot.Effects[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(effect.EffectId, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"Active {effect.ActiveHandle} / Spec {effect.SpecHandle} / Stack {effect.StackCount}");
                EditorGUILayout.LabelField(
                    $"Remaining {effect.RemainingSeconds:F2} / Duration {effect.DurationSeconds:F2} / Period {effect.PeriodSeconds:F2}");
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawTags()
        {
            for (int i = 0; i < _snapshot.Tags.Count; i++)
                EditorGUILayout.LabelField(_snapshot.Tags[i].Value);
        }

        private void DrawAttributes()
        {
            foreach (KeyValuePair<AttributeId, GameplayAttributeValue> pair in _snapshot.Attributes)
            {
                string value = $"Base {pair.Value.BaseValue:F3} / Current {pair.Value.CurrentValue:F3}";
                EditorGUILayout.LabelField(pair.Key.Value, value);
            }
        }

        private void DrawEvents(bool traceOnly)
        {
            for (int i = _snapshot.Events.Count - 1; i >= 0; i--)
            {
                AbilityDebugEvent item = _snapshot.Events[i];
                if (traceOnly != (item.Category == AbilityDebugCategory.Trace)) continue;
                EditorGUILayout.LabelField(
                    $"#{item.Sequence} [{item.Category}] {item.EventType}",
                    $"{item.Source} {item.Message}");
            }
        }
    }
}
#endif
