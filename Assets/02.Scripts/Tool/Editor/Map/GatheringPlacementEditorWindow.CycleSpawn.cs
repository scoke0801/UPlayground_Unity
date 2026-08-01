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
using UPlayGround.Cycle;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>사이클 스폰 마커 모드 UI와 마커 데이터 적용.</summary>
    public partial class GatheringPlacementEditorWindow
    {
        private void DrawCycleSpawnListPanel()
        {
            EditorGUILayout.HelpBox(
                "프리셋을 선택한 뒤 씬 뷰를 클릭하면 사이클 스폰 마커가 생성됩니다.\n" +
                "같은 ID 접두사를 연속 배치하면 고유 번호가 자동으로 붙습니다.",
                MessageType.Info);

            DrawSectionLabel("역할 프리셋");
            if (GUILayout.Button("Player 스폰"))
                SelectCycleSpawnPreset(CycleSpawnMarkerKind.Regular, CycleSpawnRole.Player, "player_spawn_pos");
            if (GUILayout.Button("Outer Boss 스폰"))
                SelectCycleSpawnPreset(CycleSpawnMarkerKind.Regular, CycleSpawnRole.OuterBoss, "outer_boss_spawn_pos");
            if (GUILayout.Button("Respawn 지점"))
                SelectCycleSpawnPreset(CycleSpawnMarkerKind.Regular, CycleSpawnRole.Respawn, "respawn_pos");
            if (GUILayout.Button("Player + Respawn"))
                SelectCycleSpawnPreset(CycleSpawnMarkerKind.Regular, CycleSpawnRole.Player | CycleSpawnRole.Respawn, "player_spawn_pos");
            if (GUILayout.Button("Central Boss 스폰"))
                SelectCycleSpawnPreset(CycleSpawnMarkerKind.CentralBoss, CycleSpawnRole.None, "central_boss");

            GUILayout.Space(8f);
            CycleSpawnPoint[] regular = FindObjectsByType<CycleSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            CentralBossSpawnPoint[] central = FindObjectsByType<CentralBossSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GUILayout.Label($"현재 씬: 일반 {regular.Length}개 / 중앙 보스 {central.Length}개", EditorStyles.wordWrappedMiniLabel);
            GUILayout.FlexibleSpace();
        }

        private void DrawCycleSpawnSettings()
        {
            DrawSectionLabel("사이클 스폰 설정");

            EditorGUI.BeginChangeCheck();
            _cycleSpawnMarkerKind = (CycleSpawnMarkerKind)EditorGUILayout.EnumPopup("Marker Kind", _cycleSpawnMarkerKind);
            if (EditorGUI.EndChangeCheck())
            {
                _cycleSpawnIdPrefix = _cycleSpawnMarkerKind == CycleSpawnMarkerKind.CentralBoss
                    ? "central_boss"
                    : GetDefaultCycleSpawnPrefix(_cycleSpawnRoles);
            }

            if (_cycleSpawnMarkerKind == CycleSpawnMarkerKind.Regular)
            {
                const CycleSpawnRole validRoles = CycleSpawnRole.Player | CycleSpawnRole.OuterBoss | CycleSpawnRole.Respawn;
                _cycleSpawnRoles = (CycleSpawnRole)EditorGUILayout.EnumFlagsField("Allowed Roles", _cycleSpawnRoles) & validRoles;
                _cycleSpawnIdPrefix = EditorGUILayout.TextField("Spawn ID Prefix", _cycleSpawnIdPrefix);
                _cycleSectorId = EditorGUILayout.TextField("Sector ID", _cycleSectorId);
                _cycleSafetyRadius = Mathf.Max(0f, EditorGUILayout.FloatField("Safety Radius", _cycleSafetyRadius));

                if ((_cycleSpawnRoles & CycleSpawnRole.OuterBoss) != 0 && string.IsNullOrWhiteSpace(_cycleSectorId))
                    DrawInlineNotice("OuterBoss 역할은 검증을 위해 Sector ID가 필요합니다.", MessageType.Warning);
                if (_cycleSpawnRoles == CycleSpawnRole.None)
                    DrawInlineNotice("Allowed Roles를 하나 이상 선택하세요.", MessageType.Warning);
            }
            else
            {
                _cycleSpawnIdPrefix = EditorGUILayout.TextField("Spawn ID", _cycleSpawnIdPrefix);
                int centralCount = FindObjectsByType<CentralBossSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                if (centralCount > 0)
                    DrawInlineNotice($"현재 씬에 CentralBossSpawnPoint가 이미 {centralCount}개 있습니다.", MessageType.Warning);
            }

            EditorGUILayout.Space(6f);
            DrawSectionLabel("배치 옵션");
            _parent = (Transform)EditorGUILayout.ObjectField("Parent", _parent, typeof(Transform), true);
            _autoCreateRoot = EditorGUILayout.Toggle("Auto Create Root", _autoCreateRoot);
            _selectAfterPlace = EditorGUILayout.Toggle("Select After Place", _selectAfterPlace);

            EditorGUILayout.Space(6f);
            DrawSectionLabel("정렬 및 배치 규칙");
            _raycastMask = LayerMaskField("Raycast Layer", _raycastMask);
            _heightOffset = EditorGUILayout.FloatField("Y Offset", _heightOffset);
            _yawOffset = EditorGUILayout.Slider("Yaw Offset", _yawOffset, -180f, 180f);
            _snapToGrid = EditorGUILayout.Toggle("Snap To Grid", _snapToGrid);
            using (new EditorGUI.DisabledScope(!_snapToGrid))
                _gridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size", _gridSize));

            DrawInlineNotice("사이클 스폰 지점은 Bake 대상이 아닌 씬 마커로 유지됩니다.", MessageType.Info);

            DrawCycleSpawnDashboard();
        }

        private void SelectCycleSpawnPreset(CycleSpawnMarkerKind kind, CycleSpawnRole roles, string idPrefix)
        {
            _cycleSpawnMarkerKind = kind;
            _cycleSpawnRoles = roles;
            _cycleSpawnIdPrefix = idPrefix;
            _placementMode = true;
            SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
            Repaint();
            SceneView.RepaintAll();
        }

        private string ApplyCycleSpawnData(GameObject instance)
        {
            string spawnId = _cycleSpawnMarkerKind == CycleSpawnMarkerKind.CentralBoss
                ? _cycleSpawnIdPrefix.Trim()
                : MakeUniqueCycleSpawnId(_cycleSpawnIdPrefix);

            if (_cycleSpawnMarkerKind == CycleSpawnMarkerKind.CentralBoss)
            {
                CentralBossSpawnPoint point = Undo.AddComponent<CentralBossSpawnPoint>(instance);
                SerializedObject serialized = new(point);
                serialized.FindProperty("_spawnId").stringValue = spawnId;
                serialized.ApplyModifiedProperties();
                instance.name = spawnId;
                return spawnId;
            }

            CycleSpawnPoint regularPoint = Undo.AddComponent<CycleSpawnPoint>(instance);
            SerializedObject regularSerialized = new(regularPoint);
            regularSerialized.FindProperty("_spawnId").stringValue = spawnId;
            regularSerialized.FindProperty("_allowedRoles").intValue = (int)_cycleSpawnRoles;
            regularSerialized.FindProperty("_sectorId").stringValue = _cycleSectorId.Trim();
            regularSerialized.FindProperty("_safetyRadius").floatValue = Mathf.Max(0f, _cycleSafetyRadius);
            regularSerialized.ApplyModifiedProperties();

            if ((_cycleSpawnRoles & CycleSpawnRole.Respawn) != 0)
            {
                CycleRespawnPoint respawnPoint = Undo.AddComponent<CycleRespawnPoint>(instance);
                SerializedObject respawnSerialized = new(respawnPoint);
                respawnSerialized.FindProperty("_respawnId").stringValue = spawnId;
                respawnSerialized.FindProperty("_isActive").boolValue = true;
                respawnSerialized.ApplyModifiedProperties();
            }

            instance.name = spawnId;
            return spawnId;
        }

    }
}
#endif
