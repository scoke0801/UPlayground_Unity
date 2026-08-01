#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.World;
using UPlayGround.Group;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>몬스터 그룹 프리셋: 조우 단위 배치, 씬 그룹 캡처, 프리셋 재적용.</summary>
    public partial class GatheringPlacementEditorWindow
    {
        private const string GroupPresetFolder = "Assets/10.Datas/World/GroupPreset";

        private readonly List<MonsterGroupPresetSO> _groupPresets = new();
        private MonsterGroupPresetSO _selectedGroupPreset;
        private Vector2 _groupPresetListScroll;
        private string _groupPresetSearchFilter = "";
        private bool _groupPresetMemberFoldout = true;

        // 앵커 방향 드래그 상태. 마우스 다운 지점 → 현재 지점 벡터로 그룹 forward를 정한다.
        private bool _groupAnchorDragging;
        private Vector3 _groupAnchorOrigin;
        private float _groupAnchorYaw;

        /// <summary>드래그 거리가 이 값 미만이면 단발 클릭으로 보고 씬 카메라 기준 yaw를 쓴다.</summary>
        private const float GroupAnchorDragThreshold = 0.75f;

        private bool IsGroupPresetMode =>
            _worldPlacementMode == WorldPlacementMode.Actor &&
            _actorSource == ActorPlacementSource.GroupPreset;

        #region 목록 / 갱신

        private void RefreshGroupPresets()
        {
            _groupPresets.Clear();

            string[] guids = AssetDatabase.FindAssets("t:MonsterGroupPresetSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<MonsterGroupPresetSO>(path);
                if (preset != null)
                    _groupPresets.Add(preset);
            }

            _groupPresets.Sort((a, b) =>
            {
                int byCategory = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
                return byCategory != 0
                    ? byCategory
                    : string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });

            if (_selectedGroupPreset != null && !_groupPresets.Contains(_selectedGroupPreset))
                _selectedGroupPreset = null;
        }

        private void DrawGroupPresetListPanel()
        {
            DrawSectionLabel("몬스터 그룹 프리셋");

            EditorGUILayout.BeginHorizontal();
            _groupPresetSearchFilter = EditorGUILayout.TextField(_groupPresetSearchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                RefreshGroupPresets();
            EditorGUILayout.EndHorizontal();

            if (_groupPresets.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "프리셋이 없습니다. 씬에서 다듬은 MonsterGroupController를 선택하고 우측의 '씬 그룹을 프리셋으로 저장'을 사용하세요.",
                    MessageType.Info);
                return;
            }

            _groupPresetListScroll = EditorGUILayout.BeginScrollView(_groupPresetListScroll);

            string lastCategory = null;
            foreach (var preset in _groupPresets)
            {
                if (preset == null || !ContainsIgnoreCase(preset.DisplayName, _groupPresetSearchFilter))
                    continue;

                if (preset.Category != lastCategory)
                {
                    lastCategory = preset.Category;
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField(lastCategory, EditorStyles.miniBoldLabel);
                }

                DrawGroupPresetRow(preset);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroupPresetRow(MonsterGroupPresetSO preset)
        {
            bool selected = _selectedGroupPreset == preset;
            var style = selected ? _selectedItemStyle : _normalItemStyle;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"{preset.DisplayName}  ({preset.TotalInstanceCount}체)", style))
                SelectGroupPreset(preset);

            if (GUILayout.Button("↗", GUILayout.Width(24f)))
                EditorGUIUtility.PingObject(preset);
            EditorGUILayout.EndHorizontal();
        }

        private void SelectGroupPreset(MonsterGroupPresetSO preset, bool armPlacement = true)
        {
            _selectedGroupPreset = preset;
            if (armPlacement && preset != null)
                _placementMode = true;

            SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
            SceneView.RepaintAll();
        }

        #endregion

        #region 우측 상세 패널

        private void DrawGroupPresetSettings()
        {
            EditorGUILayout.Space(6f);
            DrawSectionLabel("그룹 프리셋");

            _selectedGroupPreset = (MonsterGroupPresetSO)EditorGUILayout.ObjectField(
                "Preset", _selectedGroupPreset, typeof(MonsterGroupPresetSO), false);

            if (_selectedGroupPreset != null)
                DrawGroupPresetDetail(_selectedGroupPreset);
            else
                EditorGUILayout.HelpBox("좌측에서 프리셋을 선택하거나, 아래에서 씬 그룹을 캡처해 새 프리셋을 만드세요.", MessageType.Info);

            DrawGroupPresetCaptureSection();
            DrawGroupPresetReapplySection();
        }

        private void DrawGroupPresetDetail(MonsterGroupPresetSO preset)
        {
            EditorGUILayout.LabelField("멤버 종류", preset.Members.Count.ToString());
            EditorGUILayout.LabelField("생성 인스턴스", $"{preset.TotalInstanceCount}체");
            EditorGUILayout.LabelField("리비전", preset.Revision.ToString());

            if (!string.IsNullOrEmpty(preset.Description))
                EditorGUILayout.HelpBox(preset.Description, MessageType.None);

            int invalidCount = 0;
            foreach (var member in preset.Members)
                if (member == null || !member.IsValid)
                    invalidCount++;

            if (invalidCount > 0)
                DrawInlineNotice($"소스를 찾을 수 없는 멤버가 {invalidCount}개 있습니다. 해당 멤버는 배치되지 않습니다.", MessageType.Warning);

            _groupPresetMemberFoldout = EditorGUILayout.Foldout(_groupPresetMemberFoldout, "멤버 구성", true);
            if (!_groupPresetMemberFoldout)
                return;

            EditorGUI.indentLevel++;
            foreach (var member in preset.Members)
            {
                if (member == null)
                    continue;

                string countText = member.count > 1 ? $" ×{member.count}" : "";
                string offsetText = $"({member.localOffset.x:0.#}, {member.localOffset.z:0.#})";
                EditorGUILayout.LabelField($"{member.DisplayName}{countText}", offsetText);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawGroupPresetCaptureSection()
        {
            EditorGUILayout.Space(6f);
            var sceneGroup = GetSelectedSceneGroup();

            using (new EditorGUI.DisabledScope(sceneGroup == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("씬 그룹을 새 프리셋으로 저장"))
                    CaptureSceneGroup(sceneGroup, overwriteTarget: null);

                using (new EditorGUI.DisabledScope(_selectedGroupPreset == null))
                {
                    if (GUILayout.Button("선택 프리셋에 덮어쓰기", GUILayout.Width(150f)))
                        CaptureSceneGroup(sceneGroup, _selectedGroupPreset);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (sceneGroup == null)
                EditorGUILayout.HelpBox("하이어라키에서 MonsterGroupController를 선택하면 현재 구성을 프리셋으로 굳힐 수 있습니다.", MessageType.None);
        }

        private void DrawGroupPresetReapplySection()
        {
            var sceneGroup = GetSelectedSceneGroup();
            if (sceneGroup == null)
                return;

            var link = sceneGroup.GetComponent<MonsterGroupPresetLink>();
            if (link == null)
                return;

            var source = FindPresetById(link.PresetId);
            if (source == null)
            {
                DrawInlineNotice($"연결된 프리셋(id: {link.PresetId})을 찾을 수 없습니다.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4f);
            if (source.Revision > link.AppliedRevision)
                DrawInlineNotice($"'{source.DisplayName}' 프리셋이 갱신되었습니다 (r{link.AppliedRevision} → r{source.Revision}).", MessageType.Warning);
            else
                DrawInlineNotice($"'{source.DisplayName}' 프리셋과 동기화되어 있습니다 (r{link.AppliedRevision}).", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("위치만 재적용"))
                ReapplyPreset(sceneGroup, source, rebuildMembers: false);

            if (GUILayout.Button("멤버 구성까지 재적용"))
                ReapplyPreset(sceneGroup, source, rebuildMembers: true);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 그룹 오브젝트에 SceneEntityId를 보장한다.
        /// Bake 복원이 이름 대신 GUID로 그룹을 찾기 때문에 배치물 메타데이터 옵션과 무관하게 항상 붙인다.
        /// </summary>
        private static void EnsureGroupSceneEntityId(GameObject groupObject)
        {
            var entityId = groupObject.GetComponent<SceneEntityId>();
            if (entityId == null)
                entityId = Undo.AddComponent<SceneEntityId>(groupObject);

            if (entityId.HasGuid)
                return;

            Undo.RecordObject(entityId, "Set Group SceneEntityId");
            entityId.EditorSetGuid(Guid.NewGuid().ToString("N"));
            EditorUtility.SetDirty(entityId);
        }

        private static MonsterGroupController GetSelectedSceneGroup()
        {
            var active = Selection.activeGameObject;
            return active != null ? active.GetComponent<MonsterGroupController>() : null;
        }

        private MonsterGroupPresetSO FindPresetById(string presetId)
        {
            if (string.IsNullOrEmpty(presetId))
                return null;

            foreach (var preset in _groupPresets)
                if (preset != null && preset.PresetId == presetId)
                    return preset;

            return null;
        }

        #endregion

        #region 씬 뷰 배치

        /// <summary>그룹 프리셋 모드의 씬 입력 처리. 배치를 소비했으면 true.</summary>
        private bool HandleGroupPresetSceneInput(Event currentEvent, SceneView sceneView)
        {
            if (!IsGroupPresetMode)
                return false;

            switch (currentEvent.type)
            {
                case EventType.MouseDown when currentEvent.button == 0 && !currentEvent.alt:
                    if (!CanPlace(out string reason))
                    {
                        SetTemporaryStatus(reason, MessageType.Warning);
                        currentEvent.Use();
                        return true;
                    }

                    _groupAnchorDragging = true;
                    _groupAnchorOrigin = _previewPosition;
                    _groupAnchorYaw = GetSceneViewYaw(sceneView);
                    currentEvent.Use();
                    return true;

                case EventType.MouseDrag when _groupAnchorDragging:
                    UpdateGroupAnchorYaw(sceneView);
                    sceneView.Repaint();
                    currentEvent.Use();
                    return true;

                case EventType.MouseUp when _groupAnchorDragging && currentEvent.button == 0:
                    _groupAnchorDragging = false;
                    UpdateGroupAnchorYaw(sceneView);
                    PlaceGroupPreset(_groupAnchorOrigin, _groupAnchorYaw);
                    currentEvent.Use();
                    return true;
            }

            return false;
        }

        private void UpdateGroupAnchorYaw(SceneView sceneView)
        {
            Vector3 delta = _previewPosition - _groupAnchorOrigin;
            delta.y = 0f;

            _groupAnchorYaw = delta.magnitude >= GroupAnchorDragThreshold
                ? Quaternion.LookRotation(delta.normalized, Vector3.up).eulerAngles.y
                : GetSceneViewYaw(sceneView);
        }

        private static float GetSceneViewYaw(SceneView sceneView)
        {
            if (sceneView == null || sceneView.camera == null)
                return 0f;

            Vector3 forward = sceneView.camera.transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude < 0.0001f ? 0f : Quaternion.LookRotation(forward.normalized, Vector3.up).eulerAngles.y;
        }

        private void DrawGroupPresetScenePreview()
        {
            if (!IsGroupPresetMode || _selectedGroupPreset == null || !_hasPreviewHit)
                return;

            Vector3 anchor = _groupAnchorDragging ? _groupAnchorOrigin : _previewPosition;
            float yaw = _groupAnchorDragging ? _groupAnchorYaw : GetSceneViewYaw(SceneView.lastActiveSceneView);
            Quaternion anchorRotation = Quaternion.Euler(0f, yaw, 0f);

            Handles.color = new Color(0.35f, 0.75f, 1f, 0.9f);
            Handles.DrawWireDisc(anchor, Vector3.up, _selectedGroupPreset.AnchorRadiusHint);
            Handles.ArrowHandleCap(0, anchor, anchorRotation, _selectedGroupPreset.AnchorRadiusHint * 0.6f, EventType.Repaint);

            int placeable = 0;
            int total = 0;

            foreach (var member in _selectedGroupPreset.Members)
            {
                if (member == null || !member.IsValid)
                    continue;

                int count = Mathf.Max(1, member.count);
                for (int i = 0; i < count; i++)
                {
                    total++;
                    Vector3 flat = anchor + anchorRotation * GetMemberLocalPosition(member, i);
                    bool grounded = TryResolveMemberSurface(flat, out Vector3 memberPosition, out _);
                    if (grounded)
                        placeable++;

                    Handles.color = grounded ? new Color(0.25f, 0.9f, 0.35f, 0.9f) : Color.red;
                    Handles.DrawWireDisc(memberPosition, Vector3.up, 0.5f);
                    Handles.DrawDottedLine(anchor, memberPosition, 3f);
                }
            }

            Handles.color = Color.white;
            Handles.Label(anchor + Vector3.up * 1.6f,
                $"{_selectedGroupPreset.DisplayName}  {placeable}/{total} 배치 가능");
        }

        /// <summary>멤버의 앵커 기준 로컬 XZ 위치. count가 2 이상이면 결정적 지터를 적용한다.</summary>
        private static Vector3 GetMemberLocalPosition(MonsterGroupPresetMember member, int index)
        {
            var local = new Vector3(member.localOffset.x, 0f, member.localOffset.z);
            if (index == 0 || member.jitterRadius <= 0f)
                return local;

            // 프리뷰와 실제 배치가 같은 결과를 내야 하므로 난수 대신 각도 분산을 쓴다.
            float angle = index * 137.508f * Mathf.Deg2Rad;
            float radius = member.jitterRadius * Mathf.Sqrt(index / (float)Mathf.Max(1, member.count));
            return local + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private bool TryResolveMemberSurface(Vector3 flatPosition, out Vector3 position, out Vector3 normal)
        {
            const float probeHalfHeight = 5000f;
            var origin = new Vector3(flatPosition.x, flatPosition.y + probeHalfHeight, flatPosition.z);

            if (Physics.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, probeHalfHeight * 2f, _raycastMask, GetTriggerInteraction()))
            {
                normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
                position = hit.point + normal * _heightOffset;
                return true;
            }

            normal = Vector3.up;
            position = flatPosition;
            return false;
        }

        private void PlaceGroupPreset(Vector3 anchorPosition, float yaw)
        {
            var preset = _selectedGroupPreset;
            if (preset == null)
                return;

            using var undoScope = new PlacementUndoScope($"Place Group Preset: {preset.DisplayName}");

            Quaternion anchorRotation = Quaternion.Euler(0f, yaw, 0f);
            Transform parent = _parent != null ? _parent : GetOrCreatePlacementRoot(ActorPlacementRootName);

            var anchorObject = new GameObject(GameObjectUtility.GetUniqueNameForSibling(parent, preset.DisplayName));
            Undo.RegisterCreatedObjectUndo(anchorObject, "Create Group Anchor");
            if (parent != null)
                Undo.SetTransformParent(anchorObject.transform, parent, "Group Anchor Parent");

            anchorObject.transform.SetPositionAndRotation(anchorPosition, anchorRotation);

            var group = Undo.AddComponent<MonsterGroupController>(anchorObject);
            if (preset.ApplyGroupSettings)
                MonsterGroupSettingsSnapshot.Apply(group, preset.GroupSettingsJson);

            var link = Undo.AddComponent<MonsterGroupPresetLink>(anchorObject);
            link.EditorSetLink(preset.PresetId, preset.Revision);
            EditorUtility.SetDirty(link);

            // Bake 레코드가 그룹을 GUID로 되찾을 수 있어야 동명 그룹 오복원을 피한다.
            EnsureGroupSceneEntityId(anchorObject);

            int placed = 0;
            int skipped = 0;

            foreach (var member in preset.Members)
            {
                if (member == null || !member.IsValid)
                {
                    skipped++;
                    continue;
                }

                int count = Mathf.Max(1, member.count);
                for (int i = 0; i < count; i++)
                {
                    if (SpawnPresetMember(member, i, anchorObject.transform, anchorPosition, anchorRotation))
                        placed++;
                    else
                        skipped++;
                }
            }

            if (placed == 0)
            {
                SetTemporaryStatus("배치 가능한 멤버가 없어 그룹 생성을 취소했습니다.", MessageType.Warning);
                return; // Complete를 호출하지 않으므로 스코프가 앵커 생성까지 롤백한다.
            }

            _sessionPlacementCount += placed;
            _targetGroup = group;

            if (_selectAfterPlace)
                Selection.activeGameObject = anchorObject;

            string message = skipped > 0
                ? $"'{preset.DisplayName}' 배치 완료 — {placed}체 생성, {skipped}체 건너뜀"
                : $"'{preset.DisplayName}' 배치 완료 — {placed}체 생성";
            SetTemporaryStatus(message, skipped > 0 ? MessageType.Warning : MessageType.Info);

            EditorSceneManager.MarkSceneDirty(anchorObject.scene);
            undoScope.Complete();
            Repaint();
        }

        private bool SpawnPresetMember(
            MonsterGroupPresetMember member,
            int index,
            Transform anchor,
            Vector3 anchorPosition,
            Quaternion anchorRotation)
        {
            GameObject prefab = member.ResolvePrefab();
            if (prefab == null)
                return false;

            Vector3 flat = anchorPosition + anchorRotation * GetMemberLocalPosition(member, index);
            if (!TryResolveMemberSurface(flat, out Vector3 position, out Vector3 normal))
                return false;

            var instance = InstantiatePrefab(prefab);
            if (instance == null)
                return false;

            Undo.RegisterCreatedObjectUndo(instance, "Place Group Member");
            Undo.SetTransformParent(instance.transform, anchor, "Group Member Parent");

            Quaternion rotation = anchorRotation * Quaternion.Euler(0f, member.localYaw, 0f);
            if (_alignToSurface)
                rotation = Quaternion.FromToRotation(Vector3.up, normal) * rotation;

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = member.scale;

            // 표면 스냅은 기존 경로를 그대로 재사용한다(스냅 규칙 구현을 이중화하지 않는다).
            Vector3 savedPosition = _previewPosition;
            Vector3 savedNormal = _previewNormal;
            _previewPosition = position;
            _previewNormal = normal;
            StickInstanceToSurface(new PlacementInstance(instance, instance, moveSurfaceTargetOnly: false));
            _previewPosition = savedPosition;
            _previewNormal = savedNormal;

            ApplyMemberActorDefinition(instance, member);
            AddSceneEntityIdIfNeeded(instance);
            AddGroupMemberMetadata(instance, member);

            instance.SetActive(member.initiallyActive);

            return true;
        }

        /// <summary>멤버 인스턴스에 ActorDefinition의 actorId를 주입한다. 단발 배치의 주입 규칙과 동일하다.</summary>
        private void ApplyMemberActorDefinition(GameObject instance, MonsterGroupPresetMember member)
        {
            if (member.definition == null)
                return;

            var actor = instance.GetComponent<GameActor>();
            if (actor == null)
            {
                Debug.LogWarning($"[WorldPlacement] '{instance.name}'에 GameActor가 없어 actorId를 주입하지 못했습니다.", instance);
                return;
            }

            var serializedActor = new SerializedObject(actor);
            var actorIdProperty = serializedActor.FindProperty("_actorId");
            if (actorIdProperty == null)
            {
                Debug.LogWarning($"[WorldPlacement] '{instance.name}'에서 _actorId 프로퍼티를 찾지 못했습니다.", instance);
                return;
            }

            actorIdProperty.stringValue = member.definition.actorId;
            serializedActor.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(actor);
        }

        private void AddGroupMemberMetadata(GameObject instance, MonsterGroupPresetMember member)
        {
            if (!_addPlacementMetadata)
                return;

            var metadata = instance.GetComponent<WorldPlacementMetadata>();
            if (metadata == null)
                metadata = Undo.AddComponent<WorldPlacementMetadata>(instance);
            else
                Undo.RecordObject(metadata, "Set World Placement Metadata");

            bool fromDefinition = member.definition != null;
            metadata.EditorSetPlacementInfo(
                fromDefinition
                    ? WorldPlacementMetadata.PlacementSourceKind.ActorDefinition
                    : WorldPlacementMetadata.PlacementSourceKind.DirectPrefab,
                fromDefinition ? member.definition.actorId : GetAssetGuid(member.directPrefab),
                _placementBakeMode,
                cellId: "",
                randomSeed: UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                initiallyActive: member.initiallyActive);
            EditorUtility.SetDirty(metadata);
        }

        #endregion

        #region 씬 그룹 → 프리셋 캡처

        private void CaptureSceneGroup(MonsterGroupController group, MonsterGroupPresetSO overwriteTarget)
        {
            if (group == null)
                return;

            if (!TryBuildMembersFromGroup(group, out List<MonsterGroupPresetMember> members, out string error))
            {
                EditorUtility.DisplayDialog("프리셋 캡처 실패", error, "확인");
                SetTemporaryStatus(error, MessageType.Error);
                return;
            }

            MonsterGroupPresetSO target = overwriteTarget;
            if (target == null)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "그룹 프리셋 저장", $"GroupPreset_{group.name}", "asset", "새 프리셋 에셋 경로를 지정하세요.", GroupPresetFolder);

                if (string.IsNullOrEmpty(path))
                    return;

                EnsureFolder(GroupPresetFolder);
                target = ScriptableObject.CreateInstance<MonsterGroupPresetSO>();
                AssetDatabase.CreateAsset(target, path);
            }
            else if (!EditorUtility.DisplayDialog(
                         "프리셋 덮어쓰기",
                         $"'{target.DisplayName}'의 멤버 구성과 그룹 설정을 현재 씬 그룹으로 교체합니다.\n되돌릴 수 없습니다. 계속할까요?",
                         "덮어쓰기", "취소"))
            {
                return;
            }

            Undo.RecordObject(target, "Capture Group Preset");
            target.EditorEnsurePresetId();
            target.EditorSetContent(
                target.PresetId,
                MonsterGroupSettingsSnapshot.Capture(group),
                members);

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            RefreshGroupPresets();
            SelectGroupPreset(target, armPlacement: false);

            // 캡처 원본에도 링크를 남겨 이후 갱신 흐름에 태운다.
            var link = group.GetComponent<MonsterGroupPresetLink>();
            if (link == null)
                link = Undo.AddComponent<MonsterGroupPresetLink>(group.gameObject);
            else
                Undo.RecordObject(link, "Link Group Preset");

            link.EditorSetLink(target.PresetId, target.Revision);
            EditorUtility.SetDirty(link);

            SetTemporaryStatus($"'{target.DisplayName}' 프리셋에 {members.Count}종 / {target.TotalInstanceCount}체를 캡처했습니다.", MessageType.Info);
        }

        /// <summary>
        /// 씬 그룹의 자식 몬스터를 프리셋 멤버로 변환한다.
        /// 소스를 특정할 수 없는 멤버가 하나라도 있으면 캡처 전체를 실패시킨다.
        /// 조용히 건너뛰면 프리셋이 조용히 비어버리기 때문이다.
        /// </summary>
        private bool TryBuildMembersFromGroup(
            MonsterGroupController group,
            out List<MonsterGroupPresetMember> members,
            out string error)
        {
            members = new List<MonsterGroupPresetMember>();
            error = null;

            Transform anchor = group.transform;
            Quaternion inverseAnchor = Quaternion.Inverse(anchor.rotation);

            var actors = group.GetComponentsInChildren<MonsterActor>(includeInactive: true);
            if (actors.Length == 0)
            {
                error = "그룹에 MonsterActor 자식이 없습니다.";
                return false;
            }

            var unresolved = new List<string>();

            foreach (var actor in actors)
            {
                if (actor == null)
                    continue;

                var member = new MonsterGroupPresetMember();
                if (!TryResolveMemberSource(actor.gameObject, member))
                {
                    unresolved.Add(actor.name);
                    continue;
                }

                Vector3 local = inverseAnchor * (actor.transform.position - anchor.position);
                member.localOffset = new Vector3(local.x, 0f, local.z);
                member.localYaw = Mathf.DeltaAngle(anchor.eulerAngles.y, actor.transform.eulerAngles.y);
                member.scale = actor.transform.localScale;
                member.count = 1;
                member.initiallyActive = actor.gameObject.activeSelf;
                members.Add(member);
            }

            if (unresolved.Count > 0)
            {
                error = $"배치 소스를 찾을 수 없는 멤버가 있어 캡처를 중단했습니다: {string.Join(", ", unresolved)}\n" +
                        "해당 오브젝트에 WorldPlacementMetadata가 없거나 프리팹 연결이 끊어졌는지 확인하세요.";
                return false;
            }

            return true;
        }

        private bool TryResolveMemberSource(GameObject instance, MonsterGroupPresetMember member)
        {
            var metadata = instance.GetComponent<WorldPlacementMetadata>();
            if (metadata != null && !string.IsNullOrEmpty(metadata.SourceId))
            {
                if (metadata.SourceKind == WorldPlacementMetadata.PlacementSourceKind.ActorDefinition)
                {
                    member.definition = FindDefinitionByActorId(metadata.SourceId);
                    if (member.definition != null)
                        return true;
                }
                else if (metadata.SourceKind == WorldPlacementMetadata.PlacementSourceKind.DirectPrefab)
                {
                    string path = AssetDatabase.GUIDToAssetPath(metadata.SourceId);
                    member.directPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (member.directPrefab != null)
                        return true;
                }
            }

            // 메타데이터가 없어도 프리팹 인스턴스면 원본을 역참조할 수 있다.
            var prefabSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
            if (prefabSource != null)
            {
                member.directPrefab = prefabSource;
                return true;
            }

            return false;
        }

        private ActorDefinitionSO FindDefinitionByActorId(string actorId)
        {
            foreach (var definition in _actorDefinitions)
                if (definition != null && definition.actorId == actorId)
                    return definition;

            return null;
        }

        #endregion

        #region 프리셋 재적용

        private void ReapplyPreset(MonsterGroupController group, MonsterGroupPresetSO preset, bool rebuildMembers)
        {
            if (group == null || preset == null)
                return;

            string body = rebuildMembers
                ? $"'{group.name}'의 기존 멤버를 모두 지우고 '{preset.DisplayName}' 구성으로 다시 생성합니다."
                : $"'{group.name}'의 멤버 위치를 '{preset.DisplayName}' 기준으로 되돌립니다. 수동 미세조정은 사라집니다.";

            if (!EditorUtility.DisplayDialog("프리셋 재적용", body + "\n계속할까요?", "재적용", "취소"))
                return;

            using var undoScope = new PlacementUndoScope($"Reapply Group Preset: {preset.DisplayName}");

            if (preset.ApplyGroupSettings)
                MonsterGroupSettingsSnapshot.Apply(group, preset.GroupSettingsJson);

            bool succeeded = rebuildMembers
                ? RebuildGroupMembers(group, preset)
                : RepositionGroupMembers(group, preset);

            if (!succeeded)
            {
                SetTemporaryStatus("재적용에 실패해 변경을 모두 되돌렸습니다.", MessageType.Error);
                return;
            }

            var link = group.GetComponent<MonsterGroupPresetLink>();
            if (link == null)
                link = Undo.AddComponent<MonsterGroupPresetLink>(group.gameObject);
            else
                Undo.RecordObject(link, "Update Group Preset Link");

            link.EditorSetLink(preset.PresetId, preset.Revision);
            EditorUtility.SetDirty(link);

            EditorSceneManager.MarkSceneDirty(group.gameObject.scene);
            undoScope.Complete();
            SetTemporaryStatus($"'{preset.DisplayName}' 프리셋을 재적용했습니다 (r{preset.Revision}).", MessageType.Info);
            Repaint();
        }

        /// <summary>기존 멤버를 유지한 채 위치/회전만 프리셋 기준으로 되돌린다.</summary>
        private bool RepositionGroupMembers(MonsterGroupController group, MonsterGroupPresetSO preset)
        {
            var actors = group.GetComponentsInChildren<MonsterActor>(includeInactive: true);
            if (actors.Length != preset.TotalInstanceCount)
                return false;

            Transform anchor = group.transform;
            Quaternion anchorRotation = anchor.rotation;
            var unmatchedActors = new List<MonsterActor>(actors);
            var plan = new List<GroupMemberReposition>(actors.Length);

            foreach (var member in preset.Members)
            {
                if (member == null || !member.IsValid)
                    continue;

                int count = Mathf.Max(1, member.count);
                for (int i = 0; i < count; i++)
                {
                    int actorIndex = unmatchedActors.FindIndex(actor => DoesActorMatchPresetMember(actor, member));
                    if (actorIndex < 0)
                        return false;

                    var actor = unmatchedActors[actorIndex];
                    unmatchedActors.RemoveAt(actorIndex);

                    Vector3 flat = anchor.position + anchorRotation * GetMemberLocalPosition(member, i);
                    if (!TryResolveSurfaceIgnoring(flat, actor.gameObject, out Vector3 position, out Vector3 normal))
                        return false;

                    Quaternion rotation = anchorRotation * Quaternion.Euler(0f, member.localYaw, 0f);
                    if (_alignToSurface)
                        rotation = Quaternion.FromToRotation(Vector3.up, normal) * rotation;

                    plan.Add(new GroupMemberReposition(actor.transform, position, rotation));
                }
            }

            if (unmatchedActors.Count != 0)
                return false;

            foreach (var entry in plan)
            {
                Undo.RecordObject(entry.Transform, "Reposition Group Member");
                entry.Transform.SetPositionAndRotation(entry.Position, entry.Rotation);
                EditorUtility.SetDirty(entry.Transform);
            }

            return true;
        }

        private static bool DoesActorMatchPresetMember(MonsterActor actor, MonsterGroupPresetMember member)
        {
            if (actor == null || member == null)
                return false;

            var metadata = actor.GetComponent<WorldPlacementMetadata>();
            if (member.definition != null)
            {
                if (metadata != null
                    && metadata.SourceKind == WorldPlacementMetadata.PlacementSourceKind.ActorDefinition)
                    return metadata.SourceId == member.definition.actorId;

                return actor.ActorId == member.definition.actorId;
            }

            if (member.directPrefab == null)
                return false;

            string expectedGuid = GetAssetGuid(member.directPrefab);
            if (metadata != null
                && metadata.SourceKind == WorldPlacementMetadata.PlacementSourceKind.DirectPrefab
                && !string.IsNullOrEmpty(metadata.SourceId))
                return metadata.SourceId == expectedGuid;

            return PrefabUtility.GetCorrespondingObjectFromOriginalSource(actor.gameObject) == member.directPrefab;
        }

        /// <summary>기존 멤버를 제거하고 프리셋 구성으로 다시 만든다.</summary>
        private bool RebuildGroupMembers(MonsterGroupController group, MonsterGroupPresetSO preset)
        {
            var actors = group.GetComponentsInChildren<MonsterActor>(includeInactive: true);
            foreach (var actor in actors)
                if (actor != null)
                    Undo.DestroyObjectImmediate(actor.gameObject);

            Transform anchor = group.transform;
            Quaternion anchorRotation = anchor.rotation;
            int placed = 0;

            foreach (var member in preset.Members)
            {
                if (member == null || !member.IsValid)
                    continue;

                int count = Mathf.Max(1, member.count);
                for (int i = 0; i < count; i++)
                    if (SpawnPresetMember(member, i, anchor, anchor.position, anchorRotation))
                        placed++;
            }

            int expected = preset.TotalInstanceCount;
            return expected > 0 && placed == expected;
        }

        private readonly struct GroupMemberReposition
        {
            public GroupMemberReposition(Transform transform, Vector3 position, Quaternion rotation)
            {
                Transform = transform;
                Position = position;
                Rotation = rotation;
            }

            public Transform Transform { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
        }

        #endregion
    }
}
#endif
