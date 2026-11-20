using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Editor
{
    public sealed partial class WorldBuilderAuthoringWindow
    {
        [SerializeField] private WorldNavigationLayout _bakeLayout;
        private Label _bakeSummary, _bakeResult;
        private VisualElement _bakeIssues;
        private DropdownField _revision;
        private string _queuedBake, _warningApprover, _warningReason, _bakeWorld;
        private WorldBakeReport _lastBakeReport;

        private void CreateBakeUI(VisualElement parent)
        {
            var section = new Foldout { text = "P07 · 검증·Bake·발행" };
            parent.Add(section);
            var layout = new ObjectField("Navigation Layout") { objectType = typeof(WorldNavigationLayout), value = _bakeLayout };
            layout.RegisterValueChangedCallback(evt => _bakeLayout = evt.newValue as WorldNavigationLayout);
            section.Add(layout);
            section.Add(_bakeSummary = new Label());
            section.Add(_bakeResult = new Label());
            section.Add(_revision = new DropdownField("Staged revision"));
            var approver = new TextField("Warning 승인자");
            approver.RegisterValueChangedCallback(evt => _warningApprover = evt.newValue);
            section.Add(approver);
            var reason = new TextField("Warning 승인 사유");
            reason.RegisterValueChangedCallback(evt => _warningReason = evt.newValue);
            section.Add(reason);

            AddButton(section, "Validate · 저작 입력", () =>
            {
                _lastBakeReport = WorldBakeValidation.Validate(_definition, _bakeLayout);
                _bakeResult.text = _lastBakeReport.HasErrors ? "입력 검증 실패" : "입력 검사 통과 · 실제 AI 경로와 저장 참조는 Bake에서 검사";
                DrawBakeIssues();
            });
            AddButton(section, "Bake · 새 staging", () =>
            {
                var definition = _definition ?? throw new InvalidOperationException("저작 정의를 연결하세요.");
                var profile = _bakeLayout;
                string ticket = _queuedBake = Guid.NewGuid().ToString("N");
                _bakeResult.text = "Bake 대기";
                EditorApplication.delayCall += () =>
                {
                    if (this == null || _queuedBake != ticket) return;
                    _queuedBake = null;
                    Run(() =>
                    {
                        try
                        {
                            var receipt = WorldBakePipeline.Bake(definition, profile, step =>
                            {
                                if (EditorUtility.DisplayCancelableProgressBar("WorldBuilder Bake", step, .5f))
                                    throw new OperationCanceledException("Bake를 취소했습니다.");
                            });
                            _lastBakeReport = receipt.report;
                            _bakeResult.text = "Staging 검증 완료 · " + receipt.buildId;
                            RefreshBakeUI();
                            _revision.SetValueWithoutNotify(receipt.buildId);
                            DrawBakeIssues();
                        }
                        catch (Exception error) { _bakeResult.text = error.Message; RefreshBakeUI(); throw; }
                        finally { EditorUtility.ClearProgressBar(); }
                    });
                };
            });
            AddButton(section, "Cancel · 대기 작업", () => { _queuedBake = null; _bakeResult.text = "대기 작업 취소"; });
            AddButton(section, "Publish · 선택 revision", () =>
            {
                var result = WorldBakePipeline.Publish(_definition, _revision.value, _warningApprover, _warningReason);
                _bakeResult.text = "발행 완료 · " + result.current;
                RefreshBakeUI();
            });
            AddButton(section, "Rollback · 이전 Published", () =>
            {
                var result = WorldBakePipeline.Rollback(_definition.WorldGuid);
                _bakeResult.text = "이전 revision으로 전환 · " + result.current;
                RefreshBakeUI();
            });
            AddButton(section, "보존 지정 전환 · 선택 revision", () =>
            {
                string world = _definition.WorldGuid;
                string build = _revision.value;
                bool retain = !WorldBakePipeline.IsRetained(world, build);
                WorldBakePipeline.SetRetained(world, build, retain);
                _bakeResult.text = retain ? "보존 지정 · " + build : "보존 해제 · " + build;
            });
            AddButton(section, "과거 revision 정리", () =>
            {
                string build = _revision.value;
                if (!EditorUtility.DisplayDialog("과거 revision 정리", build + " 폴더의 소유권과 참조를 확인한 뒤 삭제합니다.", "정리", "취소")) return;
                WorldBakePipeline.RemoveUnusedRevision(_definition.WorldGuid, build);
                _bakeResult.text = "정리 완료 · " + build;
                RefreshBakeUI();
            });
            _revision.RegisterValueChangedCallback(evt => Run(() => ShowRevision(evt.newValue)));
            section.Add(_bakeIssues = new VisualElement());
            RefreshBakeUI();
        }

        private void RefreshBakeUI()
        {
            if (_bakeSummary == null) return;
            bool worldChanged = _bakeWorld != _definition?.WorldGuid;
            _bakeWorld = _definition?.WorldGuid;
            if (_definition == null)
            {
                _bakeSummary.text = "저작 정의를 연결하세요.";
                _revision.choices.Clear();
                _revision.SetValueWithoutNotify(null);
                ShowRevision(null);
                return;
            }
            var dirty = _definition.DirtyRegion;
            string region = dirty.HasValue ? $"X{dirty.Value.X}, Z{dirty.Value.Z}, {dirty.Value.Width}×{dirty.Value.Height}" : "없음";
            string published;
            try { published = WorldBakePipeline.ReadPublished(_definition.WorldGuid)?.current ?? "없음"; }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException)
            {
                published = "읽기 실패 · " + error.Message;
            }
            _bakeSummary.text = $"월드 {_definition.WorldGuid}\n소스 {_definition.SourceRevision}\nDirty {region}\nPublished {published}";
            var ids = WorldBakePipeline.BuildIds(_definition.WorldGuid).ToList();
            string previous = _revision.value;
            _revision.choices = ids;
            string selected = ids.Contains(previous) ? previous : ids.FirstOrDefault();
            _revision.SetValueWithoutNotify(selected);
            if (worldChanged) ShowRevision(selected);
        }

        private void ShowRevision(string build)
        {
            _lastBakeReport = null;
            _bakeResult.text = "";
            if (_definition != null && !string.IsNullOrEmpty(build))
            {
                try
                {
                    var receipt = WorldBakePipeline.LoadReceipt(_definition.WorldGuid, build);
                    _lastBakeReport = receipt.report;
                    _bakeResult.text = receipt.state + " · " + (receipt.failure ?? receipt.lastStep);
                }
                catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException)
                {
                    _bakeResult.text = "미완료 · " + error.Message;
                }
            }
            DrawBakeIssues();
        }

        private void DrawBakeIssues()
        {
            _bakeIssues.Clear();
            if (_lastBakeReport == null) return;
            foreach (var entry in _lastBakeReport.issues)
            {
                var issue = entry;
                string text = $"{issue.severity} · {issue.code} · {issue.message}\n{issue.elementId ?? issue.sourceGuid} · {issue.fixHint}";
                _bakeIssues.Add(new Button(() => LocateBakeIssue(issue)) { text = text, style = { whiteSpace = WhiteSpace.Normal } });
            }
        }

        private void LocateBakeIssue(WorldBakeIssue issue)
        {
            string id = issue.elementId;
            if (!string.IsNullOrEmpty(id))
            {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    var placement = root.GetComponentsInChildren<WorldPlacementInstance>(true).FirstOrDefault(p => p.record?.id == id);
                    var structure = root.GetComponentsInChildren<WorldPlanningInstance>(true).FirstOrDefault(p => p.record?.id == id);
                    var module = root.GetComponentsInChildren<WorldCaveModule>(true).FirstOrDefault(m =>
                        m.moduleId == id || m.surfaceId == id || m.caveGuid == id);
                    GameObject target = placement != null ? placement.gameObject : structure != null ? structure.gameObject : module?.gameObject;
                    if (target == null) continue;
                    Selection.activeGameObject = target;
                    EditorGUIUtility.PingObject(target);
                    break;
                }
            }
            if (issue.hasLocation)
                SceneView.lastActiveSceneView?.Frame(new Bounds(issue.worldBounds.center, Vector3.one * 8), false);
        }
    }
}

