#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Editor.Ability
{
    /// <summary>
    /// ActorStatSO를 변경하지 않고 Attribute Profile 변환 결과와 Shadow 일치 여부를 검토한다.
    /// 실제 에셋 생성은 전체 Preview가 오류 0일 때 별도 Apply 단계에서 수행한다.
    /// </summary>
    public sealed class ActorStatAttributeProfilePreviewWindow : EditorWindow
    {
        private ActorStatSO _source;
        private Vector2 _scroll;
        private readonly List<Row> _rows = new();
        private string _error;

        private readonly struct Row
        {
            public readonly StatType StatType;
            public readonly AttributeId AttributeId;
            public readonly float LegacyValue;
            public readonly float AttributeValue;
            public readonly bool Explicit;

            public Row(
                StatType statType,
                AttributeId attributeId,
                float legacyValue,
                float attributeValue,
                bool explicitValue)
            {
                StatType = statType;
                AttributeId = attributeId;
                LegacyValue = legacyValue;
                AttributeValue = attributeValue;
                Explicit = explicitValue;
            }
        }

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/Ability/Actor Stat → Attribute Preview")]
        private static void Open() =>
            GetWindow<ActorStatAttributeProfilePreviewWindow>("Attribute Preview");

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _source = (ActorStatSO)EditorGUILayout.ObjectField(
                "Actor Stat", _source, typeof(ActorStatSO), false);
            if (EditorGUI.EndChangeCheck()) Rebuild();

            using (new EditorGUI.DisabledScope(_source == null))
            {
                if (GUILayout.Button("Preview 다시 계산")) Rebuild();
            }

            if (_source == null)
            {
                EditorGUILayout.HelpBox(
                    "ActorStatSO를 선택하면 기존 값과 변환될 Attribute 값을 읽기 전용으로 비교합니다.",
                    MessageType.Info);
                return;
            }

            if (!string.IsNullOrEmpty(_error))
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            else
                EditorGUILayout.HelpBox(
                    $"매핑 {_rows.Count}개 · Shadow 불일치 0 · 에셋 변경 없음",
                    MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Legacy Stat", GUILayout.Width(145f));
            GUILayout.Label("Attribute ID", GUILayout.Width(220f));
            GUILayout.Label("Legacy", GUILayout.Width(70f));
            GUILayout.Label("Shadow", GUILayout.Width(70f));
            GUILayout.Label("Source", GUILayout.Width(65f));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(row.StatType.ToString(), GUILayout.Width(145f));
                GUILayout.Label(row.AttributeId.Value, GUILayout.Width(220f));
                GUILayout.Label(row.LegacyValue.ToString("0.###"), GUILayout.Width(70f));
                GUILayout.Label(row.AttributeValue.ToString("0.###"), GUILayout.Width(70f));
                GUILayout.Label(row.Explicit ? "Explicit" : "Default", GUILayout.Width(65f));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void Rebuild()
        {
            _rows.Clear();
            _error = string.Empty;
            if (_source == null) return;

            var runtime = new AttributeSetRuntime();
            var ids = new HashSet<AttributeId>();
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                if (!UPlayGroundAttributeMapping.TryGetAttributeId(statType, out AttributeId id)
                    || !id.IsValid)
                {
                    _error += $"{statType}: AttributeId 매핑 누락\n";
                    continue;
                }
                if (!ids.Add(id))
                {
                    _error += $"{statType}: 중복 AttributeId '{id.Value}'\n";
                    continue;
                }

                bool explicitValue = _source.TryGetExplicit(statType, out float value);
                runtime.Register(new GameplayAttributeDefinition(id, value), value);
                float shadow = runtime.GetCurrent(id);
                if (Mathf.Abs(value - shadow) > 0.0001f)
                    _error += $"{statType}: Legacy {value} / Shadow {shadow}\n";
                _rows.Add(new Row(statType, id, value, shadow, explicitValue));
            }

            if (!string.IsNullOrEmpty(_error)) _error = _error.TrimEnd();
            Repaint();
        }
    }
}
#endif
