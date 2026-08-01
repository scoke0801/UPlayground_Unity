#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>
    /// 스캐터 브러시. 채집물·장식물처럼 같은 배치물을 대량으로 흩뿌릴 때 쓴다.
    /// 그룹 프리셋과 목적이 달라(조우 저작 vs 대량 배치) 별도 모드로 둔다.
    /// </summary>
    public partial class GatheringPlacementEditorWindow
    {
        private bool _brushEnabled;
        private float _brushRadius = 4f;
        private int _brushDensity = 3;
        private float _brushMinSpacing = 1.5f;
        private float _brushMaxSlope = 40f;
        private bool _brushUseFixedSeed;
        private int _brushSeed = 12345;
        private bool _brushFoldout;

        private bool _brushStrokeActive;
        private bool _brushErasing;
        private PlacementUndoScope _brushStrokeScope;
        private readonly List<Vector3> _brushStrokePoints = new();
        private readonly List<Vector3> _brushExistingPlacementPoints = new();
        private int _brushStrokePlaced;
        private Random.State _brushRandomState;

        private bool IsBrushMode => _brushEnabled && _worldPlacementMode != WorldPlacementMode.CycleSpawn && !IsGroupPresetMode;

        #region 설정 UI

        private void DrawBrushSettings()
        {
            EditorGUILayout.Space(6f);
            _brushFoldout = EditorGUILayout.Foldout(_brushFoldout, "스캐터 브러시", true);
            if (!_brushFoldout)
                return;

            EditorGUI.indentLevel++;

            _brushEnabled = EditorGUILayout.Toggle("브러시 사용", _brushEnabled);
            using (new EditorGUI.DisabledScope(!_brushEnabled))
            {
                _brushRadius = Mathf.Max(0.5f, EditorGUILayout.FloatField("브러시 반경", _brushRadius));
                _brushDensity = Mathf.Max(1, EditorGUILayout.IntField("스텝당 시도 횟수", _brushDensity));
                _brushMinSpacing = Mathf.Max(0f, EditorGUILayout.FloatField("최소 간격", _brushMinSpacing));
                _brushMaxSlope = EditorGUILayout.Slider("최대 경사", _brushMaxSlope, 0f, 90f);

                _brushUseFixedSeed = EditorGUILayout.Toggle("시드 고정", _brushUseFixedSeed);
                using (new EditorGUI.DisabledScope(!_brushUseFixedSeed))
                    _brushSeed = EditorGUILayout.IntField("시드", _brushSeed);

                EditorGUILayout.HelpBox(
                    "씬 뷰에서 드래그하면 흩뿌립니다. Ctrl+드래그는 브러시 반경 안의 배치물을 지웁니다.\n" +
                    "드래그 스트로크 하나가 Undo 1회입니다.",
                    MessageType.None);
            }

            EditorGUI.indentLevel--;
        }

        #endregion

        #region 씬 입력

        /// <summary>브러시가 씬 입력을 소비했으면 true.</summary>
        private bool HandleBrushSceneInput(Event currentEvent, SceneView sceneView)
        {
            if (!IsBrushMode)
                return false;

            switch (currentEvent.type)
            {
                case EventType.MouseDown when currentEvent.button == 0 && !currentEvent.alt:
                    BeginBrushStroke(currentEvent.control || currentEvent.command);
                    ApplyBrushStep();
                    currentEvent.Use();
                    return true;

                case EventType.MouseDrag when _brushStrokeActive && currentEvent.button == 0:
                    ApplyBrushStep();
                    sceneView.Repaint();
                    currentEvent.Use();
                    return true;

                case EventType.MouseUp when _brushStrokeActive && currentEvent.button == 0:
                    EndBrushStroke();
                    currentEvent.Use();
                    return true;
            }

            return false;
        }

        private void BeginBrushStroke(bool erasing)
        {
            _brushStrokeActive = true;
            _brushErasing = erasing;
            _brushStrokePlaced = 0;
            _brushStrokePoints.Clear();
            _brushExistingPlacementPoints.Clear();
            _brushStrokeScope = new PlacementUndoScope(erasing ? "Brush Erase" : "Brush Scatter");

            if (!erasing && _brushMinSpacing > 0f)
            {
                foreach (var placement in GetPlacementQueryCache(forceRefresh: true))
                    if (placement != null)
                        _brushExistingPlacementPoints.Add(placement.transform.position);
            }

            if (_brushUseFixedSeed)
            {
                _brushRandomState = Random.state;
                Random.InitState(_brushSeed);
            }
        }

        private void EndBrushStroke()
        {
            _brushStrokeActive = false;

            if (_brushStrokePlaced > 0)
                _brushStrokeScope.Complete();

            _brushStrokeScope.Dispose();
            _brushStrokeScope = null;

            if (_brushUseFixedSeed)
                Random.state = _brushRandomState;

            string verb = _brushErasing ? "제거" : "배치";
            SetTemporaryStatus($"브러시 {verb} {_brushStrokePlaced}개 (이번 세션 {_sessionPlacementCount}개)",
                _brushStrokePlaced > 0 ? MessageType.Info : MessageType.Warning);

            if (_brushStrokePlaced > 0)
            {
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                InvalidatePlacementQueryCache();
            }

            Repaint();
        }

        private void ApplyBrushStep()
        {
            if (!_hasPreviewHit)
                return;

            if (_brushErasing)
            {
                EraseUnderBrush();
                return;
            }

            if (!HasSelectedPlacementData())
                return;

            for (int i = 0; i < _brushDensity; i++)
                TryScatterOne();
        }

        private void TryScatterOne()
        {
            Vector2 offset = Random.insideUnitCircle * _brushRadius;
            var flat = new Vector3(_previewPosition.x + offset.x, _previewPosition.y, _previewPosition.z + offset.y);

            if (!TryResolveMemberSurface(flat, out Vector3 position, out Vector3 normal))
                return;

            if (Vector3.Angle(normal, Vector3.up) > _brushMaxSlope)
                return;

            if (!IsBrushSpacingSatisfied(position))
                return;

            // 배치 파이프라인(메타데이터, 콜라이더, SceneEntityId)을 그대로 재사용한다.
            Vector3 savedPosition = _previewPosition;
            Vector3 savedNormal = _previewNormal;
            bool savedSelect = _selectAfterPlace;

            _previewPosition = position;
            _previewNormal = normal;
            _selectAfterPlace = false;

            bool placed = PlaceCurrentInternal();

            _previewPosition = savedPosition;
            _previewNormal = savedNormal;
            _selectAfterPlace = savedSelect;

            if (!placed)
                return;

            _brushStrokePoints.Add(position);
            _brushStrokePlaced++;
        }

        /// <summary>스트로크 내부 점과 씬의 기존 배치물 양쪽에 대해 최소 간격을 검사한다.</summary>
        private bool IsBrushSpacingSatisfied(Vector3 position)
        {
            if (_brushMinSpacing <= 0f)
                return true;

            float sqrSpacing = _brushMinSpacing * _brushMinSpacing;

            foreach (var point in _brushStrokePoints)
                if ((point - position).sqrMagnitude < sqrSpacing)
                    return false;

            foreach (var point in _brushExistingPlacementPoints)
                if ((point - position).sqrMagnitude < sqrSpacing)
                    return false;

            return true;
        }

        private void EraseUnderBrush()
        {
            float sqrRadius = _brushRadius * _brushRadius;

            var placements = UnityEngine.Object.FindObjectsByType<WorldPlacementMetadata>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var placement in placements)
            {
                if (placement == null)
                    continue;

                if ((placement.transform.position - _previewPosition).sqrMagnitude > sqrRadius)
                    continue;

                // 그룹 앵커까지 지우면 조우 구성이 통째로 날아가므로 멤버만 제거한다.
                if (placement.GetComponent<UPlayGround.Group.MonsterGroupController>() != null)
                    continue;

                Undo.DestroyObjectImmediate(placement.gameObject);
                _brushStrokePlaced++;
            }
        }

        #endregion

        #region 씬 프리뷰

        private void DrawBrushScenePreview()
        {
            if (!IsBrushMode || !_hasPreviewHit)
                return;

            Handles.color = _brushErasing
                ? new Color(0.95f, 0.35f, 0.35f, 0.9f)
                : new Color(0.35f, 0.8f, 1f, 0.9f);

            Handles.DrawWireDisc(_previewPosition, _previewNormal, _brushRadius);
            Handles.DrawWireDisc(_previewPosition, _previewNormal, _brushRadius * 0.5f);

            string label = _brushErasing
                ? $"브러시 지우기 r={_brushRadius:0.#}"
                : $"브러시 r={_brushRadius:0.#} · 간격 {_brushMinSpacing:0.#}";
            Handles.Label(_previewPosition + Vector3.up * 1.25f, label);
        }

        #endregion
    }
}
#endif
