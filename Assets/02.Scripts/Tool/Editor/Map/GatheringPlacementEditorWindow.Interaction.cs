#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UPlayGround.Data.EnumType;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.World;
using UPlayGround.Group;
using UPlayGround.Data.Item;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>상호작용물 및 드랍 아이템 배치 모드 UI.</summary>
    public partial class GatheringPlacementEditorWindow
    {
        private void DrawPlacementKindTabs()
        {
            EditorGUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            _placementKind = (PlacementKind)GUILayout.Toolbar((int)_placementKind, new[] { "Gathering", "Drop Item" });
            if (EditorGUI.EndChangeCheck())
            {
                _placementMode = false;
                SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
                SceneView.RepaintAll();
            }
        }

        private void DrawInteractionListPanel()
        {
            DrawPlacementKindTabs();

            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName(SearchControlName);
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                _searchFilter = "";
            EditorGUILayout.EndHorizontal();

            if (_placementKind == PlacementKind.Gathering)
                DrawRecentDataChips();

            _dataListScroll = EditorGUILayout.BeginScrollView(_dataListScroll, GUILayout.ExpandHeight(true));

            bool anyShown = false;
            if (_placementKind == PlacementKind.Gathering)
            {
                foreach (var data in _interactableDatas)
                {
                    if (!ShouldShowData(data))
                        continue;

                    anyShown = true;
                    DrawDataRow(data);
                }
            }
            else
            {
                foreach (var item in _itemDatas)
                {
                    if (!ShouldShowItem(item))
                        continue;

                    anyShown = true;
                    DrawItemRow(item);
                }
            }

            if (!anyShown)
            {
                string emptyMessage = string.IsNullOrWhiteSpace(_searchFilter)
                    ? GetEmptyDataMessage()
                    : $"'{_searchFilter}'와(과) 일치하는 데이터가 없습니다.";
                GUILayout.Label(emptyMessage, EditorStyles.centeredGreyMiniLabel, GUILayout.Height(32f));
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRecentDataChips()
        {
            PruneRecentDataGuids();
            if (_recentDataGuids.Count == 0)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("최근 사용", EditorStyles.miniBoldLabel, GUILayout.Width(56f));

            for (int i = 0; i < _recentDataGuids.Count; i++)
            {
                var data = LoadDataByGuid(_recentDataGuids[i]);
                if (data == null)
                    continue;

                bool selected = data == _selectedData;
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = selected ? new Color(0.45f, 0.62f, 0.9f) : previousColor;
                if (GUILayout.Button($"{i + 1}. {GetDataTitle(data)}", _chipStyle, GUILayout.MaxWidth(120f)))
                    SelectData(data, false);
                GUI.backgroundColor = previousColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDataRow(InteractableActorSO data)
        {
            bool isSelected = _selectedData == data;
            Rect rect = GUILayoutUtility.GetRect(0f, 32f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                (isSelected ? _selectedItemStyle : _normalItemStyle).Draw(rect, GUIContent.none, false, false, isSelected, false);

            if (isSelected && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + 3f, 3f, rect.height - 6f), new Color(0.55f, 0.72f, 1f));

            // 좌측 300px 패널에 맞춘 2줄 스택 레이아웃.
            string title = GetDataTitle(data);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, 16f), title, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 17f, rect.width - 16f, 14f),
                $"{data.interactionObjectType}  |  HP {data.hp}  |  {data.name}", EditorStyles.miniLabel);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectData(data, false);
                Event.current.Use();
            }
        }

        private void DrawItemRow(ItemSO item)
        {
            bool isSelected = _selectedItem == item;
            Rect rect = GUILayoutUtility.GetRect(0f, 34f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                (isSelected ? _selectedItemStyle : _normalItemStyle).Draw(rect, GUIContent.none, false, false, isSelected, false);

            if (isSelected && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + 3f, 3f, rect.height - 6f), new Color(0.55f, 0.72f, 1f));

            // 좌측 300px 패널에 맞춘 2줄 스택 레이아웃. 아이콘은 우측에 유지.
            float textWidth = rect.width - 46f;
            string title = GetItemTitle(item);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 3f, textWidth, 16f), title, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 18f, textWidth, 14f),
                $"ID {item.itemId}  |  {item.itemType}", EditorStyles.miniLabel);

            if (item.icon != null)
            {
                Rect iconRect = new Rect(rect.xMax - 32f, rect.y + 5f, 24f, 24f);
                GUI.DrawTexture(iconRect, item.icon.texture, ScaleMode.ScaleToFit, true);
            }

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectItem(item, false);
                Event.current.Use();
            }
        }

    }
}
#endif
