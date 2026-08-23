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
    /// <summary>씬 뷰 상호작용, 표면 스냅, 콜라이더 자동 설정 등 지오메트리 처리.</summary>
    public partial class GatheringPlacementEditorWindow
    {
        private void OnSceneGUI(SceneView sceneView)
        {
            Event currentEvent = Event.current;

            if (_placementMode)
            {
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddDefaultControl(controlId);
            }

            HandleSceneShortcuts(currentEvent);
            DrawBakedRecordMarkers();

            if (!_placementMode)
                return;

            UpdatePreview(currentEvent.mousePosition);
            DrawScenePreview();

            // 브러시는 드래그 스트로크 단위로 동작하므로 단발 클릭 배치보다 먼저 가로챈다.
            if (HandleBrushSceneInput(currentEvent, sceneView))
            {
                sceneView.Repaint();
                return;
            }

            // 그룹 프리셋은 앵커 방향을 드래그로 정하므로 MouseDown/Drag/Up 흐름을 따로 처리한다.
            if (HandleGroupPresetSceneInput(currentEvent, sceneView))
            {
                sceneView.Repaint();
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                if (!CanPlace(out string reason))
                {
                    SetTemporaryStatus(reason, MessageType.Warning);
                    currentEvent.Use();
                    return;
                }

                PlaceCurrent();
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseMove || currentEvent.type == EventType.MouseDrag)
                sceneView.Repaint();
        }

        private void HandleSceneShortcuts(Event currentEvent)
        {
            if (currentEvent.type != EventType.KeyDown)
                return;

            if (currentEvent.keyCode == KeyCode.Escape && _placementMode)
            {
                _placementMode = false;
                SetPersistentStatus("배치 모드를 종료했습니다.", MessageType.Info);
                currentEvent.Use();
                Repaint();
                SceneView.RepaintAll();
                return;
            }

            if (TrySelectRecentByKey(currentEvent.keyCode))
            {
                currentEvent.Use();
                Repaint();
                SceneView.RepaintAll();
            }
        }

        private void UpdatePreview(Vector2 guiMousePosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiMousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 10000f, _raycastMask, GetTriggerInteraction()))
            {
                _previewPosition = ApplyPositionRules(hit.point, hit.normal, out _previewNormal);
                _hasPreviewHit = true;
                return;
            }

            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                _previewPosition = ApplyPositionRules(ray.GetPoint(enter), Vector3.up, out _previewNormal);
                _hasPreviewHit = true;
                return;
            }

            _hasPreviewHit = false;
        }

        private Vector3 ApplyPositionRules(Vector3 position, Vector3 normal, out Vector3 resolvedNormal)
        {
            resolvedNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;

            if (_snapToGrid && Event.current != null && !Event.current.shift)
            {
                position.x = Mathf.Round(position.x / _gridSize) * _gridSize;
                position.z = Mathf.Round(position.z / _gridSize) * _gridSize;
                position = ResolveSurfaceAtPosition(position, resolvedNormal, out resolvedNormal);
            }

            return position + resolvedNormal * _heightOffset;
        }

        private Vector3 ResolveSurfaceAtPosition(Vector3 position, Vector3 fallbackNormal, out Vector3 resolvedNormal)
        {
            const float verticalProbeHalfHeight = 5000f;
            var origin = new Vector3(position.x, position.y + verticalProbeHalfHeight, position.z);
            var ray = new Ray(origin, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, verticalProbeHalfHeight * 2f, _raycastMask, GetTriggerInteraction()))
            {
                resolvedNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
                return hit.point;
            }

            resolvedNormal = fallbackNormal.sqrMagnitude > 0.0001f ? fallbackNormal.normalized : Vector3.up;
            return position;
        }

        private QueryTriggerInteraction GetTriggerInteraction()
        {
            return _ignoreTriggerColliders ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.UseGlobal;
        }

        /// <summary>'씬에 표시'가 켜진 동안 선택된 Bake 데이터의 레코드 위치를 씬 뷰에 마커로 그린다.</summary>
        private void DrawBakedRecordMarkers()
        {
            if (!_showBakedInScene || _selectedBakedData == null)
                return;

            var records = _selectedBakedData.Records;
            // 대량 레코드에서 씬 뷰 핸들 드로우가 프레임을 잡아먹지 않도록 상한을 둔다.
            int max = Mathf.Min(records.Count, 300);
            Handles.color = new Color(1f, 0.85f, 0.3f, 0.9f);
            for (int i = 0; i < max; i++)
            {
                var record = records[i];
                if (record == null)
                    continue;

                Handles.DrawWireDisc(record.position, Vector3.up, 0.6f);
                Handles.DrawLine(record.position, record.position + Vector3.up * 1.2f);
                Handles.Label(record.position + Vector3.up * 1.4f, WorldPlacementBakeUtility.GetRecordDisplayName(record));
            }
        }

        private void DrawScenePreview()
        {
            if (!_hasPreviewHit)
                return;

            if (IsGroupPresetMode)
            {
                _previewIssues.Clear();
                DrawGroupPresetScenePreview();
                return;
            }

            if (IsBrushMode)
            {
                DrawBrushScenePreview();
                return;
            }

            // CanPlace가 차단 이슈를 참조하므로 평가를 먼저 돌린다.
            EvaluatePlacementIssues(_previewPosition, _previewNormal);
            bool canPlace = CanPlace(out _);
            bool hasIssue = _previewIssues.Count > 0;

            Handles.color = !canPlace
                ? Color.red
                : hasIssue
                    ? new Color(0.95f, 0.7f, 0.2f, 0.95f)
                    : new Color(0.25f, 0.9f, 0.35f, 0.95f);

            Handles.DrawWireDisc(_previewPosition, _previewNormal, 0.75f);
            Handles.DrawLine(_previewPosition, _previewPosition + _previewNormal.normalized * 1.5f);

            string label = canPlace ? GetSelectedPlacementTitle() + BuildIssueSummary() : "배치할 데이터 없음";
            Handles.Label(_previewPosition + Vector3.up * 1.25f, label);
        }

        private static GameObject InstantiatePrefab(GameObject targetPrefab)
        {
            var prefabInstance = PrefabUtility.InstantiatePrefab(targetPrefab, SceneManager.GetActiveScene()) as GameObject;
            return prefabInstance != null ? prefabInstance : Instantiate(targetPrefab);
        }

        /// <summary>대상 자신의 콜라이더를 잠시 제외하고 아래 표면을 찾는다.</summary>
        private bool TryResolveSurfaceIgnoring(
            Vector3 flatPosition,
            GameObject ignoredRoot,
            out Vector3 position,
            out Vector3 normal)
        {
            if (ignoredRoot == null)
                return TryResolveMemberSurface(flatPosition, out position, out normal);

            var colliders = ignoredRoot.GetComponentsInChildren<Collider>(includeInactive: true);
            var enabledStates = new bool[colliders.Length];
            try
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    var collider = colliders[i];
                    if (collider == null)
                        continue;

                    enabledStates[i] = collider.enabled;
                    collider.enabled = false;
                }

                return TryResolveMemberSurface(flatPosition, out position, out normal);
            }
            finally
            {
                for (int i = 0; i < colliders.Length; i++)
                    if (colliders[i] != null)
                        colliders[i].enabled = enabledStates[i];
            }
        }

        private void ApplyInteractableLayer(GameObject instance)
        {
            int layer = LayerMask.NameToLayer(InteractableObjectLayerName);
            if (layer < 0)
            {
                Debug.LogWarning("[GatheringPlacement] 'InteractableObject' Layer를 찾지 못했습니다. 배치 오브젝트 Layer를 변경하지 않았습니다.", instance);
                return;
            }

            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                Undo.RecordObject(child.gameObject, "Set Gathering Layer");
                child.gameObject.layer = layer;
                EditorUtility.SetDirty(child.gameObject);
            }
        }

        private void SetupColliderIfNeeded(GameObject instance)
        {
            if (!_autoSetupCollider)
                return;

            if (HasRegularCollider(instance))
                return;

            RemoveMeshColliders(instance);
            AddColliderFromRendererBounds(instance);
        }

        private static bool HasRegularCollider(GameObject instance)
        {
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null && collider is not MeshCollider)
                    return true;
            }

            return false;
        }

        private static void RemoveMeshColliders(GameObject instance)
        {
            foreach (var meshCollider in instance.GetComponentsInChildren<MeshCollider>(true))
            {
                if (meshCollider == null)
                    continue;

                Undo.DestroyObjectImmediate(meshCollider);
            }
        }

        private static void AddColliderFromRendererBounds(GameObject instance)
        {
            if (!TryGetRendererBounds(instance, out Bounds worldBounds))
            {
                var fallbackCollider = Undo.AddComponent<BoxCollider>(instance);
                fallbackCollider.center = Vector3.up * 0.5f;
                fallbackCollider.size = Vector3.one;
                EditorUtility.SetDirty(fallbackCollider);
                return;
            }

            Vector3 localCenter = instance.transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = WorldSizeToLocalSize(instance.transform, worldBounds.size);

            if (ShouldUseCapsule(localSize))
                AddCapsuleCollider(instance, localCenter, localSize);
            else
                AddBoxCollider(instance, localCenter, localSize);
        }

        private static bool TryGetRendererBounds(GameObject instance, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private void StickInstanceToSurface(PlacementInstance placement)
        {
            if (_surfaceSnapMode == SurfaceSnapMode.None)
                return;

            GameObject surfaceTarget = placement.SurfaceTarget;
            if (surfaceTarget == null)
                return;

            Vector3 surfaceNormal = _previewNormal.sqrMagnitude > 0.0001f ? _previewNormal.normalized : Vector3.up;
            if (!TryGetLowestSupportProjection(surfaceTarget, surfaceNormal, out float lowestProjection))
                return;

            float targetProjection = Vector3.Dot(_previewPosition, surfaceNormal);
            float offset = targetProjection - lowestProjection;

            // 내리기만 모드: 최저점이 표면 아래에 있으면(뿌리·밑동 등 파묻히라고 만든 여유 지오메트리)
            // 저작된 피벗을 신뢰하고 끌어올리지 않는다. 비주얼이 표면 위에 떠 있을 때만 아래로 내린다.
            if (_surfaceSnapMode == SurfaceSnapMode.LowerOnly && offset >= -0.0001f)
                return;

            if (Mathf.Abs(offset) <= 0.0001f)
                return;

            Transform targetTransform = placement.MoveSurfaceTargetOnly
                ? surfaceTarget.transform
                : placement.Root.transform;
            Undo.RecordObject(targetTransform, "Stick Gathering To Surface");
            targetTransform.position += surfaceNormal * offset;
            EditorUtility.SetDirty(targetTransform);
        }

        private static bool TryGetLowestSupportProjection(GameObject instance, Vector3 normal, out float lowestProjection)
        {
            lowestProjection = float.PositiveInfinity;
            bool hasProjection = false;

            // 비활성 자식(채집 후 그루터기, 파편 등)이나 꺼진 렌더러는 최저점 계산을 오염시키므로 제외한다.
            foreach (var meshFilter in instance.GetComponentsInChildren<MeshFilter>(false))
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                if (!meshFilter.TryGetComponent(out MeshRenderer meshRenderer) || !meshRenderer.enabled)
                    continue;

                EncapsulateLocalBoundsProjection(meshFilter.transform, meshFilter.sharedMesh.bounds, normal, ref lowestProjection);
                hasProjection = true;
            }

            foreach (var skinnedMeshRenderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null || !skinnedMeshRenderer.enabled)
                    continue;

                // SkinnedMeshRenderer.localBounds는 rootBone 기준 공간이다 (rootBone이 없으면 SMR 트랜스폼 기준).
                Transform boundsSpace = skinnedMeshRenderer.rootBone != null
                    ? skinnedMeshRenderer.rootBone
                    : skinnedMeshRenderer.transform;
                EncapsulateLocalBoundsProjection(boundsSpace, skinnedMeshRenderer.localBounds, normal, ref lowestProjection);
                hasProjection = true;
            }

            if (hasProjection)
                return true;

            // 배치 직후에는 물리 동기화 전이라 collider.bounds가 이동 전 위치 기준일 수 있다.
            Physics.SyncTransforms();

            Bounds bounds = default;
            foreach (var collider in instance.GetComponentsInChildren<Collider>(false))
            {
                if (collider == null || !collider.enabled)
                    continue;

                if (!hasProjection)
                {
                    bounds = collider.bounds;
                    hasProjection = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (!hasProjection)
                return false;

            lowestProjection = GetLowestWorldAabbProjection(bounds, normal);
            return true;
        }

        private static void EncapsulateLocalBoundsProjection(Transform transform, Bounds localBounds, Vector3 normal, ref float lowestProjection)
        {
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        var localCorner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 worldCorner = transform.TransformPoint(localCorner);
                        lowestProjection = Mathf.Min(lowestProjection, Vector3.Dot(worldCorner, normal));
                    }
                }
            }
        }

        private static float GetLowestWorldAabbProjection(Bounds bounds, Vector3 normal)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float lowest = float.PositiveInfinity;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        var corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        lowest = Mathf.Min(lowest, Vector3.Dot(corner, normal));
                    }
                }
            }

            return lowest;
        }

        private static Vector3 WorldSizeToLocalSize(Transform root, Vector3 worldSize)
        {
            Vector3 scale = root.lossyScale;
            return new Vector3(
                DivideByScale(worldSize.x, scale.x),
                DivideByScale(worldSize.y, scale.y),
                DivideByScale(worldSize.z, scale.z));
        }

        private static float DivideByScale(float value, float scale)
        {
            scale = Mathf.Abs(scale);
            return scale <= 0.0001f ? value : Mathf.Max(0.01f, value / scale);
        }

        private static bool ShouldUseCapsule(Vector3 size)
        {
            float largest = Mathf.Max(size.x, size.y, size.z);
            float secondLargest = size.x + size.y + size.z - largest - Mathf.Min(size.x, size.y, size.z);
            return largest >= secondLargest * 1.35f;
        }

        private static void AddBoxCollider(GameObject instance, Vector3 center, Vector3 size)
        {
            var collider = Undo.AddComponent<BoxCollider>(instance);
            collider.center = center;
            collider.size = new Vector3(
                Mathf.Max(0.01f, size.x),
                Mathf.Max(0.01f, size.y),
                Mathf.Max(0.01f, size.z));
            EditorUtility.SetDirty(collider);
        }

        private static void AddCapsuleCollider(GameObject instance, Vector3 center, Vector3 size)
        {
            var collider = Undo.AddComponent<CapsuleCollider>(instance);
            collider.center = center;

            if (size.x >= size.y && size.x >= size.z)
            {
                collider.direction = 0;
                collider.radius = Mathf.Max(0.01f, Mathf.Max(size.y, size.z) * 0.5f);
                collider.height = Mathf.Max(size.x, collider.radius * 2f);
            }
            else if (size.z >= size.x && size.z >= size.y)
            {
                collider.direction = 2;
                collider.radius = Mathf.Max(0.01f, Mathf.Max(size.x, size.y) * 0.5f);
                collider.height = Mathf.Max(size.z, collider.radius * 2f);
            }
            else
            {
                collider.direction = 1;
                collider.radius = Mathf.Max(0.01f, Mathf.Max(size.x, size.z) * 0.5f);
                collider.height = Mathf.Max(size.y, collider.radius * 2f);
            }

            EditorUtility.SetDirty(collider);
        }

    }
}
#endif
